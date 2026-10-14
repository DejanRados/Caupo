
using Caupo.Data;
using Caupo.Helpers;
using Caupo.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Views
{
    public partial class CategoriesPage : UserControl
    {
        private CategoriesViewModel ViewModel =>
            (CategoriesViewModel)DataContext;

        public CategoriesPage()
        {
            InitializeComponent();

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        // ============================================================
        // PRETRAGA KATEGORIJA
        // ============================================================


        private void CategorySearchBar_SuggestionSelected(
            object? sender,
            Caupo.UserControls.SearchSuggestionSelectedEventArgs e)
        {
            if (e.Suggestion.Item is not TblKategorije kategorija)
                return;

            ViewModel.SelectedKategorija = kategorija;

            CategorySearchBar.SearchText = string.Empty;
            ViewModel.SearchText = string.Empty;

            if (ListaKategorija.Items.Contains(kategorija))
                ListaKategorija.ScrollIntoView(kategorija);
        }



        private void ViewModel_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CategoriesViewModel.SelectedKategorija))
            {
                if (ViewModel.SelectedKategorija != null &&
                    ListaKategorija.Items.Contains(ViewModel.SelectedKategorija))
                {
                    ListaKategorija.ScrollIntoView(
                        ViewModel.SelectedKategorija);
                }
            }
        }



        // ============================================================
        // ZATVARANJE STRANICE
        // ============================================================

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            var page = new ArticlesPage
            {
                DataContext = new ArticlesViewModel()
            };

            PageNavigator.NavigateWithFade(page);
        }
    }
}
