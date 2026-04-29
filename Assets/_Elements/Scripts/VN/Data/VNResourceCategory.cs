using System;
using System.Collections.Generic;
using UnityEngine;

namespace VN.Data
{
    public enum VNResourceCategory
    {
        Background,
        Bgm,
        Character,
        Sfx,
        Story
    }

    [Serializable]
    public sealed class VNResourceEntry
    {
        public string id;
        public string fileName;
        public string relativeKey;
        public string assetPath;
        public string guid;
        public VNResourceCategory category;
    }

    [CreateAssetMenu(fileName = "VNResourceCatalog", menuName = "VN/Resource Catalog", order = 30)]
    public sealed class VNResourceCatalog : ScriptableObject
    {
        [SerializeField] private List<VNResourceEntry> entries = new();

        private Dictionary<string, VNResourceEntry> _lookup;

        public IReadOnlyList<VNResourceEntry> Entries => entries;

        public void SetEntries(List<VNResourceEntry> newEntries)
        {
            entries = newEntries ?? new List<VNResourceEntry>();
            _lookup = null;
        }

        public bool TryGetById(string id, out VNResourceEntry entry)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                entry = null;
                return false;
            }

            _lookup ??= BuildLookup();
            return _lookup.TryGetValue(id.Trim(), out entry);
        }

        private Dictionary<string, VNResourceEntry> BuildLookup()
        {
            var lookup = new Dictionary<string, VNResourceEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.id))
                {
                    continue;
                }

                lookup[entry.id.Trim()] = entry;
            }

            return lookup;
        }
    }
}
