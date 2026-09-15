namespace Caupo.ArticleImport
{
    public sealed class ArticleImportMappingOption
    {
        public ArticleImportMappingType MappingType { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? SourceColumn { get; set; }
        public string Example1 { get; set; } = string.Empty;
        public string Example2 { get; set; } = string.Empty;
        public string Example3 { get; set; } = string.Empty;

        public bool IsSourceColumn => MappingType == ArticleImportMappingType.SourceColumn;

        public override string ToString()
        {
            return DisplayName;
        }
    }
}