using Caupo.Helpers;
using Caupo.Models;
using Caupo.Properties;
using Caupo.Services;
using Caupo.ViewModels;
using Syncfusion.SfSkinManager;

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace Caupo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static KitchenDisplayViewModel GlobalKitchenVM { get; set; } = new KitchenDisplayViewModel ();
        private DatabaseBackupService _backupService;
        public static string CurrentTheme { get; set; } = Settings.Default.Tema;
        public App()
        {

            // Globalni handleri za sve tipove izuzetaka
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8 / V1NNaF5cXmBCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXtcc3VWRWlYV0d3X0tWYUA =");
        }


        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Debug.WriteLine($"🚨 [UI Thread Exception] {e.Exception.Message}\n{e.Exception.StackTrace}");
            e.Handled = true; 
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Debug.WriteLine($"💥 [Non-UI Exception] {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Debug.WriteLine ($"⚠️ [Async Task Exception] {e.Exception.Message}\n{e.Exception.StackTrace}");
            e.SetObserved ();
        } 


       
        protected override void OnExit(ExitEventArgs e)
        {
            _backupService?.Dispose ();

            base.OnExit (e);
        }

        private const string ElevationStateFile = "elevation_state.json";
        protected override async void OnStartup(StartupEventArgs e)
        {
            if(CurrentTheme == "Tamna")
            {
                SfSkinManager.ApplyThemeAsDefaultStyle = true;
                SfSkinManager.ApplicationTheme = new Theme ("Office2019Black");
            }
            else
            {
                SfSkinManager.ApplyThemeAsDefaultStyle = true;
                SfSkinManager.ApplicationTheme = new Theme ("Office2019Colorful");
            }

            base.OnStartup(e);

            string tempPath = Path.Combine (Path.GetTempPath (), ElevationStateFile);

            // Ako postoji state fajl → znači aplikacija je restartovana kao admin
            if(File.Exists (tempPath) && AdminHelper.IsAdministrator ())
            {
                string json = File.ReadAllText (tempPath);
                var state = JsonSerializer.Deserialize<ElevationState> (json);

                var service = new ShareService ();
                await service.CreateAndShareFolderAsync (state.FolderPath, state.ShareName);

                File.Delete (tempPath);

                // Nakon što je share kreiran → vrati se u normalni mod (user)
                AdminHelper.RestartAsUser ();
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                File.WriteAllText ("crash.log", args.ExceptionObject.ToString ());
                MessageBox.Show ($"Greška: {args.ExceptionObject}");
            };

            DispatcherUnhandledException += (sender, args) =>
            {
                File.WriteAllText ("crash.log", args.Exception.ToString ());
                MessageBox.Show ($"Dispatcher greška: {args.Exception}");
                args.Handled = true;
            };

            ApplyTheme (CurrentTheme);
            EventManager.RegisterClassHandler (typeof (Window), Window.LoadedEvent, new RoutedEventHandler (OnWindowLoaded));

            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone ();

            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.NumberFormat.CurrencyDecimalSeparator = ".";

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            FrameworkElement.LanguageProperty.OverrideMetadata (
                typeof (FrameworkElement),
                new FrameworkPropertyMetadata (
                    System.Windows.Markup.XmlLanguage.GetLanguage (culture.IetfLanguageTag)));
            PageNavigator.Navigate = page =>
            {
                if(Application.Current.MainWindow.DataContext is MainViewModel vm)
                    vm.CurrentPage = page;
            };

            string dbPath = Path.Combine (Settings.Default.DbPath, "sysFormWPF.db");
            string backupPath = Settings.Default.BackupUrl;
            Debug.WriteLine ("❌ Backup traži bazu na: " + dbPath);
            _backupService = new DatabaseBackupService (dbPath, backupPath);
            _backupService.Start ();

        }




        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
           /*  if (sender is Window window)
             {
                 // Primijeni trenutnu temu na novi prozor
                 if (CurrentTheme == "Tamna")
                 {
                     SfSkinManager.SetTheme(window, new Theme("Office2019Black"));
                 }
                 else
                 {
                     SfSkinManager.SetTheme(window, new Theme("Office2019Colorful"));
                 }
             }*/
         
        }

        public static void ApplyTheme(string tema)
        {
            if (string.IsNullOrWhiteSpace (tema))
            {
                Settings.Default.Tema = "Tamna";
                Settings.Default.Save ();
                tema = "Tamna";
            }
            CurrentTheme = tema;
            
            // Postavi temu za sve trenutno otvorene prozore
            foreach (Window window in Current.Windows)
            {
                if (tema == "Tamna")
                {
                    SfSkinManager.SetTheme(window, new Theme("Office2019Black"));
                 
                }
                else
                {
                    SfSkinManager.SetTheme(window, new Theme("Office2019Colorful"));
                }
            }
        }

    }

}
