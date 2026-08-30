namespace Caupo.Fiscal.Common
{
    public class FiscalException : Exception
    {
        public FiscalException(string message)
            : base(message)
        {
        }

        public FiscalException(
            string message,
            Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
