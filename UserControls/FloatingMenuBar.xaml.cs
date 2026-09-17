using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Caupo.UserControls
{
    public partial class FloatingMenuBar : UserControl
    {
        public event EventHandler<string>? TypeSelected;
        public event EventHandler<string>? CategorySelected;
        public event EventHandler<string>? UnitSelected;
        public event EventHandler? FinishRequested;
        public event MouseButtonEventHandler? DragStarted;
        public event MouseEventHandler? DragMoved;
        public event MouseButtonEventHandler? DragFinished;

        public FloatingMenuBar()
        {
            InitializeComponent();

            SetOptions(TypePanel, new[] { "Piće", "Hrana", "Ostalo" }, value => TypeSelected?.Invoke(this, value));
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
        private void CloseAllMenus()
        {
            TypePanel.Visibility = Visibility.Collapsed;
            CategoryPanel.Visibility = Visibility.Collapsed;
            UnitPanel.Visibility = Visibility.Collapsed;

            TypeArrowTransform.Angle = 0;
            CategoryArrowTransform.Angle = 0;
            UnitArrowTransform.Angle = 0;
        }

        // Šalje stranici zahtjev da završi trenutnu grupnu sesiju.
        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            CloseAllMenus();
            FinishRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}