
using Caupo.Helpers;
using Caupo.ViewModels;
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
        public FoodInViewModel ViewModel { get; }
        public bool OpenedFromSupplier { get; set; }
        public UserControl? PreviousPage { get; set; }

        private readonly int _brojUlaza;
        private bool _initialized;

        public FoodInPage(int brojulaza = 0)
        {
            InitializeComponent();
            _brojUlaza = brojulaza;
            ViewModel = new FoodInViewModel();
            DataContext = ViewModel;

            ViewModel.ShowDeletePopupRequested += ShowDeletePopup;
            ViewModel.ConfirmDiscardRequested += ConfirmDiscard;
            ViewModel.AddArticleRequested += ShowAddArticlePopup;
            ViewModel.EditItemRequested += ShowEditItemPopup;
            ViewModel.ErrorOccurred += ViewModel_ErrorOccurred;
            ViewModel.InformationOccurred += ViewModel_InformationOccurred;

            Loaded += FoodInPage_Loaded;
        }

        private async void FoodInPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_initialized) return;
            _initialized = true;
            await ViewModel.InitializeAsync(_brojUlaza);
        }

        private async void StockInSearchBar_SuggestionSelected(object? sender,
            Caupo.UserControls.SearchSuggestionSelectedEventArgs e)
        {
            if (e.Suggestion.Item is not TblUlazRepromaterijal ulaz) return;

            await ViewModel.SelectStockInAsync(ulaz);
            if (ViewModel.SelectedStockIn?.BrojUlaza != ulaz.BrojUlaza) return;

            StockInSearchBar.SearchText = string.Empty;
            ViewModel.SearchText = string.Empty;
        }

        private bool ShowDeletePopup(string itemName)
        {
            return ShowConfirmation("POTVRDA BRISANJA",
                $"Da li ste sigurni da želite obrisati stavku:\n{itemName}?");
        }

        private bool ConfirmDiscard(string message)
        {
            return ShowConfirmation("UPOZORENJE", message);
        }

        private bool ShowConfirmation(string title, string message)
        {
            MainContent.Effect = new BlurEffect { Radius = 5 };
            try
            {
                var popup = new YesNoPopup { WindowStartupLocation = WindowStartupLocation.CenterScreen };
                popup.MessageTitle.Text = title;
                popup.MessageText.Text = message;
                popup.ShowDialog();
                return popup.Kliknuo == "Da";
            }
            finally { MainContent.Effect = null; }
        }

        private (bool Accepted, decimal Price, decimal Quantity, decimal Discount)
            ShowAddArticlePopup(TblRepromaterijal article)
        {
            var popup = new FoodInPopup(article);
            MainContent.Effect = new BlurEffect { Radius = 5 };
            try
            {
                if (popup.ShowDialog() != true) return (false, 0, 0, 0);
                return (true, popup.EnteredPrice, popup.EnteredQuantity, popup.EnteredDiscount);
            }
            finally { MainContent.Effect = null; }
        }

        private (bool Accepted, decimal Price, decimal Quantity, decimal Discount)
            ShowEditItemPopup(TblUlazRepromaterijalStavka item)
        {
            var popup = new FoodInPopup(item) { IsUpdate = true };
            MainContent.Effect = new BlurEffect { Radius = 5 };
            try
            {
                if (popup.ShowDialog() != true) return (false, 0, 0, 0);
                return (true, popup.EnteredPrice, popup.EnteredQuantity, popup.EnteredDiscount);
            }
            finally
            {
                popup.IsUpdate = false;
                MainContent.Effect = null;
            }
        }

        private void ViewModel_ErrorOccurred(object? sender, string? message)
        {
            ShowMessage("GREŠKA", message);
        }

        private void ViewModel_InformationOccurred(object? sender, string? message)
        {
            ShowMessage("INFORMACIJA", message);
        }

        private void ShowMessage(string title, string? message)
        {
            MainContent.Effect = new BlurEffect { Radius = 5 };
            try
            {
                var popup = new MyMessageBox { WindowStartupLocation = WindowStartupLocation.CenterScreen };
                popup.MessageTitle.Text = title;
                popup.MessageText.Text = message ?? string.Empty;
                popup.ShowDialog();
            }
            finally { MainContent.Effect = null; }
        }

        private void Artikl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGridRow { Item: TblRepromaterijal artikl }) return;
            ViewModel.SelectedArticle = artikl;
            if (ViewModel.AddArticleCommand.CanExecute(null))
                ViewModel.AddArticleCommand.Execute(null);
        }

        private void Stavka_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DependencyObject? source = e.OriginalSource as DependencyObject;
            while (source != null)
            {
                if (source is Button) return;
                if (source is DataGridRow) break;
                source = VisualTreeHelper.GetParent(source);
            }

            if (sender is not DataGridRow { Item: TblUlazRepromaterijalStavka stavka }) return;
            ViewModel.SelectedStockInItem = stavka;
            if (ViewModel.EditStockInItemCommand.CanExecute(null))
                ViewModel.EditStockInItemCommand.Execute(null);
        }

        private void ListaNamirnica_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListaArikala.SelectedItem is not TblRepromaterijal article) return;
            ViewModel.SelectedArticle = article;
            ListaArikala.ScrollIntoView(article);
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.PrintStockInCommand.CanExecute(null))
                ViewModel.PrintStockInCommand.Execute(null);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.HasUnsavedChanges &&
                !ShowConfirmation("UPOZORENJE",
                    "Postoje nespremljene promjene.\nŽelite zatvoriti stranicu bez spremanja?"))
                return;

            if (OpenedFromSupplier && PreviousPage != null)
            {
                PageNavigator.NavigateWithFade(PreviousPage);
                return;
            }

            if (OpenedFromSupplier) return;

            PageNavigator.NavigateWithFade(new HomePage { DataContext = new HomeViewModel() });
        }
    }
}
