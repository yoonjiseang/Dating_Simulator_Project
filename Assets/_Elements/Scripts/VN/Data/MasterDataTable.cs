using System;
using System.Collections.Generic;
using System.Linq;

namespace VN.Data
{
    [Serializable]
    public sealed class MasterDataTable
    {
        public string tableName;
        public string sourceHash;
        public string[] headers;
        public MasterDataRow[] rows;

        public int RowCount => rows?.Length ?? 0;
        public int ColumnCount => headers?.Length ?? 0;

        public int GetColumnIndex(string header)
        {
            if (headers == null || headers.Length == 0 || string.IsNullOrWhiteSpace(header))
            {
                return -1;
            }

            for (var i = 0; i < headers.Length; i++)
            {
                if (string.Equals(headers[i], header, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        public IEnumerable<MasterDataRow> FindRows(string header, string value)
        {
            var index = GetColumnIndex(header);
            if (index < 0 || rows == null)
            {
                return Enumerable.Empty<MasterDataRow>();
            }

            return rows.Where(r => string.Equals(r.GetCell(index), value, StringComparison.Ordinal));
        }
    }

    [Serializable]
    public sealed class MasterDataRow
    {
        public string[] cells;

        public string GetCell(int columnIndex)
        {
            if (cells == null || columnIndex < 0 || columnIndex >= cells.Length)
            {
                return string.Empty;
            }

            return cells[columnIndex] ?? string.Empty;
        }
    }
}