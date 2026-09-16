using System.IO;
using System.Text;

namespace Caupo.ArticleImport
{
    public static class CsvArticleImportReader
    {
        public static ArticleImportSourceData Read(string filePath)
        {
            
            string[] lines = File.ReadAllLines(filePath, DetectEncoding(filePath));

            ArticleImportSourceData result = new()
            {
                SourceName = Path.GetFileName(filePath),
                TableName = Path.GetFileNameWithoutExtension(filePath)
            };

            if (lines.Length == 0)
                return result;

            char separator = DetectSeparator(lines[0]);
            List<string> headers = ParseLine(lines[0], separator);

            for (int i = 0; i < headers.Count; i++)
            {
                string columnName = headers[i].Trim();

                if (string.IsNullOrWhiteSpace(columnName))
                    columnName = $"Kolona {i + 1}";

                if (result.Columns.Contains(columnName, StringComparer.OrdinalIgnoreCase))
                    columnName = $"{columnName} {i + 1}";

                result.Columns.Add(columnName);
            }

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                List<string> values = ParseLine(lines[i], separator);

                ArticleImportSourceRow row = new()
                {
                    RowNumber = i + 1
                };

                bool hasValue = false;

                for (int column = 0; column < result.Columns.Count; column++)
                {
                    string value = column < values.Count ? values[column].Trim() : string.Empty;

                    if (!string.IsNullOrWhiteSpace(value))
                        hasValue = true;

                    row.Values[result.Columns[column]] = value;
                }

                if (hasValue)
                    result.Rows.Add(row);
            }

            return result;
        }

        private static char DetectSeparator(string header)
        {
            char[] separators = { ';', ',', '\t' };

            return separators
                .OrderByDescending(separator => ParseLine(header, separator).Count)
                .First();
        }

        private static List<string> ParseLine(string line, char separator)
        {
            List<string> values = new();
            StringBuilder value = new();
            bool insideQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        value.Append('"');
                        i++;
                    }
                    else
                    {
                        insideQuotes = !insideQuotes;
                    }

                    continue;
                }

                if (c == separator && !insideQuotes)
                {
                    values.Add(value.ToString());
                    value.Clear();
                    continue;
                }

                value.Append(c);
            }

            values.Add(value.ToString());
            return values;
        }

        private static Encoding DetectEncoding(string filePath)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            byte[] bytes = File.ReadAllBytes(filePath);

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8;

            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode;

            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode;

            try
            {
                Encoding utf8Strict = new UTF8Encoding(false, true);
                utf8Strict.GetString(bytes);
                return Encoding.UTF8;
            }
            catch (DecoderFallbackException)
            {
                return Encoding.GetEncoding(1250);
            }
        }
    }
}