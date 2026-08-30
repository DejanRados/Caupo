using Caupo.Cis;
using Caupo.Fiscal.Common;

namespace Caupo.Fiscal.Croatia.Helpers
{
    public static class CroatiaPaymentMapper
    {
        public static NacinPlacanjaType ToCis(
            FiscalPaymentType paymentType)
        {
            return paymentType switch
            {
                FiscalPaymentType.Cash =>
                    NacinPlacanjaType.G,

                FiscalPaymentType.Card =>
                    NacinPlacanjaType.K,

                FiscalPaymentType.Check =>
                    NacinPlacanjaType.C,

                FiscalPaymentType.WireTransfer =>
                    NacinPlacanjaType.T,

                FiscalPaymentType.Other =>
                    NacinPlacanjaType.O,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(paymentType),
                    paymentType,
                    "Nepoznat način plaćanja.")
            };
        }
    }
}
