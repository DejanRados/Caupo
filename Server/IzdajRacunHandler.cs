using Caupo.Data;
using Caupo.Fiscal;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;

namespace Caupo.Server
{
    public class IzdajRacunHandler : ICommandHandler
    {
        public async Task<string> HandleAsync(Dictionary<string, string> request, ClientSession session)
        {
            Debug.WriteLine ("------------- RACUN_IZDAJ DOBIO -----------------------------");

            try
            {
                // 1) DEBUG: ispiši ključeve i prvih 200 znakova vrijednosti
                Debug.WriteLine ("Request keys: " + string.Join (", ", request.Keys));
                foreach(var kv in request)
                {
                    var val = kv.Value ?? "<null>";
                    Debug.WriteLine ($"Key='{kv.Key}' Len={val.Length} Preview='{(val.Length > 200 ? val.Substring (0, 200) + "..." : val)}'");
                }

                // 2) Parsiranje "parameters" ako postoji
                if(request.TryGetValue ("parameters", out var parametersJson) && !string.IsNullOrWhiteSpace (parametersJson))
                {
                    try
                    {
                        var parsed = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
                        using var doc = JsonDocument.Parse (parametersJson);
                        if(doc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            foreach(var prop in doc.RootElement.EnumerateObject ())
                            {
                                string valueStr = prop.Value.ValueKind == JsonValueKind.String
                                    ? prop.Value.GetString () ?? ""
                                    : prop.Value.GetRawText ();
                                parsed[prop.Name] = valueStr;
                            }
                            request = parsed;
                        }
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine ("Greška pri parsiranju 'parameters': " + ex);
                        return Error ("Ne mogu parsirati parameters objekt: " + ex.Message);
                    }
                }

                // 3) Provjera obaveznih parametara
                if(!request.TryGetValue ("stavke", out var stavkeJson) || string.IsNullOrWhiteSpace (stavkeJson))
                    return Error ("Nisu poslane stavke.");

                if(!request.TryGetValue ("userId", out var userId) || string.IsNullOrWhiteSpace (userId))
                    return Error ("Nije poslan korisnik.");

                // 4) Postavi ulogovanog korisnika
                using(var context = new AppDbContext ())
                {
                    Globals.ulogovaniKorisnik = context.Radnici
                        .FirstOrDefault (r => r.Radnik == userId);
                }

                // 5) Deserializiraj stavke
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                ObservableCollection<FiskalniRacun.Item> stavke;
                try
                {
                    stavke = JsonSerializer.Deserialize<ObservableCollection<FiskalniRacun.Item>> (stavkeJson, jsonOptions)
                             ?? new ObservableCollection<FiskalniRacun.Item> ();
                }
                catch(Exception ex)
                {
                    Debug.WriteLine ("Greška pri deserializaciji stavki: " + ex);
                    return Error ("Greška pri čitanju stavki: " + ex.Message);
                }

                if(stavke.Count == 0)
                    return Error ("Lista stavki je prazna.");

                // 6) Nacin placanja (opcionalno)
                int SelectedNacinPlacanjaIndex = 0;
                if(request.TryGetValue ("nacinPlacanja", out var nacinPlacanjaStr) && !string.IsNullOrWhiteSpace (nacinPlacanjaStr))
                {
                    if(!int.TryParse (nacinPlacanjaStr, out SelectedNacinPlacanjaIndex))
                    {
                        Debug.WriteLine ("Ne mogu parsirati nacinPlacanja, koristim 0.");
                        SelectedNacinPlacanjaIndex = 0;
                    }
                }

                // 7) Izračun total-a
                decimal total = stavke.Sum (s => (decimal?)((s.Quantity ?? 0) * (s.UnitPrice ?? 0)) ?? 0);

                // 8) Dohvati kupca (po default-u)
                DatabaseTables.TblKupci kupac;
                using(var db = new AppDbContext ())
                {
                    kupac = db.Kupci.FirstOrDefault () ?? new DatabaseTables.TblKupci ();
                }

                // 9) Pozovi metodu za izdavanje računa
                FiskalniRacun fiskalniRacun = new FiskalniRacun ();
                bool success = await fiskalniRacun.IzdajFiskalniRacun (
                    "Training", "Sale", null, null, stavke, kupac, SelectedNacinPlacanjaIndex, total);

                if(success)
                {
                    return JsonSerializer.Serialize (new
                    {
                        Status = "OK",
                        Data = new
                        {
                            success = true,
                            message = "Račun uspješno izdat."
                        }
                    });
                }
                else
                {
                    return JsonSerializer.Serialize (new
                    {
                        Status = "OK",
                        Data = new
                        {
                            success = false,
                            message = "Greška pri izdavanju računa."
                        }
                    });
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine ("Unhandled exception u RACUN_IZDAJ handleru: " + ex);
                return JsonSerializer.Serialize (new
                {
                    Status = "OK",
                    Data = new
                    {
                        success = false,
                        message = "Greška pri izdavanju računa: " + ex.Message
                    }
                });
            }
        }

        private string Error(string msg)
        {
            return JsonSerializer.Serialize (new
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
