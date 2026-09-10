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

        public FederationFiscalResponse PrintDuplicate(string fiscalReceiptNumber)
        {
            if (string.IsNullOrWhiteSpace(fiscalReceiptNumber))
                throw new ArgumentException("Broj fiskalnog računa nije zadan.", nameof(fiscalReceiptNumber));

            _settings.Validate();

            var printer = new TringFiskalniPrinter();

            try
            {
                printer.Inicijalizacija(
                    _settings.ServerIpAddress,
                    _settings.ServerPort,
                    _settings.PrinterIndex,
                    _settings.PrinterPassword);

                Debug.WriteLine("[FEDERACIJA/TRING] Štampanje duplikata fiskalnog računa: " + fiscalReceiptNumber);
            }
            catch (Exception ex)
            {
                return new FederationFiscalResponse
                {
                    Fiscalized = false,
                    Printed = false,
                    ErrorMessage = "Ne može se povezati na Tring fiskalni server: " + ex.Message
                };
            }

            try
            {
                if (!int.TryParse(fiscalReceiptNumber, out int receiptNumber))
                {
                    return new FederationFiscalResponse
                    {
                        Fiscalized = false,
                        Printed = false,
                        ErrorMessage = "Neispravan broj fiskalnog računa: " + fiscalReceiptNumber
                    };
                }

                TringKasaOdgovor response = printer.StampatiDuplikatFiskalnogRacuna(receiptNumber);

                if (response == null)
                {
                    return new FederationFiscalResponse
                    {
                        Fiscalized = false,
                        Printed = false,
                        ErrorMessage = "Tring fiskalni printer nije vratio odgovor."
                    };
                }

                Debug.WriteLine($"[FEDERACIJA/TRING] Duplikat - vrsta odgovora: {response.VrstaOdgovora}");
                Debug.WriteLine($"[FEDERACIJA/TRING] Duplikat - broj zahtjeva: {response.BrojZahtjeva}");

                if (response.Odgovori != null)
                {
                    foreach (var odgovor in response.Odgovori)
                        Debug.WriteLine($"[FEDERACIJA/TRING] {odgovor.Naziv}: {odgovor.Vrijednost}");
                }

                if (response.VrstaOdgovora == TringVrsteOdgovora.Greska)
                {
                    string error = response.Odgovori == null
                        ? "Tring fiskalni printer je vratio grešku pri štampanju duplikata."
                        : string.Join(Environment.NewLine, response.Odgovori.Select(x => $"{x.Naziv}: {x.Vrijednost}"));

                    return new FederationFiscalResponse
                    {
                        Fiscalized = false,
                        Printed = false,
                        ErrorMessage = error,
                        RawResponse = response
                    };
                }

                return new FederationFiscalResponse
                {
                    Fiscalized = true,
                    Printed = true,
                    FiscalReceiptNumber = fiscalReceiptNumber,
                    RawResponse = response
                };
            }
            catch (Exception ex)
            {
                return new FederationFiscalResponse
                {
                    Fiscalized = false,
                    Printed = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public FederationFiscalResponse IssueRefund(FederationBuiltInvoice builtInvoice)
        {
            if (builtInvoice == null)
                throw new ArgumentNullException(nameof(builtInvoice));

            _settings.Validate();

            var printer = new TringFiskalniPrinter();

            try
            {
                printer.Inicijalizacija(_settings.ServerIpAddress, _settings.ServerPort, _settings.PrinterIndex, _settings.PrinterPassword);

                Debug.WriteLine("[FEDERACIJA/TRING] Printer uspješno inicijalizovan za reklamaciju.");
            }
            catch (Exception ex)
            {
                return new FederationFiscalResponse
                {
                    Fiscalized = false,
                    Printed = false,
                    ErrorMessage = "Ne može se povezati na Tring fiskalni server: " + ex.Message
                };
            }

            try
            {
                TringKasaOdgovor response = printer.StampatiReklamiraniRacun(builtInvoice.Invoice);

                if (response == null)
                {
                    return new FederationFiscalResponse
                    {
                        Fiscalized = false,
                        Printed = false,
                        ErrorMessage = "Tring fiskalni printer nije vratio odgovor."
                    };
                }

                Debug.WriteLine($"[FEDERACIJA/TRING REFUND] Vrsta odgovora: {response.VrstaOdgovora}");
                Debug.WriteLine($"[FEDERACIJA/TRING REFUND] Broj zahtjeva: {response.BrojZahtjeva}");

                if (response.Odgovori != null)
                {
                    Debug.WriteLine("[FEDERACIJA/TRING REFUND] Sadržaj odgovora:");

                    foreach (var odgovor in response.Odgovori)
                        Debug.WriteLine($"[FEDERACIJA/TRING REFUND] {odgovor.Naziv}: {odgovor.Vrijednost}");
                }

                if (response.VrstaOdgovora == TringVrsteOdgovora.Greska)
                {
                    string error = response.Odgovori == null
                        ? "Tring fiskalni printer je vratio grešku pri reklamaciji računa."
                        : string.Join(Environment.NewLine, response.Odgovori.Select(x => $"{x.Naziv}: {x.Vrijednost}"));

                    Debug.WriteLine("[FEDERACIJA/TRING REFUND] GREŠKA:" + Environment.NewLine + error);

                    return new FederationFiscalResponse
                    {
                        Fiscalized = false,
                        Printed = false,
                        ErrorMessage = error,
                        RawResponse = response
                    };
                }

                string? fiscalNumber = response.Odgovori?.FirstOrDefault(x => x.Naziv == "BrojFiskalnogRacuna")?.Vrijednost?.ToString();
                string? fiscalDate = response.Odgovori?.FirstOrDefault(x => x.Naziv == "DatumFiskalnogRacuna")?.Vrijednost?.ToString();
                string? fiscalTime = response.Odgovori?.FirstOrDefault(x => x.Naziv == "VrijemeFiskalnogRacuna")?.Vrijednost?.ToString();

                if (string.IsNullOrWhiteSpace(fiscalNumber))
                {
                    return new FederationFiscalResponse
                    {
                        Fiscalized = false,
                        Printed = true,
                        ErrorMessage = "Tring je vratio OK odgovor za reklamaciju, ali broj reklamiranog fiskalnog računa nije pronađen u odgovoru.",
                        RawResponse = response
                    };
                }

                DateTime? fiscalDateTime = DateTime.Now;

                if (!string.IsNullOrWhiteSpace(fiscalDate) && !string.IsNullOrWhiteSpace(fiscalTime))
                {
                    string value = $"{fiscalDate} {fiscalTime}";

                    if (DateTime.TryParseExact(value, "d.M.yy H:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime parsed))
                        fiscalDateTime = parsed;
                    else
                        Debug.WriteLine($"[FEDERACIJA/TRING REFUND] Datum/vrijeme nije moguće parsirati: {value}. Koristi se lokalno vrijeme {fiscalDateTime:dd.MM.yyyy HH:mm:ss}.");
                }
                else
                {
                    Debug.WriteLine($"[FEDERACIJA/TRING REFUND] Tring nije vratio datum/vrijeme. Koristi se lokalno vrijeme {fiscalDateTime:dd.MM.yyyy HH:mm:ss}.");
                }

                Debug.WriteLine("[FEDERACIJA/TRING REFUND] Broj reklamiranog fiskalnog računa: " + fiscalNumber);
                Debug.WriteLine("[FEDERACIJA/TRING REFUND] Datum reklamiranog fiskalnog računa: " + (fiscalDateTime?.ToString("dd.MM.yyyy HH:mm") ?? "nije dostupan"));

                return new FederationFiscalResponse
                {
                    Fiscalized = true,
                    Printed = true,
                    FiscalReceiptNumber = fiscalNumber,
                    FiscalDateTime = fiscalDateTime,
                    RawResponse = response
                };
            }
            catch (Exception ex)
            {
                return new FederationFiscalResponse
                {
                    Fiscalized = false,
                    Printed = false,
                    ErrorMessage = ex.Message
                };
            }
        }

    }
}
