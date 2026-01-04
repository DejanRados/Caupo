using Caupo.Data;
using Caupo.Models;
using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Caupo.Services
{
    public class KnjigaSankaService
    {
        private readonly AppDbContext _db;


        public KnjigaSankaService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<StavkaKnjigeSanka>> GetKnjigaZaDanAsync(DateTime datum)
        {
            Debug.WriteLine("--------------  trigerovan public async Task<List<StavkaKnjigeSanka>> GetKnjigaZaDanAsync(DateTime datum) --------------------");
            var knjiga = new List<StavkaKnjigeSanka>();

            // 1. Svi artikli koji su piće (proizvod = 0)
            var artikli = await _db.Artikli
                .Where(a => a.VrstaArtikla == 0)
                .ToListAsync();

            Debug.WriteLine("---------------- Artikli u listi -----------------------");
            Debug.WriteLine(artikli.Count);
            int rednibroj = 1;
            foreach (var art in artikli)
            {
                Debug.WriteLine("---------------- Dodaje u knjigu  -----------------------");
                Debug.WriteLine(art.Artikl);

                knjiga.Add(new StavkaKnjigeSanka
                {
                    RedniBroj = rednibroj,
                    Naziv = art.Artikl,
                    JedinicaMjere = GetJmName(art.JedinicaMjere ?? 0),
                    OstatakOdJuce = 0m,
                    NabavljenoDanas = 0m,
                    NabavljenoDoDanas = 0m,
                    NaStanju = 0m,
                    OstatakZaSutra = 0m,
                    UtrosenoDanas = 0m,
                    UtrosenoDoDanas = 0m,
                    ReklamiranoDoDanas = 0m,
                    Dobavljac = "",
                    Dokument = "",
                    Promet = 0m,
                    Normativ = art.Normativ,
                    Cijena = art.Cijena / art.Normativ ?? 0m
                });
                rednibroj++;
            }

            // 2. Ukupni ulaz do datuma
            var ulazDo = await (from u in _db.Ulaz
                                join s in _db.UlazStavke on u.BrojUlaza equals s.BrojUlaza
                                where u.Datum < datum
                                group s by s.Artikl into g
                                select new { Naziv = g.Key, Kolicina = g.Sum(x => x.Kolicina) })
                                .ToListAsync();

            foreach (var r in ulazDo)
            {
                var stavka = knjiga.FirstOrDefault(x => x.Naziv == r.Naziv);
                Debug.WriteLine("---------------- Racuna ulaz do datuma za-----------------------");
                if (stavka != null) stavka.NabavljenoDoDanas += r.Kolicina;
            }

            Debug.WriteLine("---------------- Krece izlaz do datuma za-----------------------");
            // 3. Ukupni izlaz do datuma
            var izlazDo = await (from i in _db.Racuni
                                 join s in _db.RacunStavka on i.BrojRacuna equals s.BrojRacuna
                                 where i.Datum < datum && s.VrstaArtikla == 0
                                 group s by s.Artikl into g
                                 select new { Naziv = g.Key, Kolicina = g.Sum(x => x.Kolicina) })
                                 .ToListAsync();

            Debug.WriteLine("---------------- Krece foreach (var r in izlazDo)-----------------------");
            foreach (var r in izlazDo)
            {
                var stavka = knjiga.FirstOrDefault(x => x.Naziv == r.Naziv);
                Debug.WriteLine("---------------- Racuna izlaz do datuma za-----------------------");
                Debug.WriteLine("---------------- Racuna utrosak danas-----------------------");
                if (stavka != null)
                {
                    decimal normativ = stavka.Normativ ?? 0m;
                    stavka.UtrosenoDoDanas += (r.Kolicina ?? 0m) * normativ;
                    Debug.WriteLine("Utrosak danas , stavka: " + stavka.Naziv + " , normativ = " + stavka.Normativ + ", otroseno = " + stavka.UtrosenoDoDanas);
                }
            }

            Debug.WriteLine("----------------   // 4. Reklamacije do datuma-----------------------");
            // 4. Dohvati sve reklamirane stavke

            try
            {
                var reklDo = await (from r in _db.Racuni
                                    join s in _db.RacunStavka
                                        on r.BrojRacuna equals s.BrojRacuna
                                    where r.Datum < datum && r.Reklamiran == "DA"
                                    group s by s.Artikl into g
                                    select new { Naziv = g.Key, Kolicina = g.Sum(x => x.Kolicina) })
                                  .ToListAsync();
                Debug.WriteLine("Uspješno dohvaćeno reklDo: " + reklDo.Count);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception u reklDo upitu: " + ex);
            }

            Debug.WriteLine("----------------   //   // 5. Izračun od juče -----------------------");
            foreach (var s in knjiga)
            {
                s.OstatakOdJuce = s.NabavljenoDoDanas - s.UtrosenoDoDanas + s.ReklamiranoDoDanas;
            }

            Debug.WriteLine("----------------     // 6. Danas ulaz-----------------------");
            var ulazDanas = await (from u in _db.Ulaz
                                   join s in _db.UlazStavke on u.BrojUlaza equals s.BrojUlaza
                                   where u.Datum.Date == datum.Date
                                   group s by s.Artikl into g
                                   select new { Naziv = g.Key, Kolicina = g.Sum(x => x.Kolicina) })
                                   .ToListAsync();

            foreach (var r in ulazDanas)
            {
                var stavka = knjiga.FirstOrDefault(x => x.Naziv == r.Naziv);
                Debug.WriteLine("---------------- Racuna ulaz za danas-----------------------");
                if (stavka != null) stavka.NabavljenoDanas += r.Kolicina;
            }

            Debug.WriteLine("----------------      // 7. Na stanju-----------------------");
            foreach (var s in knjiga)
            {

                s.NaStanju = s.OstatakOdJuce + s.NabavljenoDanas;
            }

            Debug.WriteLine("----------------     // 8. Danas utrošeno-----------------------");
            var izlazDanas = await (from i in _db.Racuni
                                    join s in _db.RacunStavka on i.BrojRacuna equals s.BrojRacuna
                                    where i.Datum.Date == datum.Date && s.VrstaArtikla == 0
                                    group s by s.Artikl into g
                                    select new { Naziv = g.Key, Kolicina = g.Sum(x => x.Kolicina) })
                                    .ToListAsync();

            foreach (var r in izlazDanas)
            {
                var stavka = knjiga.FirstOrDefault(x => x.Naziv == r.Naziv);

                Debug.WriteLine("---------------- Racuna utrosak danas-----------------------");
                if (stavka != null)
                {
                    decimal normativ = stavka.Normativ ?? 0m;
                    stavka.UtrosenoDanas += (r.Kolicina ?? 0m) * normativ;
                    Debug.WriteLine("Utrosak danas , stavka: " + stavka.Naziv + " , normativ = " + stavka.Normativ + ", otroseno = " + stavka.UtrosenoDanas);
                }

            }

            Debug.WriteLine("----------------      // 9. Reklamacije danas-----------------------");
            Debug.WriteLine ("----------------      // 9. Reklamacije danas-----------------------");
            Debug.WriteLine ("----------------      // 9. Reklamacije danas-----------------------");
            Debug.WriteLine ("----------------  -----------------------");
            Debug.WriteLine ("----------------    -----------------------");
            Debug.WriteLine ("----------------    -----------------------");
            Debug.WriteLine ("----------------      -----------------------");
            var reklDanas = await (from r in _db.Racuni
                                   join s in _db.RacunStavka on r.BrojRacuna equals s.BrojRacuna
                                   where r.Datum.Date == datum.Date && r.Reklamiran == "DA"
                                   group s by s.Artikl into g
                                   select new { Naziv = g.Key, Kolicina = g.Sum(x => x.Kolicina) })
                                   .ToListAsync();

            foreach (var r in reklDanas)
            {
                var stavka = knjiga.FirstOrDefault(x => x.Naziv == r.Naziv);
                if (stavka != null)
                {
                    decimal normativ = stavka.Normativ ?? 0m;
                    stavka.UtrosenoDanas -= (r.Kolicina ?? 0m) * normativ;
                   
                    Debug.WriteLine ("Umanjuje stanje " + stavka.Naziv + " za " + (r.Kolicina ?? 0m));
                }

            }

            Debug.WriteLine("----------------       // 10. Ostatak i promet-----------------------");
            foreach (var s in knjiga)
            {
                s.OstatakZaSutra = s.NaStanju - s.UtrosenoDanas;
                s.Promet = s.UtrosenoDanas * s.Cijena;
                if (s.Promet > 0)
                {
                    s.IsPromet = true;
                }
            }

            Debug.WriteLine("------------------------ Knjiga Sanka ----------------------------");
            Debug.WriteLine(knjiga.Count);
            Debug.WriteLine("------------------------ Knjiga Sanka ----------------------------");

            return knjiga;
        }

        private string GetJmName(int jm)
        {
            return jm switch
            {
                1 => "kom",
                2 => "kg",
                3 => "m",
                4 => "m2",
                5 => "m3",
                6 => "lit",
                7 => "tona",
                8 => "g",
                9 => "por",
                10 => "pak",
                _ => ""
            };
        }
    }

}
