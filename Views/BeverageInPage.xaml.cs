using ClosedXML.Excel;
using Caupo.Data;
using Caupo.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;

using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static Caupo.Data.DatabaseTables;
using Caupo.Helpers;

namespace Caupo.Views
{

    public partial class BeverageInPage : UserControl
    {
        public BeverageInPageViewModel ViewModel { get; set; }
        public bool OpenedFromSupplier { get; set; } = false;
        public UserControl PreviousPage { get; set; }
        private int _brojUlaza;
        public BeverageInPage(int brojulaza = 0)
        {
            InitializeComponent();
            ViewModel = new BeverageInPageViewModel();
            this.DataContext = ViewModel;
            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik.Radnik;
           // this.Loaded += BeverageInPage_Loaded;
            _ = LoadInitialStockAsync(brojulaza);
          //  _brojUlaza = brojulaza;
        }

     /*   private async void BeverageInPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= BeverageInPage_Loaded; 

            await ViewModel.Start();               // prvo inicijalizacija
            await ViewModel.LoadStockInAsync (_brojUlaza);    // zatim učitaj ulaze
        }*/
        private async Task LoadInitialStockAsync(int brojUlaza)
        {
           //await ViewModel.Start ();
            await ViewModel.LoadStockInAsync(brojUlaza);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            OnClosing (null,null);
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
            myMessageBox.ShowDialog();
            _errorOccurred = true;
           
        }

     

        private void ChangeTextBoxBorderBrush(DependencyObject parent, Brush? brush)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is TextBox textBox)
                {
                    // Preskoči SearchTextBox
                    if (textBox.Name != "SearchTextBox")
                    {
                        textBox.BorderThickness = new Thickness(0, 0, 0, 1);
                        textBox.BorderBrush = brush; // samo TextBox!
                    }
                }

