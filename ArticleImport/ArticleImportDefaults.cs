namespace Caupo.ArticleImport
{
    public static class ArticleImportDefaults
    {
        public static void Apply(ArticleImportMappedRow row, IEnumerable<ArticleImportMapping> mappings)
        {
            ApplyIfNotMapped(row, mappings, "Cijena", "1.00");
            ApplyIfNotMapped(row, mappings, "Vrsta", "Piće");
            ApplyIfNotMapped(row, mappings, "Kategorija", "Ostala pića");
            ApplyIfNotMapped(row, mappings, "Jedinica", "kom");
            ApplyIfNotMapped(row, mappings, "Normativ", "1");
            ApplyIfNotMapped(row, mappings, "Aktivan", true);
            ApplyIfNotMapped(row, mappings, "Porez na potrošnju", false);
            ApplyIfNotMapped(row, mappings, "Slika", string.Empty);
        }

        private static void ApplyIfNotMapped(ArticleImportMappedRow row, IEnumerable<ArticleImportMapping> mappings, string targetColumn, object? defaultValue)
        {
            ArticleImportMapping? mapping = mappings.FirstOrDefault(x => x.TargetColumn == targetColumn);

            if (mapping == null || mapping.MappingType == ArticleImportMappingType.None)
                row.Values[targetColumn] = defaultValue;
        }
    }
}