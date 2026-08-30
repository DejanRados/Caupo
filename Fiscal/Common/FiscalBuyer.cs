namespace Caupo.Fiscal.Common
{
    public sealed class FiscalBuyer
    {
        public string? Name { get; init; }
        public string? TaxId { get; init; }
        public string? Address { get; init; }
        public string? City { get; init; }

        public bool IsCitizen =>
            string.IsNullOrWhiteSpace(Name) ||
            string.Equals(
                Name,
                "Gradjani",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                Name,
                "Građani",
                StringComparison.OrdinalIgnoreCase);
    }
}
