using System;
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

public sealed class CustomerLicensesResponse
{
    [JsonPropertyName("customer")]
    public PaddleCustomer? Customer { get; set; }

    [JsonPropertyName("licenses")]
    public List<CustomerLicense> Licenses { get; set; } = new();

    [JsonPropertyName("order")]
    public CustomerOrderStatus? Order { get; set; }
}

public sealed class CustomerOrderStatus
{
    [JsonPropertyName("transaction_id")]
    public string TransactionId { get; set; } = string.Empty;

    [JsonPropertyName("order_type")]
    public string OrderType { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("issued_count")]
    public int IssuedCount { get; set; }

    [JsonPropertyName("confirmed")]
    public bool Confirmed { get; set; }
}

public sealed class CustomerLicense
{
    [JsonPropertyName("license_id")]
    public int LicenseId { get; set; }

    [JsonPropertyName("license_public_id")]
    public string LicensePublicId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("expires_at")]
    public string? ExpiresAt { get; set; }

    [JsonPropertyName("activated_at")]
    public string? ActivatedAt { get; set; }

    [JsonPropertyName("period_start")]
    public string? PeriodStart { get; set; }

    [JsonIgnore]
    public string ExpiresDisplay => FormatDate(ExpiresAt);

    private static string FormatDate(string? value) =>
        DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed.ToLocalTime().ToString("dd.MM.yyyy. HH:mm") : "—";

    [JsonPropertyName("plan")]
    public CustomerLicensePlan Plan { get; set; } = new();

    [JsonPropertyName("subscription")]
    public CustomerLicenseSubscription? Subscription { get; set; }

    [JsonPropertyName("can_renew")]
    public bool CanRenew { get; set; }

    [JsonPropertyName("device_name")]
    public string? DeviceName { get; set; }

    [JsonPropertyName("device_public_id")]
    public string? DevicePublicId { get; set; }

    [JsonIgnore]
    public string DeviceDescription => string.IsNullOrWhiteSpace(DevicePublicId)
        ? "Nije aktivirana na uređaju"
        : string.IsNullOrWhiteSpace(DeviceName) ? "Aktivirana na drugom uređaju" : $"Uređaj: {DeviceName}";
}

public sealed class CustomerLicensePlan
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public sealed class CustomerLicenseSubscription
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("cancel_at_period_end")]
    public bool CancelAtPeriodEnd { get; set; }

    [JsonPropertyName("current_period_end")]
    public string? CurrentPeriodEnd { get; set; }
}

