using Caupo.Data;
using Caupo.Fiscal.Common;
using Caupo.Fiscal.Federation.Models;
using Microsoft.EntityFrameworkCore;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Fiscal.Federation
{
    public sealed class FederationReceiptRepository
    {

        public async Task<int> GetNextLocalReceiptNumberAsync(
            CancellationToken cancellationToken = default)
        {
            await using var db =
                new AppDbContext();

            int? max =
                await db.Racuni.MaxAsync(
                    x => (int?)x.BrojRacuna,
                    cancellationToken);

            return (max ?? 0) + 1;
        }

        public async Task<int> SaveAsync(
            FiscalRequest request,
            FederationBuiltInvoice builtInvoice,
            FederationFiscalResponse fiscalResponse,
            CancellationToken cancellationToken = default)
        {
            await using var db =
                new AppDbContext();

            await using var transaction =
                await db.Database
                    .BeginTransactionAsync(
                        cancellationToken);

            try
            {
                string buyer =
                    request.Buyer == null ||
                    request.Buyer.IsCitizen
                        ? "Gradjani"
                        : request.Buyer.Name
                            ?? "Gradjani";

                // Stari Tring tok spremao je ID radnika u polje Radnik.
                string cashier =
                    request.Cashier.Id?.ToString()
                    ?? request.Cashier.Name
                    ?? string.Empty;

                var receipt =
                    new TblRacuni
                    {
                        Datum =
                            builtInvoice.IssueDateTime,

                        Kupac =
                            buyer,

                        NacinPlacanja =
                            (int)request.PaymentType,

                        BrojFiskalnogRacuna =
                            fiscalResponse
                                .FiscalReceiptNumber,

                        Radnik =
                            cashier,

                        Fiskalizovan =
                            fiscalResponse.Fiscalized
                                ? "DA"
                                : "NE",

                        Iznos =
                            request.TotalAmount
                    };

                await db.Racuni.AddAsync(
                    receipt,
                    cancellationToken);

                await db.SaveChangesAsync(
                    cancellationToken);

                foreach(var item in request.Items)
                {
                    var row =
                        new TblRacunStavka
                        {
                            BrojRacuna =
                                receipt.BrojRacuna,

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
                        row,
                        cancellationToken);
                }

                await db.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                return receipt.BrojRacuna;
            }
            catch
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                throw;
            }
        }

        public async Task<bool> MarkRefundedAsync(FiscalRequest request, FederationFiscalResponse fiscalResponse, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.ReferentDocumentNumber))
                throw new FiscalException("Nije zadan broj originalnog fiskalnog računa.");

            if (string.IsNullOrWhiteSpace(fiscalResponse.FiscalReceiptNumber))
                throw new FiscalException("Tring nije vratio broj reklamiranog fiskalnog računa.");

            await using var db = new AppDbContext();

            var receipt = await db.Racuni
                .FirstOrDefaultAsync(
                    x => x.BrojFiskalnogRacuna == request.ReferentDocumentNumber,
                    cancellationToken);

            if (receipt == null)
                throw new FiscalException(
                    $"Originalni račun sa fiskalnim brojem '{request.ReferentDocumentNumber}' nije pronađen.");

            receipt.Reklamiran = "DA";
            receipt.BrojRefundRacuna = fiscalResponse.FiscalReceiptNumber;
            receipt.DatumRefundRacuna = DateTime.Now;

            await db.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<int> GetLocalReceiptNumberByFiscalNumberAsync(string fiscalReceiptNumber, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fiscalReceiptNumber))
                throw new FiscalException("Broj originalnog fiskalnog računa nije zadan.");

            await using var db = new AppDbContext();

            var receipt = await db.Racuni
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BrojFiskalnogRacuna == fiscalReceiptNumber, cancellationToken);

            if (receipt == null)
                throw new FiscalException($"Originalni račun sa fiskalnim brojem '{fiscalReceiptNumber}' nije pronađen.");

            return receipt.BrojRacuna;
        }
    }
}
