using Caupo.Licensing.Models;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Caupo.Licensing;

public sealed class LicenseApiClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;
    private readonly bool _ownsClient;

    public LicenseApiClient(HttpClient? client = null)
    {
        _ownsClient = client is null;
        _http = client ?? new HttpClient();
        _http.BaseAddress = new Uri("https://caupo.app/api/v2/");
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<PaddleCatalogResponse> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _http.GetAsync("paddle-plans.php", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        PaddleCatalogResponse? catalog = await response.Content.ReadFromJsonAsync<PaddleCatalogResponse>(JsonOptions, cancellationToken);

        if (catalog is null || catalog.Currency != "EUR" || catalog.Plans is null)
            throw new InvalidOperationException("Server je vratio neispravan cjenovnik.");

        return catalog;
    }

    public async Task<CheckoutResponse> StartCheckoutAsync(
        PaddleCustomer customer, int planId, int quantity, string renewal, string requestToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customer);

        if (planId < 1 || quantity is < 1 or > 10)
            throw new ArgumentException("Plan ili broj licenci nije ispravan.");

        if (renewal is not ("subscription" or "one_time"))
            throw new ArgumentException("Način plaćanja nije ispravan.", nameof(renewal));

        if (!Regex.IsMatch(requestToken, @"\A[a-f0-9]{32}\z"))
            throw new ArgumentException("Identifikator zahtjeva nije ispravan.", nameof(requestToken));

        if (string.IsNullOrWhiteSpace(customer.CompanyName)
            || string.IsNullOrWhiteSpace(customer.ContactName)
            || string.IsNullOrWhiteSpace(customer.Email)
            || string.IsNullOrWhiteSpace(customer.Phone)
            || string.IsNullOrWhiteSpace(customer.Address)
            || string.IsNullOrWhiteSpace(customer.City)
            || string.IsNullOrWhiteSpace(customer.PostalCode)
            || string.IsNullOrWhiteSpace(customer.CountryCode)
            || string.IsNullOrWhiteSpace(customer.TaxNumber))
            throw new ArgumentException("Svi podaci kupca su obavezni.", nameof(customer));

        var request = new
        {
            customer,
            plan_id = planId,
            quantity,
            renewal,
            request_token = requestToken
        };

        CheckoutResponse result = await PostAsync<CheckoutResponse>("paddle-start-checkout.php", request, cancellationToken);

        if (!Regex.IsMatch(result.TransactionId, @"\Atxn_[a-z0-9]{26}\z")
            || !Uri.TryCreate(result.CheckoutUrl, UriKind.Absolute, out Uri? url)
            || url.Scheme != Uri.UriSchemeHttps
            || !string.Equals(url.Host, "caupo.app", StringComparison.OrdinalIgnoreCase)
            || url.AbsolutePath.TrimEnd('/') != "/checkout"
            || !url.Query.Contains("_ptxn=" + result.TransactionId, StringComparison.Ordinal))
            throw new InvalidOperationException("Server nije vratio ispravnu Paddle transakciju.");

        return result;
    }

    public Task<WpfLicenseResult> GetWpfLicenseAsync(
        string transactionId, string requestToken, CancellationToken cancellationToken = default)
    {
        if (!Regex.IsMatch(transactionId, @"\Atxn_[a-z0-9]{26}\z")
            || !Regex.IsMatch(requestToken, @"\A[a-f0-9]{32}\z"))
            throw new ArgumentException("Podaci kupovine nisu ispravni.");

        return PostAsync<WpfLicenseResult>("paddle-wpf-license.php",
            new { transaction_id = transactionId, request_token = requestToken }, cancellationToken);
    }

    public async Task<LicenseClaimResponse> ActivateAsync(
        LicenseDeviceIdentity device, string licenseKey, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (string.IsNullOrWhiteSpace(licenseKey))
            throw new ArgumentException("Unesite licencni ključ.", nameof(licenseKey));

        licenseKey = licenseKey.Trim().ToUpperInvariant();

        if (!Regex.IsMatch(licenseKey, @"\A[A-F0-9]{64}\z"))
            throw new ArgumentException("Format licencnog ključa nije ispravan.", nameof(licenseKey));

        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nonce = NewRequestToken();

        var request = new
        {
            license_key = licenseKey,
            fingerprint_sha256 = device.FingerprintSha256,
            device_public_key = device.PublicKey,
            device_name = Environment.MachineName,
            timestamp,
            nonce,
            device_signature = device.SignActivation(licenseKey, timestamp, nonce)
        };

        return await PostAsync<LicenseClaimResponse>("activate", request, cancellationToken);
    }

    public Task<LicenseCheckResponse> CheckLicenseAsync(
        LicenseDeviceIdentity device, string licenseId, string deviceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (!Guid.TryParseExact(licenseId, "D", out _)
            || !Guid.TryParseExact(deviceId, "D", out _))
            throw new ArgumentException("Identifikatori licence ili uređaja nisu ispravni.");

        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nonce = NewRequestToken();

        return PostAsync<LicenseCheckResponse>("license-check.php", new
        {
            license_id = licenseId,
            device_id = deviceId,
            fingerprint_sha256 = device.FingerprintSha256,
            timestamp,
            nonce,
            device_signature = device.SignLicenseCheck(licenseId, deviceId, timestamp, nonce)
        }, cancellationToken);
    }

    public Task<CustomerLicensesResponse> GetCustomerLicensesAsync(
        LicenseDeviceIdentity device, string deviceId, string? transactionId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (!Regex.IsMatch(deviceId ?? string.Empty, @"\A[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\z"))
            throw new ArgumentException("Neispravan uređaj.", nameof(deviceId));

        if (transactionId != null && !Regex.IsMatch(transactionId, @"\Atxn_[a-z0-9]{26}\z"))
            throw new ArgumentException("Neispravna transakcija.", nameof(transactionId));

        const string operation = "customer_licenses";
        string hash = Sha256Hex("{\"scope\":\"customer_licenses\"}");
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nonce = NewRequestToken();
        string signature = device.SignRegisteredRequest(operation, deviceId, timestamp, nonce, hash);

        var request = new
        {
            operation,
            device_id = deviceId,
            fingerprint_sha256 = device.FingerprintSha256,
            timestamp,
            nonce,
            device_signature = signature,
            transaction_id = transactionId
        };

        return PostAsync<CustomerLicensesResponse>("paddle-customer-overview.php", request, cancellationToken);
    }

    public async Task<string> GetBillingPortalUrlAsync(
        LicenseDeviceIdentity device, string deviceId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (!Regex.IsMatch(deviceId ?? string.Empty, @"\A[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\z"))
            throw new ArgumentException("Neispravan uređaj.", nameof(deviceId));

        const string operation = "customer_licenses";
        string hash = Sha256Hex("{\"scope\":\"customer_licenses\"}");
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nonce = NewRequestToken();

        var request = new
        {
            operation,
            device_id = deviceId,
            fingerprint_sha256 = device.FingerprintSha256,
            timestamp,
            nonce,
            device_signature = device.SignRegisteredRequest(operation, deviceId, timestamp, nonce, hash)
        };

        BillingPortalResponse response = await PostAsync<BillingPortalResponse>("paddle-customer-portal.php", request, cancellationToken);

        if (!Uri.TryCreate(response.Url, UriKind.Absolute, out Uri? url)
            || url.Scheme != Uri.UriSchemeHttps
            || url.Host is not ("customer-portal.paddle.com" or "sandbox-customer-portal.paddle.com")
            || !url.IsDefaultPort
            || !Regex.IsMatch(url.AbsolutePath, @"\A/cpl_[a-z0-9]{26}\z")
            || !url.Query.Contains("token=pga_", StringComparison.Ordinal))
            throw new InvalidOperationException("Server nije vratio ispravan link za upravljanje plaćanjima.");

        return response.Url;
    }

    public sealed class BillingPortalResponse
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public Task<SubscriptionCancellationResponse> CancelSubscriptionAsync(
        LicenseDeviceIdentity device, string deviceId, int subscriptionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (!Regex.IsMatch(deviceId ?? string.Empty, @"\A[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\z")
            || subscriptionId < 1)
            throw new ArgumentException("Neispravni podaci pretplate.");

        const string operation = "subscription_cancel";
        var selection = new { subscription_id = subscriptionId };
        string hash = Sha256Hex(JsonSerializer.Serialize(selection, JsonOptions));
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nonce = NewRequestToken();

        var request = new
        {
            operation,
            subscription = selection,
            device_id = deviceId,
            fingerprint_sha256 = device.FingerprintSha256,
            timestamp,
            nonce,
            device_signature = device.SignRegisteredRequest(operation, deviceId, timestamp, nonce, hash)
        };

        return PostAsync<SubscriptionCancellationResponse>("paddle-cancel-subscription.php", request, cancellationToken);
    }

    public sealed class SubscriptionCancellationResponse
    {
        [JsonPropertyName("subscription_id")]
        public int SubscriptionId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("effective_at")]
        public string? EffectiveAt { get; set; }
    }

    public async Task<CheckoutResponse> StartLicenseRenewalAsync(
        LicenseDeviceIdentity device, string deviceId, int licenseId, int planId,
        string requestToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);
        ValidateRegisteredRequest(deviceId, requestToken, planId);

        if (licenseId < 1)
            throw new ArgumentException("Neispravna licenca.", nameof(licenseId));

        const string operation = "license_renewal";
        var purchase = new { request_token = requestToken, plan_id = planId, license_ids = new[] { licenseId } };

        return await StartRegisteredCheckoutAsync(device, deviceId, operation, purchase, cancellationToken);
    }

    public Task<CheckoutResponse> StartSubscriptionRenewalAsync(
        LicenseDeviceIdentity device, string deviceId, int licenseId, int planId,
        string requestToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);
        ValidateRegisteredRequest(deviceId, requestToken, planId);

        if (licenseId < 1)
            throw new ArgumentException("Neispravna licenca.", nameof(licenseId));

        const string operation = "subscription_renewal";
        var purchase = new { request_token = requestToken, plan_id = planId, license_ids = new[] { licenseId } };

        return StartRegisteredCheckoutAsync(device, deviceId, operation, purchase, cancellationToken);
    }

    public Task<CheckoutResponse> StartAdditionalLicensesAsync(
        LicenseDeviceIdentity device, string deviceId, int planId, int quantity,
        string renewal, string requestToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);
        ValidateRegisteredRequest(deviceId, requestToken, planId);

        if (quantity is < 1 or > 10 || renewal is not ("subscription" or "one_time"))
            throw new ArgumentException("Neispravna kupovina.");

        const string operation = "additional_licenses";
        var purchase = new { request_token = requestToken, plan_id = planId, quantity, renewal };

        return StartRegisteredCheckoutAsync(device, deviceId, operation, purchase, cancellationToken);
    }

    private async Task<CheckoutResponse> StartRegisteredCheckoutAsync(
        LicenseDeviceIdentity device, string deviceId, string operation, object purchase,
        CancellationToken cancellationToken)
    {
        string hash = Sha256Hex(JsonSerializer.Serialize(purchase, JsonOptions));
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nonce = NewRequestToken();

        var request = new
        {
            operation,
            purchase,
            device_id = deviceId,
            fingerprint_sha256 = device.FingerprintSha256,
            timestamp,
            nonce,
            device_signature = device.SignRegisteredRequest(operation, deviceId, timestamp, nonce, hash)
        };

        CheckoutResponse response = await PostAsync<CheckoutResponse>("paddle-registered-checkout.php", request, cancellationToken);

        if (!Regex.IsMatch(response.TransactionId ?? "", @"\Atxn_[a-z0-9]{26}\z"))
            throw new InvalidOperationException("Server nije vratio ispravnu Paddle transakciju.");

        return response;
    }

    private static void ValidateRegisteredRequest(string deviceId, string token, int planId)
    {
        if (!Regex.IsMatch(deviceId ?? "", @"\A[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\z")
            || !Regex.IsMatch(token ?? "", @"\A[a-f0-9]{32}\z") || planId < 1)
            throw new ArgumentException("Neispravni podaci registrovane kupovine.");
    }

    private static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    public sealed class LicenseCheckResponse
    {
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("license_id")]
        public string LicenseId { get; set; } = string.Empty;

        [JsonPropertyName("device_id")]
        public string DeviceId { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;
    }

    public sealed class WpfLicenseResult
    {
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("license_key")]
        public string LicenseKey { get; set; } = string.Empty;
    }

    public static string NewRequestToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

    private async Task<T> PostAsync<T>(
        string endpoint, object request, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _http.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        T? result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        return result ?? throw new InvalidOperationException("Licencni server je vratio prazan odgovor.");
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        string message = $"Licencni server je vratio HTTP {(int)response.StatusCode}.";

        try
        {
            LicenseApiError? error = await response.Content.ReadFromJsonAsync<LicenseApiError>(JsonOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(error?.Error))
                message = error.Error;
        }
        catch (JsonException)
        {
            // Ako odgovor nije JSON, zadržavamo HTTP grešku.
        }

        throw new HttpRequestException(message, null, response.StatusCode);
    }

    public void Dispose()
    {
        if (_ownsClient)
            _http.Dispose();
    }
}