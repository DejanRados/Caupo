using Caupo.Fiscal.Common;
using TringVrstePoreskihStopa =
    global::Tring.Fiscal.Driver.VrstePoreskihStopa;

namespace Caupo.Fiscal.Federation.Helpers
{
    public static class FederationTaxMapper
    {
        /// <summary>
        /// Zadržava mapiranje koje je korišteno u postojećem
        /// Caupo Tring kodu:
        /// 1 -> A, 2 -> E, 3 -> J, 4 -> K.
        /// RacunStavka.PoreskaStopa sadrži lokalni ID poreske stope.
        /// </summary>
        public static TringVrstePoreskihStopa ToTring(
            int? localTaxRateId)
        {
            return localTaxRateId switch
            {
                1 =>
                    TringVrstePoreskihStopa
                        .A_Nulta_stopa_za_neregistrirane_obveznike,

                2 =>
                    TringVrstePoreskihStopa
                        .E_Opca_poreska_stopa_PDV,

                3 =>
                    TringVrstePoreskihStopa
                        .J_Nedefinirana,

                4 =>
                    TringVrstePoreskihStopa
                        .K_Poreska_stopa_PDV_za_artikle_oslobodjene_PDV,

                _ => throw new FiscalException(
                    $"Nepoznata poreska stopa za Federaciju BiH. " +
                    $"Lokalni ID: {localTaxRateId?.ToString() ?? "(null)"}")
            };
        }
    }
}
