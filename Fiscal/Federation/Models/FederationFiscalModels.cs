using TringRacun = global::Tring.Fiscal.Driver.Racun;
using TringKasaOdgovor = global::Tring.Fiscal.Driver.KasaOdgovor;

namespace Caupo.Fiscal.Federation.Models
{
    public sealed class FederationBuiltInvoice
    {
        public TringRacun Invoice { get; init; } =
            new TringRacun();

        public int LocalReceiptNumber { get; init; }

        public DateTime IssueDateTime { get; init; }

        public decimal TotalAmount { get; init; }
    }

    public sealed class FederationFiscalResponse
    {
        public bool Fiscalized { get; init; }
        public bool Printed { get; init; }

        public string? FiscalReceiptNumber { get; init; }
        public string? ErrorMessage { get; init; }

        public TringKasaOdgovor? RawResponse { get; init; }
    }
}
