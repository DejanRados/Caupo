using Caupo.Fiscal.Common;

namespace Caupo.Fiscal.Serbia.Helpers
{
    public static class SerbiaPaymentMapper
    {
        public static string ToApiValue(
            FiscalPaymentType paymentType)
        {
            return paymentType switch
            {
                FiscalPaymentType.Cash => "Cash",
                FiscalPaymentType.Card => "Card",
                FiscalPaymentType.Check => "Check",
                FiscalPaymentType.WireTransfer => "WireTransfer",
                FiscalPaymentType.Other => "Other",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(paymentType),
                    paymentType,
                    "Nepoznata vrsta plaćanja.")
            };
        }
    }
}
