
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


        private void UpdateBlur()
        {
            MainContent.Effect = MultiUserGrid.Visibility == Visibility.Visible
                ? new BlurEffect { Radius = 8 }
                : null;
        }
        private void KasaWindow_Loaded(object sender, RoutedEventArgs e)
        {

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
                        //FocusedTextBox = null;
                        KeyboardButton_Click (null, null);
                        break;
                    case "Enter":
                        //FocusedTextBox = null;
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

                return;  // ⛔ NE IDE filter ako kucamo u textbox!
            }
            switch(key)
            {

                case "Sakrij":
                    //FocusedTextBox = null;
                    KeyboardButton_Click (null, null);
                    return;

                case "Reset":
                    FocusedTextBox = null;
                    btnReset_Click (null, null);
                    return;


                default:

                    break;

            }
            // 📌 Ako TextBox NIJE u fokusu → radi filter
            if(PicePanel.Visibility == Visibility.Visible)
                (DataContext as KasaViewModel)?.FilterDrinkByFirstLetter (key);

            if(HranaPanel.Visibility == Visibility.Visible)
                (DataContext as KasaViewModel)?.FilterFoodByFirstLetter (key);

            if(OstaloPanel.Visibility == Visibility.Visible)
                (DataContext as KasaViewModel)?.FilterRestByFirstLetter (key);
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
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if(FocusedTextBox.Name == "txtKolicina")
            {
                if(string.IsNullOrWhiteSpace (FocusedTextBox.Text))
                {
                    FocusedTextBox.Text = "1";
                }
                if(!double.TryParse (FocusedTextBox.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double value))
                {
                    FocusedTextBox.Text = "1";
                }

                if(value < 0)
                {
                    FocusedTextBox.Text = "1";
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
                        viewModel.StavkeRacuna.Clear ();
                        if(viewModel.IsMultiUser)
                        {

                            viewModel.IsLoggedIn = false;
                            await Dispatcher.BeginInvoke (new Action (() =>
                            {

                                txtPassword.Focus ();
                                Keyboard.Focus (txtPassword);
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

        private void PiceButton_Click(object sender, RoutedEventArgs e)
        {
            ArtikliScroll.ScrollToTop ();
            KategorijeScroll.ScrollToTop ();

            PicePanel.Visibility = Visibility.Visible;
            HranaPanel.Visibility = Visibility.Collapsed;
            OstaloPanel.Visibility = Visibility.Collapsed;

            KategorijaPicePanel.Visibility = Visibility.Visible;
            KategorijaHranaPanel.Visibility = Visibility.Collapsed;
            KategorijaOstaloPanel.Visibility = Visibility.Collapsed;
        }

        private void HranaButton_Click(object sender, RoutedEventArgs e)
        {
            ArtikliScroll.ScrollToTop ();
            KategorijeScroll.ScrollToTop ();

            PicePanel.Visibility = Visibility.Collapsed;
            HranaPanel.Visibility = Visibility.Visible;
            OstaloPanel.Visibility = Visibility.Collapsed;

            KategorijaPicePanel.Visibility = Visibility.Collapsed;
            KategorijaHranaPanel.Visibility = Visibility.Visible;
            KategorijaOstaloPanel.Visibility = Visibility.Collapsed;
        }

        private void OstaloButton_Click(object sender, RoutedEventArgs e)
        {
            ArtikliScroll.ScrollToTop ();
            KategorijeScroll.ScrollToTop ();

            PicePanel.Visibility = Visibility.Collapsed;
            HranaPanel.Visibility = Visibility.Collapsed;
            OstaloPanel.Visibility = Visibility.Visible;

            KategorijaPicePanel.Visibility = Visibility.Collapsed;
            KategorijaHranaPanel.Visibility = Visibility.Collapsed;
            KategorijaOstaloPanel.Visibility = Visibility.Visible;
        }


        private void ButtonCategoryDrink_Clicked(object sender, EventArgs e)
        {

            var button = sender as Button;

            Dispatcher.Invoke (() =>
        {
            if(DataContext is KasaViewModel viewModel)
            {

                viewModel?.FilterDrinkByCategory (Convert.ToInt32 (button.Tag));

            }
        });
        }

        private void ButtonCategoryFood_Clicked(object sender, EventArgs e)
        {

            var button = sender as Button;
            Debug.WriteLine ("Kliknuoa na " + button.Tag);
            Dispatcher.Invoke (() =>

            {
                if(DataContext is KasaViewModel viewModel)
                {
                    viewModel?.FilterFoodByCategory (Convert.ToInt32 (button.Tag));

                }
            });
        }

        private void ButtonCategoryRest_Clicked(object sender, EventArgs e)
        {

            var button = sender as Button;
            Dispatcher.Invoke (() =>

            {
                if(DataContext is KasaViewModel viewModel)
                {
                    viewModel?.FilterRestByCategory (Convert.ToInt32 (button.Tag));

                }
            });
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


        private async void Button_Drop(object sender, DragEventArgs e)
        {
            var targetButton = sender as Button;

            if(targetButton != null)
            {
                // Preuzimamo Artikl sa drag podacima
                var draggedArtikl = e.Data.GetData ("Artikl") as TblArtikli;
                var targetArtikl = targetButton.DataContext as TblArtikli;

                if(draggedArtikl != null && targetArtikl != null)
                {
                    // Razmenjujemo pozicije između dragovanog i ciljanog artikla
                    int? tempPozicija = draggedArtikl.Pozicija;
                    draggedArtikl.Pozicija = targetArtikl.Pozicija;
                    targetArtikl.Pozicija = tempPozicija;

                    // Ažuriramo UI tako da se reflektuje promena pozicija
                    if(DataContext is KasaViewModel viewModel)
                    {
                        // Pozivamo metodu da ažuriramo pozicije u bazi
                        await viewModel.UpdateArticlePosition (draggedArtikl);
                        await viewModel.UpdateArticlePosition (targetArtikl);
                    }
                }
            }
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
        private async void dugmic_Clicked(object sender, EventArgs e)
        {
            decimal kolicina = Convert.ToDecimal (txtKolicina.Text);
            var button = sender as Button;
            Debug.WriteLine ("Kliknuo na dugme: " + button.Tag);
            FadeInOut (button);
            using(var db = new AppDbContext ())
            {
                Debug.WriteLine ("Kliknuo na dugme: " + button.Tag);
                var artikl = await db.Artikli.SingleOrDefaultAsync (a => a.Sifra == button.Tag);
                if(artikl != null)
                {
                    FiskalniRacun.Item stavka = new FiskalniRacun.Item ();
                    stavka.Name = artikl.Artikl;
                    stavka.Sifra = artikl.Sifra;
                    stavka.BrojRacuna = 222;
                    stavka.Naziv = artikl.ArtiklNormativ;
                    //Obratiti pažnju na region poslije
                    stavka.Labels.Add (artikl.PoreskaStopa.ToString ());
                    stavka.UnitPrice = artikl.Cijena;
                    stavka.Proizvod = artikl.VrstaArtikla;
                    stavka.JedinicaMjere = artikl.JedinicaMjere;

                    Debug.WriteLine ("Convert.ToDecimal(txtKolicina.Text)  " + Convert.ToDecimal (txtKolicina.Text));
                    stavka.Quantity = Convert.ToDecimal (txtKolicina.Text);

                    Dispatcher.Invoke (() =>
                    {
                        if(DataContext is KasaViewModel viewModel)
                        {
                            var stavkaBezNote = viewModel.NadjiStavkuZaPovecanje (artikl.Sifra);

                            if(stavkaBezNote != null)
                            {
                                Debug.WriteLine ("Stavka postoji (bez note), kol: " + (decimal)stavkaBezNote.Quantity);
                                viewModel.UpdateStavkuRacunaPlus (stavkaBezNote, kolicina);
                            }
                            else
                            {
                                Debug.WriteLine ("Stavka ne postoji (bez note), dodaje novu: " + stavka.Name);
                                viewModel.DodajStavkuRacuna (stavka);
                                viewModel.StavkeRacuna.CollectionChanged += (s, e) =>
                                {
                                    if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                                    {
                                        Dispatcher.Invoke (() =>
                                        {
                                            if(ListStavkeRacuna.Items.Count > 0)
                                            {
                                                var lastItem = ListStavkeRacuna.Items[ListStavkeRacuna.Items.Count - 1];
                                                ListStavkeRacuna.ScrollIntoView (lastItem);
                                            }
                                        });
                                    }
                                };
                            }

                        }

                    });
                }
                txtKolicina.Text = "1";
            }
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

        private void ListStavkeRacuna_TouchUp(object sender, TouchEventArgs e)
        {
            e.Handled = true;
            EndPress ();
        }

        private void ListStavkeRacuna_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
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


        private void ListStavkeRacuna_TouchDown(object sender, TouchEventArgs e)
        {
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
        private void ListStavkeRacuna_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
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

        private void KasaWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(DataContext is KasaViewModel viewModel)
            {
                if(e.Key >= Key.A && e.Key <= Key.Z)
                {



                    string pressedKey = e.Key.ToString ();

                    if(PicePanel.Visibility == Visibility.Visible)
                    {
                        viewModel?.FilterDrinkByFirstLetter (pressedKey);
                    }

                    if(HranaPanel.Visibility == Visibility.Visible)
                    {
                        viewModel?.FilterFoodByFirstLetter (pressedKey);
                    }

                    if(OstaloPanel.Visibility == Visibility.Visible)
                    {
                        viewModel?.FilterRestByFirstLetter (pressedKey);
                    }
                }
                if(e.Key == Key.Escape)
                {
                    viewModel?.ArtikliFilterReset ();
                }

            }
        }

        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {

                BtnPassword_Click (null, null);
            }
        }

        private async void BtnPassword_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is KasaViewModel vm)
            {
                Debug.WriteLine ("---------------------------------------Password:" + txtPassword.Text + ", pokusaj: " + vm.pokusaj);
                vm.pokusaj -= 1;
                if(vm.pokusaj < 1)
                {
                    MyMessageBox myMessageBox = new MyMessageBox ();
                    myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    myMessageBox.MessageTitle.Text = "GREŠKA";
                    myMessageBox.MessageText.Text = "Pogrešna lozinka" + Environment.NewLine + "Nemate pristup aplikaciji";
                    myMessageBox.ShowDialog ();
                    System.Windows.Application.Current.Shutdown ();
                }

                using(var db = new AppDbContext ())
                {

                    var radnik = await db.Radnici
                                          .Where (r => r.Lozinka == txtPassword.Text)
                                          .FirstOrDefaultAsync ();

                    if(radnik == null)
                    {

                        MyMessageBox myMessageBox = new MyMessageBox ();
                        myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                        myMessageBox.MessageTitle.Text = "GREŠKA";
                        myMessageBox.MessageText.Text = "Pogrešna lozinka" + Environment.NewLine + "Pokušajte ponovo, preostalo " + vm.pokusaj + " pokušaja.";
                        myMessageBox.ShowDialog ();

                        txtPassword.Text = "";
                        txtPassword.Focus ();
                    }
                    else
                    {
                        Globals.ulogovaniKorisnik = radnik;
                        lblUlogovaniKorisnik.Content = radnik.Radnik;
                        MultiUserGrid.Visibility = Visibility.Collapsed;
                        MainWindow.Instance.HideKeyboard ();
                    }
                }
            }
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is KasaViewModel viewModel)
            {
                viewModel?.ArtikliFilterReset ();
            }
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


