using Caupo.Data;
using Caupo.Helpers;
using Caupo.Properties;
using Caupo.Views;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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

            if(tema == "Tamna")
            {

                Application.Current.Resources["GlobalFontColor"] = new SolidColorBrush (System.Windows.Media.Color.FromRgb (244, 244, 244));
                Application.Current.Resources["GlobalBackgroundColor"] = new SolidColorBrush (System.Windows.Media.Color.FromRgb (45, 45, 48));
                var brushFont = Application.Current.Resources["GlobalFontColor"] as SolidColorBrush;
                var brushBack = Application.Current.Resources["GlobalBackgroundColor"] as SolidColorBrush;
                Debug.WriteLine ("Aktivna tema koju vidi viewmodel je : " + tema + " i postavio je GlobalFontColor " + $"#{brushFont.Color.R:X2}{brushFont.Color.G:X2}{brushFont.Color.B:X2}");
                Debug.WriteLine ("Aktivna tema koju vidi viewmodel je : " + tema + " i postavio je GlobalBackgroundColor " + $"#{brushBack.Color.R:X2}{brushBack.Color.G:X2}{brushBack.Color.B:X2} ");
            }
            else
            {
                Application.Current.Resources["GlobalFontColor"] = new SolidColorBrush (System.Windows.Media.Color.FromRgb (45, 45, 48));
                Application.Current.Resources["GlobalBackgroundColor"] = new SolidColorBrush (System.Windows.Media.Color.FromRgb (244, 244, 244));
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
            //Settings.Default.Key = "";
            //Settings.Default.Email = "";
            //Settings.Default.HardwareFingerprint = "";
            //Settings.Default.LicenseType = "";
            //Settings.Default.CompanyName = "";
            //Settings.Default.ExpirationDate = "";
            // Settings.Default.LastActivation = "";
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


            // Ovdje izvrši provjeru licence
            var result = await Helpers.LicenseManager.ValidateOnStartup ();

            if(result != null && result.Success)
            {

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
                    return;
                }


                CurrentPage = new Caupo.Views.LoginPage () { DataContext = new LoginPageViewModel () };
            }
            else
            {

                var owner = Application.Current.Windows.OfType<Window> ().FirstOrDefault (w => w.IsActive);



                switch(result?.Code)
                {
                    case "LICENSE_NOT_ACTIVATED":
                        var myMessageBox = new MyMessageBox
                        {
                            Owner = owner,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner
                        };
                        myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        myMessageBox.MessageTitle.Text = "Obavještenje";
                        myMessageBox.MessageText.Text = result?.Message ?? "Licenca nije validna";
                        myMessageBox.ShowDialog ();
                        CurrentPage = new LicenseActivationPage ();
                        break;

                    case "LICENSE_EXPIRED":
                        var myMessageBox2 = new MyMessageBox
                        {
                            Owner = owner,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner
                        };
                        myMessageBox2.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        myMessageBox2.MessageTitle.Text = "Obavještenje";
                        myMessageBox2.MessageText.Text = result?.Message ?? "Licenca nije validna";
                        myMessageBox2.ShowDialog ();
                        CurrentPage = new LicensePaymentPage ();
                        break;

                    default:
                        CurrentPage = new LicensePaymentPage ();
                        break;
                }

                //CurrentPage = new Caupo.Views.LoginPage () { DataContext = new LoginPageViewModel () };
                //CurrentPage = new LicenseActivationPage ();
            }
        }





        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (name));
        }


    }
}