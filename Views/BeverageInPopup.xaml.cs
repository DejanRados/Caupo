using Caupo.Properties;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Views
{
    public partial class BeverageInPopup : Window
    {
        public bool IsUpdate { get; set; } = false;
        public decimal EnteredPrice { get; private set; }
        public decimal EnteredQuantity { get; private set; }
        public decimal EnteredDiscount { get; private set; }
        public Brush? FontColor { get; set; }
        public string Result { get; set; }

        public string ArticleName { get; set; }
        public decimal? ArticlePrice { get; set; }
        public decimal ArticleQty { get; set; }
        public decimal ArticleDiscount { get; set; }

        public BeverageInPopup(TblArtikli selectedArticle)
        {
            InitializeComponent ();
            InitializeTheme ();
            DataContext = this;
            ArticleName = selectedArticle.Artikl;
            IsUpdate = false;
        }

        public BeverageInPopup(TblUlazStavke stavka)
        {
            InitializeComponent ();
            InitializeTheme ();
            DataContext = this;

            // Popuni UI za EDIT
            ArticleName = stavka.Artikl;
            ArticleQty = stavka.Kolicina;
            ArticlePrice = stavka.CijenaBezUPDV;
            ArticleDiscount = stavka.Rabat;
            IsUpdate = true;
        }

        private void InitializeTheme()
        {
            string tema = Settings.Default.Tema;
            if(tema == "Tamna")
            {
                FontColor = new SolidColorBrush (System.Windows.Media.Color.FromRgb (212, 212, 212));
                Application.Current.Resources["GlobalFontColor"] = FontColor;
            }
            else
            {
                FontColor = new SolidColorBrush (System.Windows.Media.Color.FromRgb (50, 50, 50));
                Application.Current.Resources["GlobalFontColor"] = FontColor;
            }
        }

        private void NumberValidationHandler(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            string fullText = textBox.Text.Insert (textBox.SelectionStart, e.Text);

            // Dozvoli brojeve sa decimalnom točkom ili zarezom
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch (
                fullText,
                @"^-?\d*([.,]\d*)?$"
            );
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if(!ValidateInputs ())
            {
                MessageBox.Show ("Molimo unesite ispravne vrijednosti.", "Greška",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ParseInputs ();
            this.DialogResult = true;
            this.Close ();
        }

        private bool ValidateInputs()
        {
            // Provjeri je li količina unesena
            if(string.IsNullOrWhiteSpace (Kolicina.Text))
            {
                Kolicina.Focus ();
                return false;
            }

            // Provjeri je li cijena unesena
            if(string.IsNullOrWhiteSpace (Cijena.Text))
            {
                Cijena.Focus ();
                return false;
            }

            return true;
        }

        private void ParseInputs()
        {
            string cijenaText = Cijena.Text.Replace (',', '.');
            string kolicinaText = Kolicina.Text.Replace (',', '.');
            string popustText = Popust.Text.Replace (',', '.');

            EnteredPrice = decimal.TryParse (cijenaText, NumberStyles.Any,
                CultureInfo.InvariantCulture, out var p) ? Math.Max (p, 0) : 0;

            EnteredQuantity = decimal.TryParse (kolicinaText, NumberStyles.Any,
                CultureInfo.InvariantCulture, out var q) ? Math.Max (q, 0) : 0;

            EnteredDiscount = decimal.TryParse (popustText, NumberStyles.Any,
                CultureInfo.InvariantCulture, out var d) ? Math.Max (d, 0) : 0;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close ();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeFields ();
            SetFocus ();
        }

        private void InitializeFields()
        {
            Article.Text = ArticleName;

            if(IsUpdate)
            {
                Cijena.Text = ArticlePrice.HasValue ? ArticlePrice.Value.ToString ("0.00") : "0.00";
                Kolicina.Text = ArticleQty.ToString ("0.00");
                Popust.Text = ArticleDiscount.ToString ("0.00");
            }
            else
            {
                Cijena.Text = "0.00";
                Kolicina.Text = "1";
                Popust.Text = "0.00";
            }
        }

        private void SetFocus()
        {
            Dispatcher.BeginInvoke (new Action (() =>
            {
                Cijena.Focus ();
                Cijena.SelectAll ();
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        // Dodatna poboljšanja za bolje korisničko iskustvo
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                OKButton_Click (sender, e);
            }
            else if(e.Key == Key.Escape)
            {
                CancelButton_Click (sender, e);
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if(sender is TextBox textBox)
            {
                textBox.SelectAll ();
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            Opacity = 1;
        }
    }
}