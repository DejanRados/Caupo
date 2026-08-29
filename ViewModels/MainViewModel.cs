using Caupo.Data;
using Caupo.Helpers;
using Caupo.Properties;
using Caupo.Views;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using static Caupo.Data.DatabaseTables;

using CaupoLicenseManager = Caupo.Helpers.LicenseManager;

namespace Caupo.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // =====================================================
        // CURRENT PAGE
        // =====================================================

        private object? _currentPage;

        public object? CurrentPage
        {
            get => _currentPage;

            set
            {
                if(ReferenceEquals (
                    _currentPage,
                    value))
                {
                    return;
                }

                _currentPage =
                    value;

                OnPropertyChanged ();
            }
        }


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public MainViewModel()
        {
            Debug.WriteLine (
                "[MAIN] MainViewModel kreiran.");


            Debug.WriteLine (
                "[SETTINGS] user.config = " +
                ConfigurationManager
                    .OpenExeConfiguration (
                        ConfigurationUserLevel
                            .PerUserRoamingAndLocal)
                    .FilePath);


            /*
             * Navigacija se više NE postavlja ovdje.
             *
             * PageNavigator.Navigate se konfigurira
             * samo jednom u App.InitializeNavigation().
             */


            /*
             * Startup pokrećemo nakon što WPF završi
             * inicijalno kreiranje MainWindow-a.
             */

            Application.Current.Dispatcher.BeginInvoke (
                new Action (
                    async () =>
                    {
                        await StartApplicationAsync ();
                    }));
        }


        // =====================================================
        // STARTUP
        // =====================================================

        private async Task StartApplicationAsync()
        {
            try
            {
                Debug.WriteLine (
                    "[MAIN] Pokrećem inicijalizaciju aplikacije.");


                // -------------------------------------------------
                // 1. DATABASE
                // -------------------------------------------------

                bool databaseAvailable =
                    await CheckDatabaseAsync ();


                if(!databaseAvailable)
                {
                    Debug.WriteLine (
                        "[MAIN] Startup prekinut jer baza nije dostupna.");

                    return;
                }


                // -------------------------------------------------
                // 2. THEME
                // -------------------------------------------------

                SetColors ();


                // -------------------------------------------------
                // 3. LICENSE
                // -------------------------------------------------

                Debug.WriteLine (
                    "[LICENSE] Provjera licence...");


                CaupoLicenseManager.ActivationResponse? result =
                    await CaupoLicenseManager
                        .ValidateOnStartup ();


                // -------------------------------------------------
                // 4. VALID LICENSE
                // -------------------------------------------------

                if(result != null &&
                   result.Success)
                {
                    Debug.WriteLine (
                        $"[LICENSE] Licenca je validna. " +
                        $"Code={result.Code}");


                    HandleValidLicense ();

                    return;
                }


                // -------------------------------------------------
                // 5. INVALID LICENSE
                // -------------------------------------------------

                HandleInvalidLicense (
                    result);
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[MAIN] Neočekivana greška u startupu: {ex}");


                MessageBox.Show (
                    "Dogodila se greška prilikom pokretanja aplikacije."
                    + Environment.NewLine
                    + Environment.NewLine
                    + ex.Message,
                    "Greška",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        // =====================================================
        // DATABASE
        // =====================================================

        private async Task<bool> CheckDatabaseAsync()
        {
            try
            {
                Debug.WriteLine (
                    "[DB] Provjera veze sa bazom...");


                await using var context =
                    new AppDbContext ();


                bool canConnect =
                    await context.Database
                        .CanConnectAsync ();


                if(canConnect)
                {
                    Debug.WriteLine (
                        "[DB] Veza sa bazom je uspješna.");

                    return true;
                }


                Debug.WriteLine (
                    "[DB] Nije moguće povezivanje sa bazom.");


                MessageBox.Show (
                    "Ne mogu se spojiti na bazu podataka!"
                    + Environment.NewLine
                    + "Provjerite da li je baza dostupna.",
                    "Greška baze",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);


                return false;
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[DB] Greška pri povezivanju: {ex}");


                MessageBox.Show (
                    "Greška pri povezivanju sa bazom:"
                    + Environment.NewLine
                    + Environment.NewLine
                    + ex.Message,
                    "Greška baze",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);


                return false;
            }
        }


        // =====================================================
        // COLORS / THEME
        // =====================================================

        public void SetColors()
        {
            string tema =
                Settings.Default.Tema
                ?? string.Empty;


            bool darkTheme =
                string.Equals (
                    tema,
                    "Tamna",
                    StringComparison.OrdinalIgnoreCase);


            Color fontColor =
                darkTheme
                    ? Color.FromRgb (
                        244,
                        244,
                        244)
                    : Color.FromRgb (
                        45,
                        45,
                        48);


            Color backgroundColor =
                darkTheme
                    ? Color.FromRgb (
                        45,
                        45,
                        48)
                    : Color.FromRgb (
                        244,
                        244,
                        244);


            Application.Current.Resources[
                "GlobalFontColor"] =
                new SolidColorBrush (
                    fontColor);


            Application.Current.Resources[
                "GlobalBackgroundColor"] =
                new SolidColorBrush (
                    backgroundColor);


            Debug.WriteLine (
                $"[THEME] Tema: {tema}");


            Debug.WriteLine (
                "[THEME] GlobalFontColor = " +
                $"#{fontColor.R:X2}" +
                $"{fontColor.G:X2}" +
                $"{fontColor.B:X2}");


            Debug.WriteLine (
                "[THEME] GlobalBackgroundColor = " +
                $"#{backgroundColor.R:X2}" +
                $"{backgroundColor.G:X2}" +
                $"{backgroundColor.B:X2}");
        }


        // =====================================================
        // VALID LICENSE
        // =====================================================

        private void HandleValidLicense()
        {
            /*
             * Ako osnovni podaci firme nisu uneseni,
             * korisnika šaljemo direktno u SettingsPage.
             */

            if(!CompanySettingsComplete ())
            {
                Debug.WriteLine (
                    "[MAIN] Podaci firme nisu kompletni. " +
                    "Otvaram SettingsPage.");


                Globals.ulogovaniKorisnik =
                    new TblRadnici
                    {
                        Radnik =
                            "Admin"
                    };


                var page =
                    new SettingsPage ();


                PageNavigator.NavigateWithFade (
                    page);


                return;
            }


            Debug.WriteLine (
                "[MAIN] Otvaram LoginPage.");


            CurrentPage =
                new LoginPage ();
        }


        // =====================================================
        // COMPANY SETTINGS
        // =====================================================

        private static bool CompanySettingsComplete()
        {
            return
                !string.IsNullOrWhiteSpace (
                    Settings.Default.Firma)

                &&

                !string.IsNullOrWhiteSpace (
                    Settings.Default.Adresa)

                &&

                !string.IsNullOrWhiteSpace (
                    Settings.Default.Mjesto)

                &&

                !string.IsNullOrWhiteSpace (
                    Settings.Default.JIB)

                &&

                !string.IsNullOrWhiteSpace (
                    Settings.Default.PDV)

                &&

                !string.IsNullOrWhiteSpace (
                    Settings.Default.ZR)

                &&

                !string.IsNullOrWhiteSpace (
                    Settings.Default.Email);
        }


        // =====================================================
        // INVALID LICENSE
        // =====================================================

        private void HandleInvalidLicense(
            CaupoLicenseManager.ActivationResponse? result)
        {
            Window? owner =
                Application.Current.Windows
                    .OfType<Window> ()
                    .FirstOrDefault (
                        w => w.IsActive);


            string code =
                result?.Code
                ?? CaupoLicenseManager
                    .CodeValidationError;


            string message =
                result?.Message
                ?? "Licenca nije validna.";


            Debug.WriteLine (
                $"[LICENSE] Licenca nije validna. " +
                $"Code={code}, Message={message}");


            switch(code)
            {
                // =================================================
                // LICENSE_NOT_ACTIVATED
                // =================================================

                case CaupoLicenseManager.CodeLicenseNotActivated:

                    ShowLicenseMessage (
                        owner,
                        message);


                    OpenLicenseActivationPage ();

                    break;


                // =================================================
                // LICENSE_NOT_FOUND
                // =================================================

                case CaupoLicenseManager.CodeLicenseNotFound:

                    ShowLicenseMessage (
                        owner,
                        message);


                    OpenLicenseActivationPage ();

                    break;


                // =================================================
                // LICENSE_EXPIRED
                // =================================================

                case CaupoLicenseManager.CodeLicenseExpired:

                    ShowLicenseMessage (
                        owner,
                        message);


                    OpenLicensePaymentPage ();

                    break;


                // =================================================
                // LICENSE_INACTIVE
                // =================================================

                case CaupoLicenseManager.CodeLicenseInactive:

                    ShowLicenseMessage (
                        owner,
                        message);


                    OpenLicensePaymentPage ();

                    break;


                // =================================================
                // ACTIVATION_LIMIT_REACHED
                // =================================================

                case CaupoLicenseManager.CodeActivationLimitReached:

                    if(result != null)
                    {
                        ShowLicenseMessage (
                            owner,
                            BuildActivationLimitMessage (
                                result));
                    }
                    else
                    {
                        ShowLicenseMessage (
                            owner,
                            message);
                    }


                    OpenLicenseActivationPage ();

                    break;


                // =================================================
                // NETWORK_ERROR
                // =================================================

                case CaupoLicenseManager.CodeNetworkError:

                    ShowLicenseMessage (
                        owner,
                        message);


                    OpenLicenseActivationPage ();

                    break;


                // =================================================
                // SERVER_ERROR
                // =================================================

                case CaupoLicenseManager.CodeServerError:

                    ShowLicenseMessage (
                        owner,
                        message);


                    OpenLicenseActivationPage ();

                    break;


                // =================================================
                // VALIDATION_ERROR
                // =================================================

                case CaupoLicenseManager.CodeValidationError:

                    ShowLicenseMessage (
                        owner,
                        message);


                    OpenLicenseActivationPage ();

                    break;


                // =================================================
                // UNKNOWN
                // =================================================

                default:

                    Debug.WriteLine (
                        $"[LICENSE] Nepoznat license code: {code}");


                    ShowLicenseMessage (
                        owner,
                        message);


                    OpenLicenseActivationPage ();

                    break;
            }
        }


        // =====================================================
        // OPEN LICENSE ACTIVATION PAGE
        // =====================================================

        private void OpenLicenseActivationPage()
        {
            Debug.WriteLine (
                "[LICENSE] Otvaram LicenseActivationPage.");


            CurrentPage =
                new LicenseActivationPage ();
        }


        // =====================================================
        // OPEN LICENSE PAYMENT PAGE
        // =====================================================

        private void OpenLicensePaymentPage()
        {
            Debug.WriteLine (
                "[LICENSE] Otvaram LicensePaymentPage.");


            CurrentPage =
                new LicensePaymentPage ();
        }


        // =====================================================
        // ACTIVATION LIMIT MESSAGE
        // =====================================================

        private static string BuildActivationLimitMessage(
            CaupoLicenseManager.ActivationResponse result)
        {
            string message =
                result.Message
                ?? "Dosegnut je maksimalan broj aktiviranih računara.";


            if(result.ActiveDevices.HasValue &&
               result.MaxDevices.HasValue)
            {
                return
                    message
                    + Environment.NewLine
                    + Environment.NewLine
                    + "Aktivirani računari: "
                    + result.ActiveDevices.Value
                    + Environment.NewLine
                    + "Maksimalno dozvoljeno: "
                    + result.MaxDevices.Value;
            }


            return message;
        }


        // =====================================================
        // LICENSE MESSAGE
        // =====================================================

        private static void ShowLicenseMessage(
            Window? owner,
            string message)
        {
            var myMessageBox =
                new MyMessageBox ();


            if(owner != null)
            {
                myMessageBox.Owner =
                    owner;


                myMessageBox.WindowStartupLocation =
                    WindowStartupLocation.CenterOwner;
            }
            else
            {
                myMessageBox.WindowStartupLocation =
                    WindowStartupLocation.CenterScreen;
            }


            myMessageBox.MessageTitle.Text =
                "Obavještenje";


            myMessageBox.MessageText.Text =
                message;


            myMessageBox.ShowDialog ();
        }


        // =====================================================
        // PROPERTY CHANGED
        // =====================================================

        public event PropertyChangedEventHandler?
            PropertyChanged;


        protected void OnPropertyChanged(
            [CallerMemberName]
            string? name = null)
        {
            PropertyChanged?.Invoke (
                this,
                new PropertyChangedEventArgs (
                    name));
        }
    }
}

