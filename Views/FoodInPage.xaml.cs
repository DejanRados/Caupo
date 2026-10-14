
using Caupo.Data;
using Caupo.Helpers;
using Caupo.ViewModels;
using System.ComponentModel;
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

        private bool _errorOccurred = false;
        private bool _initialized = false;

        public FoodInPage(int brojulaza = 0)
        {
            InitializeComponent();

            ViewModel = new FoodInViewModel();
            DataContext = ViewModel;

            ViewModel.ShowDeletePopupRequested += ShowDeletePopup;
           

            _ = LoadInitialStockAsync(brojulaza);
        }

        // ============================================================
        // UČITAVANJE ULAZA
        // ============================================================

        private async Task LoadInitialStockAsync(int brojUlaza)
        {
            await ViewModel.LoadStockInAsync(brojUlaza);
            _initialized = true;
        }

        // ============================================================
        // PRETRAGA ULAZA
        // ============================================================


        private async void StockInSearchBar_SuggestionSelected(
            object? sender,
            Caupo.UserControls.SearchSuggestionSelectedEventArgs e)
        {
            if (e.Suggestion.Item is not TblUlazRepromaterijal ulaz)
                return;

            ViewModel.SelectedStockIn = ulaz;
            await ViewModel.LoadStockInItems(ulaz);

            if (ReferenceEquals(ViewModel.SelectedStockIn, ulaz))
            {
                StockInSearchBar.SearchText = string.Empty;
                ViewModel.SearchText = string.Empty;
            }
        }


        // ============================================================
        // POTVRDA BRISANJA STAVKE
        // ============================================================

        private bool ShowDeletePopup(string itemName)
        {
            MainContent.Effect = new BlurEffect { Radius = 5 };

            try
            {
                var myMessageBox = new YesNoPopup
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                myMessageBox.MessageTitle.Text = "POTVRDA BRISANJA";
                myMessageBox.MessageText.Text =
                    $"Da li ste sigurni da želite obrisati stavku:\n{itemName}?";

                myMessageBox.ShowDialog();

                return myMessageBox.Kliknuo == "Da";
            }
            finally
            {
                MainContent.Effect = null;
            }
        }

        // ============================================================
        // PRIKAZ GREŠKE
        // ============================================================

        private void ViewModel_ErrorOccurred(
            object? sender,
            string? errorMessage)
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

        // ============================================================
        // BOJA BORDERA TEXTBOX KONTROLA
        // ============================================================

        private void ChangeTextBoxBorderBrush(
            DependencyObject parent,
            Brush? brush)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is TextBox textBox)
                {
                    if (textBox.Name != "SearchTextBox")
                    {
                        textBox.BorderThickness = new Thickness(0, 0, 0, 1);
                        textBox.BorderBrush = brush;
                    }
                }

                ChangeTextBoxBorderBrush(child, brush);
            }
        }

        // ============================================================
        // ODABIR REPROMATERIJALA
        // ============================================================

        private void ListaNamirnica_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (ListaArikala.SelectedItem is TblRepromaterijal selectedItem)
            {
                ViewModel.SelectedArticle = selectedItem;

                ListaArikala.ScrollIntoView(selectedItem);
            }
        }

        // ============================================================
        // DODAVANJE REPROMATERIJALA U ULAZ
        // ============================================================

        private void ListaArikala_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            if (ListaArikala.SelectedItem is not TblRepromaterijal artikl)
                return;

            var vm = ViewModel;

            if (vm.StockIn.Count == 0)
                return;

            vm.ErrorOccurred -= ViewModel_ErrorOccurred;
            vm.ErrorOccurred += ViewModel_ErrorOccurred;

            if (vm.SelectedArticle == null)
                return;

            var win = new FoodInPopup(vm.SelectedArticle);

            MainContent.Effect = new BlurEffect { Radius = 5 };

            try
            {
                if (win.ShowDialog() == true)
                {
                    Debug.WriteLine(
                        "Dobijam nazad, cijena: " + win.EnteredPrice +
                        ", količina: " + win.EnteredQuantity +
                        " i popust: " + win.EnteredDiscount);

                    vm.EnteredPrice = win.EnteredPrice;
                    vm.EnteredQuantity = win.EnteredQuantity;
                    vm.EnteredDiscount = win.EnteredDiscount;

                    vm.ProcessArticle();
                }
            }
            finally
            {
                MainContent.Effect = null;
            }

            if (_errorOccurred)
            {
                _errorOccurred = false;
                return;
            }
        }

        // ============================================================
        // IZMJENA POSTOJEĆE STAVKE ULAZA
        // ============================================================

        private async void ListaStavki_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            if (ListaStavki.SelectedItem is not TblUlazRepromaterijalStavka stavka)
                return;

            var vm = ViewModel;

            Debug.WriteLine(
                "Selektovana stavka na stranici u listi: " + stavka.Artikl);

            if (vm.SelectedStockInItem != null)
            {
                Debug.WriteLine(
                    "Selektovana stavka na ViewModelu: " +
                    vm.SelectedStockInItem.Artikl);
            }

            vm.ErrorOccurred -= ViewModel_ErrorOccurred;
            vm.ErrorOccurred += ViewModel_ErrorOccurred;

            var win = new FoodInPopup(stavka)
            {
                IsUpdate = true
            };

            MainContent.Effect = new BlurEffect { Radius = 5 };

            try
            {
                if (win.ShowDialog() == true)
                {
                    Debug.WriteLine(
                        "Dobijam nazad, cijena: " + win.EnteredPrice +
                        ", količina: " + win.EnteredQuantity +
                        " i popust: " + win.EnteredDiscount);

                    vm.EnteredPrice = win.EnteredPrice;
                    vm.EnteredQuantity = win.EnteredQuantity;
                    vm.EnteredDiscount = win.EnteredDiscount;

                    await vm.UpdateStockInItem();
                }
            }
            finally
            {
                MainContent.Effect = null;
                win.IsUpdate = false;
            }

            if (_errorOccurred)
            {
                _errorOccurred = false;
                return;
            }
        }

        // ============================================================
        // ZATVARANJE STRANICE
        // ============================================================

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Window_Closing(null, null);
        }

        private void Window_Closing(
            object sender,
            CancelEventArgs e)
        {
            if (ViewModel.HasUnsavedChanges)
            {
                var myMessageBox = new YesNoPopup
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                myMessageBox.MessageTitle.Text = "UPOZORENJE";
                myMessageBox.MessageText.Text =
                    "Postoje nesnimljene promjene koje ste napravili.\n" +
                    "Želite zatvoriti stranicu?";

                myMessageBox.ShowDialog();

                if (myMessageBox.Kliknuo != "Da")
                    return;
            }

            if (!OpenedFromSupplier)
            {
                var page = new HomePage
                {
                    DataContext = new HomeViewModel()
                };

                PageNavigator.NavigateWithFade(page);
            }
        }

        // ============================================================
        // ŠTAMPANJE KALKULACIJE
        // ============================================================

        private void BtnPrint_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (ViewModel.SelectedStockIn == null)
                return;

            if (ViewModel.SelectedSupplier == null)
                return;

            ViewModel.PrintKalkulacija(
                ViewModel.StockInItems,
                ViewModel.Klijent,
                ViewModel.SelectedStockIn,
                ViewModel.SelectedSupplier);
        }
    }
}
