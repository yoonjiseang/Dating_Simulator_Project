#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using VN.Data;

namespace VN.Editor
{
    public sealed class VNResourceCatalogGeneratorWindow : EditorWindow
    {
        private const string ResourceRoot = "Assets/_ElementsResources/VN";
        private const string DefaultCatalogPath = ResourceRoot + "/VNResourceCatalog.asset";
        private const string DefaultCsvPath = ResourceRoot + "/VNResourceCatalog.csv";

        private static readonly CategoryConfig[] CategoryConfigs =
        {
            new("Backgrounds", VNResourceCategory.Background, new[] { ".png", ".jpg", ".jpeg" }),
            new("BGM", VNResourceCategory.Bgm, new[] { ".wav", ".mp3", ".ogg" }),
            new("Characters", VNResourceCategory.Character, new[] { ".png", ".jpg", ".jpeg", ".wav", ".mp3", ".ogg" }),
            new("SFX", VNResourceCategory.Sfx, new[] { ".wav", ".mp3", ".ogg" })
        };

        [SerializeField] private string catalogPath = DefaultCatalogPath;
        [SerializeField] private bool exportCsv = true;
        [SerializeField] private string csvPath = DefaultCsvPath;

        [MenuItem("Tools/VN/Generate Resource Catalog")]
        public static void OpenWindow()
        {
            var window = GetWindow<VNResourceCatalogGeneratorWindow>("VN Resource Catalog");
            window.minSize = new Vector2(560f, 240f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("VN Resource Catalog Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Scan VN resource folders and generate ScriptableObject + optional CSV for ID/path management.", MessageType.Info);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Scan Targets", EditorStyles.boldLabel);
                foreach (var config in CategoryConfigs)
                {
                    EditorGUILayout.LabelField($"• {ResourceRoot}/{config.folderName}");
                }
            }

            catalogPath = EditorGUILayout.TextField("Catalog Asset Path", catalogPath);
            exportCsv = EditorGUILayout.ToggleLeft("Export CSV", exportCsv);

            using (new EditorGUI.DisabledScope(!exportCsv))
            {
                csvPath = EditorGUILayout.TextField("CSV Path", csvPath);
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Generate Catalog", GUILayout.Height(36f)))
            {
                GenerateCatalog();
            }
        }

        private void GenerateCatalog()
        {
            if (!ValidatePath(catalogPath, ".asset", out var catalogError))
            {
                EditorUtility.DisplayDialog("Invalid Catalog Path", catalogError, "OK");
                return;
            }

            if (exportCsv && !ValidatePath(csvPath, ".csv", out var csvError))
            {
                EditorUtility.DisplayDialog("Invalid CSV Path", csvError, "OK");
                return;
            }

            var entries = BuildEntries();
            entries.Sort((left, right) => string.Compare(left.id, right.id, StringComparison.OrdinalIgnoreCase));

            var catalog = LoadOrCreateCatalog(catalogPath);
            catalog.SetEntries(entries);

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            if (exportCsv)
            {
                WriteCsv(csvPath, entries);
                AssetDatabase.ImportAsset(csvPath, ImportAssetOptions.ForceUpdate);
            }

            AssetDatabase.Refresh();
            Debug.Log($"[VNResourceCatalogGenerator] Generated {entries.Count} entries. Catalog: {catalogPath}" +
                      (exportCsv ? $", CSV: {csvPath}" : string.Empty));
            EditorUtility.DisplayDialog("Catalog Generated", $"Generated {entries.Count} resources.", "OK");
        }

        private static bool ValidatePath(string path, string requiredExtension, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("Assets/", StringComparison.Ordinal))
            {
                error = "Path must start with 'Assets/'.";
                return false;
            }

            if (!path.EndsWith(requiredExtension, StringComparison.OrdinalIgnoreCase))
            {
                error = $"Path must end with '{requiredExtension}'.";
                return false;
            }

            return true;
        }

        private static VNResourceCatalog LoadOrCreateCatalog(string path)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<VNResourceCatalog>(path);
            if (catalog != null)
            {
                return catalog;
            }

            EnsureDirectory(path);
            catalog = CreateInstance<VNResourceCatalog>();
            AssetDatabase.CreateAsset(catalog, path);
            return catalog;
        }

        private static List<VNResourceEntry> BuildEntries()
        {
            var entries = new List<VNResourceEntry>();

            foreach (var config in CategoryConfigs)
            {
                var folderPath = $"{ResourceRoot}/{config.folderName}";
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    Debug.LogWarning($"[VNResourceCatalogGenerator] Missing folder: {folderPath}");
                    continue;
                }

                var guids = AssetDatabase.FindAssets(string.Empty, new[] { folderPath });
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetDatabase.IsValidFolder(path))
                    {
                        continue;
                    }

                    var extension = Path.GetExtension(path);
                    if (!config.extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var relativePath = path.Substring(folderPath.Length + 1);
                    var relativeWithoutExt = Path.ChangeExtension(relativePath, null)?.Replace('\\', '/');
                    var id = $"{config.folderName}/{relativeWithoutExt}";

                    entries.Add(new VNResourceEntry
                    {
                        id = id,
                        fileName = Path.GetFileNameWithoutExtension(path),
                        relativeKey = relativeWithoutExt,
                        assetPath = path,
                        guid = guid,
                        category = config.category
                    });
                }
            }

            return entries;
        }

        private static void WriteCsv(string path, IReadOnlyList<VNResourceEntry> entries)
        {
            EnsureDirectory(path);

            var builder = new StringBuilder();
            builder.AppendLine("id,category,fileName,relativeKey,assetPath,guid");

            foreach (var entry in entries)
            {
                builder.AppendLine(string.Join(",",
                    Escape(entry.id),
                    Escape(entry.category.ToString()),
                    Escape(entry.fileName),
                    Escape(entry.relativeKey),
                    Escape(entry.assetPath),
                    Escape(entry.guid)));
            }

            File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
        }

        private static string Escape(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value.Contains(',') || value.Contains('"'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }

        private static void EnsureDirectory(string assetPath)
        {
            var dir = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrWhiteSpace(dir))
            {
                return;
            }

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private readonly struct CategoryConfig
        {
            public readonly string folderName;
            public readonly VNResourceCategory category;
            public readonly string[] extensions;

            public CategoryConfig(string folderName, VNResourceCategory category, string[] extensions)
            {
                this.folderName = folderName;
                this.category = category;
                this.extensions = extensions;
            }
        }
    }
}
#endif