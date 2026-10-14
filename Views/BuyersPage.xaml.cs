
using Caupo.Data;
using Caupo.Helpers;
using Caupo.Properties;
using Caupo.ViewModels;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Views
{
    public partial class BuyersPage : UserControl
    {
        private readonly BuyersViewModel _viewModel;
        private bool _initialized;

        public BuyersPage()
        {
            InitializeComponent();

            _viewModel = new BuyersViewModel();
            DataContext = _viewModel;

            _viewModel.OpenBuyerPopupRequested += OpenBuyerPopup;
            _viewModel.ConfirmDeleteRequested += ShowDeletePopup;
           
        }

        // ============================================================
        // UČITAVANJE
        // ============================================================

        private async void UserControl_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            if (_initialized)
                return;

            await _viewModel.InitializeAsync();
            _initialized = true;
        }

        // ============================================================
        // PRETRAGA KUPACA
        // ============================================================


        private void BuyerSearchBar_SuggestionSelected(
            object? sender,
            Caupo.UserControls.SearchSuggestionSelectedEventArgs e)
        {
            if (e.Suggestion.Item is not TblKupci buyer)
                return;

            _viewModel.SelectedBuyer = buyer;

            BuyerSearchBar.SearchText = string.Empty;
            _viewModel.SearchText = string.Empty;

            if (ListaKupaca.Items.Contains(buyer))
                ListaKupaca.ScrollIntoView(buyer);
        }


        // ============================================================
        // ODABIR KUPCA IZ DATAGRIDA
        // ============================================================

        private void ListaKupaca_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (ListaKupaca.SelectedItem is TblKupci selectedBuyer)
            {
                _viewModel.SelectedBuyer = selectedBuyer;
                ListaKupaca.ScrollIntoView(selectedBuyer);
            }
        }

        // ============================================================
        // ODABIR RAČUNA
        // ============================================================

        private void ListaRacuna_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (ListaRacuna.SelectedItem is TblRacuni selectedReceipt)
            {
                _viewModel.SelectedReceipt = selectedReceipt;
                ListaRacuna.ScrollIntoView(selectedReceipt);
            }
        }

        // ============================================================
        // POTVRDA BRISANJA KUPCA
        // ============================================================

        private bool? ShowDeletePopup(string buyerName)
        {
            var parentWindow = Window.GetWindow(this);

            if (parentWindow != null)
                parentWindow.Effect = new BlurEffect { Radius = 5 };

            try
            {
                var myMessageBox = new YesNoPopup
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                myMessageBox.MessageTitle.Text = "POTVRDA BRISANJA";
                myMessageBox.MessageText.Text =
                    $"Da li ste sigurni da želite obrisati kupca:\n{buyerName}?";

                myMessageBox.ShowDialog();

                return myMessageBox.Kliknuo == "Da";
            }
            finally
            {
                if (parentWindow != null)
                    parentWindow.Effect = null;
            }
        }

        // ============================================================
        // DODAVANJE / IZMJENA KUPCA
        // ============================================================

        private TblKupci? OpenBuyerPopup(TblKupci? buyer)
        {
            var vm = new BuyerPopupViewModel(buyer);

            var popup = new BuyerPopup
            {
                DataContext = vm,
                Owner = Window.GetWindow(this)
            };

            var parentWindow = Window.GetWindow(this);

            if (parentWindow != null)
                parentWindow.Effect = new BlurEffect { Radius = 5 };

            TblKupci? resultBuyer = null;

            vm.CloseRequested += (s, result) =>
            {
                if (result)
                    resultBuyer = vm.Model;

                popup.Close();
            };

            try
            {
                popup.ShowDialog();
            }
            finally
            {
                if (parentWindow != null)
                    parentWindow.Effect = null;
            }

            return resultBuyer;
        }

        // ============================================================
        // ZATVARANJE STRANICE
        // ============================================================

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            var page = new HomePage
            {
                DataContext = new HomeViewModel()
            };

            PageNavigator.NavigateWithFade(page);
        }

        // ============================================================
        // EXCEL - KUPCI
        // ============================================================

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                FileName = "Kupci.xlsx"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                SaveExcelFile(dialog.FileName);
                ShowMessage("IZVOZ U EXCEL", "Izvoz kupaca je uspješno završen.");
            }
            catch (Exception ex)
            {
                ShowMessage("GREŠKA", ex.Message);
            }
        }

        private void SaveExcelFile(string filePath)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Kupci");

            string[] headers = { "ID", "Kupac", "Adresa", "Mjesto", "JIB", "PDV" };
            for (int column = 0; column < headers.Length; column++)
                sheet.Cell(1, column + 1).Value = headers[column];

            int row = 2;
            foreach (var buyer in _viewModel.Buyers)
            {
                sheet.Cell(row, 1).Value = buyer.IdKupca;
                sheet.Cell(row, 2).Value = buyer.Kupac ?? string.Empty;
                sheet.Cell(row, 3).Value = buyer.Adresa ?? string.Empty;
                sheet.Cell(row, 4).Value = buyer.Mjesto ?? string.Empty;
                sheet.Cell(row, 5).Value = buyer.JIB?.ToString() ?? string.Empty;
                sheet.Cell(row, 6).Value = buyer.PDV?.ToString() ?? string.Empty;
                row++;
            }

            sheet.Row(1).Style.Font.Bold = true;
            sheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        private async void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                int count = await ImportExcelToSQLiteAsync(dialog.FileName);
                await _viewModel.LoadBuyersAsync();
                ShowMessage("UVOZ EXCEL", $"Uvezeno kupaca: {count}.");
            }
            catch (Exception ex)
            {
                ShowMessage("GREŠKA", ex.Message);
            }
        }

        public async Task<int> ImportExcelToSQLiteAsync(string excelFilePath)
        {
            using var workbook = new XLWorkbook(excelFilePath);
            var sheet = workbook.Worksheets.First();

            var rows = sheet.RowsUsed().Skip(1).ToList();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var db = new AppDbContext();
            foreach (var name in await db.Kupci.Select(x => x.Kupac).ToListAsync())
            {
                if (!string.IsNullOrWhiteSpace(name))
                    names.Add(name.Trim());
            }

            var newBuyers = new List<TblKupci>();
            foreach (var row in rows)
            {
                string name = row.Cell(2).GetString().Trim();
                if (string.IsNullOrWhiteSpace(name) || !names.Add(name))
                    continue;

                newBuyers.Add(new TblKupci
                {
                    Kupac = name,
                    Adresa = row.Cell(3).GetString().Trim(),
                    Mjesto = row.Cell(4).GetString().Trim(),
                    JIB = row.Cell(5).GetString().Trim(),
                    PDV = row.Cell(6).GetString().Trim()
                });
            }

            if (newBuyers.Count > 0)
            {
                await db.Kupci.AddRangeAsync(newBuyers);
                await db.SaveChangesAsync();
            }

            return newBuyers.Count;
        }

        private static void ShowMessage(string title, string message)
        {
            var popup = new MyMessageBox
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            popup.MessageTitle.Text = title;
            popup.MessageText.Text = message;
            popup.ShowDialog();
        }


        // ============================================================
        // LOGO FIRME
        // ============================================================

        private ImageSource LoadLogoImage()
        {
            try
            {
                BitmapImage logo = new BitmapImage();

                logo.BeginInit();
                logo.UriSource = new Uri(Settings.Default.LogoUrl);
                logo.EndInit();

                return logo;
            }
            catch
            {
                return null;
            }
        }

        // ============================================================
        // ŠTAMPANJE FAKTURE
        // ============================================================

        private void BtnFaktura_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_viewModel.SelectedReceipt == null ||
                _viewModel.SelectedBuyer == null ||
                _viewModel.Firma == null)
            {
                return;
            }

            ImageSource logo = LoadLogoImage();

            _viewModel.PrintFaktura(
                _viewModel.ReceiptItems,
                _viewModel.Firma.NazivFirme,
                _viewModel.Firma.Adresa,
                _viewModel.Firma.Grad,
                _viewModel.Firma.JIB,
                _viewModel.Firma.PDV,
                _viewModel.Firma.ZiroRacun,
                _viewModel.SelectedBuyer,
                _viewModel.SelectedReceipt.BrojRacuna.ToString(),
                _viewModel.SelectedReceipt.BrojFiskalnogRacuna.ToString(),
                DateTime.Now,
                _viewModel.SelectedReceipt.NacinPlacanjaName,
                logo);
        }
    }
}
