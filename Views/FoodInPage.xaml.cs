using Caupo.Data;
using Caupo.Helpers;
using Caupo.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Views
{

    public partial class FoodInPage : UserControl
    {
        public FoodInViewModel ViewModel { get; set; }
        public bool OpenedFromSupplier { get; set; } = false;
        public UserControl PreviousPage { get; set; }
        public FoodInPage(int brojulaza = 0)
        {
            InitializeComponent ();
            ViewModel = new FoodInViewModel ();
            this.DataContext = ViewModel;
            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik.Radnik;
            _ = LoadInitialStockAsync (brojulaza);

            if(DataContext is FoodInViewModel vm)
            {
                vm.ShowDeletePopupRequested += ShowDeletePopup;
            }
        }

        // metoda koja zapravo pokazuje popup i primjenjuje blur
        private bool ShowDeletePopup(string itemName)
        {
            // Blur cijelog sadržaja
            MainContent.Effect = new BlurEffect { Radius = 5 };

            var myMessageBox = new YesNoPopup
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            myMessageBox.MessageTitle.Text = "POTVRDA BRISANJA";
            myMessageBox.MessageText.Text = $"Da li ste sigurni da želite obrisati stavku:\n{itemName}?";

            bool? result = myMessageBox.ShowDialog ();

            // Ukloni blur
            MainContent.Effect = null;

            return myMessageBox.Kliknuo == "Da";
        }

        private async Task LoadInitialStockAsync(int brojUlaza)
        {
            await ViewModel.LoadStockInAsync (brojUlaza);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {

            Window_Closing (null, null);
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
            var selectedItem = ListaArikala.SelectedItem as DatabaseTables.TblRepromaterijal;
            if(selectedItem != null)
            {
                if(DataContext is FoodInViewModel viewModel)
                {
                    viewModel.SelectedArticle = selectedItem;
                    // viewModel.LoadReceiptItems(selectedItem);
                    SearchTextBox.Text = "";
                    ListaArikala.ScrollIntoView (ListaArikala.SelectedItem);
                }
            }
        }


        private async void BtnFirst_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine (" BtnFirst_Click DataContext" + DataContext.ToString ());
            if(DataContext is FoodInViewModel viewModel)
            {
                int index = viewModel.StockInFilter.IndexOf (viewModel.SelectedStockIn);
                Debug.WriteLine ("IndexOf SelectedStockIn: " + viewModel.StockInFilter.IndexOf (viewModel.SelectedStockIn));
                Debug.WriteLine ("StockInFilter count: " + viewModel.StockInFilter.Count);
                if(index > 0)
                {


                    viewModel.SelectedStockIn = viewModel.StockInFilter[index - 1];
                    await viewModel.LoadStockInItems (viewModel.SelectedStockIn);
                }

            }
            else
            {
                Debug.WriteLine (" BtnFirst_Click DataContext ELSE" + DataContext.ToString ());
            }
        }

        private async void BtnLast_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine (" BtnLast_Click DataContext" + DataContext.ToString ());
            if(DataContext is FoodInViewModel viewModel)
            {
                int index = viewModel.StockInFilter.IndexOf (viewModel.SelectedStockIn);
                if(index < viewModel.StockInFilter.Count - 1)
                {
                    viewModel.SelectedStockIn = viewModel.StockInFilter[index + 1];
                    await viewModel.LoadStockInItems (viewModel.SelectedStockIn);
                }

            }
            else
            {
                Debug.WriteLine (" BtnLast_Click DataContext ELSE" + DataContext.ToString ());
            }
        }


        private void ListaArikala_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(ListaArikala.SelectedItem is TblRepromaterijal artikl)
            {

                var vm = DataContext as FoodInViewModel;
                if(vm.StockIn.Count == 0)
                    return;

                vm.ErrorOccurred -= ViewModel_ErrorOccurred;
                vm.ErrorOccurred += ViewModel_ErrorOccurred;
                if(vm.SelectedArticle != null)
                {
                    var win = new FoodInPopup (vm.SelectedArticle);
                    MainContent.Effect = new BlurEffect { Radius = 5 };
                    if(win.ShowDialog () == true)
                    {
                        Debug.WriteLine ("Dobijam nazad, cijena: " + win.EnteredPrice + ", kolićina: " + win.EnteredQuantity + " i popust: " + win.EnteredDiscount);
                        vm.EnteredPrice = win.EnteredPrice;
                        vm.EnteredQuantity = win.EnteredQuantity;
                        vm.EnteredDiscount = win.EnteredDiscount;

                        vm.ProcessArticle ();
                    }
                    MainContent.Effect = null;
                }
            }
            if(_errorOccurred)
            {
                _errorOccurred = false;
                return;
            }
        }

        private async void ListaStavki_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(ListaStavki.SelectedItem is TblUlazRepromaterijalStavka stavka)
            {
                Debug.WriteLine ("Selektovana stavka na stranici u listi: " + stavka.Artikl);
                var vm = DataContext as FoodInViewModel;
                Debug.WriteLine ("Selektovana stavka na viewmodelu: " + vm.SelectedStockInItem.Artikl);
                vm.ErrorOccurred -= ViewModel_ErrorOccurred;
                vm.ErrorOccurred += ViewModel_ErrorOccurred;
                if(stavka != null)
                {
                    var win = new FoodInPopup (stavka);
                    MainContent.Effect = new BlurEffect { Radius = 5 };
                    win.IsUpdate = true;
                    if(win.ShowDialog () == true)
                    {
                        Debug.WriteLine ("Dobijam nazad, cijena: " + win.EnteredPrice + ", kolićina: " + win.EnteredQuantity + " i popust: " + win.EnteredDiscount);
                        vm.EnteredPrice = win.EnteredPrice;
                        vm.EnteredQuantity = win.EnteredQuantity;
                        vm.EnteredDiscount = win.EnteredDiscount;

                        await vm.UpdateStockInItem ();


                    }
                    MainContent.Effect = null;
                    win.IsUpdate = false;
                }
            }
            if(_errorOccurred)
            {
                _errorOccurred = false;
                return;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(DataContext is FoodInViewModel viewModel && viewModel.HasUnsavedChanges)
            {
                var myMessageBox = new YesNoPopup ();
                myMessageBox.MessageTitle.Text = "UPOZORENJE";
                myMessageBox.MessageText.Text = "Postoje nesnimljene promjene koje ste napravili.\nŽelite zatvoriti stranicu?";
                myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                bool? result = myMessageBox.ShowDialog ();

                if(myMessageBox.Kliknuo != "Da")
                {
                    return;
                }
                else
                {
                    if(!OpenedFromSupplier)
                    {
                        var page = new HomePage ();
                        page.DataContext = new HomeViewModel ();
                        PageNavigator.NavigateWithFade (page);
                    }
                }
            }
            else
            {
                if(!OpenedFromSupplier)
                {
                    var page = new HomePage ();
                    page.DataContext = new HomeViewModel ();
                    PageNavigator.NavigateWithFade (page);
                }
            }



        }
        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is FoodInViewModel viewModel)
            {
                viewModel.PrintKalkulacija (viewModel.StockInItems, viewModel.Klijent, viewModel.SelectedStockIn, viewModel.SelectedSupplier);
            }
        }

    }
}

