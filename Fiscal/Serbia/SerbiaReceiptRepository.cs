using Caupo.Data;
using Caupo.Fiscal.Common;
using Caupo.Fiscal.Serbia.Models;
using Microsoft.EntityFrameworkCore;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Fiscal.Serbia
{
    public sealed class SerbiaReceiptRepository
    {
        public async Task<int> GetNextLocalReceiptNumberAsync(
            CancellationToken cancellationToken = default)
        {
            await using var db =
                new AppDbContext();

            int? max =
                await db.Racuni
                    .MaxAsync(
                        x => (int?)x.BrojRacuna,
                        cancellationToken);

            return (max ?? 0) + 1;
        }

        public async Task<int> SaveAsync(
            FiscalRequest request,
            SerbiaInvoiceResponse response,
            CancellationToken cancellationToken = default)
        {
            await using var db =
                new AppDbContext();

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var racun =
                    new TblRacuni
                    {
                        Datum =
                            response.SdcDateTime?
                                .LocalDateTime
                            ?? DateTime.Now,

                        Kupac =
                            request.Buyer == null ||
                            request.Buyer.IsCitizen
                                ? "Gradjani"
                                : request.Buyer.Name,

                        NacinPlacanja =
                            (int)request.PaymentType,

                        BrojFiskalnogRacuna =
                            response.InvoiceNumber,

                        Radnik =
                            request.Cashier.Id?
                                .ToString(),

                        Fiskalizovan =
                            "DA",

                        Iznos =
                            request.TotalAmount
                    };

                await db.Racuni.AddAsync(
                    racun,
                    cancellationToken);

                await db.SaveChangesAsync(
                    cancellationToken);

                foreach(var item in request.Items)
                {
                    var dbItem =
                        new TblRacunStavka
                        {
                            BrojRacuna =
                                racun.BrojRacuna,

                            Artikl =
                                item.Name,

                            Sifra =
                                item.Sifra,

                            Kolicina =
                                item.Quantity ?? 0m,

                            Cijena =
                                item.UnitPrice,

                            PoreskaStopa =
                                item.PoreskaStopa,

                            JedinicaMjere =
                                item.JedinicaMjere,

                            VrstaArtikla =
                                item.Proizvod,

                            ArtiklNormativ =
                                item.Naziv
                        };

                    await db.RacunStavka.AddAsync(
                        dbItem,
                        cancellationToken);
                }

                await db.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                return racun.BrojRacuna;
            }
            catch
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                throw;
            }
        }
    }
}
