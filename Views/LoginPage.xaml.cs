
using Caupo.Data;
using Caupo.Helpers;
using Caupo.Properties;
using Caupo.ViewModels;
using Caupo.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Syncfusion.Licensing;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Caupo.Views
{
   
    public partial class LoginPage : UserControl
    {
        private string _pin = "";
     
        public LoginPage()
        {
            // SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NMaF5cXmBCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWX5dcHVWQ2JdU0NyWEo=");
            //  Settings.Default.Tema = "Office2019Black";
            //  Settings.Default.Save();
           
            Debug.WriteLine("------------------------------------------------ key ---- " + key);
          /*  if (!ValidateLicenseKey(key))
            {
                LicencePage licencePage = new LicencePage();
                licencePage.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                licencePage.ShowDialog();
               
                return;
            }*/
            InitializeComponent();
           
            this.DataContext = new LoginPageViewModel();
          
         
            // this.WindowState = WindowState.Maximized;
            // this.WindowStyle = WindowStyle.None;
            // this.ResizeMode = ResizeMode.NoResize;
            // this.Topmost = true;
        }
      


        string key = Settings.Default.Key;
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("12345678901234567890123456789000"); // 32 bytes for AES-256
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("1234567890123456"); // 16 bytes for AES

        string Decrypt(string cipherText)
        {
            Debug.WriteLine("------------------------------------------------ Decrypt(string cipherText)---- " + cipherText);
            using (Aes aes = Aes.Create())
            {
                    aes.Key = Key;
                    aes.IV = IV;
                    using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
                    using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                    using var reader = new StreamReader(cs);
                    {
                        string result = reader.ReadToEnd(); // Ensure this reads the data
                        Debug.WriteLine($"Decrypted Data: {result}");
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
                string decryptedData = Decrypt(licenseKey);
                string[] parts = decryptedData.Split('-');
                if (parts.Length != 3)
                {
                    return false;
                }
                licenseType = parts[0];
                hardwareFingerprint = parts[1];
                date = parts[2];
                // - Verifying the hardware fingerprint matches the current device
                Debug.WriteLine("------------------------------------------------ hardwareFingerprint ---- " + hardwareFingerprint);
                string fingerprint = GetHardwareInfo("Win32_Processor", "ProcessorId");

                string localHardwareFingerprint = fingerprint.ToString();
                if (localHardwareFingerprint != hardwareFingerprint)
                {

                    MyMessageBox myMessageBox = new MyMessageBox();
                    //myMessageBox.Owner = this;
                    myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    myMessageBox.MessageTitle.Text = "LICENCNI KLJUČ";
                    myMessageBox.MessageText.Text = "Vaša licencni ključ ne odgovara za ovaj kompjuter" + Environment.NewLine + "Naručite novi licencni ključ ukoliko želite dalje koristiti aplikaciju.";
                  myMessageBox.ShowDialog();
                    return false;
                }
                // - Checking if the date is within the valid range
                Debug.WriteLine("------------------------------------------------ date---- " + date);
                // - Ensuring the license type is valid
                installationDate = DateTime.ParseExact(date, "dd.MM.yy", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);
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
                            MyMessageBox myMessageBox = new MyMessageBox();
                            //myMessageBox.Owner = this;
                            myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                            myMessageBox.MessageTitle.Text = "GODIŠNJA LICENCA";
                            myMessageBox.MessageText.Text = "Vaša licenca je istekla " + Environment.NewLine + "Vrijedila je 365 dana od dana  " + installationDate + Environment.NewLine + "Naručite novi licencni ključ ukoliko želite dalje koristiti aplikaciju.";
                            myMessageBox.ShowDialog();
                            return false;
                        }
                    case "Probna":
                        if (installPeriod.TotalDays < 30)
                        {
                            return true;
                        }
                        else
                        {
                            MyMessageBox myMessageBox = new MyMessageBox();
                            //myMessageBox.Owner = this;
                            myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                            myMessageBox.MessageTitle.Text = "PROBNA LICENCA";
                            myMessageBox.MessageText.Text = "Vaša licenca je istekla " + Environment.NewLine + "Vrijedila je 30 dana od dana  " + installationDate + Environment.NewLine + "Naručite novi licencni ključ ukoliko želite dalje koristiti aplikaciju.";
                            myMessageBox.ShowDialog();
                            return false;
                        }
                    default:
                        throw new InvalidOperationException("Unknown license type.");
                }
            }
            catch
            {
                return false; // Decryption or validation failed
            }
        }
        private static string GetHardwareInfo(string wmiClass, string property)
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj[property] != null)
                    {
                        return obj[property].ToString();
                    }
                }
            }
            return string.Empty;
        }

        private async void FadeInOut(object sender)
        {
            if (sender is Button button)
            {
                  DoubleAnimation fadeInAnimation = new DoubleAnimation
                {
                    From = 0, // Start at fully transparent
                    To = 1,   // End at fully visible
                    Duration = new Duration(TimeSpan.FromSeconds(1)) // Duration of 2 seconds
                };

                
                button.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
            }
        } 

        private void OnNumericButtonClicked(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                FadeInOut(button as Button);
                _pin += button.Content;
                PinEntry.Password = new string('●', _pin.Length);
            }
        }

        private void OnClearButtonClicked(object sender, EventArgs e)
        {
            _pin = "";
            PinEntry.Password = "";
        }
        int pokusaj = 3;
        int x = 0;
        private async void OnOkButtonClicked(object sender, EventArgs e)
        {
            x = x + 1;
            Debug.WriteLine("---------------------------------------Password:" + _pin);
            pokusaj -= 1;
            if (pokusaj < 1)
            {
                
      


                MyMessageBox myMessageBox = new MyMessageBox();
                //myMessageBox.Owner = this;
                myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
             
                myMessageBox.MessageTitle.Text = "GREŠKA";
                myMessageBox.MessageText.Text = "Pogrešna lozinka" + Environment.NewLine + "Nemate pristup aplikaciji";
                myMessageBox.ShowDialog();
                Application.Current.Shutdown();
            }

            using (var db = new AppDbContext())
            {

                var radnik = await db.Radnici
                                      .Where(r => r.Lozinka == _pin)
                                   
                                      .FirstOrDefaultAsync();
                Debug.WriteLine(radnik);
                if (radnik == null)
                {
  


                    MyMessageBox myMessageBox = new MyMessageBox();
                    //myMessageBox.Owner = this;
                    myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
          
                    myMessageBox.MessageTitle.Text = "GREŠKA";
                    myMessageBox.MessageText.Text = "Pogrešna lozinka" + Environment.NewLine + "Pokušajte ponovo, preostalo " + pokusaj + " pokušaja.";
                    myMessageBox.ShowDialog();
                  
                    _pin = "";
                    PinEntry.Password = "";
                    PinEntry.Focus();
                }
                else
                {
                    Debug.WriteLine("Otvara");
                    Debug.WriteLine ("Navigator set? " + (PageNavigator.Navigate != null));

                    Globals.ulogovaniKorisnik = radnik;
                    var page = new HomePage ();
                    page.DataContext = new HomeViewModel ();
                    PageNavigator.NavigateWithFade (page);


                }
}
        }

            private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PinEntry_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _pin = PinEntry.Password;
                OnOkButtonClicked(null, null);
                e.Handled = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            Dispatcher.BeginInvoke(new Action(() =>
            {
                PinEntry.Focus();
            Keyboard.Focus(PinEntry);
            PinEntry.SelectAll();
                MainContent.Visibility = Visibility.Visible;
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }
    }
}