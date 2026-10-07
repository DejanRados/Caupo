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
    public partial class CocktailNormsPage : UserControl
    {
        private readonly CocktailNormsViewModel _viewModel;
        private readonly TblArtikli? _initialCocktail;

        private readonly bool _returnToArticles;
        public CocktailNormsPage(TblArtikli? initialCocktail = null, bool returnToArticles = false)
        {
            InitializeComponent();

            _initialCocktail = initialCocktail;
            _returnToArticles = returnToArticles;

            _viewModel = new CocktailNormsViewModel();
            DataContext = _viewModel;

            _viewModel.ErrorOccurred += ViewModel_ErrorOccurred;
            Loaded += CocktailNormsPage_Loaded;

           
        }

        private async void CocktailNormsPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= CocktailNormsPage_Loaded;

            await _viewModel.InitializeAsync();

            if (_initialCocktail != null)
            {
                var cocktail = _viewModel.Pica.FirstOrDefault(x => x.IdArtikla == _initialCocktail.IdArtikla);

                if (cocktail != null)
                    await _viewModel.LoadNormativ(cocktail);
            }
        }


        private void KeyboardButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.ShowKeyboard();
        }


        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not TextBox textBox)
                    return;

                textBox.SelectAll();

                MainWindow.Instance.ShowKeyboard();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("TextBox_GotFocus salje: " + ex);
            }
        }


        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.HideKeyboard();
        }


        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_returnToArticles)
            {
                var page = new ArticlesPage();
                page.DataContext = new ArticlesViewModel();
                PageNavigator.NavigateWithFade(page);
                return;
            }

            var homePage = new HomePage();
            homePage.DataContext = new HomeViewModel();
            PageNavigator.NavigateWithFade(homePage);
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
            if (ListaNamirnica.SelectedItem is not DatabaseTables.TblArtikli selectedItem)
                return;

            _viewModel.SelectedIngredient = selectedItem;
            ListaNamirnica.ScrollIntoView(selectedItem);
        }


        Brush? _BorderBrush;


        private async void BtnFirst_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedPice == null)
                return;

            int index = _viewModel.Pica.IndexOf(_viewModel.SelectedPice);

            if (index <= 0)
                return;

            _viewModel.SelectedPice = _viewModel.Pica[index - 1];
            await _viewModel.LoadNormativ(_viewModel.SelectedPice);
        }


        private async void BtnLast_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedPice == null)
                return;

            int index = _viewModel.Pica.IndexOf(_viewModel.SelectedPice);

            if (index < 0 || index >= _viewModel.Pica.Count - 1)
                return;

            _viewModel.SelectedPice = _viewModel.Pica[index + 1];
            await _viewModel.LoadNormativ(_viewModel.SelectedPice);
        }


        int IdPica = 0;


        private async void lstSuggestions_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (sender is ListBox lb &&
               lb.SelectedItem is TblArtikli selectedArtikl)
            {
                IdPica = selectedArtikl.IdArtikla;

                _viewModel.SearchText = "";
                _viewModel.Suggestions.Clear();

                await _viewModel.LoadNormativ(selectedArtikl);

                lb.SelectedItem = null;
            }
        }


        private async void ListaNamirnica_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            var ingredient =
                dataGrid.SelectedItem as TblArtikli;

            if (ingredient != null &&
               DataContext is CocktailNormsViewModel vm)
            {
                if (vm.SelectedPice != null)
                {
                    MainContent.Effect =
                        new BlurEffect { Radius = 8 };

                    MyInputBox myInput =
                        new MyInputBox();

                    myInput.InputTitle.Text =
                        "NOVI NORMATIV" +
                        Environment.NewLine +
                        Environment.NewLine +
                        "Unesite potrebnu količinu " +
                        ingredient.Artikl +
                        " u " +
                        ingredient.JedinicaMjereName;

                    myInput.InputTitle.FontSize = 14;
                    myInput.NumbersOnly = true;

                    myInput.ShowDialog();

                    string result = myInput.result;

                    if (!string.IsNullOrEmpty(result) &&
                       Convert.ToDecimal(result) != 0)
                    {
                        var norm = new TblNormativ();

                        norm.Cijena = await vm.GetZadnjaNabavnaCijenaAsync(ingredient.Artikl);

                        norm.Repromaterijal =
                            ingredient.Artikl;

                        norm.Kolicina =
                            Convert.ToDecimal(result);

                        norm.JedinicaMjere =
                            ingredient.JedinicaMjereName;

                        norm.IdProizvoda =
                            vm.SelectedPice.IdArtikla;

                        await vm.InsertNorm(
                            norm,
                            vm.SelectedPice);
                    }

                    MainContent.Effect = null;
                }

                if (_errorOccurred)
                {
                    _errorOccurred = false;
                    return;
                }
            }
        }


        private async void ListaNormativ_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            var norm =
                dataGrid.SelectedItem as TblNormativ;

            if (norm != null &&
               DataContext is CocktailNormsViewModel vm)
            {
                if (vm.SelectedNorm != null)
                {
                    MainContent.Effect =
                        new BlurEffect { Radius = 8 };

                    MyInputBox myInput =
                        new MyInputBox();

                    myInput.InputTitle.Text =
                        "IZMJENA NORMATIVA" +
                        Environment.NewLine +
                        Environment.NewLine +
                        "Unesite potrebnu količinu " +
                        norm.Repromaterijal +
                        " u " +
                        norm.JedinicaMjere;

                    myInput.InputTitle.FontSize = 14;

                    myInput.InputText.Text =
                        norm.Kolicina.ToString();

                    myInput.InputText.Focus();

                    myInput.NumbersOnly = true;

                    myInput.ShowDialog();

                    string result = myInput.result;

                    if (!string.IsNullOrEmpty(result) &&
                       Convert.ToDecimal(result) != 0)
                    {
                        var normUpdated =
                            new TblNormativ();

                        normUpdated.Cijena =
                            norm.Cijena ?? 0;

                        normUpdated.Repromaterijal =
                            norm.Repromaterijal;

                        normUpdated.Kolicina =
                            Convert.ToDecimal(result);

                        normUpdated.JedinicaMjere =
                            norm.JedinicaMjere;

                        normUpdated.IdProizvoda =
                            vm.SelectedPice.IdArtikla;

                        normUpdated.IdStavkeNormativa =
                            norm.IdStavkeNormativa;

                        await vm.UpdateNorm(
                            normUpdated,
                            vm.SelectedPice);
                    }

                    MainContent.Effect = null;
                }
                else
                {
                    Debug.WriteLine(
                        "vm.SelectedNorm == null");
                }

                if (_errorOccurred)
                {
                    _errorOccurred = false;
                    return;
                }
            }
            else
            {
                Debug.WriteLine(
                    "ListaNormativ.SelectedItem is NOT TblNormativ norm");
            }
        }


        private T? FindAncestor<T>(
            DependencyObject current)
            where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T)
                    return (T)current;

                current =
                    VisualTreeHelper.GetParent(current);
            }

            return null;
        }


        private void Button_PreviewMouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            var button = (Button)sender;

            var dataGridRow =
                FindAncestor<DataGridRow>(button);

            if (dataGridRow != null &&
               !dataGridRow.IsSelected)
            {
                dataGridRow.IsSelected = true;
            }
        }
    }
}