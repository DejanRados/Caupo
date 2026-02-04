using Caupo.Data;
using Caupo.Helpers;
using Caupo.Properties;
using Caupo.ViewModels;
using ClosedXML.Excel;
using Microsoft.Win32;

using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
            this.DataContext = new ArticlesViewModel ();

            InitializeComponent ();
            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik.Radnik;
        }

        private VirtualKeyboard virtualKeyboard;
        private TextBox? FocusedTextBox = null;

        public void ReceiveKey(string key)
        {
            // 📌 Ako TextBox ima fokus → unos teksta
            if(FocusedTextBox != null)
            {
                switch(key)
                {
                    case "\uE72B":

                        if(FocusedTextBox.Text.Length > 0)
                        {
                            int pos = FocusedTextBox.SelectionStart;
                            if(pos > 0)
                            {
                                FocusedTextBox.Text =
                                    FocusedTextBox.Text.Remove (pos - 1, 1);
                                FocusedTextBox.SelectionStart = pos - 1;
                            }
                        }



                        break;

                    case "\uE75D":
                        InsertIntoFocused ("\u0020");
                        break;
                    case "Sakrij":
                        FocusedTextBox = null;
                        ListaArtikala.Focus ();
                        MainWindow.Instance.HideKeyboard ();
                        break;
                    case "Enter":
                        FocusedTextBox = null;
                        ListaArtikala.Focus ();
                        MainWindow.Instance.HideKeyboard ();
                        break;
                    case "Reset":
                        FocusedTextBox.Text = "";

                        break;

                    default:
                        InsertIntoFocused (key);
                        break;
                }

                return;  // ⛔ NE IDE filter ako kucamo u textbox!
            }


        }
        private void InsertIntoFocused(string text)
        {
            if(FocusedTextBox == null)
                return;

            int pos = FocusedTextBox.SelectionStart;
            FocusedTextBox.Text = FocusedTextBox.Text.Insert (pos, text);
            FocusedTextBox.SelectionStart = pos + text.Length;
        }
        private void KeyboardButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.ShowKeyboard ();

        }
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                FocusedTextBox = sender as TextBox;
                FocusedTextBox.Clear ();
                FocusedTextBox.SelectAll ();
                MainWindow.Instance.ShowKeyboard ();
            }
            catch(Exception ex)
            {
                Debug.WriteLine ("TextBox_GotFocus salje: " + ex);
            }


        }
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {

            FocusedTextBox = null;

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var page = new HomePage ();
            page.DataContext = new HomeViewModel ();
            PageNavigator.NavigateWithFade (page);
        }

        private void btnSlika_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog ();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp";
            if(openFileDialog.ShowDialog () == true)
            {
                string filePath = openFileDialog.FileName;
                SlikaTextBox.Text = filePath;

            }
        }

        private async void NormativPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(string.IsNullOrEmpty (ArtiklTextBox.Text))
            {
                return;
            }
            var selectedNormativ = NormativPicker.SelectedItem as DatabaseTables.TblNormativPica;
            if(selectedNormativ != null)
            {
                string? normativ = selectedNormativ.Normativ;
                if(normativ == "1" || (decimal.TryParse (normativ, out decimal normValue) && normValue < 0.01m))
                {
                    if(ArtiklNormativTextBox != null)
                    {
                        ArtiklNormativTextBox.Text = ArtiklTextBox.Text;
                    }
                }
                else if(normativ != "Dodaj normativ")
                {
                    ArtiklNormativTextBox.Text = ArtiklTextBox.Text + " " + normativ;
                }
                else
                {
                    MyInputBox dialog = new MyInputBox ();
                    dialog.InputTitle.Text = "NOVI NORMATIV PIĆA";
                    dialog.InputText.Focus ();
                    dialog.ShowDialog ();
                    string result = dialog.result;

                    if(!string.IsNullOrEmpty (result))
                    {
                        DatabaseTables.TblNormativPica normativPica = new DatabaseTables.TblNormativPica ();
                        normativPica.Normativ = result;
                        using(var db = new AppDbContext ())
                        {
                            db.NormativPica.Add (normativPica);
                            db.SaveChanges ();
                            Debug.WriteLine ("New record inserted successfully!");
                            if(DataContext is ArticlesViewModel viewModel)
                            {
                                await viewModel.LoadNormativi ();

                            }
                        }
                    }
                    else
                    {
                        return;
                    }
                }

            }
        }

        private async void CancelEditButton_Click(object sender, RoutedEventArgs e)
        {
            InternaSifraGrid.Visibility = Visibility.Collapsed;
            SlikaGrid.Visibility = Visibility.Collapsed;
            JedinicaMjereGrid.Visibility = Visibility.Collapsed;
            PoreskeStopeGrid.Visibility = Visibility.Collapsed;
            NormativGrid.Visibility = Visibility.Collapsed;
            VrstaArtiklaGrid.Visibility = Visibility.Collapsed;
            KategorijaGrid.Visibility = Visibility.Collapsed;
            PozicijaGrid.Visibility = Visibility.Collapsed;
            PrikazGrid.Visibility = Visibility.Collapsed;
            EditGrid.Visibility = Visibility.Collapsed;
            AddGrid.Visibility = Visibility.Collapsed;
            lblArtikl.Visibility = Visibility.Collapsed;
            lblCijena.Visibility = Visibility.Collapsed;
            if(DataContext is ArticlesViewModel viewModel)
            {


                var artikl = viewModel.SelectedArticle;
                await viewModel.LoadArticlesAsync ();
                viewModel.SelectedArticle = artikl;
                _BorderBrush = Application.Current.Resources["GlobalFontColor"] as Brush;
                ChangeTextBoxBorderBrush (this, _BorderBrush);
                foreach(ComboBox comboBox in FindVisualChildren<ComboBox> (this))
                {
                    comboBox.BorderThickness = new Thickness (0, 0, 0, 1);
                }
                if(artikl != null)
                {
                    var foundItem = ListaArtikala.Items.Cast<dynamic> ().FirstOrDefault (item =>
                        item.IdArtikla == artikl.IdArtikla);
                    if(foundItem != null)
                    {
                        Debug.WriteLine ("Pronađen po ID-u - pokušavam selekciju i scroll");
                        ListaArtikala.SelectedItem = foundItem;
                        ListaArtikala.ScrollIntoView (foundItem);
                    }
                }
            }
        }

        private async void SaveEditButton_Click(object sender, RoutedEventArgs e)
        {
            bool isValid = ValidateForm ();

            if(!isValid)
            {
                isValid = true;
                return;
            }

            if(DataContext is ArticlesViewModel viewModel)
            {
                viewModel.ErrorOccurred -= ViewModel_ErrorOccurred;
                viewModel.ErrorOccurred += ViewModel_ErrorOccurred;

                var artikl = viewModel.SelectedArticle;
                if(artikl != null)
                {
                    artikl.IdArtikla = Convert.ToInt32 (IdArtiklaTextBox.Text);
                    artikl.Artikl = ArtiklTextBox.Text;
                    artikl.Cijena = Convert.ToDecimal (CijenaTextBox.Text);
                    artikl.Sifra = SifraTextBox.Text;
                    artikl.InternaSifra = InternaSifraTextBox.Text;
                    TblPoreskeStope ps = (TblPoreskeStope)PoreskeStopePicker.SelectedItem;
                    artikl.PoreskaStopa = ps.IdStope;
                    TblJediniceMjere jediniceMjere = (TblJediniceMjere)JediniceMjerePicker.SelectedItem;
                    artikl.JedinicaMjere = jediniceMjere.IdJedinice;
                    artikl.Slika = SlikaTextBox.Text;
                    artikl.VrstaArtikla = VrstaArtiklaPicker.SelectedIndex;
                    artikl.Pozicija = Convert.ToInt32 (PozicijaTextBox.Text);
                    artikl.PrikazatiNaDispleju = PrikazatiNaDisplejuPicker.Text;
                    TblKategorije kategorija = (TblKategorije)KategorijePicker.SelectedItem;
                    artikl.Kategorija = kategorija.IdKategorije;
                    TblNormativPica normativ = (TblNormativPica)NormativPicker.SelectedItem;
                    artikl.Normativ = Convert.ToDecimal (normativ.Normativ);
                    artikl.ArtiklNormativ = ArtiklNormativTextBox.Text;
                    await viewModel.UpdateArticle (artikl);
                }

                if(_errorOccurred)
                {
                    _errorOccurred = false;
                    return;
                }
                InternaSifraGrid.Visibility = Visibility.Collapsed;
                SlikaGrid.Visibility = Visibility.Collapsed;
                JedinicaMjereGrid.Visibility = Visibility.Collapsed;
                PoreskeStopeGrid.Visibility = Visibility.Collapsed;
                NormativGrid.Visibility = Visibility.Collapsed;
                VrstaArtiklaGrid.Visibility = Visibility.Collapsed;
                KategorijaGrid.Visibility = Visibility.Collapsed;
                PozicijaGrid.Visibility = Visibility.Collapsed;
                PrikazGrid.Visibility = Visibility.Collapsed;
                EditGrid.Visibility = Visibility.Collapsed;
                AddGrid.Visibility = Visibility.Collapsed;
                foreach(ComboBox comboBox in FindVisualChildren<ComboBox> (this))
                {
                    comboBox.BorderThickness = new Thickness (0, 0, 0, 1);
                }
                ChangeTextBoxBorderBrush (this, _BorderBrush);
                if(artikl != null)
                {
                    var foundItem = ListaArtikala.Items.Cast<dynamic> ().FirstOrDefault (item =>
                        item.IdArtikla == artikl.IdArtikla);
                    if(foundItem != null)
                    {
                        Debug.WriteLine ("Pronađen po ID-u - pokušavam selekciju i scroll");
                        ListaArtikala.SelectedItem = foundItem;
                        ListaArtikala.ScrollIntoView (foundItem);
                    }
                }

            }
        }


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
        private bool _errorOccurred = false;
        private void ViewModel_ErrorOccurred(object? sender, string? errorMessage)
        {
            // Ako je došlo do greške, prikažite poruku
            MyMessageBox myMessageBox = new MyMessageBox
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            myMessageBox.MessageTitle.Text = "GREŠKA";
            myMessageBox.MessageText.Text = errorMessage;
            myMessageBox.ShowDialog ();
            _errorOccurred = true;

        }

        private void CancelAddButton_Click(object sender, RoutedEventArgs e)
        {
            InternaSifraGrid.Visibility = Visibility.Collapsed;
            SlikaGrid.Visibility = Visibility.Collapsed;
            JedinicaMjereGrid.Visibility = Visibility.Collapsed;
            PoreskeStopeGrid.Visibility = Visibility.Collapsed;
            NormativGrid.Visibility = Visibility.Collapsed;
            VrstaArtiklaGrid.Visibility = Visibility.Collapsed;
            KategorijaGrid.Visibility = Visibility.Collapsed;
            PozicijaGrid.Visibility = Visibility.Collapsed;
            PrikazGrid.Visibility = Visibility.Collapsed;
            EditGrid.Visibility = Visibility.Collapsed;
            AddGrid.Visibility = Visibility.Collapsed;
            lblArtikl.Visibility = Visibility.Collapsed;
            lblCijena.Visibility = Visibility.Collapsed;

            if(DataContext is ArticlesViewModel viewModel)
            {
                viewModel.SelectedArticle = viewModel.Artikli?.FirstOrDefault ();

                _BorderBrush = Application.Current.Resources["GlobalFontColor"] as Brush;

                ChangeTextBoxBorderBrush (this, _BorderBrush);
                foreach(ComboBox comboBox in FindVisualChildren<ComboBox> (this))
                {
                    comboBox.BorderThickness = new Thickness (0, 0, 0, 1);
                }
                ListaArtikala.ScrollIntoView (viewModel.SelectedArticle);
            }
        }


        private void ChangeTextBoxBorderBrush(DependencyObject parent, Brush? brush)
        {
            for(int i = 0; i < VisualTreeHelper.GetChildrenCount (parent); i++)
            {
                var child = VisualTreeHelper.GetChild (parent, i);
                if(child is TextBox textBox)
                {
                    if(textBox.Name != "SearchTextBox")
                    {
                        textBox.BorderBrush = brush;
                        textBox.BorderThickness = new Thickness (0, 0, 0, 1);
                    }
                }
                if(child is ComboBox cmb)
                {

                    cmb.BorderBrush = brush;
                    cmb.BorderThickness = new Thickness (0, 0, 0, 1);

                }
                ChangeTextBoxBorderBrush (child, brush);
            }
        }

        private async void SaveAddButton_Click(object sender, RoutedEventArgs e)
        {
            bool isValid = ValidateForm ();

            if(!isValid)
            {
                isValid = true;
                return;
            }

            if(DataContext is ArticlesViewModel viewModel)
            {
                viewModel.ErrorOccurred -= ViewModel_ErrorOccurred;
                viewModel.ErrorOccurred += ViewModel_ErrorOccurred;

                TblArtikli artikl = new TblArtikli ();
                artikl.Artikl = ArtiklTextBox.Text;
                artikl.Cijena = Convert.ToDecimal (CijenaTextBox.Text);
                artikl.Sifra = SifraTextBox.Text;
                artikl.InternaSifra = InternaSifraTextBox.Text;
                TblPoreskeStope ps = (TblPoreskeStope)PoreskeStopePicker.SelectedItem;
                artikl.PoreskaStopa = ps.IdStope;
                TblJediniceMjere jediniceMjere = (TblJediniceMjere)JediniceMjerePicker.SelectedItem;
                artikl.JedinicaMjere = jediniceMjere.IdJedinice;
                artikl.Slika = SlikaTextBox.Text;
                artikl.VrstaArtikla = VrstaArtiklaPicker.SelectedIndex;
                artikl.Pozicija = Convert.ToInt32 (PozicijaTextBox.Text);
                artikl.PrikazatiNaDispleju = PrikazatiNaDisplejuPicker.Text;
                TblKategorije kategorija = (TblKategorije)KategorijePicker.SelectedItem;
                artikl.Kategorija = kategorija.IdKategorije;
                TblNormativPica normativ = (TblNormativPica)NormativPicker.SelectedItem;
                artikl.Normativ = Convert.ToDecimal (normativ.Normativ);
                artikl.ArtiklNormativ = ArtiklNormativTextBox.Text;
                await viewModel.InsertArticle (artikl);
                if(_errorOccurred)
                {
                    _errorOccurred = false;
                    return;
                }

                InternaSifraGrid.Visibility = Visibility.Collapsed;
                SlikaGrid.Visibility = Visibility.Collapsed;
                JedinicaMjereGrid.Visibility = Visibility.Collapsed;
                PoreskeStopeGrid.Visibility = Visibility.Collapsed;
                NormativGrid.Visibility = Visibility.Collapsed;
                VrstaArtiklaGrid.Visibility = Visibility.Collapsed;
                KategorijaGrid.Visibility = Visibility.Collapsed;
                PozicijaGrid.Visibility = Visibility.Collapsed;
                PrikazGrid.Visibility = Visibility.Collapsed;
                EditGrid.Visibility = Visibility.Collapsed;
                AddGrid.Visibility = Visibility.Collapsed;
                ChangeTextBoxBorderBrush (this, _BorderBrush);
                foreach(ComboBox comboBox in FindVisualChildren<ComboBox> (this))
                {
                    comboBox.BorderThickness = new Thickness (0, 0, 0, 1);
                }

                ListaArtikala.ScrollIntoView (ListaArtikala.SelectedItem);
            }
        }

        private void ArtiklTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            string newText = textBox.Text;
            if(ArtiklNormativTextBox != null)
            {
                ArtiklNormativTextBox.Text = newText;
                NormativPicker.IsEnabled = true;
                NormativPicker.SelectedIndex = 1;
            }
        }

        private void ListaArtikala_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            var selectedItem = dataGrid.SelectedItem as DatabaseTables.TblArtikli;

            if(selectedItem != null)
            {
                if(DataContext is ArticlesViewModel viewModel)
                {
                    viewModel.SelectedArticle = selectedItem;
                    //SearchTextBox.Text = "";
                    ListaArtikala.ScrollIntoView (ListaArtikala.SelectedItem);
                }
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            InternaSifraGrid.Visibility = Visibility.Visible;
            SlikaGrid.Visibility = Visibility.Visible;
            JedinicaMjereGrid.Visibility = Visibility.Visible;
            PoreskeStopeGrid.Visibility = Visibility.Visible;
            NormativGrid.Visibility = Visibility.Visible;
            VrstaArtiklaGrid.Visibility = Visibility.Visible;
            KategorijaGrid.Visibility = Visibility.Visible;
            PozicijaGrid.Visibility = Visibility.Visible;
            PrikazGrid.Visibility = Visibility.Visible;
            EditGrid.Visibility = Visibility.Visible;
            AddGrid.Visibility = Visibility.Collapsed;
        }

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            InternaSifraGrid.Visibility = Visibility.Visible;
            SlikaGrid.Visibility = Visibility.Visible;
            JedinicaMjereGrid.Visibility = Visibility.Visible;
            PoreskeStopeGrid.Visibility = Visibility.Visible;
            NormativGrid.Visibility = Visibility.Visible;
            VrstaArtiklaGrid.Visibility = Visibility.Visible;
            KategorijaGrid.Visibility = Visibility.Visible;
            PozicijaGrid.Visibility = Visibility.Visible;
            PrikazGrid.Visibility = Visibility.Visible;
            AddGrid.Visibility = Visibility.Visible;
            ArtiklNormativGrid.Visibility = Visibility.Visible;
            EditGrid.Visibility = Visibility.Collapsed;

            if(DataContext is ArticlesViewModel viewModel)
            {
                viewModel.SelectedArticle = null;



                await viewModel.GetNewSifra ();
                IdArtiklaTextBox.Text = string.Empty;
                SifraTextBox.Text = viewModel.NovaSifra;
                ArtiklTextBox.Text = string.Empty;
                CijenaTextBox.Text = string.Empty;
                InternaSifraTextBox.Text = viewModel.NovaSifra;
                SlikaTextBox.Text = string.Empty;
                JediniceMjerePicker.SelectedIndex = 0;
                PoreskeStopePicker.SelectedIndex = 1;
                NormativPicker.SelectedIndex = 1;
                NormativPicker.IsEnabled = false;
                VrstaArtiklaPicker.SelectedIndex = 0;
                KategorijePicker.SelectedIndex = 0;
                PozicijaTextBox.Text = viewModel.NovaSifra;
                ArtiklNormativTextBox.Text = string.Empty;
                PrikazatiNaDisplejuPicker.SelectedIndex = 0;
                _BorderBrush = Application.Current.Resources["GlobalFontColor"] as Brush;
                ArtiklTextBox.Focus ();

            }


        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is ArticlesViewModel viewModel)
            {
                int id = Convert.ToInt32 (IdArtiklaTextBox.Text);

                YesNoPopup myMessageBox = new YesNoPopup ();
                myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                myMessageBox.MessageTitle.Text = "POTVRDA BRISANJA";
                myMessageBox.MessageText.Text = "Da li ste sigurni da želite obrisati artikl :" + Environment.NewLine + ArtiklTextBox.Text + " ?";
                myMessageBox.ShowDialog ();
                if(myMessageBox.Kliknuo == "Da")
                {
                    await viewModel.DeleteArticle (id);
                }
            }
        }

        Brush? _BorderBrush;
        private bool ValidateForm()
        {
            bool isValid = true;
            if(DataContext is ArticlesViewModel viewModel)
            {
                _BorderBrush = Application.Current.Resources["GlobalFontColor"] as Brush;
            }

            if(string.IsNullOrWhiteSpace (ArtiklTextBox.Text))
            {
                ArtiklTextBox.BorderBrush = Brushes.Red;
                lblArtikl.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                ArtiklTextBox.BorderBrush = _BorderBrush;
                lblArtikl.Visibility = Visibility.Collapsed;
            }

            if(string.IsNullOrWhiteSpace (SifraTextBox.Text))
            {
                SifraTextBox.BorderBrush = Brushes.Red;
                isValid = false;
            }
            else
            {
                SifraTextBox.BorderBrush = _BorderBrush;
            }

            if(string.IsNullOrWhiteSpace (CijenaTextBox.Text))
            {
                CijenaTextBox.BorderBrush = Brushes.Red;
                lblCijena.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                CijenaTextBox.BorderBrush = _BorderBrush;
                lblCijena.Visibility = Visibility.Collapsed;
            }

            /*  if (string.IsNullOrWhiteSpace(ArtiklNormativTextBox.Text))
              {
                  ArtiklNormativTextBox.BorderBrush = Brushes.Red;
                  isValid = false;
              }
              else
              {
                  ArtiklNormativTextBox.BorderBrush = _BorderBrush;
              }*/

            if(string.IsNullOrWhiteSpace (InternaSifraTextBox.Text))
            {
                InternaSifraTextBox.BorderBrush = Brushes.Red;
                isValid = false;
            }
            else
            {
                InternaSifraTextBox.BorderBrush = _BorderBrush;
            }

            if(string.IsNullOrWhiteSpace (PozicijaTextBox.Text))
            {
                PozicijaTextBox.BorderBrush = Brushes.Red;
                isValid = false;
            }
            else
            {
                PozicijaTextBox.BorderBrush = _BorderBrush;
            }


            if(PoreskeStopePicker.SelectedIndex == -1)
            {
                PoreskeStopePicker.BorderBrush = Brushes.Red;
                PoreskeStopePicker.BorderThickness = new Thickness (0, 0, 0, 1);
                isValid = false;
            }
            else
            {
                PoreskeStopePicker.BorderBrush = _BorderBrush;
                PoreskeStopePicker.BorderThickness = new Thickness (0, 0, 0, 1);
            }

            if(JediniceMjerePicker.SelectedIndex == -1)
            {
                JediniceMjerePicker.BorderBrush = Brushes.Red;
                JediniceMjerePicker.BorderThickness = new Thickness (0, 0, 0, 1);
                isValid = false;
            }
            else
            {
                JediniceMjerePicker.BorderBrush = _BorderBrush;
                JediniceMjerePicker.BorderThickness = new Thickness (0, 0, 0, 1);
            }

            if(VrstaArtiklaPicker.SelectedIndex == -1)
            {
                VrstaArtiklaPicker.BorderBrush = Brushes.Red;
                VrstaArtiklaPicker.BorderThickness = new Thickness (0, 0, 0, 1);
                isValid = false;
            }
            else
            {
                VrstaArtiklaPicker.BorderBrush = _BorderBrush;
                VrstaArtiklaPicker.BorderThickness = new Thickness (0, 0, 0, 1);
            }


            if(KategorijePicker.SelectedIndex == -1)
            {
                KategorijePicker.BorderBrush = Brushes.Red;
                KategorijePicker.BorderThickness = new Thickness (0, 0, 0, 1);
                isValid = false;
            }
            else
            {
                KategorijePicker.BorderBrush = _BorderBrush;
                KategorijePicker.BorderThickness = new Thickness (0, 0, 0, 1);
            }

            if(PrikazatiNaDisplejuPicker.SelectedIndex == -1)
            {
                PrikazatiNaDisplejuPicker.BorderBrush = Brushes.Red;
                PrikazatiNaDisplejuPicker.BorderThickness = new Thickness (0, 0, 0, 1);
                isValid = false;
            }
            else
            {
                PrikazatiNaDisplejuPicker.BorderBrush = _BorderBrush;
                PrikazatiNaDisplejuPicker.BorderThickness = new Thickness (0, 0, 0, 1);
            }

            if(NormativPicker.SelectedIndex == -1)
            {
                NormativPicker.BorderBrush = Brushes.Red;
                NormativPicker.BorderThickness = new Thickness (0, 0, 0, 1);
                isValid = false;
            }
            else
            {
                NormativPicker.BorderBrush = _BorderBrush;
                NormativPicker.BorderThickness = new Thickness (0, 0, 0, 1);
            }

            return isValid;
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {

            SaveFileDialog saveFileDialog = new SaveFileDialog ();
            saveFileDialog.InitialDirectory = "C:\\";
            saveFileDialog.Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*";
            saveFileDialog.DefaultExt = ".xlsx";

            bool? result = saveFileDialog.ShowDialog ();

            if(result == true)
            {
                string filePath = saveFileDialog.FileName;
                SaveExcelFile (filePath);
            }
        }

        private async void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog ();
            openFileDialog.Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*";
            if(openFileDialog.ShowDialog () == true)
            {
                await ImportExcelToSQLiteAsync (openFileDialog.FileName);
            }
            else
            {
                return;
            }


        }

        private void SaveExcelFile(string filePath)
        {
            Debug.WriteLine ("=== START EXPORT EXCEL ===");

            using var workbook = new XLWorkbook ();
            var worksheet = workbook.Worksheets.Add ("Artikli");
            var listSheet = workbook.Worksheets.Add ("Lists");

            // =========================
            // HEADER (REDOM, BEZ RUPA)
            // =========================
            worksheet.Cell (1, 1).Value = "Artikl";
            worksheet.Cell (1, 2).Value = "VrstaArtikla";
            worksheet.Cell (1, 3).Value = "Cijena";
            worksheet.Cell (1, 4).Value = "Normativ";
            worksheet.Cell (1, 5).Value = "JedinicaMjere";
            Debug.WriteLine ("Headers written");

            // =========================
            // DROPDOWN LISTE
            // =========================
            // VrstaArtikla
            listSheet.Cell ("C1").Value = "Piće";
            listSheet.Cell ("C2").Value = "Hrana";
            listSheet.Cell ("C3").Value = "Ostalo";

            // JedinicaMjere
            listSheet.Cell ("A1").Value = "kom";
            listSheet.Cell ("A2").Value = "kg";
            listSheet.Cell ("A3").Value = "m";
            listSheet.Cell ("A4").Value = "m2";
            listSheet.Cell ("A5").Value = "m3";
            listSheet.Cell ("A6").Value = "lit";
            listSheet.Cell ("A7").Value = "tona";
            listSheet.Cell ("A8").Value = "g";
            listSheet.Cell ("A9").Value = "por";
            listSheet.Cell ("A10").Value = "pak";

            // =========================
            // DATA CONTEXT
            // =========================
            if(DataContext is not ArticlesViewModel vm || vm.Artikli == null)
            {
                Debug.WriteLine ("ERROR: DataContext or Artikli is NULL");
                return;
            }

            // Normativ list
            int normativRow = 1;
            if(vm.Normativi != null)
            {
                foreach(var n in vm.Normativi)
                {
                    // Pretvori decimal u string da dropdown radi
                    listSheet.Cell (normativRow, 2).Value = n.Normativ.ToString ();
                    Debug.WriteLine ($"Normativ added to list: {n.Normativ}");
                    normativRow++;
                }
            }

            // =========================
            // PODACI
            // =========================
            int row = 2;
            foreach(var a in vm.Artikli)
            {
                Debug.WriteLine ($"Exporting: {a.Artikl}");

                worksheet.Cell (row, 1).Value = a.Artikl;
                worksheet.Cell (row, 2).Value = a.VrstaArtiklaName;
                worksheet.Cell (row, 3).Value = a.Cijena;
                worksheet.Cell (row, 4).Value = a.Normativ.ToString (); // convert decimal -> string
                worksheet.Cell (row, 5).Value = a.JedinicaMjereName;

                //VrstaArtikla dropdown
                worksheet.Range (row, 2, row, 2)
                         .SetDataValidation ()
                         .List (listSheet.Range ("C1:C3"));

                //JedinicaMjere dropdown
                worksheet.Range (row, 5, row, 5)
                         .SetDataValidation ()
                         .List (listSheet.Range ("A1:A10"));

                // Normativ dropdown
                worksheet.Range (row, 4, row, 4)
                         .SetDataValidation ()
                         .List (listSheet.Range ($"B1:B{normativRow - 1}"));

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

            worksheet.Columns ().AdjustToContents ();

            workbook.SaveAs (filePath);
            Debug.WriteLine ("=== EXPORT FINISHED ===");

            new MyMessageBox
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                MessageTitle = { Text = "IZVOZ U EXCEL" },
                MessageText = { Text = "Izvoz tabele Artikli je uspješno završen." }
            }.ShowDialog ();
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
                        PrikazatiNaDispleju = "DA"
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
            if(string.IsNullOrWhiteSpace (value))
                return null;

            return value.Trim ().ToLower () switch
            {
                "Piće" => 0,
                "Hrana" => 1,
                "Ostalo" => 2,
                _ => null
            };
        }

        private static int? MapJedinicaMjere(string? value)
        {
            if(string.IsNullOrWhiteSpace (value))
                return null;

            return value.Trim ().ToLower () switch
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
        private void BtnFirst_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is ArticlesViewModel viewModel)
            {
                viewModel.SelectedArticle = viewModel.Artikli?.FirstOrDefault ();
            }
        }


        private void BtnLast_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is ArticlesViewModel viewModel)
            {
                viewModel.SelectedArticle = viewModel.Artikli?.Last ();
            }
        }

        private void btnKategorija_Click(object sender, RoutedEventArgs e)
        {
            var page = new CategoriesPage ();
            page.DataContext = new CategoriesViewModel ();
            PageNavigator.NavigateWithFade (page);
        }

        private void CijenaTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            lblCijena.Visibility = Visibility.Collapsed;
            CijenaTextBox.BorderBrush = (SolidColorBrush)(new BrushConverter ().ConvertFrom ("#FF2FA4A9"));

        }

        private void ArtiklTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ArtiklTextBox.BorderBrush = (SolidColorBrush)(new BrushConverter ().ConvertFrom ("#FF2FA4A9"));
            lblArtikl.Visibility = Visibility.Collapsed;
        }

        private void ArtiklTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ArtiklTextBox.BorderBrush = Application.Current.Resources["GlobalFontColor"] as Brush;
        }

        private void CijenaTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CijenaTextBox.BorderBrush = Application.Current.Resources["GlobalFontColor"] as Brush;
        }
    }
}
