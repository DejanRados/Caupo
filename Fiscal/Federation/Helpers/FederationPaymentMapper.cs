using Caupo.Fiscal.Common;
using TringVrstePlacanja = global::Tring.Fiscal.Driver.VrstePlacanja;

namespace Caupo.Fiscal.Federation.Helpers
{
    public static class FederationPaymentMapper
    {
        public static TringVrstePlacanja ToTring(
            FiscalPaymentType paymentType)
        {
            return paymentType switch
            {
                FiscalPaymentType.Cash =>
                    TringVrstePlacanja.Gotovina,

                FiscalPaymentType.Card =>
                    TringVrstePlacanja.Kartica,

                FiscalPaymentType.Check =>
                    TringVrstePlacanja.Cek,

                FiscalPaymentType.WireTransfer =>
                    TringVrstePlacanja.Virman,

                _ => throw new FiscalException(
                    "Izabrani način plaćanja nije podržan " +
                    "na Tring fiskalnom printeru.")
            };
        }
    }
}
