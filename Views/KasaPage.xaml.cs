
using Caupo.Data;
using Caupo.Fiscal;
using Caupo.Helpers;
using Caupo.Properties;
using Caupo.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Tring.Fiscal.Driver;
using Tring.Fiscal.Driver.Interfaces;
using static Caupo.Data.DatabaseTables;
using static Caupo.ViewModels.SettingsViewModel;


namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for KasaPage.xaml
    /// </summary>
    public partial class KasaPage : UserControl, IKeyboardInputReceiver
    {

        public KasaPage()
        {
            InitializeComponent ();

            cmbNacinPlacanja.SelectedIndex = 0;
            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik.Radnik;
            //this.DataContext = new KasaViewModel ();
            MultiUserGrid.IsVisibleChanged += (s, e) => UpdateBlur ();
        }

        private static bool IsInsideScrollBar(DependencyObject? source)
        {
            while(source != null)
            {
                if(source is ScrollBar)
                    return true;

                source = VisualTreeHelper.GetParent (source);
            }

            return false;
        }

        private void UpdateBlur()
        {
            MainContent.Effect = MultiUserGrid.Visibility == Visibility.Visible
                ? new BlurEffect { Radius = 8 }
                : null;
        }
        private async void KasaWindow_Loaded( object sender,  RoutedEventArgs e)
        {
                            if(DataContext is not KasaViewModel vm)
                                return;

                            await vm.InitializeAsync ();

                            ArtikliScroll.ScrollToTop ();
                            KategorijeScroll.ScrollToTop ();

                            Debug.WriteLine (
                                "[KASA] KasaPage inicijalizovan.");
          }

        private VirtualKeyboard virtualKeyboard;
        private TextBox? FocusedTextBox = null;

        public void ReceiveKey(string key)
        {
            if(FocusedTextBox != null)
            {
                switch(key)
                {
                    case "\uE72B":

                        if(FocusedTextBox.Text.Length > 0)
                        {
                            int pos =
                                FocusedTextBox.SelectionStart;

                            if(pos > 0)
                            {
                                FocusedTextBox.Text =
                                    FocusedTextBox.Text.Remove (
                                        pos - 1,
                                        1);

                                FocusedTextBox.SelectionStart =
                                    pos - 1;
                            }
                        }

                        break;


                    case "\uE75D":

                        InsertIntoFocused (" ");
                        break;


                    case "Sakrij":

                        KeyboardButton_Click (null, null);
                        break;


                    case "Enter":

                        KeyboardButton_Click (null, null);
                        break;


                    case "Reset":

                        FocusedTextBox = null;

                        btnReset_Click (null, null);
                        break;


                    default:

                        InsertIntoFocused (key);
                        break;
                }

                return;
            }


            switch(key)
            {
                case "Sakrij":

                    KeyboardButton_Click (null, null);
                    return;


                case "Reset":

                    FocusedTextBox = null;

                    btnReset_Click (null, null);
                    return;
            }


            if(DataContext is KasaViewModel vm)
            {
                vm.FilterByFirstLetter (key);

                ArtikliScroll.ScrollToTop ();
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
            MainWindow.Instance.ToggleKeyboard ();

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
        private void TextBox_LostFocus(
            object sender,
            RoutedEventArgs e)
        {
            if(sender is TextBox textBox &&
                textBox.Name == "txtKolicina")
            {
                if(!decimal.TryParse (
                        textBox.Text,
                        NumberStyles.Number,
                        CultureInfo.CurrentCulture,
                        out decimal value) ||
                    value <= 0)
                {
                    textBox.Text = "1";
                }
            }

            FocusedTextBox = null;
        }


        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.HideKeyboard ();
            var page = new HomePage ();

            PageNavigator.NavigateWithFade (page);
        }



        private async void BtnFiskalni_Click(object sender, EventArgs e)
        {
            if(DataContext is KasaViewModel viewModel)
            {
                RacunIdikator.Visibility = Visibility.Visible;

                if(viewModel.StavkeRacuna.Count == 0)
                {
                    RacunIdikator.Visibility = Visibility.Collapsed;
                    return;
                }

                try
                {

                    FiskalniRacun fiskalniRacun = new FiskalniRacun ();
                    /**/
                    int brojracuna;
                    using(var db = new AppDbContext ())
                    {
                        brojracuna = await db.Racuni
                            .MaxAsync (r => (int?)r.BrojRacuna) ?? 0;  // Ako je tabela prazna, vrati 0
                    }

                    if(!Enum.TryParse (Properties.Settings.Default.Country, out Drzava odabranaDrzava))
                    {
                        Debug.WriteLine ("[FISKALNI] Nije moguće parsirati odabranu državu. ");
                        ShowMessage ("GREŠKA", "Niste izabrali region u kome aplikacija radi." + Environment.NewLine + "Ne možete izdavati račune");
                        return;
                    }


                    bool uspjeh = odabranaDrzava switch
                    {
                        Drzava.Hrvatska => await fiskalniRacun.IzdajFiskalniRacunHrvatska (
                            cmbNacinPlacanja.SelectedIndex,
                            viewModel.SelectedKupac,
                            viewModel.TotalSum,
                            viewModel.StavkeRacuna,
                            null,
                            false),

                        Drzava.RepublikaSrpska => await fiskalniRacun.IzdajFiskalniRacun (
                            "Training", "Sale", null, null,
                            viewModel.StavkeRacuna,
                            viewModel.SelectedKupac,
                            cmbNacinPlacanja.SelectedIndex,
                            viewModel.TotalSum),

                        Drzava.Srbija => await fiskalniRacun.IzdajFiskalniRacun (
                            "Training", "Sale", null, null,
                            viewModel.StavkeRacuna,
                            viewModel.SelectedKupac,
                            cmbNacinPlacanja.SelectedIndex,
                            viewModel.TotalSum),

                        Drzava.FederacijaBiH => await fiskalniRacun.IzdajFiskalniRacunTring (
                            cmbNacinPlacanja.SelectedIndex,
                            Globals.ulogovaniKorisnik,
                            viewModel.StavkeRacuna,
                            viewModel.SelectedKupac,
                            brojracuna),

                        _ => false
                    };








                    if(uspjeh)
                    {
                       
                        if(viewModel.IsMultiUser)
                        {
                            viewModel.ClearRacun ();
                            viewModel.IsLoggedIn = false;
                            await Dispatcher.BeginInvoke (new Action (() =>
                            {

                                txtPassword.Focus ();
                                Keyboard.Focus (txtPassword);
                                txtPassword.SelectAll ();
                                txtPassword.SelectAll ();

                            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                        }
                        else
                        {
                            viewModel.IsLoggedIn = true;
                        }
                        RacunIdikator.Visibility = Visibility.Collapsed;

                    }
                    else
                    {
                        RacunIdikator.Visibility = Visibility.Collapsed;
                    }


                }
                catch(Exception ex)
                {
                    RacunIdikator.Visibility = Visibility.Collapsed;
                    ShowMessage ("Greška", $"Došlo je do greške: {ex.Message}");
                }
                finally
                {
                    RacunIdikator.Visibility = Visibility.Collapsed;
                }
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
            myMessageBox.ShowDialog ();
        }

        private void PiceButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if(DataContext is not KasaViewModel vm)
                return;

            vm.SelectedCategory =
                KasaViewModel.Category.Pice;

            ArtikliScroll.ScrollToTop ();
            KategorijeScroll.ScrollToTop ();
        }

        private void HranaButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            if(DataContext is not KasaViewModel vm)
                return;

            vm.SelectedCategory =
                KasaViewModel.Category.Hrana;

            ArtikliScroll.ScrollToTop ();
            KategorijeScroll.ScrollToTop ();
        }

        private void OstaloButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            if(DataContext is not KasaViewModel vm)
                return;

            vm.SelectedCategory =
                KasaViewModel.Category.Ostalo;

            ArtikliScroll.ScrollToTop ();
            KategorijeScroll.ScrollToTop ();
        }


        private void ButtonCategory_Clicked(
         object sender,
         RoutedEventArgs e)
        {
            if(sender is not Button button)
                return;

            if(DataContext is not KasaViewModel vm)
                return;

            if(!int.TryParse (
                    button.Tag?.ToString (),
                    out int categoryId))
            {
                return;
            }

            vm.FilterByCategory (categoryId);

            ArtikliScroll.ScrollToTop ();
        }

        private void FadeInOut(object sender)
        {
            if(sender is Button button)
            {
                DoubleAnimation fadeInAnimation = new DoubleAnimation
                {
                    From = 0, // Start at fully transparent
                    To = 1,   // End at fully visible
                    Duration = new Duration (TimeSpan.FromSeconds (0.5)) // Duration of 2 seconds
                };
                button.BeginAnimation (UIElement.OpacityProperty, fadeInAnimation);
            }
        }


        private bool isDragging = false;
        private Point startPoint;

        private void Button_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var button = sender as Button;
            if(button != null)
            {
                // Zapamti poziciju miša na početku
                startPoint = e.GetPosition (null);  // Koristi `null` da dobiješ poziciju u odnosu na ekran
                isDragging = false; // Resetuj isDragging na false svaki put kad počneš pritisak
            }
        }


        private void Button_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Ako korisnik samo klikne, resetujemo isDragging
            if(!isDragging)
            {
                // Ovde možete dodati kod za normalni klik, kao što je:
                var button = sender as Button;
                if(button != null)
                {
                    dugmic_Clicked (button, e);
                }
            }

            isDragging = false; // Resetovanje stanja na kraju
        }

        private void Button_MouseMove(object sender, MouseEventArgs e)
        {
            var button = sender as Button;

            if(button != null && e.LeftButton == MouseButtonState.Pressed)
            {
                // Ako se pomerimo više od određene distance, smatramo da je to drag
                var currentPosition = e.GetPosition (null);
                var distance = (currentPosition - startPoint).Length;
                Debug.WriteLine (distance);
                if(distance > 5) // Minimalna udaljenost da bi se smatralo dragom (podesi po potrebi)
                {
                    if(!isDragging)
                    {
                        isDragging = true; // Sada je drag započeo

                        // Pokrećemo Drag&Drop operaciju
                        var artikl = button.DataContext as TblArtikli;
                        if(artikl != null)
                        {
                            DataObject data = new DataObject ();
                            data.SetData ("Artikl", artikl);

                            DragDrop.DoDragDrop (button, data, DragDropEffects.Move);
                        }
                    }
                }
            }
        }


        private async void Button_Drop(
            object sender,
            DragEventArgs e)
        {
            if(sender is not Button targetButton)
                return;

            if(e.Data.GetData ("Artikl")
                is not TblArtikli draggedArtikl)
            {
                return;
            }

            if(targetButton.DataContext
                is not TblArtikli targetArtikl)
            {
                return;
            }

            if(draggedArtikl.IdArtikla ==
                targetArtikl.IdArtikla)
            {
                return;
            }

            if(DataContext is not KasaViewModel vm)
                return;

            await vm.SwapArticlePositionsAsync (
                draggedArtikl,
                targetArtikl);
        }


        private void Button_DragOver(object sender, DragEventArgs e)
        {
            // Ovaj događaj omogućava drop operaciju, ako je drag operacija u toku
            if(!e.Data.GetDataPresent ("Artikl"))
            {
                e.Effects = DragDropEffects.None;
            }
            else
            {
                e.Effects = DragDropEffects.Move;
            }
            e.Handled = true;
        }
        string? TaxLabelHr(string ps)
        {

            string taxeslabel;
            switch(ps)
            {

                case "2":
                    taxeslabel = "\u0415";
                    break;
                case "4":
                    taxeslabel = "\u041A";
                    break;
                case "1":
                    taxeslabel = "\u0410";
                    break;
                case "3":
                    taxeslabel = "\u0408";
                    break;
                default:
                    taxeslabel = "Е";
                    break;
            }
            return taxeslabel;

        }

        string? TaxLabel(string ps)
        {

            string taxeslabel;
            switch(ps)
            {

                case "2":
                    taxeslabel = "\u0415";
                    break;
                case "4":
                    taxeslabel = "\u041A";
                    break;
                case "1":
                    taxeslabel = "\u0410";
                    break;
                case "3":
                    taxeslabel = "\u0408";
                    break;
                default:
                    taxeslabel = "Е";
                    break;
            }
            return taxeslabel;

        }

        private void dugmic_Clicked(
            object sender,
            EventArgs e)
        {
            if(sender is not Button button)
                return;

            if(button.DataContext
                is not TblArtikli artikl)
            {
                return;
            }

            if(DataContext
                is not KasaViewModel vm)
            {
                return;
            }


            if(!decimal.TryParse (
                    txtKolicina.Text,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out decimal kolicina) ||
                kolicina <= 0)
            {
                kolicina = 1m;
            }


            FadeInOut (button);


            var postojecaStavka =
                vm.NadjiStavkuZaPovecanje (
                    artikl.Sifra);


            if(postojecaStavka != null)
            {
                vm.UpdateStavkuRacunaPlus (
                    postojecaStavka,
                    kolicina);
            }
            else
            {
                var stavka =
                    new FiskalniRacun.Item
                    {
                        Name = artikl.Artikl,

                        Sifra = artikl.Sifra,

                        BrojRacuna = 222,

                        Naziv =
                            artikl.ArtiklNormativ,

                        UnitPrice =
                            artikl.Cijena,

                        Proizvod =
                            artikl.VrstaArtikla,

                        JedinicaMjere =
                            artikl.JedinicaMjere,

                        Quantity =
                            kolicina
                    };


                stavka.Labels.Add (
                    artikl.PoreskaStopa.ToString ());


                vm.DodajStavkuRacuna (
                    stavka);


                Dispatcher.BeginInvoke (
                    new Action (() =>
                    {
                        if(ListStavkeRacuna.Items.Count == 0)
                            return;

                        var lastItem =
                            ListStavkeRacuna.Items[
                                ListStavkeRacuna.Items.Count - 1];

                        ListStavkeRacuna.ScrollIntoView (
                            lastItem);
                    }));
            }


            txtKolicina.Text = "1";
        }


        private ScrollViewer GetScrollViewer(ListView listView)
        {
            UIElement frameworkElement = listView;

            while(frameworkElement != null)
            {
                var child = VisualTreeHelper.GetChild (frameworkElement, 0);
                var scrollViewer = child as ScrollViewer;
                if(scrollViewer != null)
                {
                    return scrollViewer;
                }
                frameworkElement = child as UIElement;
                if(frameworkElement == null)
                {
                    break;
                }
            }
            return null;
        }

        private System.Timers.Timer? _longPressTimer;
        private const int LongPressThreshold = 400; // ms
        private FiskalniRacun.Item? _pressedItem;
        private DateTime _pressStartTime;



        private void EndPress()
        {
            if(_longPressTimer != null)
            {
                _longPressTimer.Stop ();
                _longPressTimer.Dispose ();
                _longPressTimer = null;
                Debug.WriteLine ("LongPressTimer stopped in Up event");
            }

            if(_pressedItem != null)
            {
                var stavkaZaUpdate = _pressedItem;
                _pressedItem = null;

                if(DataContext is KasaViewModel vm && vm.StavkeRacuna.Contains (stavkaZaUpdate))
                {
                    vm.UpdateStavkuRacunaMinus (stavkaZaUpdate, Convert.ToDecimal (txtKolicina.Text));
                }
                else
                {
                    Debug.WriteLine ("Short tap ignored, stavka više ne postoji");
                }
            }
        }



        private void ListStavkeRacuna_PreviewMouseLeftButtonUp(
       object sender,
       MouseButtonEventArgs e)
        {
            if(IsInsideScrollBar (e.OriginalSource as DependencyObject))
                return;

            e.Handled = true;

            EndPress ();
        }


        private FiskalniRacun.Item? GetItemFromTouchOrMouse(object sender, InputEventArgs e)
        {
            if(sender is not DataGrid dg)
                return null;

            Point position;

            // Odredi touch ili mouse poziciju
            if(e is TouchEventArgs te)
                position = te.GetTouchPoint (dg).Position;
            else if(e is MouseEventArgs me)
                position = me.GetPosition (dg);
            else
                return null;

            // HitTest za vizualni element ispod kursora / touch-a
            var hit = VisualTreeHelper.HitTest (dg, position)?.VisualHit;
            if(hit == null)
                return null;

            // Pronađi ListViewItem roditelja
            var itemContainer = FindAncestor<DataGridRow> (hit);
            if(itemContainer?.DataContext is FiskalniRacun.Item stavka)
                return stavka;

            return null;
        }

        // Generic helper za pronalazak roditelja u vizualnom stablu
        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while(current != null)
            {
                if(current is T t)
                    return t;
                current = VisualTreeHelper.GetParent (current);
            }
            return null;
        }


        private void ListStavkeRacuna_TouchDown(
    object sender,
    TouchEventArgs e)
        {
            if(IsInsideScrollBar (e.OriginalSource as DependencyObject))
                return;

            Debug.WriteLine ("TouchDown fired");

            e.Handled = true;

            var stavka = GetItemFromTouchOrMouse (sender, e);

            if(stavka != null)
            {
                _pressedItem = stavka;
                _pressStartTime = DateTime.Now;

                StartLongPressTimer (stavka);
            }
        }

        private void ListStavkeRacuna_TouchUp(
    object sender,
    TouchEventArgs e)
        {
            if(IsInsideScrollBar (e.OriginalSource as DependencyObject))
                return;

            e.Handled = true;

            EndPress ();
        }


        private void ListStavkeRacuna_PreviewMouseLeftButtonDown(
    object sender,
    MouseButtonEventArgs e)
        {
            if(IsInsideScrollBar (e.OriginalSource as DependencyObject))
                return;

            Debug.WriteLine ("MouseLeftButtonDown fired");

            e.Handled = true;

            var stavka = GetItemFromTouchOrMouse (sender, e);

            if(stavka != null)
            {
                _pressedItem = stavka;
                _pressStartTime = DateTime.Now;

                StartLongPressTimer (stavka);
            }
        }


        private void StartLongPressTimer(FiskalniRacun.Item stavka)
        {
            // Stop old timer ako postoji
            if(_longPressTimer != null)
            {
                _longPressTimer.Stop ();
                _longPressTimer.Dispose ();
                _longPressTimer = null;
                Debug.WriteLine ("Previous LongPressTimer stopped before starting new one");
            }

            _longPressTimer = new System.Timers.Timer (LongPressThreshold);
            _longPressTimer.Elapsed += (s, args) =>
            {
                _longPressTimer?.Stop ();
                _longPressTimer?.Dispose ();
                _longPressTimer = null;

                Debug.WriteLine ("LongPressTimer elapsed for item: " + stavka.Name);

                Dispatcher.Invoke (() =>
                {
                    if(_pressedItem != null && DataContext is KasaViewModel vm && vm.StavkeRacuna.Contains (_pressedItem))
                    {
                        Debug.WriteLine ("Opening Note popup for: " + _pressedItem.Name);
                        UnosNote (_pressedItem);
                        _pressedItem = null;
                    }
                    else
                    {
                        Debug.WriteLine ("Long press canceled, stavka više ne postoji");
                    }
                });
            };
            _longPressTimer.Start ();
            Debug.WriteLine ("LongPressTimer started for: " + stavka.Name);
        }



        private void UnosNote(FiskalniRacun.Item stavka)
        {
            MainContent.Effect = new BlurEffect { Radius = 8 };
            MyInputBox dialog = new MyInputBox ();
            dialog.InputTitle.Text = "OPIS STAVKE";
            dialog.InputText.Focus ();
            dialog.ShowDialog ();
            string result = dialog.result;



            if(!string.IsNullOrWhiteSpace (result))
            {
                stavka.Note = result;
            }
            MainContent.Effect = null;
        }



        private void btnNarudzbe_Click(object sender, RoutedEventArgs e)
        {

            if(DataContext is KasaViewModel viewModel)
            {
                Globals.forma = "Kasa";
                var page = new OrdersPage (viewModel);
                //page.DataContext = new SettingsViewModel ();
                PageNavigator.NavigateWithFade (page);


            }
        }

        private void KasaWindow_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            if(DataContext is not KasaViewModel vm)
                return;


            if(FocusedTextBox != null)
                return;


            if(e.Key >= Key.A &&
                e.Key <= Key.Z)
            {
                vm.FilterByFirstLetter (
                    e.Key.ToString ());

                ArtikliScroll.ScrollToTop ();

                e.Handled = true;

                return;
            }


            if(e.Key == Key.Escape)
            {
                vm.ArtikliFilterReset ();

                ArtikliScroll.ScrollToTop ();

                e.Handled = true;
            }
        }

        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {

                BtnPassword_Click (null, null);
            }
        }


        private async void BtnPassword_Click(
    object sender,
    RoutedEventArgs e)
        {
            if(DataContext is not KasaViewModel vm)
                return;


            string pin =
                txtPassword.Text.Trim ();


            if(string.IsNullOrWhiteSpace (pin))
            {
                txtPassword.Focus ();

                return;
            }


            await using var db =
                new AppDbContext ();


            var radnik =
                await db.Radnici
                    .AsNoTracking ()
                    .FirstOrDefaultAsync (
                        r => r.Lozinka == pin);


            // =====================================================
            // ISPRAVAN PIN
            // =====================================================

            if(radnik != null)
            {
                Globals.ulogovaniKorisnik =
                    radnik;

                lblUlogovaniKorisnik.Content =
                    radnik.Radnik;


                vm.IsLoggedIn = true;


                txtPassword.Text = "";


                MainWindow.Instance.HideKeyboard ();

                return;
            }


            // =====================================================
            // POGREŠAN PIN
            // =====================================================

            vm.pokusaj--;


            if(vm.pokusaj <= 0)
            {
                MyMessageBox myMessageBox =
                    new MyMessageBox
                    {
                        WindowStartupLocation =
                            WindowStartupLocation.CenterScreen
                    };

                myMessageBox.MessageTitle.Text =
                    "GREŠKA";

                myMessageBox.MessageText.Text =
                    "Pogrešna lozinka" +
                    Environment.NewLine +
                    "Nemate pristup aplikaciji";

                myMessageBox.ShowDialog ();


                Application.Current.Shutdown ();

                return;
            }


            MyMessageBox poruka =
                new MyMessageBox
                {
                    WindowStartupLocation =
                        WindowStartupLocation.CenterScreen
                };

            poruka.MessageTitle.Text =
                "GREŠKA";

            poruka.MessageText.Text =
                "Pogrešna lozinka" +
                Environment.NewLine +
                $"Pokušajte ponovo, preostalo {vm.pokusaj} pokušaja.";

            poruka.ShowDialog ();


            txtPassword.Text = "";

            txtPassword.Focus ();

            Keyboard.Focus (
                txtPassword);
        }


        private void btnReset_Click(
         object sender,
         RoutedEventArgs e)
        {
            if(DataContext is not KasaViewModel vm)
                return;

            vm.ArtikliFilterReset ();

            ArtikliScroll.ScrollToTop ();
            KategorijeScroll.ScrollToTop ();
        }

        private void txtKolicina_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FocusedTextBox = sender as TextBox;
            FocusedTextBox.Clear ();
            FocusedTextBox.SelectAll ();
            MainWindow.Instance.ShowKeyboard ();
        }


    }
}


