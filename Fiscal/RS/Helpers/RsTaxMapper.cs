using Caupo.Fiscal.Common;

namespace Caupo.Fiscal.RS.Helpers
{
    public static class RsTaxMapper
    {
        /// <summary>
        /// Čuva postojeće mapiranje Caupo -> RS LPFR:
        /// lokalni ID poreske stope se šalje kao fiskalna labela.
        /// Ovo je ista vrijednost koju je stari FiskalniRacun.Item
        /// slao kroz Labels[0].
        /// </summary>
        public static string ToFiscalLabel(
            int? localTaxRateId)
        {
            if(!localTaxRateId.HasValue ||
               localTaxRateId.Value <= 0)
            {
                throw new FiscalException(
                    "Stavka nema ispravnu poresku stopu " +
                    "za Republiku Srpsku.");
            }

            return localTaxRateId.Value.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
