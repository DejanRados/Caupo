using Caupo.Fiscal.Common;
using Caupo.Fiscal.Federation.Helpers;
using Caupo.Fiscal.Federation.Models;
using Caupo.Models;
using TringArtikal = global::Tring.Fiscal.Driver.Artikal;
using TringKupac = global::Tring.Fiscal.Driver.Kupac;
using TringRacun = global::Tring.Fiscal.Driver.Racun;
using TringRacunStavka = global::Tring.Fiscal.Driver.RacunStavka;

namespace Caupo.Fiscal.Federation
{
    public sealed class FederationInvoiceBuilder
    {
        public FederationBuiltInvoice Build(
            FiscalRequest request,
            int localReceiptNumber)
        {
            if(request == null)
                throw new ArgumentNullException(nameof(request));

            if(request.Items == null ||
               request.Items.Count == 0)
            {
                throw new FiscalException(
                    "Račun nema stavki.");
            }

            var invoice =
                new TringRacun();

            if(request.Buyer != null &&
               !request.Buyer.IsCitizen)
            {
                invoice.Kupac =
                    new TringKupac
                    {
                        IDbroj =
                            request.Buyer.TaxId
                            ?? string.Empty,

                        Naziv =
                            request.Buyer.Name
                            ?? string.Empty,

                        Adresa =
                            request.Buyer.Address
                            ?? string.Empty,

                        Grad =
                            request.Buyer.City
                            ?? string.Empty
                    };
            }

            foreach(Caupo.Models.RacunStavka item in request.Items)
            {
                decimal quantity =
                    item.Quantity ?? 0m;

                decimal unitPrice =
                    item.UnitPrice ?? 0m;

                if(quantity <= 0m)
                {
                    throw new FiscalException(
                        $"Neispravna količina za stavku '{item.Name}'.");
                }

                if(unitPrice < 0m)
                {
                    throw new FiscalException(
                        $"Neispravna cijena za stavku '{item.Name}'.");
                }

                var article =
                    new TringArtikal
                    {
                        Sifra =
                            item.Sifra
                            ?? string.Empty,

                        Naziv =
                            item.Name
                            ?? string.Empty,

                        JM =
                            item.JedinicaMjereName,

                        Stopa =
                            FederationTaxMapper.ToTring(
                                item.PoreskaStopa),

                        Cijena =
                            Convert.ToDouble(
                                unitPrice)
                    };

                var row =
                    new TringRacunStavka
                    {
                        artikal = article,

                        Kolicina =
                            Convert.ToDouble(
                                quantity),

                        Rabat = 0
                    };

                invoice.DodajStavkuRacuna(
                    row);
            }

            invoice.DodajVrstuPlacanja(
                FederationPaymentMapper.ToTring(
                    request.PaymentType),
                0);

            string cashier =
                request.Cashier.Name
                ?? string.Empty;

            invoice.Napomena =
                cashier +
                Environment.NewLine +
                "Int. broj računa: " +
                localReceiptNumber +
                Environment.NewLine +
                "Hvala na posjeti!";

            return new FederationBuiltInvoice
            {
                Invoice = invoice,

                LocalReceiptNumber =
                    localReceiptNumber,

                IssueDateTime =
                    DateTime.Now,

                TotalAmount =
                    request.TotalAmount
            };
        }
    }
}
