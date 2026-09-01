using Caupo.Fiscal.Common;
using Caupo.Fiscal.Croatia.Helpers;
using Caupo.Fiscal.Croatia.Models;
using MAES.Fiskal;
using System.Globalization;

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
    }
}