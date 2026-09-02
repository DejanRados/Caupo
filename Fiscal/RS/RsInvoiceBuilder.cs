using Caupo.Fiscal.Common;
using Caupo.Fiscal.RepublikaSrpska.Helpers;
using Caupo.Fiscal.RS.Helpers;
using Caupo.Fiscal.RS.Models;
using Caupo.Models;
using System.Globalization;

namespace Caupo.Fiscal.RS
{
    public sealed class RsInvoiceBuilder
    {
        private readonly RsFiscalSettings _settings;

        public RsInvoiceBuilder(
            RsFiscalSettings settings)
        {
            _settings = settings;
        }

        public RsBuiltInvoice Build(
            FiscalRequest request,
            int localReceiptNumber)
        {
            if(request.Items == null ||
               request.Items.Count == 0)
            {
                throw new FiscalException(
                    "Račun nema stavki.");
            }

            int slipWidth =
                _settings.PaperWidth == "57"
                    ? 386
                    : 576;

            int normalFont =
                _settings.PaperWidth == "57"
                    ? 23
                    : 25;

            int largeFont =
                _settings.PaperWidth == "57"
                    ? 27
                    : 30;

            var invoice =
                new RsInvoiceRequest
                {
                    InvoiceType =
                        string.IsNullOrWhiteSpace(
                            request.InvoiceType)
                            ? "Normal"
                            : request.InvoiceType,

                    TransactionType =
                        string.IsNullOrWhiteSpace(
                            request.TransactionType)
                            ? "Sale"
                            : request.TransactionType,

                    Cashier =
                        request.Cashier.Name
                        ?? string.Empty,

                    Payments =
                        new List<RsPayment>
                        {
                            new RsPayment
                            {
                                Amount =
                                    request.TotalAmount,

                                PaymentType =
                                    RsPaymentMapper.ToFiscal(
                                        request.PaymentType)
                            }
                        }
                };

            if(request.Buyer != null &&
               !request.Buyer.IsCitizen)
            {
                if(string.IsNullOrWhiteSpace(
                    request.Buyer.TaxId) ||
                   request.Buyer.TaxId ==
                    "0000000000000")
                {
                    throw new FiscalException(
                        $"Ne postoje ispravni podaci o kupcu " +
                        $"'{request.Buyer.Name}'.");
                }

                invoice.BuyerId =
                    request.Buyer.TaxId;
            }

            if(request.IsRefund ||
               string.Equals(
                    invoice.InvoiceType,
                    "Copy",
                    StringComparison.OrdinalIgnoreCase))
            {
                invoice.ReferentDocumentNumber =
                    request.ReferentDocumentNumber;

                invoice.ReferentDocumentDateTime =
                    request.ReferentDocumentDateTime?
                        .ToString(
                            "yyyy-MM-dd'T'HH:mm:ss.fffzzz",
                            CultureInfo.InvariantCulture);
            }

            foreach(RacunStavka item in request.Items)
            {
                decimal quantity =
                    item.Quantity ?? 0m;

                decimal price =
                    item.UnitPrice ?? 0m;

                if(quantity <= 0m)
                    throw new FiscalException(
                        $"Neispravna količina za '{item.Name}'.");

                if(price < 0m)
                    throw new FiscalException(
                        $"Neispravna cijena za '{item.Name}'.");

                invoice.Items.Add(
                    new RsInvoiceItem
                    {
                        Name =
                            item.Name
                            ?? string.Empty,

                        Quantity =
                            quantity,

                        UnitPrice =
                            price,

                        TotalAmount =
                            Math.Round(
                                quantity * price,
                                2,
                                MidpointRounding.AwayFromZero),

                        Labels =
                            new List<string>
                            {
                                RsTaxMapper.ToFiscalLabel(
                                    item.PoreskaStopa)
                            },

                        Note =
                            item.Note
                    });
            }

            return new RsBuiltInvoice
            {
                LocalReceiptNumber =
                    localReceiptNumber,

                IssueDateTime =
                    DateTime.Now,

                Envelope =
                    new RsFiscalEnvelope
                    {
                        Print =
                            !_settings.ExternalPrinter,

                        RenderReceiptImage =
                            !_settings.ExternalPrinter,

                        ReceiptImageFormat =
                            "Png",

                        ReceiptSlipWidth =
                            slipWidth,

                        ReceiptSlipFontSizeNormal =
                            normalFont,

                        ReceiptSlipFontSizeLarge =
                            largeFont,

                        InvoiceRequest =
                            invoice
                    }
            };
        }
    }
}
