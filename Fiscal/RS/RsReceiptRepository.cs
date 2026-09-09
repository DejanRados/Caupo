using Caupo.Data;
using Caupo.Fiscal.Common;
using Caupo.Fiscal.RS.Models;
using Microsoft.EntityFrameworkCore;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Fiscal.RS
{
    public sealed class RsReceiptRepository
    {
        public async Task<int> GetNextLocalReceiptNumberAsync(CancellationToken cancellationToken = default)
        {
            await using var db = new AppDbContext();

            int? max = await db.Racuni.MaxAsync(x => (int?)x.BrojRacuna, cancellationToken);

            return (max ?? 0) + 1;
        }

        public async Task<int> SaveAsync(
            FiscalRequest request,
            RsBuiltInvoice builtInvoice,
            RsFiscalResponse fiscalResponse,
            CancellationToken cancellationToken = default)
        {
            await using var db = new AppDbContext();

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                string buyer = request.Buyer == null || request.Buyer.IsCitizen
                    ? "Gradjani"
                    : request.Buyer.Name ?? "Gradjani";

               

                var receipt = new TblRacuni
                {
                    Datum = fiscalResponse.SdcDateTime ?? builtInvoice.IssueDateTime,
                    DatumFiskalnogDokumenta = builtInvoice.IssueDateTime,

                    Kupac = buyer,

                    NacinPlacanja = (int)request.PaymentType,

                    BrojFiskalnogRacuna = fiscalResponse.FiscalReceiptNumber,
                    BrojacFiskalnogRacuna = fiscalResponse.TotalCounter,
                    FiskalniVerificationUrl = fiscalResponse.VerificationUrl,
                    Radnik = request.Cashier.Id.Value.ToString(),
                    Fiskalizovan = fiscalResponse.Fiscalized ? "DA" : "NE",
                    Iznos = request.TotalAmount
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

        public async Task<bool> MarkRefundedAsync(
            string fiscalReceiptNumber,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fiscalReceiptNumber))
                return false;

            await using var db = new AppDbContext();

            int affected = await db.Racuni
                .Where(x => x.BrojFiskalnogRacuna == fiscalReceiptNumber)
                .ExecuteUpdateAsync(
                    update => update.SetProperty(x => x.Reklamiran, "DA"),
                    cancellationToken);

            return affected > 0;
        }
    }
}