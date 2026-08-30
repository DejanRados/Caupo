using Caupo.Cis;
using Caupo.Fiscal.Common;
using Caupo.Fiscal.Croatia.Models;
using System.Security.Cryptography.X509Certificates;

namespace Caupo.Fiscal.Croatia
{
    /// <summary>
    /// Tanki Caupo adapter prema postojećem Caupo.Cis sloju.
    /// Ne implementira SOAP/XML potpisivanje ponovo.
    /// </summary>
    public sealed class CroatiaFiscalClient
    {
        private readonly CroatiaFiscalSettings _settings;

        public CroatiaFiscalClient(
            CroatiaFiscalSettings settings)
        {
            _settings = settings;
        }

        public async Task<CroatiaFiscalizationResponse>
            FiscalizeAsync(
                CroatiaBuiltInvoice builtInvoice,
                CancellationToken cancellationToken = default)
        {
            if(builtInvoice == null)
                throw new ArgumentNullException(
                    nameof(builtInvoice));

            _settings.Validate();

            using var certificate =
                new X509Certificate2(
                    _settings.GetCertificatePath(),
                    _settings.CertificatePassword,
                    X509KeyStorageFlags.MachineKeySet |
                    X509KeyStorageFlags.Exportable);

            if(!certificate.HasPrivateKey)
            {
                throw new FiscalException(
                    "Hrvatski fiskalni certifikat nema privatni ključ.");
            }

            Fiscalization.GenerateZki(
                builtInvoice.Invoice,
                certificate);

            string? zki =
                builtInvoice.Invoice.ZastKod;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                RacunOdgovor response =
                    await Fiscalization.SendInvoiceAsync(
                        builtInvoice.Invoice,
                        certificate,
                        service =>
                        {
                            service.Url =
                                _settings.ServiceUrl;
                        });

                cancellationToken.ThrowIfCancellationRequested();

                if(response == null)
                {
                    return new CroatiaFiscalizationResponse
                    {
                        Fiscalized = false,
                        Zki = zki,
                        ErrorMessage =
                            "CIS nije vratio odgovor."
                    };
                }

                if(response.Greske != null &&
                   response.Greske.Length > 0)
                {
                    string error =
                        string.Join(
                            Environment.NewLine,
                            response.Greske.Select(
                                x =>
                                    $"({x.SifraGreske}) " +
                                    $"{x.PorukaGreske}"));

                    return new CroatiaFiscalizationResponse
                    {
                        Fiscalized = false,
                        Jir = response.Jir,
                        Zki = zki,
                        ErrorMessage = error,
                        CisResponse = response
                    };
                }

                if(string.IsNullOrWhiteSpace(
                    response.Jir))
                {
                    return new CroatiaFiscalizationResponse
                    {
                        Fiscalized = false,
                        Zki = zki,
                        ErrorMessage =
                            "CIS odgovor ne sadrži JIR.",
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
            catch(OperationCanceledException)
            {
                throw;
            }
            catch(Exception ex)
            {
                // Za Hrvatsku je važno sačuvati račun i kada
                // fiskalizacija trenutno nije moguća.
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
