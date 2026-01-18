using Newtonsoft.Json;

namespace Caupo.Fiscal
{
    public class InvoiceResponse
    {
        [JsonProperty ("address", NullValueHandling = NullValueHandling.Ignore)]
        public string Address { get; set; }

        [JsonProperty ("businessName", NullValueHandling = NullValueHandling.Ignore)]
        public string BusinessName { get; set; }

        [JsonProperty ("district", NullValueHandling = NullValueHandling.Ignore)]
        public string District { get; set; }

        [JsonProperty ("encryptedInternalData", NullValueHandling = NullValueHandling.Ignore)]
        public string EncryptedInternalData { get; set; }

        [JsonProperty ("invoiceCounter", NullValueHandling = NullValueHandling.Ignore)]
        public string InvoiceCounter { get; set; }

        [JsonProperty ("invoiceCounterExtension", NullValueHandling = NullValueHandling.Ignore)]
        public string InvoiceCounterExtension { get; set; }

        [JsonProperty ("invoiceImageHtml", NullValueHandling = NullValueHandling.Ignore)]
        public string InvoiceImageHtml { get; set; }

        [JsonProperty ("invoiceImagePdfBase64", NullValueHandling = NullValueHandling.Ignore)]
        public string InvoiceImagePdfBase64 { get; set; }

        [JsonProperty ("invoiceImagePngBase64", NullValueHandling = NullValueHandling.Ignore)]
        public string InvoiceImagePngBase64 { get; set; }

        [JsonProperty ("invoiceNumber", NullValueHandling = NullValueHandling.Ignore)]
        public string InvoiceNumber { get; set; }

        [JsonProperty ("journal", NullValueHandling = NullValueHandling.Ignore)]
        public string Journal { get; set; }

        [JsonProperty ("locationName", NullValueHandling = NullValueHandling.Ignore)]
        public string LocationName { get; set; }

        [JsonProperty ("messages", NullValueHandling = NullValueHandling.Ignore)]
        public string Messages { get; set; }

        [JsonProperty ("mrc", NullValueHandling = NullValueHandling.Ignore)]
        public string Mrc { get; set; }

        [JsonProperty ("requestedBy", NullValueHandling = NullValueHandling.Ignore)]
        public string RequestedBy { get; set; }

        [JsonProperty ("sdcDateTime", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime SdcDateTime { get; set; }

        [JsonProperty ("signature", NullValueHandling = NullValueHandling.Ignore)]
        public string Signature { get; set; }

        [JsonProperty ("signedBy", NullValueHandling = NullValueHandling.Ignore)]
        public string SignedBy { get; set; }

        [JsonProperty ("taxGroupRevision", NullValueHandling = NullValueHandling.Ignore)]
        public int TaxGroupRevision { get; set; }

        [JsonProperty ("taxItems", NullValueHandling = NullValueHandling.Ignore)]
        public List<TaxItem> TaxItems { get; set; }

        [JsonProperty ("tin", NullValueHandling = NullValueHandling.Ignore)]
        public string Tin { get; set; }

        [JsonProperty ("totalAmount", NullValueHandling = NullValueHandling.Ignore)]
        public decimal TotalAmount { get; set; }

        [JsonProperty ("totalCounter", NullValueHandling = NullValueHandling.Ignore)]
        public int TotalCounter { get; set; }

        [JsonProperty ("transactionTypeCounter", NullValueHandling = NullValueHandling.Ignore)]
        public int TransactionTypeCounter { get; set; }

        [JsonProperty ("verificationQRCode", NullValueHandling = NullValueHandling.Ignore)]
        public string VerificationQRCode { get; set; }

        [JsonProperty ("verificationUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string VerificationUrl { get; set; }
    }

    public class TaxItem
    {
        [JsonProperty ("amount", NullValueHandling = NullValueHandling.Ignore)]
        public decimal Amount { get; set; }

        [JsonProperty ("categoryName", NullValueHandling = NullValueHandling.Ignore)]
        public string CategoryName { get; set; }

        [JsonProperty ("categoryType", NullValueHandling = NullValueHandling.Ignore)]
        public int CategoryType { get; set; }

        [JsonProperty ("label", NullValueHandling = NullValueHandling.Ignore)]
        public string Label { get; set; }

        [JsonProperty ("rate", NullValueHandling = NullValueHandling.Ignore)]
        public decimal Rate { get; set; }
    }
}
