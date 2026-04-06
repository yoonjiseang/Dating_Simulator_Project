using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VN.Data;

namespace VN.Core
{
    /// <summary>
    /// Runtime provider for table-based master data built as .bytes and distributed via Addressables.
    /// Address format: MDB/{tableName}
    /// </summary>
    public sealed class MasterDataProvider
    {
        private const string AddressPrefix = "MDB/";

        private readonly Dictionary<string, MasterDataTable> _cache = new(StringComparer.OrdinalIgnoreCase);

        public async Task<MasterDataTable> LoadTableAsync(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                Debug.LogError("[MasterDataProvider] tableName is null or empty.");
                return null;
            }

            var normalized = NormalizeTableName(tableName);
            if (_cache.TryGetValue(normalized, out var cached))
            {
                return cached;
            }

            var address = AddressPrefix + normalized;
            try
            {
                var textAsset = await Addressables.LoadAssetAsync<TextAsset>(address).Task;
                if (textAsset == null)
                {
                    Debug.LogError($"[MasterDataProvider] Missing master data at address: {address}");
                    return null;
                }

                var table = MasterDataBinarySerializer.Deserialize(textAsset.bytes);
                _cache[normalized] = table;
                return table;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MasterDataProvider] Failed to load table '{normalized}' ({address}).\n{ex}");
                return null;
            }
        }

        public MasterDataTable GetCachedTable(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return null;
            }

            _cache.TryGetValue(NormalizeTableName(tableName), out var table);
            return table;
        }

        public void ClearCache()
        {
            _cache.Clear();
        }

        private static string NormalizeTableName(string tableName)
        {
            return tableName.Trim();
        }
    }
}