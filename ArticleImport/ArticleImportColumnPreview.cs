namespace Caupo.ArticleImport
{
    public sealed class ArticleImportColumnPreview
    {
        public string ColumnName { get; set; } = string.Empty;
        public string Example1 { get; set; } = string.Empty;
        public string Example2 { get; set; } = string.Empty;
        public string Example3 { get; set; } = string.Empty;

        public override string ToString()
        {
            return ColumnName;
        }
    }
}