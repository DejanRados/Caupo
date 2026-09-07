using Caupo.Fiscal.Common;
using Caupo.Fiscal.Serbia.Helpers;
using Caupo.Fiscal.Serbia.Models;
using System.Globalization;

namespace Caupo.Fiscal.Serbia
{
    public sealed class SerbiaInvoiceBuilder
    {
        private readonly SerbiaTaxMapper _taxMapper;
        private readonly SerbiaFiscalSettings _settings;

        public SerbiaInvoiceBuilder(SerbiaTaxMapper taxMapper, SerbiaFiscalSettings settings)
        {
            _taxMapper = taxMapper;
            _settings = settings;
        }

        public async Task<SerbiaInvoiceRequest> BuildAsync(
            FiscalRequest request,
            int localReceiptNumber,
            IReadOnlyCollection<SerbiaTaxRateEntry> activeRates,
            CancellationToken cancellationToken = default)
        {
            if (request.Items.Count == 0)
            {
                throw new FiscalException("Račun nema stavki.");
            }

            var items = new List<SerbiaInvoiceItem>();

            foreach (var item in request.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    throw new FiscalException("Stavka računa nema naziv.");
                }

                decimal quantity = item.Quantity ?? 0m;
                decimal unitPrice = item.UnitPrice ?? 0m;

                if (quantity <= 0m)
                {
                    throw new FiscalException($"Artikal '{item.Name}' ima neispravnu količinu.");
                }

                if (unitPrice < 0m)
                {
                    throw new FiscalException($"Artikal '{item.Name}' ima neispravnu cijenu.");
                }

                string label = await _taxMapper.GetLabelAsync(
                    item.PoreskaStopa,
                    activeRates,
                    cancellationToken);

                decimal total = Math.Round(
                    quantity * unitPrice,
                    2,
                    MidpointRounding.AwayFromZero);

                items.Add(
                    new SerbiaInvoiceItem
                    {
                        Name = item.Name.Trim(),
                        Quantity = quantity,
                        UnitPrice = unitPrice,

                        Labels = new List<string>
                        {
                            label
                        },

                        TotalAmount = total,

                        Unit = string.IsNullOrWhiteSpace(item.JedinicaMjereName)
                            ? null
                            : item.JedinicaMjereName
                    });
            }

            string? buyerId = request.Buyer == null ||
                              request.Buyer.IsCitizen
                ? null
                : request.Buyer.TaxId?.Trim();

            if (request.Buyer != null &&
                !request.Buyer.IsCitizen &&
                string.IsNullOrWhiteSpace(buyerId))
            {
                throw new FiscalException(
                    $"Kupac '{request.Buyer.Name}' nema poreski identifikacioni broj.");
            }

            string? referentDt = request.ReferentDocumentDateTime?
                .ToString(
                    "yyyy-MM-dd'T'HH:mm:ss.fffzzz",
                    CultureInfo.InvariantCulture);

            string invoiceType = string.Equals(
                request.InvoiceType,
                "Copy",
                StringComparison.OrdinalIgnoreCase)
                    ? "Copy"
                    : _settings.InvoiceType;

            return new SerbiaInvoiceRequest
            {
                DateAndTimeOfIssue = DateTimeOffset.Now,

                Cashier = request.Cashier.Name,

                InvoiceType = invoiceType,

                TransactionType = request.TransactionType,

                InvoiceNumber = localReceiptNumber.ToString(
                    CultureInfo.InvariantCulture),

                BuyerId = buyerId,

                ReferentDocumentNumber = request.ReferentDocumentNumber,

                ReferentDocumentDateTime = referentDt,

                Payment = new List<SerbiaPayment>
                {
                    new SerbiaPayment
                    {
                        Amount = request.TotalAmount,

                        PaymentType = SerbiaPaymentMapper.ToApiValue(
                            request.PaymentType)
                    }
                },

                Items = items
            };
        }
    }
}

