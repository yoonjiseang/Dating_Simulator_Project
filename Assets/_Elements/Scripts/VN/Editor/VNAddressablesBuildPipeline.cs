#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using VN.Data;

namespace VN.Editor
{
    public static class VNAddressablesBuildPipeline
    {
        private const string ResourceRoot = "Assets/_ElementsResources/VN";
        private const string BootstrapPrefabRoot = "Assets/_ElementsBundles/Resources/VN";
        private const string CatalogAssetPath = ResourceRoot + "/VNResourceCatalog.asset";
        private const string GroupName = "VNContent";
        private const string AddressPrefix = "VN/";

        private static readonly string[] AllowedExtensions =
        {
            ".png", ".jpg", ".jpeg", ".wav", ".mp3", ".ogg", ".prefab", ".asset"
        };
        
        private static readonly (string assetPath, string address)[] BootstrapPrefabs =
        {
            ($"{BootstrapPrefabRoot}/ViewStoryTop.prefab", "VN/ViewStoryTop"),
            ($"{BootstrapPrefabRoot}/VNGameController.prefab", "VN/VNGameController")
        };

        [MenuItem("Tools/VN/Addressables/Sync + Build")]
        public static void BuildFromEditorMenu()
        {
            BuildAddressablesForCi();
            EditorUtility.DisplayDialog("Addressables", "VN Addressables sync/build finished.", "OK");
        }

        public static void BuildAddressablesForCi()
        {
            VNMasterDataBuildPipeline.BuildForCi();

            var report = SyncCatalogAndAddressables();
            if (report.MissingCatalogEntries.Count > 0)
            {
                throw new BuildFailedException("Catalog is missing assets:\n" + string.Join("\n", report.MissingCatalogEntries));
            }

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                throw new BuildFailedException("Addressables settings not found after sync.");
            }

            AddressableAssetSettings.BuildPlayerContent(out var result);
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new BuildFailedException($"Addressables build failed: {result.Error}");
            }

