// Cis.Fiscalization v1.3.0 :: CIS WSDL v1.4 (2012-2017)
// https://github.com/tgrospic/Cis.Fiscalization
// Copyright (c) 2013-present Tomislav Grospic
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Caupo.Cis;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Caupo.Cis
{
    public static partial class Fiscalization
    {
        #region Constants

        public enum FiscalizationEnvironment
        {
            Demo,
            Production
        }

        public const string DATE_FORMAT_SHORT = "dd.MM.yyyy";
        public const string DATE_FORMAT_LONG = "dd.MM.yyyyTHH:mm:ss";

        public const string SERVICE_URL_PRODUCTION = "https://cis.porezna-uprava.hr:8449/FiskalizacijaService";
        public const string SERVICE_URL_DEMO = "https://cistest.apis-it.hr:8449/FiskalizacijaServiceTest";

        #endregion

        #region Service API

        /// <summary>
        /// Send invoice request
        /// </summary>
        /// <param name="request">Request to send</param>
        /// <param name="certificate">Signing certificate, optional if request is already signed</param>
        /// <param name="setupService">Function to set service settings</param>
        public static RacunOdgovor SendInvoiceRequest(RacunZahtjev request, X509Certificate2 certificate = null,
            Action<FiskalizacijaService> setupService = null)
        {
            Debug.WriteLine ($"[Fiscalization] SendInvoiceRequest called - Request ID: {request?.Id}");

            if(request == null)
                throw new ArgumentNullException (nameof (request));
            if(request.Racun == null)
                throw new ArgumentNullException ("request.Racun");

            try
            {
                return SignAndSendRequest<RacunZahtjev, RacunOdgovor> (request, x => x.racuni, certificate, setupService);
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[Fiscalization] SendInvoiceRequest failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Send invoice
        /// </summary>
        /// <param name="invoice">Invoice to send</param>
        /// <param name="certificate">Signing certificate</param>
        /// <param name="setupService">Function to set service settings</param>
        public static RacunOdgovor SendInvoice(RacunType invoice, X509Certificate2 certificate,
            Action<FiskalizacijaService> setupService = null)
        {
            Debug.WriteLine ($"[Fiscalization] SendInvoice called - OIB: {invoice?.Oib}");

            if(invoice == null)
                throw new ArgumentNullException (nameof (invoice));
            if(certificate == null)
                throw new ArgumentNullException (nameof (certificate));

            try
            {
                var request = new RacunZahtjev
                {
                    Racun = invoice,
                    Zaglavlje = Cis.Fiscalization.GetRequestHeader ()
                };

                Debug.WriteLine ($"[Fiscalization] Created request with ID: {request.Zaglavlje?.IdPoruke}");
                return SendInvoiceRequest (request, certificate, setupService);
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[Fiscalization] SendInvoice failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Check invoice request
        /// </summary>
        /// <param name="request">Request to send</param>
        /// <param name="certificate">Signing certificate, optional if request is already signed</param>
        /// <param name="setupService">Function to set service settings</param>
        public static ProvjeraOdgovor CheckInvoiceRequest(ProvjeraZahtjev request, X509Certificate2 certificate = null,
            Action<FiskalizacijaService> setupService = null)
        {
            Debug.WriteLine ($"[Fiscalization] CheckInvoiceRequest called - Request ID: {request?.Id}");

            if(request == null)
                throw new ArgumentNullException (nameof (request));
            if(request.Racun == null)
                throw new ArgumentNullException ("request.Racun");

            try
            {
                return SignAndSendRequest<ProvjeraZahtjev, ProvjeraOdgovor> (request, x => x.provjera, certificate, setupService);
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[Fiscalization] CheckInvoiceRequest failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Check invoice
        /// </summary>
        /// <param name="invoice">Invoice to check</param>
        /// <param name="certificate">Signing certificate</param>
        /// <param name="setupService">Function to set service settings</param>
        public static ProvjeraOdgovor CheckInvoice(RacunType invoice, X509Certificate2 certificate,
            Action<FiskalizacijaService> setupService = null)
        {
            Debug.WriteLine ($"[Fiscalization] CheckInvoice called - OIB: {invoice?.Oib}");

            if(invoice == null)
                throw new ArgumentNullException (nameof (invoice));
            if(certificate == null)
                throw new ArgumentNullException (nameof (certificate));

            try
            {
                var request = new ProvjeraZahtjev
                {
                    Racun = invoice,
                    Zaglavlje = Cis.Fiscalization.GetRequestHeader ()
                };

                Debug.WriteLine ($"[Fiscalization] Created check request with ID: {request.Zaglavlje?.IdPoruke}");
                return CheckInvoiceRequest (request, certificate, setupService);
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[Fiscalization] CheckInvoice failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Send echo request
        /// </summary>
        /// <param name="echo">String to send</param>
        /// <param name="setupService">Function to set service settings</param>
        public static string SendEcho(string echo, Action<FiskalizacijaService> setupService = null)
        {
            Debug.WriteLine ($"[Fiscalization] SendEcho called - Message: {echo}");

            if(echo == null)
                throw new ArgumentNullException (nameof (echo));

            try
            {
                // Create service endpoint
                var fs = new FiskalizacijaService ();
                if(setupService != null)
                    setupService (fs);

                // Response is not signed
                fs.CheckResponseSignature = false;

                Debug.WriteLine ($"[Fiscalization] Sending echo to: {fs.Url}");
                // Send request
                return fs.echo (echo);
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[Fiscalization] SendEcho failed: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Service Async API

        /// <summary>
        /// Send invoice request async
        /// </summary>
        /// <param name="request">Request to send</param>
        /// <param name="certificate">Signing certificate, optional if request is already signed</param>
        /// <param name="setupService">Function to set service settings</param>
        public static async Task<RacunOdgovor> SendInvoiceRequestAsync(RacunZahtjev request, X509Certificate2 certificate = null,
            Action<FiskalizacijaService> setupService = null)
        {
            Debug.WriteLine ($"[Fiscalization] SendInvoiceRequestAsync called - Request ID: {request?.Id}");

            if(request == null)
                throw new ArgumentNullException (nameof (request));
            if(request.Racun == null)
                throw new ArgumentNullException ("request.Racun");

            try
            {
                return await SignAndSendRequestAsync<RacunZahtjev, RacunOdgovor> (
                    request, x => x.RacuniAsync, certificate, setupService);
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[Fiscalization] SendInvoiceRequestAsync failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Send invoice async
        /// </summary>
        /// <param name="invoice">Invoice to send</param>
        /// <param name="certificate">Signing certificate</param>
        /// <param name="setupService">Function to set service settings</param>
        public static async Task<RacunOdgovor> SendInvoiceAsync(RacunType invoice, X509Certificate2 certificate,
            Action<FiskalizacijaService> setupService = null)
        {
            Debug.WriteLine ($"[Fiscalization] SendInvoiceAsync called - OIB: {invoice?.Oib}");

            if(invoice == null)
                throw new ArgumentNullException (nameof (invoice));
            if(certificate == null)
                throw new ArgumentNullException (nameof (certificate));

            try
            {
                var request = new RacunZahtjev
                {
                    Racun = invoice,
                    Zaglavlje = Cis.Fiscalization.GetRequestHeader ()
                };

                Debug.WriteLine ($"[Fiscalization] Created async request with ID: {request.Zaglavlje?.IdPoruke}");
                return await SendInvoiceRequestAsync (request, certificate, setupService);
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[Fiscalization] SendInvoiceAsync failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Check invoice request async
        /// </summary>
        /// <param name="request">Request to send</param>
        /// <param name="certificate">Signing certificate, optional if request is already signed</param>
        /// <param name="setupService">Function to set service settings</param>
        public static async Task<ProvjeraOdgovor> CheckInvoiceRequestAsync(ProvjeraZahtjev request, X509Certificate2 certificate = null,
            Action<FiskalizacijaService> setupService = null)
        {
            Debug.WriteLine ($"[Fiscalization] CheckInvoiceRequestAsync called - Request ID: {request?.Id}");

            if(request == null)
                throw new ArgumentNullException (nameof (request));
            if(request.Racun == null)
                throw new ArgumentNullException ("request.Racun");

            try
            {
                return await SignAndSendRequestAsync<ProvjeraZahtjev, ProvjeraOdgovor> (
                    request, x => x.ProvjeraAsync, certificate, setupService);
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[Fiscalization] CheckInvoiceRequestAsync failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Send invoice async
        /// </summary>
        /// <param name="invoice">Invoice to check</param>
        /// <param name="certificate">Signing certificate</param>
        /// <param name="setupService">Function to set service settings</param>
        public static async Task<ProvjeraOdgovor> CheckInvoiceAsync(RacunType invoice, X509Certificate2 certificate,
            Action<FiskalizacijaService> setupService = null)
        {
            Debug.WriteLine ($"[Fiscalization] CheckInvoiceAsync called - OIB: {invoice?.Oib}");

            if(invoice == null)
                throw new ArgumentNullException (nameof (invoice));
            if(certificate == null)
                throw new ArgumentNullException (nameof (certificate));

            try
            {
                var request = new ProvjeraZahtjev
                {
                    Racun = invoice,
                    Zaglavlje = Cis.Fiscalization.GetRequestHeader ()
                };

                Debug.WriteLine ($"[Fiscalization] Created async check request with ID: {request.Zaglavlje?.IdPoruke}");
                return await CheckInvoiceRequestAsync (request, certificate, setupService);
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[Fiscalization] CheckInvoiceAsync failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Send echo request async
        /// </summary>
        /// <param name="echo">String to send</param>
        /// <param name="setupService">Function to set service settings</param>
        public static async Task<string> SendEchoAsync(string echo, Action<FiskalizacijaService> setupService = null)
        {
            Debug.WriteLine ($"[Fiscalization] SendEchoAsync called - Message: {echo}");

            if(echo == null)
                throw new ArgumentNullException (nameof (echo));

            try
            {
                // Create service endpoint
                var fs = new FiskalizacijaService ();
                if(setupService != null)
                    setupService (fs);

                // Response is not signed
                fs.CheckResponseSignature = false;

                Debug.WriteLine ($"[Fiscalization] Sending async echo to: {fs.Url}");
                // Send request
                return await fs.EchoAsync (echo);
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[Fiscalization] SendEchoAsync failed: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Send methods (generic)

        /// <summary>
        /// Send request
        /// </summary>
        /// <typeparam name="TRequest">Type of service method argument</typeparam>
        /// <typeparam name="TResponse">Type of service method result</typeparam>
        /// <param name="request">Request to send</param>
        /// <param name="serviceMethod">Function to provide service method</param>
        /// <param name="certificate">Signing certificate</param>
        /// <param name="setupService">Function to set service settings</param>
        /// <returns>Service response object</returns>
        public static TResponse SignAndSendRequest<TRequest, TResponse>(TRequest request,
            Func<FiskalizacijaService, Func<TRequest, TResponse>> serviceMethod, X509Certificate2 certificate,
            Action<FiskalizacijaService> setupService = null)
            where TRequest : ICisRequest
            where TResponse : ICisResponse
        {
            Debug.WriteLine ($"[Fiscalization] SignAndSendRequest called - Type: {typeof (TRequest).Name}");

            if(request == null)
                throw new ArgumentNullException (nameof (request));
            if(serviceMethod == null)
                throw new ArgumentNullException (nameof (serviceMethod));
            if(certificate == null && request.Signature == null)
                throw new ArgumentNullException (nameof (certificate), "Certificate is required when request is not signed");

            try
            {
                // Create service endpoint
                var fs = new FiskalizacijaService ();
                fs.CheckResponseSignature = true;
                if(setupService != null)
                    setupService (fs);

                Debug.WriteLine ($"[Fiscalization] Service endpoint created: {fs.Url}");

                // Sign request
                Sign (request, certificate);

                // Send request to fiscalization service
                var method = serviceMethod (fs);
                Debug.WriteLine ($"[Fiscalization] Calling service method: {method.Method.Name}");
                var result = method (request);

                // Add reference to request object
                result.Request = request;

                ThrowOnResponseErrors (result);

                Debug.WriteLine ($"[Fiscalization] Request completed successfully");
                return result;
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[Fiscalization] SignAndSendRequest failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Send request async
        /// </summary>
        /// <typeparam name="TRequest">Type of service method argument</typeparam>
        /// <typeparam name="TResponse">Type of service method result</typeparam>
        /// <param name="request">Request to send</param>
        /// <param name="serviceMethod">Function to provide service method</param>
        /// <param name="certificate">Signing certificate</param>
        /// <param name="setupService">Function to set service settings</param>
        /// <returns>Service response object</returns>
        public static async Task<TResponse> SignAndSendRequestAsync<TRequest, TResponse>(TRequest request,
            Func<FiskalizacijaService, Func<TRequest, Task<TResponse>>> serviceMethod, X509Certificate2 certificate = null,
            Action<FiskalizacijaService> setupService = null)
            where TRequest : ICisRequest
            where TResponse : ICisResponse
        {
            Debug.WriteLine ($"[Fiscalization] SignAndSendRequestAsync called - Type: {typeof (TRequest).Name}");

            if(request == null)
                throw new ArgumentNullException (nameof (request));
            if(serviceMethod == null)
                throw new ArgumentNullException (nameof (serviceMethod));
            if(certificate == null && request.Signature == null)
                throw new ArgumentNullException (nameof (certificate), "Certificate is required when request is not signed");

            try
            {
                // Create service endpoint
                var fs = new FiskalizacijaService ();
                fs.CheckResponseSignature = true;
                if(setupService != null)
                    setupService (fs);

                Debug.WriteLine ($"[Fiscalization] Async service endpoint created: {fs.Url}");

                // Sign request
                Sign (request, certificate);

                // Send request to fiscalization service
                var method = serviceMethod (fs);
                Debug.WriteLine ($"[Fiscalization] Calling async service method: {method.Method.Name}");
                var result = await method (request);

                // Add reference to request object
                result.Request = request;

                ThrowOnResponseErrors (result);

                Debug.WriteLine ($"[Fiscalization] Async request completed successfully");
                return result;
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[Fiscalization] SignAndSendRequestAsync failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Send request (sync) using async service method
        /// TODO: Test
        /// </summary>
        /// <typeparam name="TRequest">Type of service method argument</typeparam>
        /// <typeparam name="TResponse">Type of service method result</typeparam>
        /// <param name="request">Request to send</param>
        /// <param name="serviceMethod">Function to provide service method</param>
        /// <param name="certificate">Signing certificate</param>
        /// <param name="setupService">Function to set service settings</param>
        /// <returns>Service response object</returns>
        public static TResponse SignAndSendRequest<TRequest, TResponse>(TRequest request,
            Func<FiskalizacijaService, Func<TRequest, Task<TResponse>>> serviceMethod, X509Certificate2 certificate = null,
            Action<FiskalizacijaService> setupService = null)
            where TRequest : ICisRequest
            where TResponse : ICisResponse
        {
            Debug.WriteLine ($"[Fiscalization] SignAndSendRequest (sync from async) called - Type: {typeof (TRequest).Name}");

            var task = SignAndSendRequestAsync (request, serviceMethod, certificate, setupService);

            try
            {
                // Wait for task to end
                task.Wait ();
                Debug.WriteLine ($"[Fiscalization] Sync from async request completed successfully");
                return task.Result;
            }
            catch(AggregateException aggEx)
            {
                Debug.WriteLine ($"[Fiscalization] Sync from async request failed with AggregateException: {aggEx.Message}");
                // We are sure that only one error exist
                throw aggEx.InnerException ?? aggEx;
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[Fiscalization] Sync from async request failed: {ex.Message}");
                throw;
            }
        }

        static void ThrowOnResponseErrors(ICisResponse response)
        {
            if(response?.Greske == null)
                return;

            var errors = response.Greske;

            // Special case for CheckInvoice service method
            // - returns error for success check WTF!!!!
            if(response is ProvjeraOdgovor)
            {
                // Remove "valid error" from response
                errors = errors.Where (x => x.SifraGreske != "v100").ToArray ();
                response.Greske = errors;
            }

            if(errors.Any ())
            {
                var strErrors = errors.Select (x => $"({x.SifraGreske}) {x.PorukaGreske}");
                var exMsg = string.Join ("\n", strErrors);

                Debug.WriteLine ($"[Fiscalization] Response contains errors: {exMsg}");
                throw new Exception ($"Fiscalization errors: {exMsg}");
            }
        }

        #endregion

        #region Helpers
        private static RSACryptoServiceProvider ConvertToRSACryptoServiceProvider(RSA rsa)
        {
            try
            {
                Debug.WriteLine ($"[Convert] Converting {rsa.GetType ().Name} to RSACryptoServiceProvider");

                // Eksportuj RSAParameters
                var parameters = rsa.ExportParameters (true);

                // Kreiraj RSACryptoServiceProvider
                var csp = new RSACryptoServiceProvider ();
                csp.ImportParameters (parameters);

                Debug.WriteLine ($"[Convert] Successfully converted to RSACryptoServiceProvider");
                return csp;
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[Convert] Error converting RSA: {ex.Message}");
                throw;
            }
        }
        class CisSignedXml : SignedXml
        {
            public CisSignedXml(XmlDocument doc) : base (doc) { }

            public override XmlElement GetIdElement(XmlDocument document, string idValue)
            {
                return document.SelectSingleNode (
                    $"//*[@Id='{idValue}']",
                    document.NameTable != null
                        ? new XmlNamespaceManager (document.NameTable)
                        : null
                ) as XmlElement;
            }
        }

        /// <summary>
        /// Sign request
        /// </summary>
        /// <param name="request">Request to sign</param>
        /// <param name="certificate">Signing certificate</param>
        /// 
        public static void Sign(ICisRequest request, X509Certificate2 certificate)
        {
            if(request == null)
                throw new ArgumentNullException ("request");
            if(request.Signature != null)
                return; // Already signed
            if(certificate == null)
                throw new ArgumentNullException ("certificate");

            // Check if ZKI is generated
            var invoiceRequest = request as RacunZahtjev;



            if(invoiceRequest != null && invoiceRequest.Racun.ZastKod == null)
                GenerateZki (invoiceRequest.Racun, certificate);

            request.Id = request.GetType ().Name;

            #region Sign request XML

            var ser = Serialize (request);
            var doc = new XmlDocument ();
            doc.PreserveWhitespace = true;
            doc.LoadXml (ser);

            string preSignedXml = doc.OuterXml;
            string preSignedFile = Path.Combine (Path.GetTempPath (), "fiskal_pre_signed.xml");
            File.WriteAllText (preSignedFile, preSignedXml);
            Console.WriteLine ($"[SIGN DEBUG] Pre-signed XML saved: {preSignedFile}");

            //SignedXml xml = new SignedXml (doc);
            SignedXml xml = new CisSignedXml (doc);
            // Umesto certificate.PrivateKey koristimo GetRSAPrivateKey()
            RSA rsaKey = certificate.GetRSAPrivateKey ();
            if(rsaKey == null)
                throw new Exception ("RSA privatni ključ nije dostupan!");

            Console.WriteLine ($"[DEBUG] RSA tip za potpisivanje: {rsaKey.GetType ()}");

            xml.SigningKey = rsaKey;
            xml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;

            var keyInfo = new KeyInfo ();
            var keyInfoData = new KeyInfoX509Data ();
            keyInfoData.AddCertificate (certificate);
            //keyInfoData.AddIssuerSerial (certificate.Issuer, certificate.GetSerialNumberString ());
            keyInfoData.AddIssuerSerial ( certificate.Issuer,  certificate.SerialNumber);

            keyInfo.AddClause (keyInfoData);
            xml.KeyInfo = keyInfo;

            var transforms = new Transform[]
            {
        new XmlDsigEnvelopedSignatureTransform(false),
        new XmlDsigExcC14NTransform(false)
            };

            Reference reference = new Reference ("#" + request.Id);
            foreach(var x in transforms)
                reference.AddTransform (x);
            xml.AddReference (reference);

            Console.WriteLine ("[DEBUG] Pozivam ComputeSignature()...");
            xml.ComputeSignature ();
            Console.WriteLine ("[DEBUG] ComputeSignature završen.");

            #endregion

            #region Fill request with signature data

            var s = xml.Signature;
            var certSerial = (X509IssuerSerial)keyInfoData.IssuerSerials[0];
            request.Signature = new SignatureType
            {
                SignedInfo = new SignedInfoType
                {
                    CanonicalizationMethod = new CanonicalizationMethodType { Algorithm = s.SignedInfo.CanonicalizationMethod },
                    SignatureMethod = new SignatureMethodType { Algorithm = s.SignedInfo.SignatureMethod },
                    Reference =
                        (from x in s.SignedInfo.References.OfType<Reference> ()
                         select new ReferenceType
                         {
                             URI = x.Uri,
                             Transforms =
                                (from t in transforms
                                 select new TransformType { Algorithm = t.Algorithm }).ToArray (),
                             DigestMethod = new DigestMethodType { Algorithm = x.DigestMethod },
                             DigestValue = x.DigestValue
                         }).ToArray ()
                },
                SignatureValue = new SignatureValueType { Value = s.SignatureValue },
                KeyInfo = new KeyInfoType
                {
                    ItemsElementName = new[] { ItemsChoiceType2.X509Data },
                    Items = new[]
                    {
                new X509DataType
                {
                    ItemsElementName = new[]
                    {
                        ItemsChoiceType.X509IssuerSerial,
                        ItemsChoiceType.X509Certificate
                    },
                    Items = new object[]
                    {
                        new X509IssuerSerialType
                        {
                            X509IssuerName = certSerial.IssuerName,
                            X509SerialNumber = certSerial.SerialNumber
                        },
                        certificate.RawData
                    }
                }
            }
                }
            };
            string finalXml = Serialize (request);
            string finalFile = Path.Combine (Path.GetTempPath (), "fiskal_final_signed.xml");
            File.WriteAllText (finalFile, finalXml);
            Console.WriteLine ($"[SIGN DEBUG] Final signed XML saved: {finalFile}");

            Console.WriteLine ("[DEBUG] SignatureType popunjen i dodan u request.");
            Console.WriteLine ("[DEBUG] SignatureType popunjen i dodan u request.");

            #endregion
        }

        private static TransformType[] GetTransformsFromChain(TransformChain chain)
        {
            var transforms = new List<TransformType> ();
            foreach(Transform transform in chain)
            {
                transforms.Add (new TransformType { Algorithm = transform.Algorithm });
            }
            return transforms.ToArray ();
        }

    
        /// <summary>
        /// Generate ZKI code
        /// </summary>
        /// <param name="invoice">Invoice to calculate and generate ZKI</param>
        /// <param name="certificate">Signing certificate</param>
        public static void GenerateZki(RacunType invoice, X509Certificate2 certificate)
        {
            Debug.WriteLine ($"[Fiscalization] GenerateZki called");

            if(certificate == null)
                throw new ArgumentNullException (nameof (certificate));
            if(invoice == null)
                throw new ArgumentNullException (nameof (invoice));

            try
            {
                StringBuilder sb = new StringBuilder ();
                sb.Append (invoice.Oib);
                sb.Append (invoice.DatVrijeme);
                sb.Append (invoice.BrRac?.BrOznRac ?? string.Empty);
                sb.Append (invoice.BrRac?.OznPosPr ?? string.Empty);
                sb.Append (invoice.BrRac?.OznNapUr ?? string.Empty);
                sb.Append (invoice.IznosUkupno);

                var dataToSign = sb.ToString ();
                Debug.WriteLine ($"[Fiscalization] Data to sign for ZKI: {dataToSign}");

                invoice.ZastKod = Fiscalization.SignAndHashMD5 (dataToSign, certificate);
                Debug.WriteLine ($"[Fiscalization] Generated ZKI: {invoice.ZastKod}");
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[Fiscalization] GenerateZki failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sign and hash with MD5 algorithm
        /// </summary>
        /// <param name="value">String to encrypt</param>
        /// <param name="certificate">Signing certificate</param>
        /// <returns>Encrypted string</returns>
        public static string SignAndHashMD5(string value, X509Certificate2 certificate)
        {
            if(value == null)
                throw new ArgumentNullException ("value");
            if(certificate == null)
                throw new ArgumentNullException ("certificate");

            // 1. Eksplicitno dobijte RSACng
            using(RSA rsa = certificate.GetRSAPrivateKey ())
            {
                if(rsa == null)
                    throw new Exception ("RSA privatni ključ nije dostupan!");

                // 2. Ako je RSACng, konvertujte ga u RSACryptoServiceProvider
                if(rsa is RSACng rsaCng)
                {
                    // Eksportujte KOMPLETNE parametre (sa privatnim ključem)
                    RSAParameters parameters = rsaCng.ExportParameters (true);

                    // Kreirajte RSACryptoServiceProvider i importujte parametre
                    using(var rsaCsp = new RSACryptoServiceProvider ())
                    {
                        rsaCsp.ImportParameters (parameters);

                        // 3. Potpišite podatke sa SHA1 - ISTO KAO U .NET FW 4.8
                        byte[] dataToSign = Encoding.ASCII.GetBytes (value);
                        byte[] signData = rsaCsp.SignData (dataToSign, new SHA1CryptoServiceProvider ());

                        // 4. Napravite MD5 hash od potpisa
                        using(MD5 md5 = MD5.Create ())
                        {
                            byte[] hash = md5.ComputeHash (signData);
                            return BitConverter.ToString (hash).Replace ("-", "").ToLower ();
                        }
                    }
                }
                else
                {
                    // Fallback ako nije RSACng (malo verovatno u .NET 8)
                    using(var rsaCsp = new RSACryptoServiceProvider ())
                    {
                        rsaCsp.ImportParameters (rsa.ExportParameters (true));
                        byte[] dataToSign = Encoding.ASCII.GetBytes (value);
                        byte[] signData = rsaCsp.SignData (dataToSign, new SHA1CryptoServiceProvider ());

                        using(MD5 md5 = MD5.Create ())
                        {
                            byte[] hash = md5.ComputeHash (signData);
                            return BitConverter.ToString (hash).Replace ("-", "").ToLower ();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check signature on request object
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static bool CheckSignature(ICisRequest request)
        {
            Debug.WriteLine ($"[Fiscalization] CheckSignature called for request type: {request?.GetType ().Name}");

            if(request == null)
                throw new ArgumentNullException (nameof (request));
            if(request.Signature == null)
                throw new ArgumentNullException ("Document not signed.");

            try
            {
                // Load signed XML
                var doc = new XmlDocument { PreserveWhitespace = true };
                var ser = Serialize (request);
                doc.LoadXml (ser);

                // Check signature
                var result = CheckSignatureXml (doc);
                Debug.WriteLine ($"[Fiscalization] Signature check result: {result}");
                return result;
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[Fiscalization] CheckSignature failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Check signature on signed XML document
        /// </summary>
        /// <param name="doc">Signed XML document</param>
        /// <returns></returns>
        public static bool CheckSignatureXml(XmlDocument doc)
        {
            Debug.WriteLine ($"[Fiscalization] CheckSignatureXml called");

            if(doc == null)
                throw new ArgumentNullException (nameof (doc));

            try
            {
                // Get signature property name with lambda expression
                Expression<Func<ICisRequest, SignatureType>> selector = x => x.Signature;
                var signatureNodeName = (selector.Body as MemberExpression)?.Member.Name ?? "Signature";

                // Get signature xml node
                var signatureNode = doc.GetElementsByTagName (signatureNodeName)[0] as XmlElement;
                if(signatureNode == null)
                {
                    Debug.WriteLine ($"[Fiscalization] Signature node not found");
                    return false;
                }

                // Signed xml (RacunOdgovor) inside SOAP XML document
                var signedElement = doc.DocumentElement?.FirstChild?.FirstChild as XmlElement;
                if(signedElement == null)
                {
                    Debug.WriteLine ($"[Fiscalization] Signed element not found");
                    return false;
                }

                SignedXml signedXml = new SignedXml (signedElement);

                // Load signature node
                signedXml.LoadXml (signatureNode);

                // Check signature
                var result = signedXml.CheckSignature ();
                Debug.WriteLine ($"[Fiscalization] XML signature check result: {result}");
                return result;
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[Fiscalization] CheckSignatureXml failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Serialize request data
        /// </summary>
        /// <param name="request">Request to serialize</param>
        /// <returns></returns>
       	public static string Serialize(ICisRequest request)
        {
            if(request == null)
                throw new ArgumentNullException ("request");

            // Fix empty arrays to null
            if(request is RacunZahtjev)
            {
                var rz = (RacunZahtjev)request;

                if(rz.Racun == null)
                    throw new ArgumentNullException ("request.Racun");

                var r = rz.Racun;
                Action<Array, Action> fixArray = (x, y) =>
                {
                    var isEmpty = x != null && !x.OfType<object> ().Any (x1 => x1 != null);
                    if(isEmpty)
                        y ();
                };
                fixArray (r.Naknade, () => r.Naknade = null);
                fixArray (r.OstaliPor, () => r.OstaliPor = null);
                fixArray (r.Pdv, () => r.Pdv = null);
                fixArray (r.Pnp, () => r.Pnp = null);
            }

            using(var ms = new MemoryStream ())
            {
                // Set namespace to root element
                var root = new XmlRootAttribute { Namespace = "http://www.apis-it.hr/fin/2012/types/f73", IsNullable = false };
                var ser = new XmlSerializer (request.GetType (), root);
                ser.Serialize (ms, request);

                return Encoding.UTF8.GetString (ms.ToArray ());
            }
        }

        /// <summary>
        /// Get default request header
        /// </summary>
        /// <returns></returns>
        public static ZaglavljeType GetRequestHeader()
        {
            var header = new ZaglavljeType
            {
                IdPoruke = Guid.NewGuid ().ToString (),
                DatumVrijeme = DateTime.Now.ToString (DATE_FORMAT_LONG)
            };

            Debug.WriteLine ($"[Fiscalization] Generated request header - ID: {header.IdPoruke}, Time: {header.DatumVrijeme}");
            return header;
        }

        #endregion
    }

    #region Interfaces

    /// <summary>
    /// Represent request data for CIS service
    /// </summary>
    public interface ICisRequest
    {
        string Id { get; set; }
        SignatureType Signature { get; set; }
    }

    /// <summary>
    /// Represent response data from CIS service
    /// </summary>
    public interface ICisResponse
    {
        GreskaType[] Greske { get; set; }
        ICisRequest Request { get; set; }
    }

    #endregion
}