namespace Caupo.Fiscal.Common
{
    public sealed class FiscalOutcomeUnknownException : FiscalException
    {
        public string RequestId { get; }

        public FiscalOutcomeUnknownException(string message, string requestId, Exception? innerException = null) : base(message, innerException)
        {
            RequestId = requestId;
        }
    }
}