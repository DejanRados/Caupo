using Caupo.Fiscal.Common;

namespace Caupo.Fiscal.Serbia.Models
{
    public sealed class SerbiaReceiptReprintData
    {
        public int LocalReceiptNumber { get; init; }
        public DateTime EsirDateTime { get; init; }
        public DateTime PfrDateTime { get; init; }

        public string FiscalNumber { get; init; } = string.Empty;
        public string InvoiceCounter { get; init; } = string.Empty;
        public string VerificationUrl { get; init; } = string.Empty;

        public string Cashier { get; init; } = string.Empty;
        public FiscalPaymentType PaymentType { get; init; }
        public decimal TotalAmount { get; init; }

        public IReadOnlyList<SerbiaReceiptReprintItem> Items { get; init; } = Array.Empty<SerbiaReceiptReprintItem>();
    }

    public sealed class SerbiaReceiptReprintItem
    {
        public string Name { get; init; } = string.Empty;
        public decimal Quantity { get; init; }
        public decimal UnitPrice { get; init; }

        public string TaxLabel { get; init; } = string.Empty;
        public string TaxName { get; init; } = string.Empty;
        public decimal TaxRate { get; init; }
    }
}