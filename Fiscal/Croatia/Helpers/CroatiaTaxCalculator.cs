using Caupo.Data;
using Caupo.Fiscal.Common;
using Caupo.Fiscal.Croatia.Models;
using Caupo.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Fiscal.Croatia.Helpers
{
    public sealed class CroatiaTaxCalculator
    {
        private readonly CroatiaFiscalSettings _settings;

        public CroatiaTaxCalculator(
            CroatiaFiscalSettings settings)
        {
            _settings = settings;
        }

        public async Task<IReadOnlyList<CroatiaTaxSummary>>
            CalculateAsync(
                IReadOnlyList<RacunStavka> items,
                CancellationToken cancellationToken = default)
        {
            if(items == null || items.Count == 0)
                return Array.Empty<CroatiaTaxSummary>();

            await using var db = new AppDbContext();

            var taxRates = await db
                .Set<TblPoreskeStope>()
                .AsNoTracking()
                .ToDictionaryAsync(
                    x => x.IdStope,
                    x => x.Postotak ?? 0m,
                    cancellationToken);

            var totals =
                new Dictionary<
                    (decimal Rate, bool IsConsumptionTax),
                    (decimal BaseAmount, decimal TaxAmount)>();

            foreach(RacunStavka item in items)
            {
                decimal quantity = item.Quantity ?? 0m;
                decimal unitPrice = item.UnitPrice ?? 0m;

                if(quantity <= 0m)
                {
                    throw new FiscalException(
                        $"Neispravna količina za stavku '{item.Name}'.");
                }

                if(unitPrice < 0m)
                {
                    throw new FiscalException(
                        $"Neispravna cijena za stavku '{item.Name}'.");
                }

                decimal vatRate = 0m;

                if(item.PoreskaStopa.HasValue)
                {
                    if(!taxRates.TryGetValue(
                        item.PoreskaStopa.Value,
                        out vatRate))
                    {
                        throw new FiscalException(
                            $"Poreska stopa ID {item.PoreskaStopa.Value} " +
                            $"za stavku '{item.Name}' nije pronađena u bazi.");
                    }
                }

                decimal pnpRate =
                    _settings.ConsumptionTaxRate;

                decimal combinedRate =
                    vatRate + pnpRate;

                decimal taxPerUnit =
                    combinedRate > 0m
                        ? Math.Round(
                            unitPrice *
                            (combinedRate /
                             (100m + combinedRate)),
                            2,
                            MidpointRounding.AwayFromZero)
                        : 0m;

                decimal basePerUnit =
                    unitPrice - taxPerUnit;

                decimal vatPerUnit =
                    combinedRate > 0m
                        ? Math.Round(
                            taxPerUnit *
                            (vatRate / combinedRate),
                            2,
                            MidpointRounding.AwayFromZero)
                        : 0m;

                decimal pnpPerUnit =
                    combinedRate > 0m
                        ? Math.Round(
                            taxPerUnit *
                            (pnpRate / combinedRate),
                            2,
                            MidpointRounding.AwayFromZero)
                        : 0m;

                Add(
                    totals,
                    vatRate,
                    false,
                    basePerUnit * quantity,
                    vatPerUnit * quantity);

                if(pnpRate > 0m)
                {
                    Add(
                        totals,
                        pnpRate,
                        true,
                        basePerUnit * quantity,
                        pnpPerUnit * quantity);
                }
            }

            return totals
                .Where(x =>
                    x.Value.BaseAmount != 0m ||
                    x.Value.TaxAmount != 0m)
                .OrderBy(x => x.Key.IsConsumptionTax)
                .ThenBy(x => x.Key.Rate)
                .Select(x =>
                    new CroatiaTaxSummary
                    {
                        Rate = x.Key.Rate,
                        IsConsumptionTax =
                            x.Key.IsConsumptionTax,

                        BaseAmount = Math.Round(
                            x.Value.BaseAmount,
                            2,
                            MidpointRounding.AwayFromZero),

                        TaxAmount = Math.Round(
                            x.Value.TaxAmount,
                            2,
                            MidpointRounding.AwayFromZero)
                    })
                .ToList();
        }

        private static void Add(
            IDictionary<
                (decimal Rate, bool IsConsumptionTax),
                (decimal BaseAmount, decimal TaxAmount)> totals,
            decimal rate,
            bool isConsumptionTax,
            decimal baseAmount,
            decimal taxAmount)
        {
            var key =
                (rate, isConsumptionTax);

            if(totals.TryGetValue(
                key,
                out var existing))
            {
                totals[key] =
                    (
                        existing.BaseAmount + baseAmount,
                        existing.TaxAmount + taxAmount
                    );
            }
            else
            {
                totals[key] =
                    (baseAmount, taxAmount);
            }
        }

        public static MAES.Fiskal.PorezType ToMaesTax(CroatiaTaxSummary tax)
        {
            return new MAES.Fiskal.PorezType
            {
                Stopa = tax.Rate.ToString("F2", CultureInfo.InvariantCulture),
                Osnovica = tax.BaseAmount.ToString("F2", CultureInfo.InvariantCulture),
                Iznos = tax.TaxAmount.ToString("F2", CultureInfo.InvariantCulture)
            };
        }
    }
}
