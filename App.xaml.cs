using Caupo.Helpers;
using Caupo.Models;
using Caupo.Properties;
using Caupo.Services;
using Caupo.ViewModels;
using Syncfusion.SfSkinManager;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace Caupo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // =====================================================
        // GLOBALNE VARIJABLE
        // =====================================================

        public static KitchenDisplayViewModel GlobalKitchenVM { get; set; }
            = null!;


        public static string CurrentTheme { get; set; }
         = Settings.Default.Tema;


        // =====================================================
        // SERVICES
        // =====================================================

        //private DatabaseBackupService? _backupService;

        private CaupoDiscoveryService? _discoveryService;


        // =====================================================
        // CONSTANTS
        // =====================================================

        private const string ElevationStateFile =
            "elevation_state.json";


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public App()
        {
            DispatcherUnhandledException +=
                App_DispatcherUnhandledException;


            AppDomain.CurrentDomain.UnhandledException +=
                CurrentDomain_UnhandledException;


            TaskScheduler.UnobservedTaskException +=
                TaskScheduler_UnobservedTaskException;


            Syncfusion.Licensing
                .SyncfusionLicenseProvider
                .RegisterLicense (
                    "Ngo9BigBOggjHTQxAR8 / V1NNaF5cXmBCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXtcc3VWRWlYV0d3X0tWYUA =");
        }


        // =====================================================
        // STARTUP
        // =====================================================

        protected override async void OnStartup(
            StartupEventArgs e)
        {
            // =================================================
            // 1. TEMA PRIJE OTVARANJA WINDOWA
            // =================================================

            InitializeTheme ();


            // =================================================
            // 2. KULTURA
            // =================================================

            InitializeCulture ();


            // =================================================
            // 3. DATA FOLDER + BAZA
            // =================================================

            try
            {
                DataFolderService.Initialize ();


                CashRegisterConfigurationService
                    .PrepareStartupDatabasePath ();


                Debug.WriteLine (
                    $"[DATA] Tip kase = " +
                    $"{CashRegisterConfigurationService.CashRegisterType}");


                Debug.WriteLine (
                    $"[DATA] DbPath = " +
                    $"{Settings.Default.DbPath}");


                Debug.WriteLine (
                    $"[DATA] Globals.CurrentDbPath = " +
                    $"{Globals.CurrentDbPath}");


                Debug.WriteLine (
                    $"[DATA] Database = " +
                    $"{DataFolderService.CurrentDatabasePath}");


                /*
                 * Globalni KitchenDisplayViewModel kreiramo tek
                 * nakon što je određen tip kase i ispravan DbPath.
                 */

                GlobalKitchenVM =
                    new KitchenDisplayViewModel ();


                Debug.WriteLine (
                    "[APP] GlobalKitchenVM inicijalizovan " +
                    "nakon DB konfiguracije.");
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[DATA] Greška pri inicijalizaciji: {ex}");


                MessageBox.Show (
                    "Nije moguće pripremiti bazu podataka."
                    + Environment.NewLine
                    + Environment.NewLine
                    + ex.Message,
                    "Caupo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);


                Shutdown ();

                return;
            }


            // =================================================
            // 4. WPF STARTUP
            // =================================================

            /*
             * StartupUri ostaje u App.xaml.
             *
             * Tek sada dopuštamo WPF-u da kreira MainWindow.
             */

            base.OnStartup (e);


            // =================================================
            // 5. PROVJERA ADMIN / ELEVATION STATE
            // =================================================

            string tempPath =
                Path.Combine (
                    Path.GetTempPath (),
                    ElevationStateFile);


            /*
             * Ako elevation_state.json postoji i Caupo
             * je pokrenut kao administrator, ova instanca
             * služi samo za kreiranje mrežnog share-a.
             */

            if(File.Exists (tempPath) &&
               AdminHelper.IsAdministrator ())
            {
                try
                {
                    string json =
                        await File.ReadAllTextAsync (
                            tempPath);


                    ElevationState? state =
                        JsonSerializer
                            .Deserialize<ElevationState> (
                                json);


                    if(state == null)
                    {
                        throw new InvalidOperationException (
                            "Elevation state nije moguće učitati.");
                    }


                    if(string.IsNullOrWhiteSpace (
                        state.FolderPath))
                    {
                        throw new InvalidOperationException (
                            "Folder za mrežni share nije definisan.");
                    }


                    if(string.IsNullOrWhiteSpace (
                        state.ShareName))
                    {
                        throw new InvalidOperationException (
                            "Naziv mrežnog share-a nije definisan.");
                    }


                    Debug.WriteLine (
                        "[SHARE] Admin mode.");


                    Debug.WriteLine (
                        $"[SHARE] Folder: {state.FolderPath}");


                    Debug.WriteLine (
                        $"[SHARE] Share name: {state.ShareName}");


                    var service =
                        new ShareService ();


                    await service
                        .CreateAndShareFolderAsync (
                            state.FolderPath,
                            state.ShareName);


                    Debug.WriteLine (
                        "[SHARE] Share kreiran.");


                    try
                    {
                        File.Delete (
                            tempPath);
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine (
                            "[SHARE] Ne mogu obrisati " +
                            $"elevation state: {ex.Message}");
                    }


                    /*
                     * Administratorski posao je završen.
                     * Vraćamo Caupo u normalni user mode.
                     */

                    Debug.WriteLine (
                        "[SHARE] Restart aplikacije " +
                        "u normalnom modu.");


                    AdminHelper.RestartAsUser ();

                    return;
                }
                catch(Exception ex)
                {
                    Debug.WriteLine (
                        $"[SHARE] Greška tokom elevation procesa: {ex}");


                    try
                    {
                        if(File.Exists (tempPath))
                        {
                            File.Delete (
                                tempPath);
                        }
                    }
                    catch
                    {
                    }


                    MessageBox.Show (
                        "Nije moguće pripremiti mrežni folder."
                        + Environment.NewLine
                        + Environment.NewLine
                        + ex.Message,
                        "Caupo",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);


                    Shutdown ();

                    return;
                }
            }


            // =================================================
            // 6. GLAVNA KASA - NETWORK SHARE
            // =================================================

            if(EnsureMainCashRegisterShare ())
            {
                /*
                 * Pokrenuta je nova administratorska
                 * instanca ili je startup prekinut.
                 */

                return;
            }


            // =================================================
            // 7. PRIMJENA TEME NA KREIRANE WINDOWE
            // =================================================

            ApplyTheme (
                CurrentTheme);


            // =================================================
            // 8. GLOBALNI WINDOW HANDLER
            // =================================================

            EventManager.RegisterClassHandler (
                typeof (Window),
                Window.LoadedEvent,
                new RoutedEventHandler (
                    OnWindowLoaded));


            // =================================================
            // 9. NAVIGACIJA
            // =================================================

            InitializeNavigation ();


            // =================================================
            // 10. BACKUP
            // =================================================

            InitializeBackup ();
        }


        // =====================================================
        // GLAVNA KASA - NETWORK SHARE
        // =====================================================

        private bool EnsureMainCashRegisterShare()
        {
            string currentDbPath =
                Settings.Default.DbPath?.Trim ()
                ?? string.Empty;


            /*
             * Tip kase određuje Settings.Default.CashRegisterType.
             *
             * Main      -> glavna kasa
             * Secondary -> dodatna kasa
             */

            if(!CashRegisterConfigurationService
                .IsMainCashRegister)
            {
                Debug.WriteLine (
                    "[SHARE] Dodatna kasa - " +
                    "share se ne provjerava.");


                Debug.WriteLine (
                    $"[SHARE] DbPath: {currentDbPath}");


                return false;
            }


            const string shareName =
                "DsoftData";


            Debug.WriteLine (
                "[SHARE] Tip kase: Glavna kasa.");


            Debug.WriteLine (
                $"[SHARE] Provjeravam share: {shareName}");


            // =================================================
            // SHARE VEĆ POSTOJI
            // =================================================

            if(IsWindowsShareAvailable (
                shareName))
            {
                string networkPath =
                    $@"\\{Environment.MachineName}\{shareName}";


                Debug.WriteLine (
                    $"[SHARE] Share već postoji: {shareName}");


                Debug.WriteLine (
                    $"[SHARE] Mrežna putanja: {networkPath}");


                StartDiscoveryService ();


                return false;
            }


            // =================================================
            // SHARE NE POSTOJI
            // =================================================

            Debug.WriteLine (
                $"[SHARE] Share {shareName} ne postoji.");


            if(AdminHelper.IsAdministrator ())
            {
                Debug.WriteLine (
                    "[SHARE] Caupo je već pokrenut " +
                    "kao administrator.");


                return false;
            }


            // =================================================
            // RESTART KAO ADMINISTRATOR
            // =================================================

            try
            {
                RestartAsAdministratorForShare (
                    DataFolderService.LocalDataFolder,
                    shareName);


                /*
                 * Nova admin instanca je pokrenuta.
                 * Gasimo trenutnu normalnu instancu.
                 */

                Shutdown ();


                return true;
            }
            catch(Win32Exception ex)
                when(ex.NativeErrorCode == 1223)
            {
                /*
                 * Windows error 1223:
                 * korisnik je odbio / otkazao UAC.
                 */

                Debug.WriteLine (
                    "[SHARE] Korisnik je otkazao UAC.");


                MessageBox.Show (
                    "Za pripremu glavne kase potrebno je jednom "
                    + "dozvoliti administratorska prava."
                    + Environment.NewLine
                    + Environment.NewLine
                    + "Ponovo pokrenite Caupo i kliknite DA kada "
                    + "Windows zatraži dozvolu.",
                    "Caupo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);


                Shutdown ();


                return true;
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[SHARE] Greška kod elevation procesa: {ex}");


                MessageBox.Show (
                    "Nije moguće pripremiti mrežnu bazu."
                    + Environment.NewLine
                    + Environment.NewLine
                    + ex.Message,
                    "Caupo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);


                Shutdown ();


                return true;
            }
        }


        // =====================================================
        // CAUPO LAN DISCOVERY
        // =====================================================

        private void StartDiscoveryService()
        {
            /*
             * Listener se pokreće samo na glavnoj kasi.
             */

            if(_discoveryService != null)
            {
                Debug.WriteLine (
                    "[DISCOVERY] Servis već radi.");


                return;
            }


            try
            {
                _discoveryService =
                    new CaupoDiscoveryService ();


                _discoveryService
                    .StartMainCashRegisterListener ();


                Debug.WriteLine (
                    "[DISCOVERY] Caupo glavna kasa " +
                    "dostupna za pronalaženje.");
            }
            catch(Exception ex)
            {
                /*
                 * Discovery greška ne smije spriječiti
                 * pokretanje glavne kase.
                 */

                Debug.WriteLine (
                    $"[DISCOVERY] Greška pri pokretanju servisa: {ex}");


                try
                {
                    _discoveryService?.Dispose ();
                }
                catch
                {
                }


                _discoveryService =
                    null;
            }
        }


        // =====================================================
        // PROVJERA POSTOJI LI WINDOWS SHARE
        // =====================================================

        private static bool IsWindowsShareAvailable(
            string shareName)
        {
            try
            {
                var startInfo =
                    new ProcessStartInfo
                    {
                        FileName =
                            "net.exe",

                        Arguments =
                            "share",

                        UseShellExecute =
                            false,

                        RedirectStandardOutput =
                            true,

                        RedirectStandardError =
                            true,

                        CreateNoWindow =
                            true,

                        WindowStyle =
                            ProcessWindowStyle.Hidden
                    };


                using Process? process =
                    Process.Start (
                        startInfo);


                if(process == null)
                {
                    Debug.WriteLine (
                        "[SHARE] Ne mogu pokrenuti net.exe.");


                    return false;
                }


                string output =
                    process.StandardOutput
                        .ReadToEnd ();


                string error =
                    process.StandardError
                        .ReadToEnd ();


                process.WaitForExit ();


                Debug.WriteLine (
                    $"[SHARE] net share ExitCode: {process.ExitCode}");


                if(!string.IsNullOrWhiteSpace (
                    error))
                {
                    Debug.WriteLine (
                        $"[SHARE] net share error: {error}");
                }


                bool exists =
                    output.Contains (
                        shareName,
                        StringComparison.OrdinalIgnoreCase);


                Debug.WriteLine (
                    $"[SHARE] Provjera share-a {shareName}: "
                    + (exists
                        ? "POSTOJI"
                        : "NE POSTOJI"));


                return exists;
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[SHARE] Greška kod provjere share-a: {ex}");


                return false;
            }
        }


        // =====================================================
        // RESTART KAO ADMINISTRATOR ZA SHARE
        // =====================================================

        private static void RestartAsAdministratorForShare(
            string folderPath,
            string shareName)
        {
            Debug.WriteLine (
                "[SHARE] Pripremam restart kao administrator...");


            var state =
                new ElevationState
                {
                    FolderPath =
                        folderPath,

                    ShareName =
                        shareName
                };


            string tempPath =
                Path.Combine (
                    Path.GetTempPath (),
                    ElevationStateFile);


            string json =
                JsonSerializer.Serialize (
                    state);


            File.WriteAllText (
                tempPath,
                json);


            Debug.WriteLine (
                $"[SHARE] Elevation state: {tempPath}");


            string? executable =
                Environment.ProcessPath;


            if(string.IsNullOrWhiteSpace (
                executable))
            {
                throw new InvalidOperationException (
                    "Nije moguće pronaći Caupo.exe.");
            }


            var startInfo =
                new ProcessStartInfo
                {
                    FileName =
                        executable,

                    UseShellExecute =
                        true,

                    Verb =
                        "runas",

                    WorkingDirectory =
                        AppContext.BaseDirectory
                };


            Debug.WriteLine (
                "[SHARE] Pokrećem Caupo kao administrator...");


            Process.Start (
                startInfo);
        }


        // =====================================================
        // TEMA
        // =====================================================

        private static void InitializeTheme()
        {
            string tema =
                Settings.Default.Tema?.Trim ()
                ?? string.Empty;


            /*
             * Ako user.config ne postoji ili tema
             * još nije postavljena, koristimo tamnu temu.
             */

            if(string.IsNullOrWhiteSpace (
                tema))
            {
                tema =
                    "Tamna";


                Settings.Default.Tema =
                    tema;


                Settings.Default.Save ();


                Debug.WriteLine (
                    "[THEME] Tema nije bila postavljena. " +
                    "Default = Tamna");
            }


            CurrentTheme =
                tema;


            SfSkinManager.ApplyThemeAsDefaultStyle =
                true;


            SfSkinManager.ApplicationTheme =
                string.Equals (
                    CurrentTheme,
                    "Tamna",
                    StringComparison.OrdinalIgnoreCase)

                    ? new Theme (
                        "Office2019Black")

                    : new Theme (
                        "Office2019Colorful");


            Debug.WriteLine (
                $"[THEME] InitializeTheme = {CurrentTheme}");
        }


        // =====================================================
        // APPLY THEME
        // =====================================================

        public static void ApplyTheme(
            string tema)
        {
            if(string.IsNullOrWhiteSpace (
                tema))
            {
                tema =
                    "Tamna";


                Settings.Default.Tema =
                    tema;


                Settings.Default.Save ();
            }


            CurrentTheme =
                tema;


            bool darkTheme =
                string.Equals (
                    tema,
                    "Tamna",
                    StringComparison.OrdinalIgnoreCase);


            Theme syncfusionTheme =
                darkTheme

                    ? new Theme (
                        "Office2019Black")

                    : new Theme (
                        "Office2019Colorful");


            /*
             * ApplicationTheme je važan i za windowe
             * koji će biti kreirani nakon promjene teme.
             */

            SfSkinManager.ApplicationTheme =
                syncfusionTheme;


            foreach(Window window
                in Current.Windows)
            {
                SfSkinManager.SetTheme (
                    window,
                    syncfusionTheme);
            }


            Debug.WriteLine (
                $"[THEME] ApplyTheme = {tema}");
        }


        // =====================================================
        // KULTURA
        // =====================================================

        private static void InitializeCulture()
        {
            var culture =
                (CultureInfo)
                CultureInfo.InvariantCulture.Clone ();


            culture.NumberFormat
                .NumberDecimalSeparator =
                ".";


            culture.NumberFormat
                .CurrencyDecimalSeparator =
                ".";


            Thread.CurrentThread.CurrentCulture =
                culture;


            Thread.CurrentThread.CurrentUICulture =
                culture;


            FrameworkElement.LanguageProperty
                .OverrideMetadata (
                    typeof (FrameworkElement),
                    new FrameworkPropertyMetadata (
                        System.Windows.Markup
                            .XmlLanguage
                            .GetLanguage (
                                culture.IetfLanguageTag)));
        }


        // =====================================================
        // NAVIGACIJA
        // =====================================================

        private static void InitializeNavigation()
        {
            /*
             * Ovo je jedino mjesto u aplikaciji koje
             * postavlja PageNavigator.Navigate.
             *
             * MainWindow i MainViewModel ga više ne mijenjaju.
             */

            PageNavigator.Navigate =
                page =>
                {
                    if(Application.Current.MainWindow?
                           .DataContext
                       is not MainViewModel viewModel)
                    {
                        Debug.WriteLine (
                            "[NAVIGATION] MainViewModel nije dostupan.");

                        return;
                    }


                    viewModel.CurrentPage =
                        page;
                };


            Debug.WriteLine (
                "[NAVIGATION] PageNavigator inicijalizovan.");
        }


        // =====================================================
        // BACKUP
        // =====================================================

        private void InitializeBackup()
        {
            try
            {
                /*
                 * Backup radi samo na glavnoj kasi.
                 */

                if(!CashRegisterConfigurationService
                    .IsMainCashRegister)
                {
                    Debug.WriteLine (
                        "[BACKUP] Dodatna kasa - " +
                        "backup se ne pokreće.");


                    Debug.WriteLine (
                        $"[BACKUP] Mrežna baza: " +
                        $"{Settings.Default.DbPath}");


                    return;
                }


                string dbPath =
                    DataFolderService
                        .CurrentDatabasePath;


                string backupPath =
                    Settings.Default.BackupUrl;


                Debug.WriteLine (
                    $"[BACKUP] Baza: {dbPath}");


                Debug.WriteLine (
                    $"[BACKUP] Backup folder: {backupPath}");


                if(string.IsNullOrWhiteSpace (
                    backupPath))
                {
                    Debug.WriteLine (
                        "[BACKUP] BackupUrl nije postavljen.");


                    return;
                }


            //    _backupService =
            //       new DatabaseBackupService (
            //            dbPath,
            //           backupPath);


             //   _backupService.Start ();


                Debug.WriteLine (
                    "[BACKUP] Backup servis pokrenut.");
            }
            catch(Exception ex)
            {
                /*
                 * Problem sa backupom ne smije
                 * spriječiti pokretanje POS-a.
                 */

                Debug.WriteLine (
                    $"[BACKUP] Greška: {ex}");
            }
        }


        // =====================================================
        // WINDOW LOADED
        // =====================================================

        private void OnWindowLoaded(
            object sender,
            RoutedEventArgs e)
        {
            /*
             * Rezervisano za globalnu obradu
             * novokreiranih prozora.
             */
        }


        // =====================================================
        // GLOBALNI EXCEPTION HANDLERI
        // =====================================================

        private void App_DispatcherUnhandledException(
            object sender,
            System.Windows.Threading
                .DispatcherUnhandledExceptionEventArgs e)
        {
            Debug.WriteLine (
                "[UI Thread Exception]"
                + Environment.NewLine
                + e.Exception);


            WriteCrashLog (
                "UI Thread Exception",
                e.Exception);


            MessageBox.Show (
                "Došlo je do greške u aplikaciji."
                + Environment.NewLine
                + Environment.NewLine
                + e.Exception.Message,
                "Caupo",
                MessageBoxButton.OK,
                MessageBoxImage.Error);


            /*
             * Zadržavamo postojeće ponašanje.
             */

            e.Handled =
                true;
        }


        private void CurrentDomain_UnhandledException(
            object sender,
            UnhandledExceptionEventArgs e)
        {
            if(e.ExceptionObject
               is not Exception ex)
            {
                return;
            }


            Debug.WriteLine (
                "[Non-UI Exception]"
                + Environment.NewLine
                + ex);


            WriteCrashLog (
                "Non-UI Exception",
                ex);
        }


        private void TaskScheduler_UnobservedTaskException(
            object? sender,
            UnobservedTaskExceptionEventArgs e)
        {
            Debug.WriteLine (
                "[Async Task Exception]"
                + Environment.NewLine
                + e.Exception);


            WriteCrashLog (
                "Async Task Exception",
                e.Exception);


            e.SetObserved ();
        }


        // =====================================================
        // CRASH LOG
        // =====================================================

        private static void WriteCrashLog(
            string source,
            Exception exception)
        {
            try
            {
                string logFolder =
                    Path.Combine (
                        Environment.GetFolderPath (
                            Environment.SpecialFolder
                                .LocalApplicationData),
                        "Dsoft",
                        "Caupo",
                        "Logs");


                Directory.CreateDirectory (
                    logFolder);


                string logFile =
                    Path.Combine (
                        logFolder,
                        "crash.log");


                string log =
                    Environment.NewLine
                    + "========================================"
                    + Environment.NewLine
                    + DateTime.Now.ToString (
                        "yyyy-MM-dd HH:mm:ss")
                    + Environment.NewLine
                    + source
                    + Environment.NewLine
                    + exception
                    + Environment.NewLine;


                File.AppendAllText (
                    logFile,
                    log);
            }
            catch
            {
                /*
                 * Logger nikad ne smije izazvati
                 * novu grešku.
                 */
            }
        }


        // =====================================================
        // EXIT
        // =====================================================

        protected override void OnExit(
            ExitEventArgs e)
        {
            // =================================================
            // DISCOVERY
            // =================================================

            try
            {
                if(_discoveryService != null)
                {
                    Debug.WriteLine (
                        "[DISCOVERY] Gasim discovery servis.");


                    _discoveryService.Stop ();


                    _discoveryService.Dispose ();


                    _discoveryService =
                        null;
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[DISCOVERY] Dispose error: {ex}");
            }


            // =================================================
            // BACKUP
            // =================================================

            try
            {
               // _backupService?.Dispose ();


              //  _backupService = null;
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[BACKUP] Dispose error: {ex}");
            }


            base.OnExit (
                e);
        }
    }
}

