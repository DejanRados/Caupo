using Caupo.Fiscal.Common;
using MAES.Fiskal;

namespace Caupo.Fiscal.Croatia.Helpers
{
    public static class CroatiaPaymentMapper
    {
        public static NacinPlacanjaType ToMaes(FiscalPaymentType paymentType)
        {
            return paymentType switch
            {
                FiscalPaymentType.Cash => NacinPlacanjaType.G,
                FiscalPaymentType.Card => NacinPlacanjaType.K,
                 FiscalPaymentType.WireTransfer => NacinPlacanjaType.T,
                FiscalPaymentType.Other => NacinPlacanjaType.O,
                _ => throw new ArgumentOutOfRangeException(nameof(paymentType), paymentType, "Nepoznat način plaćanja.")
            };
        }
    }
}