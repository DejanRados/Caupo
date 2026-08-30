using Caupo.Cis;
using Caupo.Properties;
using System.Globalization;
using System.IO;

namespace Caupo.Fiscal.Croatia
{
    public sealed class CroatiaFiscalSettings
    {
        public string CompanyName { get; init; } = string.Empty;
        public string Address { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string Oib { get; init; } = string.Empty;

        public bool IsVatRegistered { get; init; }

        public string BusinessPremise { get; init; } = string.Empty;
        public string CashRegister { get; init; } = string.Empty;
        public string SequenceSetting { get; init; } = string.Empty;

        public string CertificateName { get; init; } = string.Empty;
        public string CertificatePassword { get; init; } = string.Empty;

        public string EnvironmentName { get; init; } = string.Empty;
        public string ServiceUrl { get; init; } = string.Empty;

        public decimal ConsumptionTaxRate { get; init; }

        public string PosPrinter { get; init; } = string.Empty;
        public int PaperWidthMm { get; init; } = 80;
        public string LogoPath { get; init; } = string.Empty;

        public static CroatiaFiscalSettings FromProperties()
        {
            string environment =
                Settings.Default.VerzijaAplikacije?.Trim()
                ?? string.Empty;

            bool isDemo =
                environment.Equals(
                    "Demo",
                    StringComparison.OrdinalIgnoreCase);

            decimal pnp = 0m;
            if(decimal.TryParse(
                Settings.Default.PnpStopa,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out decimal parsedPnp))
            {
                pnp = Math.Clamp(parsedPnp, 0m, 3m);
            }

            int width = 80;
            if(int.TryParse(
                Settings.Default.SirinaTrake,
                out int parsedWidth))
            {
                width = parsedWidth;
            }

            return new CroatiaFiscalSettings
            {
                CompanyName = Settings.Default.Firma ?? string.Empty,
                Address = Settings.Default.Adresa ?? string.Empty,
                City = Settings.Default.Mjesto ?? string.Empty,
                Oib = Settings.Default.JIB ?? string.Empty,

                IsVatRegistered =
                    string.Equals(
                        Settings.Default.PDVKorisnik,
                        "DA",
                        StringComparison.OrdinalIgnoreCase),

                BusinessPremise =
                    Settings.Default.PoslovniProstor
                    ?? string.Empty,

                CashRegister =
                    Settings.Default.NaplatniUredjaj
                    ?? string.Empty,

                SequenceSetting =
                    Settings.Default.OznakaSlijednosti
                    ?? string.Empty,

                CertificateName =
                    Settings.Default.CerificateName
                    ?? string.Empty,

                CertificatePassword =
                    Settings.Default.CerificatePassword
                    ?? string.Empty,

                EnvironmentName = environment,

                ServiceUrl = isDemo
                    ? Fiscalization.SERVICE_URL_DEMO
                    : Fiscalization.SERVICE_URL_PRODUCTION,

                ConsumptionTaxRate = pnp,

                PosPrinter =
                    Settings.Default.POSPrinter
                    ?? string.Empty,

                PaperWidthMm = width,

                LogoPath =
                    Settings.Default.LogoUrl
                    ?? string.Empty
            };
        }

        public string GetCertificatePath()
        {
            return Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Certificates",
                CertificateName);
        }

        public Fiscalization.FiscalizationEnvironment Environment =>
            EnvironmentName.Equals(
                "Demo",
                StringComparison.OrdinalIgnoreCase)
                ? Fiscalization.FiscalizationEnvironment.Demo
                : Fiscalization.FiscalizationEnvironment.Production;

        public void Validate()
        {
            if(string.IsNullOrWhiteSpace(Oib))
                throw new InvalidOperationException(
                    "Nije podešen OIB firme.");

            if(string.IsNullOrWhiteSpace(BusinessPremise))
                throw new InvalidOperationException(
                    "Nije podešena oznaka poslovnog prostora.");

            if(string.IsNullOrWhiteSpace(CashRegister))
                throw new InvalidOperationException(
                    "Nije podešena oznaka naplatnog uređaja.");

            if(string.IsNullOrWhiteSpace(CertificateName))
                throw new InvalidOperationException(
                    "Nije odabran certifikat za hrvatsku fiskalizaciju.");

            if(string.IsNullOrWhiteSpace(CertificatePassword))
                throw new InvalidOperationException(
                    "Nije unesena zaporka certifikata.");

            string certificatePath = GetCertificatePath();

            if(!File.Exists(certificatePath))
            {
                throw new FileNotFoundException(
                    "Certifikat za hrvatsku fiskalizaciju nije pronađen.",
                    certificatePath);
            }
        }
    }
}
