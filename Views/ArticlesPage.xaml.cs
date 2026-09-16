using Caupo.ArticleImport;
using Caupo.Data;
using Caupo.Helpers;
using Caupo.Properties;
using Caupo.ViewModels;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
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
            ArticleTransferGrid.IsVisibleChanged += (s, e) => UpdateOverlayBlur();
            ArticleImportMapperGrid.IsVisibleChanged += (s, e) => UpdateOverlayBlur();

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
        private ArticleImportSourceData? _articleImportSource;
        private List<ArticleImportMapping>? _articleImportMappings;

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
            decimal? normativ = ParseArticleNormativ(normativText);

            if (!normativ.HasValue || normativ == 1m)
                return name;

            return $"{name} {normativ.Value.ToString("0.00", CultureInfo.InvariantCulture)}";
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
            bool overlayVisible = BatchEditGrid.Visibility == Visibility.Visible || ArticleEditGrid.Visibility == Visibility.Visible || ArticleTransferGrid.Visibility == Visibility.Visible || ArticleImportMapperGrid.Visibility == Visibility.Visible;
            MainContent.Effect = overlayVisible ? new BlurEffect { Radius = 8 } : null;
        }

        private void BtnArticleImportMapperClose_Click(object sender, RoutedEventArgs e)
        {
            ArticleImportMapperGrid.Visibility = Visibility.Collapsed;
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

        private readonly ObservableCollection<ArticleTransferRow> _articleTransferRows = new();
        private readonly ObservableCollection<ArticleTransferError> _articleTransferErrors = new();
        private bool _articleTransferValidated;

        

     

        public sealed class ArticleTransferError
        {
            public string Type { get; set; } = "Greška";
            public int RowNumber { get; set; }
            public string ColumnName { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ArticlesViewModel viewModel)
                return;

            LoadTransferRowsFromCaupo(viewModel);
            OpenArticleTransferEditor("Izvoz artikala", "Artikli su učitani iz Caupa. Možete ih urediti, validirati i zatim izvesti u XLSX.");
        }

        /*  private void BtnImport_Click(object sender, RoutedEventArgs e)
          {
              OpenArticleTransferEditor("Uvoz artikala", "Uvezite XLSX fajl. Podaci se prvo učitavaju u privremeni editor i ne mijenjaju bazu dok ne kliknete Primijeni u Caupo.");
              BtnTransferLoadFile_Click(sender, e);
          }*/

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Title = "Uvezi artikle",
                Filter = "Podržane datoteke (*.xlsx;*.csv)|*.xlsx;*.csv|Excel datoteke (*.xlsx)|*.xlsx|CSV datoteke (*.csv)|*.csv",
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                string extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();

                ArticleImportSourceData source = extension switch
                {
                    ".xlsx" => ExcelArticleImportReader.Read(dialog.FileName),
                    ".csv" => CsvArticleImportReader.Read(dialog.FileName),
                    _ => throw new NotSupportedException($"Format {extension} nije podržan.")
                };

                _articleImportSource = source;

                if (ArticleImportFormatDetector.IsCaupoArticleFormat(source))
                {
                    LoadCaupoArticleImport(source);
                    return;
                }

                _articleImportMappings = ArticleImportMapper.CreateMappings();

                LoadArticleImportMapperColumns(source);

                ArticleImportMapperTitle.Text = $"Mapiranje kolona — {source.SourceName}";
                ArticleImportMapperSummary.Text = $"Izvor: {source.TableName} · Kolona: {source.Columns.Count} · Redova: {source.Rows.Count}";
                ArticleImportMapperGrid.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ARTICLE IMPORT] Greška: {ex}");
                ShowArticleImportMapperMessage("Datoteku nije moguće učitati.");
            }
        }

        private async void LoadCaupoArticleImport(ArticleImportSourceData source)
        {
            ArticlesViewModel viewModel = (ArticlesViewModel)DataContext;

            _articleTransferRows.Clear();

            foreach (ArticleImportSourceRow row in source.Rows)
            {
                _articleTransferRows.Add(new ArticleTransferRow(viewModel.Kategorije)
                {
                    IdArtikla = int.TryParse(row.GetString("IdArtikla"), out int id) ? id : 0,
                    Sifra = row.GetString("Sifra"),
                    InternaSifra = row.GetString("InternaSifra"),
                    Artikl = row.GetString("Artikl"),
                    Cijena = row.GetString("Cijena"),
                    Vrsta = ResolveImportVrsta(row.GetString("VrstaArtikla")),
                    Kategorija = ResolveImportKategorija(row.GetString("Kategorija"), viewModel),
                    Jedinica = ResolveImportJedinica(row.GetString("JedinicaMjere"), viewModel),
                    Porez = ResolveImportPorez(row.GetString("PoreskaStopa"), viewModel),
                    Normativ = row.GetString("Normativ"),
                    Pozicija = row.GetString("Pozicija"),
                    Aktivan = ParseTransferBool(row.GetString("Aktivan"), true),
                    PorezNaPotrosnju = ParseTransferBool(row.GetString("PorezNaPotrosnju"), false),
                    Slika = row.GetString("Slika")
                });
            }

            Debug.WriteLine($"[ARTICLE IMPORT] Prepoznat Caupo format. Učitano redova: {_articleTransferRows.Count}");

            OpenArticleTransferEditor(
                "Uvoz artikala",
                $"Caupo format · Učitano {_articleTransferRows.Count} artikala. Pregledajte podatke prije validacije i spremanja."
            );
            
        }

        private static string ResolveImportVrsta(string value)
        {
            return value switch
            {
                "0" => "Piće",
                "1" => "Hrana",
                "2" => "Ostalo",
                _ => value
            };
        }

        private static string ResolveImportJedinica(string value, ArticlesViewModel viewModel)
        {
            if (!int.TryParse(value, out int id))
                return value;

            return viewModel.JediniceMjere.FirstOrDefault(x => x.IdJedinice == id)?.ToString() ?? value;
        }

        private static string ResolveImportPorez(string value, ArticlesViewModel viewModel)
        {
            if (!int.TryParse(value, out int id))
                return value;

            return viewModel.PoreskeStope.FirstOrDefault(x => x.IdStope == id)?.ToString() ?? value;
        }

        private static string ResolveImportKategorija(string value, ArticlesViewModel viewModel)
        {
            if (!int.TryParse(value, out int id))
                return value;

            return viewModel.Kategorije.FirstOrDefault(x => x.IdKategorije == id)?.ToString() ?? value;
        }

        private List<ArticleImportMappingOption> CreateArticleImportMappingOptions(ArticleImportSourceData source)
        {
            List<ArticleImportMappingOption> result = new();

            foreach (ArticleImportColumnPreview column in CreateArticleImportColumnPreviews(source))
            {
                result.Add(new ArticleImportMappingOption
                {
                    MappingType = ArticleImportMappingType.SourceColumn,
                    DisplayName = column.ColumnName,
                    SourceColumn = column.ColumnName,
                    Example1 = column.Example1,
                    Example2 = column.Example2,
                    Example3 = column.Example3
                });
            }

            return result;
        }

        private void ArticleImportMapperComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_articleImportSource == null || _articleImportSource.Rows.Count == 0 || sender is not ComboBox comboBox)
                return;

            string? columnName = (comboBox.SelectedItem as ArticleImportMappingOption)?.SourceColumn;

            if (string.IsNullOrWhiteSpace(columnName))
                return;

            string example = _articleImportSource.Rows[0].GetString(columnName);

            TextBlock? exampleText = comboBox.Name switch
            {
                nameof(ImportCodeComboBox) => ImportCodeExampleText,
                nameof(ImportInternalCodeComboBox) => ImportInternalCodeExampleText,
                nameof(ImportNameComboBox) => ImportNameExampleText,
                nameof(ImportPriceComboBox) => ImportPriceExampleText,
                nameof(ImportTypeComboBox) => ImportTypeExampleText,
                nameof(ImportCategoryComboBox) => ImportCategoryExampleText,
                nameof(ImportUnitComboBox) => ImportUnitExampleText,
                nameof(ImportTaxComboBox) => ImportTaxExampleText,
                nameof(ImportNormativComboBox) => ImportNormativExampleText,
                nameof(ImportPositionComboBox) => ImportPositionExampleText,
                nameof(ImportActiveComboBox) => ImportActiveExampleText,
                nameof(ImportConsumptionTaxComboBox) => ImportConsumptionTaxExampleText,
                nameof(ImportImageComboBox) => ImportImageExampleText,
                _ => null
            };

            if (exampleText != null)
                exampleText.Text = example;

            string? targetColumn = comboBox.Name switch
            {
                nameof(ImportCodeComboBox) => "Šifra",
                nameof(ImportInternalCodeComboBox) => "Interna šifra",
                nameof(ImportNameComboBox) => "Naziv",
                nameof(ImportPriceComboBox) => "Cijena",
                nameof(ImportTypeComboBox) => "Vrsta",
                nameof(ImportCategoryComboBox) => "Kategorija",
                nameof(ImportUnitComboBox) => "Jedinica",
                nameof(ImportTaxComboBox) => "Porez",
                nameof(ImportNormativComboBox) => "Normativ",
                nameof(ImportPositionComboBox) => "Pozicija",
                nameof(ImportActiveComboBox) => "Aktivan",
                nameof(ImportConsumptionTaxComboBox) => "Porez na potrošnju",
                nameof(ImportImageComboBox) => "Slika",
                _ => null
            };

            if (string.IsNullOrWhiteSpace(targetColumn) || _articleImportMappings == null)
                return;

            ArticleImportMapping? mapping = _articleImportMappings.FirstOrDefault(x => x.TargetColumn == targetColumn);

            if (mapping != null)
                ArticleImportMapper.MapSourceColumn(mapping, columnName);
        }

        private List<ArticleImportColumnPreview> CreateArticleImportColumnPreviews(ArticleImportSourceData source)
        {
            List<ArticleImportColumnPreview> result = new();

            foreach (string columnName in source.Columns)
            {
                List<string> examples = source.Rows
                    .Select(x => x.GetString(columnName))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Take(3)
                    .ToList();

                result.Add(new ArticleImportColumnPreview
                {
                    ColumnName = columnName,
                    Example1 = examples.ElementAtOrDefault(0) ?? string.Empty,
                    Example2 = examples.ElementAtOrDefault(1) ?? string.Empty,
                    Example3 = examples.ElementAtOrDefault(2) ?? string.Empty
                });
            }

            return result;
        }
        private void LoadArticleImportMapperColumns(ArticleImportSourceData source)
        {
            List<ArticleImportMappingOption> options = CreateArticleImportMappingOptions(source);

            ComboBox[] comboBoxes =
                    {
                ImportCodeComboBox,
                ImportInternalCodeComboBox,
                ImportNameComboBox,
                ImportPriceComboBox,
                ImportTypeComboBox,
                ImportCategoryComboBox,
                ImportUnitComboBox,
                ImportTaxComboBox,
                ImportNormativComboBox,
                ImportPositionComboBox,
                ImportActiveComboBox,
                ImportConsumptionTaxComboBox,
                ImportImageComboBox
            };

            foreach (ComboBox comboBox in comboBoxes)
            {
                comboBox.ItemsSource = options;
                comboBox.SelectedValuePath = nameof(ArticleImportMappingOption.SourceColumn);
                comboBox.SelectedIndex = -1;
            }



            TextBlock[] exampleTexts =
                    {
                ImportCodeExampleText,
                ImportInternalCodeExampleText,
                ImportNameExampleText,
                ImportPriceExampleText,
                ImportTypeExampleText,
                ImportCategoryExampleText,
                ImportUnitExampleText,
                ImportTaxExampleText,
                ImportNormativExampleText,
                ImportPositionExampleText,
                ImportActiveExampleText,
                ImportConsumptionTaxExampleText,
                ImportImageExampleText
            };

            foreach (TextBlock exampleText in exampleTexts)
                exampleText.Text = string.Empty;
        }

        private void BtnArticleImportMapperContinue_Click(object sender, RoutedEventArgs e)
        {
            if (_articleImportSource == null || _articleImportMappings == null)
                return;

            ArticleImportMapping? nameMapping = _articleImportMappings.FirstOrDefault(x => x.TargetColumn == "Naziv");

            if (nameMapping == null || nameMapping.MappingType != ArticleImportMappingType.SourceColumn || string.IsNullOrWhiteSpace(nameMapping.SourceColumn))
            {
                ShowArticleImportMapperMessage("Za uvoz artikala morate odabrati podatak koji predstavlja naziv artikla.");
                return;
            }

            Debug.WriteLine($"[ARTICLE IMPORT] Naziv ← {nameMapping.SourceColumn}");
            Debug.WriteLine("[ARTICLE IMPORT] Osnovna provjera mapiranja prošla.");

            List<ArticleImportMappedRow> mappedRows = new();

            ArticleImportMapping? taxMapping = _articleImportMappings.FirstOrDefault(x => x.TargetColumn == "Porez");

            TblPoreskeStope? highestTax = null;

            if (taxMapping == null || taxMapping.MappingType == ArticleImportMappingType.None)
            {
                highestTax = ((ArticlesViewModel)DataContext).PoreskeStope
                    .Where(x => x.Aktivna)
                    .OrderByDescending(x => x.Postotak ?? 0)
                    .FirstOrDefault();
            }

            foreach (ArticleImportSourceRow sourceRow in _articleImportSource.Rows)
            {
                ArticleImportMappedRow mappedRow = ArticleImportMapper.MapRow(sourceRow, _articleImportMappings);

                ArticleImportDefaults.Apply(mappedRow, _articleImportMappings);

                if (highestTax != null)
                    mappedRow.Values["Porez"] = highestTax.Opis ?? string.Empty;

                mappedRows.Add(mappedRow);
            }

            ArticlesViewModel viewModel = (ArticlesViewModel)DataContext;

            ArticleImportMapping? codeMapping = _articleImportMappings.FirstOrDefault(x => x.TargetColumn == "Šifra");
            ArticleImportMapping? internalCodeMapping = _articleImportMappings.FirstOrDefault(x => x.TargetColumn == "Interna šifra");
            ArticleImportMapping? positionMapping = _articleImportMappings.FirstOrDefault(x => x.TargetColumn == "Pozicija");
            ArticleImportMapping? priceMapping = _articleImportMappings.FirstOrDefault(x => x.TargetColumn == "Cijena");
            ArticleImportMapping? typeMapping = _articleImportMappings.FirstOrDefault(x => x.TargetColumn == "Vrsta");
            ArticleImportMapping? categoryMapping = _articleImportMappings.FirstOrDefault(x => x.TargetColumn == "Kategorija");
            ArticleImportMapping? unitMapping = _articleImportMappings.FirstOrDefault(x => x.TargetColumn == "Jedinica");
            ArticleImportMapping? normativMapping = _articleImportMappings.FirstOrDefault(x => x.TargetColumn == "Normativ");

            bool generateCode = codeMapping == null || codeMapping.MappingType == ArticleImportMappingType.None;
            bool generateInternalCode = internalCodeMapping == null || internalCodeMapping.MappingType == ArticleImportMappingType.None;
            bool generatePosition = positionMapping == null || positionMapping.MappingType == ArticleImportMappingType.None;

            bool defaultPrice = priceMapping == null || priceMapping.MappingType == ArticleImportMappingType.None;
            bool defaultType = typeMapping == null || typeMapping.MappingType == ArticleImportMappingType.None;
            bool defaultCategory = categoryMapping == null || categoryMapping.MappingType == ArticleImportMappingType.None;
            bool defaultUnit = unitMapping == null || unitMapping.MappingType == ArticleImportMappingType.None;
            bool defaultTax = taxMapping == null || taxMapping.MappingType == ArticleImportMappingType.None;
            bool defaultNormativ = normativMapping == null || normativMapping.MappingType == ArticleImportMappingType.None;

            int nextCode = viewModel.Artikli
                .Select(x => int.TryParse(x.Sifra, out int value) ? value : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            int nextInternalCode = viewModel.Artikli
                .Select(x => int.TryParse(x.InternaSifra, out int value) ? value : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            int nextPosition = viewModel.Artikli
                .Select(x => x.Pozicija ?? 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            foreach (ArticleImportMappedRow row in mappedRows)
            {
                if (generateCode)
                    row.Values["Šifra"] = (nextCode++).ToString();

                if (generateInternalCode)
                    row.Values["Interna šifra"] = (nextInternalCode++).ToString();

                if (generatePosition)
                    row.Values["Pozicija"] = (nextPosition++).ToString();
            }

            Debug.WriteLine($"[ARTICLE IMPORT] Pripremljeno redova: {mappedRows.Count}");

            foreach (ArticleImportMappedRow row in mappedRows.Take(3))
                Debug.WriteLine($"[ARTICLE IMPORT] Red {row.SourceRowNumber}: Šifra={row.GetString("Šifra")}, Interna={row.GetString("Interna šifra")}, Naziv={row.GetString("Naziv")}, Pozicija={row.GetString("Pozicija")}, Porez={row.GetString("Porez")}");

            _articleTransferRows.Clear();

            foreach (ArticleImportMappedRow row in mappedRows)
            {
                var transferRow = new ArticleTransferRow(viewModel.Kategorije)
                {
                    IdArtikla = 0,
                    Sifra = row.GetString("Šifra"),
                    InternaSifra = row.GetString("Interna šifra"),
                    Artikl = row.GetString("Naziv"),
                    Cijena = row.GetString("Cijena"),
                    Vrsta = row.GetString("Vrsta"),
                    Kategorija = row.GetString("Kategorija"),
                    Jedinica = row.GetString("Jedinica"),
                    Porez = row.GetString("Porez"),
                    Normativ = row.GetString("Normativ"),
                    Pozicija = row.GetString("Pozicija"),
                    Aktivan = ParseTransferBool(row.GetString("Aktivan"), true),
                    PorezNaPotrosnju = ParseTransferBool(row.GetString("Porez na potrošnju"), false),
                    Slika = row.GetString("Slika")
                };

                if (defaultPrice)
                    transferRow.DefaultedFields.Add("Cijena");

                if (defaultType)
                    transferRow.DefaultedFields.Add("Vrsta");

                if (defaultCategory)
                    transferRow.DefaultedFields.Add("Kategorija");

                if (defaultUnit)
                    transferRow.DefaultedFields.Add("Jedinica");

                if (defaultTax)
                    transferRow.DefaultedFields.Add("Porez");

                if (defaultNormativ)
                    transferRow.DefaultedFields.Add("Normativ");

                _articleTransferRows.Add(transferRow);
            }

            Debug.WriteLine($"[ARTICLE IMPORT] Pripremljeno redova za editor: {_articleTransferRows.Count}");

            ArticleImportMapperGrid.Visibility = Visibility.Collapsed;

            OpenArticleTransferEditor(
                "Uvoz artikala",
                $"Pripremljeno {_articleTransferRows.Count} artikala. Pregledajte i uredite podatke prije validacije i spremanja."
            );
        }

        private void ShowArticleImportMapperMessage(string message)
        {
            MyMessageBox messageBox = new();
            messageBox.MessageTitle.Text = "Uvoz artikala";
            messageBox.MessageText.Text = message;
            messageBox.ShowDialog();
        }
        private async void OpenArticleTransferEditor(string title, string description)
        {
            ArticleTransferTitle.Text = title;
            ArticleTransferSummary.Text = description;
            ArticleTransferDataGrid.ItemsSource = _articleTransferRows;
            ArticleTransferErrorGrid.ItemsSource = _articleTransferErrors;
            ResetTransferValidation();
            ArticleTransferGrid.Visibility = Visibility.Visible;

            await ValidateArticleTransferAsync();
        }

        private void LoadTransferRowsFromCaupo(ArticlesViewModel viewModel)
        {
            _articleTransferRows.Clear();

            foreach (var article in viewModel.Artikli.OrderBy(x => x.Pozicija).ThenBy(x => x.IdArtikla))
            {
                string vrsta = article.VrstaArtikla switch { 0 => "Piće", 1 => "Hrana", 2 => "Ostalo", _ => string.Empty };
                string kategorija = viewModel.Kategorije.FirstOrDefault(x => x.IdKategorije == article.Kategorija)?.Kategorija ?? string.Empty;
                string jedinica = viewModel.JediniceMjere.FirstOrDefault(x => x.IdJedinice == article.JedinicaMjere)?.ToString() ?? string.Empty;
                string porez = viewModel.PoreskeStope.FirstOrDefault(x => x.IdStope == article.PoreskaStopa)?.ToString() ?? string.Empty;

                _articleTransferRows.Add(new ArticleTransferRow(viewModel.Kategorije)
                {
                    IdArtikla = article.IdArtikla,
                    Sifra = article.Sifra ?? string.Empty,
                    InternaSifra = article.InternaSifra ?? string.Empty,
                    Artikl = article.Artikl ?? string.Empty,
                    Cijena = article.Cijena?.ToString("0.00", CultureInfo.CurrentCulture) ?? string.Empty,
                    Vrsta = vrsta,
                    Kategorija = kategorija,
                    Jedinica = jedinica,
                    Porez = porez,
                    Normativ = article.Normativ?.ToString(CultureInfo.InvariantCulture) ?? "1",
                    Pozicija = article.Pozicija?.ToString() ?? string.Empty,
                    Aktivan = article.Aktivan,
                    PorezNaPotrosnju = article.PorezNaPotrosnju,
                    Slika = article.Slika ?? string.Empty
                });
            }
        }

        private void BtnTransferLoadFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog { Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*" };
            if (dialog.ShowDialog() != true)
                return;

            try
            {
                using var workbook = new XLWorkbook(dialog.FileName);
                var sheet = workbook.Worksheets.First();
                var headers = sheet.Row(1).CellsUsed().ToDictionary(x => x.GetString().Trim(), x => x.Address.ColumnNumber, StringComparer.OrdinalIgnoreCase);

                string[] required = { "Šifra", "Interna šifra", "Naziv", "Cijena", "Vrsta", "Kategorija", "Jedinica", "Porez", "Normativ", "Pozicija", "Aktivan" };
                var missing = required.Where(x => !headers.ContainsKey(x)).ToList();
                if (missing.Count > 0)
                {
                    ShowArticleEditMessage("GREŠKA", "Fajl nema potrebne kolone: " + string.Join(", ", missing));
                    return;
                }

                _articleTransferRows.Clear();
                foreach (var row in sheet.RowsUsed().Skip(1))
                {
                    if (row.CellsUsed().All(x => string.IsNullOrWhiteSpace(x.GetString())))
                        continue;

                    int id = 0;
                    if (headers.TryGetValue("ID", out int idCol))
                        int.TryParse(row.Cell(idCol).GetString(), out id);

                    _articleTransferRows.Add(new ArticleTransferRow(((ArticlesViewModel)DataContext).Kategorije)
                    {
                        IdArtikla = id,
                        Sifra = row.Cell(headers["Šifra"]).GetString().Trim(),
                        InternaSifra = row.Cell(headers["Interna šifra"]).GetString().Trim(),
                        Artikl = row.Cell(headers["Naziv"]).GetString().Trim(),
                        Cijena = row.Cell(headers["Cijena"]).GetFormattedString().Trim(),
                        Vrsta = row.Cell(headers["Vrsta"]).GetString().Trim(),
                        Kategorija = row.Cell(headers["Kategorija"]).GetString().Trim(),
                        Jedinica = row.Cell(headers["Jedinica"]).GetString().Trim(),
                        Porez = row.Cell(headers["Porez"]).GetString().Trim(),
                        Normativ = row.Cell(headers["Normativ"]).GetFormattedString().Trim(),
                        Pozicija = row.Cell(headers["Pozicija"]).GetFormattedString().Trim(),
                        Aktivan = ParseTransferBool(row.Cell(headers["Aktivan"]).GetFormattedString(), true),
                        PorezNaPotrosnju = headers.TryGetValue("Porez na potrošnju", out int potrosnjaCol) && ParseTransferBool(row.Cell(potrosnjaCol).GetFormattedString(), false)
                    });
                }

                ArticleTransferTitle.Text = "Uvoz artikala";
                ArticleTransferSummary.Text = $"Učitano iz: {Path.GetFileName(dialog.FileName)}. Podaci još nisu upisani u bazu.";
                ResetTransferValidation();
                ArticleTransferFooter.Text = $"Učitano redova: {_articleTransferRows.Count}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ARTICLES] Import editor load: " + ex);
                ShowArticleEditMessage("GREŠKA", "Fajl nije moguće učitati. Provjerite da li je ispravan XLSX fajl.");
            }
        }

        private void BtnTransferSaveFile_Click(object sender, RoutedEventArgs e)
        {
            ArticleTransferExportPopup.IsOpen = !ArticleTransferExportPopup.IsOpen;
        }

        private void BtnTransferExportXlsx_Click(object sender, RoutedEventArgs e)
        {
            ArticleTransferExportPopup.IsOpen = false;
            ArticleTransferDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            ArticleTransferDataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            SaveFileDialog dialog = new()
            {
                Title = "Izvezi artikle u Excel",
                Filter = "Excel datoteka (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                AddExtension = true,
                FileName = "Caupo_Artikli.xlsx"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                ExportArticlesToXlsx(dialog.FileName);
                ArticleTransferFooter.Text = $"Izvezeno {_articleTransferRows.Count} artikala u {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ARTICLES] XLSX export: " + ex);
                ShowArticleEditMessage("GREŠKA", "XLSX fajl nije spremljen.");
            }
        }

        private void BtnTransferExportCsv_Click(object sender, RoutedEventArgs e)
        {
            ArticleTransferExportPopup.IsOpen = false;
            ArticleTransferDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            ArticleTransferDataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            SaveFileDialog dialog = new()
            {
                Title = "Izvezi artikle u CSV",
                Filter = "CSV datoteka (*.csv)|*.csv",
                DefaultExt = ".csv",
                AddExtension = true,
                FileName = "Caupo_Artikli.csv"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                ExportArticlesToCsv(dialog.FileName);
                ArticleTransferFooter.Text = $"Izvezeno {_articleTransferRows.Count} artikala u {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ARTICLES] CSV export: " + ex);
                ShowArticleEditMessage("GREŠKA", "CSV fajl nije spremljen.");
            }
        }

        private void ExportArticlesToXlsx(string filePath)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Artikli");

            string[] headers = { "IdArtikla", "Sifra", "InternaSifra", "Artikl", "Cijena", "VrstaArtikla", "Kategorija", "JedinicaMjere", "PoreskaStopa", "Normativ", "Pozicija", "Aktivan", "PorezNaPotrosnju", "Slika" };

            for (int i = 0; i < headers.Length; i++)
                sheet.Cell(1, i + 1).Value = headers[i];

            int r = 2;

            foreach (var item in _articleTransferRows)
            {
                sheet.Cell(r, 1).Value = item.IdArtikla == 0 ? string.Empty : item.IdArtikla.ToString();
                sheet.Cell(r, 2).Value = item.Sifra;
                sheet.Cell(r, 3).Value = item.InternaSifra;
                sheet.Cell(r, 4).Value = item.Artikl;
                sheet.Cell(r, 5).Value = item.Cijena;
                sheet.Cell(r, 6).Value = item.Vrsta;
                sheet.Cell(r, 7).Value = item.Kategorija;
                sheet.Cell(r, 8).Value = item.Jedinica;
                sheet.Cell(r, 9).Value = item.Porez;
                sheet.Cell(r, 10).Value = item.Normativ;
                sheet.Cell(r, 11).Value = item.Pozicija;
                sheet.Cell(r, 12).Value = item.Aktivan ? "DA" : "NE";
                sheet.Cell(r, 13).Value = item.PorezNaPotrosnju ? "DA" : "NE";
                sheet.Cell(r, 14).Value = item.Slika;
                r++;
            }

            sheet.SheetView.FreezeRows(1);
            sheet.RangeUsed().SetAutoFilter();
            sheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }


        private void ExportArticlesToCsv(string filePath)
        {
            using StreamWriter writer = new(filePath, false, new UTF8Encoding(true));

            string[] headers = { "IdArtikla", "Sifra", "InternaSifra", "Artikl", "Cijena", "VrstaArtikla", "Kategorija", "JedinicaMjere", "PoreskaStopa", "Normativ", "Pozicija", "Aktivan", "PorezNaPotrosnju", "Slika" };

            writer.WriteLine(string.Join(";", headers.Select(EscapeCsvValue)));

            foreach (var item in _articleTransferRows)
            {
                string[] values =
                {
            item.IdArtikla == 0 ? string.Empty : item.IdArtikla.ToString(),
            item.Sifra,
            item.InternaSifra,
            item.Artikl,
            item.Cijena,
            item.Vrsta,
            item.Kategorija,
            item.Jedinica,
            item.Porez,
            item.Normativ,
            item.Pozicija,
            item.Aktivan ? "DA" : "NE",
            item.PorezNaPotrosnju ? "DA" : "NE",
            item.Slika
        };

                writer.WriteLine(string.Join(";", values.Select(EscapeCsvValue)));
            }
        }

        private static string EscapeCsvValue(string? value)
        {
            string text = value ?? string.Empty;

            if (text.Contains('"'))
                text = text.Replace("\"", "\"\"");

            if (text.Contains(';') || text.Contains('"') || text.Contains('\r') || text.Contains('\n'))
                text = $"\"{text}\"";

            return text;
        }

        private async void BtnTransferValidate_Click(object sender, RoutedEventArgs e)
        {
            await ValidateArticleTransferAsync();
        }

        private async Task<bool> ValidateArticleTransferAsync()
        {
            //ArticleTransferDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            //ArticleTransferDataGrid.CommitEdit(DataGridEditingUnit.Row, true);
            _articleTransferErrors.Clear();

            if (DataContext is not ArticlesViewModel viewModel)
                return false;

            await using var db = new AppDbContext();
            var dbArticles = await db.Artikli.AsNoTracking().ToListAsync();
            int rowNumber = 1;

            foreach (var row in _articleTransferRows)
            {
                rowNumber++;

                row.ErrorFields.Clear();
                row.WarningFields.Clear();

                void Error(string field, string message)
                {
                    row.ErrorFields.Add(field);
                    _articleTransferErrors.Add(new ArticleTransferError { RowNumber = rowNumber, ColumnName = field, Message = message });
                }

                if (string.IsNullOrWhiteSpace(row.Sifra)) Error("Šifra", "Šifra artikla nije unesena.");
                if (string.IsNullOrWhiteSpace(row.InternaSifra)) Error("Interna šifra", "Interna šifra nije unesena.");
                if (string.IsNullOrWhiteSpace(row.Artikl)) Error("Naziv", "Naziv artikla nije unesen.");
                if (!TryParseArticlePrice(row.Cijena, out decimal cijena) || cijena <= 0) Error("Cijena", $"Artikl '{row.Artikl}' mora imati cijenu veću od 0.");

                int? typeId = MapTransferType(row.Vrsta);
                if (typeId == null) Error("Vrsta", $"Za artikl '{row.Artikl}' izaberite Piće, Hrana ili Ostalo.");

                var category = typeId == null ? null : viewModel.Kategorije.FirstOrDefault(x => x.VrstaArtikla == typeId && string.Equals(x.Kategorija?.Trim(), row.Kategorija?.Trim(), StringComparison.OrdinalIgnoreCase));
                if (category == null) Error("Kategorija", $"Kategorija '{row.Kategorija}' ne pripada izabranoj vrsti artikla.");

                var unit = viewModel.JediniceMjere.FirstOrDefault(x => string.Equals(x.ToString().Trim(), row.Jedinica?.Trim(), StringComparison.OrdinalIgnoreCase));
                if (unit == null) Error("Jedinica", $"Jedinica mjere '{row.Jedinica}' ne postoji u Caupu.");

                var tax = viewModel.PoreskeStope.FirstOrDefault(x => string.Equals(x.ToString().Trim(), row.Porez?.Trim(), StringComparison.OrdinalIgnoreCase));
                if (tax == null) Error("Porez", $"Poreska stopa '{row.Porez}' ne postoji ili nije aktivna.");

                decimal? normativ = ParseArticleNormativ(row.Normativ);

                if (typeId == 0)
                {
                    if (!normativ.HasValue || normativ <= 0)
                        Error("Normativ", $"Artikl '{row.Artikl}' mora imati ispravan normativ veći od 0.");
                    else if (unit != null && string.Equals(unit.ToString().Trim(), "kom", StringComparison.OrdinalIgnoreCase) && normativ != 1m)
                        Error("Normativ", $"Artikl '{row.Artikl}' ima jedinicu mjere 'kom', ali normativ nije 1. Provjerite jedinicu mjere ili normativ.");
                }
                else if (typeId == 1 || typeId == 2)
                {
                    if (!normativ.HasValue || normativ != 1m)
                        Error("Normativ", $"Artikl '{row.Artikl}' vrste '{row.Vrsta}' mora imati normativ 1.");
                }

                if (!int.TryParse(row.Pozicija, out int pozicija) || pozicija <= 0) Error("Pozicija", $"Artikl '{row.Artikl}' mora imati ispravnu poziciju.");

                string artiklNormativ = BuildArticleNormativ(row.Artikl, typeId == 0 ? row.Normativ : "1");

                var duplicateCode = dbArticles.FirstOrDefault(x => x.IdArtikla != row.IdArtikla && string.Equals(x.Sifra?.Trim(), row.Sifra?.Trim(), StringComparison.OrdinalIgnoreCase));
                if (duplicateCode != null) Error("Šifra", $"Šifra '{row.Sifra}' već pripada artiklu '{duplicateCode.Artikl}'.");

                var duplicateInternal = dbArticles.FirstOrDefault(x => x.IdArtikla != row.IdArtikla && string.Equals(x.InternaSifra?.Trim(), row.InternaSifra?.Trim(), StringComparison.OrdinalIgnoreCase));
                if (duplicateInternal != null) Error("Interna šifra", $"Interna šifra '{row.InternaSifra}' već pripada artiklu '{duplicateInternal.Artikl}'.");

                var duplicateNorm = dbArticles.FirstOrDefault(x => x.IdArtikla != row.IdArtikla && string.Equals(x.ArtiklNormativ?.Trim(), artiklNormativ.Trim(), StringComparison.OrdinalIgnoreCase));
                if (duplicateNorm != null) Error("Naziv / Normativ", $"Naziv za prodaju '{artiklNormativ}' već postoji kod artikla '{duplicateNorm.Artikl}'.");

                if (row.IdArtikla > 0)
                {
                    var original = dbArticles.FirstOrDefault(x => x.IdArtikla == row.IdArtikla);

                    if (original == null)
                        Error("ID", $"Artikl ID {row.IdArtikla} više ne postoji u bazi.");
                    else if (!string.Equals(original.Artikl, row.Artikl, StringComparison.Ordinal) && await viewModel.HasArticleBeenSold(original.Sifra))
                        Error("Naziv", $"Naziv artikla '{original.Artikl}' se ne može mijenjati jer je artikl već prodavan.");
                }

                void Warning(string field, string message)
                {
                    row.WarningFields.Add(field);
                    _articleTransferErrors.Add(new ArticleTransferError { Type = "Upozorenje", RowNumber = rowNumber, ColumnName = field, Message = message });
                }

                if (row.DefaultedFields.Contains("Cijena"))
                    Warning("Cijena", $"Za artikl '{row.Artikl}' cijena nije pronađena u izvoru. Caupo je automatski postavio {row.Cijena}.");

                if (row.DefaultedFields.Contains("Vrsta"))
                    Warning("Vrsta", $"Za artikl '{row.Artikl}' vrsta nije pronađena u izvoru. Caupo je automatski postavio '{row.Vrsta}'.");

                if (row.DefaultedFields.Contains("Kategorija"))
                    Warning("Kategorija", $"Za artikl '{row.Artikl}' kategorija nije pronađena u izvoru. Caupo je automatski postavio '{row.Kategorija}'.");

                if (row.DefaultedFields.Contains("Jedinica"))
                    Warning("Jedinica", $"Za artikl '{row.Artikl}' jedinica mjere nije pronađena u izvoru. Caupo je automatski postavio '{row.Jedinica}'.");

                if (row.DefaultedFields.Contains("Porez"))
                    Warning("Porez", $"Za artikl '{row.Artikl}' porez nije pronađen u izvoru. Caupo je automatski postavio '{row.Porez}'.");

                if (row.DefaultedFields.Contains("Normativ"))
                    Warning("Normativ", $"Za artikl '{row.Artikl}' normativ nije pronađen u izvoru. Caupo je automatski postavio {row.Normativ}.");

                row.RefreshValidationState();

            }

            foreach (var group in _articleTransferRows.Where(x => !string.IsNullOrWhiteSpace(x.Sifra)).GroupBy(x => x.Sifra.Trim(), StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1))
                _articleTransferErrors.Add(new ArticleTransferError { RowNumber = 0, ColumnName = "Šifra", Message = $"Šifra '{group.Key}' se pojavljuje više puta u editoru." });

            foreach (var group in _articleTransferRows.Where(x => !string.IsNullOrWhiteSpace(x.InternaSifra)).GroupBy(x => x.InternaSifra.Trim(), StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1))
                _articleTransferErrors.Add(new ArticleTransferError { RowNumber = 0, ColumnName = "Interna šifra", Message = $"Interna šifra '{group.Key}' se pojavljuje više puta u editoru." });

            foreach (var group in _articleTransferRows.Where(x => !string.IsNullOrWhiteSpace(x.Artikl)).GroupBy(x => BuildArticleNormativ(x.Artikl, x.Normativ), StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1))
                _articleTransferErrors.Add(new ArticleTransferError { RowNumber = 0, ColumnName = "Naziv / Normativ", Message = $"Naziv za prodaju '{group.Key}' se pojavljuje više puta u editoru." });

            int errorCount = _articleTransferErrors.Count(x => x.Type == "Greška");
            int warningCount = _articleTransferErrors.Count(x => x.Type == "Upozorenje");

            _articleTransferValidated = errorCount == 0;
            BtnTransferApply.IsEnabled = _articleTransferValidated;

            ArticleTransferErrorTitle.Text = $"Greške ({errorCount}) | Upozorenja ({warningCount})";
            ArticleTransferValidationSummary.Text = $"Redova: {_articleTransferRows.Count} | Greške: {errorCount} | Upozorenja: {warningCount}";

            if (errorCount > 0)
                ArticleTransferFooter.Text = "Ispravite navedene greške prije primjene podataka.";
            else if (warningCount > 0)
                ArticleTransferFooter.Text = "Nema grešaka. Pregledajte upozorenja prije primjene podataka.";
            else
                ArticleTransferFooter.Text = "Validacija uspješna. Podaci su spremni za primjenu.";

            return _articleTransferValidated;
        }

        private async void BtnTransferApply_Click(object sender, RoutedEventArgs e)
        {
            if (!await ValidateArticleTransferAsync() || DataContext is not ArticlesViewModel viewModel)
                return;

            try
            {
                await using var db = new AppDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();

                foreach (var row in _articleTransferRows)
                {
                    TblArtikli article;
                    if (row.IdArtikla > 0)
                    {
                        article = await db.Artikli.FirstAsync(x => x.IdArtikla == row.IdArtikla);
                    }
                    else
                    {
                        article = new TblArtikli();
                        db.Artikli.Add(article);
                    }

                    int typeId = MapTransferType(row.Vrsta)!.Value;
                    var category = viewModel.Kategorije.First(x => x.VrstaArtikla == typeId && string.Equals(x.Kategorija?.Trim(), row.Kategorija.Trim(), StringComparison.OrdinalIgnoreCase));
                    var unit = viewModel.JediniceMjere.First(x => string.Equals(x.ToString().Trim(), row.Jedinica.Trim(), StringComparison.OrdinalIgnoreCase));
                    var tax = viewModel.PoreskeStope.First(x => string.Equals(x.ToString().Trim(), row.Porez.Trim(), StringComparison.OrdinalIgnoreCase));
                    TryParseArticlePrice(row.Cijena, out decimal price);
                    decimal normativ = typeId == 0 ? ParseArticleNormativ(row.Normativ)!.Value : 1m;
                    int.TryParse(row.Pozicija, out int position);

                    article.Sifra = row.Sifra.Trim();
                    article.InternaSifra = row.InternaSifra.Trim();
                    article.Artikl = row.Artikl.Trim();
                    article.Cijena = price;
                    article.VrstaArtikla = typeId;
                    article.Kategorija = category.IdKategorije;
                    article.JedinicaMjere = unit.IdJedinice;
                    article.PoreskaStopa = tax.IdStope;
                    article.Normativ = normativ;
                    article.Pozicija = position;
                    article.Aktivan = row.Aktivan;
                    article.PorezNaPotrosnju = string.Equals(Settings.Default.Country, "Hrvatska", StringComparison.OrdinalIgnoreCase) && row.PorezNaPotrosnju;
                    article.ArtiklNormativ = BuildArticleNormativ(article.Artikl, typeId == 0 ? row.Normativ : "1");
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                await viewModel.LoadArticlesAsync();
                ArticleTransferFooter.Text = $"Primijenjeno {_articleTransferRows.Count} artikala. Baza je uspješno ažurirana.";
                LoadTransferRowsFromCaupo(viewModel);
                ResetTransferValidation();
                ShowArticleEditMessage("POTVRDA", "Artikli su uspješno primijenjeni u Caupo.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ARTICLES] Import editor apply: " + ex);
                _articleTransferErrors.Add(new ArticleTransferError { RowNumber = 0, ColumnName = "Baza", Message = "Import nije izvršen. Nijedan artikl nije promijenjen." });
                ArticleTransferErrorTitle.Text = $"Greške ({_articleTransferErrors.Count})";
                ArticleTransferFooter.Text = "Import nije izvršen. Nijedan artikl nije promijenjen.";
                BtnTransferApply.IsEnabled = false;
            }
        }

        private async void ArticleTransferDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            ArticleTransferErrorGrid.IsHitTestVisible = true;
            ArticleTransferErrorGrid.Opacity = 1;

            if (e.EditAction != DataGridEditAction.Commit)
                return;

            if (e.Row.Item is not ArticleTransferRow row)
                return;

            string? field = e.Column.Header?.ToString();

            if (field != null && row.DefaultedFields.Contains(field))
                row.DefaultedFields.Remove(field);

            await Dispatcher.InvokeAsync(async () =>
            {
                if (field == "Cijena" && TryParseArticlePrice(row.Cijena, out decimal cijena))
                    row.Cijena = cijena.ToString("0.00", CultureInfo.InvariantCulture);

                await ValidateArticleTransferAsync();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }


        private void ArticleTransferDataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            ArticleTransferErrorGrid.IsHitTestVisible = false;
            ArticleTransferErrorGrid.Opacity = 0.60;
        }

        private async void ArticleTransferErrorGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ArticleTransferErrorGrid.SelectedItem is not ArticleTransferError error || error.RowNumber < 2)
                return;

            int index = error.RowNumber - 2;
            if (index < 0 || index >= _articleTransferRows.Count)
                return;

            var item = _articleTransferRows[index];

            int columnIndex = error.ColumnName switch
            {
                "ID" => 0,
                "Šifra" => 1,
                "Interna šifra" => 2,
                "Naziv" => 3,
                "Cijena" => 4,
                "Vrsta" => 5,
                "Kategorija" => 6,
                "Jedinica" => 7,
                "Porez" => 8,
                "Normativ" => 9,
                "Naziv / Normativ" => 9,
                "Pozicija" => 10,
                _ => -1
            };

            if (error.ColumnName == "Normativ" || error.ColumnName == "Naziv / Normativ")
                item.EnableNormativCorrection();

            ArticleTransferDataGrid.SelectedItem = item;
            ArticleTransferDataGrid.ScrollIntoView(item);

            if (columnIndex < 0 || columnIndex >= ArticleTransferDataGrid.Columns.Count)
                return;

            var column = ArticleTransferDataGrid.Columns[columnIndex];

            ArticleTransferDataGrid.CurrentCell = new DataGridCellInfo(item, column);
            ArticleTransferDataGrid.ScrollIntoView(item, column);
            ArticleTransferDataGrid.Focus();
            ArticleTransferDataGrid.BeginEdit();

            await Dispatcher.InvokeAsync(() =>
            {
                DataGridCell? cell = GetDataGridCell(ArticleTransferDataGrid, item, columnIndex);
                if (cell == null)
                    return;

                TextBox? textBox = FindVisualChild<TextBox>(cell);
                if (textBox != null)
                {
                    textBox.Focus();
                    Keyboard.Focus(textBox);
                    textBox.SelectAll();
                    return;
                }

                ComboBox? comboBox = FindVisualChild<ComboBox>(cell);
                if (comboBox != null)
                {
                    comboBox.Focus();
                    Keyboard.Focus(comboBox);
                    //comboBox.IsDropDownOpen = true;
                }
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void ArticleTransferComboBox_DropDownClosed(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ArticleTransferDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                ArticleTransferDataGrid.CommitEdit(DataGridEditingUnit.Row, true);
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private static DataGridCell? GetDataGridCell(DataGrid dataGrid, object item, int columnIndex)
        {
            if (dataGrid.ItemContainerGenerator.ContainerFromItem(item) is not DataGridRow row)
                return null;

            DataGridCellsPresenter? presenter = FindVisualChild<DataGridCellsPresenter>(row);
            if (presenter == null)
                return null;

            return presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex) as DataGridCell;
        }

        private void BtnTransferClose_Click(object sender, RoutedEventArgs e)
        {
            ArticleTransferGrid.Visibility = Visibility.Collapsed;
            _articleTransferRows.Clear();
            _articleTransferErrors.Clear();
            ResetTransferValidation();
        }

        private void ResetTransferValidation()
        {
            _articleTransferValidated = false;
            BtnTransferApply.IsEnabled = false;
            _articleTransferErrors.Clear();
            ArticleTransferErrorTitle.Text = "Greške (0)";
            ArticleTransferValidationSummary.Text = "Nije validirano";
        }

        private static int? MapTransferType(string? value)
        {
            return value?.Trim().ToLowerInvariant() switch { "piće" => 0, "pice" => 0, "hrana" => 1, "ostalo" => 2, _ => null };
        }

        private static bool ParseTransferBool(string? value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            return value.Trim().ToLowerInvariant() switch { "da" => true, "true" => true, "1" => true, "ne" => false, "false" => false, "0" => false, _ => defaultValue };
        }

        private void ArticleTransferDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGrid dataGrid)
                return;

            DependencyObject? source = e.OriginalSource as DependencyObject;

            while (source != null && source is not DataGridCell)
                source = VisualTreeHelper.GetParent(source);

            if (source is not DataGridCell cell)
            {
                if (dataGrid.CurrentCell.Item != null && dataGrid.CurrentCell.Column != null)
                {
                    int columnIndex = dataGrid.Columns.IndexOf(dataGrid.CurrentCell.Column);
                    DataGridCell? currentCell = GetDataGridCell(dataGrid, dataGrid.CurrentCell.Item, columnIndex);

                    if (currentCell != null && FindVisualChild<ComboBox>(currentCell) is ComboBox comboBox && comboBox.IsDropDownOpen)
                        comboBox.IsDropDownOpen = false;
                    else
                    {
                        dataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                        dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                    }
                }

                return;
            }

            if (cell.IsEditing || cell.IsReadOnly)
                return;

            if (!cell.IsFocused)
                cell.Focus();

            dataGrid.CurrentCell = new DataGridCellInfo(cell);
            dataGrid.BeginEdit();

            if (FindVisualChild<TextBox>(cell) is TextBox textBox)
            {
                textBox.Focus();
                textBox.CaretIndex = textBox.Text.Length;
            }

            e.Handled = true;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is T result)
                    return result;

                T? nested = FindVisualChild<T>(child);

                if (nested != null)
                    return nested;
            }

            return null;
        }


        private void ArticleTransferDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not DataGrid dataGrid)
                return;

            if (e.Key == Key.Escape)
            {
                dataGrid.CancelEdit(DataGridEditingUnit.Cell);
                dataGrid.CancelEdit(DataGridEditingUnit.Row);

                ArticleTransferErrorGrid.IsHitTestVisible = true;
                ArticleTransferErrorGrid.Opacity = 1;
                ArticleTransferErrorGrid.UnselectAll();
                ArticleTransferErrorGrid.SelectedItem = null;

                e.Handled = true;
                return;
            }

            if (e.Key != Key.Up && e.Key != Key.Down && e.Key != Key.Left && e.Key != Key.Right && e.Key != Key.Enter)
                return;

            // Ako je ComboBox otvoren, njegove strelice ostavljamo njemu.
            if (FindVisualParent<ComboBox>(e.OriginalSource as DependencyObject) is ComboBox openComboBox && openComboBox.IsDropDownOpen)
                return;

            if (dataGrid.CurrentCell.Column == null || dataGrid.CurrentItem == null)
                return;

            int rowIndex = dataGrid.Items.IndexOf(dataGrid.CurrentItem);
            int columnIndex = dataGrid.Columns.IndexOf(dataGrid.CurrentCell.Column);

            if (rowIndex < 0 || columnIndex < 0)
                return;

            int targetRow = rowIndex;
            int targetColumn = columnIndex;

            switch (e.Key)
            {
                case Key.Up:
                    targetRow--;
                    break;

                case Key.Down:
                case Key.Enter:
                    targetRow++;
                    break;

                case Key.Left:
                    targetColumn--;
                    break;

                case Key.Right:
                    targetColumn++;
                    break;
            }

            if (targetRow < 0 || targetRow >= dataGrid.Items.Count)
                return;

            if (targetColumn < 0 || targetColumn >= dataGrid.Columns.Count)
                return;

            object targetItem = dataGrid.Items[targetRow];

            if (targetItem == CollectionView.NewItemPlaceholder)
                return;

            DataGridColumn targetColumnObject = dataGrid.Columns[targetColumn];

            // Završavamo trenutnu izmjenu.
            if (!dataGrid.CommitEdit(DataGridEditingUnit.Cell, true))
                return;

            dataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            // Nova ćelija postaje CurrentCell.
            dataGrid.SelectedItem = targetItem;
            dataGrid.CurrentCell = new DataGridCellInfo(targetItem, targetColumnObject);
            dataGrid.ScrollIntoView(targetItem, targetColumnObject);

            e.Handled = true;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                DataGridCell? cell = GetDataGridCell(dataGrid, targetItem, targetColumnObject);

                if (cell == null || cell.IsReadOnly)
                    return;

                cell.Focus();
                dataGrid.CurrentCell = new DataGridCellInfo(targetItem, targetColumnObject);

                // Odmah ulazimo u edit mode.
                dataGrid.BeginEdit();

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // TEXTBOX
                    if (FindVisualChild<TextBox>(cell) is TextBox textBox)
                    {
                        textBox.Focus();
                        textBox.CaretIndex = textBox.Text.Length;
                        return;
                    }

                    // COMBOBOX
                    if (FindVisualChild<ComboBox>(cell) is ComboBox comboBox)
                    {
                        comboBox.Focus();
                        return;
                    }

                    // CHECKBOX
                    if (FindVisualChild<CheckBox>(cell) is CheckBox checkBox)
                        checkBox.Focus();

                }), System.Windows.Threading.DispatcherPriority.Input);

            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;

                child = VisualTreeHelper.GetParent(child);
            }

            return null;
        }

        private static DataGridCell? GetDataGridCell(DataGrid dataGrid, object item, DataGridColumn column)
        {
            if (dataGrid.ItemContainerGenerator.ContainerFromItem(item) is not DataGridRow row)
                return null;

            DataGridCellsPresenter? presenter = FindVisualChild<DataGridCellsPresenter>(row);

            if (presenter == null)
                return null;

            int columnIndex = dataGrid.Columns.IndexOf(column);

            return presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex) as DataGridCell;
        }
        private void btnKategorija_Click(object sender, RoutedEventArgs e)
        {
            var page = new CategoriesPage();
            page.DataContext = new CategoriesViewModel();
            PageNavigator.NavigateWithFade(page);
        }




    }
}
