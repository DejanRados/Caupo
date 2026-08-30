using Caupo.Properties;

namespace Caupo.Fiscal.Federation
{
    public sealed class FederationFiscalSettings
    {
        public string ServerIpAddress { get; init; } = string.Empty;
        public int ServerPort { get; init; } = 8085;
        public int PrinterIndex { get; init; } = 0;
        public string PrinterPassword { get; init; } = "0";

        public static FederationFiscalSettings FromProperties()
        {
            return new FederationFiscalSettings
            {
                ServerIpAddress =
                    Settings.Default.TringServerIpAddress?.Trim()
                    ?? string.Empty
            };
        }

        public void Validate()
        {
            if(string.IsNullOrWhiteSpace(ServerIpAddress))
            {
                throw new InvalidOperationException(
                    "Nije podešena IP adresa Tring fiskalnog servera.");
            }
        }
    }
}
