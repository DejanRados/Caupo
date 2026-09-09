using Caupo.Fiscal.Common;
using Caupo.Helpers;
using Caupo.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Views
{
    public partial class ReceiptsPage : UserControl
    {
        public ReceiptsPage()
        {
            DataContext = new ReceiptsViewModel();

            InitializeComponent();

            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik.Radnik;
        }

        // ============================================================
        // ZATVARANJE
        // ============================================================

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var page = new HomePage
            {
                DataContext = new HomeViewModel()
            };

            PageNavigator.NavigateWithFade(page);
        }

        // ============================================================
        // SELECTION
        // ============================================================

        private async void ListaRacuna_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not DataGrid dataGrid)
                return;

            if (dataGrid.SelectedItem is not TblRacuni selectedItem)
                return;

            if (DataContext is not ReceiptsViewModel viewModel)
                return;

            viewModel.SelectedReceipt = selectedItem;

            await viewModel.LoadReceiptItems(selectedItem);

            SearchTextBox.Text = "";

            dataGrid.ScrollIntoView(dataGrid.SelectedItem);
        }

        // ============================================================
        // PRETHODNI RAČUN
        // ============================================================

        private async void BtnFirst_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ReceiptsViewModel viewModel)
                return;

            if (viewModel.SelectedReceipt == null)
                return;

            int index = viewModel.Receipts.IndexOf(viewModel.SelectedReceipt);

            if (index <= 0)
                return;

            viewModel.SelectedReceipt = viewModel.Receipts[index - 1];

            await viewModel.LoadReceiptItems(viewModel.SelectedReceipt);
        }

        // ============================================================
        // SLJEDEĆI RAČUN
        // ============================================================

        private async void BtnLast_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ReceiptsViewModel viewModel)
                return;

            if (viewModel.SelectedReceipt == null)
                return;

            int index = viewModel.Receipts.IndexOf(viewModel.SelectedReceipt);

            if (index < 0 || index >= viewModel.Receipts.Count - 1)
                return;

            viewModel.SelectedReceipt = viewModel.Receipts[index + 1];

            await viewModel.LoadReceiptItems(viewModel.SelectedReceipt);
        }

        // ============================================================
        // PNOVNI PRINT
        // ==
        private async void BtnReprint_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ReceiptsViewModel viewModel)
                return;

            try
            {
                FiscalResult result = await viewModel.PonovoStampajAsync();

                Debug.WriteLine($"[REPRINT] Success={result.Success}, Fiscalized={result.Fiscalized}, Saved={result.SavedToDatabase}, Printed={result.Printed}");

                if (!result.Success)
                {
                    ShowMessage("GREŠKA", result.ErrorMessage ?? "Ponovna štampa računa nije uspjela.");
                    return;
                }

                if (!result.Printed)
                    ShowMessage("UPOZORENJE", "Račun nije isprintan.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[REPRINT] Greška: " + ex);
                ShowMessage("GREŠKA", ex.Message);
            }
        }

        // ============================================================
        // KOPIJA RAČUNA
        // ============================================================

        private async void BtnDuplicate_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ReceiptsViewModel viewModel)
                return;

            try
            {
                FiscalResult result = await viewModel.IzdajKopijuAsync();

                Debug.WriteLine($"[COPY] Success={result.Success}, Fiscalized={result.Fiscalized}, Saved={result.SavedToDatabase}, Printed={result.Printed}");

                if (!result.Success)
                {
                    ShowMessage("GREŠKA", result.ErrorMessage ?? "Kopija računa nije izdata.");
                    return;
                }

                string? warning = BuildFiscalWarning(result);

                if (!string.IsNullOrWhiteSpace(warning))
                    ShowMessage("UPOZORENJE", warning);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[COPY] Greška: " + ex);
                ShowMessage("GREŠKA", ex.Message);
            }
        }

        // ============================================================
        // STORNO / REFUND
        // ============================================================

        private async void BtnStorno_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ReceiptsViewModel viewModel)
                return;

            if (viewModel.SelectedReceipt == null)
            {
                ShowMessage("GREŠKA", "Nije odabran račun.");
                return;
            }

            TblRacuni selectedItem = viewModel.SelectedReceipt;

            try
            {
                FiscalResult result = await viewModel.StornirajRacunAsync();

                Debug.WriteLine($"[REFUND] Success={result.Success}, Fiscalized={result.Fiscalized}, Saved={result.SavedToDatabase}, Printed={result.Printed}");

                if (!result.Success)
                {
                    ShowMessage("GREŠKA", result.ErrorMessage ?? "Storno računa nije uspio.");
                    return;
                }

                string? warning = BuildFiscalWarning(result);

                if (!string.IsNullOrWhiteSpace(warning))
                    ShowMessage("UPOZORENJE", warning);

                await viewModel.LoadReceiptsAsync(selectedItem);

                if (viewModel.SelectedReceipt != null)
                    ListaRacuna.ScrollIntoView(viewModel.SelectedReceipt);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[REFUND] Greška: " + ex);
                ShowMessage("GREŠKA", ex.Message);
            }
        }

        // ============================================================
        // FISKALIZUJ NAKNADNO
        // ============================================================
        private async void BtnFiscalizeNow_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ReceiptsViewModel viewModel)
                return;

            if (viewModel.SelectedReceipt == null)
            {
                ShowMessage("GREŠKA", "Nije odabran račun.");
                return;
            }

            TblRacuni selectedItem = viewModel.SelectedReceipt;

            try
            {
                BtnFiscalizeNow.IsEnabled = false;

                FiscalResult result = await viewModel.NaknadnoFiskalizujAsync();

                Debug.WriteLine($"[HR MANUAL FISCALIZATION] Success={result.Success}, Fiscalized={result.Fiscalized}, Saved={result.SavedToDatabase}, FiscalNumber={result.FiscalNumber}");

                if (!result.Success || !result.Fiscalized)
                {
                    ShowMessage(
                        "NAKNADNA FISKALIZACIJA",
                        result.ErrorMessage ??
                        "Račun trenutno nije fiskalizovan.\n\nCaupo će automatski nastaviti pokušavati.");

                    return;
                }

                await viewModel.LoadReceiptsAsync(selectedItem);

                if (viewModel.SelectedReceipt != null)
                    ListaRacuna.ScrollIntoView(viewModel.SelectedReceipt);

                ShowMessage(
                    "NAKNADNA FISKALIZACIJA",
                    $"Račun je uspješno fiskalizovan.\n\nJIR:\n{result.FiscalNumber}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[HR MANUAL FISCALIZATION] Greška: " + ex);
                ShowMessage("GREŠKA", ex.Message);
            }
            finally
            {
                BtnFiscalizeNow.IsEnabled = viewModel.CanFiscalizeLater;
            }
        }

        // ============================================================
        // Prekini naknadnu fiskalizaciju (Mark Impossible)
        // ============================================================

        private async void BtnMarkImpossible_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ReceiptsViewModel viewModel)
                return;

            if (!viewModel.CanMarkImpossible)
            {
                ShowMessage("Fiskalizacija računa", "Odabrani račun nije moguće ukloniti iz naknadne fiskalizacije.");
                return;
            }

            var popup = new CroatiaImpossiblePopup
            {
                Owner = Window.GetWindow(this)
            };

            bool? result = popup.ShowDialog();

            if (result != true || popup.ConfirmedWorker == null)
                return;

            var selectedReceipt = viewModel.SelectedReceipt;
            FiscalResult fiscalResult = await viewModel.MarkImpossibleAsync(popup.ConfirmedWorker);

            if (!fiscalResult.Success)
            {
                ShowMessage("Fiskalizacija računa", fiscalResult.ErrorMessage ?? "Promjena statusa računa nije uspjela.");
                return;
            }

            ShowMessage(
                "Fiskalizacija računa",
                "Dalji pokušaji fiskalizacije ovog računa su prekinuti.");

           

           

            await viewModel.LoadReceiptsAsync(selectedReceipt);
        }

        // ============================================================
        // MESSAGE BOX
        // ============================================================

        private static void ShowMessage(string title, string message)
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
        // FISCAL RESULT UPOZORENJA
        // ============================================================

        private static string? BuildFiscalWarning(FiscalResult result)
        {
            var warnings = new List<string>();

            if (!result.Fiscalized)
                warnings.Add("Fiskalizacija nije potvrđena.");

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