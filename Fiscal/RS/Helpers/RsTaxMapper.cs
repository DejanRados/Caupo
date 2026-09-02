using Caupo.Fiscal.Common;

namespace Caupo.Fiscal.RepublikaSrpska.Helpers
{
    public static class RsTaxMapper
    {
        public static string ToFiscalLabel(int? localTaxRateId)
        {
            if (!localTaxRateId.HasValue || localTaxRateId.Value <= 0)
                throw new FiscalException("Stavka nema ispravnu poresku stopu za Republiku Srpsku.");

            return localTaxRateId.Value switch
            {
                1 => "\u0410", // А
                2 => "\u0415", // Е
                3 => "\u0408", // Ј
                4 => "\u041A", // К
                _ => throw new FiscalException($"Nepoznata poreska stopa za Republiku Srpsku: {localTaxRateId.Value}")
            };
        }
    }
}