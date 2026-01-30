using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Caupo.Cis
{
    public static partial class Fiscalization
    {
        #region Async Public API

        public static Task<RacunOdgovor> SendInvoiceAsync(
            RacunType invoice,
            X509Certificate2 certificate,
            FiscalizationEnvironment environment,
            bool enableLogging)
        {
            if(invoice == null)
                throw new ArgumentNullException (nameof (invoice));
            if(certificate == null)
                throw new ArgumentNullException (nameof (certificate));

            var request = new RacunZahtjev
            {
                Racun = invoice,
                Zaglavlje = GetRequestHeader ()
            };

            return SendInvoiceRequestAsync (request, certificate, environment, enableLogging);
        }

        public static Task<RacunOdgovor> SendInvoiceRequestAsync(
            RacunZahtjev request,
            X509Certificate2 certificate,
            FiscalizationEnvironment environment,
            bool enableLogging)
        {
            if(request == null)
                throw new ArgumentNullException (nameof (request));
            if(request.Racun == null)
                throw new ArgumentNullException (nameof (request.Racun));

            return SignAndSendAsync<RacunZahtjev, RacunOdgovor> (
                request,
                "racuni",
                certificate,
                environment,
                enableLogging);
        }

        public static Task<ProvjeraOdgovor> CheckInvoiceAsync(
            RacunType invoice,
            X509Certificate2 certificate,
            FiscalizationEnvironment environment,
            bool enableLogging)
        {
            if(invoice == null)
                throw new ArgumentNullException (nameof (invoice));
            if(certificate == null)
                throw new ArgumentNullException (nameof (certificate));

            var request = new ProvjeraZahtjev
            {
                Racun = invoice,
                Zaglavlje = GetRequestHeader ()
            };

            return CheckInvoiceRequestAsync (request, certificate, environment, enableLogging);
        }

        public static Task<ProvjeraOdgovor> CheckInvoiceRequestAsync(
            ProvjeraZahtjev request,
            X509Certificate2 certificate,
            FiscalizationEnvironment environment,
            bool enableLogging)
        {
            if(request == null)
                throw new ArgumentNullException (nameof (request));
            if(request.Racun == null)
                throw new ArgumentNullException (nameof (request.Racun));

            return SignAndSendAsync<ProvjeraZahtjev, ProvjeraOdgovor> (
                request,
                "provjera",
                certificate,
                environment,
                enableLogging);
        }

        public static async Task<string> SendEchoAsync(
            string echo,
            FiscalizationEnvironment environment,
            bool enableLogging)
        {
            if(string.IsNullOrWhiteSpace (echo))
                throw new ArgumentNullException (nameof (echo));

            using var service = new FiskalizacijaService (environment, enableLogging)
            {
                CheckResponseSignature = false
            };

            var soapBody = $"<echo>{System.Security.SecurityElement.Escape (echo)}</echo>";
            return await service.EchoAsync (soapBody).ConfigureAwait (false);
        }

        #endregion

        #region Core async logic

        private static async Task<TResponse> SignAndSendAsync<TRequest, TResponse>(
            TRequest request,
            string action,
            X509Certificate2 certificate,
            FiscalizationEnvironment environment,
            bool enableLogging)
            where TRequest : ICisRequest
            where TResponse : ICisResponse
        {
            try
            {
                Sign (request, certificate);

                var soapBody = Serialize (request);

                using var service = new FiskalizacijaService (environment, enableLogging);
                var responseXml = await service.SendAsync (action, soapBody)
                                               .ConfigureAwait (false);

                var response = Deserialize<TResponse> (responseXml);
                response.Request = request;

                ThrowOnErrors (response);
                return response;
            }
            catch(FiscalizationException)
            {
                throw;
            }
            catch(Exception ex)
            {
                throw new FiscalizationException ("Async fiscalization failed", ex);
            }
        }

        #endregion
    }
}
