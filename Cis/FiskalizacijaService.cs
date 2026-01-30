// Cis.Fiscalization v1.3.0 :: CIS WSDL v1.4 (2012-2017)
// https://github.com/tgrospic/Cis.Fiscalization
// Copyright (c) 2013-present Tomislav Grospic
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Caupo.Cis
{
    #region FiskalizacijaService partial implementation

    public partial class FiskalizacijaService : IDisposable
    {

 
        private bool _disposed = false;

        public void Dispose()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if(!_disposed)
            {
                if(disposing)
                {
                    _httpClient?.Dispose ();
                    _writeStream?.Dispose ();
                }
                _disposed = true;
            }
        }

        #region Fields

        public bool CheckResponseSignature { get; set; }
        public string Url { get; set; } = Fiscalization.SERVICE_URL_PRODUCTION;

        private SpyStream _writeStream;
        private HttpClient _httpClient;

        #endregion

        #region Constructor

        public FiskalizacijaService()
        {
            _httpClient = new HttpClient ();
            _httpClient.Timeout = TimeSpan.FromSeconds (30);
            Debug.WriteLine ($"[FiskalizacijaService] Created with URL: {Url}");
        }

        #endregion

        #region Async (TPL) version of main methods

        public Task<RacunOdgovor> RacuniAsync(RacunZahtjev request)
        {
            Debug.WriteLine ($"[FiskalizacijaService] RacuniAsync called");
            return Task.Run (() => racuni (request));
        }

        public Task<ProvjeraOdgovor> ProvjeraAsync(ProvjeraZahtjev request)
        {
            Debug.WriteLine ($"[FiskalizacijaService] ProvjeraAsync called");
            return Task.Run (() => provjera (request));
        }

        public Task<string> EchoAsync(string request)
        {
            Debug.WriteLine ($"[FiskalizacijaService] EchoAsync called");
            return Task.Run (() => echo (request));
        }

        #endregion

        #region Main methods (using HttpClient for .NET 8)

        /// <remarks/>
        public RacunOdgovor racuni(RacunZahtjev RacunZahtjev)
        {
            Debug.WriteLine ($"[FiskalizacijaService] racuni called");
            return SendSoapRequest<RacunZahtjev, RacunOdgovor> ("racuni", RacunZahtjev);
        }

        /// <remarks/>
        public string echo(string EchoRequest)
        {
            Debug.WriteLine ($"[FiskalizacijaService] echo called");
            return SendSoapRequest<string, string> ("echo", EchoRequest);
        }

        /// <remarks/>
        public ProvjeraOdgovor provjera(ProvjeraZahtjev ProvjeraZahtjev)
        {
            Debug.WriteLine ($"[FiskalizacijaService] provjera called");
            return SendSoapRequest<ProvjeraZahtjev, ProvjeraOdgovor> ("provjera", ProvjeraZahtjev);
        }

        #endregion

        #region SOAP request implementation

        private TResponse SendSoapRequest<TRequest, TResponse>(string operation, TRequest request)
        {
            Debug.WriteLine ($"[FiskalizacijaService] SendSoapRequest - Operation: {operation}, Type: {typeof (TRequest).Name}");

            try
            {
                // Create SOAP envelope
                string soapEnvelope = CreateSoapEnvelope (operation, request);
                Debug.WriteLine ($"[FiskalizacijaService] SOAP envelope created, length: {soapEnvelope.Length}");

                // DODAJ OVO: Spremi SOAP zahtjev u fajl za debagiranje
                var soapRequestPath = Path.Combine (Path.GetTempPath (), $"cis_soap_request_{DateTime.Now:yyyyMMdd_HHmmss}.xml");
                File.WriteAllText (soapRequestPath, soapEnvelope);
                Debug.WriteLine ($"[FiskalizacijaService] SOAP request saved to: {soapRequestPath}");

                // Create HTTP request
                var httpRequest = new HttpRequestMessage (HttpMethod.Post, Url)
                {
                    Content = new StringContent (soapEnvelope, Encoding.UTF8, "text/xml")
                };

                httpRequest.Headers.Add ("SOAPAction",
                    $"http://e-porezna.porezna-uprava.hr/fiskalizacija/2012/services/FiskalizacijaService/{operation}");

                // Send request
                Debug.WriteLine ($"[FiskalizacijaService] Sending HTTP request to: {Url}");
                var response = _httpClient.SendAsync (httpRequest).GetAwaiter ().GetResult ();

                var responseContent = response.Content.ReadAsStringAsync ().GetAwaiter ().GetResult ();
                Debug.WriteLine ($"[FiskalizacijaService] Received response, length: {responseContent.Length}");

                // DODAJ OVO: Spremi odgovor u fajl
                var soapResponsePath = Path.Combine (Path.GetTempPath (), $"cis_soap_response_{DateTime.Now:yyyyMMdd_HHmmss}.xml");
                File.WriteAllText (soapResponsePath, responseContent);
                Debug.WriteLine ($"[FiskalizacijaService] SOAP response saved to: {soapResponsePath}");

                // DODAJ OVO: Prikaži cijeli odgovor u debug konzoli
                Debug.WriteLine ($"[FiskalizacijaService] === FULL SOAP RESPONSE ===");
                Debug.WriteLine (responseContent);
                Debug.WriteLine ($"[FiskalizacijaService] === END SOAP RESPONSE ===");

                if(!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine ($"[FiskalizacijaService] HTTP error: {response.StatusCode}");

                    // ISPRAVKA OVOG DIJELA: Parsiraj SOAP grešku detaljno
                    try
                    {
                        var errorDoc = new XmlDocument ();
                        errorDoc.LoadXml (responseContent);

                        var nsManager = new XmlNamespaceManager (errorDoc.NameTable);
                        nsManager.AddNamespace ("soap", "http://schemas.xmlsoap.org/soap/envelope/");
                        nsManager.AddNamespace ("fin", "http://www.apis-it.hr/fin/2012/types/f73");

                        // Traži SOAP Fault
                        var faultNode = errorDoc.SelectSingleNode ("//soap:Fault", nsManager);
                        if(faultNode != null)
                        {
                            var faultCode = faultNode.SelectSingleNode ("faultcode")?.InnerText ?? "N/A";
                            var faultString = faultNode.SelectSingleNode ("faultstring")?.InnerText ?? "N/A";

                            Debug.WriteLine ($"[FiskalizacijaService] SOAP Fault Code: {faultCode}");
                            Debug.WriteLine ($"[FiskalizacijaService] SOAP Fault String: {faultString}");

                            // Traži detalje unutar Fault
                            var detailNode = faultNode.SelectSingleNode ("detail");
                            if(detailNode != null)
                            {
                                var cisError = detailNode.SelectSingleNode (".//fin:Greska", nsManager);
                                if(cisError != null)
                                {
                                    var sifra = cisError.SelectSingleNode ("fin:SifraGreske", nsManager)?.InnerText ?? "N/A";
                                    var poruka = cisError.SelectSingleNode ("fin:PorukaGreske", nsManager)?.InnerText ?? "N/A";
                                    Debug.WriteLine ($"[FiskalizacijaService] CIS Error Code: {sifra}");
                                    Debug.WriteLine ($"[FiskalizacijaService] CIS Error Message: {poruka}");

                                    throw new HttpRequestException ($"CIS Error {sifra}: {poruka}");
                                }
                                Debug.WriteLine ($"[FiskalizacijaService] Fault Detail: {detailNode.InnerXml}");
                            }
                        }
                        else
                        {
                            // Ako nema SOAP Fault, možda je direktna CIS greška
                            var cisError = errorDoc.SelectSingleNode ("//fin:Greska", nsManager);
                            if(cisError != null)
                            {
                                var sifra = cisError.SelectSingleNode ("fin:SifraGreske", nsManager)?.InnerText ?? "N/A";
                                var poruka = cisError.SelectSingleNode ("fin:PorukaGreske", nsManager)?.InnerText ?? "N/A";
                                Debug.WriteLine ($"[FiskalizacijaService] Direct CIS Error: {sifra} - {poruka}");
                                throw new HttpRequestException ($"CIS Error {sifra}: {poruka}");
                            }
                        }
                    }
                    catch(Exception parseEx)
                    {
                        Debug.WriteLine ($"[FiskalizacijaService] Error parsing SOAP fault: {parseEx.Message}");
                    }

                    throw new HttpRequestException ($"HTTP error: {response.StatusCode}");
                }

                // Parse SOAP response
                var result = ParseSoapResponse<TResponse> (responseContent);

                if(CheckResponseSignature && result is ICisResponse cisResponse)
                {
                    Debug.WriteLine ($"[FiskalizacijaService] Checking response signature");
                    var doc = new XmlDocument { PreserveWhitespace = true };
                    doc.LoadXml (responseContent);

                    var isValid = Fiscalization.CheckSignatureXml (doc);
                    if(!isValid)
                        throw new ApplicationException ("Soap response signature not valid.");
                }

                Debug.WriteLine ($"[FiskalizacijaService] SOAP request completed successfully");
                return result;
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[FiskalizacijaService] SendSoapRequest failed: {ex.Message}");
                throw;
            }
        }

        private string CreateSoapEnvelope<T>(string operation, T request)
        {
            var serializer = new XmlSerializer (typeof (T), "http://www.apis-it.hr/fin/2012/types/f73");

            using(var sw = new StringWriter ())
            {
                var ns = new XmlSerializerNamespaces ();
                ns.Add ("soap", "http://schemas.xmlsoap.org/soap/envelope/");
                ns.Add ("fin", "http://www.apis-it.hr/fin/2012/types/f73");

                using(var xw = XmlWriter.Create (sw, new XmlWriterSettings { OmitXmlDeclaration = true }))
                {
                    xw.WriteStartElement ("soap", "Envelope", "http://schemas.xmlsoap.org/soap/envelope/");
                    xw.WriteAttributeString ("xmlns", "fin", null, "http://www.apis-it.hr/fin/2012/types/f73");

                    xw.WriteStartElement ("soap", "Body", null);
                    serializer.Serialize (xw, request, ns);
                    xw.WriteEndElement (); // Body
                    xw.WriteEndElement (); // Envelope
                }

                return sw.ToString ();
            }
        }

        private T ParseSoapResponse<T>(string soapResponse)
        {
            var doc = XDocument.Parse (soapResponse);
            var body = doc.Descendants (XName.Get ("Body", "http://schemas.xmlsoap.org/soap/envelope/")).FirstOrDefault ();

            if(body == null)
                throw new XmlException ("SOAP Body not found in response");

            var responseElement = body.Elements ().FirstOrDefault ();
            if(responseElement == null)
                throw new XmlException ("No response element found in SOAP Body");

            var serializer = new XmlSerializer (typeof (T), "http://www.apis-it.hr/fin/2012/types/f73");
            using(var reader = responseElement.CreateReader ())
            {
                return (T)serializer.Deserialize (reader);
            }
        }

        #endregion

        #region SOAP interceptor, logging
        /*
        partial void LogResponseRaw(XmlDocument request, XmlDocument response)
        {
            // Implementation for logging if needed
            Debug.WriteLine ($"[FiskalizacijaService] LogResponseRaw - Request length: {request?.OuterXml?.Length}, Response length: {response?.OuterXml?.Length}");
        }
        */
        /// <summary>
        /// Custom stream to monitor other writeable stream.
        /// Depends on Flush method
        /// </summary>
        class SpyStream : MemoryStream
        {
            Stream _writeStream = null;
            long _lastPosition = 0;

            public SpyStream(Stream writeStream)
            {
                _writeStream = writeStream;
            }

            public override void Flush()
            {
                Seek (_lastPosition, SeekOrigin.Begin);

                // Write to underlying stream
                CopyTo_ (_writeStream);

                _lastPosition = Position;
            }

            public override void Close()
            {
                base.Close ();
                _writeStream?.Close ();
            }

            void CopyTo_(Stream destination, int bufferSize = 4096)
            {
                byte[] buffer = new byte[bufferSize];
                int read;
                while((read = Read (buffer, 0, buffer.Length)) != 0)
                    destination.Write (buffer, 0, read);
            }
        }

        #endregion
    }

    #region Partial class implementations with interfaces

    public partial class RacunZahtjev : ICisRequest { }

    public partial class RacunOdgovor : ICisResponse
    {
        [XmlIgnore]
        public ICisRequest Request { get; set; }
    }

    public partial class ProvjeraZahtjev : ICisRequest { }

    public partial class ProvjeraOdgovor : ICisResponse
    {
        [XmlIgnore]
        public ICisRequest Request { get; set; }
    }

    public partial class RacunType
    {
        /// <summary>
        /// Generate ZKI code
        /// </summary>
        /// <param name="certificate">Signing certificate</param>
        [Obsolete ("Use static method Fiscalization.GenerateZki instead.")]
        public void GenerateZki(X509Certificate2 certificate)
        {
            Fiscalization.GenerateZki (this, certificate);
        }
    }

    #endregion

    #endregion
}