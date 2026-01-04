using Caupo.Data;
using Caupo.Fiscal;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Caupo.Server
{
    public class IzdajRacunHandler : ICommandHandler
    {


        public async Task<string> HandleAsync(Dictionary<string, string> request)
        {
            Debug.WriteLine("-------------   RACUN_IZDAJ DOBIO -----------------------------");

            try
            {
                // 1) DEBUG: ispiši ključeve i prvih 200 znakova vrijednosti da vidiš točno što dolazi
                Debug.WriteLine("Request keys: " + string.Join(", ", request.Keys));
                foreach (var kv in request)
                {
                    var val = kv.Value ?? "<null>";
                    Debug.WriteLine($"Key='{kv.Key}' Len={val.Length} Preview='{(val.Length > 200 ? val.Substring(0, 200) + "..." : val)}'");
                }

                // 2) Ako je request root koji sadrži "parameters" (rijeđi slučaj) - parsiraj ga u Dictionary<string,string>
                if (request.TryGetValue("parameters", out var parametersJson) && !string.IsNullOrWhiteSpace(parametersJson))
                {
                    Debug.WriteLine("Parameters ključ pronađen, pokušavam parsirati...");
                    try
                    {
                        // parametersJson može biti JSON object -> parsiramo ga i mapiramo sve property-e u string vrijednosti
                        var parsed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        using (var doc = JsonDocument.Parse(parametersJson))
                        {
                            if (doc.RootElement.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var prop in doc.RootElement.EnumerateObject())
                                {
                                    string valueStr;
                                    if (prop.Value.ValueKind == JsonValueKind.String)
                                        valueStr = prop.Value.GetString() ?? "";
                                    else
                                        valueStr = prop.Value.GetRawText(); // npr. array -> raw JSON
                                    parsed[prop.Name] = valueStr;
                                }

                                // zamijeni request sa parsed parametrima
                                request = parsed;
                                Debug.WriteLine("Parsed parameters keys: " + string.Join(", ", request.Keys));
                            }
                            else
                            {
                                Debug.WriteLine("Parameters nije JSON objekt (ValueKind=" + doc.RootElement.ValueKind + ")");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Greška pri parsiranju 'parameters': " + ex);
                        return Error("Ne mogu parsirati parameters objekt: " + ex.Message);
                    }
                }
                else
                {
                    Debug.WriteLine("Nema zasebnog 'parameters' ključa - koristim request direktno kao parametre.");
                }

                // 3) Sada očekujemo da request sadrži ključ 'stavke' i 'userId' (ravno u dictionaryju)
                if (!request.TryGetValue("stavke", out var stavkeJson) || string.IsNullOrWhiteSpace(stavkeJson))
                    return Error("Nisu poslane stavke.");

                if (!request.TryGetValue("userId", out var userId) || string.IsNullOrWhiteSpace(userId))
                    return Error("Nije poslan korisnik.");

                // 4) Postavimo ulogovanog korisnika
                using (var context = new AppDbContext())
                {
                    Globals.ulogovaniKorisnik = context.Radnici
                        .FirstOrDefault(r => r.Radnik == userId);
                }

                // 5) Deserializiraj stavke (case-insensitive radi sigurnosti)
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                ObservableCollection<FiskalniRacun.Item> stavke;
                try
                {
                    stavke = JsonSerializer.Deserialize<ObservableCollection<FiskalniRacun.Item>>(stavkeJson, jsonOptions)
                             ?? new ObservableCollection<FiskalniRacun.Item>();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Greška pri deserializaciji stavki: " + ex);
                    return Error("Greška pri čitanju stavki: " + ex.Message);
                }

                if (stavke.Count == 0)
                    return Error("Lista stavki je prazna.");

                // 6) Nacin placanja (opcionalno)
                int SelectedNacinPlacanjaIndex = 0;
                if (request.TryGetValue("nacinPlacanja", out var nacinPlacanjaStr) && !string.IsNullOrWhiteSpace(nacinPlacanjaStr))
                {
                    if (!int.TryParse(nacinPlacanjaStr, out SelectedNacinPlacanjaIndex))
                    {
                        Debug.WriteLine("Ne mogu parsirati nacinPlacanja, koristim 0.");
                        SelectedNacinPlacanjaIndex = 0;
                    }
                }

                // 7) Izračun total-a (sigurno, jer Quantity i UnitPrice mogu biti null)
                decimal total = stavke.Sum(s => (decimal?)((s.Quantity ?? 0) * (s.UnitPrice ?? 0)) ?? 0);

                DatabaseTables.TblKupci kupac = new DatabaseTables.TblKupci();
                using (var db = new AppDbContext ())
                {
                    kupac = db.Kupci.FirstOrDefault ();
                }

               

                // 8) Pozovi postojeću metodu za izdavanje računa
                FiskalniRacun fiskalniRacun = new FiskalniRacun();
                bool success = await fiskalniRacun.IzdajFiskalniRacun("Training","Sale", null, null, stavke, kupac, SelectedNacinPlacanjaIndex, total);

                if (success)
                {
                    return JsonSerializer.Serialize(new { success = true, message = "Račun uspješno izdat." });
                }
                else
                {
                    return JsonSerializer.Serialize(new { success = false, message = "Greška pri izdavanju računa." });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unhandled exception u RACUN_IZDAJ handleru: " + ex);
                return Error("Greška pri izdavanju računa: " + ex.Message);
            }
        }



        private string Error(string msg)
        {
            return JsonSerializer.Serialize(new { success = false, message = msg });
        }

    }

}
