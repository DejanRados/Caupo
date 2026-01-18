using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Caupo.Data;
using Caupo.Fiscal;
using Caupo.Models;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Server
{
    public class BlokHandler : ICommandHandler
    {
        public async Task<string> HandleAsync(
            Dictionary<string, string> request,
            ClientSession session)
        {
            try
            {
                Debug.WriteLine ("------------- BlokHandler DOBIO -----------------------------");

                // Parsiranje parameters ako postoji
                if(request.TryGetValue ("parameters", out var parametersJson) &&
                    !string.IsNullOrWhiteSpace (parametersJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse (parametersJson);
                        if(doc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            foreach(var prop in doc.RootElement.EnumerateObject ())
                            {
                                request[prop.Name] = prop.Value.ValueKind == JsonValueKind.String
                                    ? prop.Value.GetString () ?? ""
                                    : prop.Value.GetRawText ();
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine ("Greška pri parsiranju parameters: " + ex);
                        return Error ("Ne mogu parsirati parameters objekt: " + ex.Message);
                    }
                }

                // Validacija
                if(!request.TryGetValue ("stavke", out var stavkeJson) || string.IsNullOrWhiteSpace (stavkeJson))
                    return Error ("Nisu poslane stavke.");

                if(!request.TryGetValue ("userId", out var userId) || string.IsNullOrWhiteSpace (userId))
                    return Error ("Nije poslan korisnik.");

                string source = request.TryGetValue ("source", out var src) ? src : "Unknown";

                // Dohvat korisnika (LOKALNO)
                TblRadnici korisnik;
                using(var context = new AppDbContext ())
                {
                    korisnik = context.Radnici.FirstOrDefault (r => r.Radnik == userId);
                    Debug.WriteLine ("[Server] Radnik: " + korisnik.Radnik);
                    Debug.WriteLine ("[Server] UserId: " + userId);
                }

                if(korisnik == null)
                    return Error ("Korisnik ne postoji.");
                Globals.ulogovaniKorisnik = korisnik;
                // Deserializacija stavki
                ObservableCollection<FiskalniRacun.Item> stavke;
                try
                {
                    stavke = JsonSerializer.Deserialize<ObservableCollection<FiskalniRacun.Item>> (
                        stavkeJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    ) ?? new ObservableCollection<FiskalniRacun.Item> ();
                }
                catch(Exception ex)
                {
                    Debug.WriteLine ("Greška pri deserializaciji stavki: " + ex);
                    return Error ("Greška pri čitanju stavki: " + ex.Message);
                }

                if(stavke.Count == 0)
                    return Error ("Lista stavki je prazna.");

                // Grupiranje
                var stavkeSank = new List<FiskalniRacun.Item> ();
                var stavkeKuhinja = new List<FiskalniRacun.Item> ();

                foreach(var stavka in stavke)
                {
                    switch(stavka.Proizvod)
                    {
                        case 0:
                            stavkeSank.Add (stavka);
                            break;
                        case 1:
                            stavkeKuhinja.Add (stavka);
                            break;
                        default:
                            Debug.WriteLine ($"Nepoznat tip proizvoda: {stavka.Proizvod}");
                            break;
                    }
                }

                // Štampa
            
                    if(!string.IsNullOrWhiteSpace (Properties.Settings.Default.KuhinjaPrinter) && stavkeKuhinja.Any ())
                    {
                        var blok = new BlokPrinter (stavkeKuhinja, "Kuhinja", userId, source);
                        await blok.Print ();
                    }

                    if(!string.IsNullOrWhiteSpace (Properties.Settings.Default.SankPrinter) &&  stavkeSank.Any ())
                    {
                        var blok = new BlokPrinter (stavkeSank, "Sank", userId, source);
                        await blok.Print ();

                    }
                


                return Ok ("Blok poslat na štampanje.");
            }
            catch(Exception ex)
            {
                Debug.WriteLine ("Unhandled exception u Blok handleru: " + ex);
                return Error ("Greška pri štampanju bloka: " + ex.Message);
            }
        }


        private int GetBrojKopijaBloka()
        {
            if(!int.TryParse (Properties.Settings.Default.BlokKopija, out int kopije))
                return 1; // fallback

            // ograničenje 0–5
            return Math.Clamp (kopije, 0, 5);
        }
        private string Ok(string msg)
        {
            return JsonSerializer.Serialize (new
            {
                Status = "OK",
                Data = new
                {
                    success = true,
                    message = msg
                }
            });
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
