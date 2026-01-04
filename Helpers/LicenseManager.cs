using Caupo.Properties;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace Caupo.Helpers
{
    public static class LicenseManager
    {
        private static readonly string ApiBaseUrl = "https://caupo.app/caupo-licensing/api/endpoints/";

        public static async Task<bool> ActivateLicense(string licenseKey)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine ("[LicenseManager] Starting activation...");

                // 1. Generiši hardware fingerprint
                System.Diagnostics.Debug.WriteLine ("[LicenseManager] Getting hardware fingerprint...");
                string hardwareFingerprint = HardwareHelper.GetHardwareFingerprint ();
                System.Diagnostics.Debug.WriteLine ($"[LicenseManager] Fingerprint: {hardwareFingerprint}");

                // 2. Pripremi podatke za API
                System.Diagnostics.Debug.WriteLine ("[LicenseManager] Preparing API data...");
                var activationData = new
                {
                    license_key = licenseKey,
                    hardware_fingerprint = hardwareFingerprint
                };

                string jsonData = JsonSerializer.Serialize (activationData);
                System.Diagnostics.Debug.WriteLine ($"[LicenseManager] JSON: {jsonData}");

                // 3. Pošalji zahtjev API-ju
                System.Diagnostics.Debug.WriteLine ("[LicenseManager] Creating HttpClient...");
                using(var client = new HttpClient ())
                {
                    client.Timeout = TimeSpan.FromSeconds (30);

                    System.Diagnostics.Debug.WriteLine ("[LicenseManager] Sending POST request...");
                    var response = await client.PostAsync (
                        "https://caupo.app/caupo-licensing/api/endpoints/activate.php",
                        new StringContent (jsonData, Encoding.UTF8, "application/json")
                    );

                    System.Diagnostics.Debug.WriteLine ($"[LicenseManager] Response status: {response.StatusCode}");

                    // 4. Pročitaj odgovor
                    string responseBody = await response.Content.ReadAsStringAsync ();
                    System.Diagnostics.Debug.WriteLine ($"[LicenseManager] Response body: {responseBody}");

                    if(!response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine ($"[LicenseManager] HTTP error: {response.StatusCode}");
                        return false;
                    }

                    // 5. Parsiraj JSON odgovor
                    System.Diagnostics.Debug.WriteLine ("[LicenseManager] Parsing JSON response...");
                    var result = JsonSerializer.Deserialize<ActivationResponse> (responseBody);

                    if(result == null || !result.Success)
                    {
                        System.Diagnostics.Debug.WriteLine ($"[LicenseManager] API returned failure: {result?.Message}");
                        return false;
                    }

                    System.Diagnostics.Debug.WriteLine ("[LicenseManager] Activation successful, saving settings...");

                    // 6. Sačuvaj podatke u Settings
                    Settings.Default.Key = licenseKey;
                    Settings.Default.HardwareFingerprint = hardwareFingerprint;
                    Settings.Default.LicenseType = result.License?.Type ?? "";
                    Settings.Default.CompanyName = result.License?.CompanyName ?? "";
                    Settings.Default.ExpirationDate = result.License?.ExpirationDate ?? "";
                    Settings.Default.LastActivation = DateTime.Now.ToString ();
                    Settings.Default.Save ();

                    System.Diagnostics.Debug.WriteLine ("[LicenseManager] Settings saved successfully!");

                    return true;
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine ($"[LicenseManager] EXCEPTION: {ex.Message}");
                System.Diagnostics.Debug.WriteLine ($"[LicenseManager] Stack Trace: {ex.StackTrace}");
                return false;
            }
        }

        public static async Task<bool> ValidateLicense()
        {
            try
            {
                string licenseKey = Settings.Default.Key;
                string hardwareFingerprint = Settings.Default.HardwareFingerprint;

                if(string.IsNullOrEmpty (licenseKey) || string.IsNullOrEmpty (hardwareFingerprint))
                {
                    return false; // Nema aktivne licence
                }

                // TODO: Dodajte validaciju (periodično proveravati sa serverom)
                // Za sada samo proveri da li postoje podaci
                return !string.IsNullOrEmpty (licenseKey);
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> ValidateOnStartup()
        {
            try
            {
                // 1. Provjeri da li postoji licencni ključ u Settings
                string licenseKey = Settings.Default.Key;

                if(string.IsNullOrEmpty (licenseKey))
                {
                    return false; // Nema licence
                }

                // 2. Provjeri hardware fingerprint
                string currentFingerprint = HardwareHelper.GetHardwareFingerprint ();
                string savedFingerprint = Settings.Default.HardwareFingerprint;

                if(currentFingerprint != savedFingerprint)
                {
                    return false; // Hardware se promijenio
                }

                // 3. Vrati true - licenca postoji i hardware se podudara
                return true;
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine ($"[LicenseManager] ValidateOnStartup error: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> ValidateOnline(string licenseKey, string hardwareFingerprint)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine ("[LicenseManager] Starting online validation...");

                using var client = new HttpClient ();
                client.Timeout = TimeSpan.FromSeconds (10);

                var data = new
                {
                    license_key = licenseKey,
                    hardware_fingerprint = hardwareFingerprint,
                    action = "validate"
                };

                string json = JsonSerializer.Serialize (data);
                var content = new StringContent (json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync (
                    "https://caupo.app/caupo-licensing/api/endpoints/activate.php", 
                    content
                );

                if(!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine ($"[LicenseManager] Online validation HTTP error: {response.StatusCode}");
                    return false;
                }

                string responseBody = await response.Content.ReadAsStringAsync ();
                System.Diagnostics.Debug.WriteLine ($"[LicenseManager] Online response: {responseBody}");

                using JsonDocument doc = JsonDocument.Parse (responseBody);
                var root = doc.RootElement;

                if(root.TryGetProperty ("success", out JsonElement successElement))
                {
                    return successElement.GetBoolean ();
                }

                return false;
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine ($"[LicenseManager] Online validation exception: {ex.Message}");
                throw; // Propusti grešku da zna gornja metoda da je online validacija pala
            }
        }


        private class ActivationResponse
        {
            [JsonPropertyName ("success")]
            public bool Success { get; set; }

            [JsonPropertyName ("message")]
            public string Message { get; set; }

            [JsonPropertyName ("license")]
            public LicenseInfo License { get; set; }

            [JsonPropertyName ("activation")]
            public ActivationInfo Activation { get; set; }
        }

        private class LicenseInfo
        {
            [JsonPropertyName ("id")]
            public string Id { get; set; }

            [JsonPropertyName ("type")]
            public string Type { get; set; }

            [JsonPropertyName ("expiration_date")]
            public string ExpirationDate { get; set; }

            [JsonPropertyName ("max_users")]
            public string MaxUsers { get; set; }

            [JsonPropertyName ("company_name")]
            public string CompanyName { get; set; }
        }

        private class ActivationInfo
        {
            [JsonPropertyName ("id")]
            public string Id { get; set; }

            [JsonPropertyName ("activation_date")]
            public string ActivationDate { get; set; }

            [JsonPropertyName ("hardware_fingerprint")]
            public string HardwareFingerprint { get; set; }
        }
    }
}
