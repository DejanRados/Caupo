namespace Caupo.ArticleImport
{
    public static class ArticleImportFormatDetector
    {
        private static readonly string[] CaupoArticleColumns =
        {
            "ID",
            "Šifra",
            "Interna šifra",
            "Naziv",
            "Cijena",
            "Vrsta",
            "Kategorija",
            "Jedinica",
            "Porez",
            "Normativ",
            "Pozicija",
            "Aktivan",
            "Porez na potrošnju"
        };

        public static bool IsCaupoArticleFormat(ArticleImportSourceData source)
        {
            if (source.Columns.Count != CaupoArticleColumns.Length)
                return false;

            return CaupoArticleColumns.All(requiredColumn =>
                source.Columns.Any(sourceColumn =>
                    string.Equals(sourceColumn, requiredColumn, StringComparison.OrdinalIgnoreCase)));
        }
    }
}