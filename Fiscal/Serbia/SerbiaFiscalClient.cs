using Caupo.Fiscal.Common;
using Caupo.Fiscal.Serbia.Models;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.IO;
using System.Text.Json;

namespace Caupo.Fiscal.Serbia
{
    public sealed class SerbiaFiscalClient
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };

        private readonly SerbiaFiscalSettings _settings;

        public SerbiaFiscalClient(
            SerbiaFiscalSettings settings)
        {
            _settings = settings;
        }

        public async Task<IReadOnlyList<SerbiaTaxRateEntry>>
            GetCurrentTaxRatesAsync(
                CancellationToken cancellationToken = default)
        {
            using HttpClient client =
                await CreateClientAsync(cancellationToken);

            string statusUrl =
                BuildStatusUrl();

            Debug.WriteLine(
                $"[SRBIJA] Status URL: {statusUrl}");

            using HttpResponseMessage response =
                await client.GetAsync(
                    statusUrl,
                    cancellationToken);

            string body =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            Debug.WriteLine(
                $"[SRBIJA] Status HTTP: " +
                $"{(int)response.StatusCode} {response.StatusCode}");

            if(!response.IsSuccessStatusCode)
            {
                throw new FiscalException(
                    $"Srbija PFR status greška HTTP " +
                    $"{(int)response.StatusCode}: {body}");
            }

            return ParseTaxRates(body);
        }

        public async Task<SerbiaInvoiceResponse>
            IssueInvoiceAsync(
                SerbiaInvoiceRequest invoice,
                CancellationToken cancellationToken = default)
        {
            using HttpClient client =
                await CreateClientAsync(cancellationToken);

            string invoiceUrl =
                BuildInvoiceUrl();

            string json =
                JsonSerializer.Serialize(
                    invoice,
                    JsonOptions);

            Debug.WriteLine(
                "[SRBIJA] Invoice request:");

            Debug.WriteLine(json);

            using var content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

            using HttpResponseMessage response =
                await client.PostAsync(
                    invoiceUrl,
                    content,
                    cancellationToken);

            string body =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            Debug.WriteLine(
                $"[SRBIJA] Invoice HTTP: " +
                $"{(int)response.StatusCode} {response.StatusCode}");

            Debug.WriteLine(
                $"[SRBIJA] Invoice response: {body}");

            if(!response.IsSuccessStatusCode)
            {
                throw new FiscalException(
                    $"Srbija fiskalizacija nije uspjela. " +
                    $"HTTP {(int)response.StatusCode}: {body}");
            }

            SerbiaInvoiceResponse? result;

            try
            {
                result =
                    JsonSerializer.Deserialize<SerbiaInvoiceResponse>(
                        body,
                        JsonOptions);
            }
            catch(JsonException ex)
            {
                throw new FiscalException(
                    "Srbija PFR je vratio neispravan JSON odgovor.",
                    ex);
            }

            if(result == null)
            {
                throw new FiscalException(
                    "Srbija PFR nije vratio podatke računa.");
            }

            result.RawJson = body;

            return result;
        }

        private async Task<HttpClient> CreateClientAsync(
            CancellationToken cancellationToken)
        {
            if(string.Equals(
                _settings.PfrType,
                "VPFR",
                StringComparison.OrdinalIgnoreCase))
            {
                return CreateVpfrClient();
            }

            if(string.Equals(
                _settings.PfrType,
                "LPFR",
                StringComparison.OrdinalIgnoreCase))
            {
                HttpClient client =
                    CreateLpfrClient();

                await ConfirmLpfrPinAsync(
                    client,
                    cancellationToken);

                return client;
            }

            throw new FiscalException(
                $"Nepoznat Srbija PFR tip: {_settings.PfrType}");
        }

        private HttpClient CreateVpfrClient()
        {
            string certificatePath =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Certificates",
                    _settings.CertificateName);

            if(!File.Exists(certificatePath))
            {
                throw new FiscalException(
                    $"Srbija V-PFR certifikat nije pronađen: " +
                    $"{certificatePath}");
            }

            var certificate =
                new X509Certificate2(
                    certificatePath,
                    _settings.CertificatePassword,
                    X509KeyStorageFlags.MachineKeySet |
                    X509KeyStorageFlags.Exportable);

            if(!certificate.HasPrivateKey)
            {
                certificate.Dispose();

                throw new FiscalException(
                    "Srbija V-PFR certifikat nema privatni ključ.");
            }

            var handler =
                new HttpClientHandler
                {
                    ClientCertificateOptions =
                        ClientCertificateOption.Manual,

                    // Ovo je već potvrđeno u postojećem
                    // Caupo V-PFR testu.
                    UseProxy = false
                };

            handler.ClientCertificates.Add(
                certificate);

            var client =
                new HttpClient(
                    handler,
                    disposeHandler: true)
                {
                    Timeout =
                        TimeSpan.FromSeconds(30)
                };

            client.DefaultRequestHeaders.Accept.Clear();

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));

            client.DefaultRequestHeaders.Add(
                "PAC",
                _settings.Pac);

            string language = string.IsNullOrWhiteSpace(_settings.AcceptLanguage) ? "sr-Cyrl-RS" : _settings.AcceptLanguage.Trim();
            client.DefaultRequestHeaders.AcceptLanguage.Clear();
            client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(language));

            return client;
        }

        private HttpClient CreateLpfrClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            string language = string.IsNullOrWhiteSpace(_settings.AcceptLanguage) ? "sr-Cyrl-RS" : _settings.AcceptLanguage.Trim();
            client.DefaultRequestHeaders.AcceptLanguage.Clear();
            client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(language));

            return client;
        }

        private async Task ConfirmLpfrPinAsync(
            HttpClient client,
            CancellationToken cancellationToken)
        {
            string baseUrl =
                BuildLpfrBaseUrl(
                    _settings.LpfrUrl,
                    _settings.LpfrToken);

            string pinUrl =
                $"{baseUrl}/v3/pin";

            using var pinContent =
                new StringContent(
                    _settings.LpfrPin,
                    Encoding.UTF8,
                    "application/json");

            using HttpResponseMessage response =
                await client.PostAsync(
                    pinUrl,
                    pinContent,
                    cancellationToken);

            string result =
                (await response.Content
                    .ReadAsStringAsync(cancellationToken))
                .Trim()
                .Trim('"');

            if(!response.IsSuccessStatusCode)
            {
                throw new FiscalException(
                    $"Srbija L-PFR PIN greška HTTP " +
                    $"{(int)response.StatusCode}: {result}");
            }

            if(!string.Equals(
                result,
                "0100",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new FiscalException(
                    $"Srbija L-PFR PIN nije prihvaćen: {result}");
            }
        }

        private string BuildStatusUrl()
        {
            if(string.Equals(
                _settings.PfrType,
                "VPFR",
                StringComparison.OrdinalIgnoreCase))
            {
                return NormalizeVpfrBaseUrl(
                           _settings.VpfrUrl) +
                       "/api/v3/status";
            }

            return BuildLpfrBaseUrl(
                       _settings.LpfrUrl,
                       _settings.LpfrToken) +
                   "/v3/status";
        }

        private string BuildInvoiceUrl()
        {
            if(string.Equals(
                _settings.PfrType,
                "VPFR",
                StringComparison.OrdinalIgnoreCase))
            {
                return NormalizeVpfrBaseUrl(
                           _settings.VpfrUrl) +
                       "/api/v3/invoices";
            }

            return BuildLpfrBaseUrl(
                       _settings.LpfrUrl,
                       _settings.LpfrToken) +
                   "/v3/invoices";
        }

        private static string NormalizeVpfrBaseUrl(
            string url)
        {
            string result =
                url.Trim()
                    .TrimEnd('/');

            string[] suffixes =
            {
                "/api/v3/status",
                "/api/v3/invoices"
            };

            foreach(string suffix in suffixes)
            {
                if(result.EndsWith(
                    suffix,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return result[
                        ..^suffix.Length];
                }
            }

            return result;
        }

        private static string BuildLpfrBaseUrl(
            string url,
            string token)
        {
            string cleanUrl =
                url.Trim()
                    .TrimEnd('/');

            if(cleanUrl.EndsWith(
                "/api",
                StringComparison.OrdinalIgnoreCase))
            {
                return cleanUrl;
            }

            string cleanToken =
                token.Trim()
                    .Trim('/');

            if(!string.IsNullOrWhiteSpace(cleanToken) &&
               cleanUrl.Contains(
                   "/" + cleanToken,
                   StringComparison.OrdinalIgnoreCase))
            {
                return $"{cleanUrl}/api";
            }

            if(!string.IsNullOrWhiteSpace(cleanToken))
            {
                return $"{cleanUrl}/{cleanToken}/api";
            }

            return cleanUrl;
        }

        private static IReadOnlyList<SerbiaTaxRateEntry>
            ParseTaxRates(
                string json)
        {
            using JsonDocument document =
                JsonDocument.Parse(json);

            JsonElement root =
                document.RootElement;

            if(!root.TryGetProperty(
                "currentTaxRates",
                out JsonElement current))
            {
                throw new FiscalException(
                    "Srbija PFR status ne sadrži currentTaxRates.");
            }

            JsonElement group;

            if(current.ValueKind ==
               JsonValueKind.Array)
            {
                if(current.GetArrayLength() == 0)
                {
                    throw new FiscalException(
                        "Srbija PFR nema aktivnu poresku grupu.");
                }

                group =
                    current[0];
            }
            else
            {
                group =
                    current;
            }

            if(!group.TryGetProperty(
                "taxCategories",
                out JsonElement categories) ||
               categories.ValueKind !=
               JsonValueKind.Array)
            {
                throw new FiscalException(
                    "Srbija PFR status nema taxCategories.");
            }

            var result =
                new List<SerbiaTaxRateEntry>();

            foreach(JsonElement category in
                    categories.EnumerateArray())
            {
                string categoryName =
                    category.TryGetProperty(
                        "name",
                        out JsonElement nameElement)
                        ? nameElement.GetString()
                          ?? string.Empty
                        : string.Empty;

                if(!category.TryGetProperty(
                    "taxRates",
                    out JsonElement rates) ||
                   rates.ValueKind !=
                   JsonValueKind.Array)
                {
                    continue;
                }

                foreach(JsonElement rate in
                        rates.EnumerateArray())
                {
                    string label =
                        rate.TryGetProperty(
                            "label",
                            out JsonElement labelElement)
                            ? labelElement.GetString()
                              ?? string.Empty
                            : string.Empty;

                    decimal percentage =
                        rate.TryGetProperty(
                            "rate",
                            out JsonElement rateElement) &&
                        rateElement.TryGetDecimal(
                            out decimal parsed)
                            ? parsed
                            : 0m;

                    if(!string.IsNullOrWhiteSpace(label))
                    {
                        result.Add(
                            new SerbiaTaxRateEntry
                            {
                                Category =
                                    categoryName,

                                Label =
                                    label,

                                Rate =
                                    percentage
                            });
                    }
                }
            }

            if(result.Count == 0)
            {
                throw new FiscalException(
                    "Srbija PFR nije vratio aktivne poreske oznake.");
            }

            return result;
        }
    }
}
