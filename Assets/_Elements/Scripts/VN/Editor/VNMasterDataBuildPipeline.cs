#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build;
using UnityEngine;
using VN.Data;

namespace VN.Editor
{
    /// <summary>
    /// CSV(Assets/_ElementsResources/csv) -> table .bytes(Assets/_ElementsResources/MasterData) build pipeline.
    /// Each table is mapped to its own Addressables group for independent distribution.
    /// </summary>
    public static class VNMasterDataBuildPipeline
    {
        private const string CsvRoot = "Assets/_ElementsResources/csv";
        private const string MasterDataRoot = "Assets/_ElementsResources/MasterData";
        private const string AddressPrefix = "MDB/";
        private const string GroupPrefix = "MDB_";
        private const string ManagedLabel = "mdb";

        [MenuItem("Tools/VN/MasterData/Build CSV -> .bytes + Sync Addressables")]
        public static void BuildFromEditorMenu()
        {
            var report = BuildAndSync();
            EditorUtility.DisplayDialog(
                "MasterData Build",
                $"Built {report.BuiltTables.Count} tables.\nRemoved {report.RemovedTables.Count} stale tables.\nErrors: {report.Errors.Count}",
                "OK");
        }

        public static void BuildForCi()
        {
            var report = BuildAndSync();
            if (report.Errors.Count > 0)
            {
                throw new BuildFailedException("MasterData build failed:\n" + string.Join("\n", report.Errors));
            }

            Debug.Log($"[VNMasterDataBuildPipeline] Build succeeded. Built={report.BuiltTables.Count}, Removed={report.RemovedTables.Count}");
        }

        public static void ValidateForCi()
        {
            var report = BuildAndSync();
            if (report.Errors.Count > 0)
            {
                throw new BuildFailedException("MasterData validation failed:\n" + string.Join("\n", report.Errors));
            }

            if (report.MissingAddressables.Count > 0)
            {
                throw new BuildFailedException("MasterData missing addressable entries:\n" + string.Join("\n", report.MissingAddressables));
            }

            Debug.Log($"[VNMasterDataBuildPipeline] Validation succeeded. Tables={report.BuiltTables.Count}");
        }

        private static MasterDataBuildReport BuildAndSync()
        {
            var report = new MasterDataBuildReport();
            EnsureFolders();

            var csvPaths = FindCsvFiles();
            var builtTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var csvPath in csvPaths)
            {
                try
                {
                    var table = BuildTableFromCsv(csvPath);
                    var bytesPath = WriteTableBytes(table);
                    report.BuiltTables.Add(table.tableName);
                    report.GeneratedAssets.Add(bytesPath);
                    builtTables.Add(table.tableName);
                }
                catch (Exception ex)
                {
                    report.Errors.Add($"{csvPath}: {ex.Message}");
                }
            }

            CleanupStaleMasterDataAssets(builtTables, report);

            AssetDatabase.Refresh();
            var settings = EnsureAddressableSettings();
            SyncAddressables(settings, builtTables, report);

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return report;
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_ElementsResources"))
            {
                AssetDatabase.CreateFolder("Assets", "_ElementsResources");
            }

            if (!AssetDatabase.IsValidFolder(CsvRoot))
            {
                AssetDatabase.CreateFolder("Assets/_ElementsResources", "csv");
            }

            if (!AssetDatabase.IsValidFolder(MasterDataRoot))
            {
                AssetDatabase.CreateFolder("Assets/_ElementsResources", "MasterData");
            }
        }

