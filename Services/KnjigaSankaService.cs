using Caupo.Data;
using Caupo.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using static Caupo.Data.DatabaseTables;

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
            DateTime danas = datum.Date;

            Debug.WriteLine("========================================");
            Debug.WriteLine("KNJIGA ŠANKA - POČETAK");
            Debug.WriteLine($"Vrijeme: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            Debug.WriteLine($"Traženi datum: {danas:dd.MM.yyyy}");
            Debug.WriteLine("========================================");

            DateTime? prviUlaz = await _db.Ulaz.Select(x => (DateTime?)x.Datum).MinAsync();
            DateTime? prviRacun = await _db.Racuni.Select(x => (DateTime?)x.Datum).MinAsync();

            DateTime? prviDatum = null;

            if (prviUlaz.HasValue && prviRacun.HasValue)
                prviDatum = prviUlaz.Value.Date <= prviRacun.Value.Date ? prviUlaz.Value.Date : prviRacun.Value.Date;
            else if (prviUlaz.HasValue)
                prviDatum = prviUlaz.Value.Date;
            else if (prviRacun.HasValue)
                prviDatum = prviRacun.Value.Date;

            DateTime? datumPromjeneStanja = await _db.KnjigaSankaKontrola.Where(x => x.Id == 1).Select(x => x.DatumPromjeneStanja).FirstOrDefaultAsync();
            DateTime? zadnjiDatumKnjige = await _db.KnjigaSanka.Select(x => (DateTime?)x.Datum).MaxAsync();

            DateTime? obracunOd = null;

            if (datumPromjeneStanja.HasValue)
                obracunOd = datumPromjeneStanja.Value.Date;
            else if (!zadnjiDatumKnjige.HasValue)
                obracunOd = prviDatum;
            else if (zadnjiDatumKnjige.Value.Date < danas)
                obracunOd = zadnjiDatumKnjige.Value.Date.AddDays(1);
            else
                obracunOd = danas;

            if (obracunOd.HasValue && obracunOd.Value > danas)
                obracunOd = danas;

            Debug.WriteLine($"Prvi ulaz            : {(prviUlaz.HasValue ? prviUlaz.Value.ToString("dd.MM.yyyy HH:mm:ss") : "NEMA")}");
            Debug.WriteLine($"Prvi račun           : {(prviRacun.HasValue ? prviRacun.Value.ToString("dd.MM.yyyy HH:mm:ss") : "NEMA")}");
            Debug.WriteLine($"Prvi relevantni datum: {(prviDatum.HasValue ? prviDatum.Value.ToString("dd.MM.yyyy") : "NEMA")}");
            Debug.WriteLine($"Zadnji datum knjige  : {(zadnjiDatumKnjige.HasValue ? zadnjiDatumKnjige.Value.ToString("dd.MM.yyyy") : "NEMA")}");
            Debug.WriteLine($"Datum promjene stanja: {(datumPromjeneStanja.HasValue ? datumPromjeneStanja.Value.ToString("dd.MM.yyyy") : "NEMA")}");
            Debug.WriteLine($"OBRAČUN OD           : {(obracunOd.HasValue ? obracunOd.Value.ToString("dd.MM.yyyy") : "NEMA")}");
            Debug.WriteLine($"OBRAČUN DO           : {danas:dd.MM.yyyy}");
            Debug.WriteLine("----------------------------------------");

            if (obracunOd.HasValue)
                await ObracunajPeriodAsync(obracunOd.Value, danas);

            var rezultat = await UcitajKnjiguZaDanAsync(danas);

            Debug.WriteLine("========================================");
            Debug.WriteLine($"KNJIGA ŠANKA - GOTOVO | Redova: {rezultat.Count}");
            Debug.WriteLine("========================================");

            return rezultat;
        }

        private async Task ObracunajPeriodAsync(DateTime obracunOd, DateTime obracunDo)
        {
            obracunOd = obracunOd.Date;
            obracunDo = obracunDo.Date;

            Debug.WriteLine("KNJIGA ŠANKA - OBRAČUN PERIODA");

            var artikli = await _db.Artikli
                .AsNoTracking()
                .Where(x => x.VrstaArtikla == 0 && x.Aktivan)
                .ToListAsync();

            var bazniArtikli = artikli
                .GroupBy(x => x.Artikl)
                .Select(g => g.First())
                .OrderBy(x => x.Artikl)
                .ToList();

            Debug.WriteLine($"Baznih artikala: {bazniArtikli.Count}");

            var prethodnoStanje = new Dictionary<string, decimal>();

            DateTime prethodniDan = obracunOd.AddDays(-1);

            var prethodniRedovi = await _db.KnjigaSanka
                .AsNoTracking()
                .Where(x => x.Datum >= prethodniDan && x.Datum < obracunOd)
                .ToListAsync();

            foreach (var red in prethodniRedovi)
                prethodnoStanje[red.Artikl] = red.OstatakDanas;

            var sviNoviRedovi = new List<TblKnjigaSanka>();

            for (DateTime dan = obracunOd; dan <= obracunDo; dan = dan.AddDays(1))
            {
                DateTime sutra = dan.AddDays(1);

                // ============================================================
                // ULAZI
                // ============================================================

                var ulazi = await (
                    from u in _db.Ulaz.AsNoTracking()
                    join s in _db.UlazStavke.AsNoTracking() on u.BrojUlaza equals s.BrojUlaza
                    where u.Datum >= dan && u.Datum < sutra
                    group s by s.Artikl into g
                    select new
                    {
                        Artikl = g.Key,
                        Kolicina = g.Sum(x => x.Kolicina)
                    }).ToListAsync();

                var primljeno = ulazi
                    .Where(x => x.Artikl != null)
                    .ToDictionary(x => x.Artikl!, x => x.Kolicina);

                // ============================================================
                // PRODAJA
                //
                // Hrvatski Storno račun ne ulazi u prodaju.
                // Njegov original će biti obrađen kroz reklamacije.
                // ============================================================

                var prodaja = await (
                    from r in _db.Racuni.AsNoTracking()
                    join s in _db.RacunStavka.AsNoTracking() on r.BrojRacuna equals s.BrojRacuna
                    join a in _db.Artikli.AsNoTracking() on s.IdArtikla equals a.IdArtikla
                    where r.Datum >= dan &&
                          r.Datum < sutra &&
                          r.TipRacuna != "Storno" &&
                          s.VrstaArtikla == 0
                    group new { s, a } by a.Artikl into g
                    select new
                    {
                        Artikl = g.Key,
                        Utroseno = g.Sum(x => x.s.Kolicina * (x.a.Normativ ?? 1m))
                    }).ToListAsync();

                var prodanoPoArtiklu = prodaja
                    .Where(x => x.Artikl != null)
                    .ToDictionary(x => x.Artikl!, x => x.Utroseno);

                // ============================================================
                // REKLAMACIJE
                //
                // Uzimamo ORIGINALNE račune koji su reklamirani ovog dana.
                // Originalne stavke se obračunavaju istim normativom kao prodaja.
                // ============================================================

                var reklamacije = await (
                    from r in _db.Racuni.AsNoTracking()
                    join s in _db.RacunStavka.AsNoTracking() on r.BrojRacuna equals s.BrojRacuna
                    join a in _db.Artikli.AsNoTracking() on s.IdArtikla equals a.IdArtikla
                    where r.Reklamiran == "DA" &&
                          r.DatumRefundRacuna >= dan &&
                          r.DatumRefundRacuna < sutra &&
                          r.TipRacuna != "Storno" &&
                          s.VrstaArtikla == 0
                    group new { s, a } by a.Artikl into g
                    select new
                    {
                        Artikl = g.Key,
                        Reklamirano = g.Sum(x => x.s.Kolicina * (x.a.Normativ ?? 1m))
                    }).ToListAsync();

                var reklamiranoPoArtiklu = reklamacije
                    .Where(x => x.Artikl != null)
                    .ToDictionary(x => x.Artikl!, x => x.Reklamirano);

                // ============================================================
                // NETO UTROŠENO
                //
                // Utrošeno = prodaja - reklamacije
                // ============================================================

                var utroseno = new Dictionary<string, decimal?>();

                foreach (var artikl in bazniArtikli)
                {
                    if (string.IsNullOrWhiteSpace(artikl.Artikl))
                        continue;

                    decimal prodano = prodanoPoArtiklu.TryGetValue(artikl.Artikl, out decimal? p)
                        ? p ?? 0m
                        : 0m;

                    decimal reklamirano = reklamiranoPoArtiklu.TryGetValue(artikl.Artikl, out decimal? r)
                        ? r ?? 0m
                        : 0m;

                    utroseno[artikl.Artikl] = prodano - reklamirano;
                }

                // ============================================================
                // KNJIGA ZA DAN
                // ============================================================

                var dnevniRedovi = new List<TblKnjigaSanka>();

                foreach (var artikl in bazniArtikli)
                {
                    if (string.IsNullOrWhiteSpace(artikl.Artikl))
                        continue;

                    decimal ostatakOdJuce = prethodnoStanje.TryGetValue(artikl.Artikl, out decimal prethodno)
                        ? prethodno
                        : 0m;

                    decimal primljenoDanas = primljeno.TryGetValue(artikl.Artikl, out decimal ulaz)
                        ? ulaz
                        : 0m;

                    decimal utrosenoDanas = utroseno.TryGetValue(artikl.Artikl, out decimal? potrosnja)
                        ? potrosnja ?? 0m
                        : 0m;

                    decimal ukupno = ostatakOdJuce + primljenoDanas;
                    decimal ostatakDanas = ukupno - utrosenoDanas;

                    // ========================================================
                    // JEDINIČNA CIJENA I IZNOS
                    //
                    // Jedinična cijena predstavlja prodajnu cijenu pune
                    // jedinice mjere:
                    //
                    // JedinicnaCijena = Cijena / Normativ
                    //
                    // Primjer:
                    // 0,03 L = 2,00 EUR
                    // 2,00 / 0,03 = 66,666... EUR/L
                    //
                    // Ne zaokružujemo tokom obračuna.
                    // ========================================================

                    decimal normativ = artikl.Normativ ?? 1m;
                    decimal cijena = artikl.Cijena ?? 0m;

                    decimal jedinicnaCijena = normativ > 0m
                        ? cijena / normativ
                        : 0m;

                    decimal iznos = utrosenoDanas * jedinicnaCijena;

                    dnevniRedovi.Add(new TblKnjigaSanka
                    {
                        Datum = dan,
                        Artikl = artikl.Artikl,
                        JedinicaMjere = artikl.JedinicaMjere ?? 0,
                        OstatakOdJuce = ostatakOdJuce,
                        Primljeno = primljenoDanas,
                        Hash = string.Empty,
                        Ukupno = ukupno,
                        Utroseno = utrosenoDanas,
                        OstatakDanas = ostatakDanas,
                        JedinicnaCijena = jedinicnaCijena,
                        Iznos = iznos
                    });

                    prethodnoStanje[artikl.Artikl] = ostatakDanas;
                }

                sviNoviRedovi.AddRange(dnevniRedovi);

                Debug.WriteLine(
                    $"{dan:dd.MM.yyyy} | " +
                    $"Artikala: {dnevniRedovi.Count} | " +
                    $"Primljeno: {dnevniRedovi.Sum(x => x.Primljeno)} | " +
                    $"Utrošeno: {dnevniRedovi.Sum(x => x.Utroseno)} | " +
                    $"Ostatak: {dnevniRedovi.Sum(x => x.OstatakDanas)}");
            }

            Debug.WriteLine($"Pripremljeno za upis: {sviNoviRedovi.Count} redova");

            // ================================================================
            // UPIS
            //
            // Tek kada je kompletan obračun gotov otvaramo transakciju.
            // ================================================================

            await using var transakcija = await _db.Database.BeginTransactionAsync();

            try
            {
                var stariRedovi = await _db.KnjigaSanka
                    .Where(x => x.Datum >= obracunOd && x.Datum < obracunDo.AddDays(1))
                    .ToListAsync();

                if (stariRedovi.Count > 0)
                    _db.KnjigaSanka.RemoveRange(stariRedovi);

                _db.KnjigaSanka.AddRange(sviNoviRedovi);

                var kontrola = await _db.KnjigaSankaKontrola
                    .FirstOrDefaultAsync(x => x.Id == 1);

                if (kontrola != null)
                    kontrola.DatumPromjeneStanja = null;

                await _db.SaveChangesAsync();
                await transakcija.CommitAsync();

                Debug.WriteLine($"Upis uspješan: {sviNoviRedovi.Count} redova");
            }
            catch
            {
                await transakcija.RollbackAsync();
                _db.ChangeTracker.Clear();

                Debug.WriteLine("KNJIGA ŠANKA - GREŠKA | Transakcija poništena.");

                throw;
            }

            Debug.WriteLine("KNJIGA ŠANKA - OBRAČUN PERIODA GOTOV");
        }

        private async Task<List<StavkaKnjigeSanka>> UcitajKnjiguZaDanAsync(DateTime datum)
        {
            DateTime dan = datum.Date;
            DateTime sutra = dan.AddDays(1);

            var redovi = await _db.KnjigaSanka.AsNoTracking().Where(x => x.Datum >= dan && x.Datum < sutra).OrderBy(x => x.Artikl).ToListAsync();

            var rezultat = new List<StavkaKnjigeSanka>();
            int redniBroj = 1;

            foreach (var red in redovi)
            {
                rezultat.Add(new StavkaKnjigeSanka
                {
                    RedniBroj = redniBroj++,
                    Datum = red.Datum,
                    Namirnica = red.Artikl,
                    Naziv = red.Artikl,
                    JedinicaMjere = GetJmName(red.JedinicaMjere),
                    Cijena = red.JedinicnaCijena,
                    OstatakOdJuce = red.OstatakOdJuce,
                    NabavljenoDanas = red.Primljeno,
                    NaStanju = red.Ukupno,
                    UtrosenoDanas = red.Utroseno,
                    OstatakZaSutra = red.OstatakDanas,
                    Promet = red.Iznos,
                    IsPromet = red.Utroseno > 0
                });
            }

            return rezultat;
        }

        private static string GetJmName(int jm)
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
                _ => string.Empty
            };
        }
    }
}