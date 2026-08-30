using Caupo.Properties;

namespace Caupo.Fiscal.RS
{
    public sealed class RsFiscalSettings
    {
        public string IpAddress { get; init; } = string.Empty;
        public string ApiKey { get; init; } = string.Empty;
        public string Pin { get; init; } = string.Empty;

        public bool ExternalPrinter { get; init; }
        public string PosPrinter { get; init; } = string.Empty;
        public string PaperWidth { get; init; } = "80";

        public int Port { get; init; } = 3566;

        public string BaseUrl =>
            $"http://{IpAddress}:{Port}/api";

        public static RsFiscalSettings FromProperties()
        {
            return new RsFiscalSettings
            {
                IpAddress =
                    Settings.Default.LPFR_IP?.Trim()
                    ?? string.Empty,

                ApiKey =
                    Settings.Default.LPFR_Key?.Trim()
                    ?? string.Empty,

                Pin =
                    Settings.Default.LPFR_Pin
                    ?? string.Empty,

                ExternalPrinter =
                    string.Equals(
                        Settings.Default.ExterniPrinter,
                        "DA",
                        StringComparison.OrdinalIgnoreCase),

                PosPrinter =
                    Settings.Default.POSPrinter?.Trim()
                    ?? string.Empty,

                PaperWidth =
                    string.IsNullOrWhiteSpace(
                        Settings.Default.SirinaTrake)
                        ? "80"
                        : Settings.Default.SirinaTrake
            };
        }

        public void Validate()
        {
            if(string.IsNullOrWhiteSpace(IpAddress))
                throw new InvalidOperationException(
                    "Nije podešena IP adresa LPFR-a za Republiku Srpsku.");

            if(string.IsNullOrWhiteSpace(ApiKey))
                throw new InvalidOperationException(
                    "Nije podešen LPFR API ključ za Republiku Srpsku.");

            if(string.IsNullOrWhiteSpace(Pin))
                throw new InvalidOperationException(
                    "Nije podešen LPFR PIN za Republiku Srpsku.");
        }
    }
}
