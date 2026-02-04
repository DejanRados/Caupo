using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caupo.Helpers
{
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;
    using static Caupo.Views.ArticlesPage;

    public class ProductImageMapper
    {
        private readonly List<string> _keys;

        public ProductImageMapper(IEnumerable<ProductDefinition> products)
        {
            _keys = products
                .Select(p => Normalize(p.Key))
                .Distinct()
                .ToList();
        }

        public string? ResolveImage(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            string normalizedInput = Normalize(input);

            string? bestMatch = null;
            int bestScore = int.MaxValue;

            foreach (var key in _keys)
            {
                int distance = Levenshtein(normalizedInput, key);
                if (distance < bestScore)
                {
                    bestScore = distance;
                    bestMatch = key;
                }
            }

            // prag – fino se podešava
            return bestScore <= 4
                ? $"images/{bestMatch}.png"
                : null;
        }

        // ------------------ helpers ------------------

        private static string Normalize(string text)
        {
            text = text.ToLowerInvariant();

            text = RemoveDiacritics(text);

            text = Regex.Replace(text, @"[^a-z0-9]", "");

            // uklanja količine: 0.5, 0,33, 500ml
            text = Regex.Replace(text, @"\d+(ml|l)?", "");

            return text;
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static int Levenshtein(string a, string b)
        {
            int[,] d = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }

            return d[a.Length, b.Length];
        }
    }



}
