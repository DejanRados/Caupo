using Caupo.Data;
using Caupo.Helpers;
using Caupo.ViewModels;
using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
        }

        private VirtualKeyboard virtualKeyboard;
        private TextBox? FocusedTextBox = null;

        public void ReceiveKey(string key)
        {
            // 📌 Ako TextBox ima fokus → unos teksta
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
                        MainWindow.Instance.HideKeyboard();
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
            if (FocusedTextBox == null) return;

            int pos = FocusedTextBox.SelectionStart;
            FocusedTextBox.Text = FocusedTextBox.Text.Insert (pos, text);
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
                FocusedTextBox.Clear ();
                FocusedTextBox.SelectAll ();
                MainWindow.Instance.ShowKeyboard ();
            }
            catch (Exception ex)
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
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp";
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                SlikaTextBox.Text = filePath;

            }
        }

        private async void NormativPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(ArtiklTextBox.Text))
            {
                return;
            }
            var selectedNormativ = NormativPicker.SelectedItem as DatabaseTables.TblNormativPica;
            if (selectedNormativ != null)
            {
                string? normativ = selectedNormativ.Normativ;
                if (normativ == "1" || (decimal.TryParse (normativ, out decimal normValue) && normValue < 0.01m))
                {
                    if (ArtiklNormativTextBox != null)
                    {
                        ArtiklNormativTextBox.Text = ArtiklTextBox.Text;
                    }
                }
                else if (normativ != "Dodaj normativ")
                {
                    ArtiklNormativTextBox.Text = ArtiklTextBox.Text + " " + normativ;
                }
                else
                {
                    MyInputBox dialog = new MyInputBox();
                    dialog.InputTitle.Text = "NOVI NORMATIV PIĆA";
                    dialog.InputText.Focus();
                    dialog.ShowDialog();
                    string result = dialog.result;

                    if (!string.IsNullOrEmpty(result))
                    {
                        DatabaseTables.TblNormativPica normativPica = new DatabaseTables.TblNormativPica();
                        normativPica.Normativ = result;
                        using (var db = new AppDbContext())
                        {
                            db.NormativPica.Add(normativPica);
                            db.SaveChanges();
                            Debug.WriteLine("New record inserted successfully!");
                            if (DataContext is ArticlesViewModel viewModel)
                            {
                                await viewModel.LoadNormativi();

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
            if (DataContext is ArticlesViewModel viewModel)
            {


                var artikl = viewModel.SelectedArticle;
                await viewModel.LoadArticlesAsync();
                viewModel.SelectedArticle = artikl;
                _BorderBrush = Application.Current.Resources["GlobalFontColor"] as Brush;
                ChangeTextBoxBorderBrush(this, _BorderBrush);
                foreach (ComboBox comboBox in FindVisualChildren<ComboBox>(this))
                {
                    comboBox.BorderThickness = new Thickness(0, 0, 0, 1);
                }
                if (artikl != null)
                {
                    var foundItem = ListaArtikala.Items.Cast<dynamic>().FirstOrDefault(item =>
                        item.IdArtikla == artikl.IdArtikla);
                    if (foundItem != null)
                    {
                        Debug.WriteLine("Pronađen po ID-u - pokušavam selekciju i scroll");
                        ListaArtikala.SelectedItem = foundItem;
                        ListaArtikala.ScrollIntoView(foundItem);
                    }
                }
            }
        }

        private async void SaveEditButton_Click(object sender, RoutedEventArgs e)
        {
            bool isValid = ValidateForm();

            if (!isValid)
            {
                isValid = true;
                return;
            }

            if (DataContext is ArticlesViewModel viewModel)
            {
                viewModel.ErrorOccurred -= ViewModel_ErrorOccurred;
                viewModel.ErrorOccurred += ViewModel_ErrorOccurred;

                var artikl = viewModel.SelectedArticle;
                if (artikl != null)
                {
                    artikl.IdArtikla = Convert.ToInt32(IdArtiklaTextBox.Text);
                    artikl.Artikl = ArtiklTextBox.Text;
                    artikl.Cijena = Convert.ToDecimal(CijenaTextBox.Text);
                    artikl.Sifra = SifraTextBox.Text;
                    artikl.InternaSifra = InternaSifraTextBox.Text;
                    TblPoreskeStope ps = (TblPoreskeStope)PoreskeStopePicker.SelectedItem;
                    artikl.PoreskaStopa = ps.IdStope;
                    TblJediniceMjere jediniceMjere = (TblJediniceMjere)JediniceMjerePicker.SelectedItem;
                    artikl.JedinicaMjere = jediniceMjere.IdJedinice;
                    artikl.Slika = SlikaTextBox.Text;
                    artikl.VrstaArtikla = VrstaArtiklaPicker.SelectedIndex;
                    artikl.Pozicija = Convert.ToInt32(PozicijaTextBox.Text);
                    artikl.PrikazatiNaDispleju = PrikazatiNaDisplejuPicker.Text;
                    TblKategorije kategorija = (TblKategorije)KategorijePicker.SelectedItem;
                    artikl.Kategorija = kategorija.IdKategorije;
                    TblNormativPica normativ = (TblNormativPica)NormativPicker.SelectedItem;
                    artikl.Normativ = Convert.ToDecimal(normativ.Normativ);
                    artikl.ArtiklNormativ = ArtiklNormativTextBox.Text;
                    await viewModel.UpdateArticle(artikl);
                }

                if (_errorOccurred)
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
                foreach (ComboBox comboBox in FindVisualChildren<ComboBox>(this))
                {
                    comboBox.BorderThickness = new Thickness(0, 0, 0, 1);
                }
                ChangeTextBoxBorderBrush(this, _BorderBrush);
                if (artikl != null)
                {
                    var foundItem = ListaArtikala.Items.Cast<dynamic>().FirstOrDefault(item =>
                        item.IdArtikla == artikl.IdArtikla);
                    if (foundItem != null)
                    {
                        Debug.WriteLine("Pronađen po ID-u - pokušavam selekciju i scroll");
                        ListaArtikala.SelectedItem = foundItem;
                        ListaArtikala.ScrollIntoView(foundItem);
                    }
                }

            }
        }


        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject? child = VisualTreeHelper.GetChild(depObj, i);
                if (child == null) continue;

                if (child is T matchedChild)
                    yield return matchedChild;

                foreach (T childOfChild in FindVisualChildren<T>(child))
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
            myMessageBox.ShowDialog();
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


            if (DataContext is ArticlesViewModel viewModel)
            {
                viewModel.SelectedArticle = viewModel.Artikli?.FirstOrDefault();

                 _BorderBrush = Application.Current.Resources["GlobalFontColor"] as Brush; 

                ChangeTextBoxBorderBrush(this, _BorderBrush);
                foreach (ComboBox comboBox in FindVisualChildren<ComboBox>(this))
                {
                    comboBox.BorderThickness = new Thickness(0, 0, 0, 1);
                }
                ListaArtikala.ScrollIntoView(viewModel.SelectedArticle);
            }
        }


        private void ChangeTextBoxBorderBrush(DependencyObject parent, Brush? brush)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is TextBox textBox)
                {
                    if (textBox.Name != "SearchTextBox")
                    {
                        textBox.BorderBrush = brush;
                        textBox.BorderThickness = new Thickness(0, 0, 0, 1);
                    }
                }
                if (child is ComboBox cmb)
                {

                    cmb.BorderBrush = brush;
                    cmb.BorderThickness = new Thickness(0, 0, 0, 1);

                }
                ChangeTextBoxBorderBrush(child, brush);
            }
        }

        private async void SaveAddButton_Click(object sender, RoutedEventArgs e)
        {
            bool isValid = ValidateForm();

            if (!isValid)
            {
                isValid = true;
                return;
            }

            if (DataContext is ArticlesViewModel viewModel)
            {
                viewModel.ErrorOccurred -= ViewModel_ErrorOccurred;
                viewModel.ErrorOccurred += ViewModel_ErrorOccurred;

                TblArtikli artikl = new TblArtikli();
                artikl.Artikl = ArtiklTextBox.Text;
                artikl.Cijena = Convert.ToDecimal(CijenaTextBox.Text);
                artikl.Sifra = SifraTextBox.Text;
                artikl.InternaSifra = InternaSifraTextBox.Text;
                TblPoreskeStope ps = (TblPoreskeStope)PoreskeStopePicker.SelectedItem;
                artikl.PoreskaStopa = ps.IdStope;
                TblJediniceMjere jediniceMjere = (TblJediniceMjere)JediniceMjerePicker.SelectedItem;
                artikl.JedinicaMjere = jediniceMjere.IdJedinice;
                artikl.Slika = SlikaTextBox.Text;
                artikl.VrstaArtikla = VrstaArtiklaPicker.SelectedIndex;
                artikl.Pozicija = Convert.ToInt32(PozicijaTextBox.Text);
                artikl.PrikazatiNaDispleju = PrikazatiNaDisplejuPicker.Text;
                TblKategorije kategorija = (TblKategorije)KategorijePicker.SelectedItem;
                artikl.Kategorija = kategorija.IdKategorije;
                TblNormativPica normativ = (TblNormativPica)NormativPicker.SelectedItem;
                artikl.Normativ = Convert.ToDecimal(normativ.Normativ);
                artikl.ArtiklNormativ = ArtiklNormativTextBox.Text;
                await viewModel.InsertArticle(artikl);
                if (_errorOccurred)
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
                ChangeTextBoxBorderBrush(this, _BorderBrush);
                foreach (ComboBox comboBox in FindVisualChildren<ComboBox>(this))
                {
                    comboBox.BorderThickness = new Thickness(0, 0, 0, 1);
                }

                ListaArtikala.ScrollIntoView(ListaArtikala.SelectedItem);
            }
        }

        private void ArtiklTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            string newText = textBox.Text;
            if (ArtiklNormativTextBox != null)
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

            if (selectedItem != null)
            {
                if (DataContext is ArticlesViewModel viewModel)
                {
                    viewModel.SelectedArticle = selectedItem;
                    //SearchTextBox.Text = "";
                    ListaArtikala.ScrollIntoView(ListaArtikala.SelectedItem);
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

            if (DataContext is ArticlesViewModel viewModel)
            {
                viewModel.SelectedArticle = null;



                await viewModel.GetNewSifra();
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
                ArtiklTextBox.Focus();
            }


        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ArticlesViewModel viewModel)
            {
                int id = Convert.ToInt32(IdArtiklaTextBox.Text);

                YesNoPopup myMessageBox = new YesNoPopup();
                myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                myMessageBox.MessageTitle.Text = "POTVRDA BRISANJA";
                myMessageBox.MessageText.Text = "Da li ste sigurni da želite obrisati artikl :" + Environment.NewLine + ArtiklTextBox.Text + " ?";
                myMessageBox.ShowDialog();
                if (myMessageBox.Kliknuo == "Da")
                {
                    await viewModel.DeleteArticle(id);
                }
            }
        }

        Brush? _BorderBrush;
        private bool ValidateForm()
        {
            bool isValid = true;
            if (DataContext is ArticlesViewModel viewModel)
            {
                _BorderBrush = Application.Current.Resources["GlobalFontColor"] as Brush;
            }

            if (string.IsNullOrWhiteSpace(ArtiklTextBox.Text))
            {
                ArtiklTextBox.BorderBrush = Brushes.Red;
                isValid = false;
            }
            else
            {
                ArtiklTextBox.BorderBrush = _BorderBrush;
            }

            if (string.IsNullOrWhiteSpace(SifraTextBox.Text))
            {
                SifraTextBox.BorderBrush = Brushes.Red;
                isValid = false;
            }
            else
            {
                SifraTextBox.BorderBrush = _BorderBrush;
            }

            if (string.IsNullOrWhiteSpace(CijenaTextBox.Text))
            {
                CijenaTextBox.BorderBrush = Brushes.Red;
                isValid = false;
            }
            else
            {
                CijenaTextBox.BorderBrush = _BorderBrush;
            }

            if (string.IsNullOrWhiteSpace(ArtiklNormativTextBox.Text))
            {
                ArtiklNormativTextBox.BorderBrush = Brushes.Red;
                isValid = false;
            }
            else
            {
                ArtiklNormativTextBox.BorderBrush = _BorderBrush;
            }

            if (string.IsNullOrWhiteSpace(InternaSifraTextBox.Text))
            {
                InternaSifraTextBox.BorderBrush = Brushes.Red;
                isValid = false;
            }
            else
            {
                InternaSifraTextBox.BorderBrush = _BorderBrush;
            }

            if (string.IsNullOrWhiteSpace(PozicijaTextBox.Text))
            {
                PozicijaTextBox.BorderBrush = Brushes.Red;
                isValid = false;
            }
            else
            {
                PozicijaTextBox.BorderBrush = _BorderBrush;
            }


            if (PoreskeStopePicker.SelectedIndex == -1)
            {
                PoreskeStopePicker.BorderBrush = Brushes.Red;
                PoreskeStopePicker.BorderThickness = new Thickness(0, 0, 0, 1);
                isValid = false;
            }
            else
            {
                PoreskeStopePicker.BorderBrush = _BorderBrush;
                PoreskeStopePicker.BorderThickness = new Thickness(0, 0, 0, 1);
            }

            if (JediniceMjerePicker.SelectedIndex == -1)
            {
                JediniceMjerePicker.BorderBrush = Brushes.Red;
                JediniceMjerePicker.BorderThickness = new Thickness(0, 0, 0, 1);
                isValid = false;
            }
            else
            {
                JediniceMjerePicker.BorderBrush = _BorderBrush;
                JediniceMjerePicker.BorderThickness = new Thickness(0, 0, 0, 1);
            }

            if (VrstaArtiklaPicker.SelectedIndex == -1)
            {
                VrstaArtiklaPicker.BorderBrush = Brushes.Red;
                VrstaArtiklaPicker.BorderThickness = new Thickness(0, 0, 0, 1);
                isValid = false;
            }
            else
            {
                VrstaArtiklaPicker.BorderBrush = _BorderBrush;
                VrstaArtiklaPicker.BorderThickness = new Thickness(0, 0, 0, 1);
            }


            if (KategorijePicker.SelectedIndex == -1)
            {
                KategorijePicker.BorderBrush = Brushes.Red;
                KategorijePicker.BorderThickness = new Thickness(0, 0, 0, 1);
                isValid = false;
            }
            else
            {
                KategorijePicker.BorderBrush = _BorderBrush;
                KategorijePicker.BorderThickness = new Thickness(0, 0, 0, 1);
            }

            if (PrikazatiNaDisplejuPicker.SelectedIndex == -1)
            {
                PrikazatiNaDisplejuPicker.BorderBrush = Brushes.Red;
                PrikazatiNaDisplejuPicker.BorderThickness = new Thickness(0, 0, 0, 1);
                isValid = false;
            }
            else
            {
                PrikazatiNaDisplejuPicker.BorderBrush = _BorderBrush;
                PrikazatiNaDisplejuPicker.BorderThickness = new Thickness(0, 0, 0, 1);
            }

            if (NormativPicker.SelectedIndex == -1)
            {
                NormativPicker.BorderBrush = Brushes.Red;
                NormativPicker.BorderThickness = new Thickness(0, 0, 0, 1);
                isValid = false;
            }
            else
            {
                NormativPicker.BorderBrush = _BorderBrush;
                NormativPicker.BorderThickness = new Thickness(0, 0, 0, 1);
            }

            return isValid;
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

        private void SaveExcelFile(string filePath)
        {

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Artikli");

                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Sifra";
                worksheet.Cell(1, 3).Value = "Artikl";
                worksheet.Cell(1, 4).Value = "JedinicaMjere";
                worksheet.Cell(1, 5).Value = "Cijena";
                worksheet.Cell(1, 6).Value = "InternaSifra";
                worksheet.Cell(1, 7).Value = "PoreskaStopa";
                worksheet.Cell(1, 8).Value = "Slika";
                worksheet.Cell(1, 9).Value = "Normativ";
                worksheet.Cell(1, 10).Value = "VrstaArtikla";
                worksheet.Cell(1, 11).Value = "Kategorija";
                worksheet.Cell(1, 12).Value = "Pozicija";
                worksheet.Cell(1, 13).Value = "ArtiklNormativ";
                worksheet.Cell(1, 14).Value = "PrikazatiNaDispleju";

                if (DataContext is ArticlesViewModel viewModel)
                {
                    int row = 2;
                    if (viewModel.Artikli != null)
                    {
                        foreach (var artikl in viewModel.Artikli)
                        {
                            worksheet.Cell(row, 1).Value = artikl.IdArtikla;
                            worksheet.Cell(row, 2).Value = artikl.Sifra;
                            worksheet.Cell(row, 3).Value = artikl.Artikl;
                            worksheet.Cell(row, 4).Value = artikl.JedinicaMjere;
                            worksheet.Cell(row, 5).Value = artikl.Cijena;
                            worksheet.Cell(row, 6).Value = artikl.InternaSifra;
                            worksheet.Cell(row, 7).Value = artikl.PoreskaStopa;
                            worksheet.Cell(row, 8).Value = artikl.Slika;
                            worksheet.Cell(row, 9).Value = artikl.Normativ;
                            worksheet.Cell(row, 10).Value = artikl.VrstaArtikla;
                            worksheet.Cell(row, 11).Value = artikl.Kategorija;
                            worksheet.Cell(row, 12).Value = artikl.Pozicija;
                            worksheet.Cell(row, 13).Value = artikl.ArtiklNormativ;
                            worksheet.Cell(row, 14).Value = artikl.PrikazatiNaDispleju;

                            row++;
                        }
                    }
                }
                workbook.SaveAs(filePath);
            }

            MyMessageBox myMessageBox = new MyMessageBox
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            myMessageBox.MessageTitle.Text = "IZVOZ U EXCEL";
            myMessageBox.MessageText.Text = "Izvoz tabele Artikli je uspješno završen.";
            myMessageBox.ShowDialog();
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

        public async Task ImportExcelToSQLiteAsync(string excelFilePath)
        {
            using (var workbook = new XLWorkbook(excelFilePath))
            {
                var worksheet = workbook.Worksheets.First();
                using (var db = new AppDbContext())
                {

                    var artikliList = worksheet.RowsUsed()
                        .Skip(1)
                        .Select(row => new TblArtikli
                        {
                            Sifra = row.Cell(2).GetValue<string>(),
                            Artikl = row.Cell(3).GetValue<string>(),
                            JedinicaMjere = row.Cell(4).GetValue<int?>(),
                            Cijena = row.Cell(5).GetValue<decimal>(),
                            InternaSifra = row.Cell(6).GetValue<string>(),
                            PoreskaStopa = row.Cell(7).GetValue<int?>(),
                            Slika = row.Cell(8).GetValue<string>(),
                            Normativ = row.Cell(9).GetValue<decimal>(),
                            VrstaArtikla = row.Cell(10).GetValue<int>(),
                            Kategorija = row.Cell(11).GetValue<int?>(),
                            Pozicija = row.Cell(12).GetValue<int?>(),
                            ArtiklNormativ = row.Cell(13).GetValue<string>(),
                            PrikazatiNaDispleju = row.Cell(14).GetValue<string>(),

                        })
                        .ToList();


                    await db.Artikli.AddRangeAsync(artikliList);
                    await db.SaveChangesAsync();
                }
            }
            MyMessageBox myMessageBox = new MyMessageBox
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            myMessageBox.MessageTitle.Text = "UVOZ EXCEL";
            myMessageBox.MessageText.Text = "Uvoz artikala iz Excel fajla je završen!";
            myMessageBox.ShowDialog();

        }

        private void BtnFirst_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ArticlesViewModel viewModel)
            {
                viewModel.SelectedArticle = viewModel.Artikli?.FirstOrDefault();
            }
        }


        private void BtnLast_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ArticlesViewModel viewModel)
            {
                viewModel.SelectedArticle = viewModel.Artikli?.Last();
            }
        }
    }
}
