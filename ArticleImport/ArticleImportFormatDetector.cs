namespace Caupo.ArticleImport
{
    public static class ArticleImportFormatDetector
    {
        private static readonly string[] CaupoArticleColumns =
             {
                
                "Sifra",
                "InternaSifra",
                "Artikl",
                "Cijena",
                "VrstaArtikla",
                "Kategorija",
                "JedinicaMjere",
                "PoreskaStopa",
                "Normativ",
                "Pozicija",
                "Aktivan",
                "PorezNaPotrosnju",
                "Slika"
            };

        public static bool IsCaupoArticleFormat(ArticleImportSourceData source)
        {
            return CaupoArticleColumns.All(requiredColumn =>
                source.Columns.Any(sourceColumn =>
                    string.Equals(sourceColumn, requiredColumn, StringComparison.OrdinalIgnoreCase)));
        }
    }
}