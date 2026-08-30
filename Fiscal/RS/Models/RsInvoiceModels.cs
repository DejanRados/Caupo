using Newtonsoft.Json;

namespace Caupo.Fiscal.RS.Models
{
    public sealed class RsFiscalEnvelope
    {
        [JsonProperty("print")]
        public bool Print { get; set; }

        [JsonProperty("renderReceiptImage")]
        public bool RenderReceiptImage { get; set; }

        [JsonProperty("receiptImageFormat")]
        public string ReceiptImageFormat { get; set; } = "Png";

        [JsonProperty("receiptSlipWidth")]
        public int ReceiptSlipWidth { get; set; }

        [JsonProperty("receiptSlipFontSizeNormal")]
        public int ReceiptSlipFontSizeNormal { get; set; }

        [JsonProperty("receiptSlipFontSizeLarge")]
        public int ReceiptSlipFontSizeLarge { get; set; }

        [JsonProperty("invoiceRequest")]
        public RsInvoiceRequest InvoiceRequest { get; set; } =
            new RsInvoiceRequest();
    }

    public sealed class RsInvoiceRequest
    {
        [JsonProperty("invoiceType")]
        public string InvoiceType { get; set; } = "Normal";

        [JsonProperty("transactionType")]
        public string TransactionType { get; set; } = "Sale";

        [JsonProperty("buyerId", NullValueHandling = NullValueHandling.Ignore)]
        public string? BuyerId { get; set; }

        [JsonProperty("buyerCostCenterId", NullValueHandling = NullValueHandling.Ignore)]
        public string? BuyerCostCenterId { get; set; }

        [JsonProperty("referentDocumentNumber", NullValueHandling = NullValueHandling.Ignore)]
        public string? ReferentDocumentNumber { get; set; }

        [JsonProperty("referentDocumentDT", NullValueHandling = NullValueHandling.Ignore)]
        public string? ReferentDocumentDateTime { get; set; }

        [JsonProperty("payment")]
        public List<RsPayment> Payments { get; set; } =
            new List<RsPayment>();

        [JsonProperty("items")]
        public List<RsInvoiceItem> Items { get; set; } =
            new List<RsInvoiceItem>();

        [JsonProperty("cashier")]
        public string Cashier { get; set; } = string.Empty;
    }

    public sealed class RsPayment
    {
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("paymentType")]
        public string PaymentType { get; set; } = string.Empty;
    }

    public sealed class RsInvoiceItem
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("labels")]
        public List<string> Labels { get; set; } =
            new List<string>();

        [JsonProperty("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonProperty("unitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonProperty("quantity")]
        public decimal Quantity { get; set; }

        [JsonProperty("note", NullValueHandling = NullValueHandling.Ignore)]
        public string? Note { get; set; }
    }

    public sealed class RsBuiltInvoice
    {
        public RsFiscalEnvelope Envelope { get; init; } =
            new RsFiscalEnvelope();

        public int LocalReceiptNumber { get; init; }
        public DateTime IssueDateTime { get; init; }
    }

    public sealed class RsFiscalResponse
    {
        public bool Fiscalized { get; init; }

        public string? FiscalReceiptNumber { get; init; }
        public string? TotalCounter { get; init; }

        public DateTime? SdcDateTime { get; init; }

        public string? Journal { get; init; }
        public string? VerificationUrl { get; init; }

        public string? RawResponse { get; init; }
        public string? ErrorMessage { get; init; }
    }
}
