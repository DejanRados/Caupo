namespace Caupo.ArticleImport
{
    public sealed class ArticleImportMappedRow
    {
        public int SourceRowNumber { get; set; }
        public Dictionary<string, object?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public object? GetValue(string targetColumn)
        {
            return Values.TryGetValue(targetColumn, out object? value) ? value : null;
        }

        public string GetString(string targetColumn)
        {
            return GetValue(targetColumn)?.ToString()?.Trim() ?? string.Empty;
        }
    }
}