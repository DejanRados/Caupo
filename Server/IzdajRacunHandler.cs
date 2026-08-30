using Caupo.Data;
using Caupo.Fiscal;
using Caupo.Fiscal.Common;
using Caupo.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;

namespace Caupo.Server
{
    public class IzdajRacunHandler : ICommandHandler
    {
        public async Task<string> HandleAsync(
            Dictionary<string, string> request,
            ClientSession session)
        {
            Debug.WriteLine (
                "------------- RACUN_IZDAJ DOBIO -----------------------------");

            try
            {
                // =====================================================
                // 1. DEBUG
                // =====================================================

                Debug.WriteLine (
                    "Request keys: " +
                    string.Join (", ", request.Keys));

                foreach(var kv in request)
                {
                    var val =
                        kv.Value ?? "<null>";

                    Debug.WriteLine (
                        $"Key='{kv.Key}' " +
                        $"Len={val.Length} " +
                        $"Preview='" +
                        $"{(val.Length > 200
                            ? val.Substring (0, 200) + "..."
                            : val)}'");
                }


                // =====================================================
                // 2. PARSIRANJE PARAMETERS
                // =====================================================

                if(request.TryGetValue (
                       "parameters",
                       out var parametersJson) &&
                   !string.IsNullOrWhiteSpace (
                       parametersJson))
                {
                    try
                    {
                        var parsed =
                            new Dictionary<string, string> (
                                StringComparer.OrdinalIgnoreCase);

                        using var doc =
                            JsonDocument.Parse (
                                parametersJson);

                        if(doc.RootElement.ValueKind ==
                           JsonValueKind.Object)
                        {
                            foreach(var prop in
                                    doc.RootElement
                                        .EnumerateObject ())
                            {
                                string valueStr =
                                    prop.Value.ValueKind ==
                                    JsonValueKind.String
                                        ? prop.Value.GetString ()
                                          ?? ""
                                        : prop.Value.GetRawText ();

                                parsed[prop.Name] =
                                    valueStr;
                            }

                            request = parsed;
                        }
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine (
                            "Greška pri parsiranju 'parameters': " +
                            ex);

                        return Error (
                            "Ne mogu parsirati parameters objekt: " +
                            ex.Message);
                    }
                }


                // =====================================================
                // 3. OBAVEZNI PARAMETRI
                // =====================================================

                if(!request.TryGetValue (
                       "stavke",
                       out var stavkeJson) ||
                   string.IsNullOrWhiteSpace (
                       stavkeJson))
                {
                    return Error (
                        "Nisu poslane stavke.");
                }

                if(!request.TryGetValue (
                       "userId",
                       out var userId) ||
                   string.IsNullOrWhiteSpace (
                       userId))
                {
                    return Error (
                        "Nije poslan korisnik.");
                }


                // =====================================================
                // 4. ULOGOVANI KORISNIK
                // =====================================================

                using(var context =
                      new AppDbContext ())
                {
                    Globals.ulogovaniKorisnik =
                        context.Radnici
                            .FirstOrDefault (
                                r => r.Radnik ==
                                     userId);
                }

                if(Globals.ulogovaniKorisnik ==
                   null)
                {
                    return Error (
                        $"Korisnik '{userId}' nije pronađen.");
                }


                // =====================================================
                // 5. STAVKE
                // =====================================================

                var jsonOptions =
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive =
                            true
                    };

                ObservableCollection<RacunStavka>
                    stavke;

                try
                {
                    stavke =
                        JsonSerializer.Deserialize<
                            ObservableCollection<
                                RacunStavka>> (
                                    stavkeJson,
                                    jsonOptions)
                        ?? new ObservableCollection<
                            RacunStavka> ();
                }
                catch(Exception ex)
                {
                    Debug.WriteLine (
                        "Greška pri deserializaciji stavki: " +
                        ex);

                    return Error (
                        "Greška pri čitanju stavki: " +
                        ex.Message);
                }

                if(stavke.Count == 0)
                {
                    return Error (
                        "Lista stavki je prazna.");
                }


                // =====================================================
                // 6. NAČIN PLAĆANJA
                // =====================================================

                int selectedNacinPlacanjaIndex =
                    0;

                if(request.TryGetValue (
                       "nacinPlacanja",
                       out var nacinPlacanjaStr) &&
                   !string.IsNullOrWhiteSpace (
                       nacinPlacanjaStr))
                {
                    if(!int.TryParse (
                           nacinPlacanjaStr,
                           out selectedNacinPlacanjaIndex))
                    {
                        Debug.WriteLine (
                            "Ne mogu parsirati nacinPlacanja, koristim 0.");

                        selectedNacinPlacanjaIndex =
                            0;
                    }
                }

