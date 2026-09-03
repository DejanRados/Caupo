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
                string cashier = request.Cashier.Name ?? string.Empty;

                decimal pnpRate = builtInvoice.Taxes
                    .Where(x => x.IsConsumptionTax)
                    .Select(x => x.Rate)
                    .FirstOrDefault();

                var receipt = new TblRacuni
                {
                    Kupac = buyerName,
                    Datum = builtInvoice.IssueDateTime,
                    NacinPlacanja = (int)request.PaymentType,
                    BrojFiskalnogRacuna = builtInvoice.LocalReceiptNumber.ToString(),
                    Radnik = cashier,
                    Fiskalizovan = fiscalization.Fiscalized ? "DA" : "NE",
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
    }
}