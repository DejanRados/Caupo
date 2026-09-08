using System.IO;
using System.Text.Json;

namespace Caupo.Services
{
    public static class TaxRatesJsonService
    {
        public sealed class TaxRatesFile
        {
            public int Version { get; set; }
            public string Updated { get; set; } = string.Empty;
            public Dictionary<string, CountryTaxRates> Countries { get; set; } = new();
        }

        public sealed class CountryTaxRates
        {
            public List<TaxRate> Rates { get; set; } = new();
        }

        public sealed class TaxRate
        {
            public string Opis { get; set; } = string.Empty;
            public decimal Postotak { get; set; }
            public string Oznaka { get; set; } = string.Empty;
        }

        public static async Task<TaxRatesFile> LoadAsync()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "poreskestope.json");

            if (!File.Exists(path))
                throw new FileNotFoundException("Datoteka poreskih stopa nije pronađena.", path);

            string json = await File.ReadAllTextAsync(path);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            TaxRatesFile? data = JsonSerializer.Deserialize<TaxRatesFile>(json, options);

            if (data == null)
                throw new InvalidOperationException("Datoteka poreskih stopa nije ispravna.");

            return data;
        }

        public static async Task<IReadOnlyList<TaxRate>> GetRatesAsync(string country)
        {
            if (string.IsNullOrWhiteSpace(country))
                throw new ArgumentException("Država nije određena.", nameof(country));

            TaxRatesFile data = await LoadAsync();

            if (!data.Countries.TryGetValue(country, out CountryTaxRates? countryRates))
                throw new InvalidOperationException($"Poreske stope za državu '{country}' nisu pronađene.");

            return countryRates.Rates;
        }
    }
}