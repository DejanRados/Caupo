using Caupo.Data;
using Caupo.Fiscal;
using Caupo.Helpers;
using Caupo.ViewModels;
using ClosedXML.Excel;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Views
{

    public partial class ReceiptsPage : UserControl
    {
        public ReceiptsPage()
        {
            this.DataContext = new ReceiptsViewModel ();

            InitializeComponent ();
            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik.Radnik;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var page = new HomePage ();
            page.DataContext = new HomeViewModel ();
            PageNavigator.NavigateWithFade (page);
        }









        private bool _errorOccurred = false;
        private void ViewModel_ErrorOccurred(object? sender, string? errorMessage)
        {
            // Ako je došlo do greške, prikažite poruku
            MyMessageBox myMessageBox = new MyMessageBox
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            myMessageBox.MessageTitle.Text = "GREŠKA";
            myMessageBox.MessageText.Text = errorMessage;
            myMessageBox.ShowDialog ();
            _errorOccurred = true;

        }



        private void ChangeTextBoxBorderBrush(DependencyObject parent, Brush? brush)
        {
            for(int i = 0; i < VisualTreeHelper.GetChildrenCount (parent); i++)
            {
                var child = VisualTreeHelper.GetChild (parent, i);

                if(child is TextBox textBox)
                {
                    // Preskoči SearchTextBox
                    if(textBox.Name != "SearchTextBox")
                    {
                        textBox.BorderThickness = new Thickness (0, 0, 0, 1);
                        textBox.BorderBrush = brush; // samo TextBox!
                    }
                }

                // Rekurzivno provjeri djecu
                ChangeTextBoxBorderBrush (child, brush);
            }
        }








        private void ListaNamirnica_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = ListaRacuna.SelectedItem as DatabaseTables.TblRacuni;
            if(selectedItem != null)
            {
                if(DataContext is ReceiptsViewModel viewModel)
                {
                    viewModel.SelectedReceipt = selectedItem;
                    viewModel.LoadReceiptItems (selectedItem);
                    SearchTextBox.Text = "";
                    ListaRacuna.ScrollIntoView (ListaRacuna.SelectedItem);
                }
            }
        }


        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {

            SaveFileDialog saveFileDialog = new SaveFileDialog ();
            saveFileDialog.InitialDirectory = "C:\\";
            saveFileDialog.Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*";
            saveFileDialog.DefaultExt = ".xlsx";

            bool? result = saveFileDialog.ShowDialog ();

            if(result == true)
            {
                string filePath = saveFileDialog.FileName;
                SaveExcelFile (filePath);
            }
        }

        private void SaveExcelFile(string filePath)
        {

            using(var workbook = new XLWorkbook ())
            {
                var worksheet = workbook.Worksheets.Add ("Namirnice");

                worksheet.Cell (1, 1).Value = "ID";
                worksheet.Cell (1, 2).Value = "Namirnica";
                worksheet.Cell (1, 3).Value = "Jedinica mjere";
                worksheet.Cell (1, 4).Value = "Planska cijena";
                worksheet.Cell (1, 5).Value = "Nabavna cijena";


                if(DataContext is IngredientsViewModel viewModel)
                {
                    int row = 2;

                    foreach(var ing in viewModel.Ingredients)
                    {
                        worksheet.Cell (row, 1).Value = ing.IdRepromaterijala;
                        worksheet.Cell (row, 2).Value = ing.Repromaterijal;
                        worksheet.Cell (row, 3).Value = ing.JedinicaMjere;
                        worksheet.Cell (row, 4).Value = ing.JedinicaMjere;
                        worksheet.Cell (row, 5).Value = ing.PlanskaCijena;
                        worksheet.Cell (row, 6).Value = ing.NabavnaCijena;


                        row++;
                    }
                }
                workbook.SaveAs (filePath);
            }

            MyMessageBox myMessageBox = new MyMessageBox
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            myMessageBox.MessageTitle.Text = "IZVOZ U EXCEL";
            myMessageBox.MessageText.Text = "Izvoz tabele sa namirnicama je uspješno završen.";
            myMessageBox.ShowDialog ();
        }
        private async void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog ();
            openFileDialog.Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*";
            if(openFileDialog.ShowDialog () == true)
            {
                await ImportExcelToSQLiteAsync (openFileDialog.FileName);
            }
            else
            {
                return;
            }


        }

        public async Task ImportExcelToSQLiteAsync(string excelFilePath)
        {
            using(var workbook = new XLWorkbook (excelFilePath))
            {
                var worksheet = workbook.Worksheets.First ();
                using(var db = new AppDbContext ())
                {

                    var ingList = worksheet.RowsUsed ()
                        .Skip (1)
                        .Select (row => new TblRepromaterijal
                        {
                            Repromaterijal = row.Cell (2).GetValue<string> (),
                            JedinicaMjere = row.Cell (3).GetValue<int> (),
                            PlanskaCijena = row.Cell (4).GetValue<decimal?> (),
                            NabavnaCijena = row.Cell (5).GetValue<decimal?> (),
                            Zaliha = 0,


                        })
                        .ToList ();


                    await db.Repromaterijal.AddRangeAsync (ingList);
                    await db.SaveChangesAsync ();
                }
            }
            MyMessageBox myMessageBox = new MyMessageBox
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            myMessageBox.MessageTitle.Text = "UVOZ EXCEL";
            myMessageBox.MessageText.Text = "Uvoz namirnica iz Excel fajla je završen!";
            myMessageBox.ShowDialog ();

        }

        private async void BtnFirst_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is ReceiptsViewModel viewModel)
            {
                int index = viewModel.Receipts.IndexOf (viewModel.SelectedReceipt);
                if(index > 0)
                {
                    viewModel.SelectedReceipt = viewModel.Receipts[index - 1];
                    await viewModel.LoadReceiptItems (viewModel.SelectedReceipt);
                }

            }
        }

        private async void BtnLast_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is ReceiptsViewModel viewModel)
            {
                int index = viewModel.Receipts.IndexOf (viewModel.SelectedReceipt);
                if(index < viewModel.Receipts.Count - 1)
                {
                    viewModel.SelectedReceipt = viewModel.Receipts[index + 1];
                    await viewModel.LoadReceiptItems (viewModel.SelectedReceipt);
                }

            }
        }
        private async void BtnDuplicate_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is ReceiptsViewModel viewModel)
            {
                bool result = false;
                using(var db = new AppDbContext ())
                {
                    kupac = db.Kupci.FirstOrDefault (k => k.Kupac.ToLower () == viewModel.SelectedReceipt.Kupac.ToLower ());
                }
                string referentDocumentDT = viewModel.SelectedReceipt.Datum.ToString ("yyyy-MM-dd'T'HH:mm:ss.fffzzz");
                string referentDocumentNumber = viewModel.SelectedReceipt.BrojFiskalnogRacuna;
                FiskalniRacun racun = new FiskalniRacun ();

                result = await racun.IzdajFiskalniRacun ("Copy", "Sale", referentDocumentNumber, referentDocumentDT, viewModel.StavkeRacuna, kupac, viewModel.SelectedReceipt.NacinPlacanja, viewModel.IznosRacuna);
                if(result)
                {
                    // await viewModel.LoadReceiptsAsync ();
                }

            }
        }

        private void ListaRacuna_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            var selectedItem = dataGrid.SelectedItem as DatabaseTables.TblRacuni;

            if(selectedItem != null && DataContext is ReceiptsViewModel viewModel)
            {
                viewModel.SelectedReceipt = selectedItem;
                viewModel.LoadReceiptItems (selectedItem);
                SearchTextBox.Text = "";
                dataGrid.ScrollIntoView (dataGrid.SelectedItem);
            }
        }

        TblKupci kupac = new TblKupci ();
        private async void BtnStorno_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is ReceiptsViewModel viewModel)
            {

                var selectedItem = viewModel.SelectedReceipt;
                bool result = false;
                using(var db = new AppDbContext ())
                {
                    kupac = db.Kupci.FirstOrDefault (k => k.Kupac.ToLower () == viewModel.SelectedReceipt.Kupac.ToLower ());
                }
                string referentDocumentDT = viewModel.SelectedReceipt.Datum.ToString ("yyyy-MM-dd'T'HH:mm:ss.fffzzz");
                string referentDocumentNumber = viewModel.SelectedReceipt.BrojFiskalnogRacuna;
                FiskalniRacun racun = new FiskalniRacun ();
                result = await racun.IzdajFiskalniRacun ("Training", "Refund", referentDocumentNumber, referentDocumentDT, viewModel.StavkeRacuna, kupac, viewModel.SelectedReceipt.NacinPlacanja, viewModel.IznosRacuna);
                if(result)
                {
                    await viewModel.LoadReceiptsAsync (selectedItem);
                    ListaRacuna.ScrollIntoView (selectedItem);
                }

            }


        }

    }
}

