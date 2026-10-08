using Caupo.Helpers;
using Caupo.ViewModels;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Views
{
    public partial class BeverageInPage : UserControl
    {
        public BeverageInPageViewModel ViewModel { get; }
        public bool OpenedFromSupplier { get; set; }
        public UserControl? PreviousPage { get; set; }
        public IRelayCommand ClosePageCommand { get; }

        private readonly int _brojUlaza;
        private bool _initialized;

        public BeverageInPage(int brojulaza = 0)
        {
            InitializeComponent();
            _brojUlaza = brojulaza;
            ViewModel = new BeverageInPageViewModel();
            DataContext = ViewModel;
            ClosePageCommand = new RelayCommand(ClosePage);

            ViewModel.ShowDeletePopupRequested += ShowDeletePopup;
            ViewModel.ConfirmDiscardRequested += ConfirmDiscard;
            ViewModel.AddArticleRequested += ShowAddArticlePopup;
            ViewModel.EditItemRequested += ShowEditItemPopup;
            ViewModel.ErrorOccurred += ViewModel_ErrorOccurred;
            ViewModel.InformationOccurred += ViewModel_InformationOccurred;
            Loaded += BeverageInPage_Loaded;
        }

        private async void BeverageInPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_initialized) return;
            _initialized = true;
            await ViewModel.InitializeAsync(_brojUlaza);
        }

        private (bool Accepted, decimal Price, decimal Quantity, decimal Discount) ShowAddArticlePopup(TblArtikli artikl)
        {
            MainContent.Effect = new BlurEffect { Radius = 5 };
            try
            {
                var popup = new BeverageInPopup(artikl)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                if (popup.ShowDialog() != true) return (false, 0, 0, 0);
                return (true, popup.EnteredPrice, popup.EnteredQuantity, popup.EnteredDiscount);
            }
            finally { MainContent.Effect = null; }
        }

        private (bool Accepted, decimal Price, decimal Quantity, decimal Discount) ShowEditItemPopup(TblUlazStavke stavka)
        {
            MainContent.Effect = new BlurEffect { Radius = 5 };
            try
            {
                var popup = new BeverageInPopup(stavka)
                {
                    IsUpdate = true,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                if (popup.ShowDialog() != true) return (false, 0, 0, 0);
                return (true, popup.EnteredPrice, popup.EnteredQuantity, popup.EnteredDiscount);
            }
            finally { MainContent.Effect = null; }
        }

        private bool ShowDeletePopup(string itemName) =>
            ShowConfirmation("POTVRDA BRISANJA", $"Da li ste sigurni da želite ukloniti stavku:\n{itemName}?");

        private bool ConfirmDiscard(string message) =>
            ShowConfirmation("NESPREMLJENE PROMJENE", message);

        private bool ShowConfirmation(string title, string message)
        {
            MainContent.Effect = new BlurEffect { Radius = 5 };
            try
            {
                var popup = new YesNoPopup
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                popup.MessageTitle.Text = title;
                popup.MessageText.Text = message;
                popup.ShowDialog();
                return popup.Kliknuo == "Da";
            }
            finally { MainContent.Effect = null; }
        }

        private void ViewModel_ErrorOccurred(object? sender, string? message) =>
            ShowMessage("GREŠKA", message ?? "Došlo je do greške.");

        private void ViewModel_InformationOccurred(object? sender, string? message)
        {
            if (!string.IsNullOrWhiteSpace(message)) ShowMessage("INFORMACIJA", message);
        }

        private void ShowMessage(string title, string message)
        {
            MainContent.Effect = new BlurEffect { Radius = 5 };
            try
            {
                var popup = new MyMessageBox
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                popup.MessageTitle.Text = title;
                popup.MessageText.Text = message;
                popup.ShowDialog();
            }
            finally { MainContent.Effect = null; }
        }

        private void ClosePage()
        {
            if (ViewModel.IsBusy) return;
            if (ViewModel.HasUnsavedChanges &&
                !ShowConfirmation("NESPREMLJENE PROMJENE", "Postoje nespremljene promjene.\nŽelite zatvoriti stranicu bez spremanja?"))
                return;

            if (OpenedFromSupplier && PreviousPage != null)
            {
                PageNavigator.NavigateWithFade(PreviousPage);
                return;
            }
            var page = new HomePage { DataContext = new HomeViewModel() };
            PageNavigator.NavigateWithFade(page);
        }

        private void Artikl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGridRow { Item: TblArtikli artikl }) return;
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
            if (sender is not DataGridRow { Item: TblUlazStavke stavka }) return;
            ViewModel.SelectedStockInItem = stavka;
            if (ViewModel.EditStockInItemCommand.CanExecute(null))
                ViewModel.EditStockInItemCommand.Execute(null);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => ClosePage();

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_initialized && !ViewModel.IsBusy)
                StockInSearchPopup.IsOpen = true;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_initialized && !ViewModel.IsBusy && SearchTextBox.IsKeyboardFocusWithin)
                StockInSearchPopup.IsOpen = true;
        }

        private async void StockInSearchResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StockInSearchResults.SelectedItem is not TblUlaz ulaz) return;
            StockInSearchPopup.IsOpen = false;
            StockInSearchResults.SelectedItem = null;
            await ViewModel.SelectStockInAsync(ulaz);
        }
    }
}
