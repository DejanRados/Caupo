using Caupo.Data;
using static Caupo.Data.DatabaseTables;
using Microsoft.EntityFrameworkCore;
using Caupo.Fiscal.Common;
using Caupo.Fiscal.Croatia.Helpers;
using Caupo.Fiscal.Croatia.Models;
using MAES.Fiskal;
using System.Globalization;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Fiscal.Croatia
{
    public sealed class CroatiaInvoiceBuilder
    {
        private const string DateFormatLong = "dd.MM.yyyyTHH:mm:ss";

        private readonly CroatiaFiscalSettings _settings;
        private readonly CroatiaTaxCalculator _taxCalculator;

        public CroatiaInvoiceBuilder(CroatiaFiscalSettings settings)
        {
            _settings = settings;
            _taxCalculator = new CroatiaTaxCalculator(settings);
        }

        public async Task<CroatiaBuiltInvoice> BuildAsync(FiscalRequest request, int localReceiptNumber, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Items == null || request.Items.Count == 0)
                throw new FiscalException("Račun nema stavki.");

            DateTime issueDateTime = DateTime.Now;

            var taxes = await _taxCalculator.CalculateAsync(request.Items, cancellationToken);

            var vat = taxes.Where(x => !x.IsConsumptionTax).Select(CroatiaTaxCalculator.ToMaesTax).ToArray();
            var consumptionTax = taxes.Where(x => x.IsConsumptionTax).Select(CroatiaTaxCalculator.ToMaesTax).ToArray();

            OznakaSlijednostiType sequence =
                string.Equals(_settings.SequenceSetting, "Poslovni prostor", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_settings.SequenceSetting, "Na nivou poslovnog prostora", StringComparison.OrdinalIgnoreCase)
                    ? OznakaSlijednostiType.P
                    : OznakaSlijednostiType.N;

            var invoice = new RacunType
            {
                BrRac = new BrojRacunaType
                {
                    BrOznRac = localReceiptNumber.ToString(CultureInfo.InvariantCulture),
                    OznPosPr = _settings.BusinessPremise,
                    OznNapUr = _settings.CashRegister
                },

                DatVrijeme = issueDateTime.ToString(DateFormatLong, CultureInfo.InvariantCulture),
                IznosUkupno = request.TotalAmount.ToString("F2", CultureInfo.InvariantCulture),
                NakDost = false,
                Oib = _settings.Oib,
                OibOper = request.Cashier.IdentificationNumber ?? string.Empty,
                OznSlijed = sequence,
                Pdv = vat.Length > 0 ? vat : null,
                Pnp = consumptionTax.Length > 0 ? consumptionTax : null,
                USustPdv = _settings.IsVatRegistered,
                NacinPlac = CroatiaPaymentMapper.ToMaes(request.PaymentType)
            };

            string receiptNumberHr = $"{localReceiptNumber}/{_settings.BusinessPremise}/{_settings.CashRegister}";

            return new CroatiaBuiltInvoice
            {
                Invoice = invoice,
                LocalReceiptNumber = localReceiptNumber,
                ReceiptNumberHr = receiptNumberHr,
                IssueDateTime = issueDateTime,
                Taxes = taxes,
                TotalAmount = request.TotalAmount
            };
        }


        public async Task<CroatiaBuiltInvoice> BuildStornoAsync(
    TblRacuni originalReceipt,
    IReadOnlyList<TblRacunStavka> originalItems,
    int newLocalReceiptNumber,
    string workerOib,
    CancellationToken cancellationToken = default)
        {
            if (originalReceipt == null)
                throw new ArgumentNullException(nameof(originalReceipt));

            if (originalItems == null || originalItems.Count == 0)
                throw new FiscalException("Originalni račun nema spremljenih stavki.");

            if (newLocalReceiptNumber <= 0)
                throw new FiscalException("Neispravan broj storno računa.");

            if (string.IsNullOrWhiteSpace(workerOib))
                throw new FiscalException("Radnik koji radi storno nema upisan OIB.");

            decimal pnpRate = originalReceipt.PorezNaPotrosnjuStopa ?? 0m;

            IReadOnlyList<CroatiaTaxSummary> originalTaxes =
                await _taxCalculator.CalculateFromStoredItemsAsync(
                    originalItems,
                    pnpRate,
                    cancellationToken);

            IReadOnlyList<CroatiaTaxSummary> stornoTaxes =
                originalTaxes
                    .Select(x => new CroatiaTaxSummary
                    {
                        Rate = x.Rate,
                        IsConsumptionTax = x.IsConsumptionTax,
                        BaseAmount = -Math.Abs(x.BaseAmount),
                        TaxAmount = -Math.Abs(x.TaxAmount)
                    })
                    .ToList();

            var vat = stornoTaxes
                .Where(x => !x.IsConsumptionTax)
                .Select(CroatiaTaxCalculator.ToMaesTax)
                .ToArray();

            var consumptionTax = stornoTaxes
                .Where(x => x.IsConsumptionTax)
                .Select(CroatiaTaxCalculator.ToMaesTax)
                .ToArray();

            OznakaSlijednostiType sequence =
                string.Equals(_settings.SequenceSetting, "Poslovni prostor", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_settings.SequenceSetting, "Na nivou poslovnog prostora", StringComparison.OrdinalIgnoreCase)
                    ? OznakaSlijednostiType.P
                    : OznakaSlijednostiType.N;

            decimal originalTotal = originalItems.Sum(x => (x.Kolicina ?? 0m) * (x.Cijena ?? 0m));
            decimal stornoTotal = -Math.Abs(originalTotal);

            DateTime issueDateTime = DateTime.Now;

            var invoice = new RacunType
            {
                BrRac = new BrojRacunaType
                {
                    BrOznRac = newLocalReceiptNumber.ToString(CultureInfo.InvariantCulture),
                    OznPosPr = _settings.BusinessPremise,
                    OznNapUr = _settings.CashRegister
                },

                DatVrijeme = issueDateTime.ToString(DateFormatLong, CultureInfo.InvariantCulture),
                IznosUkupno = stornoTotal.ToString("F2", CultureInfo.InvariantCulture),
                NakDost = false,
                Oib = _settings.Oib,
                OibOper = workerOib,
                OznSlijed = sequence,
                Pdv = vat.Length > 0 ? vat : null,
                Pnp = consumptionTax.Length > 0 ? consumptionTax : null,
                USustPdv = _settings.IsVatRegistered,
                NacinPlac = CroatiaPaymentMapper.ToMaes((FiscalPaymentType)originalReceipt.NacinPlacanja)
            };

            string receiptNumberHr =
                $"{newLocalReceiptNumber}/{_settings.BusinessPremise}/{_settings.CashRegister}";

            return new CroatiaBuiltInvoice
            {
                Invoice = invoice,
                LocalReceiptNumber = newLocalReceiptNumber,
                ReceiptNumberHr = receiptNumberHr,
                IssueDateTime = issueDateTime,
                Taxes = stornoTaxes,
                TotalAmount = stornoTotal
            };
        }

        public async Task<CroatiaBuiltInvoice> BuildSubsequentAsync( TblRacuni receipt, IReadOnlyList<TblRacunStavka> items, CancellationToken cancellationToken = default)
        {
            if (receipt == null)
                throw new ArgumentNullException(nameof(receipt));

            if (items == null || items.Count == 0)
                throw new FiscalException("Račun nema spremljenih stavki.");

            if (string.IsNullOrWhiteSpace(receipt.BrojRacunaHr))
                throw new FiscalException("Račun nema hrvatski broj računa.");

            if (string.IsNullOrWhiteSpace(receipt.Zki))
                throw new FiscalException("Račun nema originalni ZKI.");

            if (string.IsNullOrWhiteSpace(receipt.Radnik))
                throw new FiscalException("Račun nema spremljenog radnika.");

            if (!int.TryParse(receipt.Radnik, out int workerId))
                throw new FiscalException($"Neispravan ID radnika na računu {receipt.BrojRacuna}.");

            await using var db = new AppDbContext();

            TblRadnici? worker = await db.Radnici
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdRadnika == workerId, cancellationToken);

            if (worker == null)
                throw new FiscalException($"Radnik sa ID-em {workerId} nije pronađen.");

            if (string.IsNullOrWhiteSpace(worker.IB))
                throw new FiscalException($"Radnik {worker.Radnik} nema upisan OIB.");

            string[] numberParts = receipt.BrojRacunaHr.Split('/');

            if (numberParts.Length != 3 ||
                string.IsNullOrWhiteSpace(numberParts[0]) ||
                string.IsNullOrWhiteSpace(numberParts[1]) ||
                string.IsNullOrWhiteSpace(numberParts[2]))
            {
                throw new FiscalException($"Neispravan hrvatski broj računa: {receipt.BrojRacunaHr}");
            }

            bool isStorno = string.Equals(
                receipt.TipRacuna,
                "Storno",
                StringComparison.OrdinalIgnoreCase);

            decimal pnpRate = receipt.PorezNaPotrosnjuStopa ?? 0m;

            IReadOnlyList<CroatiaTaxSummary> taxes;

            if (isStorno)
            {
                var positiveItems = items
                    .Select(x => new TblRacunStavka
                    {
                        BrojRacuna = x.BrojRacuna,
                        Artikl = x.Artikl,
                        Sifra = x.Sifra,
                        Kolicina = Math.Abs(x.Kolicina ?? 0m),
                        Cijena = x.Cijena,
                        PoreskaStopa = x.PoreskaStopa,
                        PorezNaPotrosnju = x.PorezNaPotrosnju,
                        JedinicaMjere = x.JedinicaMjere,
                        VrstaArtikla = x.VrstaArtikla,
                        ArtiklNormativ = x.ArtiklNormativ
                    })
                    .ToList();

                IReadOnlyList<CroatiaTaxSummary> positiveTaxes =
                    await _taxCalculator.CalculateFromStoredItemsAsync(
                        positiveItems,
                        pnpRate,
                        cancellationToken);

                taxes = positiveTaxes
                    .Select(x => new CroatiaTaxSummary
                    {
                        Rate = x.Rate,
                        IsConsumptionTax = x.IsConsumptionTax,
                        BaseAmount = -Math.Abs(x.BaseAmount),
                        TaxAmount = -Math.Abs(x.TaxAmount)
                    })
                    .ToList();
            }
            else
            {
                taxes = await _taxCalculator.CalculateFromStoredItemsAsync(
                    items,
                    pnpRate,
                    cancellationToken);
            }

            var vat = taxes
                .Where(x => !x.IsConsumptionTax)
                .Select(CroatiaTaxCalculator.ToMaesTax)
                .ToArray();

            var consumptionTax = taxes
                .Where(x => x.IsConsumptionTax)
                .Select(CroatiaTaxCalculator.ToMaesTax)
                .ToArray();

            OznakaSlijednostiType sequence =
                string.Equals(_settings.SequenceSetting, "Poslovni prostor", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_settings.SequenceSetting, "Na nivou poslovnog prostora", StringComparison.OrdinalIgnoreCase)
                    ? OznakaSlijednostiType.P
                    : OznakaSlijednostiType.N;

            decimal totalAmount = items.Sum(x => (x.Kolicina ?? 0m) * (x.Cijena ?? 0m));

            if (isStorno)
                totalAmount = -Math.Abs(totalAmount);

            var invoice = new RacunType
            {
                BrRac = new BrojRacunaType
                {
                    BrOznRac = numberParts[0],
                    OznPosPr = numberParts[1],
                    OznNapUr = numberParts[2]
                },

                DatVrijeme = receipt.Datum.ToString(DateFormatLong, CultureInfo.InvariantCulture),
                IznosUkupno = totalAmount.ToString("F2", CultureInfo.InvariantCulture),
                NakDost = true,
                Oib = _settings.Oib,
                OibOper = worker.IB,
                OznSlijed = sequence,
                Pdv = vat.Length > 0 ? vat : null,
                Pnp = consumptionTax.Length > 0 ? consumptionTax : null,
                USustPdv = _settings.IsVatRegistered,
                NacinPlac = CroatiaPaymentMapper.ToMaes((FiscalPaymentType)receipt.NacinPlacanja),
                ZastKod = receipt.Zki
            };

            return new CroatiaBuiltInvoice
            {
                Invoice = invoice,
                LocalReceiptNumber = receipt.BrojRacuna,
                ReceiptNumberHr = receipt.BrojRacunaHr,
                IssueDateTime = receipt.Datum,
                Taxes = taxes,
                TotalAmount = totalAmount
            };
        }


    }
}