        private static List<string> FindCsvFiles()
        {
            if (!AssetDatabase.IsValidFolder(CsvRoot))
            {
                return new List<string>();
            }

            var guids = AssetDatabase.FindAssets("t:TextAsset", new[] { CsvRoot });
            var files = new List<string>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    files.Add(path);
                }
            }

            files.Sort(StringComparer.OrdinalIgnoreCase);
            return files;
        }

        private static MasterDataTable BuildTableFromCsv(string csvPath)
        {
            var raw = File.ReadAllText(csvPath, Encoding.UTF8);
            var tableName = Path.GetFileNameWithoutExtension(csvPath)?.Trim();
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new InvalidDataException("Unable to resolve table name from file path.");
            }

            var records = ParseCsv(raw);
            if (records.Count == 0)
            {
                throw new InvalidDataException("CSV is empty.");
            }

            var headers = records[0].Select(h => (h ?? string.Empty).Trim()).ToArray();
            ValidateHeaders(headers);

            var rows = new List<MasterDataRow>();
            for (var i = 1; i < records.Count; i++)
            {
                var sourceRow = records[i];
                if (IsCompletelyEmptyRow(sourceRow))
                {
                    continue;
                }

                if (sourceRow.Count > headers.Length)
                {
                    throw new InvalidDataException($"Row {i + 1} has {sourceRow.Count} columns; expected {headers.Length}.");
                }

                var normalizedCells = new string[headers.Length];
                for (var c = 0; c < headers.Length; c++)
                {
                    normalizedCells[c] = c < sourceRow.Count ? (sourceRow[c] ?? string.Empty).Trim() : string.Empty;
                }

                rows.Add(new MasterDataRow { cells = normalizedCells });
            }

            if (rows.Count == 0)
            {
                throw new InvalidDataException("CSV has no data rows.");
            }

            ValidateIdColumnIfExists(headers, rows);

            return new MasterDataTable
            {
                tableName = tableName,
                sourceHash = ComputeSha256(raw),
                headers = headers,
                rows = rows.ToArray()
            };
        }

        private static void ValidateHeaders(IReadOnlyList<string> headers)
        {
            if (headers.Count == 0)
            {
                throw new InvalidDataException("CSV header row is missing.");
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
            {
                var header = headers[i];
                if (string.IsNullOrWhiteSpace(header))
                {
                    throw new InvalidDataException($"Header at column {i + 1} is empty.");
                }

                if (!seen.Add(header))
                {
                    throw new InvalidDataException($"Duplicate header detected: '{header}'.");
                }
            }
        }

        private static void ValidateIdColumnIfExists(string[] headers, IReadOnlyList<MasterDataRow> rows)
        {
            var idIndex = Array.FindIndex(headers, h => string.Equals(h, "id", StringComparison.OrdinalIgnoreCase));
            if (idIndex < 0)
            {
                return;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < rows.Count; i++)
            {
                var id = rows[i].GetCell(idIndex);
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new InvalidDataException($"Row {i + 2} has empty id.");
                }

                if (!seen.Add(id))
                {
                    throw new InvalidDataException($"Duplicate id detected: '{id}' (row {i + 2}).");
                }
            }
        }

        private static bool IsCompletelyEmptyRow(IReadOnlyList<string> row)
        {
            for (var i = 0; i < row.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(row[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static string WriteTableBytes(MasterDataTable table)
        {
            var bytes = MasterDataBinarySerializer.Serialize(table);
            var path = $"{MasterDataRoot}/{table.tableName}.bytes";
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return path;
        }

        private static void CleanupStaleMasterDataAssets(ISet<string> builtTables, MasterDataBuildReport report)
        {
            if (!AssetDatabase.IsValidFolder(MasterDataRoot))
            {
                return;
            }

            var guids = AssetDatabase.FindAssets("t:TextAsset", new[] { MasterDataRoot });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!Path.GetExtension(path).Equals(".bytes", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var tableName = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrWhiteSpace(tableName) || builtTables.Contains(tableName))
                {
                    continue;
                }

                if (AssetDatabase.DeleteAsset(path))
                {
                    report.RemovedTables.Add(tableName);
                }
            }
        }

        private static AddressableAssetSettings EnsureAddressableSettings()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null)
            {
                return settings;
            }

            const string settingsFolder = "Assets/AddressableAssetsData";
            if (!AssetDatabase.IsValidFolder(settingsFolder))
            {
                AssetDatabase.CreateFolder("Assets", "AddressableAssetsData");
            }

            settings = AddressableAssetSettings.Create(settingsFolder, "AddressableAssetSettings", true, true);
            AddressableAssetSettingsDefaultObject.Settings = settings;
            return settings;
        }

        private static void SyncAddressables(AddressableAssetSettings settings, ISet<string> builtTables, MasterDataBuildReport report)
        {
            foreach (var tableName in builtTables.OrderBy(v => v, StringComparer.OrdinalIgnoreCase))
            {
                var assetPath = $"{MasterDataRoot}/{tableName}.bytes";
                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrWhiteSpace(guid))
                {
                    report.MissingAddressables.Add(assetPath);
                    continue;
                }

                var group = EnsureGroup(settings, GroupPrefix + tableName);
                var entry = settings.FindAssetEntry(guid) ?? settings.CreateOrMoveEntry(guid, group, false, false);
                if (entry == null)
                {
                    report.MissingAddressables.Add(assetPath);
                    continue;
                }

                entry.address = AddressPrefix + tableName;
                entry.SetLabel(ManagedLabel, true, true, false);
                entry.SetLabel($"mdb_{tableName.ToLowerInvariant()}", true, true, false);
            }

            CleanupStaleAddressableGroups(settings, builtTables);
        }

        private static void CleanupStaleAddressableGroups(AddressableAssetSettings settings, ISet<string> activeTables)
        {
            var groups = settings.groups
                .Where(g => g != null && g.Name.StartsWith(GroupPrefix, StringComparison.Ordinal))
                .ToList();

            foreach (var group in groups)
            {
                var tableName = group.Name.Substring(GroupPrefix.Length);
                if (activeTables.Contains(tableName))
                {
                    continue;
                }

                settings.RemoveGroup(group);
            }
        }

        private static AddressableAssetGroup EnsureGroup(AddressableAssetSettings settings, string groupName)
        {
            var group = settings.FindGroup(groupName);
            if (group != null)
            {
                return group;
            }

            return settings.CreateGroup(groupName, false, false, false,
                new List<AddressableAssetGroupSchema>(),
                typeof(BundledAssetGroupSchema),
                typeof(ContentUpdateGroupSchema));
        }

        private static string ComputeSha256(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            byte[] hash;
            using (var sha = SHA256.Create())
            {
                hash = sha.ComputeHash(bytes);
            }
            var builder = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }

        private static List<List<string>> ParseCsv(string csvText)
        {
            var rows = new List<List<string>>();
            var currentRow = new List<string>();
            var currentCell = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < csvText.Length; i++)
            {
                var ch = csvText[i];

                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        var hasEscapedQuote = i + 1 < csvText.Length && csvText[i + 1] == '"';
                        if (hasEscapedQuote)
                        {
                            currentCell.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        currentCell.Append(ch);
                    }

                    continue;
                }

                switch (ch)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        currentRow.Add(currentCell.ToString());
                        currentCell.Clear();
                        break;
                    case '\r':
                        break;
                    case '\n':
                        currentRow.Add(currentCell.ToString());
                        currentCell.Clear();
                        rows.Add(currentRow);
                        currentRow = new List<string>();
                        break;
                    default:
                        currentCell.Append(ch);
                        break;
                }
            }

            if (inQuotes)
            {
                throw new InvalidDataException("CSV has unclosed quoted field.");
            }

            if (currentCell.Length > 0 || currentRow.Count > 0)
            {
                currentRow.Add(currentCell.ToString());
                rows.Add(currentRow);
            }

            // Strip UTF-8 BOM from first header cell if present.
            if (rows.Count > 0 && rows[0].Count > 0 && rows[0][0].Length > 0 && rows[0][0][0] == '\uFEFF')
            {
                rows[0][0] = rows[0][0].TrimStart('\uFEFF');
            }

            return rows;
        }

        private sealed class MasterDataBuildReport
        {
            public readonly List<string> BuiltTables = new();
            public readonly List<string> RemovedTables = new();
            public readonly List<string> GeneratedAssets = new();
            public readonly List<string> MissingAddressables = new();
            public readonly List<string> Errors = new();
        }
    }
}
#endif