using Caupo.Helpers;
using Caupo.Properties;
using Caupo.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Caupo.Views
{
    public partial class LicenseActivationPage : UserControl
    {
        private int _failedAttempts = 0;
        private const int MAX_ATTEMPTS = 5;
        public LicenseActivationPage()
        {
            InitializeComponent ();
            LoadHardwareFingerprint ();
            if(!string.IsNullOrEmpty (Properties.Settings.Default.Key))
            {
                LicenseKeyTextBox.Text = Properties.Settings.Default.Key;
            }
        }

        private void LoadHardwareFingerprint()
        {
            try
            {
                string fingerprint = HardwareHelper.GetHardwareFingerprint ();
                FingerprintTextBox.Text = fingerprint;
            }
            catch(Exception ex)
            {
                FingerprintTextBox.Text = $"Error: {ex.Message}";
            }
        }

        private async void ActivateButton_Click(object sender, RoutedEventArgs e)
        {
            string licenseKey = LicenseKeyTextBox.Text.Trim ();

            System.Diagnostics.Debug.WriteLine ("=== DEBUG START ===");
            System.Diagnostics.Debug.WriteLine ($"License key: {licenseKey}");

            if(string.IsNullOrEmpty (licenseKey))
            {
                System.Diagnostics.Debug.WriteLine ("ERROR: Empty license key");
                MessageBox.Show ("Molimo unesite licencni ključ.");
                return;
            }

            // Disable UI
            ActivateButton.IsEnabled = false;
            ActivationProgress.Visibility = Visibility.Visible;
            StatusText.Text = "Aktiviranje licence…";

            System.Diagnostics.Debug.WriteLine ("UI disabled, calling LicenseManager...");

            try
            {
                System.Diagnostics.Debug.WriteLine ("Calling LicenseManager.ActivateLicense...");
                bool success = await LicenseManager.ActivateLicense (licenseKey);

                System.Diagnostics.Debug.WriteLine ($"LicenseManager returned: {success}");

                if(success)
                {
                    System.Diagnostics.Debug.WriteLine ("SUCCESS: License activated");
                    StatusText.Text = "✅ Licenca je uspešno aktivirana.!";

                    MyMessageBox myMessageBox = new MyMessageBox ();
                    myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                    myMessageBox.MessageTitle.Text = "Obavještenje";
                    myMessageBox.MessageText.Text = "Uspješno ste aktivirali licencu";
                    myMessageBox.ShowDialog ();

                    Settings.Default.Key = licenseKey;

                    System.Diagnostics.Debug.WriteLine ("Navigating to HomePage...");
                    var page = new LoginPage ();
                    page.DataContext = new LoginPageViewModel ();
                    PageNavigator.NavigateWithFade (page);
                }
                else
                {
                    _failedAttempts++;

                    if(_failedAttempts >= 3)
                    {
                        // Blokiraj dugme i prikaži poruku
                        StatusText.Text = "❌ Previše neuspešnih pokušaja. Molimo ponovo pokrenite aplikaciju.";
                        ActivateButton.IsEnabled = false;
                        LicenseKeyTextBox.IsEnabled = false;

                        // Dodaj dugme za Exit
                        var exitButton = new Button
                        {
                            Content = "Izlaz",
                            Margin = new Thickness (0, 10, 0, 0)
                        };
                        exitButton.Click += (s, args) => Application.Current.Shutdown ();

                        // Dodaj u layout (ako imaš StackPanel ili Grid)
                        MainGrid.Children.Add (exitButton);
                    }
                    else
                    {
                        StatusText.Text = $"❌ Nevažeći licencni ključ. Pokušaji: {_failedAttempts}/3";
                        ActivateButton.IsEnabled = true;
                    }
                }

            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine ($"EXCEPTION: {ex.Message}");
                System.Diagnostics.Debug.WriteLine ($"Stack Trace: {ex.StackTrace}");
                StatusText.Text = $"Greška: {ex.Message}";
                ActivateButton.IsEnabled = true;
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine ("=== DEBUG END ===");
                ActivationProgress.Visibility = Visibility.Collapsed;
            }
        }

        private void GetLicenseButton_Click(object sender, RoutedEventArgs e)
        {
            var page = new LicensePaymentPage ();
            // Navigacija na PayPalPaymentControl
            PageNavigator.NavigateWithFade (page);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var page = new HomePage ();
            page.DataContext = new HomeViewModel ();
            PageNavigator.NavigateWithFade (page);
        }
    }
}