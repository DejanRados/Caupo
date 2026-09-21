using Caupo.Data;
using Caupo.Fiscal.Common;
using Caupo.Helpers;
using Caupo.Models;
using Caupo.ViewModels;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for KasaPage.xaml
    /// </summary>
    public partial class KasaPage : UserControl, IKeyboardInputReceiver
    {
        private bool _isFiscalizing = false;

        public KasaPage()
        {
            InitializeComponent();
            VirtualKeyboardManager.EnterPressed += VirtualKeyboard_EnterPressed;
            Unloaded += KasaPage_Unloaded;

            cmbNacinPlacanja.SelectedIndex = 0;
            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik.Radnik;
            //this.DataContext = new KasaViewModel ();
            MultiUserGrid.IsVisibleChanged += (s, e) => UpdateBlur();
        }

        private void VirtualKeyboard_EnterPressed()
        {
            if (DataContext is not KasaViewModel vm)
                return;

            // PIN unos
            if (vm.IsMultiUserVisible)
            {
                BtnPassword_Click(null, null);
                return;
            }

            // Unos količine
            if (VirtualKeyboardManager.ActiveTextBox == txtKolicina)
            {
                Keyboard.ClearFocus();
                VirtualKeyboardManager.ClearActiveTextBox();
                MainWindow.Instance.HideKeyboard();
                return;
            }
        }

        private void KasaPage_Unloaded(object sender, RoutedEventArgs e)
        {
            VirtualKeyboardManager.EnterPressed -= VirtualKeyboard_EnterPressed;
        }

        private static bool IsInsideScrollBar(DependencyObject? source)
        {
            while (source != null)
            {
                if (source is ScrollBar)
                    return true;

                source = VisualTreeHelper.GetParent(source);
            }

            return false;
        }

        private void UpdateBlur()
        {
            MainContent.Effect = MultiUserGrid.Visibility == Visibility.Visible
                ? new BlurEffect { Radius = 8 }
                : null;
        }

        private async void KasaWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not KasaViewModel vm)
                return;

            await vm.InitializeAsync();

            ArtikliScroll.ScrollToTop();
            KategorijeScroll.ScrollToTop();

            Debug.WriteLine("[KASA] KasaPage inicijalizovan.");
        }

        public void ReceiveKey(string key)
        {
            switch (key)
            {
                case "Sakrij":
                    MainWindow.Instance.HideKeyboard();
                    return;

                case "Reset":
                    btnReset_Click(null, null);
                    return;

                case "Enter":
                    return;
            }

            if (DataContext is KasaViewModel vm)
            {
                vm.FilterByFirstLetter(key);
                ArtikliScroll.ScrollToTop();
            }
        }

        private void KeyboardButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.ToggleKeyboard();
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
            if (sender is TextBox textBox && textBox.Name == "txtKolicina")
            {
                if (!decimal.TryParse(
                        textBox.Text,
                        NumberStyles.Number,
                        CultureInfo.CurrentCulture,
                        out decimal value) ||
                    value <= 0)
                {
                    textBox.Text = "1";
                }
            }

            VirtualKeyboardManager.ClearActiveTextBox();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.HideKeyboard();

            var page = new HomePage();
            PageNavigator.NavigateWithFade(page);
        }

        private async void BtnFiskalni_Click(object sender, EventArgs e)
        {
            if (_isFiscalizing)
            {
                Debug.WriteLine("[FISKALNI] Fiskalizacija je već u toku. Dodatni klik ignorisan.");
                return;
            }

            if (DataContext is not KasaViewModel viewModel || viewModel.StavkeRacuna.Count == 0)
                return;

            try
            {
                _isFiscalizing = true;
                MainContent.IsEnabled = false;
                btnFiskalni.IsEnabled = false;
                RacunIdikator.Visibility = Visibility.Visible;

                var fiscal = await viewModel.FiskalizujAsync(cmbNacinPlacanja.SelectedIndex);
                var result = fiscal.Result;

                if (result.FiscalizationStatus == FiscalizationStatus.Unknown)
                {
                    ShowMessage("STATUS RAČUNA NIJE POZNAT", result.ErrorMessage ?? "Nije moguće utvrditi da li je račun fiskalizovan.");
                    return;
                }

                if (!result.Success && !result.Fiscalized)
                {
                    ShowMessage("GREŠKA", result.ErrorMessage ?? "Račun nije izdan.");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(fiscal.Warning))
                    ShowMessage("UPOZORENJE", fiscal.Warning);

                if (viewModel.IsMultiUser)
                {
                    viewModel.IsLoggedIn = false;
                    await Dispatcher.BeginInvoke(new Action(() =>
                    {
                        txtPassword.Focus();
                        Keyboard.Focus(txtPassword);
                        txtPassword.SelectAll();
                    }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                }
                else
                {
                    viewModel.IsLoggedIn = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[FISKALNI] Greška: " + ex);
                ShowMessage("Greška", $"Došlo je do greške: {ex.Message}");
            }
            finally
            {
                _isFiscalizing = false;
                MainContent.IsEnabled = true;
                btnFiskalni.IsEnabled = true;
                RacunIdikator.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowMessage(string title, string message)
        {
            var myMessageBox = new MyMessageBox
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            myMessageBox.MessageTitle.Text = title;
            myMessageBox.MessageText.Text = message;
            myMessageBox.ShowDialog();
        }

        private void PiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not KasaViewModel vm)
                return;

            vm.SelectedCategory = KasaViewModel.Category.Pice;

            ArtikliScroll.ScrollToTop();
            KategorijeScroll.ScrollToTop();
        }

        private void HranaButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not KasaViewModel vm)
                return;

            vm.SelectedCategory = KasaViewModel.Category.Hrana;

            ArtikliScroll.ScrollToTop();
            KategorijeScroll.ScrollToTop();
        }

        private void OstaloButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not KasaViewModel vm)
                return;

            vm.SelectedCategory = KasaViewModel.Category.Ostalo;

            ArtikliScroll.ScrollToTop();
            KategorijeScroll.ScrollToTop();
        }

        private void ButtonCategory_Clicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (DataContext is not KasaViewModel vm)
                return;

            if (!int.TryParse(button.Tag?.ToString(), out int categoryId))
                return;

            vm.FilterByCategory(categoryId);

            ArtikliScroll.ScrollToTop();
        }

        private void FadeInOut(object sender)
        {
            if (sender is Button button)
            {
                DoubleAnimation fadeInAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = new Duration(TimeSpan.FromSeconds(0.5))
                };

                button.BeginAnimation(
                    UIElement.OpacityProperty,
                    fadeInAnimation);
            }
        }

        private bool isDragging = false;
        private Point startPoint;

        private void Button_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var button = sender as Button;

            if (button != null)
            {
                startPoint = e.GetPosition(null);
                isDragging = false;
            }
        }

        private void Button_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!isDragging)
            {
                var button = sender as Button;

                if (button != null)
                {
                    dugmic_Clicked(button, e);
                }
            }

            isDragging = false;
        }

        private void Button_MouseMove(object sender, MouseEventArgs e)
        {
            var button = sender as Button;

            if (button != null &&
                e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(null);
                var distance = (currentPosition - startPoint).Length;

                Debug.WriteLine(distance);

                if (distance > 5)
                {
                    if (!isDragging)
                    {
                        isDragging = true;

                        var artikl = button.DataContext as TblArtikli;

                        if (artikl != null)
                        {
                            DataObject data = new DataObject();
                            data.SetData("Artikl", artikl);

                            DragDrop.DoDragDrop(
                                button,
                                data,
                                DragDropEffects.Move);
                        }
                    }
                }
            }
        }

        private async void Button_Drop(object sender, DragEventArgs e)
        {
            if (sender is not Button targetButton)
                return;

            if (e.Data.GetData("Artikl") is not TblArtikli draggedArtikl)
                return;

            if (targetButton.DataContext is not TblArtikli targetArtikl)
                return;

            if (draggedArtikl.IdArtikla == targetArtikl.IdArtikla)
                return;

            if (DataContext is not KasaViewModel vm)
                return;

            await vm.SwapArticlePositionsAsync(draggedArtikl, targetArtikl);
        }

        private void Button_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("Artikl"))
            {
                e.Effects = DragDropEffects.None;
            }
            else
            {
                e.Effects = DragDropEffects.Move;
            }

            e.Handled = true;
        }

        private void dugmic_Clicked(object sender, EventArgs e)
        {
            if (sender is not Button button)
                return;

            VirtualKeyboardManager.ClearActiveTextBox();
            Keyboard.ClearFocus();

            if (button.DataContext is not TblArtikli artikl)
                return;

            if (DataContext is not KasaViewModel vm)
                return;

            if (!decimal.TryParse(
                    txtKolicina.Text,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out decimal kolicina) ||
                kolicina <= 0)
            {
                kolicina = 1m;
            }

            FadeInOut(button);

            int brojStavkiPrijeDodavanja = vm.StavkeRacuna.Count;

            vm.DodajArtikl(artikl, kolicina);

            if (vm.StavkeRacuna.Count > brojStavkiPrijeDodavanja)
            {
                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        if (ListStavkeRacuna.Items.Count == 0)
                            return;

                        var lastItem = ListStavkeRacuna.Items[ListStavkeRacuna.Items.Count - 1];

                        ListStavkeRacuna.ScrollIntoView(lastItem);
                    }));
            }

            txtKolicina.Text = "1";
        }

        private System.Timers.Timer? _longPressTimer;
        private const int LongPressThreshold = 400;
        private RacunStavka? _pressedItem;
        private DateTime _pressStartTime;

        private void EndPress()
        {
            if (_longPressTimer != null)
            {
                _longPressTimer.Stop();
                _longPressTimer.Dispose();
                _longPressTimer = null;

                Debug.WriteLine("LongPressTimer stopped in Up event");
            }

            if (_pressedItem != null)
            {
                var stavkaZaUpdate = _pressedItem;
                _pressedItem = null;

                if (DataContext is KasaViewModel vm &&
                    vm.StavkeRacuna.Contains(stavkaZaUpdate))
                {
                    decimal kolicina = 1m;

                    if (decimal.TryParse(
                            txtKolicina.Text,
                            NumberStyles.Number,
                            CultureInfo.CurrentCulture,
                            out decimal unesenaKolicina) &&
                        unesenaKolicina > 0)
                    {
                        kolicina = unesenaKolicina;
                    }

                    vm.UpdateStavkuRacunaMinus(stavkaZaUpdate, kolicina);
                }
                else
                {
                    Debug.WriteLine("Short tap ignored, stavka više ne postoji");
                }
            }
        }

        private void ListStavkeRacuna_PreviewMouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            if (IsInsideScrollBar(e.OriginalSource as DependencyObject))
                return;

            e.Handled = true;

            EndPress();
        }

        private RacunStavka? GetItemFromTouchOrMouse(
            object sender,
            InputEventArgs e)
        {
            if (sender is not DataGrid dg)
                return null;

            Point position;

            if (e is TouchEventArgs te)
            {
                position = te.GetTouchPoint(dg).Position;
            }
            else if (e is MouseEventArgs me)
            {
                position = me.GetPosition(dg);
            }
            else
            {
                return null;
            }

            var hit = VisualTreeHelper.HitTest(dg, position)?.VisualHit;

            if (hit == null)
                return null;

            var itemContainer = FindAncestor<DataGridRow>(hit);

            if (itemContainer?.DataContext is RacunStavka stavka)
                return stavka;

            return null;
        }

        private static T? FindAncestor<T>(DependencyObject current)
            where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T t)
                    return t;

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private void ListStavkeRacuna_TouchDown(
            object sender,
            TouchEventArgs e)
        {
            if (IsInsideScrollBar(e.OriginalSource as DependencyObject))
                return;

            Debug.WriteLine("TouchDown fired");

            e.Handled = true;

            var stavka = GetItemFromTouchOrMouse(sender, e);

            if (stavka != null)
            {
                _pressedItem = stavka;
                _pressStartTime = DateTime.Now;

                StartLongPressTimer(stavka);
            }
        }

        private void ListStavkeRacuna_TouchUp(
            object sender,
            TouchEventArgs e)
        {
            if (IsInsideScrollBar(e.OriginalSource as DependencyObject))
                return;

            e.Handled = true;

            EndPress();
        }

        private void ListStavkeRacuna_PreviewMouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (IsInsideScrollBar(e.OriginalSource as DependencyObject))
                return;

            Debug.WriteLine("MouseLeftButtonDown fired");

            e.Handled = true;

            var stavka = GetItemFromTouchOrMouse(sender, e);

            if (stavka != null)
            {
                _pressedItem = stavka;
                _pressStartTime = DateTime.Now;

                StartLongPressTimer(stavka);
            }
        }

        private void StartLongPressTimer(RacunStavka stavka)
        {
            if (_longPressTimer != null)
            {
                _longPressTimer.Stop();
                _longPressTimer.Dispose();
                _longPressTimer = null;

                Debug.WriteLine(
                    "Previous LongPressTimer stopped before starting new one");
            }

            _longPressTimer = new System.Timers.Timer(LongPressThreshold);

            _longPressTimer.Elapsed += (s, args) =>
            {
                _longPressTimer?.Stop();
                _longPressTimer?.Dispose();
                _longPressTimer = null;

                Debug.WriteLine(
                    "LongPressTimer elapsed for item: " +
                    stavka.Name);

                Dispatcher.Invoke(() =>
                {
                    if (_pressedItem != null &&
                        DataContext is KasaViewModel vm &&
                        vm.StavkeRacuna.Contains(_pressedItem))
                    {
                        Debug.WriteLine(
                            "Opening Note popup for: " +
                            _pressedItem.Name);

                        UnosNote(_pressedItem);
                        _pressedItem = null;
                    }
                    else
                    {
                        Debug.WriteLine(
                            "Long press canceled, stavka više ne postoji");
                    }
                });
            };

            _longPressTimer.Start();

            Debug.WriteLine(
                "LongPressTimer started for: " +
                stavka.Name);
        }

        private void UnosNote(RacunStavka stavka)
        {
            MainContent.Effect = new BlurEffect { Radius = 8 };

            MyInputBox dialog = new MyInputBox();

            dialog.InputTitle.Text = "OPIS STAVKE";
            dialog.InputText.Focus();

            dialog.ShowDialog();

            string result = dialog.result;

            if (!string.IsNullOrWhiteSpace(result))
            {
                stavka.Note = result;
            }

            MainContent.Effect = null;
        }

        private void btnNarudzbe_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is KasaViewModel viewModel)
            {
                Globals.forma = "Kasa";

                var page = new OrdersPage(viewModel);

                PageNavigator.NavigateWithFade(page);
            }
        }

        private void KasaWindow_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (DataContext is not KasaViewModel vm)
                return;

            if (Keyboard.FocusedElement is TextBox)
                return;

            if (e.Key >= Key.A && e.Key <= Key.Z)
            {
                vm.FilterByFirstLetter(e.Key.ToString());

                ArtikliScroll.ScrollToTop();

                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape)
            {
                vm.ArtikliFilterReset();

                ArtikliScroll.ScrollToTop();

                e.Handled = true;
            }
        }

        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnPassword_Click(null, null);
            }
        }

        private async void BtnPassword_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (DataContext is not KasaViewModel vm)
                return;

            string pin = txtPassword.Text.Trim();

            if (string.IsNullOrWhiteSpace(pin))
            {
                txtPassword.Focus();
                return;
            }

            TblRadnici? radnik;

            try
            {
                radnik = await vm.PrijaviRadnikaAsync(pin);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[KASA] Greška pri prijavi radnika: " + ex);
                ShowMessage("GREŠKA", "Nije moguće provjeriti lozinku zbog greške baze podataka. Pokušajte ponovo.");
                txtPassword.Focus();
                return;
            }

            // =====================================================
            // ISPRAVAN PIN
            // =====================================================

            if (radnik != null)
            {
                Globals.ulogovaniKorisnik = radnik;
                lblUlogovaniKorisnik.Content = radnik.Radnik;
                txtPassword.Text = "";
                MainWindow.Instance.HideKeyboard();
                return;
            }

            // =====================================================
            // POGREŠAN PIN
            // =====================================================

            vm.pokusaj--;

            if (vm.pokusaj <= 0)
            {
                MyMessageBox myMessageBox = new MyMessageBox
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                myMessageBox.MessageTitle.Text = "GREŠKA";
                myMessageBox.MessageText.Text =
                    "Pogrešna lozinka" +
                    Environment.NewLine +
                    "Nemate pristup aplikaciji";

                myMessageBox.ShowDialog();

                Application.Current.Shutdown();

                return;
            }

            MyMessageBox poruka = new MyMessageBox
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            poruka.MessageTitle.Text = "GREŠKA";
            poruka.MessageText.Text =
                "Pogrešna lozinka" +
                Environment.NewLine +
                $"Pokušajte ponovo, preostalo {vm.pokusaj} pokušaja.";

            poruka.ShowDialog();

            txtPassword.Text = "";

            txtPassword.Focus();
            Keyboard.Focus(txtPassword);
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not KasaViewModel vm)
                return;

            vm.ArtikliFilterReset();

            ArtikliScroll.ScrollToTop();
            KategorijeScroll.ScrollToTop();
        }

        private void txtKolicina_PreviewMouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            textBox.Focus();
            Keyboard.Focus(textBox);
            textBox.SelectAll();

            MainWindow.Instance.ShowKeyboard();
        }
    }
}