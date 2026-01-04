using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.Entity;
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
using Caupo.Data;
using Caupo.ViewModels;
using static Caupo.Data.DatabaseTables;
using Caupo.Helpers;

namespace Caupo.Views
{

    public partial class IngredientsPage : UserControl
    {
        public IngredientsPage()
        {
            this.DataContext = new IngredientsViewModel();
         
            InitializeComponent();
            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik.Radnik;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var page = new HomePage();
            page.DataContext = new HomeViewModel();
            PageNavigator.NavigateWithFade (page);
        }





        private async void CancelEditButton_Click(object sender, RoutedEventArgs e)
        {
           
            EditGrid.Visibility = Visibility.Collapsed;
            AddGrid.Visibility = Visibility.Collapsed;
            
            if (DataContext is IngredientsViewModel viewModel)
            {
                
                _BorderBrush = viewModel.FontColor;
                ChangeTextBoxBorderBrush(this, _BorderBrush);
                var ingredient = viewModel.SelectedIngredient;
               await viewModel.LoadIngredientsAsync();
                viewModel.SelectedIngredient = ingredient;
              
                NamirnicaTextBox.IsReadOnly = true;
                PlanskaCijenaTextBox.IsReadOnly = true;
                NabavnaCijenaTextBox.IsReadOnly = true;
                JediniceMjerePicker.IsEnabled = false;
                JediniceMjerePicker.BorderThickness = new Thickness(0, 0, 0, 1);
                if (ingredient != null)
                {
                    var foundItem = ListaNamirnica.Items.Cast<dynamic>().FirstOrDefault(item =>
                        item.IdRepromaterijala == ingredient.IdRepromaterijala );
                    if (foundItem != null)
                    {
                        Debug.WriteLine("Pronađen po ID-u - pokušavam selekciju i scroll");
                        ListaNamirnica.SelectedItem = foundItem;
                        ListaNamirnica.ScrollIntoView(foundItem);
                    }
                }

                ListaNamirnica.ScrollIntoView(ListaNamirnica.SelectedItem);
            }
        }

        private async void SaveEditButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Kreće validacije forme");
            bool isValid = ValidateForm();
            Debug.WriteLine("Završila validacije forme");
            if (!isValid)
            {
                isValid = true;
                Debug.WriteLine("Prekida zbog validacije forme");
                return;
            }
            Debug.WriteLine("Validna forma");
            if (DataContext is IngredientsViewModel viewModel)
            {
                viewModel.ErrorOccurred -= ViewModel_ErrorOccurred;
                viewModel.ErrorOccurred += ViewModel_ErrorOccurred;

                var ingredient = viewModel.SelectedIngredient;
                if (ingredient != null)
                {
                    ingredient.Repromaterijal = NamirnicaTextBox.Text;
                    ingredient.PlanskaCijena = decimal.Parse(PlanskaCijenaTextBox.Text);
                    ingredient.NabavnaCijena = decimal.Parse(NabavnaCijenaTextBox.Text);
                    ingredient.JedinicaMjere = JediniceMjerePicker.SelectedIndex + 1;

                    await viewModel.UpdateIngredient(ingredient);
                }
               
                if (_errorOccurred)
                {
                    _errorOccurred = false;
                    return;  
                }
               
                EditGrid.Visibility = Visibility.Collapsed;
                AddGrid.Visibility = Visibility.Collapsed;
                NamirnicaTextBox.IsReadOnly = true;
                PlanskaCijenaTextBox.IsReadOnly = true;
                NabavnaCijenaTextBox.IsReadOnly = true;
                JediniceMjerePicker.IsEnabled = false;
                JediniceMjerePicker.BorderThickness = new Thickness(0, 0, 0, 1);
                _BorderBrush = viewModel.FontColor;
            
                ChangeTextBoxBorderBrush(this, _BorderBrush);
               

                // Provjeri Contains s različitim pristupima
                bool containsDirect = ListaNamirnica.Items.Contains(ingredient);
                Debug.WriteLine($"Contains direktno: {containsDirect}");

                // Provjeri pomoću LINQ-a (ako je ItemsSource kolekcija)
                var collection = ListaNamirnica.ItemsSource as System.Collections.IEnumerable;
                if (collection != null)
                {
                    bool containsInSource = collection.Cast<object>().Contains(ingredient);
                    Debug.WriteLine($"Contains u ItemsSource: {containsInSource}");
                }

                // Provjeri reference equality
                if (ingredient != null)
                {
                    bool foundByReference = false;
                    foreach (var item in ListaNamirnica.Items)
                    {
                        if (object.ReferenceEquals(item, ingredient))
                        {
                            foundByReference = true;
                            break;
                        }
                    }
                    Debug.WriteLine($"Pronađen po referenci: {foundByReference}");
                }

                // Provjeri Equals metodu
                if (ingredient != null)
                {
                    bool foundByEquals = false;
                    foreach (var item in ListaNamirnica.Items)
                    {
                        if (item != null && item.Equals(ingredient))
                        {
                            foundByEquals = true;
                            break;
                        }
                    }
                    Debug.WriteLine($"Pronađen po Equals: {foundByEquals}");
                }

                // Sada pokušaj sa selekcijom i scrollom
                if (containsDirect)
                {
                    Debug.WriteLine("Ima ga u kolekciji - pokušavam selekciju i scroll");
                    ListaNamirnica.SelectedItem = ingredient;
                    ListaNamirnica.ScrollIntoView(ingredient);
                }
                else
                {
                    Debug.WriteLine("Nema ga u kolekciji");

                    // Pokušaj pronaći item po nekom ID-u ili jedinstvenom svojstvu
                    if (ingredient != null)
                    {
                        var foundItem = ListaNamirnica.Items.Cast<dynamic>().FirstOrDefault(item =>
                            item.IdRepromaterijala == ingredient.IdRepromaterijala /* ili neko drugo svojstvo */);
                        if (foundItem != null)
                        {
                            Debug.WriteLine("Pronađen po ID-u - pokušavam selekciju i scroll");
                            ListaNamirnica.SelectedItem = foundItem;
                            ListaNamirnica.ScrollIntoView(foundItem);
                        }
                    }
                }

            }
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

