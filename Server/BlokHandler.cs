using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Caupo.Data;
using Caupo.Fiscal;
using Caupo.Models;

namespace Caupo.Server
{
    public class BlokHandler : ICommandHandler
    {


        public async Task<string> HandleAsync(Dictionary<string, string> request)
        {
            Debug.WriteLine("-------------   BlokHandler DOBIO -----------------------------");

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
                List<FiskalniRacun.Item> stavkeSank = new List<FiskalniRacun.Item>();
                List<FiskalniRacun.Item> stavkeKuhinja = new List<FiskalniRacun.Item>();
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

            foreach(var stavka in stavke)
                {
                    Debug.WriteLine("Stavka proizvod : " + stavka.Proizvod);
                    if (stavka.Proizvod == 0)
                    {
                        stavkeSank.Add(stavka);
                        Debug.WriteLine("Dodaje u stavkeSank: " + stavka);
                    }
                    else if(stavka.Proizvod == 1)
                    {
                        stavkeKuhinja.Add(stavka);
                        Debug.WriteLine("Dodaje u stavkeKuhinja: " + stavka);
                    }

                }
                Debug.WriteLine($"Kuhinja count: {stavkeKuhinja.Count}");
                Debug.WriteLine($"Sank count: {stavkeSank.Count}");
                Debug.WriteLine($"Kuhinja printer: {Properties.Settings.Default.KuhinjaPrinter}");
                Debug.WriteLine($"Sank printer: {Properties.Settings.Default.SankPrinter}");
                // 7) Izračun total-a (sigurno, jer Quantity i UnitPrice mogu biti null)
                decimal total = stavke.Sum(s => (decimal?)((s.Quantity ?? 0) * (s.UnitPrice ?? 0)) ?? 0);
                if (!string.IsNullOrEmpty(Properties.Settings.Default.KuhinjaPrinter))
                {
                    if (stavkeKuhinja.Count > 0)
                    {
                        BlokPrinter blok = new BlokPrinter(stavkeKuhinja, "Kuhinja", userId, "Android");
                        await blok.Print();
                    }
                }

                if (!string.IsNullOrEmpty(Properties.Settings.Default.SankPrinter))
                {
                    if (stavkeSank.Count > 0)
                    {
                        BlokPrinter blok = new BlokPrinter(stavkeSank, "Sank", userId, "Android");
                        await blok.Print();
                    }
                }
              

                    return JsonSerializer.Serialize(new { success = true, message = "Blok poslat na štampanje." });
              
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unhandled exception u Blok handleru: " + ex.Message);
                return Error("Greška pri štampanju bloka: " + ex.Message);
            }
        }



        private string Error(string msg)
        {
            return JsonSerializer.Serialize(new { success = false, message = msg });
        }

    }

}
