using System.Text.Json.Serialization;

namespace Caupo.Licensing.Models;

public sealed class CheckoutResponse
{
    [JsonPropertyName("checkout_url")]
    public string CheckoutUrl { get; set; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string SessionId { get; set; } = string.Empty;
}

public sealed class LicenseClaimResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("license_id")]
    public string? LicenseId { get; set; }

    [JsonPropertyName("device_id")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("paid_until")]
    public string? PaidUntil { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }
}

public sealed class BillingPortalResponse
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

public sealed class LicenseApiError
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;
}