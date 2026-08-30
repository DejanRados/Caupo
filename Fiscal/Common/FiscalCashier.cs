namespace Caupo.Fiscal.Common
{
    public sealed class FiscalCashier
    {
        public int? Id { get; init; }
        public string? Name { get; init; }

        // OIB/JIB/IB operatera kada je potreban regionalnoj fiskalizaciji.
        public string? IdentificationNumber { get; init; }
    }
}
