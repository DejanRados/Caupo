using Caupo.Data;
using Caupo.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Caupo.Services
{
    public class KnjigaKuhinjeService
    {
        private readonly AppDbContext _db;

        public KnjigaKuhinjeService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<StavkaKnjigeKuhinje>> GetKnjigaZaDanAsync(DateTime datum)
        {

            var knjiga = new List<StavkaKnjigeKuhinje> ();
            try
            {


                // 1. Svi repromaterijali (namirnice)
                var namirnice = await _db.Repromaterijal.ToListAsync ();

                int redniBroj = 1;
                foreach(var n in namirnice)
                {

                    knjiga.Add (new StavkaKnjigeKuhinje
                    {
                        RedniBroj = redniBroj++,
                        Namirnica = n.Repromaterijal,
                        JedinicaMjere = GetJmName (n.JedinicaMjere),
                        OstatakOdJuce = 0m,
                        NabavljenoDanas = 0m,
                        NabavljenoDoDanas = 0m,
                        NaStanju = 0m,
                        OstatakZaSutra = 0m,
                        UtrosenoDanas = 0m,
                        UtrosenoDoDanas = 0m,
                        ReklamiranoDoDanas = 0m,

                    });
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine ("--------------     // 1. Svi repromaterijali (namirnice) -Exception e-------------------" + e.Message);
            }

            // 2. Ukupni ulaz do datuma
            try
            {
                var ulazDo = await (from u in _db.UlazRepromaterijal
                                    join s in _db.UlazRepromaterijalStavka on u.BrojUlaza equals s.BrojUlaza
                                    where u.Datum.Date < datum.Date
                                    group s by s.Artikl into g
                                    select new { Naziv = g.Key, Kolicina = g.Sum (x => x.Kolicina) })
                                    .ToListAsync ();

                foreach(var r in ulazDo)
                {
                    var stavka = knjiga.FirstOrDefault (x => x.Namirnica == r.Naziv);
                    if(stavka != null)
                    {

                        stavka.NabavljenoDoDanas += r.Kolicina;
                        Debug.WriteLine ("--------------     Ukupni ulaz do datuma namirnica: " + stavka.Namirnica + " =" + stavka.NabavljenoDoDanas);
                    }
                }
            }
            catch(Exception x)
            {
                Debug.WriteLine ("--------------     // 2. Ukupni ulaz do datuma ---------Exception x----------" + x.Message);
            }

            // 3. Ukupni izlaz (jela) do datuma preko normativa

            try
            {
                // 1. Dohvati sumirane količine jela do datuma sa eksplicitnim provjerama NULL vrijednosti
                var izlazDo = await (from i in _db.Racuni
                                     join s in _db.RacunStavka on i.BrojRacuna equals s.BrojRacuna
                                     where i.Datum.Date < datum.Date && s.VrstaArtikla == 1
                                     && s.Artikl != null // eksplicitna provjera za NULL
                                     group s by s.Artikl into g
                                     select new
                                     {
                                         NazivJela = g.Key,
                                         Kolicina = g.Sum (x => x.Kolicina ?? 0m)
                                     }).ToListAsync ();

                foreach(var jelo in izlazDo)
                {
                    if(string.IsNullOrEmpty (jelo.NazivJela))
                        continue;

                    // 2. Pronađi Id jela iz tabele Artikli sa eksplicitnom selekcijom polja
                    var jeloArtikl = await _db.Artikli
                                              .Where (a => a.Artikl != null && a.Artikl == jelo.NazivJela && a.VrstaArtikla == 1)
                                              .Select (a => new { a.IdArtikla, a.Artikl }) // eksplicitno selektuj samo potrebna polja
                                              .FirstOrDefaultAsync ();

                    if(jeloArtikl == null)
                        continue;

                    // 3. Dohvati normativ(e) po IdProizvoda sa eksplicitnim provjerama
                    var normativi = await _db.Normativ
                                             .Where (n => n.IdProizvoda == jeloArtikl.IdArtikla && n.Repromaterijal != null)
                                             .Select (n => new { n.Repromaterijal, n.Kolicina }) // eksplicitno selektuj polja
                                             .ToListAsync ();

                    if(!normativi.Any ())
                        continue;

                    // 4. Prođi kroz normativ i primijeni količine
                    foreach(var n in normativi)
                    {
                        if(n?.Repromaterijal == null)
                            continue;

                        var stavka = knjiga.FirstOrDefault (x => x.Namirnica == n.Repromaterijal);
                        if(stavka != null)
                        {
                            decimal jeloKolicina = jelo.Kolicina;
                            decimal normativKolicina = n.Kolicina ?? 0m;

                            stavka.UtrosenoDoDanas += jeloKolicina * normativKolicina;
                            Debug.WriteLine ("-------------- Ukupni izlaz do  danas (namirnica): " + stavka.Namirnica + " =  " + stavka.UtrosenoDoDanas);
                        }
                    }
                }
            }
            catch(Exception m)
            {
                Debug.WriteLine ("-------------- Ukupni izlaz (jela) do datuma preko normativa - Exception --------- " + m.Message);
                Debug.WriteLine ("Inner exception: " + m.InnerException?.Message);
                Debug.WriteLine ("Stack trace: " + m.StackTrace);

                // Dodatna dijagnostika
                Debug.WriteLine ("Datum parametar: " + datum);
            }




            // 4. Reklamacije do datuma
            try
            {
                var reklDo = await (from r in _db.ReklamiraniRacun
                                    join s in _db.ReklamiraniStavka
                                        on r.BrojRekRacuna equals s.BrojReklamiranogRacuna
                                    where r.Datum.Date < datum.Date
                                    group s by s.Artikl into g
                                    select new { NazivJela = g.Key, Kolicina = g.Sum (x => x.Kolicina) })
                                    .ToListAsync ();

                foreach(var rekl in reklDo)
                {
                    // Pronađi Id jela iz Artikli
                    var jeloArtikl = await _db.Artikli.FirstOrDefaultAsync (a => a.Artikl == rekl.NazivJela && a.VrstaArtikla == 1);
                    if(jeloArtikl == null)
                        continue;

                    // Dohvati normativ za to jelo
                    var normativi = await _db.Normativ
                                        .Where (n => n.IdProizvoda == jeloArtikl.IdArtikla)
                                        .ToListAsync ();

                    foreach(var n in normativi)
                    {
                        var stavka = knjiga.FirstOrDefault (x => x.Namirnica == n.Repromaterijal);
                        if(stavka != null)
                            stavka.ReklamiranoDoDanas += rekl.Kolicina * n.Kolicina ?? 0m;
                    }
                }
            }
            catch(Exception g)
            {
                Debug.WriteLine ("--------------     // 4. Reklamacije do datuma ---------Exception g----------" + g.Message);
            }

            // 5. Preneseno od juče
            foreach(var s in knjiga)
            {
                s.OstatakOdJuce = s.NabavljenoDoDanas - s.UtrosenoDoDanas + s.ReklamiranoDoDanas;
            }
            // 6. Ulaz danas
            try
            {
                var ulazDanas = await (from u in _db.UlazRepromaterijal
                                       join s in _db.UlazRepromaterijalStavka
                                           on u.BrojUlaza equals s.BrojUlaza
                                       where u.Datum.Date == datum.Date
                                       select new
                                       {
                                           Artikl = s.Artikl,
                                           Kolicina = s.Kolicina,
                                           Dobavljac = u.Dobavljac,
                                           Dokument = u.BrojFakture
                                       })
                                       .ToListAsync ();


                foreach(var grupa in ulazDanas.GroupBy (x => new { x.Artikl, x.Dobavljac, x.Dokument }))
                {
                    var stavka = knjiga.FirstOrDefault (x => x.Namirnica == grupa.Key.Artikl);
                    if(stavka != null)
                    {
                        stavka.NabavljenoDanas += grupa.Sum (x => x.Kolicina);
                        stavka.Dobavljac = grupa.Key.Dobavljac;
                        stavka.Dokument = grupa.Key.Dokument;
                        Debug.WriteLine ("Nabavljeno danas  Namirnica: " + grupa.Key.Artikl + ", dobavljac: " + stavka.Dobavljac + ", dokument: " + stavka.Dokument);
                    }
                }

            }
            catch(Exception h)
            {
                Debug.WriteLine ("--------------     // 6. Ulaz danas ---------Exception h----------" + h.Message);
            }

            // 7. Na stanju
            foreach(var s in knjiga)
            {
                s.NaStanju = s.OstatakOdJuce + s.NabavljenoDanas;
            }

            // 8. Izlaz danas (jela) preko normativi

            try
            {
                // 1. Dohvati sumirane količine jela danas sa eksplicitnim provjerama NULL vrijednosti
                var izlazDanas = await (from i in _db.Racuni
                                        join s in _db.RacunStavka on i.BrojRacuna equals s.BrojRacuna
                                        where i.Datum.Date == datum.Date && s.VrstaArtikla == 1
                                        && s.Artikl != null // eksplicitna provjera za NULL
                                        group s by s.Artikl into g
                                        select new
                                        {
                                            NazivJela = g.Key,
                                            Kolicina = g.Sum (x => x.Kolicina ?? 0m)
                                        }).ToListAsync ();

                foreach(var jelo in izlazDanas)
                {
                    if(string.IsNullOrEmpty (jelo.NazivJela))
                        continue;

                    // 2. Pronađi Id jela iz tabele Artikli sa eksplicitnom selekcijom polja
                    var jeloArtikl = await _db.Artikli
                                              .Where (a => a.Artikl != null && a.Artikl == jelo.NazivJela && a.VrstaArtikla == 1)
                                              .Select (a => new { a.IdArtikla, a.Artikl }) // eksplicitno selektuj samo potrebna polja
                                              .FirstOrDefaultAsync ();

                    if(jeloArtikl == null)
                        continue;

                    // 3. Dohvati normativ(e) po IdProizvoda sa eksplicitnim provjerama
                    var normativi = await _db.Normativ
                                             .Where (n => n.IdProizvoda == jeloArtikl.IdArtikla && n.Repromaterijal != null)
                                             .Select (n => new { n.Repromaterijal, n.Kolicina }) // eksplicitno selektuj polja
                                             .ToListAsync ();

                    if(!normativi.Any ())
                        continue;

                    // 4. Prođi kroz normativ i primijeni količine
                    foreach(var n in normativi)
                    {
                        if(n?.Repromaterijal == null)
                            continue;

                        var stavka = knjiga.FirstOrDefault (x => x.Namirnica == n.Repromaterijal);
                        if(stavka != null)
                        {
                            decimal jeloKolicina = jelo.Kolicina;
                            decimal normativKolicina = n.Kolicina ?? 0m;

                            stavka.UtrosenoDanas += jeloKolicina * normativKolicina;
                            stavka.IsPromet = true;
                            Debug.WriteLine ("-------------- Ukupni izlaz  danas (namirnica): " + stavka.Namirnica + " =  " + stavka.UtrosenoDanas);
                        }
                    }
                }
            }
            catch(Exception m)
            {
                Debug.WriteLine ("-------------- Ukupni izlaz (jela) danas preko normativa - Exception --------- " + m.Message);
                Debug.WriteLine ("Inner exception: " + m.InnerException?.Message);
                Debug.WriteLine ("Stack trace: " + m.StackTrace);

                // Dodatna dijagnostika
                Debug.WriteLine ("Datum parametar: " + datum);
            }

            // 9. Reklamacije danas
            try
            {
                var reklDanas = await (from r in _db.ReklamiraniRacun
                                       join s in _db.ReklamiraniStavka
                                           on r.BrojRekRacuna equals s.BrojReklamiranogRacuna
                                       where r.Datum.Date == datum.Date
                                       group s by s.Artikl into g
                                       select new { NazivJela = g.Key, Kolicina = g.Sum (x => x.Kolicina) })
                                       .ToListAsync ();

                foreach(var reklamacija in reklDanas)
                {
                    // Nađi artikl u tblArtikli (VrstaArtikla = 1 → jelo)
                    var jeloArtikl = await _db.Artikli
                                        .FirstOrDefaultAsync (a => a.Artikl == reklamacija.NazivJela && a.VrstaArtikla == 1);

                    if(jeloArtikl == null)
                        continue;

                    // Dohvati normativ za to jelo
                    var normativi = await _db.Normativ
                                        .Where (n => n.IdProizvoda == jeloArtikl.IdArtikla)
                                        .ToListAsync ();

                    foreach(var n in normativi)
                    {
                        var stavka = knjiga.FirstOrDefault (x => x.Namirnica == n.Repromaterijal);
                        if(stavka != null)
                            // reklamacija vraća repromaterijal → treba oduzeti utrošeno
                            stavka.UtrosenoDanas -= reklamacija.Kolicina * n.Kolicina ?? 0m;

                    }
                }
            }
            catch(Exception c)
            {
                Debug.WriteLine ("--------------        // 9. Reklamacije danas ---------Exception c----------" + c.Message);
            }

            // 10. Ostatak za sutra i promet
            foreach(var s in knjiga)
            {
                s.OstatakZaSutra = s.NaStanju - s.UtrosenoDanas;

            }

            return knjiga;
        }

        private string GetJmName(int? jm)
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
