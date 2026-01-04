using Caupo.Data;
using Caupo.Helpers;
using Caupo.Properties;
using Caupo.Views;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private object _currentPage;
        public object CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged (nameof (CurrentPage));
            }
        }

        public MainViewModel()
        {
           
            PageNavigator.Navigate = page => CurrentPage = page;
            Application.Current.Dispatcher.BeginInvoke (new Action (StartApplication));
            
        }

        public async Task SetIColors()
        {
            Debug.WriteLine ("MAINVIEWMODEL ");
            await Task.Delay (5);
            string tema = Settings.Default.Tema;
           
            if (tema == "Tamna")
            {
               
                Application.Current.Resources["GlobalFontColor"] = new SolidColorBrush (System.Windows.Media.Color.FromRgb (212, 212, 212));
                Application.Current.Resources["GlobalBackgroundColor"] = new SolidColorBrush (System.Windows.Media.Color.FromRgb (50, 50, 50));
                var brushFont = Application.Current.Resources["GlobalFontColor"] as SolidColorBrush;
                var brushBack = Application.Current.Resources["GlobalBackgroundColor"] as SolidColorBrush;
                Debug.WriteLine ("Aktivna tema koju vidi viewmodel je : " + tema + " i postavio je GlobalFontColor " + $"#{brushFont.Color.R:X2}{brushFont.Color.G:X2}{brushFont.Color.B:X2}");
                Debug.WriteLine ("Aktivna tema koju vidi viewmodel je : " + tema + " i postavio je GlobalBackgroundColor " + $"#{brushBack.Color.R:X2}{brushBack.Color.G:X2}{brushBack.Color.B:X2} ");
            }
            else
            {
                Application.Current.Resources["GlobalFontColor"] = new SolidColorBrush (System.Windows.Media.Color.FromRgb (50, 50, 50));
                Application.Current.Resources["GlobalBackgroundColor"] = new SolidColorBrush (System.Windows.Media.Color.FromRgb (212, 212, 212));
                var brush = Application.Current.Resources["GlobalFontColor"] as SolidColorBrush;
                var brushBack = Application.Current.Resources["GlobalBackgroundColor"] as SolidColorBrush;
                Debug.WriteLine ("Aktivna tema koju vidi viewmodel je : " + tema + " i postavio je GlobalFontColor " + $"#{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}");
                Debug.WriteLine ("Aktivna tema koju vidi viewmodel je : " + tema + " i postavio je GlobalBackgroundColor " + $"#{brushBack.Color.R:X2}{brushBack.Color.G:X2}{brushBack.Color.B:X2} ");
            }
        }



        private async void StartApplication()
        {
            //Properties.Settings.Default.DbPath = "";
            //Globals.CurrentDbPath = "";
            // Settings.Default.Key = "";
            //  Settings.Default.Email = "";
            // Settings.Default.HardwareFingerprint = "";
            //   Settings.Default.LicenseType = "";
            //   Settings.Default.CompanyName = "";
            //   Settings.Default.ExpirationDate = "";
            //  Settings.Default.LastActivation = "";
            //Settings.Default.Save ();

            try
            {
                using(var context = new AppDbContext ())
                {
                    // Proveri da li se može spojiti na bazu
                    bool canConnect = context.Database.CanConnect ();

                    if(!canConnect)
                    {
                        // Ako ne može da se spoji, prikaži grešku
                        MessageBox.Show (
                            "Ne mogu se spojiti na bazu podataka!\n" +
                            "Proverite da li baza postoji na ispravnoj lokaciji.",
                            "Greška baze",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );

                        // Možeš i zatvoriti aplikaciju ako je kritično
                        // Application.Current.Shutdown();
                    }
                }
            }
            catch(Exception ex)
            {
                // Loguj grešku ali ne kreiraj bazu
                Debug.WriteLine ($"Database connection error: {ex.Message}");

                MessageBox.Show (
                    $"Greška pri povezivanju sa bazom:\n{ex.Message}",
                    "Greška baze",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }

            await SetIColors ();

            if(string.IsNullOrEmpty (Properties.Settings.Default.Firma) ||
               string.IsNullOrEmpty (Properties.Settings.Default.Adresa) ||
               string.IsNullOrEmpty (Properties.Settings.Default.Mjesto) ||
               string.IsNullOrEmpty (Properties.Settings.Default.JIB) ||
               string.IsNullOrEmpty (Properties.Settings.Default.PDV) ||
               string.IsNullOrEmpty (Properties.Settings.Default.ZR) ||
               string.IsNullOrEmpty (Properties.Settings.Default.Email))
            {
                // Otvori SettingsPage prvo
                Globals.ulogovaniKorisnik = new TblRadnici
                {
                    Radnik = "Admin",

                };
                var page = new SettingsPage ();
                PageNavigator.NavigateWithFade (page);
            }
                // Ovdje izvrši provjeru licence
                //bool licensed = ValidateLicenseKey (key);
                bool licensed = await Helpers.LicenseManager.ValidateOnStartup ();


            if (licensed)
            {
                CurrentPage = new Caupo.Views.LoginPage () { DataContext = new LoginPageViewModel () };
            }
            else
            {
                 CurrentPage = new Caupo.Views.LoginPage () { DataContext = new LoginPageViewModel () };
                //CurrentPage = new LicenseActivationPage ();
            }
        }

        string key = Settings.Default.Key;
        private static readonly byte[] Key = Encoding.UTF8.GetBytes ("12345678901234567890123456789000"); // 32 bytes for AES-256
        private static readonly byte[] IV = Encoding.UTF8.GetBytes ("1234567890123456"); // 16 bytes for AES

        string Decrypt(string cipherText)
        {
            Debug.WriteLine ("------------------------------------------------ Decrypt(string cipherText)---- " + cipherText);
            using (Aes aes = Aes.Create ())
            {
                aes.Key = Key;
                aes.IV = IV;
                using var decryptor = aes.CreateDecryptor (aes.Key, aes.IV);
                using var ms = new MemoryStream (Convert.FromBase64String (cipherText));
                using var cs = new CryptoStream (ms, decryptor, CryptoStreamMode.Read);
                using var reader = new StreamReader (cs);
                {
                    string result = reader.ReadToEnd (); // Ensure this reads the data
                    Debug.WriteLine ($"Decrypted Data: {result}");
                    return result;
                }
            }
        }

        bool isHardwareCorrect;
        bool isYearLicence;
        bool isTrialLicence;
        DateTime installationDate;
        TimeSpan installPeriod;
        private bool ValidateLicenseKey(string licenseKey)
        {
            string licenseType;
            string hardwareFingerprint;
            string date;
            try
            {
                string decryptedData = Decrypt (licenseKey);
                string[] parts = decryptedData.Split ('-');
                if (parts.Length != 3)
                {
                    return false;
                }
                licenseType = parts[0];
                hardwareFingerprint = parts[1];
                date = parts[2];
                // - Verifying the hardware fingerprint matches the current device
                Debug.WriteLine ("------------------------------------------------ hardwareFingerprint ---- " + hardwareFingerprint);
                string fingerprint = GetHardwareInfo ("Win32_Processor", "ProcessorId");

                string localHardwareFingerprint = fingerprint.ToString ();
                if (localHardwareFingerprint != hardwareFingerprint)
                {

                    MyMessageBox myMessageBox = new MyMessageBox ();
                    ////myMessageBox.Owner = this;
                    myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    myMessageBox.MessageTitle.Text = "LICENCNI KLJUČ";
                    myMessageBox.MessageText.Text = "Vaša licencni ključ ne odgovara za ovaj kompjuter" + Environment.NewLine + "Naručite novi licencni ključ ukoliko želite dalje koristiti aplikaciju.";
                    myMessageBox.ShowDialog ();
                    return false;
                }
                // - Checking if the date is within the valid range
                Debug.WriteLine ("------------------------------------------------ date---- " + date);
                // - Ensuring the license type is valid
                installationDate = DateTime.ParseExact (date, "dd.MM.yy", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);
                DateTime currentDate = DateTime.Now;
                installPeriod = currentDate - installationDate;
                switch (licenseType)
                {
                    case "Permanentna":
                        return true;
                    case "Godišnja":
                        if (installPeriod.TotalDays < 365)
                        {
                            return true;
                        }
                        else
                        {
                            MyMessageBox myMessageBox = new MyMessageBox ();
                            ////myMessageBox.Owner = this;
                            myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                            myMessageBox.MessageTitle.Text = "GODIŠNJA LICENCA";
                            myMessageBox.MessageText.Text = "Vaša licenca je istekla " + Environment.NewLine + "Vrijedila je 365 dana od dana  " + installationDate + Environment.NewLine + "Naručite novi licencni ključ ukoliko želite dalje koristiti aplikaciju.";
                            myMessageBox.ShowDialog ();
                            return false;
                        }
                    case "Probna":
                        if (installPeriod.TotalDays < 30)
                        {
                            return true;
                        }
                        else
                        {
                            MyMessageBox myMessageBox = new MyMessageBox ();
                            ////myMessageBox.Owner = this;
                            myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                            myMessageBox.MessageTitle.Text = "PROBNA LICENCA";
                            myMessageBox.MessageText.Text = "Vaša licenca je istekla " + Environment.NewLine + "Vrijedila je 30 dana od dana  " + installationDate + Environment.NewLine + "Naručite novi licencni ključ ukoliko želite dalje koristiti aplikaciju.";
                            myMessageBox.ShowDialog ();
                            return false;
                        }
                    default:
                        throw new InvalidOperationException ("Unknown license type.");
                }
            }
            catch
            {
                return false; // Decryption or validation failed
            }
        }
        private static string GetHardwareInfo(string wmiClass, string property)
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher ($"SELECT {property} FROM {wmiClass}"))
            {
                foreach (ManagementObject obj in searcher.Get ())
                {
                    if (obj[property] != null)
                    {
                        return obj[property].ToString ();
                    }
                }
            }
            return string.Empty;
        }

        private bool CheckLicense()
        {
            // Tvoja logika provjere licence
            return true; // ili false
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (name));
        }
    }
}