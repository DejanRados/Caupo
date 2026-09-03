namespace Caupo.Fiscal.Common
{
    public sealed class FiscalResult
    {
        public bool Success { get; init; }

        // Važno: ova tri stanja su odvojena.
        // Fiskalizovan račun se ne smije ponovo fiskalizovati
        // samo zato što print ili DB spremanje nisu uspjeli.
        public bool Fiscalized { get; init; }
        public bool SavedToDatabase { get; init; }
        public bool Printed { get; init; }

        public FiscalizationStatus FiscalizationStatus { get; init; } = FiscalizationStatus.NotFiscalized;
        public int? LocalReceiptNumber { get; init; }

        public string? ReceiptNumber { get; init; }
        public string? FiscalNumber { get; init; }

        public DateTime? FiscalDateTime { get; init; }

        public string? ErrorMessage { get; init; }

        // Po potrebi čuvamo originalni regionalni odgovor
        // radi dijagnostike, bez da Kasa zna njegov format.
        public string? RawResponse { get; init; }

        public static FiscalResult Failed(
            string errorMessage)
        {
            return new FiscalResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
