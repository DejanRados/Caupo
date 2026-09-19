using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Caupo.UserControls
{
    public partial class FloatingMenuBar : UserControl
    {
        public event EventHandler<string>? PriceSelected;
        public event EventHandler<string>? TypeSelected;
        public event EventHandler<string>? CategorySelected;
        public event EventHandler<string>? TaxSelected;
        public event EventHandler<string>? UnitSelected;
        public event EventHandler<string>? NormativSelected;
        public event EventHandler<bool>? ConsumptionTaxSelected;
        public event EventHandler? DeleteSelectedRequested;
        public event EventHandler? FinishRequested;
        public event MouseButtonEventHandler? DragStarted;
        public event MouseEventHandler? DragMoved;
        public event MouseButtonEventHandler? DragFinished;

        public FloatingMenuBar()
        {
            InitializeComponent();
            SetOptions (ConsumptionTaxPanel, new[] { "DA", "NE" }, value => ConsumptionTaxSelected?.Invoke (this, value == "DA"));
            SetOptions (TypePanel, new[] { "Piće", "Hrana", "Ostalo" }, value => TypeSelected?.Invoke(this, value));
        }

        // Prosljeđuje početak povlačenja FloatingMenuBara stranici koja upravlja njegovom pozicijom.
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TitleBar.CaptureMouse();
            DragStarted?.Invoke(this, e);
        }

        // Prosljeđuje pomjeranje FloatingMenuBara stranici koja određuje njegove granice.
        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            DragMoved?.Invoke(this, e);
        }

        // Završava povlačenje i oslobađa mouse capture naslovne trake.
        private void TitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TitleBar.ReleaseMouseCapture();
            DragFinished?.Invoke(this, e);
        }
        // Postavlja broj trenutno označenih artikala u zaglavlju FloatingMenuBara.
        public void SetSelectedCount(int count)
        {
            SelectedCountText.Text = $"Odabrano: {count} artikala";
        }

        // Postavlja kategorije koje su dostupne za trenutno označenu vrstu artikla.
        public void SetCategories(IEnumerable<string> categories)
        {
            SetOptions(CategoryPanel, categories, value => CategorySelected?.Invoke(this, value));
        }



        // Postavlja raspoložive jedinice mjere.
        public void SetUnits(IEnumerable<string> units)
        {
            SetOptions(UnitPanel, units, value => UnitSelected?.Invoke(this, value));
        }

        // Postavlja raspoložive poreske stope.
        // Postavlja raspoložive poreske stope sa odvojenim prikazom i stvarnom vrijednošću.
        public void SetTaxes(IEnumerable<(string Display, string Value)> taxes)
        {
            TaxPanel.Children.Clear ();

            foreach(var tax in taxes)
            {
                var button = new Button
                {
                    Content = tax.Display,
                    Height = 42,
                    Style = (Style)FindResource ("FloatingSubMenuButtonStyle")
                };

                button.Click += (s, e) =>
                {
                    TaxSelected?.Invoke (this, tax.Value);
                    CloseAllMenus ();
                };

                TaxPanel.Children.Add (button);
            }
        }


        // Postavlja raspoložive normative, uključujući akciju za dodavanje novog normativa.
        public void SetNormativi(IEnumerable<string> normativi)
        {
            SetOptions (NormativPanel, normativi, value => NormativSelected?.Invoke (this, value));
        }


        // Kreira touch-friendly dugmad za ponuđene vrijednosti jednog podmenija.
        private void SetOptions(StackPanel panel, IEnumerable<string> values, Action<string> selected)
        {
            panel.Children.Clear();

            foreach (string value in values)
            {
                var button = new Button
                {
                    Content = value,
                    Height = 42,
                    Style = (Style)FindResource("FloatingSubMenuButtonStyle")
                };

                button.Click += (s, e) =>
                {
                    selected(value);
                    CloseAllMenus();
                };

                panel.Children.Add(button);
            }
        }


        // Otvara ili zatvara unos cijene i zatvara sve ostale podmenije.
        private void PriceButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenu (PricePanel, PriceArrowTransform);

            if(PricePanel.Visibility != Visibility.Visible)
                return;

            Dispatcher.BeginInvoke (new Action (() =>
            {
                PriceTextBox.Focus ();
                PriceTextBox.SelectAll ();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        // Primjenjuje unesenu cijenu.
        private void PriceOKButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyPrice ();
        }

        // Enter radi isto što i dugme OK.
        private void PriceTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key != Key.Enter)
                return;

            ApplyPrice ();
            e.Handled = true;
        }

        private void ApplyPrice()
        {
            PriceSelected?.Invoke (this, PriceTextBox.Text);
        }
        // Otvara ili zatvara izbor vrste i zatvara sve ostale podmenije.
        private void TypeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenu(TypePanel, TypeArrowTransform);
        }

        // Otvara ili zatvara izbor kategorije i zatvara sve ostale podmenije.
        private void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenu(CategoryPanel, CategoryArrowTransform);
        }

        // Otvara ili zatvara izbor jedinice mjere i zatvara sve ostale podmenije.
        private void UnitButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenu(UnitPanel, UnitArrowTransform);
        }

        // Otvara ili zatvara izbor poreske stope i zatvara sve ostale podmenije.
        private void TaxButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenu (TaxPanel, TaxArrowTransform);
        }

        // Otvara ili zatvara izbor normativa i zatvara sve ostale podmenije.
        private void NormativButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenu (NormativPanel, NormativArrowTransform);
        }

        // Otvara ili zatvara izbor poreza na potrošnju i zatvara sve ostale podmenije.
        private void ConsumptionTaxButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenu (ConsumptionTaxPanel, ConsumptionTaxArrowTransform);
        }

        private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedRequested?.Invoke (this, EventArgs.Empty);
        }

        // Otvara odabrani podmeni, a sve ostale zatvara.
        private void ToggleMenu(StackPanel panel, RotateTransform arrow)
        {
            bool open = panel.Visibility != Visibility.Visible;

            CloseAllMenus();

            if (!open)
                return;

            panel.Visibility = Visibility.Visible;
            arrow.Angle = 90;
        }


        // Zatvara sve trenutno otvorene podmenije.
        public void CloseAllMenus()
        {
            PricePanel.Visibility = Visibility.Collapsed;
            TypePanel.Visibility = Visibility.Collapsed;
            CategoryPanel.Visibility = Visibility.Collapsed;
            UnitPanel.Visibility = Visibility.Collapsed;
            TaxPanel.Visibility = Visibility.Collapsed;
            NormativPanel.Visibility = Visibility.Collapsed;
            ConsumptionTaxPanel.Visibility = Visibility.Collapsed;

            PriceArrowTransform.Angle = 0;
            TypeArrowTransform.Angle = 0;
            CategoryArrowTransform.Angle = 0;
            UnitArrowTransform.Angle = 0;
            TaxArrowTransform.Angle = 0;
            NormativArrowTransform.Angle = 0;
            ConsumptionTaxArrowTransform.Angle = 0;
        }

        // Šalje stranici zahtjev da završi trenutnu grupnu sesiju.
        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            CloseAllMenus();
            FinishRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}