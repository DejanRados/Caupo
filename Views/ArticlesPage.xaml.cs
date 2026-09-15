using Caupo.Data;
using Caupo.Helpers;
using Caupo.Properties;
using Caupo.ViewModels;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for ArticlesPage.xaml
    /// </summary>
    public partial class ArticlesPage : UserControl, IKeyboardInputReceiver
    {
        public ArticlesPage()
        {
            this.DataContext = new ArticlesViewModel();

            InitializeComponent();

            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik.Radnik;
            BatchEditGrid.IsVisibleChanged += (s, e) => UpdateOverlayBlur();
            ArticleEditGrid.IsVisibleChanged += (s, e) => UpdateOverlayBlur();

            if (DataContext is ArticlesViewModel viewModel)
            {
                viewModel.PropertyChanged += ArticlesViewModel_PropertyChanged;
                viewModel.ErrorOccurred += ViewModel_ErrorOccurred;
            }
        }

        private void ArticlesViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ArticlesViewModel.BrojOznacenihArtikala))
                return;

            if (sender is not ArticlesViewModel viewModel)
                return;

            _updatingArticleCheckBoxes = true;
            SelectAllArticlesCheckBox.IsChecked = viewModel.AreAllVisibleArticlesChecked();
            _updatingArticleCheckBoxes = false;

            UpdateBatchEditButton();
        }

        private bool _isEditingArticle;
        private int? _editingArticleId;
        private VirtualKeyboard virtualKeyboard;
        private TextBox? FocusedTextBox = null;

        public void ReceiveKey(string key)
        {

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
                                    FocusedTextBox.Text.Remove(pos - 1, 1);
                                FocusedTextBox.SelectionStart = pos - 1;
                            }
                        }



                        break;

                    case "\uE75D":
                        InsertIntoFocused("\u0020");
                        break;
                    case "Sakrij":
                        FocusedTextBox = null;
                        ListaArtikala.Focus();
                        MainWindow.Instance.HideKeyboard();
                        break;
                    case "Enter":
                        FocusedTextBox = null;
                        ListaArtikala.Focus();
                        MainWindow.Instance.HideKeyboard();
                        break;
                    case "Reset":
                        FocusedTextBox.Text = "";

                        break;

                    default:
                        InsertIntoFocused(key);
                        break;
                }

                return;
            }


        }
        private void InsertIntoFocused(string text)
        {
            if (FocusedTextBox == null)
                return;

            int pos = FocusedTextBox.SelectionStart;
            FocusedTextBox.Text = FocusedTextBox.Text.Insert(pos, text);
            FocusedTextBox.SelectionStart = pos + text.Length;
        }
        private void KeyboardButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.ShowKeyboard();

        }
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                FocusedTextBox = sender as TextBox;
                FocusedTextBox.Clear();
                FocusedTextBox.SelectAll();
                MainWindow.Instance.ShowKeyboard();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("TextBox_GotFocus salje: " + ex);
            }


        }
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {

            FocusedTextBox = null;

        }

        private bool _updatingArticleCheckBoxes;

        private sealed class ArticleTypeOption
        {
            public int TypeId { get; }
            public string Name { get; }

            public ArticleTypeOption(int typeId, string name)
            {
                TypeId = typeId;
                Name = name;
            }

            public override string ToString()
            {
                return Name;
            }
        }
        private sealed class BatchUnitOption
        {
            public int? UnitId { get; }
            public string Name { get; }

            public BatchUnitOption(int? unitId, string name)
            {
                UnitId = unitId;
                Name = name;
            }

            public override string ToString()
            {
                return Name;
            }
        }
        private sealed class BatchCategoryOption
        {
            public int? CategoryId { get; }
            public string Name { get; }

            public BatchCategoryOption(int? categoryId, string name)
            {
                CategoryId = categoryId;
                Name = name;
            }

            public override string ToString()
            {
                return Name;
            }
        }
        private sealed class BatchTaxOption
        {
            public int? TaxId { get; }
            public string Name { get; }

            public BatchTaxOption(int? taxId, string name)
            {
                TaxId = taxId;
                Name = name;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        private void ConfigureArticleEditOptions(ArticlesViewModel viewModel)
        {
            ArticleTypeComboBox.ItemsSource = new List<ArticleTypeOption>
            {
                new ArticleTypeOption(-1, "SVI"),
                new ArticleTypeOption(0, "Piće"),
                new ArticleTypeOption(1, "Hrana"),
                new ArticleTypeOption(2, "Ostalo")
            };

            var articleTaxes = new List<BatchTaxOption> { new BatchTaxOption(null, "SVE") };
            articleTaxes.AddRange(viewModel.PoreskeStope.OrderBy(x => x.IdStope).Select(x => new BatchTaxOption(x.IdStope, x.ToString())));
            ArticleTaxComboBox.ItemsSource = articleTaxes;
            ArticleUnitComboBox.ItemsSource = viewModel.JediniceMjere.OrderBy(x => x.IdJedinice).Select(x => new BatchUnitOption(x.IdJedinice, x.ToString())).ToList();
            ArticleNormativComboBox.ItemsSource = viewModel.Normativi.ToList();

            bool isCroatia = string.Equals(Settings.Default.Country, "Hrvatska", StringComparison.OrdinalIgnoreCase);
            ArticleConsumptionTaxPanel.Visibility = isCroatia ? Visibility.Visible : Visibility.Collapsed;
        }

        private static decimal? ParseArticleNormativ(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string normalized = value.Trim().Replace(',', '.');
            return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result) ? result : null;
        }

        private static string BuildArticleNormativ(string? articleName, string? normativText)
        {
            string name = articleName?.Trim() ?? string.Empty;
            string text = normativText?.Trim() ?? "1";
            decimal? normativ = ParseArticleNormativ(text);
            return normativ == 1m ? name : $"{name} {text}";
        }

        private void UpdateArticleNormativLabel()
        {
            if (ArticleNormativLabel == null || ArticleNameTextBox == null || ArticleNormativComboBox == null)
                return;

            if (ArticleNormativComboBox.SelectedItem is not TblNormativPica selectedNormativ)
            {
                ArticleNormativLabel.Content = ArticleNameTextBox.Text.Trim();
                return;
            }

            if (string.Equals(selectedNormativ.Normativ, "Dodaj normativ", StringComparison.OrdinalIgnoreCase))
                return;

            decimal? normativ = ParseArticleNormativ(selectedNormativ.Normativ);
            ArticleNormativLabel.Content = normativ.HasValue ? BuildArticleNormativ(ArticleNameTextBox.Text, selectedNormativ.Normativ) : ArticleNameTextBox.Text.Trim();
        }

        private async void ArticleNormativComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ArticleEditComboBox_SelectionChanged(sender, e);
            if (ArticleNormativComboBox.SelectedItem is not TblNormativPica selectedNormativ)
            {
                UpdateArticleNormativLabel();
                return;
            }

            if (!string.Equals(selectedNormativ.Normativ, "Dodaj normativ", StringComparison.OrdinalIgnoreCase))
            {
                UpdateArticleNormativLabel();
                return;
            }

            if (DataContext is not ArticlesViewModel viewModel)
                return;

            MyInputBox dialog = new MyInputBox();
            dialog.InputTitle.Text = "NOVI NORMATIV PIĆA";
            dialog.InputText.Focus();
            dialog.ShowDialog();

            string result = dialog.result?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(result))
            {
                ArticleNormativComboBox.SelectedItem = viewModel.Normativi.FirstOrDefault(x => ParseArticleNormativ(x.Normativ) == 1m);
                return;
            }

            decimal? newValue = ParseArticleNormativ(result);

            if (!newValue.HasValue || newValue.Value <= 0)
            {
                ShowArticleEditMessage("GREŠKA", "Unesite ispravan normativ veći od 0.");
                ArticleNormativComboBox.SelectedItem = viewModel.Normativi.FirstOrDefault(x => ParseArticleNormativ(x.Normativ) == 1m);
                return;
            }

            TblNormativPica? existing = viewModel.Normativi.FirstOrDefault(x => ParseArticleNormativ(x.Normativ) == newValue.Value);

            if (existing != null)
            {
                ArticleNormativComboBox.SelectedItem = existing;
                return;
            }

            string normalizedText = result.Replace(',', '.');

            await using (var db = new AppDbContext())
            {
                db.NormativPica.Add(new TblNormativPica { Normativ = normalizedText });
                await db.SaveChangesAsync();
            }

            await viewModel.LoadNormativi();
            ArticleNormativComboBox.ItemsSource = viewModel.Normativi.ToList();
            ArticleNormativComboBox.SelectedItem = ArticleNormativComboBox.Items.OfType<TblNormativPica>().FirstOrDefault(x => ParseArticleNormativ(x.Normativ) == newValue.Value);
        }

        private void ArticleNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateArticleNormativLabel();
            ArticleEditTextBox_TextChanged(sender, e);
        }

        private void ArticleTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ArticleEditComboBox_SelectionChanged(sender, e);
            if (DataContext is not ArticlesViewModel viewModel || ArticleTypeComboBox.SelectedItem is not ArticleTypeOption selectedType)
            {
                ArticleCategoryComboBox.ItemsSource = null;
                return;
            }

            if (selectedType.TypeId < 0)
            {
                ArticleCategoryComboBox.ItemsSource = new List<BatchCategoryOption> { new BatchCategoryOption(null, "SVE") };
                ArticleCategoryComboBox.SelectedIndex = 0;
                ArticleNormativPanel.Visibility = Visibility.Collapsed;
                return;
            }

            var articleCategories = new List<BatchCategoryOption> { new BatchCategoryOption(null, "SVE") };
            articleCategories.AddRange(viewModel.Kategorije.Where(x => x.VrstaArtikla == selectedType.TypeId).OrderBy(x => x.Kategorija).Select(x => new BatchCategoryOption(x.IdKategorije, x.Kategorija ?? string.Empty)));
            ArticleCategoryComboBox.ItemsSource = articleCategories;
            ArticleCategoryComboBox.SelectedIndex = 0;
            ArticleNormativPanel.Visibility = selectedType.TypeId == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (selectedType.TypeId != 0)
                ArticleNormativLabel.Content = ArticleNameTextBox.Text.Trim();
        }

        private void SetArticleFieldError(Control control)
        {
            control.BorderBrush = Brushes.Red;
            control.BorderThickness = new Thickness(0, 0, 0, 2);
        }

        private void ClearArticleFieldError(Control control)
        {
            control.BorderBrush = Application.Current.Resources["GlobalFontColor"] as Brush;
            control.BorderThickness = new Thickness(0, 0, 0, 1);
        }

        private void ClearArticleEditErrors()
        {
            ClearArticleFieldError(ArticleNameTextBox);
            ClearArticleFieldError(ArticleCodeTextBox);
            ClearArticleFieldError(ArticleInternalCodeTextBox);
            ClearArticleFieldError(ArticlePriceTextBox);
            ClearArticleFieldError(ArticleUnitComboBox);
            ClearArticleFieldError(ArticleNormativComboBox);
            ClearArticleFieldError(ArticleTypeComboBox);
            ClearArticleFieldError(ArticleCategoryComboBox);
            ClearArticleFieldError(ArticleTaxComboBox);
            ClearArticleFieldError(ArticlePositionTextBox);
        }

        private void ArticleEditTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrWhiteSpace(textBox.Text))
                ClearArticleFieldError(textBox);
        }

        private void ArticleEditComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox comboBox || comboBox.SelectedItem == null)
                return;

            bool valid = comboBox switch
            {
                _ when comboBox == ArticleTypeComboBox => comboBox.SelectedItem is ArticleTypeOption type && type.TypeId >= 0,
                _ when comboBox == ArticleCategoryComboBox => comboBox.SelectedItem is BatchCategoryOption category && category.CategoryId != null,
                _ when comboBox == ArticleTaxComboBox => comboBox.SelectedItem is BatchTaxOption tax && tax.TaxId != null,
                _ when comboBox == ArticleUnitComboBox => comboBox.SelectedItem is BatchUnitOption unit && unit.UnitId != null,
                _ when comboBox == ArticleNormativComboBox => comboBox.SelectedItem is TblNormativPica normativ && !string.Equals(normativ.Normativ, "Dodaj normativ", StringComparison.OrdinalIgnoreCase) && ParseArticleNormativ(normativ.Normativ) > 0m,
                _ => true
            };

            if (valid)
                ClearArticleFieldError(comboBox);
        }

        private void ConfigureBatchUnit(ArticlesViewModel viewModel)
        {
            var units = new List<BatchUnitOption>
            {
                new BatchUnitOption (null, "Ne mijenjaj")
            };

            units.AddRange(
                viewModel.JediniceMjere
                    .OrderBy(x => x.IdJedinice)
                    .Select(x => new BatchUnitOption(x.IdJedinice, x.ToString()))
            );

            BatchUnitComboBox.ItemsSource = units;
            BatchUnitComboBox.SelectedIndex = 0;
        }
        private void ConfigureBatchConsumptionTax()
        {
            bool isCroatia = string.Equals(Settings.Default.Country, "Hrvatska", StringComparison.OrdinalIgnoreCase);

            BatchConsumptionTaxPanel.Visibility = isCroatia ? Visibility.Visible : Visibility.Collapsed;
            BatchConsumptionTaxComboBox.SelectedIndex = 0;
        }
        private void ConfigureBatchTax(ArticlesViewModel viewModel)
        {
            var taxes = new List<BatchTaxOption>
    {
        new BatchTaxOption (null, "Ne mijenjaj")
    };

            taxes.AddRange(
                viewModel.PoreskeStope
                    .OrderBy(x => x.IdStope)
                    .Select(x => new BatchTaxOption(x.IdStope, x.ToString()))
            );

            BatchTaxComboBox.ItemsSource = taxes;
            BatchTaxComboBox.SelectedIndex = 0;
        }
        private void ConfigureBatchCategory(ArticlesViewModel viewModel)
        {
            var selectedArticles = viewModel.Artikli
                .Where(viewModel.IsArticleChecked)
                .ToList();

            var articleTypes = selectedArticles
                .Select(x => x.VrstaArtikla)
                .Distinct()
                .ToList();

            if (articleTypes.Count != 1 || !articleTypes[0].HasValue)
            {
                BatchCategoryComboBox.ItemsSource = new List<BatchCategoryOption>
        {
            new BatchCategoryOption (null, "Nije dostupno - različite vrste artikala")
        };

                BatchCategoryComboBox.SelectedIndex = 0;
                BatchCategoryComboBox.IsEnabled = false;
                return;
            }

            int articleType = articleTypes[0]!.Value;

            var categories = new List<BatchCategoryOption>
    {
        new BatchCategoryOption (null, "Ne mijenjaj")
    };

            categories.AddRange(
                viewModel.Kategorije
                    .Where(x => x.VrstaArtikla == articleType)
                    .OrderBy(x => x.Kategorija)
                    .Select(x => new BatchCategoryOption(x.IdKategorije, x.Kategorija ?? string.Empty))
            );

            BatchCategoryComboBox.ItemsSource = categories;
            BatchCategoryComboBox.SelectedIndex = 0;
            BatchCategoryComboBox.IsEnabled = true;
        }
        private void ArticleCheckBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox || checkBox.Tag is not DatabaseTables.TblArtikli article)
                return;

            if (DataContext is not ArticlesViewModel viewModel)
                return;

            _updatingArticleCheckBoxes = true;
            checkBox.IsChecked = viewModel.IsArticleChecked(article);
            _updatingArticleCheckBoxes = false;
        }

        private void ArticleCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_updatingArticleCheckBoxes)
                return;

            if (sender is not CheckBox checkBox || checkBox.Tag is not DatabaseTables.TblArtikli article)
                return;

            if (DataContext is not ArticlesViewModel viewModel)
                return;

            viewModel.SetArticleChecked(article, true);
            RefreshSelectAllCheckBox(viewModel);
            UpdateBatchEditButton();
        }

        private void ArticleCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_updatingArticleCheckBoxes)
                return;

            if (sender is not CheckBox checkBox || checkBox.Tag is not DatabaseTables.TblArtikli article)
                return;

            if (DataContext is not ArticlesViewModel viewModel)
                return;

            viewModel.SetArticleChecked(article, false);
            RefreshSelectAllCheckBox(viewModel);
            UpdateBatchEditButton();
        }

        private void SelectAllArticlesCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_updatingArticleCheckBoxes)
                return;

            if (DataContext is not ArticlesViewModel viewModel)
                return;

            viewModel.SetAllVisibleArticlesChecked(true);
            RefreshVisibleArticleCheckBoxes(viewModel);
            UpdateBatchEditButton();
        }

        private void SelectAllArticlesCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_updatingArticleCheckBoxes)
                return;

            if (DataContext is not ArticlesViewModel viewModel)
                return;

            viewModel.SetAllVisibleArticlesChecked(false);
            RefreshVisibleArticleCheckBoxes(viewModel);
            UpdateBatchEditButton();
        }

        private void RefreshVisibleArticleCheckBoxes(ArticlesViewModel viewModel)
        {
            _updatingArticleCheckBoxes = true;

            foreach (var article in ListaArtikala.Items.OfType<DatabaseTables.TblArtikli>())
            {
                if (ListaArtikala.ItemContainerGenerator.ContainerFromItem(article) is not DataGridRow row)
                    continue;

                var cellContent = ListaArtikala.Columns[^1].GetCellContent(row);

                if (cellContent == null)
                    continue;

                CheckBox? checkBox = FindVisualChildren<CheckBox>(cellContent).FirstOrDefault();

                if (checkBox != null)
                    checkBox.IsChecked = viewModel.IsArticleChecked(article);
            }

            SelectAllArticlesCheckBox.IsChecked = viewModel.AreAllVisibleArticlesChecked();

            _updatingArticleCheckBoxes = false;
        }

        private void RefreshSelectAllCheckBox(ArticlesViewModel viewModel)
        {
            _updatingArticleCheckBoxes = true;
            SelectAllArticlesCheckBox.IsChecked = viewModel.AreAllVisibleArticlesChecked();
            _updatingArticleCheckBoxes = false;
        }

        private void UpdateBatchEditButton()
        {
            if (DataContext is ArticlesViewModel viewModel)
                BtnBatchEdit.IsEnabled = viewModel.BrojOznacenihArtikala > 0;
        }

        private void UpdateOverlayBlur()
        {
            bool overlayVisible = BatchEditGrid.Visibility == Visibility.Visible || ArticleEditGrid.Visibility == Visibility.Visible;
            MainContent.Effect = overlayVisible ? new BlurEffect { Radius = 8 } : null;
        }

        private void BtnBatchEdit_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ArticlesViewModel viewModel || viewModel.BrojOznacenihArtikala == 0)
                return;

            BatchEditSelectedCountText.Text = $"Označeno: {viewModel.BrojOznacenihArtikala} artikala";

            BatchValueTextBox.Clear();
            BatchPriceOperationComboBox.SelectedIndex = 0;
            BatchActiveComboBox.SelectedIndex = 0;
            BatchValueLabel.Text = "Nova cijena";

            ConfigureBatchCategory(viewModel);
            ConfigureBatchTax(viewModel);
            ConfigureBatchConsumptionTax();
            ConfigureBatchUnit(viewModel);

            BatchEditGrid.Visibility = Visibility.Visible;
            BatchValueTextBox.Focus();
        }

        private void BtnBatchEditCancel_Click(object sender, RoutedEventArgs e)
        {
            BatchEditGrid.Visibility = Visibility.Collapsed;
        }

        private async void BtnBatchEditApply_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ArticlesViewModel viewModel || viewModel.BrojOznacenihArtikala == 0)
                return;

            bool changePrice = !string.IsNullOrWhiteSpace(BatchValueTextBox.Text);
            bool changeActive = BatchActiveComboBox.SelectedIndex > 0;

            BatchCategoryOption? selectedCategory = BatchCategoryComboBox.SelectedItem as BatchCategoryOption;
            bool changeCategory = BatchCategoryComboBox.IsEnabled && selectedCategory?.CategoryId != null;

            BatchTaxOption? selectedTax = BatchTaxComboBox.SelectedItem as BatchTaxOption;
            bool changeTax = selectedTax?.TaxId != null;

            bool changeConsumptionTax = BatchConsumptionTaxPanel.Visibility == Visibility.Visible && BatchConsumptionTaxComboBox.SelectedIndex > 0;

            BatchUnitOption? selectedUnit = BatchUnitComboBox.SelectedItem as BatchUnitOption;
            bool changeUnit = selectedUnit?.UnitId != null;

            if (!changePrice && !changeActive && !changeCategory && !changeTax && !changeConsumptionTax && !changeUnit)
            {
                ShowBatchEditMessage("GRUPNO UREĐIVANJE", "Niste odabrali nijednu promjenu.");
                return;
            }

            decimal value = 0m;

            if (changePrice && (!TryParseBatchValue(BatchValueTextBox.Text, out value) || value <= 0))
            {
                ShowBatchEditMessage("GREŠKA", "Unesite ispravnu vrijednost veću od 0.");
                BatchValueTextBox.Focus();
                BatchValueTextBox.SelectAll();
                return;
            }

            int operation = BatchPriceOperationComboBox.SelectedIndex;

            if (changePrice && (operation < 0 || operation > 2))
                return;

            if (changePrice && operation == 2 && value >= 100m)
            {
                ShowBatchEditMessage("GREŠKA", "Postotak smanjenja mora biti manji od 100%.");
                BatchValueTextBox.Focus();
                BatchValueTextBox.SelectAll();
                return;
            }

            var selectedIds = viewModel.Artikli
                .Where(viewModel.IsArticleChecked)
                .Select(x => x.IdArtikla)
                .ToList();

            if (selectedIds.Count == 0)
                return;

            int? selectedArticleId = viewModel.SelectedArticle?.IdArtikla;

            try
            {
                await using var db = new AppDbContext();

                var articles = await db.Artikli
                    .Where(x => selectedIds.Contains(x.IdArtikla))
                    .ToListAsync();

                foreach (var article in articles)
                {
                    if (changePrice)
                    {
                        if (operation == 0)
                        {
                            article.Cijena = value;
                        }
                        else if (article.Cijena.HasValue)
                        {
                            decimal newPrice = operation == 1
                                ? article.Cijena.Value * (1m + value / 100m)
                                : article.Cijena.Value * (1m - value / 100m);

                            article.Cijena = RoundPriceUpToTenCents(newPrice);
                        }
                    }

                    if (changeActive)
                        article.Aktivan = BatchActiveComboBox.SelectedIndex == 1;

                    if (changeCategory && selectedCategory?.CategoryId != null)
                        article.Kategorija = selectedCategory.CategoryId.Value;

                    if (changeTax && selectedTax?.TaxId != null)
                        article.PoreskaStopa = selectedTax.TaxId.Value;

                    if (changeConsumptionTax)
                        article.PorezNaPotrosnju = BatchConsumptionTaxComboBox.SelectedIndex == 1;

                    if (changeUnit && selectedUnit?.UnitId != null)
                        article.JedinicaMjere = selectedUnit.UnitId.Value;
                }

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ARTICLES] Batch save: " + ex);
                ShowBatchEditMessage("GREŠKA", "Grupna promjena artikala nije spremljena.");
                return;
            }

            await viewModel.LoadArticlesAsync();

            if (selectedArticleId.HasValue)
                viewModel.SelectedArticle = viewModel.Artikli.FirstOrDefault(x => x.IdArtikla == selectedArticleId.Value);

            BatchEditGrid.Visibility = Visibility.Collapsed;
            RefreshVisibleArticleCheckBoxes(viewModel);
            RefreshSelectAllCheckBox(viewModel);
            UpdateBatchEditButton();
        }

        private static decimal RoundPriceUpToTenCents(decimal price)
        {
            return Math.Ceiling(price * 10m) / 10m;
        }

        private static bool TryParseBatchValue(string? text, out decimal value)
        {
            value = 0m;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();

            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
                return true;

            string normalized = text.Replace(',', '.');
            return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private static void ShowBatchEditMessage(string title, string message)
        {
            var dialog = new MyMessageBox
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            dialog.MessageTitle.Text = title;
            dialog.MessageText.Text = message;
            dialog.ShowDialog();
        }

        private void BatchPriceOperationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BatchValueLabel == null || BatchPriceOperationComboBox.SelectedIndex < 0)
                return;

            BatchValueLabel.Text = BatchPriceOperationComboBox.SelectedIndex == 0 ? "Nova cijena" : "Postotak";
        }




        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var page = new HomePage();
            page.DataContext = new HomeViewModel();
            PageNavigator.NavigateWithFade(page);
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null)
                yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject? child = VisualTreeHelper.GetChild(depObj, i);
                if (child == null)
                    continue;

                if (child is T matchedChild)
                    yield return matchedChild;

                foreach (T childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }
        private void ViewModel_ErrorOccurred(object? sender, string? errorMessage)
        {
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                if (errorMessage.StartsWith("Šifra ", StringComparison.OrdinalIgnoreCase) || errorMessage.Contains(", Šifra ", StringComparison.OrdinalIgnoreCase))
                    SetArticleFieldError(ArticleCodeTextBox);

                if (errorMessage.Contains("Interna šifra", StringComparison.OrdinalIgnoreCase))
                    SetArticleFieldError(ArticleInternalCodeTextBox);

                if (errorMessage.Contains("Naziv za prodaju", StringComparison.OrdinalIgnoreCase))
                {
                    SetArticleFieldError(ArticleNameTextBox);
                    if (ArticleNormativPanel.Visibility == Visibility.Visible)
                        SetArticleFieldError(ArticleNormativComboBox);
                }
            }

            MyMessageBox myMessageBox = new MyMessageBox
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            myMessageBox.MessageTitle.Text = "GREŠKA";
            myMessageBox.MessageText.Text = errorMessage;
            myMessageBox.ShowDialog();
        }

        private void ListaArtikala_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject? source = e.OriginalSource as DependencyObject;

            while (source != null && source is not DataGridRow)
                source = VisualTreeHelper.GetParent(source);

            if (source is DataGridRow row && !row.IsSelected)
            {
                row.IsSelected = true;
                ListaArtikala.SelectedItem = row.Item;
                ListaArtikala.CurrentItem = row.Item;
            }
        }



        private void ListaArtikala_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not DataGrid dataGrid || dataGrid.SelectedItem is not DatabaseTables.TblArtikli selectedItem)
                return;

            if (DataContext is ArticlesViewModel viewModel)
                viewModel.SelectedArticle = selectedItem;

            dataGrid.ScrollIntoView(selectedItem);
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ArticlesViewModel viewModel || viewModel.SelectedArticle == null)
                return;


            TblArtikli article = viewModel.SelectedArticle;

            _isEditingArticle = true;
            _editingArticleId = article.IdArtikla;
            ClearArticleEditErrors();

            bool hasBeenSold = await viewModel.HasArticleBeenSold(article.Sifra);
            ArticleNameTextBox.IsReadOnly = hasBeenSold;

            ArticleEditTitle.Text = "Uredi artikl";
            ArticleEditDescription.Text = $"Uređivanje artikla: {article.Artikl}";

            ConfigureArticleEditOptions(viewModel);

            ArticleNameTextBox.Text = article.Artikl ?? string.Empty;
            ArticleCodeTextBox.Text = article.Sifra ?? string.Empty;
            ArticleInternalCodeTextBox.Text = article.InternaSifra ?? string.Empty;
            ArticlePriceTextBox.Text = article.Cijena?.ToString("0.00", CultureInfo.CurrentCulture) ?? string.Empty;

            ArticleTypeComboBox.SelectedItem = ArticleTypeComboBox.Items.OfType<ArticleTypeOption>().FirstOrDefault(x => x.TypeId == article.VrstaArtikla);

            ArticleCategoryComboBox.SelectedItem = ArticleCategoryComboBox.Items.OfType<BatchCategoryOption>().FirstOrDefault(x => x.CategoryId == article.Kategorija);
            ArticleTaxComboBox.SelectedItem = ArticleTaxComboBox.Items.OfType<BatchTaxOption>().FirstOrDefault(x => x.TaxId == article.PoreskaStopa);
            ArticleUnitComboBox.SelectedItem = ArticleUnitComboBox.Items.OfType<BatchUnitOption>().FirstOrDefault(x => x.UnitId == article.JedinicaMjere);
            ArticleNormativComboBox.SelectedItem = ArticleNormativComboBox.Items.OfType<TblNormativPica>().FirstOrDefault(x => ParseArticleNormativ(x.Normativ) == article.Normativ);
            ArticlePositionTextBox.Text = article.Pozicija?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            ArticleNormativLabel.Content = BuildArticleNormativ(article.Artikl, ArticleNormativComboBox.SelectedItem is TblNormativPica editNormativ ? editNormativ.Normativ : "1");

            ArticleConsumptionTaxComboBox.SelectedIndex = article.PorezNaPotrosnju ? 0 : 1;
            ArticleActiveComboBox.SelectedIndex = article.Aktivan ? 0 : 1;

            ArticleEditGrid.Visibility = Visibility.Visible;
            ArticleNameTextBox.Focus();
        }

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ArticlesViewModel viewModel)
                return;

            _isEditingArticle = false;
            _editingArticleId = null;
            ClearArticleEditErrors();
            ArticleNameTextBox.IsReadOnly = false;

            ArticleEditTitle.Text = "Novi artikl";
            ArticleEditDescription.Text = "Unos podataka novog artikla";

            long nextId = await viewModel.GetNextArticleSequenceValueAsync();
            string suggestedValue = nextId.ToString(CultureInfo.InvariantCulture);

            ArticleNameTextBox.Clear();
            ArticleCodeTextBox.Text = suggestedValue;
            ArticleInternalCodeTextBox.Text = suggestedValue;
            ArticlePriceTextBox.Clear();
            ArticlePositionTextBox.Text = suggestedValue;

            ConfigureArticleEditOptions(viewModel);

            ArticleTypeComboBox.SelectedIndex = 0;
            ArticleTaxComboBox.SelectedIndex = 0;
            ArticleUnitComboBox.SelectedItem = ArticleUnitComboBox.Items.OfType<BatchUnitOption>().FirstOrDefault(x => string.Equals(x.Name, "komad", StringComparison.OrdinalIgnoreCase))
                ?? ArticleUnitComboBox.Items.OfType<BatchUnitOption>().FirstOrDefault(x => string.Equals(x.Name, "kom", StringComparison.OrdinalIgnoreCase));
            ArticleNormativComboBox.SelectedItem = ArticleNormativComboBox.Items.OfType<TblNormativPica>().FirstOrDefault(x => ParseArticleNormativ(x.Normativ) == 1m);
            UpdateArticleNormativLabel();
            ArticleConsumptionTaxComboBox.SelectedIndex = 1;
            ArticleActiveComboBox.SelectedIndex = 0;

            ArticleEditGrid.Visibility = Visibility.Visible;
            ArticleNameTextBox.Focus();
        }

        private void BtnArticleEditCancel_Click(object sender, RoutedEventArgs e)
        {
            ClearArticleEditErrors();
            ArticleEditGrid.Visibility = Visibility.Collapsed;
        }

        private async void BtnArticleEditSave_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ArticlesViewModel viewModel)
                return;

            ClearArticleEditErrors();

            string naziv = ArticleNameTextBox.Text.Trim();
            string sifra = ArticleCodeTextBox.Text.Trim();
            string internaSifra = ArticleInternalCodeTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(naziv))
            {
                SetArticleFieldError(ArticleNameTextBox);
                ShowArticleEditMessage("GREŠKA", "Unesite naziv artikla.");
                ArticleNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(sifra))
            {
                SetArticleFieldError(ArticleCodeTextBox);
                ShowArticleEditMessage("GREŠKA", "Unesite šifru artikla.");
                ArticleCodeTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(internaSifra))
            {
                SetArticleFieldError(ArticleInternalCodeTextBox);
                ShowArticleEditMessage("GREŠKA", "Unesite internu šifru artikla.");
                ArticleInternalCodeTextBox.Focus();
                return;
            }

            if (!TryParseArticlePrice(ArticlePriceTextBox.Text, out decimal cijena) || cijena < 0)
            {
                SetArticleFieldError(ArticlePriceTextBox);
                ShowArticleEditMessage("GREŠKA", "Unesite ispravnu cijenu artikla.");
                ArticlePriceTextBox.Focus();
                ArticlePriceTextBox.SelectAll();
                return;
            }

            if (ArticleTypeComboBox.SelectedItem is not ArticleTypeOption selectedType || selectedType.TypeId < 0)
            {
                SetArticleFieldError(ArticleTypeComboBox);
                ShowArticleEditMessage("GREŠKA", "Odaberite vrstu artikla.");
                ArticleTypeComboBox.Focus();
                return;
            }

            if (ArticleCategoryComboBox.SelectedItem is not BatchCategoryOption selectedCategory || selectedCategory.CategoryId == null)
            {
                SetArticleFieldError(ArticleCategoryComboBox);
                ShowArticleEditMessage("GREŠKA", "Odaberite kategoriju artikla.");
                ArticleCategoryComboBox.Focus();
                return;
            }

            if (ArticleTaxComboBox.SelectedItem is not BatchTaxOption selectedTax || selectedTax.TaxId == null)
            {
                SetArticleFieldError(ArticleTaxComboBox);
                ShowArticleEditMessage("GREŠKA", "Odaberite poresku stopu.");
                ArticleTaxComboBox.Focus();
                return;
            }

            if (ArticleUnitComboBox.SelectedItem is not BatchUnitOption selectedUnit || selectedUnit.UnitId == null)
            {
                SetArticleFieldError(ArticleUnitComboBox);
                ShowArticleEditMessage("GREŠKA", "Odaberite jedinicu mjere.");
                ArticleUnitComboBox.Focus();
                return;
            }

            decimal normativ = 1m;
            string normativText = "1";

            if (selectedType.TypeId == 0)
            {
                if (ArticleNormativComboBox.SelectedItem is not TblNormativPica selectedNormativ || string.Equals(selectedNormativ.Normativ, "Dodaj normativ", StringComparison.OrdinalIgnoreCase))
                {
                    SetArticleFieldError(ArticleNormativComboBox);
                    ShowArticleEditMessage("GREŠKA", "Odaberite normativ artikla.");
                    ArticleNormativComboBox.Focus();
                    return;
                }

                decimal? parsedNormativ = ParseArticleNormativ(selectedNormativ.Normativ);

                if (!parsedNormativ.HasValue || parsedNormativ.Value <= 0)
                {
                    SetArticleFieldError(ArticleNormativComboBox);
                    ShowArticleEditMessage("GREŠKA", "Odaberite ispravan normativ artikla.");
                    ArticleNormativComboBox.Focus();
                    return;
                }

                normativ = parsedNormativ.Value;
                normativText = selectedNormativ.Normativ?.Trim() ?? "1";
            }

            if (!int.TryParse(ArticlePositionTextBox.Text.Trim(), out int pozicija) || pozicija < 0)
            {
                SetArticleFieldError(ArticlePositionTextBox);
                ShowArticleEditMessage("GREŠKA", "Unesite ispravnu poziciju artikla.");
                ArticlePositionTextBox.Focus();
                ArticlePositionTextBox.SelectAll();
                return;
            }

            TblArtikli article;

            if (_isEditingArticle)
            {
                if (!_editingArticleId.HasValue)
                    return;

                article = viewModel.Artikli.FirstOrDefault(x => x.IdArtikla == _editingArticleId.Value);

                if (article == null)
                {
                    ShowArticleEditMessage("GREŠKA", "Artikl više nije pronađen.");
                    return;
                }

                if (await viewModel.HasArticleBeenSold(article.Sifra))
                    naziv = article.Artikl ?? string.Empty;
            }
            else
            {
                article = new TblArtikli();
            }

            article.Artikl = naziv;
            article.Sifra = sifra;
            article.InternaSifra = internaSifra;
            article.Cijena = cijena;
            article.VrstaArtikla = selectedType.TypeId;
            article.Kategorija = selectedCategory.CategoryId.Value;
            article.PoreskaStopa = selectedTax.TaxId.Value;
            article.JedinicaMjere = selectedUnit.UnitId.Value;
            article.Normativ = normativ;
            article.Pozicija = pozicija;
            article.ArtiklNormativ = selectedType.TypeId == 0 ? BuildArticleNormativ(naziv, normativText) : naziv;
            article.Aktivan = ArticleActiveComboBox.SelectedIndex != 1;
            article.PorezNaPotrosnju = ArticleConsumptionTaxPanel.Visibility == Visibility.Visible && ArticleConsumptionTaxComboBox.SelectedIndex == 0;

            try
            {
                bool saved = _isEditingArticle ? await viewModel.UpdateArticle(article) : await viewModel.InsertArticle(article);

                if (!saved)
                    return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ARTICLES] Save article: " + ex);
                ShowArticleEditMessage("GREŠKA", "Artikl nije spremljen.");
                return;
            }

            ArticleEditGrid.Visibility = Visibility.Collapsed;
            _isEditingArticle = false;
            _editingArticleId = null;
        }

        private static bool TryParseArticlePrice(string text, out decimal value)
        {
            string normalized = text.Trim().Replace(',', '.');
            return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private static void ShowArticleEditMessage(string title, string message)
        {
            MyMessageBox myMessageBox = new MyMessageBox();
            myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            myMessageBox.MessageTitle.Text = title;
            myMessageBox.MessageText.Text = message;
            myMessageBox.ShowDialog();
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ArticlesViewModel viewModel || viewModel.SelectedArticle == null)
            {
                ShowArticleEditMessage("GREŠKA", "Odaberite artikl koji želite obrisati.");
                return;
            }

            TblArtikli article = viewModel.SelectedArticle;
            bool hasBeenSold = await viewModel.HasArticleBeenSold(article.Sifra);

            if (hasBeenSold)
            {
                if (!article.Aktivan)
                {
                    ShowArticleEditMessage("INFORMACIJA", $"Artikl {article.Artikl} je već neaktivan i ne može se fizički obrisati jer je korišten na računima.");
                    return;
                }

                YesNoPopup deactivatePopup = new YesNoPopup();
                deactivatePopup.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                deactivatePopup.MessageTitle.Text = "DEAKTIVACIJA ARTIKLA";
                deactivatePopup.MessageText.Text = $"Artikl {article.Artikl} je već korišten na računima i ne može se fizički obrisati." + Environment.NewLine + Environment.NewLine + "Želite li ga postaviti kao neaktivan?";
                deactivatePopup.ShowDialog();

                if (deactivatePopup.Kliknuo != "Da")
                    return;

                if (await viewModel.DeactivateArticle(article.IdArtikla))
                    ShowArticleEditMessage("POTVRDA", $"Artikl {article.Artikl} je postavljen kao neaktivan.");

                return;
            }

            YesNoPopup deletePopup = new YesNoPopup();
            deletePopup.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            deletePopup.MessageTitle.Text = "POTVRDA BRISANJA";
            deletePopup.MessageText.Text = $"Da li ste sigurni da želite obrisati artikl:" + Environment.NewLine + article.Artikl + " ?";
            deletePopup.ShowDialog();

            if (deletePopup.Kliknuo == "Da")
                await viewModel.DeleteArticle(article.IdArtikla);
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

        private async void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                await ImportExcelToSQLiteAsync(openFileDialog.FileName);
            }
            else
            {
                return;
            }


        }

        private void SaveExcelFile(string filePath)
        {
            Debug.WriteLine("=== START EXPORT EXCEL ===");

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Artikli");
            var listSheet = workbook.Worksheets.Add("Lists");

            // =========================
            // HEADER (REDOM, BEZ RUPA)
            // =========================
            worksheet.Cell(1, 1).Value = "Artikl";
            worksheet.Cell(1, 2).Value = "VrstaArtikla";
            worksheet.Cell(1, 3).Value = "Cijena";
            worksheet.Cell(1, 4).Value = "Normativ";
            worksheet.Cell(1, 5).Value = "JedinicaMjere";
            Debug.WriteLine("Headers written");

            // =========================
            // DROPDOWN LISTE
            // =========================
            // VrstaArtikla
            listSheet.Cell("C1").Value = "Piće";
            listSheet.Cell("C2").Value = "Hrana";
            listSheet.Cell("C3").Value = "Ostalo";

            // JedinicaMjere
            listSheet.Cell("A1").Value = "kom";
            listSheet.Cell("A2").Value = "kg";
            listSheet.Cell("A3").Value = "m";
            listSheet.Cell("A4").Value = "m2";
            listSheet.Cell("A5").Value = "m3";
            listSheet.Cell("A6").Value = "lit";
            listSheet.Cell("A7").Value = "tona";
            listSheet.Cell("A8").Value = "g";
            listSheet.Cell("A9").Value = "por";
            listSheet.Cell("A10").Value = "pak";

            // =========================
            // DATA CONTEXT
            // =========================
            if (DataContext is not ArticlesViewModel vm || vm.Artikli == null)
            {
                Debug.WriteLine("ERROR: DataContext or Artikli is NULL");
                return;
            }

            // Normativ list
            int normativRow = 1;
            if (vm.Normativi != null)
            {
                foreach (var n in vm.Normativi)
                {
                    // Pretvori decimal u string da dropdown radi
                    listSheet.Cell(normativRow, 2).Value = n.Normativ.ToString();
                    Debug.WriteLine($"Normativ added to list: {n.Normativ}");
                    normativRow++;
                }
            }

            // =========================
            // PODACI
            // =========================
            int row = 2;
            foreach (var a in vm.Artikli)
            {
                Debug.WriteLine($"Exporting: {a.Artikl}");

                worksheet.Cell(row, 1).Value = a.Artikl;
                worksheet.Cell(row, 2).Value = a.VrstaArtiklaName;
                worksheet.Cell(row, 3).Value = a.Cijena;
                worksheet.Cell(row, 4).Value = a.Normativ.ToString(); // convert decimal -> string
                worksheet.Cell(row, 5).Value = a.JedinicaMjereName;

                //VrstaArtikla dropdown
                worksheet.Range(row, 2, row, 2)
                         .SetDataValidation()
                         .List(listSheet.Range("C1:C3"));

                //JedinicaMjere dropdown
                worksheet.Range(row, 5, row, 5)
                         .SetDataValidation()
                         .List(listSheet.Range("A1:A10"));

                // Normativ dropdown
                worksheet.Range(row, 4, row, 4)
                         .SetDataValidation()
                         .List(listSheet.Range($"B1:B{normativRow - 1}"));

                row++;
            }

            // =========================
            // ZAKLJUČAVANJE STRUKTURE
            // =========================
            //worksheet.RangeUsed ().Style.Protection.Locked = false;
            // worksheet.Row (1).Style.Protection.Locked = true;

            //worksheet.Protect ("lock");
            //listSheet.Protect ("lock");
            //listSheet.Visibility = XLWorksheetVisibility.VeryHidden;

            worksheet.Columns().AdjustToContents();

            workbook.SaveAs(filePath);
            Debug.WriteLine("=== EXPORT FINISHED ===");

            new MyMessageBox
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                MessageTitle = { Text = "IZVOZ U EXCEL" },
                MessageText = { Text = "Izvoz tabele Artikli je uspješno završen." }
            }.ShowDialog();
        }



        public async Task ImportExcelToSQLiteAsync(string excelFilePath)
        {
            using var workbook = new XLWorkbook(excelFilePath);
            var worksheet = workbook.Worksheets.First();

            using var db = new AppDbContext();

            var productsFromJson = JsonSerializer.Deserialize<List<ProductDefinition>>(
                File.ReadAllText("products.json")
            )!;

            var imageMapper = new ProductImageMapper(productsFromJson);

            var artikliList = worksheet.RowsUsed()
                .Skip(1)
                .Select((row, index) =>
                {
                    int rb = index + 1;

                    string artikl = row.Cell(1).GetValue<string>();
                    decimal normativ = row.Cell(4).GetValue<decimal>();

                    string? slika = imageMapper.ResolveImage(artikl);

                    return new TblArtikli
                    {
                        Sifra = rb.ToString(),
                        InternaSifra = rb.ToString(),
                        Artikl = artikl,
                        JedinicaMjere = MapJedinicaMjere(row.Cell(5).GetValue<string>()),
                        Cijena = row.Cell(3).GetValue<decimal>(),
                        Normativ = normativ,
                        VrstaArtikla = MapVrstaArtikla(row.Cell(2).GetValue<string>()),
                        PoreskaStopa = Settings.Default.PDV == "DA" ? 2 : 0,

                        Slika = slika ?? "placeholder.png",

                        Kategorija = null,
                        Pozicija = rb,
                        ArtiklNormativ = normativ != 1 ? $"{artikl} {normativ}" : artikl,
                        Aktivan = true
                    };
                })
                .ToList();

            await db.Artikli.AddRangeAsync(artikliList);
            await db.SaveChangesAsync();
        }

        public class ProductDefinition
        {
            public string Key { get; set; } = null!;
            public string Category { get; set; } = null!;
        }


        /*
        public async Task ImportExcelToSQLiteAsync(string excelFilePath)
        {
            using var workbook = new XLWorkbook (excelFilePath);
            var worksheet = workbook.Worksheets.First ();

            using var db = new AppDbContext ();

            var artikliList = worksheet.RowsUsed ()
                .Skip (1)
                .Select ((row, index) =>
                {
                    int rb = index + 1;

                    string artikl = row.Cell (1).GetValue<string> ();
                    decimal normativ = row.Cell (4).GetValue<decimal> ();

                    return new TblArtikli
                    {
                        Sifra = rb.ToString (),
                        InternaSifra = rb.ToString (),
                        Artikl = artikl,
                        JedinicaMjere = MapJedinicaMjere (row.Cell (5).GetValue<string> ()),
                        Cijena = row.Cell (3).GetValue<decimal> (),
                        Normativ = normativ,
                        VrstaArtikla = MapVrstaArtikla (row.Cell (2).GetValue<string> ()),

                        PoreskaStopa = Settings.Default.PDV == "DA" ? 2 : 0,
                        Slika = "placeholder.png",
                        Kategorija = null,
                        Pozicija = rb,
                        ArtiklNormativ = normativ != 1 ? $"{artikl} {normativ}" : artikl,
                        PrikazatiNaDispleju = "DA"
                    };
                })
                .ToList ();

            await db.Artikli.AddRangeAsync (artikliList);
            await db.SaveChangesAsync ();

            new MyMessageBox
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                MessageTitle = { Text = "UVOZ EXCEL" },
                MessageText = { Text = "Uvoz artikala iz Excel fajla je završen!" }
            }.ShowDialog ();
        }
        */

        private static int? MapVrstaArtikla(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim().ToLower() switch
            {
                "Piće" => 0,
                "Hrana" => 1,
                "Ostalo" => 2,
                _ => null
            };
        }

        private static int? MapJedinicaMjere(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim().ToLower() switch
            {
                "kom" => 1,
                "kg" => 2,
                "m" => 3,
                "m2" => 4,
                "m3" => 5,
                "lit" => 6,
                "tona" => 7,
                "g" => 8,
                "por" => 9,
                "pak" => 10,
                _ => null
            };
        }


        private void btnKategorija_Click(object sender, RoutedEventArgs e)
        {
            var page = new CategoriesPage();
            page.DataContext = new CategoriesViewModel();
            PageNavigator.NavigateWithFade(page);
        }

       
    }
}
