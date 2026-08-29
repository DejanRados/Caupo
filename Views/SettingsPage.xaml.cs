using Caupo.Helpers;
using Caupo.Services;
using Caupo.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : UserControl
    {
        private bool _initializingCashRegisterType;


        public SettingsPage()
        {
            InitializeComponent ();


            DataContext =
                new SettingsViewModel ();


            lblUlogovaniKorisnik.Content =
                Globals.ulogovaniKorisnik?.Radnik
                ?? string.Empty;


            InitializeCashRegisterType ();
        }


        // =====================================================
        // TIP KASE
        // =====================================================

        private void InitializeCashRegisterType()
        {
            _initializingCashRegisterType =
                true;


            string currentType =
                CashRegisterConfigurationService
                    .CashRegisterType;


            foreach(object item in
                cmbCashRegisterType.Items)
            {
                if(item is not ComboBoxItem comboItem)
                {
                    continue;
                }


                string tag =
                    comboItem.Tag?.ToString ()
                    ?? string.Empty;


                if(string.Equals (
                    tag,
                    currentType,
                    StringComparison.OrdinalIgnoreCase))
                {
                    cmbCashRegisterType.SelectedItem =
                        comboItem;

                    break;
                }
            }


            DbPathButton.Visibility =
                CashRegisterConfigurationService
                    .IsSecondaryCashRegister
                    ? Visibility.Visible
                    : Visibility.Collapsed;


            UpdateCashRegisterStatus ();


            _initializingCashRegisterType =
                false;
        }


        private void CashRegisterType_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if(_initializingCashRegisterType)
            {
                return;
            }


            if(cmbCashRegisterType.SelectedItem
                is not ComboBoxItem selectedItem)
            {
                return;
            }


            string selectedType =
                selectedItem.Tag?.ToString ()
                ?? CashRegisterConfigurationService
                    .MainCashRegister;


            // =================================================
            // GLAVNA KASA
            // =================================================

            if(string.Equals (
                selectedType,
                CashRegisterConfigurationService
                    .MainCashRegister,
                StringComparison.OrdinalIgnoreCase))
            {
                bool wasSecondary =
                    CashRegisterConfigurationService
                        .IsSecondaryCashRegister;


                DbPathButton.Visibility =
                    Visibility.Collapsed;


                CashRegisterConfigurationService
                    .SetMainCashRegister ();


                UpdateDbPathInViewModel (
                    DataFolderService
                        .LocalDatabasePath);


                UpdateCashRegisterStatus ();


                Debug.WriteLine (
                    "[SETTINGS] Izabrana glavna kasa.");


                if(wasSecondary)
                {
                    AskForRestart (
                        "Tip kase je promijenjen na Glavnu kasu.");
                }


                return;
            }


            // =================================================
            // DODATNA KASA
            // =================================================

            /*
             * Sam izbor "Dodatna kasa" još NE spremamo.
             *
             * Secondary i mrežni DbPath spremaju se zajedno
             * tek kada discovery pronađe glavnu kasu i kada
             * provjera baze/pisanja uspješno prođe.
             */

            DbPathButton.Visibility =
                Visibility.Visible;


            txtCashRegisterStatus.Text =
                "Dodatna kasa nije još povezana. Kliknite „Pronađi“.";


            Debug.WriteLine (
                "[SETTINGS] Izabrana dodatna kasa - čekam povezivanje.");
        }


        // =====================================================
        // ZATVARANJE SETTINGS STRANICE
        // =====================================================

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            var page =
                new HomePage
                {
                    DataContext =
                        new HomeViewModel ()
                };


            PageNavigator.NavigateWithFade (
                page);
        }


        // =====================================================
        // PRONAĐI I POVEŽI GLAVNU KASU
        // =====================================================

        private async void DbPathButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                DbPathButton.IsEnabled =
                    false;


                txtCashRegisterStatus.Text =
                    "Tražim glavnu kasu na mreži...";


                Debug.WriteLine (
                    "[SETTINGS] Tražim glavnu kasu...");


                MainCashRegisterInfo? main =
               await CashRegisterConfigurationService
                   .FindAndConfigureSecondaryCashRegisterAsync (
                       3000);


                if(main == null)
                {
                    txtCashRegisterStatus.Text =
                        "Glavna kasa nije pronađena na mreži.";


                    MessageBox.Show (
                        "Glavna kasa nije pronađena na mreži.\n\n" +
                        "Provjerite da li je glavna kasa uključena " +
                        "i da li su oba računara spojena na istu mrežu.",
                        "Caupo",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);


                    return;
                }


                UpdateDbPathInViewModel (
                    main.DatabasePath);


                UpdateDbPathInViewModel (
                    main.DatabasePath);


                UpdateCashRegisterStatus ();


                Debug.WriteLine (
                    "[SETTINGS] Dodatna kasa uspješno povezana.");


                Debug.WriteLine (
                    $"[SETTINGS] Glavna kasa: {main.MachineName}");


                Debug.WriteLine (
                    $"[SETTINGS] IP: {main.IpAddress}");


                Debug.WriteLine (
                    $"[SETTINGS] DbPath: {main.DatabasePath}");


                MessageBox.Show (
                    "Dodatna kasa je uspješno povezana.\n\n" +
                    $"Glavna kasa: {main.MachineName}\n" +
                    $"Baza: {main.DatabasePath}",
                    "Caupo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);


                AskForRestart (
                    "Dodatna kasa je uspješno podešena.");
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[SETTINGS] Greška pri povezivanju: {ex}");


                txtCashRegisterStatus.Text =
                    "Glavna kasa nije pronađena ili nije dostupna.";


                MessageBox.Show (
                    ex.Message,
                    "Caupo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                DbPathButton.IsEnabled =
                    true;
            }
        }


        // =====================================================
        // STATUS KASE
        // =====================================================

        private void UpdateCashRegisterStatus()
        {
            if(CashRegisterConfigurationService
                .IsMainCashRegister)
            {
                txtCashRegisterStatus.Text =
                    "Glavna kasa - lokalna baza podataka.";

                return;
            }


            string dbPath =
                Properties.Settings.Default.DbPath?.Trim ()
                ?? string.Empty;


            if(string.IsNullOrWhiteSpace (dbPath))
            {
                txtCashRegisterStatus.Text =
                    "Dodatna kasa nije povezana.";

                return;
            }


            string machineName =
                ExtractMachineNameFromUncPath (
                    dbPath);


            if(string.IsNullOrWhiteSpace (machineName))
            {
                txtCashRegisterStatus.Text =
                    "Dodatna kasa - mrežna baza podataka.";
            }
            else
            {
                txtCashRegisterStatus.Text =
                    $"Dodatna kasa - povezana sa {machineName}.";
            }
        }


        private static string ExtractMachineNameFromUncPath(
            string path)
        {
            if(string.IsNullOrWhiteSpace (path))
            {
                return string.Empty;
            }


            if(!path.StartsWith (
                @"\\",
                StringComparison.Ordinal))
            {
                return string.Empty;
            }


            string trimmed =
                path.TrimStart ('\\');


            int slashIndex =
                trimmed.IndexOf ('\\');


            if(slashIndex <= 0)
            {
                return string.Empty;
            }


            return trimmed.Substring (
                0,
                slashIndex);
        }


        // =====================================================
        // RESTART NAKON PROMJENE TIPA KASE
        // =====================================================

        private void AskForRestart(
            string message)
        {
            MessageBoxResult result =
                MessageBox.Show (
                    message +
                    "\n\n" +
                    "Caupo je potrebno ponovo pokrenuti " +
                    "da bi promjena potpuno stupila na snagu.\n\n" +
                    "Ponovo pokrenuti sada?",
                    "Caupo",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);


            if(result !=
               MessageBoxResult.Yes)
            {
                return;
            }


            RestartCaupo ();
        }


        private static void RestartCaupo()
        {
            try
            {
                string? exePath =
                    Environment.ProcessPath;


                if(string.IsNullOrWhiteSpace (
                    exePath))
                {
                    throw new InvalidOperationException (
                        "Nije moguće pronaći Caupo.exe.");
                }


                var startInfo =
                    new ProcessStartInfo
                    {
                        FileName =
                            exePath,

                        UseShellExecute =
                            true,

                        WorkingDirectory =
                            AppContext.BaseDirectory
                    };


                Process.Start (
                    startInfo);


                Application.Current.Shutdown ();
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[SETTINGS] Restart greška: {ex}");


                MessageBox.Show (
                    "Caupo nije moguće automatski ponovo pokrenuti.\n\n" +
                    ex.Message,
                    "Caupo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        // =====================================================
        // DBPATH -> VIEWMODEL/BINDING
        // =====================================================

        private void UpdateDbPathInViewModel(
            string databasePath)
        {
            if(DataContext is SettingsViewModel vm)
            {
                vm.DbPath =
                    databasePath;
            }


            /*
             * Ako DbPath property još nema
             * OnPropertyChanged(), ručno osvježi binding.
             */

            BindingExpression? binding =
                txtDBPath.GetBindingExpression (
                    TextBox.TextProperty);


            binding?.UpdateTarget ();
        }
    }
}