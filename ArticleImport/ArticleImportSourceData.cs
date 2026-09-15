using System;
using System.Collections.Generic;

namespace Caupo.ArticleImport
{
    public sealed class ArticleImportSourceData
    {
        public string SourceName { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public List<string> Columns { get; set; } = new();
        public List<ArticleImportSourceRow> Rows { get; set; } = new();
    }

    public sealed class ArticleImportSourceRow
    {
        public int RowNumber { get; set; }
        public Dictionary<string, object?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public object? GetValue(string columnName)
        {
            return Values.TryGetValue(columnName, out object? value) ? value : null;
        }

        public string GetString(string columnName)
        {
            return GetValue(columnName)?.ToString()?.Trim() ?? string.Empty;
        }
    }
}