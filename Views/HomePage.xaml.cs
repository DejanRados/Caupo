using Caupo.Data;
using Caupo.Helpers;
using Caupo.Server;
using Caupo.Services;
using Caupo.ViewModels;
using Caupo.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : UserControl
    {
        public HomePage()
        {
            InitializeComponent ();
            this.DataContext = new HomeViewModel ();
            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik.Radnik;

        }

        private void FadeInOut(object sender)
        {
            if(sender is Button button)
            {
                DoubleAnimation fadeInAnimation = new DoubleAnimation
                {
                    From = 0, // Start at fully transparent
                    To = 1,   // End at fully visible
                    Duration = new Duration (TimeSpan.FromSeconds (1.5)) // Duration of 2 seconds
                };


                button.BeginAnimation (UIElement.OpacityProperty, fadeInAnimation);
            }
        }





        private void KasaButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button as Button);
                var page = new KasaPage ();
                page.DataContext = new KasaViewModel ();
                PageNavigator.NavigateWithFade (page);

            }
        }

        private void StoloviButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                Globals.forma = "Home";
                FadeInOut (button as Button);
                var page = new OrdersPage (null);
                //page.DataContext = new OrdersViewModel (null);
                PageNavigator.NavigateWithFade (page);

            }
        }

        private void RacuniButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button as Button);
                var page = new ReceiptsPage ();
                //page.DataContext = new ReceiptsViewModel ();
                PageNavigator.NavigateWithFade (page);


            }
        }

        private void ArtikliButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button as Button);
                var page = new ArticlesPage ();
                //page.DataContext = new ArticlesViewModel ();
                PageNavigator.NavigateWithFade (page);


            }
        }

        private void UlazPiceButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button as Button);
                var page = new BeverageInPage ();
                //page.DataContext = new BeverageInPageViewModel ();
                PageNavigator.NavigateWithFade (page);


            }
        }

        private void UlazNamirnicaButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button as Button);
                var page = new FoodInPage ();
                //page.DataContext = new FoodInViewModel ();
                PageNavigator.NavigateWithFade (page);


            }
        }

        private void PodesavanjeButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {

                FadeInOut (button as Button);
                var page = new SettingsPage ();
                //page.DataContext = new SettingsViewModel ();
                PageNavigator.NavigateWithFade (page);

            }
        }

        private void KnjigaSankaButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                var db = new AppDbContext ();
                var service = new KnjigaSankaService (db);
                FadeInOut (button as Button);
                var page = new KnjigaSankaPage ();
                //page.DataContext = new KnjigaSankaViewModel (service);
                PageNavigator.NavigateWithFade (page);

            }
        }

        private void KnjigaKuhinjeButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                var db = new AppDbContext ();
                var service = new KnjigaKuhinjeService (db);
                FadeInOut (button as Button);
                var page = new KnjigaKuhinjePage ();
                //page.DataContext = new KnjigaKuhinjeViewModel (service);
                PageNavigator.NavigateWithFade (page);

            }
        }

        private void NamirniceButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button as Button);
                var page = new IngredientsPage ();
                //page.DataContext = new IngredientsViewModel ();
                PageNavigator.NavigateWithFade (page);


            }
        }

        private void NormativiButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button as Button);
                var page = new NormsPage ();
                //page.DataContext = new NormsViewModel ();
                PageNavigator.NavigateWithFade (page);

            }
        }

        private void NivelacijaButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button as Button);
            }
        }

        private void KupciButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button as Button);
                var page = new BuyersPage ();
                //page.DataContext = new BuyersViewModel ();
                PageNavigator.NavigateWithFade (page);
            }
        }

        private void AnalizaButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button as Button);
                var page = new DashboardPage ();
                //page.DataContext = new DashboardViewModel ();
                PageNavigator.NavigateWithFade (page);


            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown ();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine ("Treba da pokrene server.");
            var dbPath = Properties.Settings.Default.DbPath;
            var connectionString = $"Data Source={dbPath};Version=3;";

            try
            {
                var server = new TcpIpServer (5000, connectionString);
                _ = Task.Run (async () => await server.StartAsync ());

                Debug.WriteLine ("Server je uspješno pokrenut.");
                imgServer.Source = new BitmapImage (new Uri ("pack://application:,,,/Images/green.png"));


                // Dohvati sve aktivne IPv4 adrese osim loopbacka
                var ipAddresses = NetworkInterface.GetAllNetworkInterfaces ()
                    .Where (ni => ni.OperationalStatus == OperationalStatus.Up)
                    .SelectMany (ni => ni.GetIPProperties ().UnicastAddresses
                        .Where (addr => addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                                       !IPAddress.IsLoopback (addr.Address))
                        .Select (addr => $"{ni.Name} ({ni.NetworkInterfaceType}): {addr.Address}"))
                    .ToList ();

                // Spoji ih u jedan string za labelu
                var ipListString = string.Join (Environment.NewLine, ipAddresses);
                lblServer.Content = $"Server je pokrenut na:\n{ipListString}";

            }
            catch(Exception ex)
            {
                // Ako je došlo do greške prilikom pokretanja servera
                Debug.WriteLine ("Greška prilikom pokretanja servera: " + ex.Message);
                imgServer.Source = new BitmapImage (new Uri ("pack://application:,,,/Images/red.png"));
                lblServer.Content = $"Greška prilikom pokretanja servera: " + ex.Message;

            }
        }

        private void DobavljaciButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button as Button);
                var page = new SuppliersPage ();
                //page.DataContext = new SuppliersViewModel ();
                PageNavigator.NavigateWithFade (page);
            }
        }

        private void LicencaButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button as Button);
                var page = new LicenseActivationPage();
                ////page.DataContext = new SuppliersViewModel ();
                PageNavigator.NavigateWithFade (page);
            }
        }

        private void LicenseButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button as Button);
                var page = new LicensePaymentPage();
                ////page.DataContext = new SuppliersViewModel ();
                PageNavigator.NavigateWithFade (page);
            }
        }
    }
}
