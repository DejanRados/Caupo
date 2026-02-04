
using Caupo.Data;
using Caupo.Helpers;
using Caupo.Properties;
using Caupo.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

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

            InitializeComponent ();

            this.DataContext = new LoginPageViewModel ();

        }

        private async void FadeInOut(object sender)
        {
            if(sender is Button button)
            {
                DoubleAnimation fadeInAnimation = new DoubleAnimation
                {
                    From = 0, // Start at fully transparent
                    To = 1,   // End at fully visible
                    Duration = new Duration (TimeSpan.FromSeconds (1)) // Duration of 2 seconds
                };


                button.BeginAnimation (UIElement.OpacityProperty, fadeInAnimation);
            }
        }

        private void OnNumericButtonClicked(object sender, EventArgs e)
        {
            if(sender is Button button)
            {
                FadeInOut (button);
                _pin += button.Content;
                PinEntry.Password = new string ('●', _pin.Length);
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
            Debug.WriteLine ("---------------------------------------Password:" + _pin);
            pokusaj -= 1;
            if(pokusaj < 1)
            {




                MyMessageBox myMessageBox = new MyMessageBox ();
                //myMessageBox.Owner = this;
                myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                myMessageBox.MessageTitle.Text = "GREŠKA";
                myMessageBox.MessageText.Text = "Pogrešna lozinka" + Environment.NewLine + "Nemate pristup aplikaciji";
                myMessageBox.ShowDialog ();
                Application.Current.Shutdown ();
            }

            using(var db = new AppDbContext ())
            {

                var radnik = await db.Radnici
                                      .Where (r => r.Lozinka == _pin)

                                      .FirstOrDefaultAsync ();
                Debug.WriteLine (radnik);
                if(radnik == null)
                {



                    MyMessageBox myMessageBox = new MyMessageBox ();
                    //myMessageBox.Owner = this;
                    myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                    myMessageBox.MessageTitle.Text = "GREŠKA";
                    myMessageBox.MessageText.Text = "Pogrešna lozinka" + Environment.NewLine + "Pokušajte ponovo, preostalo " + pokusaj + " pokušaja.";
                    myMessageBox.ShowDialog ();

                    _pin = "";
                    PinEntry.Password = "";
                    PinEntry.Focus ();
                }
                else
                {
                    Debug.WriteLine ("Otvara");
                    Debug.WriteLine ("Navigator set? " + (PageNavigator.Navigate != null));

                    Globals.ulogovaniKorisnik = radnik;

                    string savedIndexStr = Settings.Default.DisplayKuhinja;
                    Debug.WriteLine("[KitchenDisplay] DisplayKuhinja = " + savedIndexStr);

                    var monitors = MonitorHelper.GetMonitors();
                    Debug.WriteLine("[KitchenDisplay] Broj monitora: " + monitors.Count);

                    MonitorInfo targetMonitor = null;

                    // Ako ima više od jednog monitora
                    if (monitors.Count > 1)
                    {
                        // 1. Ako postoji savedIndexStr → koristi samo ako NIJE primarni
                        if (int.TryParse(savedIndexStr, out int savedIndex))
                        {
                            var savedMonitor = monitors.FirstOrDefault(m => m.Index == savedIndex);

                            if (savedMonitor != null && !savedMonitor.IsPrimary)
                            {
                                targetMonitor = savedMonitor;
                                Debug.WriteLine("[KitchenDisplay] Koristim saved monitor: " + savedIndex);
                            }
                            else
                            {
                                Debug.WriteLine("[KitchenDisplay] Saved monitor je primarni ili ne postoji – ignorišem");
                            }
                        }

                        // 2. Ako nema validnog saved monitora → prvi ne-primarni
                        if (targetMonitor == null)
                        {
                            targetMonitor = monitors.FirstOrDefault(m => !m.IsPrimary);

                            if (targetMonitor != null)
                            {
                                Debug.WriteLine("[KitchenDisplay] Koristim prvi ne-primarni monitor: " + targetMonitor.Index);
                            }
                        }
                    }

                    // ⛔ NEMA return-a, samo uslovno kreiranje prozora
                    if (targetMonitor != null)
                    {
                        var window = new KitchenDisplay
                        {
                            WindowStyle = WindowStyle.None,
                            ResizeMode = ResizeMode.NoResize,
                            WindowStartupLocation = WindowStartupLocation.Manual,
                            Left = targetMonitor.Bounds.Left,
                            Top = targetMonitor.Bounds.Top,
                            Width = targetMonitor.Bounds.Width,
                            Height = targetMonitor.Bounds.Height
                        };

                        window.Show();
                        window.WindowState = WindowState.Normal;
                    }
                    else
                    {
                        Debug.WriteLine("[KitchenDisplay] KitchenDisplay nije prikazan (nema dozvoljenog monitora)");
                    }

                    // 👇 ostatak metode ide dalje bez prekida
                    var page = new HomePage();
                    page.DataContext = new HomeViewModel();
                    PageNavigator.NavigateWithFade(page);


                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown ();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PinEntry_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == System.Windows.Input.Key.Enter)
            {
                _pin = PinEntry.Password;
                OnOkButtonClicked (null, null);
                e.Handled = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            Dispatcher.BeginInvoke (new Action (() =>
            {
                PinEntry.Focus ();
                Keyboard.Focus (PinEntry);
                PinEntry.SelectAll ();
                MainContent.Visibility = Visibility.Visible;
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }
    }
}