                FiscalPaymentType paymentType =
                    selectedNacinPlacanjaIndex switch
                    {
                        0 => FiscalPaymentType.Cash,
                        1 => FiscalPaymentType.Card,
                        2 => FiscalPaymentType.Check,
                        3 => FiscalPaymentType.WireTransfer,

                        _ => throw new FiscalException (
                            "Nepoznat način plaćanja.")
                    };


                // =====================================================
                // 7. UKUPAN IZNOS
                // =====================================================

                decimal total =
                    Math.Round (
                        stavke.Sum (
                            s =>
                                (s.Quantity ?? 0m) *
                                (s.UnitPrice ?? 0m)),
                        2,
                        MidpointRounding.AwayFromZero);


                // =====================================================
                // 8. KUPAC
                // =====================================================

                DatabaseTables.TblKupci?
                    kupac;

                using(var db =
                      new AppDbContext ())
                {
                    kupac =
                        db.Kupci
                            .FirstOrDefault ();
                }

                FiscalBuyer? fiscalBuyer =
                    null;

                if(kupac != null)
                {
                    fiscalBuyer =
                        new FiscalBuyer
                        {
                            Name =
                                kupac.Kupac,

                            TaxId =
                                kupac.JIB,

                            Address =
                                kupac.Adresa,

                            City =
                                kupac.Mjesto
                        };
                }


                // =====================================================
                // 9. NEUTRALNI FISCAL REQUEST
                // =====================================================

                var fiscalRequest =
                    new FiscalRequest
                    {
                        Items =
                            stavke.ToList (),

                        Buyer =
                            fiscalBuyer,

                        Cashier =
                            new FiscalCashier
                            {
                                Id =
                                    Globals
                                        .ulogovaniKorisnik
                                        .IdRadnika,

                                Name =
                                    Globals
                                        .ulogovaniKorisnik
                                        .Radnik,

                                IdentificationNumber =
                                    Globals
                                        .ulogovaniKorisnik
                                        .IB
                            },

                        PaymentType =
                            paymentType,

                        TotalAmount =
                            total,

                        InvoiceType =
                            "Normal",

                        TransactionType =
                            "Sale"
                    };


                // =====================================================
                // 10. ODABIR REGIONALNOG SERVISA
                // =====================================================

                IFiscalService fiscalService =
                    FiscalServiceFactory.Create (
                        Properties.Settings
                            .Default.Country);


                // =====================================================
                // 11. IZDAVANJE RAČUNA
                // =====================================================

                FiscalResult result =
                    await fiscalService
                        .IzdajRacunAsync (
                            fiscalRequest);


                Debug.WriteLine (
                    $"[RACUN_IZDAJ] " +
                    $"Success={result.Success}, " +
                    $"Fiscalized={result.Fiscalized}, " +
                    $"Saved={result.SavedToDatabase}, " +
                    $"Printed={result.Printed}, " +
                    $"FiscalNumber={result.FiscalNumber}");


                // =====================================================
                // 12. ODGOVOR KLIJENTU
                // =====================================================

                if(result.Success)
                {
                    return JsonSerializer.Serialize (
                        new
                        {
                            Status = "OK",

                            Data = new
                            {
                                success = true,

                                fiscalized =
                                    result.Fiscalized,

                                savedToDatabase =
                                    result.SavedToDatabase,

                                printed =
                                    result.Printed,

                                localReceiptNumber =
                                    result.LocalReceiptNumber,

                                receiptNumber =
                                    result.ReceiptNumber,

                                fiscalNumber =
                                    result.FiscalNumber,

                                message =
                                    string.IsNullOrWhiteSpace (
                                        result.ErrorMessage)
                                        ? "Račun uspješno izdat."
                                        : result.ErrorMessage
                            }
                        });
                }


                return JsonSerializer.Serialize (
                    new
                    {
                        Status = "OK",

                        Data = new
                        {
                            success = false,

                            fiscalized =
                                result.Fiscalized,

                            savedToDatabase =
                                result.SavedToDatabase,

                            printed =
                                result.Printed,

                            message =
                                result.ErrorMessage
                                ?? "Greška pri izdavanju računa."
                        }
                    });
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    "Unhandled exception u RACUN_IZDAJ handleru: " +
                    ex);

                return Error (
                    "Greška pri izdavanju računa: " +
                    ex.Message);
            }
        }


        private static string Error(
            string msg)
        {
            return JsonSerializer.Serialize (
                new
                {
                    Status = "OK",

                    Data = new
                    {
                        success = false,
                        message = msg
                    }
                });
        }
    }
}