                // Rekurzivno provjeri djecu
                ChangeTextBoxBorderBrush(child, brush);
            }
        }


    

     

      

        private void ListaNamirnica_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = ListaArikala.SelectedItem as DatabaseTables.TblArtikli;
            if (selectedItem != null)
            {
                if (DataContext is BeverageInPageViewModel viewModel)
                { 
                  viewModel.SelectedArticle = selectedItem;
                   // viewModel.LoadReceiptItems(selectedItem);
                    SearchTextBox.Text = "";
                    ListaArikala.ScrollIntoView( ListaArikala.SelectedItem);
                }
             }
        }


     
  

     

  

      
   

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = "C:\\";
            saveFileDialog.Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*";
            saveFileDialog.DefaultExt = ".xlsx"; 

            bool? result = saveFileDialog.ShowDialog();

            if (result == true)
            {
                string filePath = saveFileDialog.FileName;
                SaveExcelFile(filePath);
            }
        }

        private void SaveExcelFile(string filePath)
        {

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Namirnice");

                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Namirnica";
                worksheet.Cell(1, 3).Value = "Jedinica mjere";
                worksheet.Cell(1, 4).Value = "Planska cijena";
                worksheet.Cell(1, 5).Value = "Nabavna cijena";

                
                if (DataContext is IngredientsViewModel viewModel)
                {
                    int row = 2;

                    foreach (var ing in viewModel.Ingredients)
                    {
                        worksheet.Cell(row, 1).Value = ing.IdRepromaterijala;
                        worksheet.Cell(row, 2).Value = ing.Repromaterijal;
                        worksheet.Cell(row, 3).Value = ing.JedinicaMjere;
                        worksheet.Cell(row, 4).Value = ing.JedinicaMjere;
                        worksheet.Cell(row, 5).Value = ing.PlanskaCijena;
                        worksheet.Cell(row, 6).Value = ing.NabavnaCijena;
                       
                        
                        row++;
                    }
                }
                workbook.SaveAs(filePath);
            }

            MyMessageBox myMessageBox = new MyMessageBox
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            myMessageBox.MessageTitle.Text = "IZVOZ U EXCEL";
            myMessageBox.MessageText.Text = "Izvoz tabele sa namirnicama je uspješno završen.";
            myMessageBox.ShowDialog();
        }
        private async void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
              await  ImportExcelToSQLiteAsync(openFileDialog.FileName);
            }
            else
            {
                return;
            }

      
        }

        public async Task ImportExcelToSQLiteAsync(string excelFilePath)
        {
            using (var workbook = new XLWorkbook(excelFilePath))
            {
                var worksheet = workbook.Worksheets.First(); 
                using (var db= new AppDbContext())
                {

                    var ingList = worksheet.RowsUsed()
                        .Skip(1) 
                        .Select(row => new TblRepromaterijal
                        {
                            Repromaterijal = row.Cell(2).GetValue<string>(),
                            JedinicaMjere= row.Cell(3).GetValue<int>(), 
                           PlanskaCijena = row.Cell(4).GetValue<decimal?>(),
                            NabavnaCijena = row.Cell(5).GetValue<decimal?>(), 
                            Zaliha = 0,
                           

                        })
                        .ToList();


                    await db.Repromaterijal.AddRangeAsync(ingList);
                    await db.SaveChangesAsync();
                }
            }
            MyMessageBox myMessageBox = new MyMessageBox
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            myMessageBox.MessageTitle.Text = "UVOZ EXCEL";
            myMessageBox.MessageText.Text = "Uvoz namirnica iz Excel fajla je završen!";
            myMessageBox.ShowDialog();
           
        }

        private async void BtnFirst_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(" BtnFirst_Click DataContext" + DataContext.ToString());
            if (DataContext is BeverageInPageViewModel viewModel)
            {
                int index = viewModel.StockInFilter.IndexOf(viewModel.SelectedStockIn);
                if (index > 0)
                {
                    viewModel.SelectedStockIn = viewModel.StockInFilter[index - 1];
                    await viewModel.LoadStockInItems(viewModel.SelectedStockIn);
                }

            }
            else
            {
                Debug.WriteLine(" BtnFirst_Click DataContext ELSE" + DataContext.ToString());
            }
        }

        private async void BtnLast_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(" BtnLast_Click DataContext" + DataContext.ToString());
            if (DataContext is BeverageInPageViewModel viewModel)
            {
                int index = viewModel.StockInFilter.IndexOf(viewModel.SelectedStockIn);
                if (index < viewModel.StockInFilter.Count - 1)
                {
                    viewModel.SelectedStockIn = viewModel.StockInFilter[index + 1];
                    await viewModel.LoadStockInItems(viewModel.SelectedStockIn);
                }

            }
            else
            {
                Debug.WriteLine(" BtnLast_Click DataContext ELSE" + DataContext.ToString());
            }
        }
        private void BtnDuplicate_Click(object sender, RoutedEventArgs e)
        {

        }

      


        private void ListaArikala_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ListaArikala.SelectedItem is TblArtikli artikl)
            {

                var vm = DataContext as BeverageInPageViewModel;
                vm.ErrorOccurred -= ViewModel_ErrorOccurred;
                vm.ErrorOccurred += ViewModel_ErrorOccurred;
                if (vm.SelectedArticle != null)
                {
                    var win = new BeverageInPopup(vm.SelectedArticle);
                    if (win.ShowDialog() == true)
                    {
                        Debug.WriteLine("Dobijam nazad, cijena: " + win.EnteredPrice + ", kolićina: " + win.EnteredQuantity + " i popust: " + win.EnteredDiscount);
                        vm.EnteredPrice = win.EnteredPrice;
                        vm.EnteredQuantity = win.EnteredQuantity;
                        vm.EnteredDiscount = win.EnteredDiscount;

                        vm.ProcessArticle();
                    }

                }
                }
                if (_errorOccurred)
                {
                    _errorOccurred = false;
                    return;
                }
            }

        private async void ListaStavki_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ListaStavki.SelectedItem is TblUlazStavke stavka)
            {
                Debug.WriteLine("Selektovana stavka na stranici u listi: " + stavka.Artikl);
                var vm = DataContext as BeverageInPageViewModel;
                Debug.WriteLine("Selektovana stavka na viewmodelu: " + vm.SelectedStockInItem.Artikl);
                vm.ErrorOccurred -= ViewModel_ErrorOccurred;
                vm.ErrorOccurred += ViewModel_ErrorOccurred;
                if (stavka != null)
                {
                    var win = new BeverageInPopup(stavka);

                    win.IsUpdate = true;
                    if (win.ShowDialog() == true)
                    {
                        Debug.WriteLine("Dobijam nazad, cijena: " + win.EnteredPrice + ", kolićina: " + win.EnteredQuantity + " i popust: " + win.EnteredDiscount);
                        vm.EnteredPrice = win.EnteredPrice;
                        vm.EnteredQuantity = win.EnteredQuantity;
                        vm.EnteredDiscount = win.EnteredDiscount;

                      await  vm.UpdateStockInItem();
                      
                      
                    }
                    win.IsUpdate = false;
                }
            }
            if (_errorOccurred)
            {
                _errorOccurred = false;
                return;
            }
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Debug.WriteLine ("OnClosing triggered");

            if(DataContext is BeverageInPageViewModel viewModel && viewModel.HasUnsavedChanges)
            {
                Debug.WriteLine ("DataContext is BeverageInPageViewModel and HasUnsavedChanges = true");

                var myMessageBox = new YesNoPopup ();
                myMessageBox.MessageTitle.Text = "UPOZORENJE";
                myMessageBox.MessageText.Text = "Postoje nesnimljene promjene koje ste napravili.\nŽelite zatvoriti stranicu?";
                myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                Debug.WriteLine ("Showing YesNoPopup...");
                bool? result = myMessageBox.ShowDialog ();
                Debug.WriteLine ($"YesNoPopup closed. Kliknuo = {myMessageBox.Kliknuo}");

                if(myMessageBox.Kliknuo != "Da")
                {
                    Debug.WriteLine ("Korisnik nije kliknuo 'Da', otkazujem zatvaranje.");
                    //e.Cancel = true; // Dodao sam e.Cancel da zapravo spriječi zatvaranje
                    return;
                }
                else
                {
                    Debug.WriteLine ("Korisnik je kliknuo 'Da', nastavljam navigaciju.");

                    if(OpenedFromSupplier && PreviousPage != null)
                    {
                        PageNavigator.NavigateWithFade (PreviousPage);
                      
                    }
                    else
                    {
                        Debug.WriteLine ("OpenedFromSupplier = false, navigiram na HomePage");
                        var page = new HomePage ();
                        page.DataContext = new HomeViewModel ();
                        PageNavigator.NavigateWithFade (page);
                    }
                  
                }
            }
            else
            {
                Debug.WriteLine ("Nema nesnimljenih promjena ili DataContext nije BeverageInPageViewModel");

                if(OpenedFromSupplier && PreviousPage != null)
                {
                    PageNavigator.NavigateWithFade (PreviousPage);
                   
                }
                else
                {
                    Debug.WriteLine ("OpenedFromSupplier = false, navigiram na HomePage");
                    var page = new HomePage ();
                    page.DataContext = new HomeViewModel ();
                    PageNavigator.NavigateWithFade (page);
                }
            }

            Debug.WriteLine ("OnClosing finished");
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is BeverageInPageViewModel viewModel)
            {
               viewModel.PrintKalkulacija(viewModel.StockInItems, viewModel.Klijent, viewModel.SelectedStockIn, viewModel.SelectedSupplier);
            }
         }
     
    }
 }

