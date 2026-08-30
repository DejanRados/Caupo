using Caupo.Fiscal.Common;

namespace Caupo.Fiscal.RS.Helpers
{
    public static class RsPaymentMapper
    {
        public static string ToFiscal(
            FiscalPaymentType paymentType)
        {
            return paymentType switch
            {
                FiscalPaymentType.Cash => "Cash",
                FiscalPaymentType.Card => "Card",
                FiscalPaymentType.Check => "Check",
                FiscalPaymentType.WireTransfer => "WireTransfer",

                _ => throw new FiscalException(
                    "Izabrani način plaćanja nije podržan " +
                    "za Republiku Srpsku.")
            };
        }
    }
}