            Debug.Log($"[VNAddressablesBuildPipeline] Addressables build succeeded. Entries: {report.CatalogCount}");
        }

        public static void ValidateAddressablesForCi()
        {
            VNMasterDataBuildPipeline.ValidateForCi();
            
            var report = SyncCatalogAndAddressables();
            if (report.MissingCatalogEntries.Count > 0)
            {
                throw new BuildFailedException("Missing resources in VNResourceCatalog:\n" + string.Join("\n", report.MissingCatalogEntries));
            }

            if (report.MissingAddressableEntries.Count > 0)
            {
                throw new BuildFailedException("Missing Addressables keys:\n" + string.Join("\n", report.MissingAddressableEntries));
            }

            Debug.Log($"[VNAddressablesBuildPipeline] Validation passed. Catalog={report.CatalogCount}, Addressables={report.AddressableCount}");
        }

        private static SyncReport SyncCatalogAndAddressables()
        {
            var entries = ScanResourceEntries();
            SaveCatalog(entries);

            var settings = EnsureAddressableSettings();
            var group = EnsureGroup(settings, GroupName);

            var missingAddressables = new List<string>();
            foreach (var entry in entries)
            {
                var guid = entry.guid;
                if (string.IsNullOrWhiteSpace(guid))
                {
                    continue;
                }

                var assetEntry = settings.FindAssetEntry(guid) ?? settings.CreateOrMoveEntry(guid, group, false, false);
                if (assetEntry == null)
                {
                    missingAddressables.Add(entry.assetPath);
                    continue;
                }

                assetEntry.address = ToAddress(entry.relativeKey);
                assetEntry.SetLabel("vn", true, true, false);
                assetEntry.SetLabel(entry.category.ToString().ToLowerInvariant(), true, true, false);
            }

            foreach (var prefab in BootstrapPrefabs)
            {
                var guid = AssetDatabase.AssetPathToGUID(prefab.assetPath);
                if (string.IsNullOrWhiteSpace(guid))
                {
                    missingAddressables.Add(prefab.assetPath);
                    continue;
                }

                var assetEntry = settings.FindAssetEntry(guid) ?? settings.CreateOrMoveEntry(guid, group, false, false);
                if (assetEntry == null)
                {
                    missingAddressables.Add(prefab.assetPath);
                    continue;
                }

                assetEntry.address = prefab.address;
                assetEntry.SetLabel("vn", true, true, false);
                assetEntry.SetLabel("bootstrap", true, true, false);
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var scannedPathSet = new HashSet<string>(entries.Select(e => e.assetPath), StringComparer.OrdinalIgnoreCase);
            var allCandidateAssets = FindCandidateAssets();
            var missingCatalog = allCandidateAssets.Where(path => !scannedPathSet.Contains(path)).ToList();

            return new SyncReport
            {
                CatalogCount = entries.Count,
                AddressableCount = settings.groups.SelectMany(g => g.entries).Count(),
                MissingCatalogEntries = missingCatalog,
                MissingAddressableEntries = missingAddressables
            };
        }

        private static List<VNResourceEntry> ScanResourceEntries()
        {
            var entries = new List<VNResourceEntry>();
            if (!AssetDatabase.IsValidFolder(ResourceRoot))
            {
                return entries;
            }

            foreach (var path in FindCandidateAssets())
            {
                var guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrWhiteSpace(guid))
                {
                    continue;
                }

                var relativePath = path.Substring(ResourceRoot.Length + 1);
                var relativeWithoutExt = Path.ChangeExtension(relativePath, null)?.Replace('\\', '/');
                if (string.IsNullOrWhiteSpace(relativeWithoutExt))
                {
                    continue;
                }

                entries.Add(new VNResourceEntry
                {
                    id = relativeWithoutExt,
                    fileName = Path.GetFileNameWithoutExtension(path),
                    relativeKey = relativeWithoutExt,
                    assetPath = path,
                    guid = guid,
                    category = ResolveCategory(relativeWithoutExt)
                });
            }

            return entries.OrderBy(e => e.id, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static List<string> FindCandidateAssets()
        {
            var guids = AssetDatabase.FindAssets(string.Empty, new[] { ResourceRoot });
            var paths = new List<string>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path))
                {
                    continue;
                }

                var extension = Path.GetExtension(path);
                if (!AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                paths.Add(path);
            }

            return paths;
        }

        private static VNResourceCategory ResolveCategory(string relativeWithoutExt)
        {
            if (relativeWithoutExt.StartsWith("Backgrounds/", StringComparison.OrdinalIgnoreCase))
            {
                return VNResourceCategory.Background;
            }

            if (relativeWithoutExt.StartsWith("BGM/", StringComparison.OrdinalIgnoreCase))
            {
                return VNResourceCategory.Bgm;
            }

            if (relativeWithoutExt.StartsWith("SFX/", StringComparison.OrdinalIgnoreCase))
            {
                return VNResourceCategory.Sfx;
            }

            return VNResourceCategory.Character;
        }

        private static void SaveCatalog(List<VNResourceEntry> entries)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<VNResourceCatalog>(CatalogAssetPath);
            if (catalog == null)
            {
                var dir = Path.GetDirectoryName(CatalogAssetPath);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                catalog = ScriptableObject.CreateInstance<VNResourceCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
            }

            catalog.SetEntries(entries);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static AddressableAssetSettings EnsureAddressableSettings()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null)
            {
                return settings;
            }

            var settingsFolder = "Assets/AddressableAssetsData";
            if (!AssetDatabase.IsValidFolder(settingsFolder))
            {
                AssetDatabase.CreateFolder("Assets", "AddressableAssetsData");
            }

            settings = AddressableAssetSettings.Create(settingsFolder, "AddressableAssetSettings", true, true);
            AddressableAssetSettingsDefaultObject.Settings = settings;
            return settings;
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

        private static string ToAddress(string relativeKey)
        {
            return AddressPrefix + relativeKey.Trim().Replace('\\', '/');
        }

        private sealed class SyncReport
        {
            public int CatalogCount;
            public int AddressableCount;
            public List<string> MissingCatalogEntries = new();
            public List<string> MissingAddressableEntries = new();
        }
    }
}
#endif