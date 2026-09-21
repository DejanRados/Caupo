using Caupo.ArticleImport;
using Caupo.Data;
using Caupo.Helpers;
using Caupo.Properties;
using Caupo.Services;
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
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for ArticlesPage.xaml
    /// </summary>
    public partial class ArticlesPage : UserControl
    {

        private int _articleTransferSelectionAnchorIndex = -1;
        private bool _isDraggingArticleTransferFloatingBar;
        private Point _articleTransferFloatingBarDragStart;
        private Point _articleTransferFloatingBarStartPosition;
        private bool _isEditingArticle;
        private int? _editingArticleId;
        private string? _selectedArticleImagePath;
        private Int32Rect? _selectedArticleImageCrop;
        private int? _selectedArticleImageCropType;
        private ArticleImportSourceData? _articleImportSource;
        private List<ArticleImportMapping>? _articleImportMappings;

       


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


        private readonly ObservableCollection<ArticleTransferRow> _articleTransferRows = new ();
        private readonly ObservableCollection<ArticleTransferError> _articleTransferErrors = new ();
        private bool _articleTransferValidated;


        public sealed class ArticleTransferError
        {
            public string Type { get; set; } = "Greška";
            public int RowNumber { get; set; }
            public string ColumnName { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }


        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for(int i = 0; i < VisualTreeHelper.GetChildrenCount (parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild (parent, i);

                if(child is T result)
                    return result;

                T? nested = FindVisualChild<T> (child);

                if(nested != null)
                    return nested;
            }

            return null;
        }


        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while(child != null)
            {
                if(child is T parent)
                    return parent;

                child = VisualTreeHelper.GetParent (child);
            }

            return null;
        }


        // ============================================================================
        // METODE
        // ============================================================================

        #region INICIJALIZACIJA I VIEWMODEL

        // Inicijalizuje stranicu, ViewModel, prikaz prijavljenog korisnika i praćenje overlay prozora.
        public ArticlesPage()
        {
            this.DataContext = new ArticlesViewModel ();

            InitializeComponent ();
            ConfigureArticleTransferFloatingMenuBar ();
            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik.Radnik;
            BatchEditGrid.IsVisibleChanged += (s, e) => UpdateOverlayBlur ();
            ArticleEditGrid.IsVisibleChanged += (s, e) => UpdateOverlayBlur ();
            ArticleTransferGrid.IsVisibleChanged += (s, e) => UpdateOverlayBlur ();
            ArticleImportMapperGrid.IsVisibleChanged += (s, e) => UpdateOverlayBlur ();

            if(DataContext is ArticlesViewModel viewModel)
            {
                viewModel.PropertyChanged += ArticlesViewModel_PropertyChanged;
                viewModel.ErrorOccurred += ViewModel_ErrorOccurred;
            }
        }

        // Reaguje na promjenu broja označenih artikala i usklađuje stanje glavnog checkboxa i dugmeta za grupno uređivanje.
        private void ArticlesViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName != nameof (ArticlesViewModel.BrojOznacenihArtikala))
                return;

            if(sender is not ArticlesViewModel viewModel)
                return;

            _updatingArticleCheckBoxes = true;
            SelectAllArticlesCheckBox.IsChecked = viewModel.AreAllVisibleArticlesChecked ();
            _updatingArticleCheckBoxes = false;

            UpdateBatchEditButton ();
        }

        // Prikazuje grešku iz ViewModela i označava odgovarajuća polja editora artikla.
        private void ViewModel_ErrorOccurred(object? sender, string? errorMessage)
        {
            if(!string.IsNullOrWhiteSpace (errorMessage))
            {
                if(errorMessage.StartsWith ("Šifra ", StringComparison.OrdinalIgnoreCase) || errorMessage.Contains (", Šifra ", StringComparison.OrdinalIgnoreCase))
                    SetArticleFieldError (ArticleCodeTextBox);

                if(errorMessage.Contains ("Interna šifra", StringComparison.OrdinalIgnoreCase))
                    SetArticleFieldError (ArticleInternalCodeTextBox);

                if(errorMessage.Contains ("Naziv za prodaju", StringComparison.OrdinalIgnoreCase))
                {
                    SetArticleFieldError (ArticleNameTextBox);
                    if(ArticleNormativPanel.Visibility == Visibility.Visible)
                        SetArticleFieldError (ArticleNormativComboBox);
                }
            }

            MyMessageBox myMessageBox = new MyMessageBox
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            myMessageBox.MessageTitle.Text = "GREŠKA";
            myMessageBox.MessageText.Text = errorMessage;
            myMessageBox.ShowDialog ();
        }

        // Uključuje ili uklanja blur glavnog sadržaja zavisno od toga da li je neki overlay editor otvoren.
        private void UpdateOverlayBlur()
        {
            bool overlayVisible = BatchEditGrid.Visibility == Visibility.Visible || ArticleEditGrid.Visibility == Visibility.Visible || ArticleTransferGrid.Visibility == Visibility.Visible || ArticleImportMapperGrid.Visibility == Visibility.Visible;
            MainContent.Effect = overlayVisible ? new BlurEffect { Radius = 8 } : null;
        }

        #endregion


        #region VIRTUALNA TASTATURA

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
        }

        #endregion


        #region NAVIGACIJA STRANICE

        // Vraća korisnika na početnu stranicu.
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var page = new HomePage ();
            page.DataContext = new HomeViewModel ();
            PageNavigator.NavigateWithFade (page);
        }

        // Otvara stranicu za upravljanje kategorijama artikala.
        private void btnKategorija_Click(object sender, RoutedEventArgs e)
        {
            var page = new CategoriesPage ();
            page.DataContext = new CategoriesViewModel ();
            PageNavigator.NavigateWithFade (page);
        }

        #endregion


        #region LISTA ARTIKALA I SELEKCIJA

        // Osigurava da klik na red odmah postavi taj artikl kao selektovani.
        private void ListaArtikala_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject? source = e.OriginalSource as DependencyObject;

            while(source != null && source is not DataGridRow)
                source = VisualTreeHelper.GetParent (source);

            if(source is DataGridRow row && !row.IsSelected)
            {
                row.IsSelected = true;
                ListaArtikala.SelectedItem = row.Item;
                ListaArtikala.CurrentItem = row.Item;
            }
        }

        // Sinhronizuje selektovani red DataGrida sa SelectedArticle u ViewModelu.
        private void ListaArtikala_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(sender is not DataGrid dataGrid || dataGrid.SelectedItem is not DatabaseTables.TblArtikli selectedItem)
                return;

            if(DataContext is ArticlesViewModel viewModel)
                viewModel.SelectedArticle = selectedItem;

            dataGrid.ScrollIntoView (selectedItem);
        }

        // Postavlja stanje checkboxa reda prema selekciji sačuvanoj u ViewModelu.
        private void ArticleCheckBox_Loaded(object sender, RoutedEventArgs e)
        {
            if(sender is not CheckBox checkBox || checkBox.Tag is not DatabaseTables.TblArtikli article)
                return;

            if(DataContext is not ArticlesViewModel viewModel)
                return;

            _updatingArticleCheckBoxes = true;
            checkBox.IsChecked = viewModel.IsArticleChecked (article);
            _updatingArticleCheckBoxes = false;
        }

        // Označava pojedinačni artikl za grupno uređivanje.
        private void ArticleCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if(_updatingArticleCheckBoxes)
                return;

            if(sender is not CheckBox checkBox || checkBox.Tag is not DatabaseTables.TblArtikli article)
                return;

            if(DataContext is not ArticlesViewModel viewModel)
                return;

            viewModel.SetArticleChecked (article, true);
            RefreshSelectAllCheckBox (viewModel);
            UpdateBatchEditButton ();
        }

        // Uklanja pojedinačni artikl iz grupne selekcije.
        private void ArticleCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if(_updatingArticleCheckBoxes)
                return;

            if(sender is not CheckBox checkBox || checkBox.Tag is not DatabaseTables.TblArtikli article)
                return;

            if(DataContext is not ArticlesViewModel viewModel)
                return;

            viewModel.SetArticleChecked (article, false);
            RefreshSelectAllCheckBox (viewModel);
            UpdateBatchEditButton ();
        }

        // Označava sve trenutno vidljive artikle.
        private void SelectAllArticlesCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if(_updatingArticleCheckBoxes)
                return;

            if(DataContext is not ArticlesViewModel viewModel)
                return;

            viewModel.SetAllVisibleArticlesChecked (true);
            RefreshVisibleArticleCheckBoxes (viewModel);
            UpdateBatchEditButton ();
        }

        // Poništava oznaku svih trenutno vidljivih artikala.
        private void SelectAllArticlesCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if(_updatingArticleCheckBoxes)
                return;

            if(DataContext is not ArticlesViewModel viewModel)
                return;

            viewModel.SetAllVisibleArticlesChecked (false);
            RefreshVisibleArticleCheckBoxes (viewModel);
            UpdateBatchEditButton ();
        }

        // Osvježava checkboxe vidljivih redova nakon promjene podataka ili selekcije.
        private void RefreshVisibleArticleCheckBoxes(ArticlesViewModel viewModel)
        {
            _updatingArticleCheckBoxes = true;

            foreach(var article in ListaArtikala.Items.OfType<DatabaseTables.TblArtikli> ())
            {
                if(ListaArtikala.ItemContainerGenerator.ContainerFromItem (article) is not DataGridRow row)
                    continue;

                var cellContent = ListaArtikala.Columns[^1].GetCellContent (row);

                if(cellContent == null)
                    continue;

                CheckBox? checkBox = FindVisualChildren<CheckBox> (cellContent).FirstOrDefault ();

                if(checkBox != null)
                    checkBox.IsChecked = viewModel.IsArticleChecked (article);
            }

            SelectAllArticlesCheckBox.IsChecked = viewModel.AreAllVisibleArticlesChecked ();

            _updatingArticleCheckBoxes = false;
        }

        // Usklađuje glavni checkbox sa stanjem vidljivih artikala.
        private void RefreshSelectAllCheckBox(ArticlesViewModel viewModel)
        {
            _updatingArticleCheckBoxes = true;
            SelectAllArticlesCheckBox.IsChecked = viewModel.AreAllVisibleArticlesChecked ();
            _updatingArticleCheckBoxes = false;
        }

        // Omogućava grupno uređivanje samo kada postoji barem jedan označen artikl.
        private void UpdateBatchEditButton()
        {
            if(DataContext is ArticlesViewModel viewModel)
                BtnBatchEdit.IsEnabled = viewModel.BrojOznacenihArtikala > 0;
        }

        #endregion


        #region UREĐIVANJE POJEDINAČNOG ARTIKLA

        // Puni ComboBox kontrole vrijednostima potrebnim za unos ili uređivanje artikla.
        private void ConfigureArticleEditOptions(ArticlesViewModel viewModel)
        {
            ArticleTypeComboBox.ItemsSource = new List<ArticleTypeOption>
            {
                new ArticleTypeOption(-1, "SVI"),
                new ArticleTypeOption(0, "Piće"),
                new ArticleTypeOption(1, "Hrana"),
                new ArticleTypeOption(2, "Ostalo")
            };

            var articleTaxes = new List<BatchTaxOption> { new BatchTaxOption (null, "SVE") };
            articleTaxes.AddRange (viewModel.PoreskeStope.OrderBy (x => x.IdStope).Select (x => new BatchTaxOption (x.IdStope, x.Postotak.HasValue ? $"{x.Postotak.Value:0.##}%" : string.Empty)));
            ArticleTaxComboBox.ItemsSource = articleTaxes;
            ArticleUnitComboBox.ItemsSource = viewModel.JediniceMjere.OrderBy (x => x.IdJedinice).Select (x => new BatchUnitOption (x.IdJedinice, x.ToString ())).ToList ();
            ArticleNormativComboBox.ItemsSource = viewModel.Normativi.ToList ();

            bool isCroatia = string.Equals (Settings.Default.Country, "Hrvatska", StringComparison.OrdinalIgnoreCase);
            ArticleConsumptionTaxPanel.Visibility = isCroatia ? Visibility.Visible : Visibility.Collapsed;
        }

        // Pretvara tekst normativa u decimalnu vrijednost nezavisno od decimalnog separatora.
        private static decimal? ParseArticleNormativ(string? value)
        {
            if(string.IsNullOrWhiteSpace (value))
                return null;

            string normalized = value.Trim ().Replace (',', '.');
            return decimal.TryParse (normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result) ? result : null;
        }

        // Formira naziv za prodaju kombinovanjem naziva artikla i normativa kada normativ nije 1.
        private static string BuildArticleNormativ(string? articleName, string? normativText)
        {
            string name = articleName?.Trim () ?? string.Empty;
            decimal? normativ = ParseArticleNormativ (normativText);

            if(!normativ.HasValue || normativ == 1m)
                return name;

            return $"{name} {normativ.Value.ToString ("0.00", CultureInfo.InvariantCulture)}";
        }

        // Osvježava prikaz naziva za prodaju prema trenutnom nazivu i normativu.
        private void UpdateArticleNormativLabel()
        {
            if(ArticleNormativLabel == null || ArticleNameTextBox == null || ArticleNormativComboBox == null)
                return;

            if(ArticleNormativComboBox.SelectedItem is not TblNormativPica selectedNormativ)
            {
                ArticleNormativLabel.Content = ArticleNameTextBox.Text.Trim ();
                return;
            }

            if(string.Equals (selectedNormativ.Normativ, "Dodaj normativ", StringComparison.OrdinalIgnoreCase))
                return;

            decimal? normativ = ParseArticleNormativ (selectedNormativ.Normativ);
            ArticleNormativLabel.Content = normativ.HasValue ? BuildArticleNormativ (ArticleNameTextBox.Text, selectedNormativ.Normativ) : ArticleNameTextBox.Text.Trim ();
        }

        // Obrađuje izbor normativa i omogućava dodavanje nove vrijednosti normativa.
        private async void ArticleNormativComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ArticleEditComboBox_SelectionChanged (sender, e);

            if(ArticleNormativComboBox.SelectedItem is not TblNormativPica selectedNormativ)
            {
                UpdateArticleNormativLabel ();
                return;
            }

            if(!string.Equals (selectedNormativ.Normativ, "Dodaj normativ", StringComparison.OrdinalIgnoreCase))
            {
                UpdateArticleNormativLabel ();
                return;
            }

            if(DataContext is not ArticlesViewModel viewModel)
                return;

            MyInputBox dialog = new MyInputBox ();
            dialog.InputTitle.Text = "NOVI NORMATIV PIĆA";
            dialog.InputText.Focus ();
            dialog.ShowDialog ();

            string result = dialog.result?.Trim () ?? string.Empty;

            if(string.IsNullOrWhiteSpace (result))
            {
                ArticleNormativComboBox.SelectedItem = viewModel.Normativi.FirstOrDefault (x => ParseArticleNormativ (x.Normativ) == 1m);
                return;
            }

            decimal? newValue = ParseArticleNormativ (result);

            if(!newValue.HasValue || newValue.Value <= 0)
            {
                ShowArticleEditMessage ("GREŠKA", "Unesite ispravan normativ veći od 0.");
                ArticleNormativComboBox.SelectedItem = viewModel.Normativi.FirstOrDefault (x => ParseArticleNormativ (x.Normativ) == 1m);
                return;
            }

            TblNormativPica? existing = viewModel.Normativi.FirstOrDefault (x => ParseArticleNormativ (x.Normativ) == newValue.Value);

            if(existing != null)
            {
                ArticleNormativComboBox.SelectedItem = existing;
                return;
            }

            string normalizedText = result.Replace (',', '.');

            try
            {
                await using var db = new AppDbContext ();
                db.NormativPica.Add (new TblNormativPica { Normativ = normalizedText });
                await db.SaveChangesAsync ();
            }
            catch(Exception ex)
            {
                Debug.WriteLine ("[ARTICLES] Dodavanje normativa: " + ex);
                ShowArticleEditMessage ("GREŠKA", "Normativ nije spremljen. Pokušajte ponovo.");
                ArticleNormativComboBox.SelectedItem = viewModel.Normativi.FirstOrDefault (x => ParseArticleNormativ (x.Normativ) == 1m);
                return;
            }

            try
            {
                await viewModel.LoadNormativi ();
                ArticleNormativComboBox.ItemsSource = viewModel.Normativi.ToList ();
                ArticleNormativComboBox.SelectedItem = ArticleNormativComboBox.Items.OfType<TblNormativPica> ().FirstOrDefault (x => ParseArticleNormativ (x.Normativ) == newValue.Value);
            }
            catch(Exception ex)
            {
                Debug.WriteLine ("[ARTICLES] Osvježavanje normativa nakon dodavanja: " + ex);
                ShowArticleEditMessage ("UPOZORENJE", "Normativ je spremljen, ali lista normativa nije uspješno osvježena.");
            }
        }

        // Osvježava naziv za prodaju i stanje validacije kada se promijeni naziv artikla.
        private void ArticleNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateArticleNormativLabel ();
            ArticleEditTextBox_TextChanged (sender, e);
        }

        // Prilagođava kategorije i prikaz normativa izabranoj vrsti artikla.
        private void ArticleTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ArticleEditComboBox_SelectionChanged (sender, e);
            if(DataContext is not ArticlesViewModel viewModel || ArticleTypeComboBox.SelectedItem is not ArticleTypeOption selectedType)
            {
                ArticleCategoryComboBox.ItemsSource = null;
                return;
            }

            if(selectedType.TypeId < 0)
            {
                ArticleCategoryComboBox.ItemsSource = new List<BatchCategoryOption> { new BatchCategoryOption (null, "SVE") };
                ArticleCategoryComboBox.SelectedIndex = 0;
                ArticleNormativPanel.Visibility = Visibility.Collapsed;
                return;
            }

            var articleCategories = new List<BatchCategoryOption> { new BatchCategoryOption (null, "SVE") };
            articleCategories.AddRange (viewModel.Kategorije.Where (x => x.VrstaArtikla == selectedType.TypeId).OrderBy (x => x.Kategorija).Select (x => new BatchCategoryOption (x.IdKategorije, x.Kategorija ?? string.Empty)));
            ArticleCategoryComboBox.ItemsSource = articleCategories;
            ArticleCategoryComboBox.SelectedIndex = 0;
            ArticleNormativPanel.Visibility = selectedType.TypeId == 0 ? Visibility.Visible : Visibility.Collapsed;

            if(selectedType.TypeId != 0)
                ArticleNormativLabel.Content = ArticleNameTextBox.Text.Trim ();
        }

        // Vizuelno označava kontrolu koja sadrži neispravan podatak.
        private void SetArticleFieldError(Control control)
        {
            control.BorderBrush = Brushes.Red;
            control.BorderThickness = new Thickness (0, 0, 0, 2);
        }

        // Vraća standardni izgled kontrole nakon ispravke podatka.
        private void ClearArticleFieldError(Control control)
        {
            control.BorderBrush = Application.Current.Resources["GlobalFontColor"] as Brush;
            control.BorderThickness = new Thickness (0, 0, 0, 1);
        }

        // Uklanja sva vizuelna upozorenja sa editora artikla.
        private void ClearArticleEditErrors()
        {
            ClearArticleFieldError (ArticleNameTextBox);
            ClearArticleFieldError (ArticleCodeTextBox);
            ClearArticleFieldError (ArticleInternalCodeTextBox);
            ClearArticleFieldError (ArticlePriceTextBox);
            ClearArticleFieldError (ArticleUnitComboBox);
            ClearArticleFieldError (ArticleNormativComboBox);
            ClearArticleFieldError (ArticleTypeComboBox);
            ClearArticleFieldError (ArticleCategoryComboBox);
            ClearArticleFieldError (ArticleTaxComboBox);
            ClearArticleFieldError (ArticlePositionTextBox);
        }

        // Uklanja oznaku greške sa TextBoxa kada korisnik unese vrijednost.
        private void ArticleEditTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(sender is TextBox textBox && !string.IsNullOrWhiteSpace (textBox.Text))
                ClearArticleFieldError (textBox);
        }

        // Uklanja oznaku greške sa ComboBoxa kada je izabrana validna vrijednost.
        private void ArticleEditComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(sender is not ComboBox comboBox || comboBox.SelectedItem == null)
                return;

            bool valid = comboBox switch
            {
                _ when comboBox == ArticleTypeComboBox => comboBox.SelectedItem is ArticleTypeOption type && type.TypeId >= 0,
                _ when comboBox == ArticleCategoryComboBox => comboBox.SelectedItem is BatchCategoryOption category && category.CategoryId != null,
                _ when comboBox == ArticleTaxComboBox => comboBox.SelectedItem is BatchTaxOption tax && tax.TaxId != null,
                _ when comboBox == ArticleUnitComboBox => comboBox.SelectedItem is BatchUnitOption unit && unit.UnitId != null,
                _ when comboBox == ArticleNormativComboBox => comboBox.SelectedItem is TblNormativPica normativ && !string.Equals (normativ.Normativ, "Dodaj normativ", StringComparison.OrdinalIgnoreCase) && ParseArticleNormativ (normativ.Normativ) > 0m,
                _ => true
            };

            if(valid)
                ClearArticleFieldError (comboBox);
        }

        // Prikazuje odabranu sliku ili njen odabrani crop u Add/Edit editoru artikla.
        private void SetArticleEditImage(string? imagePath, Int32Rect? cropPixels = null)
        {
            _selectedArticleImagePath = string.IsNullOrWhiteSpace (imagePath) || imagePath == "bez_slike" ? null : imagePath;
            _selectedArticleImageCrop = cropPixels;
            if(!cropPixels.HasValue)
                _selectedArticleImageCropType = null;

            string previewPath;

            if(string.IsNullOrWhiteSpace (_selectedArticleImagePath))
                previewPath = Path.Combine (AppContext.BaseDirectory, "Images", "noimage.png");
            else if(Path.IsPathRooted (_selectedArticleImagePath))
                previewPath = _selectedArticleImagePath;
            else
                previewPath = Path.Combine (ArticleImageStorage.ArticlesFolder, _selectedArticleImagePath);

            if(!File.Exists (previewPath))
                previewPath = Path.Combine (AppContext.BaseDirectory, "Images", "noimage.png");

            ArticleImagePreview.Source = null;

            if(!File.Exists (previewPath))
                return;

            try
            {
                BitmapImage bitmap = new ();
                bitmap.BeginInit ();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri (previewPath, UriKind.Absolute);
                bitmap.EndInit ();
                bitmap.Freeze ();

                if(cropPixels.HasValue)
                {
                    Int32Rect crop = cropPixels.Value;
                    crop.X = Math.Max (0, Math.Min (crop.X, bitmap.PixelWidth - 1));
                    crop.Y = Math.Max (0, Math.Min (crop.Y, bitmap.PixelHeight - 1));
                    crop.Width = Math.Max (1, Math.Min (crop.Width, bitmap.PixelWidth - crop.X));
                    crop.Height = Math.Max (1, Math.Min (crop.Height, bitmap.PixelHeight - crop.Y));
                    CroppedBitmap cropped = new (bitmap, crop);
                    cropped.Freeze ();
                    ArticleImagePreview.Source = cropped;
                }
                else
                {
                    ArticleImagePreview.Source = bitmap;
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[ARTICLES] Ne mogu prikazati sliku '{previewPath}': {ex.Message}");
            }
        }

        // Otvara Windows izbor slike početno u Caupo biblioteci; vanjska slika prolazi kroz ručni crop.
        private void BtnArticleImageSelect_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new ()
            {
                Title = "Odaberi sliku artikla",
                Filter = "Slike (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|JPEG slike (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG slike (*.png)|*.png",
                InitialDirectory = ArticleImageStorage.ArticlesFolder,
                Multiselect = false
            };

            if(dialog.ShowDialog () != true)
                return;

            string selectedPath = Path.GetFullPath (dialog.FileName);
            string articlesFolder = Path.GetFullPath (ArticleImageStorage.ArticlesFolder);
            string folderPrefix = articlesFolder.TrimEnd (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            string importedFolder = Path.GetFullPath (Path.Combine (ArticleImageStorage.ArticlesFolder, "Imported"));
            string importedPrefix = importedFolder.TrimEnd (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            bool isImportedImage = selectedPath.StartsWith (importedPrefix, StringComparison.OrdinalIgnoreCase);

            if(selectedPath.StartsWith (folderPrefix, StringComparison.OrdinalIgnoreCase) && !isImportedImage)
            {
                SetArticleEditImage (selectedPath);
                return;
            }

            if(ArticleTypeComboBox.SelectedItem is not ArticleTypeOption selectedType || selectedType.TypeId < 0)
            {
                ShowArticleEditMessage ("SLIKA ARTIKLA", "Prvo odaberite vrstu artikla, pa zatim sliku.");
                ArticleTypeComboBox.Focus ();
                return;
            }

            (int targetWidth, int targetHeight) = ArticleImageStorage.GetTargetSize (selectedType.TypeId);

            ArticleImageCropWindow cropWindow = new (selectedPath, targetWidth, targetHeight)
            {
                Owner = Window.GetWindow (this)
            };

            if(cropWindow.ShowDialog () != true)
                return;

            SetArticleEditImage (selectedPath, cropWindow.CropPixels);
            _selectedArticleImageCropType = selectedType.TypeId;
        }

        // Naše već obrađene slike ostavlja netaknute, a vanjsku sliku cropuje, resizeuje i sprema u biblioteku.
        private string? PrepareArticleImageForSave(int articleType)
        {
            if(string.IsNullOrWhiteSpace (_selectedArticleImagePath))
                return null;

            string sourcePath = Path.IsPathRooted (_selectedArticleImagePath)
                ? Path.GetFullPath (_selectedArticleImagePath)
                : Path.GetFullPath (Path.Combine (ArticleImageStorage.ArticlesFolder, _selectedArticleImagePath));

            if(!File.Exists (sourcePath))
                return _selectedArticleImagePath;

            string articlesFolder = Path.GetFullPath (ArticleImageStorage.ArticlesFolder);
            string folderPrefix = articlesFolder.TrimEnd (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            string importedFolder = Path.GetFullPath (Path.Combine (ArticleImageStorage.ArticlesFolder, "Imported"));
            string importedPrefix = importedFolder.TrimEnd (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            bool isImportedImage = sourcePath.StartsWith (importedPrefix, StringComparison.OrdinalIgnoreCase);

            if(sourcePath.StartsWith (folderPrefix, StringComparison.OrdinalIgnoreCase) && !isImportedImage)
                return sourcePath;

            if(!_selectedArticleImageCrop.HasValue)
                return _selectedArticleImagePath;

            if(_selectedArticleImageCropType != articleType)
                throw new InvalidOperationException ("Vrsta artikla je promijenjena nakon odabira slike. Ponovo odaberite sliku i namjestite crop.");

            return ArticleImageStorage.ProcessAndSaveExternalImage (sourcePath, _selectedArticleImageCrop.Value, articleType);
        }

        // Otvara editor i učitava podatke trenutno selektovanog artikla.
        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is not ArticlesViewModel viewModel || viewModel.SelectedArticle == null)
                return;


            TblArtikli article = viewModel.SelectedArticle;

            _isEditingArticle = true;
            _editingArticleId = article.IdArtikla;
            ClearArticleEditErrors ();

            bool hasBeenSold = await viewModel.HasArticleBeenSold (article.IdArtikla);
            ArticleNameTextBox.IsReadOnly = hasBeenSold;

            ArticleEditTitle.Text = "Uredi artikl";
            ArticleEditDescription.Text = $"Uređivanje artikla: {article.Artikl}";

            ConfigureArticleEditOptions (viewModel);

            ArticleNameTextBox.Text = article.Artikl ?? string.Empty;
            ArticleCodeTextBox.Text = article.Sifra ?? string.Empty;
            ArticleInternalCodeTextBox.Text = article.InternaSifra ?? string.Empty;
            ArticlePriceTextBox.Text = article.Cijena?.ToString ("0.00", CultureInfo.CurrentCulture) ?? string.Empty;

            ArticleTypeComboBox.SelectedItem = ArticleTypeComboBox.Items.OfType<ArticleTypeOption> ().FirstOrDefault (x => x.TypeId == article.VrstaArtikla);

            ArticleCategoryComboBox.SelectedItem = ArticleCategoryComboBox.Items.OfType<BatchCategoryOption> ().FirstOrDefault (x => x.CategoryId == article.Kategorija);
            ArticleTaxComboBox.SelectedItem = ArticleTaxComboBox.Items.OfType<BatchTaxOption> ().FirstOrDefault (x => x.TaxId == article.PoreskaStopa);
            ArticleUnitComboBox.SelectedItem = ArticleUnitComboBox.Items.OfType<BatchUnitOption> ().FirstOrDefault (x => x.UnitId == article.JedinicaMjere);
            ArticleNormativComboBox.SelectedItem = ArticleNormativComboBox.Items.OfType<TblNormativPica> ().FirstOrDefault (x => ParseArticleNormativ (x.Normativ) == article.Normativ);
            ArticlePositionTextBox.Text = article.Pozicija?.ToString (CultureInfo.InvariantCulture) ?? string.Empty;
            ArticleNormativLabel.Content = BuildArticleNormativ (article.Artikl, ArticleNormativComboBox.SelectedItem is TblNormativPica editNormativ ? editNormativ.Normativ : "1");

            ArticleConsumptionTaxComboBox.SelectedIndex = article.PorezNaPotrosnju ? 0 : 1;
            ArticleActiveComboBox.SelectedIndex = article.Aktivan ? 0 : 1;
            SetArticleEditImage (article.Slika);

            ArticleEditGrid.Visibility = Visibility.Visible;

            if(!string.IsNullOrWhiteSpace (article.Slika) && File.Exists (article.Slika))
            {
                string imagePath = Path.GetFullPath (article.Slika);
                string importedFolder = Path.GetFullPath (Path.Combine (ArticleImageStorage.ArticlesFolder, "Imported"));
                string importedPrefix = importedFolder.TrimEnd (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

                if(imagePath.StartsWith (importedPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    (int targetWidth, int targetHeight) = ArticleImageStorage.GetTargetSize (article.VrstaArtikla ?? 2);

                    ArticleImageCropWindow cropWindow = new (imagePath, targetWidth, targetHeight)
                    {
                        Owner = Window.GetWindow (this)
                    };

                    if(cropWindow.ShowDialog () == true)
                    {
                        SetArticleEditImage (imagePath, cropWindow.CropPixels);
                        _selectedArticleImageCropType = article.VrstaArtikla;
                    }
                }
            }

            ArticleNameTextBox.Focus ();
        }

        // Otvara editor za novi artikl i postavlja početne vrijednosti.
        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is not ArticlesViewModel viewModel)
                return;

            _isEditingArticle = false;
            _editingArticleId = null;
            ClearArticleEditErrors ();
            ArticleNameTextBox.IsReadOnly = false;

            ArticleEditTitle.Text = "Novi artikl";
            ArticleEditDescription.Text = "Unos podataka novog artikla";

            long nextId = await viewModel.GetNextArticleSequenceValueAsync ();
            string suggestedValue = nextId.ToString (CultureInfo.InvariantCulture);

            ArticleNameTextBox.Clear ();
            ArticleCodeTextBox.Text = suggestedValue;
            ArticleInternalCodeTextBox.Text = suggestedValue;
            ArticlePriceTextBox.Clear ();
            ArticlePositionTextBox.Text = suggestedValue;

            ConfigureArticleEditOptions (viewModel);

            ArticleTypeComboBox.SelectedIndex = 0;
            ArticleTaxComboBox.SelectedIndex = 0;
            ArticleUnitComboBox.SelectedItem = ArticleUnitComboBox.Items.OfType<BatchUnitOption> ().FirstOrDefault (x => string.Equals (x.Name, "komad", StringComparison.OrdinalIgnoreCase))
                ?? ArticleUnitComboBox.Items.OfType<BatchUnitOption> ().FirstOrDefault (x => string.Equals (x.Name, "kom", StringComparison.OrdinalIgnoreCase));
            ArticleNormativComboBox.SelectedItem = ArticleNormativComboBox.Items.OfType<TblNormativPica> ().FirstOrDefault (x => ParseArticleNormativ (x.Normativ) == 1m);
            UpdateArticleNormativLabel ();
            ArticleConsumptionTaxComboBox.SelectedIndex = 1;
            ArticleActiveComboBox.SelectedIndex = 0;
            SetArticleEditImage (null);

            ArticleEditGrid.Visibility = Visibility.Visible;
            ArticleNameTextBox.Focus ();
        }

        // Zatvara editor artikla bez spremanja promjena.
        private void BtnArticleEditCancel_Click(object sender, RoutedEventArgs e)
        {
            ClearArticleEditErrors ();
            _selectedArticleImagePath = null;
            _selectedArticleImageCrop = null;
            _selectedArticleImageCropType = null;
            ArticleImagePreview.Source = null;
            ArticleEditGrid.Visibility = Visibility.Collapsed;
        }

        // Validira podatke i sprema novi ili izmijenjeni artikl.
        private async void BtnArticleEditSave_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is not ArticlesViewModel viewModel)
                return;

            ClearArticleEditErrors ();

            string naziv = ArticleNameTextBox.Text.Trim ();
            string sifra = ArticleCodeTextBox.Text.Trim ();
            string internaSifra = ArticleInternalCodeTextBox.Text.Trim ();

            if(string.IsNullOrWhiteSpace (naziv))
            {
                SetArticleFieldError (ArticleNameTextBox);
                ShowArticleEditMessage ("GREŠKA", "Unesite naziv artikla.");
                ArticleNameTextBox.Focus ();
                return;
            }

            if(string.IsNullOrWhiteSpace (sifra))
            {
                SetArticleFieldError (ArticleCodeTextBox);
                ShowArticleEditMessage ("GREŠKA", "Unesite šifru artikla.");
                ArticleCodeTextBox.Focus ();
                return;
            }

            if(string.IsNullOrWhiteSpace (internaSifra))
            {
                SetArticleFieldError (ArticleInternalCodeTextBox);
                ShowArticleEditMessage ("GREŠKA", "Unesite internu šifru artikla.");
                ArticleInternalCodeTextBox.Focus ();
                return;
            }

            if(!TryParseArticlePrice (ArticlePriceTextBox.Text, out decimal cijena) || cijena < 0)
            {
                SetArticleFieldError (ArticlePriceTextBox);
                ShowArticleEditMessage ("GREŠKA", "Unesite ispravnu cijenu artikla.");
                ArticlePriceTextBox.Focus ();
                ArticlePriceTextBox.SelectAll ();
                return;
            }

            if(ArticleTypeComboBox.SelectedItem is not ArticleTypeOption selectedType || selectedType.TypeId < 0)
            {
                SetArticleFieldError (ArticleTypeComboBox);
                ShowArticleEditMessage ("GREŠKA", "Odaberite vrstu artikla.");
                ArticleTypeComboBox.Focus ();
                return;
            }

            if(ArticleCategoryComboBox.SelectedItem is not BatchCategoryOption selectedCategory || selectedCategory.CategoryId == null)
            {
                SetArticleFieldError (ArticleCategoryComboBox);
                ShowArticleEditMessage ("GREŠKA", "Odaberite kategoriju artikla.");
                ArticleCategoryComboBox.Focus ();
                return;
            }

            if(ArticleTaxComboBox.SelectedItem is not BatchTaxOption selectedTax || selectedTax.TaxId == null)
            {
                SetArticleFieldError (ArticleTaxComboBox);
                ShowArticleEditMessage ("GREŠKA", "Odaberite poresku stopu.");
                ArticleTaxComboBox.Focus ();
                return;
            }

            if(ArticleUnitComboBox.SelectedItem is not BatchUnitOption selectedUnit || selectedUnit.UnitId == null)
            {
                SetArticleFieldError (ArticleUnitComboBox);
                ShowArticleEditMessage ("GREŠKA", "Odaberite jedinicu mjere.");
                ArticleUnitComboBox.Focus ();
                return;
            }

            decimal normativ = 1m;
            string normativText = "1";

            if(selectedType.TypeId == 0)
            {
                if(ArticleNormativComboBox.SelectedItem is not TblNormativPica selectedNormativ || string.Equals (selectedNormativ.Normativ, "Dodaj normativ", StringComparison.OrdinalIgnoreCase))
                {
                    SetArticleFieldError (ArticleNormativComboBox);
                    ShowArticleEditMessage ("GREŠKA", "Odaberite normativ artikla.");
                    ArticleNormativComboBox.Focus ();
                    return;
                }

                decimal? parsedNormativ = ParseArticleNormativ (selectedNormativ.Normativ);

                if(!parsedNormativ.HasValue || parsedNormativ.Value <= 0)
                {
                    SetArticleFieldError (ArticleNormativComboBox);
                    ShowArticleEditMessage ("GREŠKA", "Odaberite ispravan normativ artikla.");
                    ArticleNormativComboBox.Focus ();
                    return;
                }

                normativ = parsedNormativ.Value;
                normativText = selectedNormativ.Normativ?.Trim () ?? "1";
            }

            if(!int.TryParse (ArticlePositionTextBox.Text.Trim (), out int pozicija) || pozicija < 0)
            {
                SetArticleFieldError (ArticlePositionTextBox);
                ShowArticleEditMessage ("GREŠKA", "Unesite ispravnu poziciju artikla.");
                ArticlePositionTextBox.Focus ();
                ArticlePositionTextBox.SelectAll ();
                return;
            }

            string? articleImagePath;

            try
            {
                articleImagePath = PrepareArticleImageForSave (selectedType.TypeId);
            }
            catch(Exception ex)
            {
                Debug.WriteLine ("[ARTICLES] Spremanje slike artikla: " + ex);
                ShowArticleEditMessage ("GREŠKA", "Slika artikla nije spremljena. Odaberite drugu sliku ili pokušajte ponovo.");
                return;
            }

            TblArtikli article;

            if(_isEditingArticle)
            {
                if(!_editingArticleId.HasValue)
                    return;

                TblArtikli? existingArticle = viewModel.Artikli.FirstOrDefault (x => x.IdArtikla == _editingArticleId.Value);

                if(existingArticle == null)
                {
                    ShowArticleEditMessage ("GREŠKA", "Artikl više nije pronađen.");
                    return;
                }

                if(await viewModel.HasArticleBeenSold (existingArticle.IdArtikla))
                    naziv = existingArticle.Artikl ?? string.Empty;

                article = new TblArtikli
                {
                    IdArtikla = existingArticle.IdArtikla
                };
            }
            else
            {
                article = new TblArtikli ();
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
            article.ArtiklNormativ = selectedType.TypeId == 0 ? BuildArticleNormativ (naziv, normativText) : naziv;
            article.Aktivan = ArticleActiveComboBox.SelectedIndex != 1;
            article.PorezNaPotrosnju = ArticleConsumptionTaxPanel.Visibility == Visibility.Visible && ArticleConsumptionTaxComboBox.SelectedIndex == 0;
            article.Slika = articleImagePath;

            try
            {
                bool saved = _isEditingArticle ? await viewModel.UpdateArticle (article) : await viewModel.InsertArticle (article);

                if(!saved)
                    return;
            }
            catch(Exception ex)
            {
                Debug.WriteLine ("[ARTICLES] Save article: " + ex);
                ShowArticleEditMessage ("GREŠKA", "Artikl nije spremljen.");
                return;
            }

            ArticleEditGrid.Visibility = Visibility.Collapsed;
            _isEditingArticle = false;
            _editingArticleId = null;
            _selectedArticleImagePath = null;
            _selectedArticleImageCrop = null;
            _selectedArticleImageCropType = null;
            ArticleImagePreview.Source = null;
        }

        // Pretvara tekst cijene u decimalnu vrijednost.
        private static bool TryParseArticlePrice(string text, out decimal value)
        {
            string normalized = text.Trim ().Replace (',', '.');
            return decimal.TryParse (normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        // Prikazuje standardnu Caupo poruku za operacije nad artiklima.
        private static void ShowArticleEditMessage(string title, string message)
        {
            MyMessageBox myMessageBox = new MyMessageBox ();
            myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            myMessageBox.MessageTitle.Text = title;
            myMessageBox.MessageText.Text = message;
            myMessageBox.ShowDialog ();
        }

        // Briše nekorišten artikl ili deaktivira artikl koji je već korišten na računima.
        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is not ArticlesViewModel viewModel || viewModel.SelectedArticle == null)
            {
                ShowArticleEditMessage ("GREŠKA", "Odaberite artikl koji želite obrisati.");
                return;
            }

            TblArtikli article = viewModel.SelectedArticle;
            bool hasBeenSold = await viewModel.HasArticleBeenSold (article.IdArtikla);

            if(hasBeenSold)
            {
                if(!article.Aktivan)
                {
                    ShowArticleEditMessage ("INFORMACIJA", $"Artikl {article.Artikl} je već neaktivan i ne može se fizički obrisati jer je korišten na računima.");
                    return;
                }

                YesNoPopup deactivatePopup = new YesNoPopup ();
                deactivatePopup.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                deactivatePopup.MessageTitle.Text = "DEAKTIVACIJA ARTIKLA";
                deactivatePopup.MessageText.Text = $"Artikl {article.Artikl} je već korišten na računima i ne može se fizički obrisati." + Environment.NewLine + Environment.NewLine + "Želite li ga postaviti kao neaktivan?";
                deactivatePopup.ShowDialog ();

                if(deactivatePopup.Kliknuo != "Da")
                    return;

                if(await viewModel.DeactivateArticle (article.IdArtikla))
                    ShowArticleEditMessage ("POTVRDA", $"Artikl {article.Artikl} je postavljen kao neaktivan.");

                return;
            }

            YesNoPopup deletePopup = new YesNoPopup ();
            deletePopup.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            deletePopup.MessageTitle.Text = "POTVRDA BRISANJA";
            deletePopup.MessageText.Text = $"Da li ste sigurni da želite obrisati artikl:" + Environment.NewLine + article.Artikl + " ?";
            deletePopup.ShowDialog ();

            if(deletePopup.Kliknuo == "Da")
                await viewModel.DeleteArticle (article.IdArtikla);
        }

        #endregion


        #region GRUPNO UREĐIVANJE POSTOJEĆIH ARTIKALA

        // Puni izbor jedinica mjere za grupno uređivanje postojećih artikala.
        private void ConfigureBatchUnit(ArticlesViewModel viewModel)
        {
            var units = new List<BatchUnitOption>
            {
                new BatchUnitOption (null, "Ne mijenjaj")
            };

            units.AddRange (
                viewModel.JediniceMjere
                    .OrderBy (x => x.IdJedinice)
                    .Select (x => new BatchUnitOption (x.IdJedinice, x.ToString ()))
            );

            BatchUnitComboBox.ItemsSource = units;
            BatchUnitComboBox.SelectedIndex = 0;
        }

        // Podešava grupni izbor poreza na potrošnju prema državi.
        private void ConfigureBatchConsumptionTax()
        {
            bool isCroatia = string.Equals (Settings.Default.Country, "Hrvatska", StringComparison.OrdinalIgnoreCase);

            BatchConsumptionTaxPanel.Visibility = isCroatia ? Visibility.Visible : Visibility.Collapsed;
            BatchConsumptionTaxComboBox.SelectedIndex = 0;
        }

        // Puni izbor poreskih stopa za grupno uređivanje.
        private void ConfigureBatchTax(ArticlesViewModel viewModel)
        {
            var taxes = new List<BatchTaxOption>
    {
        new BatchTaxOption (null, "Ne mijenjaj")
    };

            taxes.AddRange (
                viewModel.PoreskeStope
                    .OrderBy (x => x.IdStope)
                    .Select (x => new BatchTaxOption (x.IdStope, x.ToString ()))
            );

            BatchTaxComboBox.ItemsSource = taxes;
            BatchTaxComboBox.SelectedIndex = 0;
        }

        // Puni kategorije za grupno uređivanje samo kada označeni artikli pripadaju istoj vrsti.
        private void ConfigureBatchCategory(ArticlesViewModel viewModel)
        {
            var selectedArticles = viewModel.Artikli
                .Where (viewModel.IsArticleChecked)
                .ToList ();

            var articleTypes = selectedArticles
                .Select (x => x.VrstaArtikla)
                .Distinct ()
                .ToList ();

            if(articleTypes.Count != 1 || !articleTypes[0].HasValue)
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

            categories.AddRange (
                viewModel.Kategorije
                    .Where (x => x.VrstaArtikla == articleType)
                    .OrderBy (x => x.Kategorija)
                    .Select (x => new BatchCategoryOption (x.IdKategorije, x.Kategorija ?? string.Empty))
            );

            BatchCategoryComboBox.ItemsSource = categories;
            BatchCategoryComboBox.SelectedIndex = 0;
            BatchCategoryComboBox.IsEnabled = true;
        }

        // Otvara editor grupnih promjena za označene postojeće artikle.
        private void BtnBatchEdit_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is not ArticlesViewModel viewModel || viewModel.BrojOznacenihArtikala == 0)
                return;

            BatchEditSelectedCountText.Text = $"Označeno: {viewModel.BrojOznacenihArtikala} artikala";

            BatchValueTextBox.Clear ();
            BatchPriceOperationComboBox.SelectedIndex = 0;
            BatchActiveComboBox.SelectedIndex = 0;
            BatchValueLabel.Text = "Nova cijena";

            ConfigureBatchCategory (viewModel);
            ConfigureBatchTax (viewModel);
            ConfigureBatchConsumptionTax ();
            ConfigureBatchUnit (viewModel);

            BatchEditGrid.Visibility = Visibility.Visible;
            BatchValueTextBox.Focus ();
        }

        // Zatvara grupni editor bez primjene novih promjena.
        private void BtnBatchEditCancel_Click(object sender, RoutedEventArgs e)
        {
            BatchEditGrid.Visibility = Visibility.Collapsed;
        }

        // Primjenjuje odabrane grupne promjene na označene artikle u bazi.
        private async void BtnBatchEditApply_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is not ArticlesViewModel viewModel || viewModel.BrojOznacenihArtikala == 0)
                return;

            bool changePrice = !string.IsNullOrWhiteSpace (BatchValueTextBox.Text);
            bool changeActive = BatchActiveComboBox.SelectedIndex > 0;

            BatchCategoryOption? selectedCategory = BatchCategoryComboBox.SelectedItem as BatchCategoryOption;
            bool changeCategory = BatchCategoryComboBox.IsEnabled && selectedCategory?.CategoryId != null;

            BatchTaxOption? selectedTax = BatchTaxComboBox.SelectedItem as BatchTaxOption;
            bool changeTax = selectedTax?.TaxId != null;

            bool changeConsumptionTax = BatchConsumptionTaxPanel.Visibility == Visibility.Visible && BatchConsumptionTaxComboBox.SelectedIndex > 0;

            BatchUnitOption? selectedUnit = BatchUnitComboBox.SelectedItem as BatchUnitOption;
            bool changeUnit = selectedUnit?.UnitId != null;

            if(!changePrice && !changeActive && !changeCategory && !changeTax && !changeConsumptionTax && !changeUnit)
            {
                ShowBatchEditMessage ("GRUPNO UREĐIVANJE", "Niste odabrali nijednu promjenu.");
                return;
            }

            decimal value = 0m;

            if(changePrice && (!TryParseBatchValue (BatchValueTextBox.Text, out value) || value <= 0))
            {
                ShowBatchEditMessage ("GREŠKA", "Unesite ispravnu vrijednost veću od 0.");
                BatchValueTextBox.Focus ();
                BatchValueTextBox.SelectAll ();
                return;
            }

            int operation = BatchPriceOperationComboBox.SelectedIndex;

            if(changePrice && (operation < 0 || operation > 2))
                return;

            if(changePrice && operation == 2 && value >= 100m)
            {
                ShowBatchEditMessage ("GREŠKA", "Postotak smanjenja mora biti manji od 100%.");
                BatchValueTextBox.Focus ();
                BatchValueTextBox.SelectAll ();
                return;
            }

            var selectedIds = viewModel.Artikli
                .Where (viewModel.IsArticleChecked)
                .Select (x => x.IdArtikla)
                .ToList ();

            if(selectedIds.Count == 0)
                return;

            int? selectedArticleId = viewModel.SelectedArticle?.IdArtikla;

            try
            {
                await using var db = new AppDbContext ();

                var articles = await db.Artikli
                    .Where (x => selectedIds.Contains (x.IdArtikla))
                    .ToListAsync ();

                foreach(var article in articles)
                {
                    if(changePrice)
                    {
                        if(operation == 0)
                        {
                            article.Cijena = value;
                        }
                        else if(article.Cijena.HasValue)
                        {
                            decimal newPrice = operation == 1
                                ? article.Cijena.Value * (1m + value / 100m)
                                : article.Cijena.Value * (1m - value / 100m);

                            article.Cijena = RoundPriceUpToTenCents (newPrice);
                        }
                    }

                    if(changeActive)
                        article.Aktivan = BatchActiveComboBox.SelectedIndex == 1;

                    if(changeCategory && selectedCategory?.CategoryId != null)
                        article.Kategorija = selectedCategory.CategoryId.Value;

                    if(changeTax && selectedTax?.TaxId != null)
                        article.PoreskaStopa = selectedTax.TaxId.Value;

                    if(changeConsumptionTax)
                        article.PorezNaPotrosnju = BatchConsumptionTaxComboBox.SelectedIndex == 1;

                    if(changeUnit && selectedUnit?.UnitId != null)
                        article.JedinicaMjere = selectedUnit.UnitId.Value;
                }

                await db.SaveChangesAsync ();
            }
            catch(Exception ex)
            {
                Debug.WriteLine ("[ARTICLES] Batch save: " + ex);
                ShowBatchEditMessage ("GREŠKA", "Grupna promjena artikala nije spremljena.");
                return;
            }

            await viewModel.LoadArticlesAsync ();

            if(selectedArticleId.HasValue)
                viewModel.SelectedArticle = viewModel.Artikli.FirstOrDefault (x => x.IdArtikla == selectedArticleId.Value);

            BatchEditGrid.Visibility = Visibility.Collapsed;
            RefreshVisibleArticleCheckBoxes (viewModel);
            RefreshSelectAllCheckBox (viewModel);
            UpdateBatchEditButton ();
        }

        // Zaokružuje izračunatu cijenu naviše na deset centi.
        private static decimal RoundPriceUpToTenCents(decimal price)
        {
            return Math.Ceiling (price * 10m) / 10m;
        }

        // Pretvara vrijednost unesenu u grupnom editoru u decimalni broj.
        private static bool TryParseBatchValue(string? text, out decimal value)
        {
            value = 0m;

            if(string.IsNullOrWhiteSpace (text))
                return false;

            text = text.Trim ();

            if(decimal.TryParse (text, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
                return true;

            string normalized = text.Replace (',', '.');
            return decimal.TryParse (normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        // Prikazuje standardnu poruku grupnog editora.
        private static void ShowBatchEditMessage(string title, string message)
        {
            var dialog = new MyMessageBox
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            dialog.MessageTitle.Text = title;
            dialog.MessageText.Text = message;
            dialog.ShowDialog ();
        }

        // Mijenja opis polja vrijednosti prema odabranoj operaciji nad cijenom.
        private void BatchPriceOperationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(BatchValueLabel == null || BatchPriceOperationComboBox.SelectedIndex < 0)
                return;

            BatchValueLabel.Text = BatchPriceOperationComboBox.SelectedIndex == 0 ? "Nova cijena" : "Postotak";
        }

        #endregion


        #region UVOZ ARTIKALA - OTVARANJE I PREPOZNAVANJE FORMATA

        // Otvara XLSX ili CSV datoteku i bira direktan Caupo import ili univerzalno mapiranje.
        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new ()
            {
                Title = "Uvezi artikle",
                Filter = "Podržane datoteke (*.xlsx;*.csv)|*.xlsx;*.csv|Excel datoteke (*.xlsx)|*.xlsx|CSV datoteke (*.csv)|*.csv",
                Multiselect = false
            };

            if(dialog.ShowDialog () != true)
                return;

            try
            {
                string extension = Path.GetExtension (dialog.FileName).ToLowerInvariant ();

                ArticleImportSourceData source = extension switch
                {
                    ".xlsx" => ExcelArticleImportReader.Read (dialog.FileName),
                    ".csv" => CsvArticleImportReader.Read (dialog.FileName),
                    _ => throw new NotSupportedException ($"Format {extension} nije podržan.")
                };

                _articleImportSource = source;

                bool isCaupoFormat = ArticleImportFormatDetector.IsCaupoArticleFormat (source);
                Debug.WriteLine ($"[TAX DEBUG] IMPORT START | File='{Path.GetFileName (dialog.FileName)}' | CaupoFormat={isCaupoFormat} | Rows={source.Rows.Count}");

                if(source.Rows.Count > 0)
                {
                    ArticleImportSourceRow debugRow = source.Rows[0];
                    Debug.WriteLine ($"[TAX DEBUG] RAW FIRST ROW | PoreskaStopa='{debugRow.GetString ("PoreskaStopa")}' | Porez='{debugRow.GetString ("Porez")}'");
                }

                if(isCaupoFormat)
                {
                    Debug.WriteLine ("[TAX DEBUG] PATH = LoadCaupoArticleImport");
                    LoadCaupoArticleImport (source);
                    return;
                }

                Debug.WriteLine ("[TAX DEBUG] PATH = Universal mapper");

                _articleImportMappings = ArticleImportMapper.CreateMappings ();

                LoadArticleImportMapperColumns (source);

                ArticleImportMapperTitle.Text = $"Mapiranje kolona — {source.SourceName}";
                ArticleImportMapperSummary.Text = $"Izvor: {source.TableName} · Kolona: {source.Columns.Count} · Redova: {source.Rows.Count}";
                ArticleImportMapperGrid.Visibility = Visibility.Visible;
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[ARTICLE IMPORT] Greška: {ex}");
                ShowArticleImportMapperMessage ("Datoteku nije moguće učitati.");
            }
        }

        // Učitava prepoznati Caupo format direktno u privremeni transfer editor.
        private async void LoadCaupoArticleImport(ArticleImportSourceData source)
        {
            ArticlesViewModel viewModel = (ArticlesViewModel)DataContext;

            _articleTransferRows.Clear ();

            TblPoreskeStope? defaultTax = viewModel.PoreskeStope
                .Where (x => x.Aktivna)
                .OrderByDescending (x => x.Postotak ?? 0)
                .FirstOrDefault ();

            foreach(ArticleImportSourceRow row in source.Rows)
            {
                string rawTax = row.GetString ("PoreskaStopa");
                string resolvedTax = ResolveImportPorez (rawTax, viewModel);

                if(string.IsNullOrWhiteSpace (resolvedTax) && defaultTax?.Postotak != null)
                    resolvedTax = $"{defaultTax.Postotak:0.##}%";

                var transferRow = new ArticleTransferRow (viewModel.Kategorije)
                {
                    Sifra = row.GetString ("Sifra"),
                    InternaSifra = row.GetString ("InternaSifra"),
                    Artikl = row.GetString ("Artikl"),
                    Cijena = row.GetString ("Cijena"),
                    Vrsta = ResolveImportVrsta (row.GetString ("VrstaArtikla")),
                    Kategorija = ResolveImportKategorija (row.GetString ("Kategorija"), viewModel),
                    Jedinica = ResolveImportJedinica (row.GetString ("JedinicaMjere"), viewModel),
                    Porez = resolvedTax,
                    Normativ = row.GetString ("Normativ"),
                    Pozicija = row.GetString ("Pozicija"),
                    Aktivan = ParseTransferBool (row.GetString ("Aktivan"), true),
                    PorezNaPotrosnju = ParseTransferBool (row.GetString ("PorezNaPotrosnju"), false),
                    Slika = row.GetString ("Slika")
                };

                if(!IsRecognizedImportTax (rawTax, viewModel))
                    transferRow.DefaultedFields.Add ("Porez");

                _articleTransferRows.Add (transferRow);
            }

            Debug.WriteLine ($"[ARTICLE IMPORT] Prepoznat Caupo format. Učitano redova: {_articleTransferRows.Count}");

            OpenArticleTransferEditor (
                "Uvoz artikala",
                $"Caupo format · Učitano {_articleTransferRows.Count} artikala. Pregledajte podatke prije validacije i spremanja."
            );
        }

        // Pretvara ID vrste iz Caupo fajla u naziv vrste prikazan u transfer editoru.
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

        // Pretvara ID jedinice mjere u njen prikazni naziv kada je moguće.
        private static string ResolveImportJedinica(string value, ArticlesViewModel viewModel)
        {
            if(!int.TryParse (value, out int id))
                return value;

            return viewModel.JediniceMjere.FirstOrDefault (x => x.IdJedinice == id)?.ToString () ?? value;
        }

        // Pretvara ID poreske stope u njen prikazni naziv kada je moguće.

        private static string ResolveImportPorez(string value, ArticlesViewModel viewModel)
        {
            string text = value?.Trim () ?? string.Empty;

            if(!string.IsNullOrWhiteSpace (text))
            {
                string taxText = text.TrimEnd ('%').Trim ().Replace (',', '.');

                if(decimal.TryParse (taxText, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal rate))
                {
                    var taxByRate = viewModel.PoreskeStope.FirstOrDefault (x => x.Aktivna && x.Postotak == rate);

                    if(taxByRate?.Postotak != null)
                        return $"{taxByRate.Postotak:0.##}%";

                    if(int.TryParse (text, out int id))
                    {
                        var taxById = viewModel.PoreskeStope.FirstOrDefault (x => x.Aktivna && x.IdStope == id);

                        if(taxById?.Postotak != null)
                            return $"{taxById.Postotak:0.##}%";
                    }
                }
            }

            var defaultTax = viewModel.PoreskeStope
                .Where (x => x.Aktivna)
                .OrderByDescending (x => x.Postotak ?? 0)
                .FirstOrDefault ();

            return defaultTax?.Postotak != null ? $"{defaultTax.Postotak:0.##}%" : string.Empty;
        }

        private static bool IsRecognizedImportTax(string value, ArticlesViewModel viewModel)
        {
            string text = value?.Trim () ?? string.Empty;

            if(string.IsNullOrWhiteSpace (text))
                return false;

            string taxText = text.TrimEnd ('%').Trim ().Replace (',', '.');

            if(!decimal.TryParse (taxText, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal rate))
                return false;

            if(viewModel.PoreskeStope.Any (x => x.Aktivna && x.Postotak == rate))
                return true;

            return int.TryParse (text, out int id) && viewModel.PoreskeStope.Any (x => x.Aktivna && x.IdStope == id);
        }

        // Pretvara ID kategorije u njen prikazni naziv kada je moguće.
        private static string ResolveImportKategorija(string value, ArticlesViewModel viewModel)
        {
            if(!int.TryParse (value, out int id))
                return value;

            return viewModel.Kategorije.FirstOrDefault (x => x.IdKategorije == id)?.ToString () ?? value;
        }

        #endregion


        #region UVOZ ARTIKALA - MAPIRANJE KOLONA

        // Kreira listu izvornih kolona koje korisnik može povezati sa Caupo poljima.
        private List<ArticleImportMappingOption> CreateArticleImportMappingOptions(ArticleImportSourceData source)
        {
            List<ArticleImportMappingOption> result = new ();

            foreach(ArticleImportColumnPreview column in CreateArticleImportColumnPreviews (source))
            {
                result.Add (new ArticleImportMappingOption
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

        // Pamti odabrano mapiranje kolone i prikazuje primjer podatka iz izvora.
        private void ArticleImportMapperComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(_articleImportSource == null || _articleImportSource.Rows.Count == 0 || sender is not ComboBox comboBox)
                return;

            string? columnName = (comboBox.SelectedItem as ArticleImportMappingOption)?.SourceColumn;

            if(string.IsNullOrWhiteSpace (columnName))
                return;

            string example = _articleImportSource.Rows[0].GetString (columnName);

            TextBlock? exampleText = comboBox.Name switch
            {
                nameof (ImportCodeComboBox) => ImportCodeExampleText,
                nameof (ImportInternalCodeComboBox) => ImportInternalCodeExampleText,
                nameof (ImportNameComboBox) => ImportNameExampleText,
                nameof (ImportPriceComboBox) => ImportPriceExampleText,
                nameof (ImportTypeComboBox) => ImportTypeExampleText,
                nameof (ImportCategoryComboBox) => ImportCategoryExampleText,
                nameof (ImportUnitComboBox) => ImportUnitExampleText,
                nameof (ImportTaxComboBox) => ImportTaxExampleText,
                nameof (ImportNormativComboBox) => ImportNormativExampleText,
                nameof (ImportPositionComboBox) => ImportPositionExampleText,
                nameof (ImportActiveComboBox) => ImportActiveExampleText,
                nameof (ImportConsumptionTaxComboBox) => ImportConsumptionTaxExampleText,
                nameof (ImportImageComboBox) => ImportImageExampleText,
                _ => null
            };

            if(exampleText != null)
                exampleText.Text = example;

            string? targetColumn = comboBox.Name switch
            {
                nameof (ImportCodeComboBox) => "Šifra",
                nameof (ImportInternalCodeComboBox) => "Interna šifra",
                nameof (ImportNameComboBox) => "Naziv",
                nameof (ImportPriceComboBox) => "Cijena",
                nameof (ImportTypeComboBox) => "Vrsta",
                nameof (ImportCategoryComboBox) => "Kategorija",
                nameof (ImportUnitComboBox) => "Jedinica",
                nameof (ImportTaxComboBox) => "Porez",
                nameof (ImportNormativComboBox) => "Normativ",
                nameof (ImportPositionComboBox) => "Pozicija",
                nameof (ImportActiveComboBox) => "Aktivan",
                nameof (ImportConsumptionTaxComboBox) => "Porez na potrošnju",
                nameof (ImportImageComboBox) => "Slika",
                _ => null
            };

            if(string.IsNullOrWhiteSpace (targetColumn) || _articleImportMappings == null)
                return;

            ArticleImportMapping? mapping = _articleImportMappings.FirstOrDefault (x => x.TargetColumn == targetColumn);

            if(mapping != null)
                ArticleImportMapper.MapSourceColumn (mapping, columnName);
        }

        // Priprema do tri primjera vrijednosti za svaku kolonu uvoznog fajla.
        private List<ArticleImportColumnPreview> CreateArticleImportColumnPreviews(ArticleImportSourceData source)
        {
            List<ArticleImportColumnPreview> result = new ();

            foreach(string columnName in source.Columns)
            {
                List<string> examples = source.Rows
                    .Select (x => x.GetString (columnName))
                    .Where (x => !string.IsNullOrWhiteSpace (x))
                    .Take (3)
                    .ToList ();

                result.Add (new ArticleImportColumnPreview
                {
                    ColumnName = columnName,
                    Example1 = examples.ElementAtOrDefault (0) ?? string.Empty,
                    Example2 = examples.ElementAtOrDefault (1) ?? string.Empty,
                    Example3 = examples.ElementAtOrDefault (2) ?? string.Empty
                });
            }

            return result;
        }

        // Puni sve ComboBox kontrole mapera dostupnim kolonama iz izvornog fajla.
        private void LoadArticleImportMapperColumns(ArticleImportSourceData source)
        {
            List<ArticleImportMappingOption> options = CreateArticleImportMappingOptions (source);

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

            foreach(ComboBox comboBox in comboBoxes)
            {
                comboBox.ItemsSource = options;
                comboBox.SelectedValuePath = nameof (ArticleImportMappingOption.SourceColumn);
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

            foreach(TextBlock exampleText in exampleTexts)
                exampleText.Text = string.Empty;
        }

        // Provjerava mapiranje, primjenjuje podrazumijevane vrijednosti i otvara transfer editor.
        private void BtnArticleImportMapperContinue_Click(object sender, RoutedEventArgs e)
        {
            if(_articleImportSource == null || _articleImportMappings == null)
                return;

            ArticleImportMapping? nameMapping = _articleImportMappings.FirstOrDefault (x => x.TargetColumn == "Naziv");

            if(nameMapping == null || nameMapping.MappingType != ArticleImportMappingType.SourceColumn || string.IsNullOrWhiteSpace (nameMapping.SourceColumn))
            {
                ShowArticleImportMapperMessage ("Za uvoz artikala morate odabrati podatak koji predstavlja naziv artikla.");
                return;
            }

            Debug.WriteLine ($"[ARTICLE IMPORT] Naziv ← {nameMapping.SourceColumn}");
            Debug.WriteLine ("[ARTICLE IMPORT] Osnovna provjera mapiranja prošla.");

            List<ArticleImportMappedRow> mappedRows = new ();

            ArticleImportMapping? taxMapping = _articleImportMappings.FirstOrDefault (x => x.TargetColumn == "Porez");

            TblPoreskeStope? highestTax = null;

            if(taxMapping == null || taxMapping.MappingType == ArticleImportMappingType.None)
            {
                highestTax = ((ArticlesViewModel)DataContext).PoreskeStope
                    .Where (x => x.Aktivna)
                    .OrderByDescending (x => x.Postotak ?? 0)
                    .FirstOrDefault ();
            }

            foreach(ArticleImportSourceRow sourceRow in _articleImportSource.Rows)
            {
                ArticleImportMappedRow mappedRow = ArticleImportMapper.MapRow (sourceRow, _articleImportMappings);

                ArticleImportDefaults.Apply (mappedRow, _articleImportMappings);

                if(highestTax != null)
                    mappedRow.Values["Porez"] = $"{highestTax.Postotak:0.##}%";

                Debug.WriteLine ($"[TAX DEBUG] MAPPER ROW | HighestTaxId='{highestTax?.IdStope}' | Postotak='{highestTax?.Postotak}' | Opis='{highestTax?.Opis}' | MappedTax='{mappedRow.GetString ("Porez")}'");

                mappedRows.Add (mappedRow);
            }

            ArticlesViewModel viewModel = (ArticlesViewModel)DataContext;

            ArticleImportMapping? codeMapping = _articleImportMappings.FirstOrDefault (x => x.TargetColumn == "Šifra");
            ArticleImportMapping? internalCodeMapping = _articleImportMappings.FirstOrDefault (x => x.TargetColumn == "Interna šifra");
            ArticleImportMapping? positionMapping = _articleImportMappings.FirstOrDefault (x => x.TargetColumn == "Pozicija");
            ArticleImportMapping? priceMapping = _articleImportMappings.FirstOrDefault (x => x.TargetColumn == "Cijena");
            ArticleImportMapping? typeMapping = _articleImportMappings.FirstOrDefault (x => x.TargetColumn == "Vrsta");
            ArticleImportMapping? categoryMapping = _articleImportMappings.FirstOrDefault (x => x.TargetColumn == "Kategorija");
            ArticleImportMapping? unitMapping = _articleImportMappings.FirstOrDefault (x => x.TargetColumn == "Jedinica");
            ArticleImportMapping? normativMapping = _articleImportMappings.FirstOrDefault (x => x.TargetColumn == "Normativ");

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
                .Select (x => int.TryParse (x.Sifra, out int value) ? value : 0)
                .DefaultIfEmpty (0)
                .Max () + 1;

            int nextInternalCode = viewModel.Artikli
                .Select (x => int.TryParse (x.InternaSifra, out int value) ? value : 0)
                .DefaultIfEmpty (0)
                .Max () + 1;

            int nextPosition = viewModel.Artikli
                .Select (x => x.Pozicija ?? 0)
                .DefaultIfEmpty (0)
                .Max () + 1;

            foreach(ArticleImportMappedRow row in mappedRows)
            {
                if(generateCode)
                    row.Values["Šifra"] = (nextCode++).ToString ();

                if(generateInternalCode)
                    row.Values["Interna šifra"] = (nextInternalCode++).ToString ();

                if(generatePosition)
                    row.Values["Pozicija"] = (nextPosition++).ToString ();
            }

            Debug.WriteLine ($"[ARTICLE IMPORT] Pripremljeno redova: {mappedRows.Count}");

            foreach(ArticleImportMappedRow row in mappedRows.Take (3))
                Debug.WriteLine ($"[ARTICLE IMPORT] Red {row.SourceRowNumber}: Šifra={row.GetString ("Šifra")}, Interna={row.GetString ("Interna šifra")}, Naziv={row.GetString ("Naziv")}, Pozicija={row.GetString ("Pozicija")}, Porez={row.GetString ("Porez")}");

            _articleTransferRows.Clear ();

            foreach(ArticleImportMappedRow row in mappedRows)
            {
                var transferRow = new ArticleTransferRow (viewModel.Kategorije)
                {

                    Sifra = row.GetString ("Šifra"),
                    InternaSifra = row.GetString ("Interna šifra"),
                    Artikl = row.GetString ("Naziv"),
                    Cijena = row.GetString ("Cijena"),
                    Vrsta = row.GetString ("Vrsta"),
                    Kategorija = row.GetString ("Kategorija"),
                    Jedinica = row.GetString ("Jedinica"),
                    Porez = row.GetString ("Porez"),
                    Normativ = row.GetString ("Normativ"),
                    Pozicija = row.GetString ("Pozicija"),
                    Aktivan = ParseTransferBool (row.GetString ("Aktivan"), true),
                    PorezNaPotrosnju = ParseTransferBool (row.GetString ("Porez na potrošnju"), false),
                    Slika = row.GetString ("Slika")
                };

                Debug.WriteLine ($"[TAX DEBUG] TRANSFER ROW CREATED | Artikl='{transferRow.Artikl}' | SourceMappedTax='{row.GetString ("Porez")}' | TransferTax='{transferRow.Porez}'");

                if(defaultPrice)
                    transferRow.DefaultedFields.Add ("Cijena");

                if(defaultType)
                    transferRow.DefaultedFields.Add ("Vrsta");

                if(defaultCategory)
                    transferRow.DefaultedFields.Add ("Kategorija");

                if(defaultUnit)
                    transferRow.DefaultedFields.Add ("Jedinica");

                if(defaultTax)
                    transferRow.DefaultedFields.Add ("Porez");

                if(defaultNormativ)
                    transferRow.DefaultedFields.Add ("Normativ");

                _articleTransferRows.Add (transferRow);
            }

            Debug.WriteLine ($"[ARTICLE IMPORT] Pripremljeno redova za editor: {_articleTransferRows.Count}");

            ArticleImportMapperGrid.Visibility = Visibility.Collapsed;

            OpenArticleTransferEditor (
                "Uvoz artikala",
                $"Pripremljeno {_articleTransferRows.Count} artikala. Pregledajte i uredite podatke prije validacije i spremanja."
            );
        }

        // Prikazuje poruku vezanu za mapiranje uvoznog fajla.
        private void ShowArticleImportMapperMessage(string message)
        {
            MyMessageBox messageBox = new ();
            messageBox.MessageTitle.Text = "Uvoz artikala";
            messageBox.MessageText.Text = message;
            messageBox.ShowDialog ();
        }

        // Zatvara mapper kolona bez nastavka uvoza.
        private void BtnArticleImportMapperClose_Click(object sender, RoutedEventArgs e)
        {
            ArticleImportMapperGrid.Visibility = Visibility.Collapsed;
        }

        #endregion


        #region TRANSFER EDITOR - OTVARANJE, UČITAVANJE I IZVOZ

        // Učitava postojeće Caupo artikle u transfer editor radi pregleda i izvoza.
        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is not ArticlesViewModel viewModel)
                return;

            LoadTransferRowsFromCaupo (viewModel);
            OpenArticleTransferEditor ("Izvoz artikala", "Artikli su učitani iz Caupa. Možete ih urediti, validirati i zatim izvesti u XLSX.");
        }

        // Priprema i prikazuje transfer editor sa zadatim naslovom i opisom.
        private async void OpenArticleTransferEditor(string title, string description)
        {
            ArticleTransferTitle.Text = title;
            ArticleTransferSummary.Text = description;
            ArticleTransferDataGrid.ItemsSource = _articleTransferRows;
            ArticleTransferErrorGrid.ItemsSource = _articleTransferErrors;
            ResetTransferValidation ();
            ArticleTransferGrid.Visibility = Visibility.Visible;

            await ValidateArticleTransferAsync ();
        }

        // Pretvara postojeće Caupo artikle u privremene redove transfer editora.
        private void LoadTransferRowsFromCaupo(ArticlesViewModel viewModel)
        {
            _articleTransferRows.Clear ();

            foreach(var article in viewModel.Artikli.OrderBy (x => x.Pozicija).ThenBy (x => x.IdArtikla))
            {
                string vrsta = article.VrstaArtikla switch { 0 => "Piće", 1 => "Hrana", 2 => "Ostalo", _ => string.Empty };
                string kategorija = viewModel.Kategorije.FirstOrDefault (x => x.IdKategorije == article.Kategorija)?.Kategorija ?? string.Empty;
                string jedinica = viewModel.JediniceMjere.FirstOrDefault (x => x.IdJedinice == article.JedinicaMjere)?.ToString () ?? string.Empty;
                var poreskaStopa = viewModel.PoreskeStope.FirstOrDefault (x => x.IdStope == article.PoreskaStopa);
                string porez = poreskaStopa?.Postotak != null ? $"{poreskaStopa.Postotak:0.##}%" : string.Empty;

                _articleTransferRows.Add (new ArticleTransferRow (viewModel.Kategorije)
                {

                    Sifra = article.Sifra ?? string.Empty,
                    InternaSifra = article.InternaSifra ?? string.Empty,
                    Artikl = article.Artikl ?? string.Empty,
                    Cijena = article.Cijena?.ToString ("0.00", CultureInfo.CurrentCulture) ?? string.Empty,
                    Vrsta = vrsta,
                    Kategorija = kategorija,
                    Jedinica = jedinica,
                    Porez = porez,
                    Normativ = article.Normativ?.ToString (CultureInfo.InvariantCulture) ?? "1",
                    Pozicija = article.Pozicija?.ToString () ?? string.Empty,
                    Aktivan = article.Aktivan,
                    PorezNaPotrosnju = article.PorezNaPotrosnju,
                    Slika = article.Slika ?? string.Empty
                });
            }
        }

        // Učitava podržanu datoteku u transfer editor.
        private void BtnTransferLoadFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog { Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*" };
            if(dialog.ShowDialog () != true)
                return;

            try
            {
                using var workbook = new XLWorkbook (dialog.FileName);
                var sheet = workbook.Worksheets.First ();
                var headers = sheet.Row (1).CellsUsed ().ToDictionary (x => x.GetString ().Trim (), x => x.Address.ColumnNumber, StringComparer.OrdinalIgnoreCase);

                string[] required = { "Šifra", "Interna šifra", "Naziv", "Cijena", "Vrsta", "Kategorija", "Jedinica", "Porez", "Normativ", "Pozicija", "Aktivan" };
                var missing = required.Where (x => !headers.ContainsKey (x)).ToList ();
                if(missing.Count > 0)
                {
                    ShowArticleEditMessage ("GREŠKA", "Fajl nema potrebne kolone: " + string.Join (", ", missing));
                    return;
                }

                _articleTransferRows.Clear ();
                foreach(var row in sheet.RowsUsed ().Skip (1))
                {
                    if(row.CellsUsed ().All (x => string.IsNullOrWhiteSpace (x.GetString ())))
                        continue;

                    int id = 0;
                    if(headers.TryGetValue ("ID", out int idCol))
                        int.TryParse (row.Cell (idCol).GetString (), out id);

                    _articleTransferRows.Add (new ArticleTransferRow (((ArticlesViewModel)DataContext).Kategorije)
                    {

                        Sifra = row.Cell (headers["Šifra"]).GetString ().Trim (),
                        InternaSifra = row.Cell (headers["Interna šifra"]).GetString ().Trim (),
                        Artikl = row.Cell (headers["Naziv"]).GetString ().Trim (),
                        Cijena = row.Cell (headers["Cijena"]).GetFormattedString ().Trim (),
                        Vrsta = row.Cell (headers["Vrsta"]).GetString ().Trim (),
                        Kategorija = row.Cell (headers["Kategorija"]).GetString ().Trim (),
                        Jedinica = row.Cell (headers["Jedinica"]).GetString ().Trim (),
                        Porez = row.Cell (headers["Porez"]).GetString ().Trim (),
                        Normativ = row.Cell (headers["Normativ"]).GetFormattedString ().Trim (),
                        Pozicija = row.Cell (headers["Pozicija"]).GetFormattedString ().Trim (),
                        Aktivan = ParseTransferBool (row.Cell (headers["Aktivan"]).GetFormattedString (), true),
                        PorezNaPotrosnju = headers.TryGetValue ("Porez na potrošnju", out int potrosnjaCol) && ParseTransferBool (row.Cell (potrosnjaCol).GetFormattedString (), false)
                    });
                }

                ArticleTransferTitle.Text = "Uvoz artikala";
                ArticleTransferSummary.Text = $"Učitano iz: {Path.GetFileName (dialog.FileName)}. Podaci još nisu upisani u bazu.";
                ResetTransferValidation ();
                ArticleTransferFooter.Text = $"Učitano redova: {_articleTransferRows.Count}";
            }
            catch(Exception ex)
            {
                Debug.WriteLine ("[ARTICLES] Import editor load: " + ex);
                ShowArticleEditMessage ("GREŠKA", "Fajl nije moguće učitati. Provjerite da li je ispravan XLSX fajl.");
            }
        }

        // Otvara izbor formata za izvoz podataka iz transfer editora.
        private void BtnTransferSaveFile_Click(object sender, RoutedEventArgs e)
        {
            ArticleTransferExportPopup.IsOpen = !ArticleTransferExportPopup.IsOpen;
        }

        // Traži odredište i izvozi trenutne transfer redove u XLSX.
        private void BtnTransferExportXlsx_Click(object sender, RoutedEventArgs e)
        {
            ArticleTransferExportPopup.IsOpen = false;
            ArticleTransferDataGrid.CommitEdit (DataGridEditingUnit.Cell, true);
            ArticleTransferDataGrid.CommitEdit (DataGridEditingUnit.Row, true);

            SaveFileDialog dialog = new ()
            {
                Title = "Izvezi artikle u Excel",
                Filter = "Excel datoteka (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                AddExtension = true,
                FileName = "Caupo_Artikli.xlsx"
            };

            if(dialog.ShowDialog () != true)
                return;

            try
            {
                ExportArticlesToXlsx (dialog.FileName);
                ArticleTransferFooter.Text = $"Izvezeno {_articleTransferRows.Count} artikala u {Path.GetFileName (dialog.FileName)}";
            }
            catch(Exception ex)
            {
                Debug.WriteLine ("[ARTICLES] XLSX export: " + ex);
                ShowArticleEditMessage ("GREŠKA", "XLSX fajl nije spremljen.");
            }
        }

        // Traži odredište i izvozi trenutne transfer redove u CSV.
        private void BtnTransferExportCsv_Click(object sender, RoutedEventArgs e)
        {
            ArticleTransferExportPopup.IsOpen = false;
            ArticleTransferDataGrid.CommitEdit (DataGridEditingUnit.Cell, true);
            ArticleTransferDataGrid.CommitEdit (DataGridEditingUnit.Row, true);

            SaveFileDialog dialog = new ()
            {
                Title = "Izvezi artikle u CSV",
                Filter = "CSV datoteka (*.csv)|*.csv",
                DefaultExt = ".csv",
                AddExtension = true,
                FileName = "Caupo_Artikli.csv"
            };

            if(dialog.ShowDialog () != true)
                return;

            try
            {
                ExportArticlesToCsv (dialog.FileName);
                ArticleTransferFooter.Text = $"Izvezeno {_articleTransferRows.Count} artikala u {Path.GetFileName (dialog.FileName)}";
            }
            catch(Exception ex)
            {
                Debug.WriteLine ("[ARTICLES] CSV export: " + ex);
                ShowArticleEditMessage ("GREŠKA", "CSV fajl nije spremljen.");
            }
        }

        // Upisuje transfer redove u Caupo XLSX format.
        private void ExportArticlesToXlsx(string filePath)
        {
            using var workbook = new XLWorkbook ();
            var sheet = workbook.Worksheets.Add ("Artikli");

            string[] headers = { "Sifra", "InternaSifra", "Artikl", "Cijena", "VrstaArtikla", "Kategorija", "JedinicaMjere", "PoreskaStopa", "Normativ", "Pozicija", "Aktivan", "PorezNaPotrosnju", "Slika" };

            for(int i = 0; i < headers.Length; i++)
                sheet.Cell (1, i + 1).Value = headers[i];

            int r = 2;

            foreach(var item in _articleTransferRows)
            {
                sheet.Cell (r, 1).Value = item.Sifra;
                sheet.Cell (r, 2).Value = item.InternaSifra;
                sheet.Cell (r, 3).Value = item.Artikl;
                sheet.Cell (r, 4).Value = item.Cijena;
                sheet.Cell (r, 5).Value = item.Vrsta;
                sheet.Cell (r, 6).Value = item.Kategorija;
                sheet.Cell (r, 7).Value = item.Jedinica;
                sheet.Cell (r, 8).Value = item.Porez;
                sheet.Cell (r, 9).Value = item.Normativ;
                sheet.Cell (r, 10).Value = item.Pozicija;
                sheet.Cell (r, 11).Value = item.Aktivan ? "DA" : "NE";
                sheet.Cell (r, 12).Value = item.PorezNaPotrosnju ? "DA" : "NE";
                sheet.Cell (r, 13).Value = item.Slika;
                r++;
            }

            sheet.SheetView.FreezeRows (1);
            sheet.RangeUsed ().SetAutoFilter ();
            sheet.Columns ().AdjustToContents ();
            workbook.SaveAs (filePath);
        }

        // Upisuje transfer redove u Caupo CSV format.
        private void ExportArticlesToCsv(string filePath)
        {
            using StreamWriter writer = new (filePath, false, new UTF8Encoding (true));

            string[] headers = { "Sifra", "InternaSifra", "Artikl", "Cijena", "VrstaArtikla", "Kategorija", "JedinicaMjere", "PoreskaStopa", "Normativ", "Pozicija", "Aktivan", "PorezNaPotrosnju", "Slika" };

            writer.WriteLine (string.Join (";", headers.Select (EscapeCsvValue)));

            foreach(var item in _articleTransferRows)
            {
                string[] values =
                {
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

                writer.WriteLine (string.Join (";", values.Select (EscapeCsvValue)));
            }
        }

        // Sigurno formatira jednu vrijednost za zapis u CSV.
        private static string EscapeCsvValue(string? value)
        {
            string text = value ?? string.Empty;

            if(text.Contains ('"'))
                text = text.Replace ("\"", "\"\"");

            if(text.Contains (';') || text.Contains ('"') || text.Contains ('\r') || text.Contains ('\n'))
                text = $"\"{text}\"";

            return text;
        }

        // Zatvara transfer editor i čisti njegov privremeni sadržaj.
        private void BtnTransferClose_Click(object sender, RoutedEventArgs e)
        {
            ArticleTransferGrid.Visibility = Visibility.Collapsed;
            _articleTransferRows.Clear ();
            _articleTransferErrors.Clear ();
            ResetTransferValidation ();
        }

        // Vraća transfer editor u početno stanje validacije.
        private void ResetTransferValidation()
        {
            _articleTransferValidated = false;
            BtnTransferApply.IsEnabled = false;
            _articleTransferErrors.Clear ();
            ArticleTransferErrorTitle.Text = "Greške (0)";
            ArticleTransferValidationSummary.Text = "Nije validirano";
        }

        #endregion


        #region TRANSFER EDITOR - VALIDACIJA I PRIMJENA

        // Ručno pokreće validaciju transfer redova ako je handler još povezan iz XAML-a.
        private async void BtnTransferValidate_Click(object sender, RoutedEventArgs e)
        {
            await ValidateArticleTransferAsync ();
        }

        // Validira sve transfer redove, označava greške i upozorenja te određuje da li je import dozvoljen.
        private async Task<bool> ValidateArticleTransferAsync()
        {
            _articleTransferErrors.Clear ();

            if(DataContext is not ArticlesViewModel viewModel)
                return false;

            await using var db = new AppDbContext ();
            var dbArticles = await db.Artikli.AsNoTracking ().ToListAsync ();
            int rowNumber = 1;

            foreach(var row in _articleTransferRows)
            {
                rowNumber++;

                row.ErrorFields.Clear ();
                row.WarningFields.Clear ();

                void Error(string field, string message)
                {
                    row.ErrorFields.Add (field);
                    _articleTransferErrors.Add (new ArticleTransferError { RowNumber = rowNumber, ColumnName = field, Message = message });
                }

                if(string.IsNullOrWhiteSpace (row.Sifra))
                    Error ("Šifra", "Šifra artikla nije unesena.");

                if(string.IsNullOrWhiteSpace (row.InternaSifra))
                    Error ("Interna šifra", "Interna šifra nije unesena.");

                if(string.IsNullOrWhiteSpace (row.Artikl))
                    Error ("Naziv", "Naziv artikla nije unesen.");

                if(!TryParseArticlePrice (row.Cijena, out decimal cijena) || cijena <= 0)
                    Error ("Cijena", $"Artikl '{row.Artikl}' mora imati cijenu veću od 0.");

                int? typeId = MapTransferType (row.Vrsta);

                if(typeId == null)
                    Error ("Vrsta", $"Za artikl '{row.Artikl}' izaberite Piće, Hrana ili Ostalo.");

                var category = typeId == null ? null : viewModel.Kategorije.FirstOrDefault (x => x.VrstaArtikla == typeId && string.Equals (x.Kategorija?.Trim (), row.Kategorija?.Trim (), StringComparison.OrdinalIgnoreCase));

                if(category == null)
                    Error ("Kategorija", $"Kategorija '{row.Kategorija}' ne pripada izabranoj vrsti artikla.");

                var unit = viewModel.JediniceMjere.FirstOrDefault (x => string.Equals (x.ToString ().Trim (), row.Jedinica?.Trim (), StringComparison.OrdinalIgnoreCase));

                if(unit == null)
                    Error ("Jedinica", $"Jedinica mjere '{row.Jedinica}' ne postoji u Caupu.");

                string taxText = row.Porez?.Trim ().TrimEnd ('%').Trim () ?? string.Empty;

                var tax = decimal.TryParse (taxText.Replace (',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal taxRate)
                    ? viewModel.PoreskeStope.FirstOrDefault (x => x.Postotak == taxRate)
                    : null;

                if(tax == null)
                    Error ("Porez", $"Poreska stopa '{row.Porez}' ne postoji ili nije aktivna.");

                decimal? normativ = ParseArticleNormativ (row.Normativ);

                if(typeId == 0)
                {
                    if(!normativ.HasValue || normativ <= 0)
                        Error ("Normativ", $"Artikl '{row.Artikl}' mora imati ispravan normativ veći od 0.");
                    else if(unit != null && string.Equals (unit.ToString ().Trim (), "kom", StringComparison.OrdinalIgnoreCase) && normativ != 1m)
                        Error ("Normativ", $"Artikl '{row.Artikl}' ima jedinicu mjere 'kom', ali normativ nije 1. Provjerite jedinicu mjere ili normativ.");
                }
                else if(typeId == 1 || typeId == 2)
                {
                    if(!normativ.HasValue || normativ != 1m)
                        Error ("Normativ", $"Artikl '{row.Artikl}' vrste '{row.Vrsta}' mora imati normativ 1.");
                }

                if(!int.TryParse (row.Pozicija, out int pozicija) || pozicija <= 0)
                    Error ("Pozicija", $"Artikl '{row.Artikl}' mora imati ispravnu poziciju.");

                string artiklNormativ = BuildArticleNormativ (row.Artikl, typeId == 0 ? row.Normativ : "1");

                var duplicateCode = dbArticles.FirstOrDefault (x => string.Equals (x.Sifra?.Trim (), row.Sifra?.Trim (), StringComparison.OrdinalIgnoreCase));

                if(duplicateCode != null)
                    Error ("Šifra", $"Šifra '{row.Sifra}' već pripada artiklu '{duplicateCode.Artikl}'.");

                var duplicateInternal = dbArticles.FirstOrDefault (x => string.Equals (x.InternaSifra?.Trim (), row.InternaSifra?.Trim (), StringComparison.OrdinalIgnoreCase));

                if(duplicateInternal != null)
                    Error ("Interna šifra", $"Interna šifra '{row.InternaSifra}' već pripada artiklu '{duplicateInternal.Artikl}'.");

                var duplicateNorm = dbArticles.FirstOrDefault (x => string.Equals (x.ArtiklNormativ?.Trim (), artiklNormativ.Trim (), StringComparison.OrdinalIgnoreCase));

                if(duplicateNorm != null)
                    Error ("Naziv / Normativ", $"Naziv za prodaju '{artiklNormativ}' već postoji kod artikla '{duplicateNorm.Artikl}'.");

                void Warning(string field, string message)
                {
                    row.WarningFields.Add (field);
                    _articleTransferErrors.Add (new ArticleTransferError { Type = "Upozorenje", RowNumber = rowNumber, ColumnName = field, Message = message });
                }

                if(row.DefaultedFields.Contains ("Cijena"))
                    Warning ("Cijena", $"Za artikl '{row.Artikl}' cijena nije pronađena u izvoru. Caupo je automatski postavio {row.Cijena}.");

                if(row.DefaultedFields.Contains ("Vrsta"))
                    Warning ("Vrsta", $"Za artikl '{row.Artikl}' vrsta nije pronađena u izvoru. Caupo je automatski postavio '{row.Vrsta}'.");

                if(row.DefaultedFields.Contains ("Kategorija"))
                    Warning ("Kategorija", $"Za artikl '{row.Artikl}' kategorija nije pronađena u izvoru. Caupo je automatski postavio '{row.Kategorija}'.");

                if(row.DefaultedFields.Contains ("Jedinica"))
                    Warning ("Jedinica", $"Za artikl '{row.Artikl}' jedinica mjere nije pronađena u izvoru. Caupo je automatski postavio '{row.Jedinica}'.");

                if(row.DefaultedFields.Contains ("Porez"))
                    Warning ("Porez", $"Za artikl '{row.Artikl}' porez nije pronađen u izvoru. Caupo je automatski postavio '{row.Porez}'.");

                if(row.DefaultedFields.Contains ("Normativ"))
                    Warning ("Normativ", $"Za artikl '{row.Artikl}' normativ nije pronađen u izvoru. Caupo je automatski postavio {row.Normativ}.");

                row.RefreshValidationState ();
            }

            foreach(var group in _articleTransferRows.Where (x => !string.IsNullOrWhiteSpace (x.Sifra)).GroupBy (x => x.Sifra.Trim (), StringComparer.OrdinalIgnoreCase).Where (x => x.Count () > 1))
                _articleTransferErrors.Add (new ArticleTransferError { RowNumber = 0, ColumnName = "Šifra", Message = $"Šifra '{group.Key}' se pojavljuje više puta u editoru." });

            foreach(var group in _articleTransferRows.Where (x => !string.IsNullOrWhiteSpace (x.InternaSifra)).GroupBy (x => x.InternaSifra.Trim (), StringComparer.OrdinalIgnoreCase).Where (x => x.Count () > 1))
                _articleTransferErrors.Add (new ArticleTransferError { RowNumber = 0, ColumnName = "Interna šifra", Message = $"Interna šifra '{group.Key}' se pojavljuje više puta u editoru." });

            foreach(var group in _articleTransferRows.Where (x => !string.IsNullOrWhiteSpace (x.Artikl)).GroupBy (x => BuildArticleNormativ (x.Artikl, x.Normativ), StringComparer.OrdinalIgnoreCase).Where (x => x.Count () > 1))
                _articleTransferErrors.Add (new ArticleTransferError { RowNumber = 0, ColumnName = "Naziv / Normativ", Message = $"Naziv za prodaju '{group.Key}' se pojavljuje više puta u editoru." });

            int errorCount = _articleTransferErrors.Count (x => x.Type == "Greška");
            int warningCount = _articleTransferErrors.Count (x => x.Type == "Upozorenje");

            _articleTransferValidated = errorCount == 0;
            BtnTransferApply.IsEnabled = _articleTransferValidated;

            ArticleTransferErrorTitle.Text = $"Greške ({errorCount}) | Upozorenja ({warningCount})";
            ArticleTransferValidationSummary.Text = $"Redova: {_articleTransferRows.Count} | Greške: {errorCount} | Upozorenja: {warningCount}";

            if(errorCount > 0)
                ArticleTransferFooter.Text = "Ispravite navedene greške prije primjene podataka.";
            else if(warningCount > 0)
                ArticleTransferFooter.Text = "Nema grešaka. Pregledajte upozorenja prije primjene podataka.";
            else
                ArticleTransferFooter.Text = "Validacija uspješna. Podaci su spremni za primjenu.";

            return _articleTransferValidated;
        }

        // Upisuje sve validne transfer redove kao nove artikle, osvježava listu i fokusira prvi uvezeni artikl.
        private async void BtnTransferApply_Click(object sender, RoutedEventArgs e)
        {
            if(!await ValidateArticleTransferAsync () || DataContext is not ArticlesViewModel viewModel)
                return;

            int importedCount = _articleTransferRows.Count;
            int firstImportedId;

            // FAZA 1: Kopiranje slika importa u Images\Articles\Imported.
            try
            {
                string importedFolder = Path.Combine (ArticleImageStorage.ArticlesFolder, "Imported");
                Directory.CreateDirectory (importedFolder);

                foreach(var row in _articleTransferRows)
                {
                    if(string.IsNullOrWhiteSpace (row.Slika))
                        continue;

                    string sourcePath = row.Slika.Trim ();

                    if(!Path.IsPathRooted (sourcePath) || !File.Exists (sourcePath))
                        continue;

                    string destinationPath = Path.Combine (importedFolder, Path.GetFileName (sourcePath));

                    if(!string.Equals (Path.GetFullPath (sourcePath), Path.GetFullPath (destinationPath), StringComparison.OrdinalIgnoreCase))
                        File.Copy (sourcePath, destinationPath, true);

                    row.Slika = destinationPath;
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine ("[ARTICLES] Import editor apply - images: " + ex);

                _articleTransferErrors.Add (new ArticleTransferError
                {
                    RowNumber = 0,
                    ColumnName = "Slika",
                    Message = "Slike artikala nisu uspješno kopirane. Import nije izvršen."
                });

                ArticleTransferErrorTitle.Text = $"Greške ({_articleTransferErrors.Count})";
                ArticleTransferFooter.Text = "Import nije izvršen. Slike artikala nisu uspješno kopirane.";
                BtnTransferApply.IsEnabled = false;
                return;
            }

            // FAZA 2: Upis artikala u bazu.
            try
            {
                await using var db = new AppDbContext ();
                await using var transaction = await db.Database.BeginTransactionAsync ();

                TblArtikli? firstImportedArticle = null;

                foreach(var row in _articleTransferRows)
                {
                    var article = new TblArtikli ();
                    db.Artikli.Add (article);

                    firstImportedArticle ??= article;

                    int typeId = MapTransferType (row.Vrsta)!.Value;
                    var category = viewModel.Kategorije.First (x => x.VrstaArtikla == typeId && string.Equals (x.Kategorija?.Trim (), row.Kategorija.Trim (), StringComparison.OrdinalIgnoreCase));
                    var unit = viewModel.JediniceMjere.First (x => string.Equals (x.ToString ().Trim (), row.Jedinica.Trim (), StringComparison.OrdinalIgnoreCase));

                    string taxText = row.Porez.Trim ().TrimEnd ('%').Trim ();
                    decimal.TryParse (taxText.Replace (',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal taxRate);
                    var tax = viewModel.PoreskeStope.First (x => x.Postotak == taxRate);

                    TryParseArticlePrice (row.Cijena, out decimal price);
                    decimal normativ = typeId == 0 ? ParseArticleNormativ (row.Normativ)!.Value : 1m;
                    int.TryParse (row.Pozicija, out int position);

                    article.Sifra = row.Sifra.Trim ();
                    article.InternaSifra = row.InternaSifra.Trim ();
                    article.Artikl = row.Artikl.Trim ();
                    article.Cijena = price;
                    article.VrstaArtikla = typeId;
                    article.Kategorija = category.IdKategorije;
                    article.JedinicaMjere = unit.IdJedinice;
                    article.PoreskaStopa = tax.IdStope;
                    article.Normativ = normativ;
                    article.Pozicija = position;
                    article.Aktivan = row.Aktivan;
                    article.PorezNaPotrosnju = string.Equals (Settings.Default.Country, "Hrvatska", StringComparison.OrdinalIgnoreCase) && row.PorezNaPotrosnju;
                    article.ArtiklNormativ = BuildArticleNormativ (article.Artikl, typeId == 0 ? row.Normativ : "1");
                    article.Slika = string.IsNullOrWhiteSpace (row.Slika) ? null : row.Slika.Trim ();
                }

                await db.SaveChangesAsync ();

                firstImportedId = firstImportedArticle!.IdArtikla;

                await transaction.CommitAsync ();
            }
            catch(Exception ex)
            {
                Debug.WriteLine ("[ARTICLES] Import editor apply - database: " + ex);

                _articleTransferErrors.Add (new ArticleTransferError
                {
                    RowNumber = 0,
                    ColumnName = "Baza",
                    Message = "Import nije izvršen. Nijedan artikl nije dodan."
                });

                ArticleTransferErrorTitle.Text = $"Greške ({_articleTransferErrors.Count})";
                ArticleTransferFooter.Text = "Import nije izvršen. Nijedan artikl nije dodan.";
                BtnTransferApply.IsEnabled = false;
                return;
            }

            // FAZA 3: Osvježavanje korisničkog interfejsa nakon uspješnog upisa.
            try
            {
                await viewModel.LoadArticlesAsync ();

                TblArtikli? firstImported = viewModel.Artikli.FirstOrDefault (x => x.IdArtikla == firstImportedId);

                if(firstImported != null)
                {
                    viewModel.SelectedArticle = firstImported;
                    ListaArtikala.SelectedItem = firstImported;
                    ListaArtikala.CurrentItem = firstImported;
                    ListaArtikala.ScrollIntoView (firstImported);
                }

                ShowArticleEditMessage ("POTVRDA", $"Uspješno je uvezeno {importedCount} artikala.");
                BtnTransferClose_Click (sender, e);
            }
            catch(Exception ex)
            {
                Debug.WriteLine ("[ARTICLES] Import editor apply - refresh: " + ex);

                ShowArticleEditMessage (
                    "UPOZORENJE",
                    $"Uspješno je uvezeno {importedCount} artikala, ali lista artikala nije uspješno osvježena.");
            }
        }

        // Pretvara tekstualnu vrstu iz transfer editora u interni ID vrste artikla.
        private static int? MapTransferType(string? value)
        {
            return value?.Trim ().ToLowerInvariant () switch { "piće" => 0, "pice" => 0, "hrana" => 1, "ostalo" => 2, _ => null };
        }

        // Pretvara tekstualnu vrijednost iz transfera u bool uz zadanu podrazumijevanu vrijednost.
        private static bool ParseTransferBool(string? value, bool defaultValue)
        {
            if(string.IsNullOrWhiteSpace (value))
                return defaultValue;
            return value.Trim ().ToLowerInvariant () switch { "da" => true, "true" => true, "1" => true, "ne" => false, "false" => false, "0" => false, _ => defaultValue };
        }

        #endregion


        #region TRANSFER EDITOR - UREĐIVANJE DATAGRIDA

        // Nakon završetka izmjene ćelije formatira posebna polja i ponovo validira podatke.
        private async void ArticleTransferDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            ArticleTransferErrorGrid.IsHitTestVisible = true;
            ArticleTransferErrorGrid.Opacity = 1;

            if(e.EditAction != DataGridEditAction.Commit)
                return;

            if(e.Row.Item is not ArticleTransferRow row)
                return;

            string? field = e.Column.Header?.ToString ();

            if(field != null && row.DefaultedFields.Contains (field))
                row.DefaultedFields.Remove (field);

            await Dispatcher.InvokeAsync (async () =>
            {
                if(field == "Cijena" && TryParseArticlePrice (row.Cijena, out decimal cijena))
                    row.Cijena = cijena.ToString ("0.00", CultureInfo.InvariantCulture);

                await ValidateArticleTransferAsync ();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        // Privremeno onemogućava listu grešaka dok korisnik uređuje ćeliju.
        private void ArticleTransferDataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            ArticleTransferErrorGrid.IsHitTestVisible = false;
            ArticleTransferErrorGrid.Opacity = 0.60;
        }

        // Potvrđuje izmjenu ComboBox ćelije nakon zatvaranja padajuće liste.
        private void ArticleTransferComboBox_DropDownClosed(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke (new Action (() =>
            {
                ArticleTransferDataGrid.CommitEdit (DataGridEditingUnit.Cell, true);
                ArticleTransferDataGrid.CommitEdit (DataGridEditingUnit.Row, true);
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        // Upisuje izabranu poresku stopu kao postotak u transfer red.
        private void ArticleTransferTaxComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(sender is not ComboBox comboBox || comboBox.Tag is not ArticleTransferRow row || comboBox.SelectedItem is not TblPoreskeStope tax || !tax.Postotak.HasValue)
                return;

            row.Porez = $"{tax.Postotak.Value:0.##}%";
            row.DefaultedFields.Remove ("Porez");
        }

        // Upravlja ulaskom u edit mode i klikovima izvan ćelije bez ometanja CheckBox i ComboBox kontrola.
        private void ArticleTransferDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(sender is not DataGrid dataGrid)
                return;

            DependencyObject? source = e.OriginalSource as DependencyObject;

            if(FindVisualParent<CheckBox> (source) != null)
                return;

            while(source != null && source is not DataGridCell)
                source = VisualTreeHelper.GetParent (source);

            if(source is not DataGridCell cell)
            {
                if(FindVisualParent<ComboBoxItem> (e.OriginalSource as DependencyObject) != null)
                    return;

                if(dataGrid.CurrentCell.Item != null && dataGrid.CurrentCell.Column != null)
                {
                    dataGrid.CommitEdit (DataGridEditingUnit.Cell, true);
                    dataGrid.CommitEdit (DataGridEditingUnit.Row, true);
                }

                return;
            }

            if(cell.IsEditing || cell.IsReadOnly)
                return;

            if(!cell.IsFocused)
                cell.Focus ();

            dataGrid.CurrentCell = new DataGridCellInfo (cell);
            dataGrid.BeginEdit ();

            if(FindVisualChild<TextBox> (cell) is TextBox textBox)
            {
                textBox.Focus ();
                textBox.CaretIndex = textBox.Text.Length;
            }

            e.Handled = true;
        }

        // Omogućava navigaciju tastaturom kroz ćelije transfer editora i pravilno potvrđivanje izmjena.
        private void ArticleTransferDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(sender is not DataGrid dataGrid)
                return;

            if(e.Key == Key.Escape)
            {
                dataGrid.CancelEdit (DataGridEditingUnit.Cell);
                dataGrid.CancelEdit (DataGridEditingUnit.Row);

                ArticleTransferErrorGrid.IsHitTestVisible = true;
                ArticleTransferErrorGrid.Opacity = 1;
                ArticleTransferErrorGrid.UnselectAll ();
                ArticleTransferErrorGrid.SelectedItem = null;

                e.Handled = true;
                return;
            }

            if(e.Key != Key.Up && e.Key != Key.Down && e.Key != Key.Left && e.Key != Key.Right && e.Key != Key.Enter)
                return;

            // Ako je ComboBox otvoren, njegove strelice ostavljamo njemu.
            if(FindVisualParent<ComboBox> (e.OriginalSource as DependencyObject) is ComboBox openComboBox && openComboBox.IsDropDownOpen)
                return;

            if(dataGrid.CurrentCell.Column == null || dataGrid.CurrentItem == null)
                return;

            int rowIndex = dataGrid.Items.IndexOf (dataGrid.CurrentItem);
            int columnIndex = dataGrid.Columns.IndexOf (dataGrid.CurrentCell.Column);

            if(rowIndex < 0 || columnIndex < 0)
                return;

            int targetRow = rowIndex;
            int targetColumn = columnIndex;

            switch(e.Key)
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

            if(targetRow < 0 || targetRow >= dataGrid.Items.Count)
                return;

            if(targetColumn < 0 || targetColumn >= dataGrid.Columns.Count)
                return;

            object targetItem = dataGrid.Items[targetRow];

            if(targetItem == CollectionView.NewItemPlaceholder)
                return;

            DataGridColumn targetColumnObject = dataGrid.Columns[targetColumn];

            // Završavamo trenutnu izmjenu.
            if(!dataGrid.CommitEdit (DataGridEditingUnit.Cell, true))
                return;

            dataGrid.CommitEdit (DataGridEditingUnit.Row, true);

            // Nova ćelija postaje CurrentCell.
            dataGrid.SelectedItem = targetItem;
            dataGrid.CurrentCell = new DataGridCellInfo (targetItem, targetColumnObject);
            dataGrid.ScrollIntoView (targetItem, targetColumnObject);

            e.Handled = true;

            Dispatcher.BeginInvoke (new Action (() =>
            {
                DataGridCell? cell = GetDataGridCell (dataGrid, targetItem, targetColumnObject);

                if(cell == null || cell.IsReadOnly)
                    return;

                cell.Focus ();
                dataGrid.CurrentCell = new DataGridCellInfo (targetItem, targetColumnObject);

                // Odmah ulazimo u edit mode.
                dataGrid.BeginEdit ();

                Dispatcher.BeginInvoke (new Action (() =>
                {
                    // TEXTBOX
                    if(FindVisualChild<TextBox> (cell) is TextBox textBox)
                    {
                        textBox.Focus ();
                        textBox.SelectAll ();
                        return;
                    }

                    // COMBOBOX
                    if(FindVisualChild<ComboBox> (cell) is ComboBox comboBox)
                    {
                        comboBox.Focus ();
                        return;
                    }

                    // CHECKBOX
                    if(FindVisualChild<CheckBox> (cell) is CheckBox checkBox)
                        checkBox.Focus ();

                }), System.Windows.Threading.DispatcherPriority.Input);

            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        // Briše samo privremeni transfer red i ponovo validira preostale podatke.
        private async void ArticleTransferDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if(sender is not Button button || button.DataContext is not ArticleTransferRow row)
                return;

            _articleTransferRows.Remove (row);

            if(_articleTransferRows.Count == 0)
            {
                BtnTransferClose_Click (sender, e);
                return;
            }

            await ValidateArticleTransferAsync ();
        }

        #endregion


        #region TRANSFER EDITOR - SELEKCIJA I GRUPNO UREĐIVANJE

        // Označava sve transfer redove za grupno uređivanje.
        private void ArticleTransferSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach(var row in _articleTransferRows)
                row.IsSelected = true;

            UpdateArticleTransferSelection ();
        }

        // Poništava grupnu selekciju svih transfer redova.
        private void ArticleTransferSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach(var row in _articleTransferRows)
                row.IsSelected = false;

            UpdateArticleTransferSelection ();
        }

        // Osvježava FloatingMenuBar nakon promjene pojedinačnog checkboxa.
        // Osvježava selekciju i podržava Shift označavanje raspona redova.
        private void ArticleTransferRowCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if(sender is not CheckBox checkBox || checkBox.DataContext is not ArticleTransferRow clickedRow)
                return;

            int clickedIndex = _articleTransferRows.IndexOf (clickedRow);
            if(clickedIndex < 0)
                return;

            bool shiftPressed = Keyboard.Modifiers.HasFlag (ModifierKeys.Shift);

            if(shiftPressed && _articleTransferSelectionAnchorIndex >= 0 && _articleTransferSelectionAnchorIndex < _articleTransferRows.Count)
            {
                int start = Math.Min (_articleTransferSelectionAnchorIndex, clickedIndex);
                int end = Math.Max (_articleTransferSelectionAnchorIndex, clickedIndex);

                for(int i = start; i <= end; i++)
                    _articleTransferRows[i].IsSelected = true;
            }

            _articleTransferSelectionAnchorIndex = clickedIndex;
            UpdateArticleTransferSelection ();
        }

        // Ažurira broj označenih redova te prikazuje, skriva i resetuje FloatingMenuBar.
        private void UpdateArticleTransferSelection()
        {
            int selectedCount = _articleTransferRows.Count (x => x.IsSelected);

            ArticleTransferFloatingMenuBar.SetSelectedCount (selectedCount);

            if(selectedCount > 0)
            {
                ConfigureArticleTransferFloatingMenuBarCategories ();

                if(DataContext is ArticlesViewModel viewModel)
                {
                    ArticleTransferFloatingMenuBar.SetTaxes (viewModel.PoreskeStope.OrderBy (x => x.IdStope).Select (x => ($"{x.Postotak:0.##}%", $"{x.Postotak:0.##}%")));
                    ArticleTransferFloatingMenuBar.SetNormativi (viewModel.Normativi.Select (x => x.Normativ ?? string.Empty).Where (x => !string.IsNullOrWhiteSpace (x)));
                }

                ArticleTransferFloatingBar.Visibility = Visibility.Visible;
                return;
            }

            ArticleTransferFloatingBar.Visibility = Visibility.Collapsed;
            ArticleTransferFloatingBar.HorizontalAlignment = HorizontalAlignment.Right;
            ArticleTransferFloatingBar.VerticalAlignment = VerticalAlignment.Top;
            ArticleTransferFloatingBar.Margin = new Thickness (0, 150, 45, 0);
        }

        // Povezuje FloatingMenuBar sa grupnim akcijama transfer editora.
        private void ConfigureArticleTransferFloatingMenuBar()
        {
            ArticleTransferFloatingMenuBar.PriceSelected += ArticleTransferFloatingMenuBar_PriceSelected;
            ArticleTransferFloatingMenuBar.TypeSelected += ArticleTransferFloatingMenuBar_TypeSelected;
            ArticleTransferFloatingMenuBar.CategorySelected += ArticleTransferFloatingMenuBar_CategorySelected;
            ArticleTransferFloatingMenuBar.UnitSelected += ArticleTransferFloatingMenuBar_UnitSelected;
            ArticleTransferFloatingMenuBar.TaxSelected += ArticleTransferFloatingMenuBar_TaxSelected;
            ArticleTransferFloatingMenuBar.ConsumptionTaxSelected += ArticleTransferFloatingMenuBar_ConsumptionTaxSelected;
            ArticleTransferFloatingMenuBar.NormativSelected += ArticleTransferFloatingMenuBar_NormativSelected;
            ArticleTransferFloatingMenuBar.DeleteSelectedRequested += ArticleTransferFloatingMenuBar_DeleteSelectedRequested;
            ArticleTransferFloatingMenuBar.FinishRequested += ArticleTransferFloatingMenuBar_FinishRequested;

            ArticleTransferFloatingMenuBar.DragStarted += ArticleTransferFloatingBarTitle_MouseLeftButtonDown;
            ArticleTransferFloatingMenuBar.DragMoved += ArticleTransferFloatingBarTitle_MouseMove;
            ArticleTransferFloatingMenuBar.DragFinished += ArticleTransferFloatingBarTitle_MouseLeftButtonUp;

            if(DataContext is ArticlesViewModel viewModel)
                ArticleTransferFloatingMenuBar.SetUnits (viewModel.JediniceMjere.OrderBy (x => x.IdJedinice).Select (x => x.ToString ()));
        }


        // Postavlja konačnu cijenu svim označenim artiklima.

        private async void ArticleTransferFloatingMenuBar_PriceSelected(object? sender, string priceText)
        {
            var selectedRows = _articleTransferRows.Where (x => x.IsSelected).ToList ();

            if(selectedRows.Count == 0)
                return;

            string normalizedInput = priceText.Trim ().Replace (',', '.');

            if(!decimal.TryParse (normalizedInput, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal price) || price <= 0)
            {
                ShowArticleEditMessage ("UPOZORENJE", "Unesite ispravnu cijenu veću od 0.");
                return;
            }

            string normalizedPrice = price.ToString ("0.00", CultureInfo.InvariantCulture);

            foreach(var row in selectedRows)
            {
                row.Cijena = normalizedPrice;
                row.DefaultedFields.Remove ("Cijena");
            }

            await ValidateArticleTransferAsync ();
            ArticleTransferFloatingMenuBar.CloseAllMenus ();
            UpdateArticleTransferSelection ();
        }
        // Primjenjuje izabranu vrstu na sve označene transfer redove.
        private async void ArticleTransferFloatingMenuBar_TypeSelected(object? sender, string type)
        {
            var selectedRows = _articleTransferRows.Where (x => x.IsSelected).ToList ();

            if(selectedRows.Count == 0)
                return;

            foreach(var row in selectedRows)
            {
                row.Vrsta = type;
                row.DefaultedFields.Remove ("Vrsta");
            }

            await ValidateArticleTransferAsync ();
            UpdateArticleTransferSelection ();
        }

        // Priprema kategorije za označene redove kada svi pripadaju istoj vrsti.
        private void ConfigureArticleTransferFloatingMenuBarCategories()
        {
            var selectedRows = _articleTransferRows.Where (x => x.IsSelected).ToList ();

            if(selectedRows.Count == 0)
            {
                ArticleTransferFloatingMenuBar.SetCategories (Array.Empty<string> ());
                return;
            }

            string type = selectedRows[0].Vrsta;

            if(selectedRows.Any (x => !string.Equals (x.Vrsta, type, StringComparison.OrdinalIgnoreCase)))
            {
                ArticleTransferFloatingMenuBar.SetCategories (Array.Empty<string> ());
                return;
            }

            ArticleTransferFloatingMenuBar.SetCategories (selectedRows[0].AvailableCategories);
        }

        // Primjenjuje izabranu kategoriju na sve označene transfer redove.
        private async void ArticleTransferFloatingMenuBar_CategorySelected(object? sender, string category)
        {
            var selectedRows = _articleTransferRows.Where (x => x.IsSelected).ToList ();

            if(selectedRows.Count == 0)
                return;

            string type = selectedRows[0].Vrsta;

            if(selectedRows.Any (x => !string.Equals (x.Vrsta, type, StringComparison.OrdinalIgnoreCase)))
            {
                ShowArticleEditMessage ("UPOZORENJE", "Kategoriju nije moguće grupno promijeniti jer označeni artikli nisu iste vrste. Svi označeni artikli moraju biti iste vrste.");
                return;
            }

            foreach(var row in selectedRows)
            {
                row.Kategorija = category;
                row.DefaultedFields.Remove ("Kategorija");
            }

            await ValidateArticleTransferAsync ();
            UpdateArticleTransferSelection ();
        }

        // Primjenjuje izabranu jedinicu mjere na sve označene transfer redove.
        private async void ArticleTransferFloatingMenuBar_UnitSelected(object? sender, string unit)
        {
            var selectedRows = _articleTransferRows.Where (x => x.IsSelected).ToList ();

            if(selectedRows.Count == 0)
                return;

            foreach(var row in selectedRows)
            {
                row.Jedinica = unit;
                row.DefaultedFields.Remove ("Jedinica");
            }

            await ValidateArticleTransferAsync ();
            UpdateArticleTransferSelection ();
        }

        // Primjenjuje odabranu poresku stopu na označene transfer redove.

        private async void ArticleTransferFloatingMenuBar_TaxSelected(object? sender, string tax)
        {
            var selectedRows = _articleTransferRows.Where (x => x.IsSelected).ToList ();
            if(selectedRows.Count == 0)
                return;

            foreach(var row in selectedRows)
            {
                row.Porez = tax;
                row.DefaultedFields.Remove ("Porez");
            }

            await ValidateArticleTransferAsync ();
            UpdateArticleTransferSelection ();
        }

        // Postavlja porez na potrošnju svim označenim artiklima.
        private async void ArticleTransferFloatingMenuBar_ConsumptionTaxSelected(object? sender, bool enabled)
        {
            var selectedRows = _articleTransferRows.Where (x => x.IsSelected).ToList ();

            if(selectedRows.Count == 0)
                return;

            foreach(var row in selectedRows)
                row.PorezNaPotrosnju = enabled;

            await ValidateArticleTransferAsync ();
            UpdateArticleTransferSelection ();
        }

        // Briše sve označene artikle iz transfer liste.
        private async void ArticleTransferFloatingMenuBar_DeleteSelectedRequested(object? sender, EventArgs e)
        {
            var selectedRows = _articleTransferRows.Where (x => x.IsSelected).ToList ();

            if(selectedRows.Count == 0)
                return;

            var popup = new YesNoPopup
            {
                Owner = Window.GetWindow (this),
                PopupTitle = "BRISANJE ARTIKALA",
                PopupMessage = $"Izbrisati {selectedRows.Count} označenih artikala iz liste za uvoz?",
                ConfirmText = "DA",
                CancelText = "NE"
            };

            popup.ShowDialog ();

            if(popup.Kliknuo != "Da")
                return;

            foreach(var row in selectedRows)
                _articleTransferRows.Remove (row);

            _articleTransferSelectionAnchorIndex = -1;

            if(_articleTransferRows.Count == 0)
            {
                BtnTransferClose_Click (this, new RoutedEventArgs ());
                return;
            }

            await ValidateArticleTransferAsync ();
            UpdateArticleTransferSelection ();
        }

        // Primjenjuje normativ samo kada su svi označeni artikli piće sa jedinicom različitom od kom.
        private async void ArticleTransferFloatingMenuBar_NormativSelected(object? sender, string normativ)
        {
            var selectedRows = _articleTransferRows.Where (x => x.IsSelected).ToList ();

            if(selectedRows.Count == 0)
                return;

            if(selectedRows.Any (x => !string.Equals (x.Vrsta, "Piće", StringComparison.OrdinalIgnoreCase) || string.Equals (x.Jedinica, "kom", StringComparison.OrdinalIgnoreCase)))
            {
                ShowArticleEditMessage ("UPOZORENJE", "Normativ nije moguće grupno promijeniti. Normativ se može mijenjati samo za piće čija jedinica mjere nije kom.");
                return;
            }

            if(DataContext is not ArticlesViewModel viewModel)
                return;

            string selectedNormativ = normativ;

            if(string.Equals (normativ, "Dodaj normativ", StringComparison.OrdinalIgnoreCase))
            {
                MyInputBox dialog = new MyInputBox ();
                dialog.InputTitle.Text = "NOVI NORMATIV PIĆA";
                dialog.InputText.Focus ();
                dialog.ShowDialog ();

                string result = dialog.result?.Trim () ?? string.Empty;

                if(string.IsNullOrWhiteSpace (result))
                    return;

                decimal? newValue = ParseArticleNormativ (result);

                if(!newValue.HasValue || newValue.Value <= 0)
                {
                    ShowArticleEditMessage ("GREŠKA", "Unesite ispravan normativ veći od 0.");
                    return;
                }

                TblNormativPica? existing = viewModel.Normativi.FirstOrDefault (x => ParseArticleNormativ (x.Normativ) == newValue.Value);

                if(existing != null)
                {
                    selectedNormativ = existing.Normativ ?? "1";
                }
                else
                {
                    string normalizedText = result.Replace (',', '.');

                    await using(var db = new AppDbContext ())
                    {
                        db.NormativPica.Add (new TblNormativPica { Normativ = normalizedText });
                        await db.SaveChangesAsync ();
                    }

                    await viewModel.LoadNormativi ();
                    selectedNormativ = viewModel.Normativi.FirstOrDefault (x => ParseArticleNormativ (x.Normativ) == newValue.Value)?.Normativ ?? normalizedText;

                    ArticleTransferFloatingMenuBar.SetNormativi (viewModel.Normativi.Select (x => x.Normativ ?? string.Empty).Where (x => !string.IsNullOrWhiteSpace (x)));
                }
            }

            foreach(var row in selectedRows)
            {
                row.Normativ = selectedNormativ;
                row.DefaultedFields.Remove ("Normativ");
            }

            await ValidateArticleTransferAsync ();
            UpdateArticleTransferSelection ();
        }
        // Završava grupno uređivanje i uklanja oznake sa svih transfer redova.
        private void ArticleTransferFloatingMenuBar_FinishRequested(object? sender, EventArgs e)
        {
            foreach(var row in _articleTransferRows)
                row.IsSelected = false;

            UpdateArticleTransferSelection ();
        }

        #endregion


        #region TRANSFER EDITOR - FLOATING BAR

        // Započinje povlačenje FloatingBara i pamti njegovu stvarnu početnu poziciju.
        private void ArticleTransferFloatingBarTitle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingArticleTransferFloatingBar = true;
            _articleTransferFloatingBarDragStart = e.GetPosition (this);
            _articleTransferFloatingBarStartPosition = ArticleTransferFloatingBar.TranslatePoint (new Point (0, 0), this);

            e.Handled = true;
        }

        // Pomjera FloatingBar unutar granica stranice dok korisnik vuče naslovnu traku.
        private void ArticleTransferFloatingBarTitle_MouseMove(object sender, MouseEventArgs e)
        {
            if(!_isDraggingArticleTransferFloatingBar || e.LeftButton != MouseButtonState.Pressed)
                return;

            Point currentPosition = e.GetPosition (this);

            double left = _articleTransferFloatingBarStartPosition.X + currentPosition.X - _articleTransferFloatingBarDragStart.X;
            double top = _articleTransferFloatingBarStartPosition.Y + currentPosition.Y - _articleTransferFloatingBarDragStart.Y;

            double maxLeft = Math.Max (0, ActualWidth - ArticleTransferFloatingBar.ActualWidth);
            double maxTop = Math.Max (0, ActualHeight - ArticleTransferFloatingBar.ActualHeight);

            left = Math.Max (0, Math.Min (left, maxLeft));
            top = Math.Max (0, Math.Min (top, maxTop));

            ArticleTransferFloatingBar.HorizontalAlignment = HorizontalAlignment.Left;
            ArticleTransferFloatingBar.VerticalAlignment = VerticalAlignment.Top;
            ArticleTransferFloatingBar.Margin = new Thickness (left, top, 0, 0);
        }

        // Završava povlačenje FloatingBara.
        private void ArticleTransferFloatingBarTitle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(!_isDraggingArticleTransferFloatingBar)
                return;

            _isDraggingArticleTransferFloatingBar = false;
            e.Handled = true;
        }

        #endregion


        #region TRANSFER EDITOR - LISTA GREŠAKA

        // Premješta fokus na ćeliju povezanu sa izabranom greškom ili upozorenjem.
        private async void ArticleTransferErrorGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(ArticleTransferErrorGrid.SelectedItem is not ArticleTransferError error || error.RowNumber < 2)
                return;

            int index = error.RowNumber - 2;
            if(index < 0 || index >= _articleTransferRows.Count)
                return;

            var item = _articleTransferRows[index];

            int columnIndex = error.ColumnName switch
            {
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

            if(error.ColumnName == "Normativ" || error.ColumnName == "Naziv / Normativ")
                item.EnableNormativCorrection ();

            ArticleTransferDataGrid.SelectedItem = item;
            ArticleTransferDataGrid.ScrollIntoView (item);

            if(columnIndex < 0 || columnIndex >= ArticleTransferDataGrid.Columns.Count)
                return;

            var column = ArticleTransferDataGrid.Columns[columnIndex];

            ArticleTransferDataGrid.CurrentCell = new DataGridCellInfo (item, column);
            ArticleTransferDataGrid.ScrollIntoView (item, column);
            ArticleTransferDataGrid.Focus ();
            ArticleTransferDataGrid.BeginEdit ();

            await Dispatcher.InvokeAsync (() =>
            {
                DataGridCell? cell = GetDataGridCell (ArticleTransferDataGrid, item, columnIndex);
                if(cell == null)
                    return;

                TextBox? textBox = FindVisualChild<TextBox> (cell);
                if(textBox != null)
                {
                    textBox.Focus ();
                    Keyboard.Focus (textBox);
                    textBox.SelectAll ();
                    return;
                }

                ComboBox? comboBox = FindVisualChild<ComboBox> (cell);
                if(comboBox != null)
                {
                    comboBox.Focus ();
                    Keyboard.Focus (comboBox);
                }
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        #endregion


        #region VISUAL TREE I DATAGRID POMOĆNE METODE

        // Rekurzivno pronalazi sve vizuelne potomke zadatog WPF tipa.
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if(depObj == null)
                yield break;

            for(int i = 0; i < VisualTreeHelper.GetChildrenCount (depObj); i++)
            {
                DependencyObject? child = VisualTreeHelper.GetChild (depObj, i);
                if(child == null)
                    continue;

                if(child is T matchedChild)
                    yield return matchedChild;

                foreach(T childOfChild in FindVisualChildren<T> (child))
                    yield return childOfChild;
            }
        }

        // Pronalazi konkretnu DataGrid ćeliju za zadati red i kolonu preko indeksa.
        private static DataGridCell? GetDataGridCell(DataGrid dataGrid, object item, int columnIndex)
        {
            if(dataGrid.ItemContainerGenerator.ContainerFromItem (item) is not DataGridRow row)
                return null;

            DataGridCellsPresenter? presenter = FindVisualChild<DataGridCellsPresenter> (row);
            if(presenter == null)
                return null;

            return presenter.ItemContainerGenerator.ContainerFromIndex (columnIndex) as DataGridCell;
        }

        // Pronalazi konkretnu DataGrid ćeliju za zadati red i kolonu.
        private static DataGridCell? GetDataGridCell(DataGrid dataGrid, object item, DataGridColumn column)
        {
            if(dataGrid.ItemContainerGenerator.ContainerFromItem (item) is not DataGridRow row)
                return null;

            DataGridCellsPresenter? presenter = FindVisualChild<DataGridCellsPresenter> (row);

            if(presenter == null)
                return null;

            int columnIndex = dataGrid.Columns.IndexOf (column);

            return presenter.ItemContainerGenerator.ContainerFromIndex (columnIndex) as DataGridCell;
        }

        #endregion

    }
}
