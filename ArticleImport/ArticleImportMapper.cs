namespace Caupo.ArticleImport
{
    public static class ArticleImportMapper
    {

        public static void MapSourceColumn(ArticleImportMapping mapping, string sourceColumn)
        {
            mapping.MappingType = ArticleImportMappingType.SourceColumn;
            mapping.SourceColumn = sourceColumn;
            mapping.FixedValue = null;
        }

        public static void ClearMapping(ArticleImportMapping mapping)
        {
            mapping.MappingType = ArticleImportMappingType.None;
            mapping.SourceColumn = null;
            mapping.FixedValue = null;
        }

        public static void MapFixedValue(ArticleImportMapping mapping, string? fixedValue)
        {
            mapping.MappingType = ArticleImportMappingType.FixedValue;
            mapping.SourceColumn = null;
            mapping.FixedValue = fixedValue;
        }

        public static void MapAutomatic(ArticleImportMapping mapping)
        {
            mapping.MappingType = ArticleImportMappingType.Automatic;
            mapping.SourceColumn = null;
            mapping.FixedValue = null;
        }

        public static ArticleImportMappedRow MapRow(ArticleImportSourceRow sourceRow, IEnumerable<ArticleImportMapping> mappings)
        {
            ArticleImportMappedRow result = new()
            {
                SourceRowNumber = sourceRow.RowNumber
            };

            foreach (ArticleImportMapping mapping in mappings)
            {
                object? value = mapping.MappingType switch
                {
                    ArticleImportMappingType.SourceColumn => GetSourceValue(sourceRow, mapping),
                    ArticleImportMappingType.FixedValue => mapping.FixedValue,
                    ArticleImportMappingType.None => null,
                    ArticleImportMappingType.Automatic => null,
                    _ => null
                };

                result.Values[mapping.TargetColumn] = value;
            }

            return result;
        }

        private static object? GetSourceValue(ArticleImportSourceRow sourceRow, ArticleImportMapping mapping)
        {
            if (string.IsNullOrWhiteSpace(mapping.SourceColumn))
                return null;

            return sourceRow.GetValue(mapping.SourceColumn);
        }
        public static List<ArticleImportMapping> CreateMappings()
        {
            List<ArticleImportMapping> mappings = new();

            foreach (ArticleImportTargetField targetField in ArticleImportTargetFields.All)
            {
                mappings.Add(new ArticleImportMapping
                {
                    TargetColumn = targetField.Name,
                    MappingType = ArticleImportMappingType.None
                });
            }

            return mappings;
        }

        public static List<ArticleImportMapping> CreateMappings(ArticleImportSourceData source)
        {
            List<ArticleImportMapping> mappings = CreateMappings();

            foreach (ArticleImportMapping mapping in mappings)
            {
                string? matchingSourceColumn = source.Columns.FirstOrDefault(x => string.Equals(x, mapping.TargetColumn, StringComparison.OrdinalIgnoreCase));

                if (matchingSourceColumn == null)
                    continue;

                mapping.MappingType = ArticleImportMappingType.SourceColumn;
                mapping.SourceColumn = matchingSourceColumn;
            }

            return mappings;
        }
    }
}