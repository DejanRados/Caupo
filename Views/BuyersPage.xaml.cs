using Caupo.Data;
using Caupo.Helpers;
using Caupo.Properties;
using Caupo.ViewModels;
using ClosedXML.Excel;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Views
{

    public partial class BuyersPage : UserControl
    {
        public BuyersPage()
        {
            InitializeComponent ();
            this.DataContext = new BuyersViewModel ();

            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik.Radnik;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var page = new HomePage ();
            page.DataContext = new HomeViewModel ();
            PageNavigator.NavigateWithFade (page);
        }


        private void ListaKupaca_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(ListaKupaca.SelectedItem is TblKupci selectedBuyer &&
                DataContext is BuyersViewModel vm)
            {
                vm.SelectedBuyer = selectedBuyer;

                SearchTextBox.Text = string.Empty;
                ListaKupaca.ScrollIntoView (selectedBuyer);
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
        private void BtnFirst_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is BuyersViewModel vm)
            {
                int index = vm.Buyers.IndexOf (vm.SelectedBuyer);
                if(index > 0)
                {
                    vm.SelectedBuyer = vm.Buyers[index - 1];
                }
            }
        }

        private void BtnLast_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is BuyersViewModel vm)
            {
                int index = vm.Buyers.IndexOf (vm.SelectedBuyer);
                if(index < vm.Buyers.Count - 1)
                {
                    vm.SelectedBuyer = vm.Buyers[index + 1];
                }
            }
        }


        private void BtnDuplicate_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ListaRacuna_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(ListaRacuna.SelectedItem is TblRacuni selectedReceipt &&
                DataContext is BuyersViewModel vm)
            {
                vm.SelectedReceipt = selectedReceipt;
                ListaRacuna.ScrollIntoView (selectedReceipt);
            }
        }


        private ImageSource LoadLogoImage()
        {

            try
            {
                BitmapImage logo = new BitmapImage ();
                logo.BeginInit ();
                logo.UriSource = new Uri (Settings.Default.LogoUrl);
                logo.EndInit ();
                return logo;
            }
            catch
            {
                return null; // Ako logo nije pronađen
            }
        }

        private void BtnFaktura_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is BuyersViewModel vm &&
                vm.SelectedReceipt != null &&
                vm.SelectedBuyer != null &&
                vm.Firma != null)
            {
                ImageSource logo = LoadLogoImage ();

                vm.PrintFaktura (
                    vm.ReceiptItems,
                    vm.Firma.NazivFirme,
                    vm.Firma.Adresa,
                    vm.Firma.Grad,
                    vm.Firma.JIB,
                    vm.Firma.PDV,
                    vm.Firma.ZiroRacun,
                    vm.SelectedBuyer,
                    vm.SelectedReceipt.BrojRacuna.ToString (),
                    vm.SelectedReceipt.BrojFiskalnogRacuna.ToString (),
                    DateTime.Now,
                    vm.SelectedReceipt.NacinPlacanjaName,
                    logo);
            }
        }



        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if(DataContext is BuyersViewModel vm)
                await vm.InitializeAsync ();
        }

    }
}
