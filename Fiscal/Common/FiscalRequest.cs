using Caupo.Models;

namespace Caupo.Fiscal.Common
{
    public sealed class FiscalRequest
    {
        public IReadOnlyList<RacunStavka> Items { get; init; } =
            Array.Empty<RacunStavka>();

        public FiscalBuyer? Buyer { get; init; }

        public FiscalCashier Cashier { get; init; } =
            new FiscalCashier();

        public FiscalPaymentType PaymentType { get; init; }

        public decimal TotalAmount { get; init; }

        // Normal / Sale su neutralne početne vrijednosti.
        // Regionalni builder ih prevodi u format koji njegov API očekuje.
        public string InvoiceType { get; init; } = "Normal";
        public string TransactionType { get; init; } = "Sale";

        public string? ReferentDocumentNumber { get; init; }
        public DateTime? ReferentDocumentDateTime { get; init; }

        public bool IsRefund =>
            string.Equals(
                TransactionType,
                "Refund",
                StringComparison.OrdinalIgnoreCase);
    }
}
