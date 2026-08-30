using Caupo.Fiscal.RS.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;
using System.Net.Http;
using Caupo.Fiscal.Common;

namespace Caupo.Fiscal.RS
{
    public sealed class RsFiscalClient
    {
        private readonly RsFiscalSettings _settings;
        private readonly HttpClient _client;

        public RsFiscalClient(
            RsFiscalSettings settings)
        {
            _settings = settings;

            _client =
                new HttpClient
                {
                    Timeout =
                        TimeSpan.FromSeconds(30)
                };
        }

        public async Task<RsFiscalResponse> IssueAsync(
            RsBuiltInvoice builtInvoice,
            CancellationToken cancellationToken = default)
        {
            _settings.Validate();

            try
            {
                await CheckAttentionAsync(
                    cancellationToken);

                await CheckStatusAsync(
                    cancellationToken);

                await SendPinAsync(
                    cancellationToken);
            }
            catch(Exception ex)
            {
                return new RsFiscalResponse
                {
                    Fiscalized = false,
                    ErrorMessage = ex.Message
                };
            }

            try
            {
                string json =
                    JsonConvert.SerializeObject(
                        builtInvoice.Envelope);

                using var request =
                    CreateRequest(
                        HttpMethod.Post,
                        $"{_settings.BaseUrl}/invoices");

                request.Headers.Add(
                    "RequestId",
                    builtInvoice
                        .LocalReceiptNumber
                        .ToString());

                request.Content =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json");

                Debug.WriteLine(
                    "[RS/LPFR] Invoice request:");
                Debug.WriteLine(json);

                using HttpResponseMessage response =
                    await _client.SendAsync(
                        request,
                        cancellationToken);

                string raw =
                    await response.Content
                        .ReadAsStringAsync(
                            cancellationToken);

                Debug.WriteLine(
                    $"[RS/LPFR] Invoice HTTP: " +
                    $"{(int)response.StatusCode} " +
                    $"{response.StatusCode}");

                Debug.WriteLine(
                    "[RS/LPFR] Invoice response: " +
                    raw);

                if(!response.IsSuccessStatusCode)
                {
                    return new RsFiscalResponse
                    {
                        Fiscalized = false,
                        RawResponse = raw,
                        ErrorMessage =
                            $"LPFR je vratio HTTP " +
                            $"{(int)response.StatusCode}: {raw}"
                    };
                }

                JObject obj =
                    JObject.Parse(raw);

                string? invoiceNumber =
                    obj["invoiceNumber"]?
                        .ToString();

                if(string.IsNullOrWhiteSpace(
                    invoiceNumber))
                {
                    return new RsFiscalResponse
                    {
                        Fiscalized = false,
                        RawResponse = raw,
                        ErrorMessage =
                            "LPFR je vratio uspješan HTTP odgovor, " +
                            "ali broj fiskalnog računa nije pronađen."
                    };
                }

                DateTime? sdcDateTime = null;

                if(DateTime.TryParse(
                    obj["sdcDateTime"]?.ToString(),
                    out DateTime parsedDate))
                {
                    sdcDateTime = parsedDate;
                }

                return new RsFiscalResponse
                {
                    Fiscalized = true,

                    FiscalReceiptNumber =
                        invoiceNumber,

                    TotalCounter =
                        obj["totalCounter"]?
                            .ToString(),

                    SdcDateTime =
                        sdcDateTime,

                    Journal =
                        obj["journal"]?
                            .ToString(),

                    VerificationUrl =
                        obj["verificationUrl"]?
                            .ToString(),

                    RawResponse =
                        raw
                };
            }
            catch(Exception ex)
            {
                return new RsFiscalResponse
                {
                    Fiscalized = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task CheckAttentionAsync(
            CancellationToken cancellationToken)
        {
            using var request =
                CreateRequest(
                    HttpMethod.Get,
                    $"{_settings.BaseUrl}/attention");

            using HttpResponseMessage response =
                await _client.SendAsync(
                    request,
                    cancellationToken);

            string content =
                await response.Content
                    .ReadAsStringAsync(
                        cancellationToken);

            if(!response.IsSuccessStatusCode)
            {
                throw new FiscalException(
                    $"Greška provjere dostupnosti LPFR-a: " +
                    $"{response.StatusCode}. {content}");
            }
        }

        private async Task CheckStatusAsync(
            CancellationToken cancellationToken)
        {
            using var request =
                CreateRequest(
                    HttpMethod.Get,
                    $"{_settings.BaseUrl}/status");

            using HttpResponseMessage response =
                await _client.SendAsync(
                    request,
                    cancellationToken);

            string content =
                await response.Content
                    .ReadAsStringAsync(
                        cancellationToken);

            if(!response.IsSuccessStatusCode)
            {
                throw new FiscalException(
                    $"Greška LPFR statusa: " +
                    $"{response.StatusCode}. {content}");
            }

            JObject obj =
                JObject.Parse(content);

            JToken? gscToken =
                obj.GetValue(
                    "gsc",
                    StringComparison.OrdinalIgnoreCase);

            if(gscToken != null &&
               gscToken.ToString()
                   .Contains(
                       "1300",
                       StringComparison.OrdinalIgnoreCase))
            {
                throw new FiscalException(
                    "Bezbednosni element nije prisutan. " +
                    "Provjerite fiskalnu karticu.");
            }
        }

        private async Task SendPinAsync(
            CancellationToken cancellationToken)
        {
            using var request =
                CreateRequest(
                    HttpMethod.Post,
                    $"{_settings.BaseUrl}/pin");

            request.Content =
                new StringContent(
                    _settings.Pin,
                    Encoding.UTF8,
                    "text/plain");

            using HttpResponseMessage response =
                await _client.SendAsync(
                    request,
                    cancellationToken);

            string content =
                await response.Content
                    .ReadAsStringAsync(
                        cancellationToken);

            if(!response.IsSuccessStatusCode)
            {
                throw new FiscalException(
                    $"LPFR PIN nije prihvaćen: " +
                    $"{response.StatusCode}. {content}");
            }
        }

        private HttpRequestMessage CreateRequest(
            HttpMethod method,
            string url)
        {
            var request =
                new HttpRequestMessage(
                    method,
                    url);

            request.Headers.Add(
                "Authorization",
                "Bearer " +
                _settings.ApiKey);

            return request;
        }
    }
}
