using Caupo.Helpers;
using Caupo.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Effects;
using static Caupo.Data.DatabaseTables;
using static Caupo.ViewModels.SuppliersViewModel;

namespace Caupo.Views
{

    public partial class SuppliersPage : UserControl
    {
        private SuppliersViewModel viewModel;
        public SuppliersPage()
        {


            InitializeComponent();
            viewModel = new SuppliersViewModel();
            DataContext = viewModel;

            // --- OVDJE DODAŠ LISTENER ---
            viewModel.PropertyChanged += ViewModel_PropertyChanged;


            if (DataContext is SuppliersViewModel vm)
            {
                vm.OpenSupplierPopupRequested += OpenSupplierPopup;
            }
        }



        private void ApplyBlur()
        {
            MainContent.Effect = new BlurEffect { Radius = 8 };
        }

        private void RemoveBlur()
        {
            MainContent.Effect = null;
        }


        private TblDobavljaci? OpenSupplierPopup(TblDobavljaci? supplier)
        {
            ApplyBlur();

            SupplierPopup popup =
                supplier == null
                    ? new SupplierPopup()
                    : new SupplierPopup(supplier);

            var result = popup.Open(); // ShowDialog

            RemoveBlur();

            return result;
        }




        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine("Trigerovan  private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)");
            if (e.PropertyName == "NewSupplier")
            {
                Debug.WriteLine("(e.PropertyName == NewSupplier");
                if (viewModel.NewSupplier != null)
                {

                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ListaDobavljaca.SelectedItem = viewModel.NewSupplier;
                        ListaDobavljaca.ScrollIntoView(viewModel.NewSupplier);
                    }, System.Windows.Threading.DispatcherPriority.Background);
                    viewModel.FilterItems("");
                }
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var page = new HomePage();
            page.DataContext = new HomeViewModel();
            PageNavigator.NavigateWithFade(page);
        }



        private async void ListaDobavljaca_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = ListaDobavljaca.SelectedItem as TblDobavljaci;
            if (selectedItem != null)
            {
                if (DataContext is SuppliersViewModel viewModel)
                {
                    viewModel.SelectedSupplier = selectedItem;
                    await viewModel.LoadStockInSupplier(selectedItem);
                    viewModel.SearchText = "";
                    ListaDobavljaca.ScrollIntoView(ListaDobavljaca.SelectedItem);
                }
            }
        }


        private void SupplierSearchBar_SuggestionSelected(object? sender, Caupo.UserControls.SearchSuggestionSelectedEventArgs e)
        {
            if (e.Suggestion.Item is not TblDobavljaci supplier)
                return;

            if (DataContext is not SuppliersViewModel vm)
                return;

            vm.SelectedSupplier = supplier;

            SupplierSearchBar.SearchText = string.Empty;
            vm.SearchText = string.Empty;

            if (ListaDobavljaca.Items.Contains(supplier))
                ListaDobavljaca.ScrollIntoView(supplier);
        }


        private void ListaUlaza_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListaUlaza.SelectedItem is StockInList ulaz)
            {
                OpenStockInPage(ulaz);
            }
        }




        private async void OpenStockInPage(StockInList ulaz)
        {
            //Window pageToOpen;
            if (DataContext is SuppliersViewModel viewModel)
            {
                if (ulaz.Tip == "Piće")
                {
                    if (viewModel.SelectedStockIn == null)
                        return;

                    var beveragepage = new BeverageInPage(viewModel.SelectedStockIn.BrojUlaza);
                    Debug.WriteLine("Otvara stranicu sa selektovanim ulazom broj: " + viewModel.SelectedStockIn.BrojUlaza);
                    beveragepage.OpenedFromSupplier = true;
                    beveragepage.PreviousPage = this;
                    PageNavigator.Navigate?.Invoke(beveragepage);
                }
                else if (ulaz.Tip == "Namirnice")
                {
                    var foodpage = new FoodInPage(viewModel.SelectedStockIn.BrojUlaza);
                    foodpage.OpenedFromSupplier = true;
                    foodpage.PreviousPage = this;
                    PageNavigator.Navigate?.Invoke(foodpage);
                }
                else
                {
                    return; // nepoznat tip
                }

            }
        }


    }

}
