using ClosedXML.Excel;
using System.IO;

namespace Caupo.ArticleImport
{
    public static class ExcelArticleImportReader
    {
        public static ArticleImportSourceData Read(string filePath)
        {
            using var workbook = new XLWorkbook(filePath);

            IXLWorksheet worksheet = workbook.Worksheets.First();

            ArticleImportSourceData result = new()
            {
                SourceName = Path.GetFileName(filePath),
                TableName = worksheet.Name
            };

            IXLRange? usedRange = worksheet.RangeUsed();

            if (usedRange == null)
                return result;

            int firstRow = usedRange.RangeAddress.FirstAddress.RowNumber;
            int lastRow = usedRange.RangeAddress.LastAddress.RowNumber;
            int firstColumn = usedRange.RangeAddress.FirstAddress.ColumnNumber;
            int lastColumn = usedRange.RangeAddress.LastAddress.ColumnNumber;

            // Prvi red smatramo zaglavljem.
            for (int column = firstColumn; column <= lastColumn; column++)
            {
                string columnName = worksheet.Cell(firstRow, column).GetString().Trim();

                if (string.IsNullOrWhiteSpace(columnName))
                    columnName = $"Kolona {column}";

                result.Columns.Add(columnName);
            }

            // Ostali redovi su podaci.
            for (int row = firstRow + 1; row <= lastRow; row++)
            {
                ArticleImportSourceRow sourceRow = new()
                {
                    RowNumber = row
                };

                bool hasValue = false;

                for (int column = firstColumn; column <= lastColumn; column++)
                {
                    string columnName = result.Columns[column - firstColumn];
                    IXLCell cell = worksheet.Cell(row, column);

                    object? value = GetCellValue(cell);

                    if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        hasValue = true;

                    sourceRow.Values[columnName] = value;
                }

                // Potpuno prazne redove ne uvozimo.
                if (hasValue)
                    result.Rows.Add(sourceRow);
            }

            return result;
        }

        private static object? GetCellValue(IXLCell cell)
        {
            if (cell.IsEmpty())
                return null;

            return cell.DataType switch
            {
                XLDataType.Number => cell.GetDouble(),
                XLDataType.Boolean => cell.GetBoolean(),
                XLDataType.DateTime => cell.GetDateTime(),
                XLDataType.TimeSpan => cell.GetTimeSpan(),
                _ => cell.GetString()
            };
        }
    }
}