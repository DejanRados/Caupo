using System.Text.Json;
using System.Text.Json.Serialization;

namespace Caupo.Fiscal.Serbia.Models
{
    public sealed class SerbiaInvoiceRequest
    {
        [JsonPropertyName("dateAndTimeOfIssue")]
        public DateTimeOffset DateAndTimeOfIssue { get; init; }

        [JsonPropertyName("cashier")]
        public string? Cashier { get; init; }

        [JsonPropertyName("invoiceType")]
        public string InvoiceType { get; init; } = "Normal";

        [JsonPropertyName("transactionType")]
        public string TransactionType { get; init; } = "Sale";

        [JsonPropertyName("payment")]
        public List<SerbiaPayment> Payment { get; init; } = new();

        [JsonPropertyName("invoiceNumber")]
        public string InvoiceNumber { get; init; } = string.Empty;

        [JsonPropertyName("buyerId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? BuyerId { get; init; }

        [JsonPropertyName("buyerCostCenterId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? BuyerCostCenterId { get; init; }

        [JsonPropertyName("referentDocumentNumber")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ReferentDocumentNumber { get; init; }

        [JsonPropertyName("referentDocumentDT")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ReferentDocumentDateTime { get; init; }

        [JsonPropertyName("options")]
        public SerbiaInvoiceOptions Options { get; init; } =
            new SerbiaInvoiceOptions();

        [JsonPropertyName("items")]
        public List<SerbiaInvoiceItem> Items { get; init; } = new();
    }

    public sealed class SerbiaPayment
    {
        [JsonPropertyName("amount")]
        public decimal Amount { get; init; }

        [JsonPropertyName("paymentType")]
        public string PaymentType { get; init; } = "Cash";
    }

    public sealed class SerbiaInvoiceItem
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; init; }

        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; init; }

        [JsonPropertyName("labels")]
        public List<string> Labels { get; init; } = new();

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; init; }

        [JsonPropertyName("unit")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Unit { get; init; }
    }

    public sealed class SerbiaInvoiceOptions
    {
        [JsonPropertyName("omitQRCodeGen")]
        public string OmitQrCodeGen { get; init; } = "0";

        [JsonPropertyName("omitTextualRepresentation")]
        public string OmitTextualRepresentation { get; init; } = "0";
    }

    public sealed class SerbiaInvoiceResponse
    {
        [JsonPropertyName("requestedBy")]
        public string? RequestedBy { get; init; }

        [JsonPropertyName("signedBy")]
        public string? SignedBy { get; init; }

        [JsonPropertyName("sdcDateTime")]
        public DateTimeOffset? SdcDateTime { get; init; }

        [JsonPropertyName("invoiceNumber")]
        public string? InvoiceNumber { get; init; }

        [JsonPropertyName("invoiceCounter")]
        public string? InvoiceCounter { get; init; }

        [JsonPropertyName("journal")]
        public string? Journal { get; init; }

        [JsonPropertyName("verificationUrl")]
        public string? VerificationUrl { get; init; }

    

        [JsonPropertyName("verificationQRCode")]
        public string? VerificationQRCode { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalData { get; init; }

        [JsonIgnore]
        public string? RawJson { get; set; }
    }

    public sealed class SerbiaTaxRateEntry
    {
        public string Category { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
        public decimal Rate { get; init; }
    }
}
