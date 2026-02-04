using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Caupo.Helpers
{
  
  /*  public static class TextNormalizer
    {
        public static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            input = input.ToLowerInvariant();

            // ukloni dijakritiku (č, ć, š, đ…)
            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            input = sb.ToString();

            // ukloni brojeve i mjere
            input = Regex.Replace(input, @"\b\d+([.,]\d+)?\s?(l|ml|cl|kg|g)\b", "");
            input = Regex.Replace(input, @"\d+", "");

            

            // cleanup
            input = Regex.Replace(input, @"[^a-z\s]", "");
            input = Regex.Replace(input, @"\s+", " ").Trim();

            return input;
        }
    }*/

}
