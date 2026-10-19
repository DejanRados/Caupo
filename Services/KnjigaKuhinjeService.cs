
using Caupo.Data;
using Caupo.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Threading;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Services
{
    public sealed class KnjigaKuhinjeService
    {
        private static readonly SemaphoreSlim SyncLock = new(1, 1);
        private readonly AppDbContext _db;

        public KnjigaKuhinjeService(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task SinhronizujDoDanasAsync()
        {
            await SyncLock.WaitAsync();
            try
            {
                DateTime danas = DateTime.Today;

                DateTime? prviUlaz = await _db.UlazRepromaterijal
                    .AsNoTracking()
                    .Select(x => (DateTime?)x.Datum)
                    .MinAsync();

                DateTime? prviRacun = await (
                    from r in _db.Racuni.AsNoTracking()
                    join s in _db.RacunStavka.AsNoTracking()
                        on r.BrojRacuna equals s.BrojRacuna
                    where s.VrstaArtikla == 1
                       && r.TipRacuna != "Storno"
                    select (DateTime?)r.Datum
                ).MinAsync();

                DateTime? prviDatum = null;
                if (prviUlaz.HasValue)
                    prviDatum = prviUlaz.Value.Date;
                if (prviRacun.HasValue &&
                    (!prviDatum.HasValue || prviRacun.Value.Date < prviDatum))
                    prviDatum = prviRacun.Value.Date;

                DateTime? zadnji = await _db.KnjigaKuhinje
                    .AsNoTracking()
                    .Select(x => (DateTime?)x.Datum)
                    .MaxAsync();

                DateTime? promjena = await _db.KnjigaKuhinjeKontrola
                    .AsNoTracking()
                    .Where(x => x.Id == 1)
                    .Select(x => x.DatumPromjeneStanja)
                    .FirstOrDefaultAsync();

                if (zadnji.HasValue && zadnji.Value.Date > danas)
                    throw new InvalidOperationException(
                        "Knjiga kuhinje sadrži datum u budućnosti.");

                DateTime od;

                if (!zadnji.HasValue)
                {
                    od = prviDatum ?? danas;
                }
                else if (zadnji.Value.Date < danas)
                {
                    od = zadnji.Value.Date.AddDays(1);
                }
                else
                {
                    od = danas;
                }

                if (promjena.HasValue && promjena.Value.Date < od)
                    od = promjena.Value.Date;

                if (prviDatum.HasValue && !zadnji.HasValue &&
                    prviDatum.Value.Date < od)
                    od = prviDatum.Value.Date;

                if (od > danas)
                    od = danas;

                Debug.WriteLine(
                    $"[KUHINJA] Sinhronizacija {od:dd.MM.yyyy} - {danas:dd.MM.yyyy}, " +
                    $"promjena: {promjena:dd.MM.yyyy}");

                await ObracunajPeriodAsync(od, danas, promjena);
            }
            finally
            {
                SyncLock.Release();
            }
        }

        private async Task ObracunajPeriodAsync(
            DateTime od, DateTime doDatuma, DateTime? evidentiranaPromjena)
        {
            od = od.Date;
            doDatuma = doDatuma.Date;

            var materijal = await _db.Repromaterijal
                .AsNoTracking()
                .Where(x => x.Repromaterijal != null &&
                            x.Repromaterijal != "")
                .OrderBy(x => x.Repromaterijal)
                .ToListAsync();

            var nazivi = materijal.Select(x => x.Repromaterijal!)
                .ToHashSet(StringComparer.Ordinal);

            if (nazivi.Count != materijal.Count)
                throw new InvalidOperationException(
                    "Postoje dupli nazivi repromaterijala.");

            var stanje = new Dictionary<string, decimal>(
                StringComparer.Ordinal);

            DateTime prethodni = od.AddDays(-1);

            var prethodniRedovi = await _db.KnjigaKuhinje
                .AsNoTracking()
                .Where(x => x.Datum >= prethodni && x.Datum < od)
                .ToListAsync();

            if (prethodniRedovi.Count > 0)
            {
                foreach (var red in prethodniRedovi)
                    if (red.Artikl != null)
                        stanje[red.Artikl] = red.Zaliha ?? 0m;
            }
            else
            {
                bool imaStarijih = await _db.KnjigaKuhinje
                    .AsNoTracking()
                    .AnyAsync(x => x.Datum < od);

                if (imaStarijih)
                    throw new InvalidOperationException(
                        $"Nedostaje dnevni list za {prethodni:dd.MM.yyyy}.");

                var ranijiUlazi = await (
                    from u in _db.UlazRepromaterijal.AsNoTracking()
                    join s in _db.UlazRepromaterijalStavka.AsNoTracking()
                        on u.BrojUlaza equals s.BrojUlaza
                    where u.Datum < od
                    group s by s.Artikl into g
                    select new
                    {
                        Naziv = g.Key,
                        Kolicina = g.Sum(x => x.Kolicina)
                    }
                ).ToListAsync();

                foreach (var x in ranijiUlazi)
                    if (x.Naziv != null)
                        stanje[x.Naziv] = x.Kolicina;
            }

            var spremljeni = await _db.KnjigaKuhinje
                .AsNoTracking()
                .Where(x => x.Datum >= od &&
                            x.Datum < doDatuma.AddDays(1))
                .ToListAsync();

            var poDanu = spremljeni
                .GroupBy(x => x.Datum.Date)
                .ToDictionary(x => x.Key, x => x.ToList());

            var artikli = await _db.Artikli.AsNoTracking()
                .Where(x => x.VrstaArtikla == 1)
                .ToListAsync();

            var normativi = await _db.Normativ.AsNoTracking()
                .ToListAsync();

            var artiklPoId = artikli.ToDictionary(x => x.IdArtikla);

            var artiklPoNazivu = artikli
                .Where(x => !string.IsNullOrWhiteSpace(x.Artikl))
                .GroupBy(x => x.Artikl!)
                .ToDictionary(x => x.Key, x => x.First());

            var normPoId = normativi
                .Where(x => !string.IsNullOrWhiteSpace(x.Repromaterijal))
                .GroupBy(x => x.IdProizvoda)
                .ToDictionary(x => x.Key, x => x.ToList());

            var novi = new List<TblKnjigaKuhinje>();

            for (DateTime dan = od; dan <= doDatuma; dan = dan.AddDays(1))
            {
                DateTime sutra = dan.AddDays(1);

                var ulazi = await (
                    from u in _db.UlazRepromaterijal.AsNoTracking()
                    join s in _db.UlazRepromaterijalStavka.AsNoTracking()
                        on u.BrojUlaza equals s.BrojUlaza
                    where u.Datum >= dan && u.Datum < sutra
                    select new
                    {
                        s.Artikl,
                        s.Kolicina,
                        u.Dobavljac,
                        u.BrojFakture
                    }
                ).ToListAsync();

                Dictionary<string, decimal> utroseno;

                if (dan < DateTime.Today &&
                    poDanu.TryGetValue(dan, out var sacuvani) &&
                    sacuvani.Count > 0)
                {
                    utroseno = sacuvani
                        .Where(x => x.Artikl != null)
                        .ToDictionary(
                            x => x.Artikl!,
                            x => x.Utroseno ?? 0m,
                            StringComparer.Ordinal);
                }
                else
                {
                    var prodaja = await (
                        from r in _db.Racuni.AsNoTracking()
                        join s in _db.RacunStavka.AsNoTracking()
                            on r.BrojRacuna equals s.BrojRacuna
                        where r.Datum >= dan && r.Datum < sutra
                           && r.TipRacuna != "Storno"
                           && s.VrstaArtikla == 1
                        select new
                        {
                            s.IdArtikla,
                            s.Artikl,
                            s.Kolicina
                        }
                    ).ToListAsync();

                    var refund = await (
                        from r in _db.Racuni.AsNoTracking()
                        join s in _db.RacunStavka.AsNoTracking()
                            on r.BrojRacuna equals s.BrojRacuna
                        where r.Reklamiran == "DA"
                           && r.DatumRefundRacuna >= dan
                           && r.DatumRefundRacuna < sutra
                           && r.TipRacuna != "Storno"
                           && s.VrstaArtikla == 1
                        select new
                        {
                            s.IdArtikla,
                            s.Artikl,
                            s.Kolicina
                        }
                    ).ToListAsync();

                    utroseno = new Dictionary<string, decimal>(
                        StringComparer.Ordinal);

                    void DodajPotrosnju(
                        int id, string? naziv, decimal? kolicina, int znak)
                    {
                        if (!artiklPoId.TryGetValue(id, out var art) &&
                            (naziv == null ||
                             !artiklPoNazivu.TryGetValue(naziv, out art)))
                            throw new InvalidOperationException(
                                $"Jelo '{naziv}' nije pronađeno.");

                        if (!normPoId.TryGetValue(art.IdArtikla, out var recept))
                            throw new InvalidOperationException(
                                $"Jelo '{art.Artikl}' nema normativ.");

                        foreach (var n in recept)
                        {
                            string namirnica = n.Repromaterijal!;

                            if (!nazivi.Contains(namirnica))
                                throw new InvalidOperationException(
                                    $"Namirnica '{namirnica}' nije pronađena.");

                            utroseno[namirnica] =
                                utroseno.GetValueOrDefault(namirnica)
                                + znak * (kolicina ?? 0m) *
                                (n.Kolicina ?? 0m);
                        }
                    }

                    foreach (var x in prodaja)
                        DodajPotrosnju(
                            x.IdArtikla, x.Artikl, x.Kolicina, 1);

                    foreach (var x in refund)
                        DodajPotrosnju(
                            x.IdArtikla, x.Artikl, x.Kolicina, -1);
                }

                foreach (var n in materijal)
                {
                    string naziv = n.Repromaterijal!;
                    var nabavke = ulazi
                        .Where(x => x.Artikl == naziv).ToList();

                    decimal pocetno = stanje.GetValueOrDefault(naziv);
                    decimal primljeno = nabavke.Sum(x => x.Kolicina);
                    decimal trosak = utroseno.GetValueOrDefault(naziv);
                    decimal ukupno = pocetno + primljeno;
                    decimal kraj = ukupno - trosak;

                    novi.Add(new TblKnjigaKuhinje
                    {
                        Datum = dan,
                        Artikl = naziv,
                        JedinicaMjere = n.JedinicaMjere ?? 0,
                        Ostatak = pocetno,
                        Primljeno = primljeno,
                        Stanje = ukupno,
                        Utroseno = trosak,
                        Zaliha = kraj,
                        Cijena = n.NabavnaCijena,
                        Dobavljac = string.Join(", ",
                            nabavke.Select(x => x.Dobavljac)
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .Distinct()),
                        BrojFakture = string.Join(", ",
                            nabavke.Select(x => x.BrojFakture)
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .Distinct())
                    });

                    stanje[naziv] = kraj;
                }

                Debug.WriteLine(
                    $"[KUHINJA] Obračunat {dan:dd.MM.yyyy}");
            }

            await using var transakcija =
                await _db.Database.BeginTransactionAsync();

            try
            {
                var stari = await _db.KnjigaKuhinje
                    .Where(x => x.Datum >= od &&
                                x.Datum < doDatuma.AddDays(1))
                    .ToListAsync();

                _db.KnjigaKuhinje.RemoveRange(stari);
                await _db.SaveChangesAsync();

                _db.KnjigaKuhinje.AddRange(novi);
                await _db.SaveChangesAsync();

                if (evidentiranaPromjena.HasValue)
                {
                    var kontrola = await _db.KnjigaKuhinjeKontrola
                        .FirstOrDefaultAsync(x => x.Id == 1);

                    if (kontrola != null &&
                        kontrola.DatumPromjeneStanja.HasValue &&
                        kontrola.DatumPromjeneStanja.Value.Date ==
                            evidentiranaPromjena.Value.Date)
                    {
                        kontrola.DatumPromjeneStanja = null;
                        await _db.SaveChangesAsync();
                    }
                }

                await transakcija.CommitAsync();
            }
            catch
            {
                await transakcija.RollbackAsync();
                _db.ChangeTracker.Clear();
                throw;
            }
        }

        public async Task<List<StavkaKnjigeKuhinje>>
            GetKnjigaZaDanAsync(DateTime datum)
        {
            DateTime dan = datum.Date;

            var redovi = await _db.KnjigaKuhinje.AsNoTracking()
                .Where(x => x.Datum >= dan &&
                            x.Datum < dan.AddDays(1))
                .OrderBy(x => x.Artikl)
                .ToListAsync();

            return redovi.Select((x, i) => new StavkaKnjigeKuhinje
            {
                RedniBroj = i + 1,
                Datum = x.Datum,
                Namirnica = x.Artikl,
                JedinicaMjere = GetJmName(x.JedinicaMjere),
                OstatakOdJuce = x.Ostatak,
                NabavljenoDanas = x.Primljeno,
                NaStanju = x.Stanje,
                UtrosenoDanas = x.Utroseno,
                OstatakZaSutra = x.Zaliha,
                Dobavljac = x.Dobavljac,
                Dokument = x.BrojFakture,
                IsPromet = (x.Utroseno ?? 0m) != 0m
            }).ToList();
        }

        public async Task<List<(string Naziv, decimal Kolicina,
            decimal Vrijednost)>> GetJelaZaDanAsync(DateTime datum)
        {
            DateTime dan = datum.Date;

            var jela = await (
                from r in _db.Racuni.AsNoTracking()
                join s in _db.RacunStavka.AsNoTracking()
                    on r.BrojRacuna equals s.BrojRacuna
                where r.Datum >= dan &&
                      r.Datum < dan.AddDays(1) &&
                      r.TipRacuna != "Storno" &&
                      s.VrstaArtikla == 1
                group s by s.Artikl into g
                select new
                {
                    Naziv = g.Key,
                    Kolicina = g.Sum(x => x.Kolicina),
                    Vrijednost = g.Sum(x => x.Cijena * x.Kolicina)
                }
            ).ToListAsync();

            return jela.Select(x => (
                x.Naziv ?? "",
                x.Kolicina ?? 0m,
                x.Vrijednost ?? 0m)).ToList();
        }

        private static string GetJmName(int jm) => jm switch
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
