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
        private const int MaxPokusaja = 3;

        private int _preostaliPokusaji =
            MaxPokusaja;

        private bool _loginUToku;


        public LoginPage()
        {
            InitializeComponent ();

            DataContext =
                new LoginPageViewModel ();
        }


        // =====================================================
        // ANIMACIJA TIPKE
        // =====================================================

        private void FadeInOut(
            object sender)
        {
            if(sender is not Button button)
                return;


            var fadeInAnimation =
                new DoubleAnimation
                {
                    From = 0,
                    To = 1,

                    Duration =
                        new Duration (
                            TimeSpan.FromSeconds (1))
                };


            button.BeginAnimation (
                UIElement.OpacityProperty,
                fadeInAnimation);
        }


        // =====================================================
        // NUMERIČKA TASTATURA
        // =====================================================

        private void OnNumericButtonClicked(
            object sender,
            RoutedEventArgs e)
        {
            if(sender is not Button button)
                return;


            FadeInOut (button);


            string number =
                button.Content?.ToString ()
                ?? string.Empty;


            if(string.IsNullOrWhiteSpace (number))
                return;


            /*
             * PasswordBox sam prikazuje maskirane znakove.
             *
             * Nema potrebe za dodatnim _pin stringom niti za
             * ručnim upisivanjem ●●●●.
             *
             * I fizička i ekranska tastatura sada koriste
             * potpuno isti PIN.
             */

            PinEntry.Password +=
                number;


            PinEntry.Focus ();
        }


        // =====================================================
        // CLEAR
        // =====================================================

        private void OnClearButtonClicked(
            object sender,
            RoutedEventArgs e)
        {
            ClearPin ();

            PinEntry.Focus ();
        }


        // =====================================================
        // OK
        // =====================================================

        private async void OnOkButtonClicked(
            object sender,
            RoutedEventArgs e)
        {
            await TryLoginAsync ();
        }


        // =====================================================
        // LOGIN
        // =====================================================

        private async Task TryLoginAsync()
        {
            if(_loginUToku)
                return;


            string pin =
                PinEntry.Password;


            if(string.IsNullOrWhiteSpace (pin))
            {
                ShowLoginError (
                    "Unesite lozinku.");

                PinEntry.Focus ();

                return;
            }


            try
            {
                _loginUToku =
                    true;

                OkButton.IsEnabled =
                    false;


                Debug.WriteLine (
                    "[LOGIN] Provjera korisnika...");


                using var db =
                    new AppDbContext ();


                var radnik =
                    await db.Radnici
                        .FirstOrDefaultAsync (
                            r => r.Lozinka == pin);


                // =====================================================
                // POGREŠAN PIN
                // =====================================================

                if(radnik == null)
                {
                    _preostaliPokusaji--;


                    Debug.WriteLine (
                        "[LOGIN] Pogrešna lozinka.");

                    Debug.WriteLine (
                        $"[LOGIN] Preostalo pokušaja: {_preostaliPokusaji}");


                    ClearPin ();


                    if(_preostaliPokusaji <= 0)
                    {
                        ShowLoginError (
                            "Pogrešna lozinka" +
                            Environment.NewLine +
                            "Nemate pristup aplikaciji.");


                        Application.Current.Shutdown ();

                        return;
                    }


                    ShowLoginError (
                        "Pogrešna lozinka" +
                        Environment.NewLine +
                        "Pokušajte ponovo, preostalo " +
                        _preostaliPokusaji +
                        " pokušaja.");


                    PinEntry.Focus ();

                    return;
                }


                // =====================================================
                // USPJEŠAN LOGIN
                // =====================================================

                Debug.WriteLine (
                    "[LOGIN] Prijava uspješna.");

                Debug.WriteLine (
                    $"[LOGIN] Radnik ID: {radnik.IdRadnika}");

                Debug.WriteLine (
                    "Navigator set? " +
                    (PageNavigator.Navigate != null));


                Globals.ulogovaniKorisnik =
                    radnik;


                // =====================================================
                // KUHINJSKI DISPLAY
                // =====================================================

                OpenKitchenDisplay ();


                // =====================================================
                // HOME PAGE
                // =====================================================

                var page =
                    new HomePage
                    {
                        DataContext =
                            new HomeViewModel ()
                    };


                PageNavigator.NavigateWithFade (
                    page);
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[LOGIN] Greška pri prijavi: {ex}");


                ShowLoginError (
                    "Greška pri prijavi." +
                    Environment.NewLine +
                    Environment.NewLine +
                    ex.Message);
            }
            finally
            {
                _loginUToku =
                    false;


                if(OkButton != null)
                {
                    OkButton.IsEnabled =
                        true;
                }
            }
        }


        // =====================================================
        // KITCHEN DISPLAY
        // =====================================================

        private void OpenKitchenDisplay()
        {
            try
            {
                string savedIndexStr =
                    Settings.Default.DisplayKuhinja;


                Debug.WriteLine (
                    "[KitchenDisplay] DisplayKuhinja = " +
                    savedIndexStr);


                var monitors =
                    MonitorHelper.GetMonitors ();


                Debug.WriteLine (
                    "[KitchenDisplay] Broj monitora: " +
                    monitors.Count);


                MonitorInfo? targetMonitor =
                    null;


                // =====================================================
                // KITCHEN DISPLAY SAMO AKO POSTOJI VIŠE MONITORA
                // =====================================================

                if(monitors.Count > 1)
                {
                    // -------------------------------------------------
                    // 1. Pokušaj koristiti spremljeni monitor
                    // -------------------------------------------------

                    if(int.TryParse (
                        savedIndexStr,
                        out int savedIndex))
                    {
                        MonitorInfo? savedMonitor =
                            monitors.FirstOrDefault (
                                m => m.Index == savedIndex);


                        /*
                         * Nikada ne otvaramo KitchenDisplay
                         * na primarnom monitoru.
                         */

                        if(savedMonitor != null &&
                           !savedMonitor.IsPrimary)
                        {
                            targetMonitor =
                                savedMonitor;


                            Debug.WriteLine (
                                "[KitchenDisplay] Koristim saved monitor: " +
                                savedIndex);
                        }
                        else
                        {
                            Debug.WriteLine (
                                "[KitchenDisplay] Saved monitor je primarni " +
                                "ili više ne postoji - ignorišem.");
                        }
                    }


                    // -------------------------------------------------
                    // 2. Ako spremljeni nije dostupan,
                    //    uzmi prvi sekundarni monitor
                    // -------------------------------------------------

                    if(targetMonitor == null)
                    {
                        targetMonitor =
                            monitors.FirstOrDefault (
                                m => !m.IsPrimary);


                        if(targetMonitor != null)
                        {
                            Debug.WriteLine (
                                "[KitchenDisplay] Koristim prvi " +
                                "ne-primarni monitor: " +
                                targetMonitor.Index);
                        }
                    }
                }


                // =====================================================
                // NEMA SEKUNDARNOG MONITORA
                // =====================================================

                if(targetMonitor == null)
                {
                    Debug.WriteLine (
                        "[KitchenDisplay] KitchenDisplay nije prikazan " +
                        "(nema dozvoljenog monitora).");

                    return;
                }


                // =====================================================
                // OTVORI KITCHEN DISPLAY
                // =====================================================

                var window =
                    new KitchenDisplay
                    {
                        WindowStyle =
                            WindowStyle.None,

                        ResizeMode =
                            ResizeMode.NoResize,

                        WindowStartupLocation =
                            WindowStartupLocation.Manual,

                        Left =
                            targetMonitor.Bounds.Left,

                        Top =
                            targetMonitor.Bounds.Top,

                        Width =
                            targetMonitor.Bounds.Width,

                        Height =
                            targetMonitor.Bounds.Height
                    };


                window.Show ();


                window.WindowState =
                    WindowState.Normal;


                Debug.WriteLine (
                    "[KitchenDisplay] KitchenDisplay otvoren.");
            }
            catch(Exception ex)
            {
                /*
                 * Greška KitchenDisplay-a ne smije
                 * spriječiti korisnika da se prijavi.
                 */

                Debug.WriteLine (
                    $"[KitchenDisplay] Greška pri otvaranju: {ex}");
            }
        }


        // =====================================================
        // MESSAGE BOX
        // =====================================================

        private static void ShowLoginError(
            string message)
        {
            var myMessageBox =
                new MyMessageBox
                {
                    WindowStartupLocation =
                        WindowStartupLocation.CenterScreen
                };


            myMessageBox.MessageTitle.Text =
                "GREŠKA";


            myMessageBox.MessageText.Text =
                message;


            myMessageBox.ShowDialog ();
        }


        // =====================================================
        // CLEAR PIN
        // =====================================================

        private void ClearPin()
        {
            PinEntry.Password =
                string.Empty;
        }


        // =====================================================
        // CLOSE
        // =====================================================

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Application.Current.Shutdown ();
        }


        // =====================================================
        // FIZIČKA TASTATURA
        // =====================================================

        private async void PinEntry_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if(e.Key != Key.Enter)
                return;


            /*
             * PasswordBox već sadrži pravi PIN.
             * Nema više kopiranja u _pin.
             */

            e.Handled =
                true;


            await TryLoginAsync ();
        }


        // =====================================================
        // LOADED
        // =====================================================

        private void Window_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke (
                new Action (
                    () =>
                    {
                        PinEntry.Focus ();

                        Keyboard.Focus (
                            PinEntry);

                        PinEntry.SelectAll ();

                        MainContent.Visibility =
                            Visibility.Visible;
                    }),
                System.Windows.Threading
                    .DispatcherPriority
                    .ApplicationIdle);
        }
    }
}