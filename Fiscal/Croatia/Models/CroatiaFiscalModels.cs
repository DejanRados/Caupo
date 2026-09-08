using MAES.Fiskal;

namespace Caupo.Fiscal.Croatia.Models
{
    public sealed class CroatiaTaxSummary
    {
        public decimal Rate { get; init; }
        public decimal BaseAmount { get; init; }
        public decimal TaxAmount { get; init; }
        public bool IsConsumptionTax { get; init; }
    }

    public sealed class CroatiaBuiltInvoice
    {
        public RacunType Invoice { get; init; } = new RacunType();
        public int LocalReceiptNumber { get; init; }
        public string ReceiptNumberHr { get; init; } = string.Empty;
        public DateTime IssueDateTime { get; init; }
        public IReadOnlyList<CroatiaTaxSummary> Taxes { get; init; } = Array.Empty<CroatiaTaxSummary>();
        public decimal TotalAmount { get; init; }
    }

    public sealed class CroatiaFiscalizationResponse
    {
        public bool Fiscalized { get; init; }
        public string? Jir { get; init; }
        public string? Zki { get; init; }
        public string? ErrorMessage { get; init; }
        public bool CisRejected { get; init; }
        public RacunOdgovor? CisResponse { get; init; }
    }
}