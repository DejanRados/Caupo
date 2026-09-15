namespace Caupo.ArticleImport
{
    public enum ArticleImportMappingType
    {
        None,
        SourceColumn,
        FixedValue,
        Automatic
    }

    public sealed class ArticleImportMapping
    {
        public string TargetColumn { get; set; } = string.Empty;
        public ArticleImportMappingType MappingType { get; set; } = ArticleImportMappingType.None;
        public string? SourceColumn { get; set; }
        public string? FixedValue { get; set; }
    }
}