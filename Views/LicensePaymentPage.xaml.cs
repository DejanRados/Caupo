using Caupo.Properties;
using Caupo.ViewModels;
using Caupo.Views;
using Microsoft.Web.WebView2.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Caupo.Helpers;

namespace Caupo.Views
{
    public partial class LicensePaymentPage : UserControl
    {
        private bool _paymentSuccessHandled = false;
        private string _userEmail = "";
        private bool _isWebViewInitialized = false;
        private bool _isInitializing = false;
        private bool _isNavigationHandled = false;
        private bool _canGoBack = false;

        public LicensePaymentPage()
        {
            InitializeComponent ();
            Loaded += OnPageLoaded;
            Unloaded += OnPageUnloaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if(!_isWebViewInitialized && !_isInitializing)
            {
                InitializeWebView ();
            }
        }

        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            // Cleanup
            if(webView.CoreWebView2 != null)
            {
                webView.CoreWebView2.WebMessageReceived -= WebMessageReceived;
                webView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;
            }
        }

        async void InitializeWebView()
        {
            // Prevent multiple initializations
            if(_isWebViewInitialized || _isInitializing)
            {
                Debug.WriteLine ("[WPF] ⚠ WebView already initialized or initializing, skipping");
                return;
            }

            _isInitializing = true;
            _isNavigationHandled = false;

            try
            {
                Debug.WriteLine ("[WPF] Starting WebView initialization...");

                // Show loading overlay
                loadingOverlay.Visibility = Visibility.Visible;

                // Check if WebView2 Runtime is installed
                try
                {
                    string version = CoreWebView2Environment.GetAvailableBrowserVersionString ();
                    Debug.WriteLine ($"[WPF] WebView2 Runtime version: {version}");
                }
                catch(Exception ex)
                {
                    Debug.WriteLine ($"[WPF] ❌ WebView2 Runtime check failed: {ex.Message}");
                    MessageBox.Show (
                        "WebView2 Runtime nije instaliran.\n\n" +
                        "Molimo instalirajte Microsoft Edge WebView2 Runtime sa:\n" +
                        "https://go.microsoft.com/fwlink/p/?LinkId=2124703\n\n" +
                        "Nakon instalacije, restartujte aplikaciju.",
                        "WebView2 Runtime nije pronađen",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    loadingOverlay.Visibility = Visibility.Collapsed;
                    _isInitializing = false;
                    return;
                }

                // Check if WebView is already initialized
                if(webView.CoreWebView2 != null)
                {
                    Debug.WriteLine ("[WPF] ⚠ WebView already has CoreWebView2, reusing");

                    // Remove existing handlers first
                    webView.CoreWebView2.WebMessageReceived -= WebMessageReceived;
                    webView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;

                    SetupWebViewHandlers ();

                    _isWebViewInitialized = true;
                    loadingOverlay.Visibility = Visibility.Collapsed;
                    return;
                }

                // Create WebView2 environment with cache
                string cachePath = Path.Combine (
                    Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData),
                    "Caupo", "WebView2Cache");

                try
                {
                    Directory.CreateDirectory (cachePath);
                    Debug.WriteLine ($"[WPF] Cache path created: {cachePath}");
                }
                catch(Exception ex)
                {
                    Debug.WriteLine ($"[WPF] ❌ Failed to create cache directory: {ex.Message}");
                    // Use default cache path
                    cachePath = null;
                }

                // Create environment
                var environment = await CoreWebView2Environment.CreateAsync (
                    browserExecutableFolder: null,
                    userDataFolder: cachePath
                );

                Debug.WriteLine ("[WPF] WebView2 environment created successfully");

                // Initialize WebView2 control
                Debug.WriteLine ("[WPF] Initializing WebView2 control...");

                try
                {
                    // Prvo pokušaj bez environmenta
                    await webView.EnsureCoreWebView2Async ();
                    Debug.WriteLine ("[WPF] WebView2 initialized (already had environment)");
                }
                catch
                {
                    // Ako ne uspije, pokušaj sa našim environmentom
                    Debug.WriteLine ("[WPF] WebView2 not initialized yet, using our environment");
                    await webView.EnsureCoreWebView2Async (environment);
                    Debug.WriteLine ("[WPF] WebView2 initialized with our environment");
                }

                // Setup handlers
                SetupWebViewHandlers ();

                // Wait a bit before navigating
                await Task.Delay (300);

                // Navigate to payment page
                Debug.WriteLine ("[WPF] Navigating to payment page...");
                webView.CoreWebView2.Navigate ("https://caupo.app/caupo-licensing/public/index.html");

                _isWebViewInitialized = true;
                Debug.WriteLine ("[WPF] ✅ WebView initialization completed successfully");

            }
            catch(Exception ex)
            {
                _isInitializing = false;
                loadingOverlay.Visibility = Visibility.Collapsed;

                Debug.WriteLine ($"[WPF] ❌ Initialization error: {ex.Message}");
                Debug.WriteLine ($"[WPF] Stack trace: {ex.StackTrace}");

                // Check if it's the "already initialized" error
                if(ex.Message.Contains ("already initialized") || ex.Message.Contains ("different CoreWebView2Environment"))
                {
                    Debug.WriteLine ("[WPF] ⚠ WebView was already initialized, trying to recover...");

                    // Try to use existing WebView
                    if(webView.CoreWebView2 != null)
                    {
                        Debug.WriteLine ("[WPF] ✅ WebView has CoreWebView2, recovering...");
                        _isWebViewInitialized = true;
                        SetupWebViewHandlers ();
                        loadingOverlay.Visibility = Visibility.Collapsed;
                        return;
                    }
                }

                // Show user-friendly error message
                string userMessage = $"Greška pri inicijalizaciji platnog prozora:\n\n{ex.Message}";
                MessageBox.Show (userMessage, "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private void SetupWebViewHandlers()
        {
            try
            {
                if(webView.CoreWebView2 == null)
                {
                    Debug.WriteLine ("[WPF] ❌ Cannot setup handlers - CoreWebView2 is null");
                    return;
                }

                // Remove existing handlers first
                webView.CoreWebView2.WebMessageReceived -= WebMessageReceived;
                webView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;
                // DODAJ OVU LINIJU ↓
                webView.CoreWebView2.HistoryChanged -= CoreWebView2_HistoryChanged;

                // Configure WebView2 settings
                webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;

                // Set up message handler
                webView.CoreWebView2.WebMessageReceived += WebMessageReceived;

                // Handle navigation completion
                webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;

                // DODAJ OVU LINIJU ↓
                webView.CoreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;

                Debug.WriteLine ("[WPF] ✅ WebView handlers setup completed");
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[WPF] ❌ Error setting up handlers: {ex.Message}");
            }
        }

        private void CoreWebView2_HistoryChanged(object sender, object e)
        {
            try
            {
                if(webView.CoreWebView2 != null)
                {
                    _canGoBack = webView.CoreWebView2.CanGoBack;

                    Dispatcher.Invoke (() =>
                    {
                        btnBack.Visibility = _canGoBack ? Visibility.Visible : Visibility.Collapsed;

                        string currentUrl = webView.CoreWebView2.Source;
                        txtCurrentUrl.Text = currentUrl.Length > 50
                            ? currentUrl.Substring (0, 47) + "..."
                            : currentUrl;
                    });

                    Debug.WriteLine ($"[WPF] History changed - CanGoBack: {_canGoBack}");
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[WPF] ❌ Error in HistoryChanged: {ex.Message}");
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if(webView.CoreWebView2 != null && webView.CoreWebView2.CanGoBack)
                {
                    Debug.WriteLine ("[WPF] ← Going back in navigation history");
                    webView.CoreWebView2.GoBack ();
                }
                else
                {
                    Debug.WriteLine ("[WPF] ⚠ Cannot go back - no history");
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[WPF] ❌ Error going back: {ex.Message}");
                MessageBox.Show ("Greška pri vraćanju na prethodnu stranicu.",
                    "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if(webView.CoreWebView2 != null)
                {
                    Debug.WriteLine ("[WPF] ⟳ Refreshing page");

                    string currentUrl = webView.CoreWebView2.Source;
                    if(currentUrl.Contains ("success=true"))
                    {
                        Debug.WriteLine ("[WPF] ⚠ On success page - navigating to home instead");
                        webView.CoreWebView2.Navigate ("https://caupo.app/caupo-licensing/public/index.html");
                    }
                    else
                    {
                        webView.CoreWebView2.Reload ();
                    }
                }
                else
                {
                    Debug.WriteLine ("[WPF] ⚠ WebView not initialized - reinitializing");
                    InitializeWebView ();
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[WPF] ❌ Error refreshing: {ex.Message}");
                MessageBox.Show ("Greška pri osvežavanju stranice.",
                    "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if(_isNavigationHandled)
                return;
            _isNavigationHandled = true;

            Debug.WriteLine ($"[WPF] Navigation completed. Success: {e.IsSuccess}, Error: {e.WebErrorStatus}");

            loadingOverlay.Visibility = Visibility.Collapsed;

            if(e.IsSuccess)
            {
                Debug.WriteLine ("[WPF] ✅ Page loaded successfully");

                // DODAJ OVO ↓ - Ažuriraj URL u toolbar-u
                if(webView.CoreWebView2 != null)
                {
                    string currentUrl = webView.CoreWebView2.Source;
                    Dispatcher.Invoke (() =>
                    {
                        txtCurrentUrl.Text = currentUrl.Length > 50
                            ? currentUrl.Substring (0, 47) + "..."
                            : currentUrl;
                    });
                }

                await Task.Delay (500);

                string currentUrl2 = webView.CoreWebView2.Source;
                Debug.WriteLine ($"[WPF] Current URL: {currentUrl2}");

                if(currentUrl2.Contains ("success=true") && currentUrl2.Contains ("session_id"))
                {
                    Debug.WriteLine ("[WPF] 🎉 Success URL detected!");
                    await HandleSuccessfulPayment (currentUrl2);
                }
                else
                {
                    await SendUserDataToWeb ();
                }
            }
            else
            {
                Debug.WriteLine ($"[WPF] ❌ Navigation failed with error: {e.WebErrorStatus}");

                _isNavigationHandled = false;

                if(e.WebErrorStatus == CoreWebView2WebErrorStatus.ConnectionAborted)
                {
                    Debug.WriteLine ("[WPF] 🔄 Connection aborted, retrying navigation...");

                    await Task.Delay (1000);

                    try
                    {
                        webView.CoreWebView2.Navigate ("https://caupo.app/caupo-licensing/public/index.html");
                        Debug.WriteLine ("[WPF] 🔄 Retrying navigation...");
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine ($"[WPF] ❌ Retry failed: {ex.Message}");
                        MessageBox.Show ("Ne mogu učitati platnu stranicu. Pokušajte ponovo ili kontaktirajte podršku.",
                            "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    string errorMessage = GetErrorMessage (e.WebErrorStatus);
                    MessageBox.Show ($"Greška pri učitavanju stranice:\n\n{errorMessage}",
                        "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string GetErrorMessage(CoreWebView2WebErrorStatus errorStatus)
        {
            switch(errorStatus)
            {
                case CoreWebView2WebErrorStatus.ConnectionAborted:
                    return "Veza prekinuta. Provjerite internet vezu.";
                case CoreWebView2WebErrorStatus.CannotConnect:
                    return "Ne mogu se povezati. Server ne odgovara.";
                case CoreWebView2WebErrorStatus.ConnectionReset:
                    return "Veza resetovana.";
                case CoreWebView2WebErrorStatus.Disconnected:
                    return "Diskonektovano.";
                case CoreWebView2WebErrorStatus.Timeout:
                    return "Vrijeme čekanja isteklo. Provjerite internet vezu.";
                case CoreWebView2WebErrorStatus.HostNameNotResolved:
                    return "Host nije pronađen. Provjerite URL.";
                case CoreWebView2WebErrorStatus.OperationCanceled:
                    return "Operacija otkazana.";
                default:
                    return $"Greška: {errorStatus}";
            }
        }

        private async Task HandleSuccessfulPayment(string url)
        {
            if(_paymentSuccessHandled)
            {
                Debug.WriteLine ("[WPF] ⚠ Payment already handled, skipping");
                return;
            }

            _paymentSuccessHandled = true;

            Debug.WriteLine ("[WPF] 🔄 Processing successful payment...");

            try
            {
                // Get session ID from URL
                var uri = new Uri (url);
                var query = System.Web.HttpUtility.ParseQueryString (uri.Query);
                string sessionId = query["session_id"];

                if(string.IsNullOrEmpty (sessionId))
                {
                    Debug.WriteLine ("[WPF] ❌ No session ID found");
                    MessageBox.Show ("Plaćanje je uspješno, ali nije pronađen session ID. Kontaktirajte podršku.",
                        "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Debug.WriteLine ($"[WPF] 🔑 Session ID: {sessionId}");

                // Check if license already exists in settings
                if(!string.IsNullOrEmpty (Settings.Default.Key))
                {
                    Debug.WriteLine ($"[WPF] ⚠ License already exists in settings: {Settings.Default.Key}");
                    Debug.WriteLine ("[WPF] ⚠ Skipping JavaScript execution");
                    return;
                }

                // JavaScript to execute on the page
                string jsCode = $@"
                    console.log('[WPF] Starting license retrieval for session:', '{sessionId}');
                    
                    // Check if we're in WPF environment
                    if (!window.chrome?.webview) {{
                        console.error('[WPF] ❌ Not in WPF WebView2 environment');
                        return 'NOT_IN_WPF';
                    }}
                    
                    // Try to use getLicenseBySession if it exists
                    if (typeof getLicenseBySession === 'function') {{
                        console.log('[WPF] Using getLicenseBySession function');
                        return getLicenseBySession('{sessionId}')
                            .then(licenseData => {{
                                console.log('[WPF] License data received:', licenseData);
                                
                                // Send to WPF
                                const message = {{
                                    type: 'payment_success',
                                    licenseKey: licenseData.license_key,
                                    license_key: licenseData.license_key,
                                    license_id: licenseData.license_id,
                                    license_type: licenseData.license_type,
                                    existing_license: licenseData.existing_license || false,
                                    company_name: licenseData.company_name,
                                    contact_email: licenseData.contact_email,
                                    expiration_date: licenseData.expiration_date,
                                    timestamp: new Date().toISOString()
                                }};
                                
                                console.log('[WPF] Sending to WPF:', message);
                                window.chrome.webview.postMessage(JSON.stringify(message));
                                
                                return 'LICENSE_SENT';
                            }})
                            .catch(error => {{
                                console.error('[WPF] Error with getLicenseBySession:', error);
                                return 'ERROR: ' + error.message;
                            }});
                    }} else {{
                        console.error('[WPF] getLicenseBySession function not found');
                        return 'FUNCTION_NOT_FOUND';
                    }}
                ";

                // Execute JavaScript
                Debug.WriteLine ("[WPF] Executing JavaScript to get license...");
                string result = await webView.CoreWebView2.ExecuteScriptAsync (jsCode);

                // Remove JSON quotes if present
                if(result.StartsWith ("\"") && result.EndsWith ("\""))
                {
                    result = result.Substring (1, result.Length - 2);
                }

                Debug.WriteLine ($"[WPF] JavaScript result: {result}");

                if(result == "NOT_IN_WPF")
                {
                    Debug.WriteLine ("[WPF] ⚠ Not running in WPF environment");
                }
                else if(result == "FUNCTION_NOT_FOUND")
                {
                    Debug.WriteLine ("[WPF] ❌ getLicenseBySession function not found on page");
                    MessageBox.Show ("Stranica nema potrebnu funkciju za dobijanje licence. Kontaktirajte podršku.",
                        "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if(result.StartsWith ("ERROR:"))
                {
                    Debug.WriteLine ($"[WPF] ❌ JavaScript error: {result}");
                    MessageBox.Show ($"Greška pri dobijanju licence: {result.Replace ("ERROR: ", "")}",
                        "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if(result == "LICENSE_SENT")
                {
                    Debug.WriteLine ("[WPF] ✅ License retrieval initiated successfully");
                    // License will come via WebMessageReceived event
                }

            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[WPF] ❌ Error in HandleSuccessfulPayment: {ex.Message}");
                Debug.WriteLine ($"[WPF] Stack trace: {ex.StackTrace}");

                MessageBox.Show ($"Greška pri obradi plaćanja:\n\n{ex.Message}",
                    "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SendUserDataToWeb()
        {
            try
            {
                // Check if this is a success URL before sending data
                string currentUrl = webView.CoreWebView2?.Source ?? "";
                if(currentUrl.Contains ("success=true"))
                {
                    Debug.WriteLine ("[WPF] ⚠ Not sending user data - success URL detected");
                    return;
                }

                string company = Settings.Default.Firma ?? "";
                string email = Settings.Default.Email ?? "";

                // Ako su prazni, možda korisnik još nije unio podatke
                if(string.IsNullOrEmpty (email) && string.IsNullOrEmpty (company))
                {
                    Debug.WriteLine ("[WPF] ⚠ No user data in settings - not sending empty data");
                    return;
                }

                Debug.WriteLine ($"[WPF] Sending user data - Company: {company}, Email: {email}");

                // Escape for JSON
                company = EscapeForJson (company);
                email = EscapeForJson (email);

                // Create JSON message
                string json = $"{{\"firma\":\"{company}\",\"email\":\"{email}\"}}";

                // Send to web page
                webView.CoreWebView2.PostWebMessageAsString (json);
                Debug.WriteLine ($"[WPF] 📤 Sent user data to web page: {json}");

            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[WPF] ❌ Error sending user data: {ex.Message}");
            }
        }

        // Handle messages from web page
        private void WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                string message = args.TryGetWebMessageAsString ();
                Debug.WriteLine ($"[WPF] 📨 Raw message received: {message}");

                // Process on UI thread
                Dispatcher.Invoke (() => ProcessWebMessage (message));
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[WPF] ❌ Error in WebMessageReceived: {ex.Message}");
            }
        }

        private void ProcessWebMessage(string jsonMessage)
        {
            try
            {
                if(string.IsNullOrEmpty (jsonMessage))
                {
                    Debug.WriteLine ("[WPF] ⚠ Empty message received");
                    return;
                }

                Debug.WriteLine ($"[WPF] Processing message: {jsonMessage}");

                // Parse JSON
                using(var doc = JsonDocument.Parse (jsonMessage))
                {
                    var root = doc.RootElement;

                    // Get message type
                    if(!root.TryGetProperty ("type", out var typeElement))
                    {
                        Debug.WriteLine ("[WPF] ⚠ Message has no 'type' field");
                        return;
                    }

                    string messageType = typeElement.GetString ();
                    Debug.WriteLine ($"[WPF] 📋 Message type: {messageType}");

                    switch(messageType)
                    {
                        case "payment_success":
                            HandlePaymentSuccess (root);
                            break;

                        case "close_window":
                            Debug.WriteLine ("[WPF] 🪟 Close window requested");
                            CloseButton_Click_1 (null, null);
                            break;

                        case "form_submitted":
                            Debug.WriteLine ("[WPF] 📝 Form submitted");
                            break;

                        default:
                            Debug.WriteLine ($"[WPF] ⚠ Unknown message type: {messageType}");
                            break;
                    }
                }
            }
            catch(JsonException jex)
            {
                Debug.WriteLine ($"[WPF] ❌ JSON parsing error: {jex.Message}");
                Debug.WriteLine ($"[WPF] Invalid JSON: {jsonMessage}");
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[WPF] ❌ Error in ProcessWebMessage: {ex.Message}");
            }
        }

        private void HandlePaymentSuccess(JsonElement root)
        {
            try
            {
                Debug.WriteLine ("[WPF] Handling payment success...");

                // Try to get licenseKey (with different possible property names)
                string licenseKey = null;

                if(root.TryGetProperty ("licenseKey", out var keyElement))
                {
                    licenseKey = keyElement.GetString ();
                    Debug.WriteLine ($"[WPF] Got licenseKey from 'licenseKey' property: {licenseKey}");
                }
                else if(root.TryGetProperty ("license_key", out var keyElement2))
                {
                    licenseKey = keyElement2.GetString ();
                    Debug.WriteLine ($"[WPF] Got licenseKey from 'license_key' property: {licenseKey}");
                }

                bool isRenewal = root.TryGetProperty ("existing_license", out var renewalElement)
                    ? renewalElement.GetBoolean ()
                    : false;

                string licenseType = root.TryGetProperty ("license_type", out var typeElement)
                    ? typeElement.GetString ()
                    : "unknown";

                string contactEmail = root.TryGetProperty ("contact_email", out var emailElement)
                    ? emailElement.GetString ()
                    : "";

                string companyName = root.TryGetProperty ("company_name", out var companyElement)
                    ? companyElement.GetString ()
                    : "";

                if(string.IsNullOrEmpty (licenseKey))
                {
                    Debug.WriteLine ("[WPF] ❌ No license key in message");
                    MessageBox.Show ("Licenca je kupljena, ali licencni ključ nije primljen.\n\nProvjerite svoj email ili kontaktirajte podršku.",
                        "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Debug.WriteLine ($"[WPF] 🎉 License received successfully!");
                Debug.WriteLine ($"[WPF] Key: {licenseKey}");
                Debug.WriteLine ($"[WPF] Type: {licenseType}");
                Debug.WriteLine ($"[WPF] Renewal: {isRenewal}");
                Debug.WriteLine ($"[WPF] Contact Email: {contactEmail}");
                Debug.WriteLine ($"[WPF] Company Name: {companyName}");
                Debug.WriteLine ($"[WPF] Current Settings Email: {Settings.Default.Email ?? "(empty)"}");
                Debug.WriteLine ($"[WPF] Current Settings Company: {Settings.Default.Firma ?? "(empty)"}");

                // VAŽNA ISPRAVKA: Ako korisnik nema email u Settings, automatski spremamo licence
                string currentUserEmail = Settings.Default.Email ?? "";
                bool emailInSettingsIsEmpty = string.IsNullOrEmpty (currentUserEmail);

                Debug.WriteLine ($"[WPF] Is email in settings empty? {emailInSettingsIsEmpty}");

                if(emailInSettingsIsEmpty)
                {
                    Debug.WriteLine ($"[WPF] ⚡ Email in settings is empty - this is likely first license purchase");
                    Debug.WriteLine ($"[WPF] ⚡ Automatically saving license and user data from payment");

                    // SPREMI LICENCU
                    Settings.Default.Key = licenseKey;
                    Settings.Default.HardwareFingerprint = ""; // Reset for new license

                    // SPREMI KORISNIČKE PODATKE IZ KUPOVINE
                    if(!string.IsNullOrEmpty (contactEmail))
                    {
                        Settings.Default.Email = contactEmail;
                        Debug.WriteLine ($"[WPF] 💾 Saved email from payment: {contactEmail}");
                    }

                    if(!string.IsNullOrEmpty (companyName))
                    {
                        Settings.Default.Firma = companyName;
                        Debug.WriteLine ($"[WPF] 💾 Saved company from payment: {companyName}");
                    }

                    Settings.Default.Save ();
                    Debug.WriteLine ("[WPF] Settings saved successfully");

                    // Show success message
                    string message = $"✅ Nova licenca kupljena!\n\n" +
                                   $"Licencni ključ: {licenseKey}\n" +
                                   $"Tip licence: {licenseType}\n" +
                                   $"Email: {contactEmail}\n" +
                                   $"Firma: {companyName}\n\n" +
                                   $"Licenca i podaci su spremljeni. Sada možete aktivirati licencu.";

                    MessageBox.Show (message, "Uspješna kupovina", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Navigate to activation page
                    Debug.WriteLine ("[WPF] Navigating to LicenseActivationPage");
                    var activationPage = new LicenseActivationPage ();
                    PageNavigator.NavigateWithFade (activationPage);
                }
                else
                {
                    // KORISNIK VEĆ IMA EMAIL U SETTINGS - provjeri da li je ovo za njega
                    bool isForCurrentUser = contactEmail.Equals (currentUserEmail, StringComparison.OrdinalIgnoreCase);

                    Debug.WriteLine ($"[WPF] Is license for current user? {isForCurrentUser}");

                    if(!isForCurrentUser)
                    {
                        Debug.WriteLine ($"[WPF] ⚠ License is for different user ({contactEmail}), not for current user ({currentUserEmail})");

                        var result = MessageBox.Show (
                            $"Licenca je kupljena za drugog korisnika:\n\n" +
                            $"Email: {contactEmail}\n" +
                            $"Firma: {companyName}\n\n" +
                            $"Trenutni korisnik u aplikaciji:\n" +
                            $"Email: {currentUserEmail}\n" +
                            $"Firma: {Settings.Default.Firma ?? "(empty)"}\n\n" +
                            "Želite li zamijeniti postojeće podatke i licencu novima?",
                            "Licenca za drugog korisnika",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if(result != MessageBoxResult.Yes)
                        {
                            Debug.WriteLine ($"[WPF] User chose not to replace existing data");
                            return;
                        }

                        Debug.WriteLine ($"[WPF] User chose to replace existing data");
                    }

                    // Provjeri da li je ovo stvarno produženje ili nova licenca
                    bool shouldUpdateLicense = true;
                    string currentLicense = Settings.Default.Key;

                    if(!string.IsNullOrEmpty (currentLicense))
                    {
                        if(currentLicense.Equals (licenseKey, StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.WriteLine ($"[WPF] ⚠ Same license key, already in settings");

                            if(isRenewal)
                            {
                                Debug.WriteLine ($"[WPF] 🔄 Same license but marked as renewal - showing success message");
                                MessageBox.Show ($"✅ Licenca uspješno produžena!\n\nTip licence: {licenseType}",
                                    "Produžena licenca", MessageBoxButton.OK, MessageBoxImage.Information);

                                Debug.WriteLine ("[WPF] Navigating to HomePage");
                                var homePage = new HomePage ();
                                homePage.DataContext = new HomeViewModel ();
                                PageNavigator.NavigateWithFade (homePage);
                            }

                            return; // Ista licenca, ne treba ništa mijenjati
                        }
                        else if(isRenewal)
                        {
                            Debug.WriteLine ($"[WPF] 🔄 Different license key, but marked as renewal - updating");
                            shouldUpdateLicense = true;
                        }
                        else
                        {
                            Debug.WriteLine ($"[WPF] 🔄 Different license key, not a renewal - asking user");

                            var result = MessageBox.Show (
                                "Već imate licencu u aplikaciji.\n\n" +
                                $"Nova licenca je kupljena za:\n" +
                                $"Email: {contactEmail}\n" +
                                $"Firma: {companyName}\n\n" +
                                "Želite li zamijeniti postojeću licencu i podatke novima?",
                                "Zamjena licence",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);

                            shouldUpdateLicense = (result == MessageBoxResult.Yes);
                        }
                    }

                    if(shouldUpdateLicense)
                    {
                        // Save to settings
                        Settings.Default.Key = licenseKey;

                        // Update user data from payment
                        if(!string.IsNullOrEmpty (contactEmail))
                        {
                            Settings.Default.Email = contactEmail;
                        }

                        if(!string.IsNullOrEmpty (companyName))
                        {
                            Settings.Default.Firma = companyName;
                        }

                        if(!isRenewal)
                        {
                            Settings.Default.HardwareFingerprint = ""; // Reset for new license
                            Debug.WriteLine ("[WPF] Reset hardware fingerprint for new license");
                        }

                        Settings.Default.Save ();
                        Debug.WriteLine ("[WPF] Settings saved successfully");

                        // Show success message
                        string message = isRenewal
                            ? $"✅ Licenca uspješno produžena!\n\nTip licence: {licenseType}\n\nPodaci su ažurirani."
                            : $"✅ Nova licenca kupljena!\n\nLicencni ključ: {licenseKey}\nTip licence: {licenseType}\n\nPodaci su spremljeni i spremni za aktivaciju.";

                        MessageBox.Show (message,
                            isRenewal ? "Produžena licenca" : "Uspješna kupovina",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Navigate to appropriate page
                        if(isRenewal)
                        {
                            Debug.WriteLine ("[WPF] Navigating to HomePage (renewal)");
                            var homePage = new HomePage ();
                            homePage.DataContext = new HomeViewModel ();
                            PageNavigator.NavigateWithFade (homePage);
                        }
                        else
                        {
                            Debug.WriteLine ("[WPF] Navigating to LicenseActivationPage (new license)");
                            var activationPage = new LicenseActivationPage ();
                            PageNavigator.NavigateWithFade (activationPage);
                        }
                    }
                    else
                    {
                        Debug.WriteLine ("[WPF] ⚠ License not updated - user chose not to replace");
                    }
                }

            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[WPF] ❌ Error handling payment success: {ex.Message}");
                Debug.WriteLine ($"[WPF] Stack trace: {ex.StackTrace}");

                MessageBox.Show ($"Greška pri obradi licence:\n\n{ex.Message}\n\nKontaktirajte podršku sa informacijama o kupovini.",
                    "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string EscapeForJson(string input)
        {
            if(string.IsNullOrEmpty (input))
                return "";

            return input
                .Replace ("\\", "\\\\")
                .Replace ("\"", "\\\"")
                .Replace ("\n", "\\n")
                .Replace ("\r", "\\r")
                .Replace ("\t", "\\t");
        }

        private void CloseButton_Click_1(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine ("[WPF] Close button clicked");
            var page = new LicenseActivationPage();

            PageNavigator.NavigateWithFade (page);
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine ("[WPF] Help button clicked");
                Process.Start (new ProcessStartInfo
                {
                    FileName = "https://caupo.app/pomoc",
                    UseShellExecute = true
                });
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[WPF] ❌ Error opening help: {ex.Message}");
                MessageBox.Show ($"Greška pri otvaranju pomoći: {ex.Message}",
                    "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}