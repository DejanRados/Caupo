using Caupo.Data;
using Caupo.Fiscal.Common;
using Caupo.Fiscal.RS.Models;
using Caupo.Helpers;
using Caupo.Models;
using Caupo.ViewModels;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for OrderPage.xaml
    /// </summary>
    public partial class OrderPage : UserControl
    {
        private readonly OrderViewModel orderViewModel;
        

        private readonly bool hasNewItems;

        public OrderPage(int? idStola, string? imeStola, string? sala, ObservableCollection<RacunStavka> stavkeRacuna, bool hasNewItems)
        {
            InitializeComponent();

            this.hasNewItems = hasNewItems;

            orderViewModel = new OrderViewModel(idStola, imeStola, sala, stavkeRacuna);
            DataContext = orderViewModel;

            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik.Radnik;

            Loaded += OrderPage_Loaded;
        }

        private async void OrderPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OrderPage_Loaded;

            try
            {
                Exception? printException = await orderViewModel.InitializeAsync();

                if (printException != null)
                {
                    ShowMessage(
                        "UPOZORENJE",
                        $"Narudžba je spremljena, ali blok nije u potpunosti obrađen:{Environment.NewLine}{printException.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ORDER] Greška pri inicijalizaciji: " + ex);

                ShowMessage(
                    "GREŠKA",
                    $"Nije moguće spremiti ili učitati narudžbu:{Environment.NewLine}{ex.Message}");
            }
        }

        private void ShowMessage(string title, string message)
        {
            var myMessageBox = new MyMessageBox
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            myMessageBox.MessageTitle.Text = title;
            myMessageBox.MessageText.Text = message;
            myMessageBox.ShowDialog();
        }

        // ============================================================
        // PREBACI STAVKU NA GOST RAČUN
        // ============================================================

        private void ListnarudzbeStavke_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGrid listView)
                return;

            var source = e.OriginalSource as DependencyObject;

            while (source != null && source is not DataGridRow)
                source = VisualTreeHelper.GetParent(source);

            if (source is not DataGridRow row)
                return;

            if (row.Item is not DatabaseTables.TblNarudzbeStavke clickedItem)
                return;

            if (DataContext is not OrderViewModel viewModel)
                return;

            listView.SelectedItem = clickedItem;

            BorderRacunStavke.Visibility = Visibility.Visible;
            ViewKolicina.Visibility = Visibility.Visible;
            lblIznosGostRacun.Visibility = Visibility.Visible;
            ListGostRacunStavke.Visibility = Visibility.Visible;

            if (string.IsNullOrWhiteSpace(txtKolicina.Text))
                txtKolicina.Text = "1";

            if (!decimal.TryParse(txtKolicina.Text, out decimal kolicina))
            {
                ShowMessage("GREŠKA", "Molimo unesite validnu količinu.");
                txtKolicina.Focus();
                txtKolicina.SelectAll();
                return;
            }

            if (kolicina <= 0)
            {
                ShowMessage("GREŠKA", "Količina mora biti veća od 0.");
                txtKolicina.Focus();
                txtKolicina.SelectAll();
                return;
            }

            viewModel.PrebaciStavku(clickedItem, kolicina);
        }

        // ============================================================
        // ZATVARANJE
        // ============================================================

        private void CloseButton_Click(object? sender, RoutedEventArgs? e)
        {
            if (hasNewItems)
            {
               
                var page = new KasaPage
                {
                    DataContext = new KasaViewModel()
                };

                PageNavigator.NavigateWithFade(page);
            }
            else
            {
                var page = new OrdersPage();
                PageNavigator.NavigateWithFade(page);
            }
        }

        // ============================================================
        // IZDAVANJE RAČUNA
        // ============================================================

        private async void BtnRacun_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[ORDER] BtnRacun_Click pokrenut");

            if (DataContext is not OrderViewModel viewModel)
            {
                Debug.WriteLine("[ORDER] DataContext nije OrderViewModel.");
                return;
            }

            RacunIdikator.Visibility = Visibility.Visible;

            try
            {
                bool gostRacun = viewModel.GostRacunStavke.Count > 0;

                Debug.WriteLine(gostRacun
                    ? "[ORDER] Izdajem podijeljeni račun."
                    : "[ORDER] Izdajem cijeli račun.");

                FiscalResult result = await viewModel.IzdajRacunAsync(cmbNacinPlacanja.SelectedIndex, gostRacun);

                Debug.WriteLine($"[ORDER] Success={result.Success}, Fiscalized={result.Fiscalized}, Saved={result.SavedToDatabase}, Printed={result.Printed}");

                // ----------------------------------------------------
                // Fiskalizacija / lokalna obrada nije uspjela.
                // Narudžbu NE diramo.
                // ----------------------------------------------------

                if (!result.Success && !result.Fiscalized)
                {
                    ShowMessage("GREŠKA", "Račun nije uspješno fiskalizovan.");
                    return;
                }

                // ----------------------------------------------------
                // VAŽNO:
                //
                // Ako je račun fiskalizovan, ne ostavljamo
                // narudžbu za ponovno slanje jer bi korisnik
                // mogao napraviti dupli fiskalni račun.
                //
                // Croatia može imati:
                //
                // Success = true
                // Fiscalized = false
                // SavedToDatabase = true
                //
                // kod naknadne dostave.
                // ----------------------------------------------------

                if (gostRacun)
                {
                    await viewModel.ZavrsiGostRacunAsync();
                    SakrijGostRacun();
                }
                else
                {
                    await viewModel.ZavrsiCijeliRacunAsync();
                }

                // ----------------------------------------------------
                // UPOZORENJA
                // ----------------------------------------------------

                string? warning = BuildFiscalWarning(result);

                if (!string.IsNullOrWhiteSpace(warning))
                {
                    ShowMessage("UPOZORENJE", warning);
                }

                // ----------------------------------------------------
                // Cijeli račun zatvara sto/page.
                // Kod podijeljenog ostajemo na stolu jer može
                // postojati ostatak narudžbe.
                // ----------------------------------------------------

                if (!gostRacun)
                {
                    Debug.WriteLine("[ORDER] Cijeli račun završen.");
                    CloseButton_Click(null, null);
                }
                else
                {
                    Debug.WriteLine("[ORDER] Podijeljeni račun završen.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ORDER] BtnRacun_Click greška: " + ex);
                ShowMessage("GREŠKA", "Došlo je do greške: " + ex.Message);
            }
            finally
            {
                RacunIdikator.Visibility = Visibility.Collapsed;
            }
        }

        // ============================================================
        // VRATI STAVKU SA GOST RAČUNA
        // ============================================================

        private void ListGostRacunStavke_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGrid listView)
                return;

            var source = e.OriginalSource as DependencyObject;

            while (source != null && source is not DataGridRow)
                source = VisualTreeHelper.GetParent(source);

            if (source is not DataGridRow row)
                return;

            if (row.Item is not DatabaseTables.TblNarudzbeStavke clickedItem)
                return;

            if (DataContext is not OrderViewModel viewModel)
                return;

            listView.SelectedItem = clickedItem;

            if (string.IsNullOrWhiteSpace(txtKolicina.Text))
                txtKolicina.Text = "1";

            if (!decimal.TryParse(txtKolicina.Text, out decimal kolicina))
            {
                ShowMessage("GREŠKA", "Molimo unesite validnu količinu.");
                txtKolicina.Focus();
                txtKolicina.SelectAll();
                return;
            }

            if (kolicina <= 0)
            {
                ShowMessage("GREŠKA", "Količina mora biti veća od 0.");
                txtKolicina.Focus();
                txtKolicina.SelectAll();
                return;
            }

            viewModel.VratiStavku(clickedItem, kolicina);

            if (viewModel.GostRacunStavke.Count == 0)
                SakrijGostRacun();
        }

        // ============================================================
        // UI GOST RAČUNA
        // ============================================================

        private void SakrijGostRacun()
        {
            BorderRacunStavke.Visibility = Visibility.Collapsed;
            ViewKolicina.Visibility = Visibility.Collapsed;
        }

        // ============================================================
        // FISCAL RESULT UPOZORENJA
        // ============================================================

        private static string? BuildFiscalWarning(FiscalResult result)
        {
            var warnings = new List<string>();

            if (!result.Fiscalized)
                warnings.Add("Račun je lokalno obrađen, ali fiskalizacija nije potvrđena.");

            if (!result.SavedToDatabase)
                warnings.Add("Račun nije spremljen u lokalnu bazu.");

            if (!result.Printed)
                warnings.Add("Račun nije isprintan.");

            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                warnings.Add(result.ErrorMessage);

            if (warnings.Count == 0)
                return null;

            return string.Join(Environment.NewLine, warnings.Distinct());
        }
    }
}