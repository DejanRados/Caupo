using Caupo.Properties;

namespace Caupo.Fiscal.Serbia
{
    public sealed class SerbiaFiscalSettings
    {
        public string PfrType { get; init; } = "VPFR";
        public string Environment { get; init; } = "Sandbox";

        public string VpfrUrl { get; init; } = string.Empty;
        public string CertificateName { get; init; } = string.Empty;
        public string CertificatePassword { get; init; } = string.Empty;
        public string Pac { get; init; } = string.Empty;
        public string AcceptLanguage { get; init; } = "en-US";

        public string LpfrToken { get; init; } = string.Empty;
        public string LpfrUrl { get; init; } = string.Empty;
        public string LpfrPin { get; init; } = string.Empty;
        public string LpfrJid { get; init; } = string.Empty;

        public static SerbiaFiscalSettings Load()
        {
            return new SerbiaFiscalSettings
            {
                PfrType =
                    string.IsNullOrWhiteSpace(Settings.Default.SrbijaPfrType)
                        ? "VPFR"
                        : Settings.Default.SrbijaPfrType.Trim(),

                Environment =
                    string.IsNullOrWhiteSpace(Settings.Default.SrbijaEnvironment)
                        ? "Sandbox"
                        : Settings.Default.SrbijaEnvironment.Trim(),

                VpfrUrl =
                    Settings.Default.SrbijaVPFRUrl?.Trim()
                    ?? string.Empty,

                CertificateName =
                    Settings.Default.SrbijaCertificateName?.Trim()
                    ?? string.Empty,

                CertificatePassword =
                    Settings.Default.SrbijaCertificatePassword
                    ?? string.Empty,

                Pac =
                    Settings.Default.SrbijaPAC?.Trim()
                    ?? string.Empty,

                AcceptLanguage =
                    string.IsNullOrWhiteSpace(
                        Settings.Default.SrbijaAcceptLanguage)
                        ? "en-US"
                        : Settings.Default.SrbijaAcceptLanguage.Trim(),

                LpfrToken =
                    Settings.Default.SrbijaLPFRToken?.Trim()
                    ?? string.Empty,

                LpfrUrl =
                    Settings.Default.SrbijaLPFRUrl?.Trim()
                    ?? string.Empty,

                LpfrPin =
                    Settings.Default.SrbijaLPFRPin?.Trim()
                    ?? string.Empty,

                LpfrJid =
                    Settings.Default.SrbijaLPFRJid?.Trim()
                    ?? string.Empty
            };
        }

        public void Validate()
        {
            if(string.Equals(
                PfrType,
                "VPFR",
                StringComparison.OrdinalIgnoreCase))
            {
                if(string.IsNullOrWhiteSpace(VpfrUrl))
                    throw new InvalidOperationException(
                        "Srbija V-PFR URL nije podešen.");

                if(string.IsNullOrWhiteSpace(CertificateName))
                    throw new InvalidOperationException(
                        "Srbija V-PFR certifikat nije podešen.");

                if(string.IsNullOrWhiteSpace(CertificatePassword))
                    throw new InvalidOperationException(
                        "Srbija V-PFR zaporka certifikata nije podešena.");

                if(string.IsNullOrWhiteSpace(Pac))
                    throw new InvalidOperationException(
                        "Srbija V-PFR PAC nije podešen.");

                return;
            }

            if(string.Equals(
                PfrType,
                "LPFR",
                StringComparison.OrdinalIgnoreCase))
            {
                if(string.IsNullOrWhiteSpace(LpfrUrl))
                    throw new InvalidOperationException(
                        "Srbija L-PFR URL nije podešen.");

                if(string.IsNullOrWhiteSpace(LpfrToken))
                    throw new InvalidOperationException(
                        "Srbija L-PFR token nije podešen.");

                if(string.IsNullOrWhiteSpace(LpfrPin))
                    throw new InvalidOperationException(
                        "Srbija L-PFR PIN nije podešen.");

                return;
            }

            throw new InvalidOperationException(
                $"Nepoznat Srbija PFR tip: {PfrType}");
        }
    }
}
