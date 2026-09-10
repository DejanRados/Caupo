using Caupo.Fiscal.Common;

namespace Caupo.Fiscal.RS.Models
{
    public sealed class RsReceiptReprintData
    {
        public int LocalReceiptNumber { get; init; }

        public DateTime EsirDateTime { get; init; }

        public DateTime PfrDateTime { get; init; }

        public string FiscalReceiptNumber { get; init; } = string.Empty;

        public string TotalCounter { get; init; } = string.Empty;

        public string VerificationUrl { get; init; } = string.Empty;

        public string Cashier { get; init; } = string.Empty;

        public FiscalPaymentType PaymentType { get; init; }

        public decimal TotalAmount { get; init; }

        public IReadOnlyList<RsReceiptReprintItem> Items { get; init; } = Array.Empty<RsReceiptReprintItem>();
        
        public bool IsRefund { get; init; }

        public string ReferentDocumentNumber { get; init; } = string.Empty;

        public DateTime? ReferentDocumentDateTime { get; init; }
    }

    public sealed class RsReceiptReprintItem
    {
        public string Name { get; init; } = string.Empty;

        public decimal Quantity { get; init; }

        public decimal UnitPrice { get; init; }

        public decimal TotalAmount { get; init; }

        public int? TaxRateId { get; init; }

        public string TaxLabel { get; init; } = string.Empty;

        public string TaxName { get; init; } = string.Empty;

        public decimal TaxRate { get; init; }
    }
}