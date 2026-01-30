using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caupo.Fiscal
{


    public class FiskalniRacunSrbija
    {
        [JsonProperty ("invoiceNumber")]
        public string? InvoiceNumber { get; set; }

        [JsonProperty ("invoiceType")]
        public int? InvoiceType { get; set; }

        [JsonProperty ("transactionType")]
        public int? TransactionType { get; set; }

        [JsonProperty ("buyerId")]
        public string? BuyerId { get; set; }

        [JsonProperty ("buyerCostCenterId")]
        public string? BuyerCostCenterId { get; set; }

        [JsonProperty ("cashier")]
        public string? Cashier { get; set; }

        [JsonProperty ("dateAndTimeOfIssue")]
        public DateTime? DateAndTimeOfIssue { get; set; }

        [JsonProperty ("options")]
        public Options? InvoiceOptions { get; set; }

        [JsonProperty ("payment")]
        public List<Payment>? Payments { get; set; }

        [JsonProperty ("items")]
        public List<Item>? Items { get; set; }

        // --- Inner classes ---
        public class Options
        {
            [JsonProperty ("omitQRCodeGen")]
            public string? OmitQRCodeGen { get; set; }

            [JsonProperty ("omitTextualRepresentation")]
            public string? OmitTextualRepresentation { get; set; }
        }

        public class Payment
        {
            [JsonProperty ("paymentType")]
            public int? PaymentType { get; set; }

            [JsonProperty ("amount")]
            public decimal? Amount { get; set; }
        }

        public class Item
        {
            [JsonProperty ("name")]
            public string? Name { get; set; }

            [JsonProperty ("labels")]
            public List<string> Labels { get; set; } = new List<string> ();

            [JsonProperty ("quantity")]
            public decimal? Quantity { get; set; }

            [JsonProperty ("unitPrice")]
            public decimal? UnitPrice { get; set; }

            [JsonProperty ("totalAmount")]
            public decimal? TotalAmount { get; set; } // JSON ima unaprijed izračunat totalAmount
        }
    }

}
