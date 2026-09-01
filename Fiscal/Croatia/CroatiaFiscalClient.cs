using Caupo.Fiscal.Common;
using Caupo.Fiscal.Croatia.Models;
using MAES.Fiskal;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;

namespace Caupo.Fiscal.Croatia
{
    public sealed class CroatiaFiscalClient
    {
        private readonly CroatiaFiscalSettings _settings;

        public CroatiaFiscalClient(CroatiaFiscalSettings settings)
        {
            _settings = settings;
        }

        public async Task<CroatiaFiscalizationResponse> FiscalizeAsync(CroatiaBuiltInvoice builtInvoice, CancellationToken cancellationToken = default)
        {
            if (builtInvoice == null)
                throw new ArgumentNullException(nameof(builtInvoice));

            _settings.Validate();

            using var certificate = new X509Certificate2(_settings.GetCertificatePath(), _settings.CertificatePassword, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

            if (!certificate.HasPrivateKey)
                throw new FiscalException("Hrvatski fiskalni certifikat nema privatni ključ.");

            string zki = builtInvoice.Invoice.ZKI(certificate);
            builtInvoice.Invoice.ZastKod = zki;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                ReferenceTypeExtensions.SslCertificateAuthentification = new()
                {
                    CertificateValidationMode = X509CertificateValidationMode.ChainTrust,
                    RevocationMode = X509RevocationMode.Online
                };

                Debug.WriteLine("[HRVATSKA] MAES koristi ChainTrust + Online revocation provjeru.");

                RacunOdgovor response = await builtInvoice.Invoice.SendAsync(certificate, _settings.ServiceUrl);

                cancellationToken.ThrowIfCancellationRequested();

                if (response == null)
                {
                    return new CroatiaFiscalizationResponse
                    {
                        Fiscalized = false,
                        Zki = zki,
                        ErrorMessage = "CIS nije vratio odgovor."
                    };
                }

                if (response.Greske != null && response.Greske.Length > 0)
                {
                    string error = string.Join(Environment.NewLine, response.Greske.Select(x => $"({x.SifraGreske}) {x.PorukaGreske}"));

                    return new CroatiaFiscalizationResponse
                    {
                        Fiscalized = false,
                        Jir = response.Jir,
                        Zki = zki,
                        ErrorMessage = error,
                        CisResponse = response
                    };
                }

                if (string.IsNullOrWhiteSpace(response.Jir))
                {
                    return new CroatiaFiscalizationResponse
                    {
                        Fiscalized = false,
                        Zki = zki,
                        ErrorMessage = "CIS odgovor ne sadrži JIR.",
                        CisResponse = response
                    };
                }

                return new CroatiaFiscalizationResponse
                {
                    Fiscalized = true,
                    Jir = response.Jir,
                    Zki = zki,
                    CisResponse = response
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("=======================================================");
                Debug.WriteLine("[HRVATSKA] MAES FISKALIZACIJA ERROR");
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("=======================================================");

                return new CroatiaFiscalizationResponse
                {
                    Fiscalized = false,
                    Zki = zki,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}