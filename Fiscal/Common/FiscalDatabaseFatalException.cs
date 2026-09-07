namespace Caupo.Fiscal.Common
{
    public sealed class FiscalDatabaseFatalException : Exception
    {
        public FiscalDatabaseFatalException(string message) : base(message)
        {
        }

        public FiscalDatabaseFatalException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}