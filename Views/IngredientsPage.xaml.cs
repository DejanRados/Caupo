
using Caupo.Helpers;
using Caupo.UserControls;
using Caupo.ViewModels;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Effects;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Views
{
    public partial class IngredientsPage : UserControl
    {
        private bool _initialized;
        private bool _saving;

        private IngredientsViewModel ViewModel => (IngredientsViewModel)DataContext;

        public IngredientsPage()
        {
            DataContext = new IngredientsViewModel();
            InitializeComponent();

            Loaded += IngredientsPage_Loaded;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            IngredientEditGrid.IsVisibleChanged += (_, _) => UpdateOverlayBlur();
        }

        private async void IngredientsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_initialized)
                return;

            _initialized = true;

            try
            {
                await ViewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                _initialized = false;
                Debug.WriteLine("[NAMIRNICE] Inicijalizacija: " + ex);
                ShowMessage("GREŠKA", $"Učitavanje namirnica nije uspjelo: {ex.Message}");
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(IngredientsViewModel.SelectedIngredient))
                return;

            var ingredient = ViewModel.SelectedIngredient;
            if (ingredient != null && ListaNamirnica.Items.Contains(ingredient))
                ListaNamirnica.ScrollIntoView(ingredient);
        }

        private void IngredientSearchBar_SuggestionSelected(object? sender, SearchSuggestionSelectedEventArgs e)
        {
            if (e.Suggestion.Item is not TblRepromaterijal ingredient)
                return;

            ViewModel.SearchText = string.Empty;
            ViewModel.SelectedIngredient = ViewModel.IngredientFilter
                .FirstOrDefault(x => x.IdRepromaterijala == ingredient.IdRepromaterijala);

            if (ViewModel.SelectedIngredient != null)
                ListaNamirnica.ScrollIntoView(ViewModel.SelectedIngredient);
        }

        private void ListaNamirnica_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListaNamirnica.SelectedItem is TblRepromaterijal ingredient)
                ListaNamirnica.ScrollIntoView(ingredient);
        }

        private void ListaNamirnica_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject source)
                return;

            while (source != null && source is not DataGridRow)
                source = System.Windows.Media.VisualTreeHelper.GetParent(source);

            if (source is DataGridRow row && !row.IsSelected)
            {
                row.IsSelected = true;
                ListaNamirnica.SelectedItem = row.Item;
                ListaNamirnica.CurrentItem = row.Item;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var page = new HomePage { DataContext = new HomeViewModel() };
            PageNavigator.NavigateWithFade(page);
        }

        private void UpdateOverlayBlur()
        {
            MainContent.Effect = IngredientEditGrid.Visibility == Visibility.Visible
                ? new BlurEffect { Radius = 8 }
                : null;
        }

        private void OpenEditor()
        {
            IngredientEditGrid.Visibility = Visibility.Visible;
            Keyboard.Focus(EditorNameTextBox);
            EditorNameTextBox.Focus();
            EditorNameTextBox.SelectAll();
        }

        private void CloseEditor()
        {
            IngredientEditGrid.Visibility = Visibility.Collapsed;
            ViewModel.CancelEdit();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsBusy)
                return;

            ViewModel.BeginAdd();
            OpenEditor();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsBusy || !ViewModel.BeginEdit())
                return;

            OpenEditor();
        }

        private void BtnIngredientEditCancel_Click(object sender, RoutedEventArgs e)
        {
            if (!_saving)
                CloseEditor();
        }

        private async void BtnIngredientEditSave_Click(object sender, RoutedEventArgs e)
        {
            if (_saving)
                return;

            _saving = true;
            BtnIngredientEditSave.IsEnabled = false;
            BtnIngredientEditCancel.IsEnabled = false;

            try
            {
                bool editing = ViewModel.IsEditing;
                string? error = await ViewModel.SaveEditorAsync();

                if (error != null)
                {
                    ShowMessage("GREŠKA", error);
                    return;
                }

                CloseEditor();
                ShowMessage("POTVRDA", editing
                    ? "Namirnica je uspješno ažurirana."
                    : "Namirnica je uspješno dodana.");
            }
            finally
            {
                _saving = false;
                BtnIngredientEditSave.IsEnabled = true;
                BtnIngredientEditCancel.IsEnabled = true;
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsBusy || ViewModel.SelectedIngredient == null)
                return;

            string name = ViewModel.SelectedIngredient.Repromaterijal ?? string.Empty;

            var confirmation = new YesNoPopup
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            confirmation.MessageTitle.Text = "POTVRDA BRISANJA";
            confirmation.MessageText.Text = $"Da li ste sigurni da želite obrisati namirnicu:{Environment.NewLine}{name}?";
            confirmation.ShowDialog();

            if (confirmation.Kliknuo != "Da")
                return;

            string? error = await ViewModel.DeleteSelectedAsync();
            ShowMessage(error == null ? "POTVRDA" : "GREŠKA",
                error ?? $"Namirnica '{name}' je uspješno obrisana.");
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel fajl (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                FileName = "Namirnice.xlsx"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                ViewModel.ExportExcel(dialog.FileName);
                ShowMessage("IZVOZ U EXCEL", "Izvoz namirnica je uspješno završen.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[NAMIRNICE] Izvoz: " + ex);
                ShowMessage("GREŠKA", $"Izvoz nije uspio: {ex.Message}");
            }
        }

        private async void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsBusy)
                return;

            var dialog = new OpenFileDialog
            {
                Filter = "Excel fajl (*.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() != true)
                return;

            var result = await ViewModel.ImportExcelAsync(dialog.FileName);

            ShowMessage(result.Error == null ? "UVOZ EXCEL" : "GREŠKA",
                result.Error ?? $"Uvoz je uspješno završen. Dodano namirnica: {result.Count}.");
        }

        private static void ShowMessage(string title, string message)
        {
            var dialog = new MyMessageBox
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            dialog.MessageTitle.Text = title;
            dialog.MessageText.Text = message;
            dialog.ShowDialog();
        }
    }
}
