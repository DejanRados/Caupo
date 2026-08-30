namespace Caupo.Fiscal.Common
{
    public interface IFiscalService
    {
        Task<FiscalResult> IzdajRacunAsync(
            FiscalRequest request,
            CancellationToken cancellationToken = default);
    }
}
