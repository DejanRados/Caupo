
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Caupo.Licensing.Models;

public sealed class PaddleCatalogResponse
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("plans")]
    public List<PaddlePlan> Plans { get; set; } = new();
}

public sealed class PaddlePlan
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("interval")]
    public string Interval { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("tiers")]
    public List<PaddlePriceTier> Tiers { get; set; } = new();

    public override string ToString() => Name;
}

public sealed class PaddlePriceTier
{
    [JsonPropertyName("min")]
    public int Min { get; set; }

    [JsonPropertyName("max")]
    public int? Max { get; set; }

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("recurring_price_id")]
    public string? RecurringPriceId { get; set; }

    [JsonPropertyName("one_time_price_id")]
    public string? OneTimePriceId { get; set; }
}

public sealed class PaddleCustomer
{
    [JsonPropertyName("company_name")]
    public string CompanyName { get; set; } = string.Empty;

    [JsonPropertyName("contact_name")]
    public string ContactName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("postal_code")]
    public string PostalCode { get; set; } = string.Empty;

    [JsonPropertyName("country_code")]
    public string CountryCode { get; set; } = string.Empty;

    [JsonPropertyName("tax_number")]
    public string TaxNumber { get; set; } = string.Empty;
}

public sealed class CheckoutResponse
{
    [JsonPropertyName("transaction_id")]
    public string TransactionId { get; set; } = string.Empty;

    [JsonPropertyName("checkout_url")]
    public string CheckoutUrl { get; set; } = string.Empty;
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

    [JsonPropertyName("customer")]
    public PaddleCustomer? Customer { get; set; }
}

public sealed class LicenseApiError
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;
}
