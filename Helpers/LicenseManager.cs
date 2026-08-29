using Caupo.Properties;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Caupo.Helpers
{
    public static class LicenseManager
    {
        // =====================================================
        // API
        // =====================================================

        private const string ApiBaseUrl =
            "https://caupo.app/caupo-licensing/api/endpoints/";

        private const string ActivateEndpoint =
            ApiBaseUrl + "activate.php";


        // =====================================================
        // SERVER CODES
        // =====================================================

        public const string CodeLicenseActivated =
            "LICENSE_ACTIVATED";

        public const string CodeAlreadyActivated =
            "ALREADY_ACTIVATED";

        public const string CodeLicenseValid =
            "LICENSE_VALID";

        public const string CodeLicenseNotFound =
            "LICENSE_NOT_FOUND";

        public const string CodeLicenseInactive =
            "LICENSE_INACTIVE";

        public const string CodeLicenseExpired =
            "LICENSE_EXPIRED";

        public const string CodeActivationLimitReached =
            "ACTIVATION_LIMIT_REACHED";

        public const string CodeLicenseNotActivated =
            "LICENSE_NOT_ACTIVATED";

        public const string CodeServerError =
            "SERVER_ERROR";


        // =====================================================
        // LOCAL CLIENT CODES
        // =====================================================

        public const string CodeNetworkError =
            "NETWORK_ERROR";

        public const string CodeValidationError =
            "VALIDATION_ERROR";


        // =====================================================
        // HTTP CLIENT
        // =====================================================

        private static HttpClient CreateHttpClient(
            int timeoutSeconds)
        {
            return new HttpClient
            {
                Timeout =
                    TimeSpan.FromSeconds (
                        timeoutSeconds)
            };
        }


        // =====================================================
        // ACTIVATE LICENSE
        //
        // Ova metoda ostaje zbog kompatibilnosti sa postojećim
        // kodom koji očekuje samo true / false.
        // =====================================================

        public static async Task<bool> ActivateLicense(
            string licenseKey)
        {
            ActivationResponse result =
                await ActivateLicenseDetailed (
                    licenseKey);

            return result.Success;
        }


        // =====================================================
        // ACTIVATE LICENSE - DETAILED
        // =====================================================

        public static async Task<ActivationResponse>
            ActivateLicenseDetailed(
                string licenseKey)
        {
            licenseKey =
                licenseKey?.Trim ()
                ?? string.Empty;


            // -------------------------------------------------
            // LOCAL VALIDATION
            // -------------------------------------------------

            if(string.IsNullOrWhiteSpace (
                licenseKey))
            {
                return new ActivationResponse
                {
                    Success = false,
                    Code = CodeLicenseNotActivated,
                    Message =
                        "Unesite licencni ključ."
                };
            }


            string hardwareFingerprint;


            try
            {
                hardwareFingerprint =
                    HardwareHelper
                        .GetHardwareFingerprint ();
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[LICENSE] Hardware fingerprint error: {ex}");


                return new ActivationResponse
                {
                    Success = false,
                    Code = CodeValidationError,
                    Message =
                        "Nije moguće identificirati računar."
                };
            }


            if(string.IsNullOrWhiteSpace (
                hardwareFingerprint))
            {
                return new ActivationResponse
                {
                    Success = false,
                    Code = CodeValidationError,
                    Message =
                        "Nije moguće identificirati računar."
                };
            }


            Debug.WriteLine (
                "[LICENSE] Pokrećem aktivaciju licence.");

            Debug.WriteLine (
                $"[LICENSE] Hardware fingerprint: " +
                $"{hardwareFingerprint}");


            // -------------------------------------------------
            // REQUEST
            // -------------------------------------------------

            var request =
                new
                {
                    license_key =
                        licenseKey,

                    hardware_fingerprint =
                        hardwareFingerprint,

                    action =
                        "activate"
                };


            ActivationResponse result =
                await SendRequestAsync (
                    request,
                    30);


            // -------------------------------------------------
            // SAVE SUCCESS
            // -------------------------------------------------

            if(result.Success)
            {
                SaveLicenseSettings (
                    licenseKey,
                    hardwareFingerprint,
                    result);


                Debug.WriteLine (
                    $"[LICENSE] Aktivacija uspješna. " +
                    $"Code={result.Code}");
            }
            else
            {
                Debug.WriteLine (
                    $"[LICENSE] Aktivacija odbijena. " +
                    $"Code={result.Code}, " +
                    $"Message={result.Message}");
            }


            return result;
        }


        // =====================================================
        // VALIDATE ON STARTUP
        // =====================================================

        public static async Task<ActivationResponse?>
            ValidateOnStartup()
        {
            try
            {


                Debug.WriteLine ("[LICENSE] Startup provjera licence.");

                Debug.WriteLine (
                    $"[LICENSE] ValidateOnStartup POZVAN. Key='{Settings.Default.Key}'");

           


                string licenseKey =
                    Settings.Default.Key?.Trim ()
                    ?? string.Empty;


                string savedFingerprint =
                    Settings.Default
                        .HardwareFingerprint?
                        .Trim ()
                    ?? string.Empty;


                // -------------------------------------------------
                // NEMA LOKALNOG KLJUČA
                //
                // Nova instalacija ili reinstalacija kod koje
                // Settings više ne postoje.
                // -------------------------------------------------

                if(string.IsNullOrWhiteSpace (
                    licenseKey))
                {
                    Debug.WriteLine (
                        "[LICENSE] Lokalni licencni ključ ne postoji.");


                    return new ActivationResponse
                    {
                        Success = false,
                        Code = CodeLicenseNotActivated,
                        Message =
                            "Licenca nije aktivirana na ovom računaru."
                    };
                }


                // -------------------------------------------------
                // CURRENT HARDWARE
                // -------------------------------------------------

                string currentFingerprint;


                try
                {
                    currentFingerprint =
                        HardwareHelper
                            .GetHardwareFingerprint ();
                }
                catch(Exception ex)
                {
                    Debug.WriteLine (
                        $"[LICENSE] Hardware fingerprint error: {ex}");


                    return new ActivationResponse
                    {
                        Success = false,
                        Code = CodeValidationError,
                        Message =
                            "Nije moguće identificirati računar."
                    };
                }


                if(string.IsNullOrWhiteSpace (
                    currentFingerprint))
                {
                    return new ActivationResponse
                    {
                        Success = false,
                        Code = CodeValidationError,
                        Message =
                            "Nije moguće identificirati računar."
                    };
                }


                // -------------------------------------------------
                // NEMA SAČUVANOG FINGERPRINTA
                // -------------------------------------------------

                if(string.IsNullOrWhiteSpace (
                    savedFingerprint))
                {
                    Debug.WriteLine (
                        "[LICENSE] Lokalni hardware fingerprint ne postoji.");


                    return new ActivationResponse
                    {
                        Success = false,
                        Code = CodeLicenseNotActivated,
                        Message =
                            "Licencu je potrebno ponovo aktivirati."
                    };
                }


                // -------------------------------------------------
                // LOKALNI HARDWARE SE PROMIJENIO
                //
                // Server će pri pokušaju aktivacije odlučiti
                // da li postoji slobodan broj aktivacija.
                // -------------------------------------------------

                if(!string.Equals (
                    currentFingerprint,
                    savedFingerprint,
                    StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine (
                        "[LICENSE] Hardware fingerprint se razlikuje.");


                    return new ActivationResponse
                    {
                        Success = false,
                        Code = CodeLicenseNotActivated,
                        Message =
                            "Licenca nije aktivirana na ovom računaru."
                    };
                }


                // -------------------------------------------------
                // ONLINE VALIDATION
                // -------------------------------------------------

                ActivationResponse result =
                    await ValidateOnline (
                        licenseKey,
                        currentFingerprint);


                // -------------------------------------------------
                // REFRESH LOCAL DATA
                // -------------------------------------------------

                if(result.Success)
                {
                    SaveLicenseSettings (
                        licenseKey,
                        currentFingerprint,
                        result);


                    Debug.WriteLine (
                        $"[LICENSE] Startup validacija uspješna. " +
                        $"Code={result.Code}");
                }
                else
                {
                    Debug.WriteLine (
                        $"[LICENSE] Startup validacija nije uspjela. " +
                        $"Code={result.Code}, " +
                        $"Message={result.Message}");
                }


                return result;
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[LICENSE] Startup validation error: {ex}");


                return new ActivationResponse
                {
                    Success = false,
                    Code = CodeValidationError,
                    Message =
                        "Dogodila se greška prilikom provjere licence."
                };
            }
        }


        // =====================================================
        // ONLINE VALIDATION
        // =====================================================

        public static async Task<ActivationResponse>
            ValidateOnline(
                string licenseKey,
                string hardwareFingerprint)
        {
            licenseKey =
                licenseKey?.Trim ()
                ?? string.Empty;


            hardwareFingerprint =
                hardwareFingerprint?.Trim ()
                ?? string.Empty;


            if(string.IsNullOrWhiteSpace (
                licenseKey))
            {
                return new ActivationResponse
                {
                    Success = false,
                    Code = CodeLicenseNotActivated,
                    Message =
                        "Licencni ključ nije pronađen."
                };
            }


            if(string.IsNullOrWhiteSpace (
                hardwareFingerprint))
            {
                return new ActivationResponse
                {
                    Success = false,
                    Code = CodeValidationError,
                    Message =
                        "Hardware fingerprint nije pronađen."
                };
            }


            var request =
                new
                {
                    license_key =
                        licenseKey,

                    hardware_fingerprint =
                        hardwareFingerprint,

                    action =
                        "validate"
                };


            return await SendRequestAsync (
                request,
                10);
        }


        // =====================================================
        // SEND REQUEST
        //
        // VAŽNO:
        //
        // JSON odgovor se parsira BEZ OBZIRA na HTTP status.
        //
        // Server legitimno koristi:
        //
        // 403 -> LICENSE_EXPIRED
        // 403 -> LICENSE_INACTIVE
        // 403 -> ACTIVATION_LIMIT_REACHED
        // 403 -> LICENSE_NOT_ACTIVATED
        // 404 -> LICENSE_NOT_FOUND
        // 500 -> SERVER_ERROR
        //
        // Zato IsSuccessStatusCode ne smije biti korišten prije
        // parsiranja JSON-a.
        // =====================================================

        private static async Task<ActivationResponse>
            SendRequestAsync(
                object request,
                int timeoutSeconds)
        {
            try
            {
                string json =
                    JsonSerializer.Serialize (
                        request);


                using HttpClient client =
                    CreateHttpClient (
                        timeoutSeconds);


                using var content =
                    new StringContent (
                        json,
                        Encoding.UTF8,
                        "application/json");


                Debug.WriteLine (
                    $"[LICENSE] POST {ActivateEndpoint}");

                Debug.WriteLine (
                    $"[LICENSE] Request: {json}");


                using HttpResponseMessage response =
                    await client.PostAsync (
                        ActivateEndpoint,
                        content);


                string responseBody =
                    await response.Content
                        .ReadAsStringAsync ();


                Debug.WriteLine (
                    $"[LICENSE] HTTP " +
                    $"{(int)response.StatusCode} " +
                    $"{response.StatusCode}");


                Debug.WriteLine (
                    $"[LICENSE] Response: " +
                    $"{responseBody}");


                // -------------------------------------------------
                // EMPTY RESPONSE
                // -------------------------------------------------

                if(string.IsNullOrWhiteSpace (
                    responseBody))
                {
                    return new ActivationResponse
                    {
                        Success = false,

                        Code =
                            response.IsSuccessStatusCode
                                ? CodeValidationError
                                : response.StatusCode >=
                                  System.Net.HttpStatusCode.InternalServerError
                                    ? CodeServerError
                                    : CodeValidationError,

                        Message =
                            "Server nije vratio odgovor."
                    };
                }


                // -------------------------------------------------
                // PARSE JSON
                // -------------------------------------------------

                ActivationResponse? result;


                try
                {
                    result =
                        JsonSerializer.Deserialize
                            <ActivationResponse> (
                                responseBody,
                                JsonOptions);
                }
                catch(JsonException ex)
                {
                    Debug.WriteLine (
                        $"[LICENSE] JSON parse error: " +
                        $"{ex.Message}");


                    return new ActivationResponse
                    {
                        Success = false,
                        Code = CodeValidationError,
                        Message =
                            "Server je vratio neispravan odgovor."
                    };
                }


                if(result == null)
                {
                    return new ActivationResponse
                    {
                        Success = false,
                        Code = CodeValidationError,
                        Message =
                            "Server je vratio neispravan odgovor."
                    };
                }


                // -------------------------------------------------
                // SERVER NIJE VRATIO CODE
                // -------------------------------------------------

                if(string.IsNullOrWhiteSpace (
                    result.Code))
                {
                    result.Code =
                        response.IsSuccessStatusCode
                            ? CodeValidationError
                            : response.StatusCode >=
                              System.Net.HttpStatusCode.InternalServerError
                                ? CodeServerError
                                : CodeValidationError;
                }


                // -------------------------------------------------
                // SERVER NIJE VRATIO MESSAGE
                // -------------------------------------------------

                if(string.IsNullOrWhiteSpace (
                    result.Message))
                {
                    result.Message =
                        result.Success
                            ? "Operacija je uspješno završena."
                            : "Operacija sa licencom nije uspjela.";
                }


                return result;
            }

            // =====================================================
            // TIMEOUT
            // =====================================================

            catch(TaskCanceledException ex)
            {
                Debug.WriteLine (
                    $"[LICENSE] Timeout: {ex.Message}");


                return new ActivationResponse
                {
                    Success = false,
                    Code = CodeNetworkError,
                    Message =
                        "Server za licence trenutno nije dostupan."
                };
            }

            // =====================================================
            // NETWORK
            // =====================================================

            catch(HttpRequestException ex)
            {
                Debug.WriteLine (
                    $"[LICENSE] Network error: {ex.Message}");


                return new ActivationResponse
                {
                    Success = false,
                    Code = CodeNetworkError,
                    Message =
                        "Nije moguće povezati se sa serverom za licence."
                };
            }

            // =====================================================
            // OTHER CLIENT ERROR
            // =====================================================

            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[LICENSE] Request error: {ex}");


                return new ActivationResponse
                {
                    Success = false,
                    Code = CodeValidationError,
                    Message =
                        "Dogodila se greška prilikom provjere licence."
                };
            }
        }


        // =====================================================
        // SAVE LICENSE SETTINGS
        // =====================================================

        private static void SaveLicenseSettings(
            string licenseKey,
            string hardwareFingerprint,
            ActivationResponse result)
        {
            Settings.Default.Key =
                licenseKey;


            Settings.Default.HardwareFingerprint =
                hardwareFingerprint;


            // -------------------------------------------------
            // LICENSE DATA
            //
            // Ne brišemo postojeće lokalne podatke ako ih server
            // iz nekog razloga nije vratio.
            // -------------------------------------------------

            if(result.License != null)
            {
                Settings.Default.LicenseType =
                    result.License.Type
                    ?? string.Empty;


                Settings.Default.CompanyName =
                    result.License.CompanyName
                    ?? string.Empty;


                Settings.Default.ExpirationDate =
                    result.License.ExpirationDate
                    ?? string.Empty;
            }


            // -------------------------------------------------
            // LAST SUCCESSFUL SERVER CHECK
            //
            // Trenutno koristimo postojeći LastActivation setting.
            // Vrijednost se osvježava i nakon uspješne validacije.
            // -------------------------------------------------

            Settings.Default.LastActivation =
                DateTime.Now.ToString (
                    "O");


            Settings.Default.Save ();


            Debug.WriteLine (
                "[LICENSE] Lokalni podaci licence sačuvani.");
        }


        // =====================================================
        // JSON OPTIONS
        // =====================================================

        private static readonly JsonSerializerOptions JsonOptions =
            new ()
            {
                PropertyNameCaseInsensitive =
                    true
            };


        // =====================================================
        // RESPONSE MODEL
        // =====================================================

        public class ActivationResponse
        {
            [JsonPropertyName ("success")]
            public bool Success
            {
                get;
                set;
            }


            [JsonPropertyName ("code")]
            public string? Code
            {
                get;
                set;
            }


            [JsonPropertyName ("message")]
            public string? Message
            {
                get;
                set;
            }


            [JsonPropertyName ("license")]
            public LicenseInfo? License
            {
                get;
                set;
            }


            [JsonPropertyName ("activation")]
            public ActivationInfo? Activation
            {
                get;
                set;
            }


            [JsonPropertyName ("active_devices")]
            public int? ActiveDevices
            {
                get;
                set;
            }


            [JsonPropertyName ("max_devices")]
            public int? MaxDevices
            {
                get;
                set;
            }
        }


        // =====================================================
        // LICENSE INFO
        // =====================================================

        public class LicenseInfo
        {
            [JsonPropertyName ("id")]
            public int Id
            {
                get;
                set;
            }


            [JsonPropertyName ("type")]
            public string? Type
            {
                get;
                set;
            }


            [JsonPropertyName ("expiration_date")]
            public string? ExpirationDate
            {
                get;
                set;
            }


            [JsonPropertyName ("max_users")]
            public int MaxUsers
            {
                get;
                set;
            }


            [JsonPropertyName ("company_name")]
            public string? CompanyName
            {
                get;
                set;
            }
        }


        // =====================================================
        // ACTIVATION INFO
        // =====================================================

        public class ActivationInfo
        {
            [JsonPropertyName ("id")]
            public string? Id
            {
                get;
                set;
            }


            [JsonPropertyName ("activation_date")]
            public string? ActivationDate
            {
                get;
                set;
            }


            [JsonPropertyName ("hardware_fingerprint")]
            public string? HardwareFingerprint
            {
                get;
                set;
            }
        }
    }
}