using Caupo.Data;
using Caupo.Fiscal.Common;
using Caupo.Helpers;
using Caupo.ViewModels;
using ClosedXML.Excel;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Views
{
    public partial class ReceiptsPage : UserControl
    {
        private bool _errorOccurred = false;


        public ReceiptsPage()
        {
            DataContext =
                new ReceiptsViewModel ();

            InitializeComponent ();

            lblUlogovaniKorisnik.Content =
                Globals
                    .ulogovaniKorisnik
                    .Radnik;
        }


        // ============================================================
        // ZATVARANJE
        // ============================================================

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


        // ============================================================
        // GREŠKE
        // ============================================================

        private void ViewModel_ErrorOccurred(
            object? sender,
            string? errorMessage)
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
                errorMessage;

            myMessageBox.ShowDialog ();

            _errorOccurred =
                true;
        }


        // ============================================================
        // TEXTBOX BORDER
        // ============================================================

        private void ChangeTextBoxBorderBrush(
            DependencyObject parent,
            Brush? brush)
        {
            for(int i = 0;
                i < VisualTreeHelper.GetChildrenCount (parent);
                i++)
            {
                var child =
                    VisualTreeHelper.GetChild (
                        parent,
                        i);


                if(child is TextBox textBox)
                {
                    if(textBox.Name !=
                       "SearchTextBox")
                    {
                        textBox.BorderThickness =
                            new Thickness (
                                0,
                                0,
                                0,
                                1);

                        textBox.BorderBrush =
                            brush;
                    }
                }


                ChangeTextBoxBorderBrush (
                    child,
                    brush);
            }
        }


        // ============================================================
        // SELECTION
        // ============================================================

        private async void ListaNamirnica_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if(ListaRacuna.SelectedItem
               is not TblRacuni selectedItem)
            {
                return;
            }


            if(DataContext
               is not ReceiptsViewModel viewModel)
            {
                return;
            }


            viewModel.SelectedReceipt =
                selectedItem;

            await viewModel.LoadReceiptItems (
                selectedItem);

            SearchTextBox.Text =
                "";

            ListaRacuna.ScrollIntoView (
                ListaRacuna.SelectedItem);
        }


        private async void ListaRacuna_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if(sender
               is not DataGrid dataGrid)
            {
                return;
            }


            if(dataGrid.SelectedItem
               is not TblRacuni selectedItem)
            {
                return;
            }


            if(DataContext
               is not ReceiptsViewModel viewModel)
            {
                return;
            }


            viewModel.SelectedReceipt =
                selectedItem;

            await viewModel.LoadReceiptItems (
                selectedItem);

            SearchTextBox.Text =
                "";

            dataGrid.ScrollIntoView (
                dataGrid.SelectedItem);
        }


        // ============================================================
        // EXCEL EXPORT
        // ============================================================

        private void BtnExport_Click(
            object sender,
            RoutedEventArgs e)
        {
            var saveFileDialog =
                new SaveFileDialog
                {
                    InitialDirectory =
                        "C:\\",

                    Filter =
                        "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",

                    DefaultExt =
                        ".xlsx"
                };


            bool? result =
                saveFileDialog.ShowDialog ();


            if(result == true)
            {
                SaveExcelFile (
                    saveFileDialog.FileName);
            }
        }


        private void SaveExcelFile(
            string filePath)
        {
            using(var workbook =
                  new XLWorkbook ())
            {
                var worksheet =
                    workbook.Worksheets.Add (
                        "Namirnice");


                worksheet.Cell (1, 1).Value =
                    "ID";

                worksheet.Cell (1, 2).Value =
                    "Namirnica";

                worksheet.Cell (1, 3).Value =
                    "Jedinica mjere";

                worksheet.Cell (1, 4).Value =
                    "Planska cijena";

                worksheet.Cell (1, 5).Value =
                    "Nabavna cijena";


                if(DataContext
                   is IngredientsViewModel viewModel)
                {
                    int row =
                        2;


                    foreach(var ing in
                            viewModel.Ingredients)
                    {
                        worksheet.Cell (row, 1).Value =
                            ing.IdRepromaterijala;

                        worksheet.Cell (row, 2).Value =
                            ing.Repromaterijal;

                        worksheet.Cell (row, 3).Value =
                            ing.JedinicaMjere;

                        worksheet.Cell (row, 4).Value =
                            ing.JedinicaMjere;

                        worksheet.Cell (row, 5).Value =
                            ing.PlanskaCijena;

                        worksheet.Cell (row, 6).Value =
                            ing.NabavnaCijena;

                        row++;
                    }
                }


                workbook.SaveAs (
                    filePath);
            }


            var myMessageBox =
                new MyMessageBox
                {
                    WindowStartupLocation =
                        WindowStartupLocation.CenterScreen
                };

            myMessageBox.MessageTitle.Text =
                "IZVOZ U EXCEL";

            myMessageBox.MessageText.Text =
                "Izvoz tabele sa namirnicama je uspješno završen.";

            myMessageBox.ShowDialog ();
        }


        // ============================================================
        // EXCEL IMPORT
        // ============================================================

        private async void BtnImport_Click(
            object sender,
            RoutedEventArgs e)
        {
            var openFileDialog =
                new OpenFileDialog
                {
                    Filter =
                        "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*"
                };


            if(openFileDialog.ShowDialog ()
               != true)
            {
                return;
            }


            await ImportExcelToSQLiteAsync (
                openFileDialog.FileName);
        }


        public async Task ImportExcelToSQLiteAsync(
            string excelFilePath)
        {
            using(var workbook =
                  new XLWorkbook (
                      excelFilePath))
            {
                var worksheet =
                    workbook.Worksheets.First ();


                using var db =
                    new AppDbContext ();


                var ingList =
                    worksheet.RowsUsed ()
                        .Skip (1)
                        .Select (
                            row =>
                                new TblRepromaterijal
                                {
                                    Repromaterijal =
                                        row.Cell (2)
                                            .GetValue<string> (),

                                    JedinicaMjere =
                                        row.Cell (3)
                                            .GetValue<int> (),

                                    PlanskaCijena =
                                        row.Cell (4)
                                            .GetValue<decimal?> (),

                                    NabavnaCijena =
                                        row.Cell (5)
                                            .GetValue<decimal?> (),

                                    Zaliha =
                                        0
                                })
                        .ToList ();


                await db.Repromaterijal
                    .AddRangeAsync (
                        ingList);

                await db.SaveChangesAsync ();
            }


            var myMessageBox =
                new MyMessageBox
                {
                    WindowStartupLocation =
                        WindowStartupLocation.CenterScreen
                };

            myMessageBox.MessageTitle.Text =
                "UVOZ EXCEL";

            myMessageBox.MessageText.Text =
                "Uvoz namirnica iz Excel fajla je završen!";

            myMessageBox.ShowDialog ();
        }


        // ============================================================
        // PRETHODNI RAČUN
        // ============================================================

        private async void BtnFirst_Click(
            object sender,
            RoutedEventArgs e)
        {
            if(DataContext
               is not ReceiptsViewModel viewModel)
            {
                return;
            }


            if(viewModel.SelectedReceipt == null)
                return;


            int index =
                viewModel.Receipts.IndexOf (
                    viewModel.SelectedReceipt);


            if(index <= 0)
                return;


            viewModel.SelectedReceipt =
                viewModel.Receipts[
                    index - 1];


            await viewModel.LoadReceiptItems (
                viewModel.SelectedReceipt);
        }


        // ============================================================
        // SLJEDEĆI RAČUN
        // ============================================================

        private async void BtnLast_Click(
            object sender,
            RoutedEventArgs e)
        {
            if(DataContext
               is not ReceiptsViewModel viewModel)
            {
                return;
            }


            if(viewModel.SelectedReceipt == null)
                return;


            int index =
                viewModel.Receipts.IndexOf (
                    viewModel.SelectedReceipt);


            if(index < 0 ||
               index >=
               viewModel.Receipts.Count - 1)
            {
                return;
            }


            viewModel.SelectedReceipt =
                viewModel.Receipts[
                    index + 1];


            await viewModel.LoadReceiptItems (
                viewModel.SelectedReceipt);
        }


        // ============================================================
        // KOPIJA RAČUNA
        // ============================================================

        private async void BtnDuplicate_Click(
            object sender,
            RoutedEventArgs e)
        {
            if(DataContext
               is not ReceiptsViewModel viewModel)
            {
                return;
            }


            try
            {
                FiscalResult result =
                    await viewModel
                        .IzdajKopijuAsync ();


                Debug.WriteLine (
                    $"[COPY] " +
                    $"Success={result.Success}, " +
                    $"Fiscalized={result.Fiscalized}, " +
                    $"Saved={result.SavedToDatabase}, " +
                    $"Printed={result.Printed}");


                if(!result.Success)
                {
                    ShowMessage (
                        "GREŠKA",
                        result.ErrorMessage
                        ?? "Kopija računa nije izdata.");

                    return;
                }


                string? warning =
                    BuildFiscalWarning (
                        result);


                if(!string.IsNullOrWhiteSpace (
                    warning))
                {
                    ShowMessage (
                        "UPOZORENJE",
                        warning);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    "[COPY] Greška: " +
                    ex);

                ShowMessage (
                    "GREŠKA",
                    ex.Message);
            }
        }


        // ============================================================
        // STORNO / REFUND
        // ============================================================

        private async void BtnStorno_Click(
            object sender,
            RoutedEventArgs e)
        {
            if(DataContext
               is not ReceiptsViewModel viewModel)
            {
                return;
            }


            if(viewModel.SelectedReceipt == null)
            {
                ShowMessage (
                    "GREŠKA",
                    "Nije odabran račun.");

                return;
            }


            TblRacuni selectedItem =
                viewModel.SelectedReceipt;


            try
            {
                FiscalResult result =
                    await viewModel
                        .StornirajRacunAsync ();


                Debug.WriteLine (
                    $"[REFUND] " +
                    $"Success={result.Success}, " +
                    $"Fiscalized={result.Fiscalized}, " +
                    $"Saved={result.SavedToDatabase}, " +
                    $"Printed={result.Printed}");


                if(!result.Success)
                {
                    ShowMessage (
                        "GREŠKA",
                        result.ErrorMessage
                        ?? "Storno računa nije uspio.");

                    return;
                }


                string? warning =
                    BuildFiscalWarning (
                        result);


                if(!string.IsNullOrWhiteSpace (
                    warning))
                {
                    ShowMessage (
                        "UPOZORENJE",
                        warning);
                }


                await viewModel.LoadReceiptsAsync (
                    selectedItem);


                if(viewModel.SelectedReceipt != null)
                {
                    ListaRacuna.ScrollIntoView (
                        viewModel.SelectedReceipt);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    "[REFUND] Greška: " +
                    ex);

                ShowMessage (
                    "GREŠKA",
                    ex.Message);
            }
        }


        // ============================================================
        // MESSAGE BOX
        // ============================================================

        private static void ShowMessage(
            string title,
            string message)
        {
            var myMessageBox =
                new MyMessageBox
                {
                    WindowStartupLocation =
                        WindowStartupLocation.CenterScreen
                };


            myMessageBox.MessageTitle.Text =
                title;

            myMessageBox.MessageText.Text =
                message;

            myMessageBox.ShowDialog ();
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
                    "Fiskalizacija nije potvrđena.");
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