        private void CancelAddButton_Click(object sender, RoutedEventArgs e)
        {

            EditGrid.Visibility = Visibility.Collapsed;
            AddGrid.Visibility = Visibility.Collapsed;
            var firstItem = ListaNamirnica.Items[0] ;

            if (DataContext is IngredientsViewModel viewModel)
            {
                viewModel.SelectedIngredient = (TblRepromaterijal)firstItem;
                _BorderBrush = viewModel.FontColor;
            }
          
            ChangeTextBoxBorderBrush(this, _BorderBrush);
            NamirnicaTextBox.IsReadOnly = true;
            PlanskaCijenaTextBox.IsReadOnly = true;
            NabavnaCijenaTextBox.IsReadOnly = true;
            JediniceMjerePicker.IsEnabled = false;
            JediniceMjerePicker.BorderThickness = new Thickness(0, 0, 0, 1);
            ListaNamirnica.ScrollIntoView(firstItem);
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


    

        private async void SaveAddButton_Click(object sender, RoutedEventArgs e)
        {
            bool isValid = ValidateForm();

            if (!isValid)
            {
                isValid = true;
                Debug.WriteLine("Pekida zbog praznog polja");
                return;
            }

            if (DataContext is IngredientsViewModel viewModel)
            {
                viewModel.ErrorOccurred -= ViewModel_ErrorOccurred;
                viewModel.ErrorOccurred += ViewModel_ErrorOccurred;

                TblRepromaterijal repromaterijal = new TblRepromaterijal();
               // repromaterijal.IdRepromaterijala = Convert.ToInt32(IdNamirniceTextBox.Text);
                repromaterijal.Repromaterijal = NamirnicaTextBox.Text;
                repromaterijal.PlanskaCijena = Convert.ToDecimal(PlanskaCijenaTextBox.Text);
                repromaterijal.NabavnaCijena = Convert.ToDecimal(NabavnaCijenaTextBox.Text);
                repromaterijal.JedinicaMjere = JediniceMjerePicker.SelectedIndex+1;
                repromaterijal.Zaliha = 0;

                await viewModel.InsertIngredient(repromaterijal);
             
                if (_errorOccurred)
                {
                    _errorOccurred = false;
                    return;
                }

             
                EditGrid.Visibility = Visibility.Collapsed;
                AddGrid.Visibility = Visibility.Collapsed;
                NamirnicaTextBox.IsReadOnly = true;
                PlanskaCijenaTextBox.IsReadOnly = true;
                NabavnaCijenaTextBox.IsReadOnly = true;
                JediniceMjerePicker.IsEnabled = false;
                JediniceMjerePicker.BorderThickness = new Thickness(0, 0, 0, 1);
                _BorderBrush = viewModel.FontColor;

                ChangeTextBoxBorderBrush(this, _BorderBrush);
              
                ListaNamirnica.ScrollIntoView(ListaNamirnica.SelectedItem);
            }
        }

      

        private void ListaNamirnica_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = ListaNamirnica.SelectedItem as DatabaseTables.TblRepromaterijal;
            if (selectedItem != null)
            {
                if (DataContext is IngredientsViewModel viewModel)
                { 
                  viewModel.SelectedIngredient = selectedItem;
                    SearchTextBox.Text = "";
                    ListaNamirnica.ScrollIntoView( ListaNamirnica.SelectedItem);
                }
             }
        }


     
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is IngredientsViewModel viewModel)
            {
       
                EditGrid.Visibility = Visibility.Visible;
                AddGrid.Visibility = Visibility.Collapsed;
                NamirnicaTextBox.IsReadOnly = false;
                PlanskaCijenaTextBox.IsReadOnly = false;
                NabavnaCijenaTextBox.IsReadOnly = false;
                JediniceMjerePicker.IsEnabled = true;
            }
        }

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            await Task.Delay(1);
            EditGrid.Visibility= Visibility.Collapsed;
            AddGrid.Visibility= Visibility.Visible;
            if (DataContext is IngredientsViewModel viewModel)
            {
                viewModel.SelectedIngredient = null;
                NamirnicaTextBox.IsReadOnly = false;
                PlanskaCijenaTextBox.IsReadOnly = false;
                NabavnaCijenaTextBox.IsReadOnly = false;
                JediniceMjerePicker.IsEnabled = true;
                IdNamirniceTextBox.Text = string.Empty;
                NamirnicaTextBox.Text = string.Empty;
                PlanskaCijenaTextBox.Text = string.Empty;
                NabavnaCijenaTextBox.Text = string.Empty;
                JediniceMjerePicker.SelectedIndex = 1;
             
                NamirnicaTextBox.Focus();
            }


        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is IngredientsViewModel viewModel)
            {
                int id = Convert.ToInt32(IdNamirniceTextBox.Text);

                YesNoPopup myMessageBox = new YesNoPopup();
                myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                myMessageBox.MessageTitle.Text = "POTVRDA BRISANJA";
                myMessageBox.MessageText.Text = "Da li ste sigurni da želite obrisati namirnicu :" + Environment.NewLine + NamirnicaTextBox.Text + " ?";
                myMessageBox.ShowDialog();
                if (myMessageBox.Kliknuo == "Da")
                {
                    await viewModel.DeleteIngredients(id);
                }
            }
        }

        Brush? _BorderBrush;
        private bool ValidateForm()
        {
            bool isValid = true;
            if (DataContext is ArticlesViewModel viewModel)
            {
                _BorderBrush = Application.Current.Resources["GlobalFontColor"] as Brush;
            }

            if (string.IsNullOrWhiteSpace(NamirnicaTextBox.Text))
            {
                NamirnicaTextBox.BorderBrush = Brushes.Red;
               isValid = false;
            }
            else
            {
                NamirnicaTextBox.BorderBrush = _BorderBrush;
            }

            if (JediniceMjerePicker.SelectedIndex == -1)
            {
                JediniceMjerePicker.BorderBrush = Brushes.Red;
                JediniceMjerePicker.BorderThickness = new Thickness(0, 0, 0, 1);
                isValid = false;
            }
            else
            {
                JediniceMjerePicker.BorderBrush = _BorderBrush;
                JediniceMjerePicker.BorderThickness = new Thickness(1);
            }

            if (string.IsNullOrWhiteSpace(NabavnaCijenaTextBox.Text))
            {
                NabavnaCijenaTextBox.BorderBrush = Brushes.Red;
                isValid = false;
            }
            else
            {
                NabavnaCijenaTextBox.BorderBrush = _BorderBrush;
            }

            if (string.IsNullOrWhiteSpace(PlanskaCijenaTextBox.Text))
            {
                PlanskaCijenaTextBox.BorderBrush = Brushes.Red;
                isValid = false;
            }
            else
            {
                PlanskaCijenaTextBox.BorderBrush = _BorderBrush;
            }


            return isValid;
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

        private void BtnFirst_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is IngredientsViewModel viewModel)
            {
                viewModel.SelectedIngredient = viewModel.Ingredients.FirstOrDefault();
            }
         }

        private void BtnLast_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is IngredientsViewModel viewModel)
            {
                viewModel.SelectedIngredient = viewModel.Ingredients.Last();
            }
        }
    }
}
