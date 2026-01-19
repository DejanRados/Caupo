using Caupo.Data;
using Caupo.Helpers;
using Caupo.Services;
using Caupo.ViewModels;
using QRCoder;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;


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
                    Duration = new Duration (TimeSpan.FromSeconds (0.5)) // Duration of 2 seconds
                };


                button.BeginAnimation (UIElement.OpacityProperty, fadeInAnimation);
            }
        }





        private void KasaButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button);
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
                FadeInOut (button);
                var page = new OrdersPage (null);
                //page.DataContext = new OrdersViewModel (null);
                PageNavigator.NavigateWithFade (page);

            }
        }

        private void RacuniButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button);
                var page = new ReceiptsPage ();
                //page.DataContext = new ReceiptsViewModel ();
                PageNavigator.NavigateWithFade (page);


            }
        }

        private void ArtikliButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button);
                var page = new ArticlesPage ();
                //page.DataContext = new ArticlesViewModel ();
                PageNavigator.NavigateWithFade (page);


            }
        }

        private void UlazPiceButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button);
                var page = new BeverageInPage ();
                //page.DataContext = new BeverageInPageViewModel ();
                PageNavigator.NavigateWithFade (page);


            }
        }

        private void UlazNamirnicaButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button);
                var page = new FoodInPage ();
                //page.DataContext = new FoodInViewModel ();
                PageNavigator.NavigateWithFade (page);


            }
        }

        private void PodesavanjeButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {

                FadeInOut (button);
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
                FadeInOut (button);
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
                FadeInOut (button);
                var page = new KnjigaKuhinjePage ();
                //page.DataContext = new KnjigaKuhinjeViewModel (service);
                PageNavigator.NavigateWithFade (page);

            }
        }

        private void NamirniceButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button);
                var page = new IngredientsPage ();
                //page.DataContext = new IngredientsViewModel ();
                PageNavigator.NavigateWithFade (page);


            }
        }

        private void NormativiButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button);
                var page = new NormsPage ();
                //page.DataContext = new NormsViewModel ();
                PageNavigator.NavigateWithFade (page);

            }
        }

        private void NivelacijaButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button);
            }
        }

        private void KupciButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button);
                var page = new BuyersPage ();
                //page.DataContext = new BuyersViewModel ();
                PageNavigator.NavigateWithFade (page);
            }
        }

        private void AnalizaButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button);
                var page = new DashboardPage ();
                //page.DataContext = new DashboardViewModel ();
                PageNavigator.NavigateWithFade (page);


            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown ();
        }

        private Bitmap _qrBitmap;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            try
            {


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

                var ipAddress = NetworkInterface.GetAllNetworkInterfaces ()
                 .Where (ni => ni.OperationalStatus == OperationalStatus.Up)
                 .SelectMany (ni => ni.GetIPProperties ().UnicastAddresses
                     .Where (addr => addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                                    !IPAddress.IsLoopback (addr.Address))
                     .Select (addr => $"{addr.Address}"))
                 .ToList ();

                // Spoji ih u jedan string za labelu
                var ipListString = string.Join (Environment.NewLine, ipAddresses);
                lblServer.Content = $"Server je pokrenut na:\n{ipListString}";

                if(ipAddress.Any ())
                {
                    var firstIp = ipAddress.First ();
                    int port = 5000;

                    // Format koji je lako parsirati na Androidu
                    // Koristite jednostavan separator kao što je "|"
                    string qrText = $"{firstIp}|{port}";

                    using(QRCodeGenerator qrGenerator = new QRCodeGenerator ())
                    {
                        QRCodeData qrCodeData = qrGenerator.CreateQrCode (qrText, QRCodeGenerator.ECCLevel.Q);
                        using(QRCode qrCode = new QRCode (qrCodeData))
                        {
                            // GetGraphic vraća System.Drawing.Bitmap
                            System.Drawing.Bitmap qrBitmap = qrCode.GetGraphic (
                                pixelsPerModule: 20,
                                darkColor: System.Drawing.Color.Black,
                                lightColor: System.Drawing.Color.White,
                                drawQuietZones: true
                            );

                            // Konvertuj System.Drawing.Bitmap u WPF BitmapSource
                            qrCodeImage.Source = BitmapToImageSource (qrBitmap);

                            // Očisti bitmap ako više ne treba
                            qrBitmap.Dispose ();
                        }
                    }

                    Debug.WriteLine ($"QR kod generisan: {qrText}");

                }
            }
            catch(Exception ex)
            {
                // Ako je došlo do greške prilikom pokretanja servera
                Debug.WriteLine ("Greška prilikom pokretanja servera: " + ex.Message);
                imgServer.Source = new BitmapImage (new Uri ("pack://application:,,,/Images/red.png"));
                lblServer.Content = $"Greška prilikom pokretanja servera: " + ex.Message;

            }
        }

        private BitmapSource BitmapToImageSource(System.Drawing.Bitmap bitmap)
        {
            using(var memory = new System.IO.MemoryStream ())
            {
                bitmap.Save (memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                var bitmapImage = new BitmapImage ();
                bitmapImage.BeginInit ();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit ();
                bitmapImage.Freeze ();
                return bitmapImage;
            }
        }

        private void DobavljaciButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button);
                var page = new SuppliersPage ();
                //page.DataContext = new SuppliersViewModel ();
                PageNavigator.NavigateWithFade (page);
            }
        }

        private void LicencaButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button);
                var page = new LicenseActivationPage ();
                ////page.DataContext = new SuppliersViewModel ();
                PageNavigator.NavigateWithFade (page);
            }
        }

        private void LicenseButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button);
                var page = new LicensePaymentPage ();
                ////page.DataContext = new SuppliersViewModel ();
                PageNavigator.NavigateWithFade (page);
            }
        }
    }
}
