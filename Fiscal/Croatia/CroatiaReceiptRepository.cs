using Caupo.Data;
using Caupo.Fiscal.Common;
using Caupo.Fiscal.Croatia.Models;
using Microsoft.EntityFrameworkCore;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Fiscal.Croatia
{
    public sealed class CroatiaReceiptRepository
    {
        public async Task<int> GetNextLocalReceiptNumberAsync(CancellationToken cancellationToken = default)
        {
            await using var db = new AppDbContext();

            int? max = await db.Racuni.MaxAsync(x => (int?)x.BrojRacuna, cancellationToken);

            return (max ?? 0) + 1;
        }

        public async Task<int> SaveAsync(FiscalRequest request, CroatiaBuiltInvoice builtInvoice, CroatiaFiscalizationResponse fiscalization, CancellationToken cancellationToken = default)
        {
            await using var db = new AppDbContext();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                string buyerName = request.Buyer == null || request.Buyer.IsCitizen ? "Gradjani" : request.Buyer.Name ?? "Gradjani";
                string cashierOib = request.Cashier.IdentificationNumber ?? string.Empty;

                decimal pnpRate = builtInvoice.Taxes
                    .Where(x => x.IsConsumptionTax)
                    .Select(x => x.Rate)
                    .FirstOrDefault();

                var receipt = new TblRacuni
                {
                    Kupac = buyerName,
                    Datum = builtInvoice.IssueDateTime,
                    DatumFiskalnogDokumenta = null,
                    NacinPlacanja = (int)request.PaymentType,
                    BrojFiskalnogRacuna = builtInvoice.LocalReceiptNumber.ToString(),
                    Radnik = cashierOib,
                    Fiskalizovan = fiscalization.Fiscalized ? FiscalizationStatus.Fiscalized.ToString() : FiscalizationStatus.NotFiscalized.ToString(),
                    Jir = fiscalization.Jir,
                    Zki = fiscalization.Zki,
                    BrojRacunaHr = builtInvoice.ReceiptNumberHr,
                    Iznos = request.TotalAmount,
                    PorezNaPotrosnjuStopa = pnpRate
                };

                await db.Racuni.AddAsync(receipt, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);

                foreach (var item in request.Items)
                {
                    var row = new TblRacunStavka
                    {
                        BrojRacuna = receipt.BrojRacuna,
                        Artikl = item.Name,
                        Sifra = item.Sifra,
                        Kolicina = item.Quantity ?? 0m,
                        Cijena = item.UnitPrice,
                        PoreskaStopa = item.PoreskaStopa,
                        PorezNaPotrosnju = item.PorezNaPotrosnju,
                        JedinicaMjere = item.JedinicaMjere,
                        VrstaArtikla = item.Proizvod,
                        ArtiklNormativ = item.Naziv
                    };

                    await db.RacunStavka.AddAsync(row, cancellationToken);
                }

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return receipt.BrojRacuna;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<List<TblRacuni>> GetNotFiscalizedAsync(CancellationToken cancellationToken = default)
        {
            await using var db = new AppDbContext();

            return await db.Racuni
                .AsNoTracking()
                .Where(x => x.Fiskalizovan == FiscalizationStatus.NotFiscalized.ToString())
                .OrderBy(x => x.Datum)
                .ToListAsync(cancellationToken);
        }

        public async Task MarkFiscalizationAttemptAsync(int localReceiptNumber, CancellationToken cancellationToken = default)
        {
            await using var db = new AppDbContext();

            var receipt = await db.Racuni.FirstOrDefaultAsync(x => x.BrojRacuna == localReceiptNumber, cancellationToken);

            if (receipt == null)
                throw new FiscalException($"Račun {localReceiptNumber} nije pronađen.");

            receipt.DatumFiskalnogDokumenta = DateTime.Now;

            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkFiscalizedAsync(int localReceiptNumber, string jir, string zki, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jir))
                throw new FiscalException("CIS nije vratio JIR.");

            await using var db = new AppDbContext();

            var receipt = await db.Racuni.FirstOrDefaultAsync(x => x.BrojRacuna == localReceiptNumber, cancellationToken);

            if (receipt == null)
                throw new FiscalException($"Račun {localReceiptNumber} nije pronađen.");

            receipt.Jir = jir;
            receipt.Zki = zki;
            receipt.Fiskalizovan = FiscalizationStatus.Fiscalized.ToString();

            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task<(TblRacuni Receipt, List<TblRacunStavka> Items)> GetForSubsequentFiscalizationAsync(int localReceiptNumber, CancellationToken cancellationToken = default)
        {
            await using var db = new AppDbContext();

            var receipt = await db.Racuni
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BrojRacuna == localReceiptNumber, cancellationToken);

            if (receipt == null)
                throw new FiscalException($"Račun {localReceiptNumber} nije pronađen.");

            if (!string.Equals(receipt.Fiskalizovan, FiscalizationStatus.NotFiscalized.ToString(), StringComparison.OrdinalIgnoreCase))
                throw new FiscalException($"Račun {localReceiptNumber} nije označen za naknadnu fiskalizaciju.");

            if (string.IsNullOrWhiteSpace(receipt.BrojRacunaHr))
                throw new FiscalException($"Račun {localReceiptNumber} nema hrvatski broj računa.");

            if (string.IsNullOrWhiteSpace(receipt.Zki))
                throw new FiscalException($"Račun {localReceiptNumber} nema ZKI.");

            if (string.IsNullOrWhiteSpace(receipt.Radnik))
                throw new FiscalException($"Račun {localReceiptNumber} nema OIB operatera.");

            var items = await db.RacunStavka
                .AsNoTracking()
                .Where(x => x.BrojRacuna == localReceiptNumber)
                .ToListAsync(cancellationToken);

            if (items.Count == 0)
                throw new FiscalException($"Račun {localReceiptNumber} nema spremljenih stavki.");

            return (receipt, items);
        }
    }
}