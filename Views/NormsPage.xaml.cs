using Caupo.Data;
using Caupo.Helpers;
using Caupo.ViewModels;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Globalization;
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

namespace Caupo.Views
{

    public partial class NormsPage : UserControl, IKeyboardInputReceiver
    {
        public NormsPage()
        {
            this.DataContext = new NormsViewModel();
         
            InitializeComponent();
            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik.Radnik;
        }

        private VirtualKeyboard virtualKeyboard;
        private TextBox? FocusedTextBox = null;

        public void ReceiveKey(string key)
        {
            // 📌 Ako TextBox ima fokus → unos teksta
            if (FocusedTextBox != null)
            {
                switch (key)
                {
                    case "\uE72B":

                        if (FocusedTextBox.Text.Length > 0)
                        {
                            int pos = FocusedTextBox.SelectionStart;
                            if (pos > 0)
                            {
                                FocusedTextBox.Text =
                                    FocusedTextBox.Text.Remove (pos - 1, 1);
                                FocusedTextBox.SelectionStart = pos - 1;
                            }
                        }
                     break;

                    case "\uE75D":
                        InsertIntoFocused ("\u0020");
                        break;
                    case "Sakrij":
                        FocusedTextBox.Text = "";
                        FocusedTextBox = null;
                        ListaNamirnica.Focus ();
                        MainWindow.Instance.HideKeyboard ();
                        break;
                    case "Enter":
                        FocusedTextBox.Text = "";
                        FocusedTextBox = null;
                        ListaNamirnica.Focus ();
                        MainWindow.Instance.HideKeyboard ();
                        break;
                    case "Reset":
                        FocusedTextBox.Text = "";
          
                        break;

                    default:
                        InsertIntoFocused (key);
                        break;
                }

                return; 
            }
          
            }
       
        
        private void InsertIntoFocused(string text)
        {
            if (FocusedTextBox == null) return;

            int pos = FocusedTextBox.SelectionStart;
            FocusedTextBox.Text = FocusedTextBox.Text.Insert (pos, text);
            FocusedTextBox.SelectionStart = pos + text.Length;
        }
        private void KeyboardButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.ToggleKeyboard ();

        }
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                FocusedTextBox = sender as TextBox;
                FocusedTextBox.Clear ();
                FocusedTextBox.SelectAll ();
                MainWindow.Instance.ShowKeyboard ();
            }
            catch (Exception ex)
            {
                Debug.WriteLine ("TextBox_GotFocus salje: " + ex);
            }


        }
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            FocusedTextBox = null;
            MainWindow.Instance.HideKeyboard ();
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

            MyMessageBox myMessageBox = new MyMessageBox
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            myMessageBox.MessageTitle.Text = "GREŠKA";
            myMessageBox.MessageText.Text = errorMessage;
            myMessageBox.ShowDialog();
            _errorOccurred = true;
           
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

        Brush? _BorderBrush;

        private async void BtnFirst_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is NormsViewModel viewModel)
            {
                int index = viewModel.Jela.IndexOf(viewModel.SelectedJelo);
                if (index > 0)
                {
                    viewModel.SelectedJelo = viewModel.Jela[index - 1];
                   await viewModel.LoadNormativ(viewModel.SelectedJelo);
                }
                   
            }
         }

        private async void BtnLast_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is NormsViewModel viewModel)
            {
                int index = viewModel.Jela.IndexOf(viewModel.SelectedJelo);
                if (index < viewModel.Jela.Count - 1)
                {
                    viewModel.SelectedJelo = viewModel.Jela[index + 1];
                    await viewModel.LoadNormativ(viewModel.SelectedJelo);
                }
                  
            }
        }
        int IdJela = 0;
        private async void lstSuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox lb && lb.SelectedItem is TblArtikli selectedArtikl)
            {

 
                IdJela = selectedArtikl.IdArtikla;
                var vm = DataContext as NormsViewModel;
                vm.SearchText = "";
                vm.Suggestions.Clear();
                await vm.LoadNormativ(selectedArtikl);

                lb.SelectedItem = null;
            }
        }

        private async void ListaNamirnica_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

            var dataGrid = sender as DataGrid;
            var repromaterijal = dataGrid.SelectedItem as TblRepromaterijal;

            if (repromaterijal != null && DataContext is NormsViewModel vm)
            {

               vm.ErrorOccurred -= ViewModel_ErrorOccurred;
               vm.ErrorOccurred += ViewModel_ErrorOccurred;
                if (vm.SelectedJelo != null)
                {
                   MyInputBox myInput = new MyInputBox();
                    myInput.InputTitle.Text = "NOVI NORMATIV" + Environment.NewLine + Environment.NewLine + "Unesite potrebnu količinu " + repromaterijal.Repromaterijal + " u " + repromaterijal.JedinicaMjereName;
                    myInput.InputTitle.FontSize = 14;
                    myInput.NumbersOnly = true;

                    myInput.ShowDialog();
                    string result = myInput.result;

                    if (!string.IsNullOrEmpty(result) && Convert.ToDecimal(result)!=0)
                    {

                        var norm = new TblNormativ();
                        norm.Cijena = repromaterijal.NabavnaCijena ?? 0;
                        norm.Repromaterijal = repromaterijal.Repromaterijal;
                        norm.Kolicina =  Convert.ToDecimal(result);
                        norm.JedinicaMjere = repromaterijal.JedinicaMjereName;
                        norm.IdProizvoda = vm.SelectedJelo.IdArtikla;
                        await vm.InsertNorm(norm, vm.SelectedJelo);

                    }
                }
                if (_errorOccurred)
                {
                    _errorOccurred = false;
                    return;
                }
            }
        }

        private async void ListaNormativ_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            var norm = dataGrid.SelectedItem as TblNormativ;

            if (norm != null && DataContext is NormsViewModel vm)
            {


                vm.ErrorOccurred -= ViewModel_ErrorOccurred;
                vm.ErrorOccurred += ViewModel_ErrorOccurred;
                if (vm.SelectedNorm != null)
                {
                    MyInputBox myInput = new MyInputBox();
                    myInput.InputTitle.Text = "IZMJENA NORMATIVA" + Environment.NewLine + Environment.NewLine + "Unesite potrebnu količinu " + norm.Repromaterijal + " u " + norm.JedinicaMjere;
                    myInput.InputTitle.FontSize = 14;
                    myInput.InputText.Text = norm.Kolicina.ToString();
                    myInput.InputText.Focus();
                    myInput.NumbersOnly = true;

                    myInput.ShowDialog();
                    string result = myInput.result;

                    if (!string.IsNullOrEmpty(result) && Convert.ToDecimal(result) != 0)
                    {

                        var normUpdated = new TblNormativ();
                        normUpdated.Cijena = norm.Cijena ?? 0;
                        normUpdated.Repromaterijal = norm.Repromaterijal;
                        normUpdated.Kolicina = Convert.ToDecimal(result);
                        normUpdated.JedinicaMjere = norm.JedinicaMjere;
                        normUpdated.IdProizvoda = vm.SelectedJelo.IdArtikla;
                        normUpdated.IdStavkeNormativa = norm.IdStavkeNormativa;
                        await vm.UpdateNorm(normUpdated, vm.SelectedJelo);

                    }
                }
                else
                {
                    Debug.WriteLine("vm.SelectedNorm == null");
                }
                if (_errorOccurred)
                {
                    _errorOccurred = false;
                    return;
                }
            }
            else
            {
                Debug.WriteLine("ListaNormativ.SelectedItem is NOT TblNormativ norm");
            }
        }

        private T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T)
                    return (T)current;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
        private void Button_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
           
            var button = (Button)sender;
            var dataGridRow = FindAncestor<DataGridRow>(button);

            if (dataGridRow != null && !dataGridRow.IsSelected)
            {
                dataGridRow.IsSelected = true; 
            }
        }
    }
}
