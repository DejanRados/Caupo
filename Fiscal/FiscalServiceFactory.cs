using Caupo.Fiscal.Common;
using Caupo.Fiscal.Croatia;
using Caupo.Fiscal.Federation;
using Caupo.Fiscal.RS;
using Caupo.Fiscal.Serbia;

namespace Caupo.Fiscal
{
    public static class FiscalServiceFactory
    {
        public static IFiscalService Create(
            string? country)
        {
            string value =
                country?.Trim()
                ?? string.Empty;

            return value switch
            {
                "FederacijaBiH" =>
                    new FederationFiscalService(),

                "RepublikaSrpska" =>
                    new RsFiscalService(),

                "Hrvatska" =>
                    new CroatiaFiscalService(),

                "Srbija" =>
                    new SerbiaFiscalService(),

                _ => throw new FiscalException(
                    $"Nepoznata ili nepodešena država: '{value}'.")
            };
        }
    }
}
