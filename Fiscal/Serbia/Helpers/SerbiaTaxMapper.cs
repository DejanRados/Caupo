using Caupo.Data;
using Caupo.Fiscal.Common;
using Caupo.Fiscal.Serbia.Models;
using Microsoft.EntityFrameworkCore;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Fiscal.Serbia.Helpers
{
    public sealed class SerbiaTaxMapper
    {
        public async Task<string> GetLabelAsync(
            int? localTaxRateId,
            IReadOnlyCollection<SerbiaTaxRateEntry> activeRates,
            CancellationToken cancellationToken = default)
        {
            if(!localTaxRateId.HasValue)
            {
                throw new FiscalException(
                    "Artikal nema definisanu poresku stopu.");
            }

            await using var db =
                new AppDbContext();

            TblPoreskeStope? localRate =
                await db.Set<TblPoreskeStope>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.IdStope == localTaxRateId.Value,
                        cancellationToken);

            if(localRate == null)
            {
                throw new FiscalException(
                    $"Poreska stopa ID {localTaxRateId.Value} " +
                    "nije pronađena u bazi.");
            }

            if(!localRate.Postotak.HasValue)
            {
                throw new FiscalException(
                    $"Poreska stopa '{localRate.Opis}' " +
                    "nema definisan postotak.");
            }

            decimal percentage =
                localRate.Postotak.Value;

            // Za obične artikle prvo tražimo VAT kategoriju.
            // Ovo izbjegava pogrešno mapiranje npr. 0% na N-TAX,
            // VAT-EXCL ili neku drugu posebnu kategoriju.
            var vatMatches =
                activeRates
                    .Where(x =>
                        string.Equals(
                            x.Category,
                            "VAT",
                            StringComparison.OrdinalIgnoreCase) &&
                        Math.Abs(x.Rate - percentage) < 0.0001m)
                    .ToList();

            if(vatMatches.Count == 1)
                return vatMatches[0].Label;

            // Ako nema VAT poklapanja, dozvoljavamo samo potpuno
            // jednoznačno poklapanje kroz sve aktivne kategorije.
            var allMatches =
                activeRates
                    .Where(x =>
                        Math.Abs(x.Rate - percentage) < 0.0001m)
                    .ToList();

            if(allMatches.Count == 1)
                return allMatches[0].Label;

            if(allMatches.Count > 1)
            {
                string labels =
                    string.Join(
                        ", ",
                        allMatches.Select(
                            x => $"{x.Category}:{x.Label}"));

                throw new FiscalException(
                    $"Poreska stopa {percentage:0.####}% " +
                    $"ima više mogućih oznaka u PFR-u ({labels}). " +
                    "Za ovu stopu mora se definisati eksplicitna " +
                    "Srbija poreska oznaka.");
            }

            throw new FiscalException(
                $"Za poresku stopu {percentage:0.####}% " +
                "nije pronađena aktivna oznaka u Srbija PFR-u.");
        }
    }
}
