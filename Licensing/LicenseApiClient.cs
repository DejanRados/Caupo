
using Caupo.Licensing.Models;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Caupo.Licensing;

public sealed class LicenseApiClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public LicenseApiClient(HttpClient? client = null)
    {
        _ownsClient = client is null;
        _http = client ?? new HttpClient();
        _http.BaseAddress = new Uri("https://caupo.app/api/v2/");
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<CheckoutResponse> StartCheckoutAsync(
        LicenseDeviceIdentity device, string company, string email, string plan,
        CancellationToken cancellationToken = default)
    {
        company = company.Trim();
        email = email.Trim();
        plan = plan.Trim();

        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nonce = NewNonce();

        var request = new
        {
            company,
            email,
            plan,
            fingerprint_sha256 = device.FingerprintSha256,
            device_public_key = device.PublicKey,
            device_signature = device.SignCheckout(
                plan, device.FingerprintSha256, company, email, timestamp, nonce),
            nonce,
            timestamp
        };

        CheckoutResponse result = await PostAsync<CheckoutResponse>(
            "start-checkout.php", request, cancellationToken);

        if (string.IsNullOrWhiteSpace(result.SessionId)
            || !Uri.TryCreate(result.CheckoutUrl, UriKind.Absolute, out Uri? url)
            || url.Scheme != Uri.UriSchemeHttps
            || !string.Equals(url.Host, "checkout.stripe.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Server nije vratio ispravnu Stripe Checkout sesiju.");
        }

        return result;
    }

    public async Task<LicenseClaimResponse> ActivateManualAsync(
        LicenseDeviceIdentity device, string licenseKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
            throw new ArgumentException("Unesi licencni ključ.", nameof(licenseKey));

        licenseKey = licenseKey.Trim().ToUpperInvariant();
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nonce = NewNonce();

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

        return await PostAsync<LicenseClaimResponse>(
            "activate", request, cancellationToken);
    }

    public async Task<LicenseClaimResponse> GetCheckoutStatusAsync(
        LicenseDeviceIdentity device, string sessionId,
        CancellationToken cancellationToken = default)
    {
        var request = CreateClaimRequest(device, sessionId);

        return await PostAsync<LicenseClaimResponse>(
            "checkout-status.php", request, cancellationToken);
    }

    public async Task<BillingPortalResponse> OpenBillingPortalAsync(
        LicenseDeviceIdentity device, string sessionId,
        CancellationToken cancellationToken = default)
    {
        var request = CreateClaimRequest(device, sessionId);

        BillingPortalResponse result = await PostAsync<BillingPortalResponse>(
            "billing-portal.php", request, cancellationToken);

        if (!Uri.TryCreate(result.Url, UriKind.Absolute, out Uri? url)
            || url.Scheme != Uri.UriSchemeHttps
            || !string.Equals(url.Host, "billing.stripe.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Server nije vratio ispravan Stripe Billing Portal URL.");
        }

        return result;
    }

    private static object CreateClaimRequest(LicenseDeviceIdentity device, string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Nedostaje Stripe Checkout session ID.", nameof(sessionId));

        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nonce = NewNonce();

        return new
        {
            session_id = sessionId,
            timestamp,
            nonce,
            signature = device.SignClaim(sessionId, timestamp, nonce)
        };
    }

    private async Task<T> PostAsync<T>(string endpoint, object request, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _http.PostAsJsonAsync(
            endpoint, request, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string message = $"Licencni server je vratio HTTP {(int)response.StatusCode}.";

            try
            {
                LicenseApiError? error = await response.Content.ReadFromJsonAsync<LicenseApiError>(
                    JsonOptions, cancellationToken);

                if (!string.IsNullOrWhiteSpace(error?.Error))
                    message = error.Error;
            }
            catch (JsonException)
            {
                // Zadržava se HTTP greška ako odgovor nije JSON.
            }

            throw new HttpRequestException(message, null, response.StatusCode);
        }

        T? result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        return result ?? throw new InvalidOperationException("Licencni server je vratio prazan odgovor.");
    }

    private static string NewNonce()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
    }

    public void Dispose()
    {
        if (_ownsClient)
            _http.Dispose();
    }
}
