using Caupo.Data;
using Caupo.Fiscal.Common;
using Caupo.Helpers;
using Caupo.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for OrderPage.xaml
    /// </summary>
    public partial class OrderPage : UserControl
    {
        private readonly OrderViewModel orderViewModel;
        private readonly OrdersViewModel ordersViewModel;


        public OrderPage(
            OrdersViewModel _ordersViewModel)
        {
            ordersViewModel =
                _ordersViewModel;

            orderViewModel =
                new OrderViewModel (
                    ordersViewModel);

            DataContext =
                orderViewModel;

            InitializeComponent ();

            lblUlogovaniKorisnik.Content =
                Globals
                    .ulogovaniKorisnik
                    .Radnik;
        }


        // ============================================================
        // PREBACI STAVKU NA GOST RAČUN
        // ============================================================

        private async void ListnarudzbeStavke_PreviewMouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            if(sender is not DataGrid listView)
                return;

            if(listView.SelectedItem
               is not DatabaseTables.TblNarudzbeStavke clickedItem)
            {
                return;
            }

            if(DataContext
               is not OrderViewModel viewModel)
            {
                return;
            }


            BorderRacunStavke.Visibility =
                Visibility.Visible;

            ViewKolicina.Visibility =
                Visibility.Visible;

            lblIznosGostRacun.Visibility =
                Visibility.Visible;

            ListGostRacunStavke.Visibility =
                Visibility.Visible;


            if(!decimal.TryParse (
                   txtKolicina.Text,
                   out decimal kolicina))
            {
                MessageBox.Show (
                    "Molimo unesite validnu količinu.");

                return;
            }


            if(kolicina <= 0)
            {
                MessageBox.Show (
                    "Količina mora biti veća od 0.");

                return;
            }


            await viewModel.PrebaciStavku (
                clickedItem,
                kolicina);
        }


        // ============================================================
        // ZATVARANJE
        // ============================================================

        private void CloseButton_Click(
            object? sender,
            RoutedEventArgs? e)
        {
            if(Globals.forma == "Kasa")
            {
                var page =
                    new KasaPage
                    {
                        DataContext =
                            new KasaViewModel ()
                    };

                PageNavigator.NavigateWithFade (
                    page);
            }
            else
            {
                var page =
                    new OrdersPage (null)
                    {
                        DataContext =
                            new OrdersViewModel (null)
                    };

                PageNavigator.NavigateWithFade (
                    page);
            }
        }


        // ============================================================
        // IZDAVANJE RAČUNA
        // ============================================================

        private async void BtnRacun_Click(
            object sender,
            RoutedEventArgs e)
        {
            Debug.WriteLine (
                "[ORDER] BtnRacun_Click pokrenut");


            if(DataContext
               is not OrderViewModel viewModel)
            {
                Debug.WriteLine (
                    "[ORDER] DataContext nije OrderViewModel.");

                return;
            }


            RacunIdikator.Visibility =
                Visibility.Visible;


            try
            {
                bool gostRacun =
                    viewModel.GostRacunStavke.Count > 0;


                Debug.WriteLine (
                    gostRacun
                        ? "[ORDER] Izdajem podijeljeni račun."
                        : "[ORDER] Izdajem cijeli račun.");


                FiscalResult result =
                    await viewModel.IzdajRacunAsync (
                        cmbNacinPlacanja.SelectedIndex,
                        gostRacun);


                Debug.WriteLine (
                    $"[ORDER] " +
                    $"Success={result.Success}, " +
                    $"Fiscalized={result.Fiscalized}, " +
                    $"Saved={result.SavedToDatabase}, " +
                    $"Printed={result.Printed}");


                // ----------------------------------------------------
                // Fiskalizacija / lokalna obrada nije uspjela.
                // Narudžbu NE diramo.
                // ----------------------------------------------------

                if(!result.Success &&
                   !result.Fiscalized)
                {
                    MessageBox.Show (
                        result.ErrorMessage
                        ?? "Račun nije izdat.",
                        "Greška",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

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

                if(gostRacun)
                {
                    await viewModel
                        .ZavrsiGostRacunAsync ();

                    SakrijGostRacun ();
                }
                else
                {
                    await viewModel
                        .ZavrsiCijeliRacunAsync ();
                }


                // ----------------------------------------------------
                // UPOZORENJA
                // ----------------------------------------------------

                string? warning =
                    BuildFiscalWarning (
                        result);

                if(!string.IsNullOrWhiteSpace (
                    warning))
                {
                    MessageBox.Show (
                        warning,
                        "Upozorenje",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }


                // ----------------------------------------------------
                // Cijeli račun zatvara sto/page.
                // Kod podijeljenog ostajemo na stolu jer može
                // postojati ostatak narudžbe.
                // ----------------------------------------------------

                if(!gostRacun)
                {
                    Debug.WriteLine (
                        "[ORDER] Cijeli račun završen.");

                    CloseButton_Click (
                        null,
                        null);
                }
                else
                {
                    Debug.WriteLine (
                        "[ORDER] Podijeljeni račun završen.");
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    "[ORDER] BtnRacun_Click greška: " +
                    ex);

                MessageBox.Show (
                    "Došlo je do greške: " +
                    ex.Message,
                    "Greška",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                RacunIdikator.Visibility =
                    Visibility.Collapsed;
            }
        }


        // ============================================================
        // VRATI STAVKU SA GOST RAČUNA
        // ============================================================

        private async void ListGostRacunStavke_PreviewMouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            if(sender is not DataGrid listView)
                return;


            if(listView.SelectedItem
               is not DatabaseTables.TblNarudzbeStavke clickedItem)
            {
                return;
            }


            if(DataContext
               is not OrderViewModel viewModel)
            {
                return;
            }


            if(!decimal.TryParse (
                   txtKolicina.Text,
                   out decimal kolicina))
            {
                MessageBox.Show (
                    "Molimo unesite validnu količinu.");

                return;
            }


            if(kolicina <= 0)
            {
                MessageBox.Show (
                    "Količina mora biti veća od 0.");

                return;
            }


            await viewModel.VratiStavku (
                clickedItem,
                kolicina);


            if(viewModel.GostRacunStavke.Count == 0)
            {
                SakrijGostRacun ();
            }
        }


        // ============================================================
        // UI GOST RAČUNA
        // ============================================================

        private void SakrijGostRacun()
        {
            BorderRacunStavke.Visibility =
                Visibility.Collapsed;

            ViewKolicina.Visibility =
                Visibility.Collapsed;

            lblKolicina.Visibility =
                Visibility.Collapsed;

            txtKolicina.Visibility =
                Visibility.Collapsed;

            lblIznosGostRacun.Visibility =
                Visibility.Collapsed;

            ListGostRacunStavke.Visibility =
                Visibility.Collapsed;
        }


        // ============================================================
        // FISCAL RESULT UPOZORENJA
        // ============================================================

        private static string? BuildFiscalWarning(
            FiscalResult result)
        {
            var warnings =
                new List<string> ();


            if(!result.Fiscalized)
            {
                warnings.Add (
                    "Račun je lokalno obrađen, ali fiskalizacija nije potvrđena.");
            }


            if(!result.SavedToDatabase)
            {
                warnings.Add (
                    "Račun nije spremljen u lokalnu bazu.");
            }


            if(!result.Printed)
            {
                warnings.Add (
                    "Račun nije isprintan.");
            }


            if(!string.IsNullOrWhiteSpace (
                result.ErrorMessage))
            {
                warnings.Add (
                    result.ErrorMessage);
            }


            if(warnings.Count == 0)
                return null;


            return string.Join (
                Environment.NewLine,
                warnings.Distinct ());
        }
    }
}