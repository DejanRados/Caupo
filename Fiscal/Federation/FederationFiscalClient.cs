using Caupo.Fiscal.Federation.Models;
using System.Diagnostics;
using TringFiskalniPrinter = global::Tring.Fiscal.Driver.TringFiskalniPrinter;
using TringKasaOdgovor = global::Tring.Fiscal.Driver.KasaOdgovor;
using TringVrsteOdgovora = global::Tring.Fiscal.Driver.VrsteOdgovora;

namespace Caupo.Fiscal.Federation
{
    public sealed class FederationFiscalClient
    {
        private readonly FederationFiscalSettings _settings;

        public FederationFiscalClient(
            FederationFiscalSettings settings)
        {
            _settings = settings;
        }

        public FederationFiscalResponse Issue(
            FederationBuiltInvoice builtInvoice)
        {
            if(builtInvoice == null)
                throw new ArgumentNullException(
                    nameof(builtInvoice));

            _settings.Validate();

            var printer =
                new TringFiskalniPrinter();

            try
            {
                printer.Inicijalizacija(
                    _settings.ServerIpAddress,
                    _settings.ServerPort,
                    _settings.PrinterIndex,
                    _settings.PrinterPassword);

                Debug.WriteLine(
                    "[FEDERACIJA/TRING] Printer uspješno inicijalizovan.");
                Debug.WriteLine($"[FEDERACIJA/TRING] IP={_settings.ServerIpAddress}");
                Debug.WriteLine($"[FEDERACIJA/TRING] Port={_settings.ServerPort}");
                Debug.WriteLine($"[FEDERACIJA/TRING] PrinterIndex={_settings.PrinterIndex}");
                Debug.WriteLine($"[FEDERACIJA/TRING] PrinterPassword={_settings.PrinterPassword}");
            }
            catch(Exception ex)
            {
                return new FederationFiscalResponse
                {
                    Fiscalized = false,
                    Printed = false,
                    ErrorMessage =
                        "Ne može se povezati na Tring fiskalni server: " +
                        ex.Message
                };
            }

            try
            {
                TringKasaOdgovor response =
                    printer.StampatiFiskalniRacun(
                        builtInvoice.Invoice);

                if(response == null)
                {
                    return new FederationFiscalResponse
                    {
                        Fiscalized = false,
                        Printed = false,
                        ErrorMessage =
                            "Tring fiskalni printer nije vratio odgovor."
                    };
                }

                Debug.WriteLine($"[FEDERACIJA/TRING] Vrsta odgovora: {response.VrstaOdgovora}");
                Debug.WriteLine($"[FEDERACIJA/TRING] Broj zahtjeva: {response.BrojZahtjeva}");


                if (response.Odgovori != null)
                {
                    Debug.WriteLine("[FEDERACIJA/TRING] Sadržaj odgovora:");

                    foreach (var odgovor in response.Odgovori)
                        Debug.WriteLine($"[FEDERACIJA/TRING] {odgovor.Naziv}: {odgovor.Vrijednost}");
                }
                else
                {
                    Debug.WriteLine("[FEDERACIJA/TRING] Odgovori = NULL");
                }

                if (response.VrstaOdgovora ==
                   TringVrsteOdgovora.Greska)
                {
                    string error =
                        response.Odgovori == null
                            ? "Tring fiskalni printer je vratio grešku."
                            : string.Join(
                                Environment.NewLine,
                                response.Odgovori.Select(
                                    x =>
                                        $"{x.Naziv}: {x.Vrijednost}"));

                    Debug.WriteLine(
                        "[FEDERACIJA/TRING] GREŠKA:" +
                        Environment.NewLine +
                        error);

                    return new FederationFiscalResponse
                    {
                        Fiscalized = false,
                        Printed = false,
                        ErrorMessage = error,
                        RawResponse = response
                    };
                }

                string? fiscalNumber =
                    response.Odgovori?
                        .FirstOrDefault(
                            x =>
                                x.Naziv ==
                                "BrojFiskalnogRacuna")
                        ?.Vrijednost
                        ?.ToString();

                if(string.IsNullOrWhiteSpace(
                    fiscalNumber))
                {
                    return new FederationFiscalResponse
                    {
                        Fiscalized = false,
                        Printed = true,
                        ErrorMessage =
                            "Tring je vratio OK odgovor, ali broj " +
                            "fiskalnog računa nije pronađen u odgovoru.",
                        RawResponse = response
                    };
                }

                Debug.WriteLine(
                    "[FEDERACIJA/TRING] Fiskalni račun: " +
                    fiscalNumber);

                return new FederationFiscalResponse
                {
                    Fiscalized = true,
                    Printed = true,
                    FiscalReceiptNumber =
                        fiscalNumber,
                    RawResponse = response
                };
            }
            catch(Exception ex)
            {
                return new FederationFiscalResponse
                {
                    Fiscalized = false,
                    Printed = false,
                    ErrorMessage =
                        ex.Message
                };
            }
        }
    }
}
