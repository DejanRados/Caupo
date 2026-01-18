using Caupo.Data;
using Caupo.Helpers;
using Caupo.ViewModels;
using Microsoft.EntityFrameworkCore;
//using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for OrdersPage.xaml
    /// </summary>
    public partial class OrdersPage : UserControl
    {

        private List<ButtonInfo> buttons = new List<ButtonInfo>();

        private Button draggedButton = null;
        private Point clickPosition;
        private bool isInitialized = false;
        private OrdersViewModel ordersViewModel;
        private KasaViewModel kasaViewModel;
        public OrdersPage(KasaViewModel _kasaViewModel)
        {
            kasaViewModel = _kasaViewModel;
            ordersViewModel = new OrdersViewModel(kasaViewModel);
            this.DataContext = ordersViewModel;


            InitializeComponent();
            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik.Radnik;
            //LoadButtons();

        }

    


        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized)
                return;
            TabItem tab = new TabItem();
            if (sender is TabControl tabCon)
            {
                // Selektovani tab je u SelectedItem property
                if (tabControl.SelectedItem is TabItem selectedTab)
                {
                    string header = (selectedTab.Header as TextBlock)?.Text ?? selectedTab.Header.ToString();
                    tab.Name = selectedTab.Name;
                    Debug.WriteLine("Selektovan tab: " + header + ", Name: " + tab.Name);
                }
            }

            if (tabControl.SelectedIndex == tabControl.Items.Count - 1)
            {
                if (DataContext is OrdersViewModel viewModel)
                {
                    int totalTabs = tabControl.Items.Count;

                    // Kreiraj novi Canvas
                    Canvas CanvasPanel = new Canvas
                    {

                        Background = Brushes.Transparent,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch
                    };

                    // Dodaj event handler za dvostruki klik
                    CanvasPanel.MouseDown += CanvasPanel_MouseDoubleClick;


                    string header = GenerateNextTabName();
                    string name = "sala_" + Guid.NewGuid ().ToString ("N");

                    tabControl.Items.Insert(tabControl.Items.Count - 1, CreateTab(CanvasPanel, name, header));
                    // Postavi fokus na novokreirani tab
                    tabControl.Dispatcher.BeginInvoke(new Action(() =>
                    {

                        tabControl.SelectedIndex = tabControl.Items.Count - 2;


                    }), System.Windows.Threading.DispatcherPriority.Background);

                }
            }

        }

        private void CanvasPanel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

            if (e.ClickCount == 2) // Proveravamo da li je dvokliknut
            {
                draggedButton = null;
                if (sender is Canvas current)
                {
                    int buttonCount = current.Children.Count;
                    var position = e.GetPosition(current);
                    Debug.WriteLine("Dvoklik na: " + position.X + ", " + position.Y);
                    // Kreiranje dugmeta
                    var button = new TableButton();
                    //button.Name = "STO" + (buttonCount + 1);
                    button.TableName = "STO" + (buttonCount + 1);
                    button.Waiter = null;
                    button.WaiterId = null;
                    button.Total = null;
                    button.Height = current.ActualHeight*0.14;
                    button.Width = current.ActualHeight * 0.14;
                    button.Tag = Environment.TickCount;
                    button.IsHitTestVisible = true;
                    button.PreviewMouseLeftButtonDown += Button_MouseLeftButtonDown;
                    button.PreviewMouseMove += Button_MouseMove;
                    button.MouseUp += Button_MouseUp;
                    button.MouseRightButtonDown += Button_MouseRightButtonDown;
                    button.Click += Button_Click;
                    var style = Application.Current.FindResource("TableButtonStyle") as Style;
                    if (style != null)
                    {
                        button.Style = style;
                    }
                    var stackPanel = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    var textBlock = new TextBlock
                    {
                        Name = "TableName", 
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Center,
                        Text = button.TableName,
                    };
                    // Binding TextBlock.Text → TableButton.TableName
                    textBlock.SetBinding (TextBlock.TextProperty, new System.Windows.Data.Binding (nameof (TableButton.TableName))
                    {
                        Source = button,
                        Mode = System.Windows.Data.BindingMode.TwoWay
                    });

                    var textBlock2 = new TextBlock
                    {
                        Name = "WaiterName",
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        FontSize = 10,
                        VerticalAlignment = VerticalAlignment.Center,
                        Text = button.Waiter
                    };

                    var textBlock3 = new TextBlock
                    {
                        Name = "TotalAmount",
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        FontSize = 10,
                        VerticalAlignment = VerticalAlignment.Center,
                        Text = button.Total
                    };

                    stackPanel.Children.Add (textBlock);
                    stackPanel.Children.Add (textBlock2);
                    stackPanel.Children.Add (textBlock3);

                    button.Content = stackPanel;


                    Canvas.SetLeft(button, position.X);
                    Canvas.SetTop(button, position.Y);
                    current.Children.Add(button);

                    buttons.Add(new ButtonInfo
                    {
                        ID = Convert.ToInt32(button.Tag),
                        X = position.X,
                        Y = position.Y,
                        Text = button.TableName,
                        Width = button.Width,
                        Height = button.Height,
                        ButtonBorderBrush = (SolidColorBrush)button.BorderBrush,
                        Total = null,
                        Waiter = null,
                        WaiterId = null
                    });

                }
            }
        }

        private void Button_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {

            var button = sender as TableButton;
            if (button != null)
            {
                var menuSto = new ContextMenu();

                var separatorSto1 = new Separator
                {
                    Height = 1,
                    Margin = new Thickness (5, 2, 0, 0),
                    Background = Brushes.LightGray
                };
                var separatorSto2 = new Separator
                {
                    Height = 1,
                    Margin = new Thickness (5, 2, 0, 0),
                    Background = Brushes.LightGray
                };

                var renameItemSto = new MenuItem ();
                var stackPanelRenameSto = new StackPanel
                {
                    Orientation = Orientation.Horizontal
                };
                var iconSto1 = new TextBlock
                {
                    Text = "\uE895", // Edit ikona (hex kod)
                    FontFamily = new FontFamily ("Segoe MDL2 Assets"),
                    FontSize = 16,
                    Margin = new Thickness (0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                var textSto1 = new TextBlock
                {
                    Text = "Preimenuj sto",
                    VerticalAlignment = VerticalAlignment.Center
                };
                stackPanelRenameSto.Children.Add (iconSto1);
                stackPanelRenameSto.Children.Add (textSto1);
                renameItemSto.Header = stackPanelRenameSto;
                renameItemSto.Click += (s, args) => EditText (button);
             
                var resizeItem = new MenuItem ();
                var stackPanelResize = new StackPanel
                {
                    Orientation = Orientation.Horizontal
                };
                var iconResize = new TextBlock
                {
                    Text = "\uE744", 
                    FontFamily = new FontFamily ("Segoe MDL2 Assets"),
                    FontSize = 16,
                    Margin = new Thickness (0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                var textResize = new TextBlock
                {
                    Text = "Promijeni veličinu stola",
                    VerticalAlignment = VerticalAlignment.Center
                };
                stackPanelResize.Children.Add (iconResize);
                stackPanelResize.Children.Add (textResize);
                resizeItem.Header = stackPanelResize;
                resizeItem.Click += (s, args) => EditSize (button);
    
                var deleteItemSto = new MenuItem ();
                var stackPanelDeleteSto = new StackPanel
                {
                    Orientation = Orientation.Horizontal
                };
                var iconSto2 = new TextBlock
                {
                    Text = "\uE74D", // Edit ikona (hex kod)
                    FontFamily = new FontFamily ("Segoe MDL2 Assets"),
                    FontSize = 16,
                    Margin = new Thickness (0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                var textSto2 = new TextBlock
                {
                    Text = "Obriši dugme",
                    VerticalAlignment = VerticalAlignment.Center
                };
                stackPanelDeleteSto.Children.Add (iconSto2);
                stackPanelDeleteSto.Children.Add (textSto2);
                deleteItemSto.Header = stackPanelDeleteSto;
                deleteItemSto.Click += (s, args) => DeleteButton (button);

                menuSto.Items.Add (renameItemSto);
                menuSto.Items.Add (separatorSto1);
                menuSto.Items.Add (resizeItem);
                menuSto.Items.Add (separatorSto2);
              
                if (button.BorderBrush is SolidColorBrush solidBrush && solidBrush.Color != Colors.Red)
                {
                    menuSto.Items.Add(deleteItemSto);
                }
                menuSto.IsOpen = true;
            }
        }
     
        private Point dragStartPoint;
        private bool isDragging;
        private void Button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Počinjemo praćenje drag operacije
            draggedButton = sender as TableButton;

            var current = VisualTreeHelper.GetParent(draggedButton);
            while (current != null && current is not Canvas)
            {
                current = VisualTreeHelper.GetParent(current);
            }
            if (current is Canvas canvas)
            {
                dragStartPoint = e.GetPosition(canvas);
                isDragging = false;

                // Počinjemo hvatanje miša
                draggedButton.CaptureMouse();
                e.Handled = true;
            }
        }

        private void Button_MouseMove(object sender, MouseEventArgs e)
        {
            if (draggedButton != null && e.LeftButton == MouseButtonState.Pressed)
            {
                var current = VisualTreeHelper.GetParent (draggedButton);
                while (current != null && current is not Canvas)
                {
                    current = VisualTreeHelper.GetParent (current);
                }

                if (current is Canvas canvas)
                {
                    Point currentPosition = e.GetPosition (canvas);

                    // Proveravam da li je miš dovoljno pomjeren da smatram da je drag operacija
                    if (!isDragging &&
                        (Math.Abs (currentPosition.X - dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                         Math.Abs (currentPosition.Y - dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance))
                    {
                        isDragging = true;
                    }

                    if (isDragging)
                    {
                        // Ograničenje unutar granica canvasa
                        double newLeft = currentPosition.X - draggedButton.ActualWidth / 2;
                        double newTop = currentPosition.Y - draggedButton.ActualHeight / 2;

                        // Ne dozvoljavamo da dugme izađe van lijeve/gore granice
                        newLeft = Math.Max (0, newLeft);
                        newTop = Math.Max (0, newTop);

                        // Ne dozvoljavam da dugme izađe van desne/dole granice
                        newLeft = Math.Min (canvas.ActualWidth - draggedButton.ActualWidth, newLeft);
                        newTop = Math.Min (canvas.ActualHeight - draggedButton.ActualHeight, newTop);

                        Canvas.SetLeft (draggedButton, newLeft);
                        Canvas.SetTop (draggedButton, newTop);
                    }
                }
            }
        }


        private void Button_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (draggedButton != null)
            {
                if (!isDragging)
                {
                    // Ako nije bilo drag operacije, ostavljamo dugme da obradi Click event
                    return;
                }

                // Oslobađamo miš
                draggedButton.ReleaseMouseCapture();
                draggedButton = null;
                isDragging = false;
            }
        }

        private TabItem FindParentTab(DependencyObject child)
        {
            DependencyObject parent = child;

            while (parent != null)
            {
                if (parent is TabItem tabItem)
                    return tabItem;

                parent = LogicalTreeHelper.GetParent(parent);
            }

            return null;
        }

        string sto;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string sala = string.Empty;
            if (isDragging) return;

            TableButton? button = sender as TableButton;
            Debug.WriteLine ($"Dugme kliknuto Tag: {button.Tag} ");
            TabItem tab = FindParentTab(button);
            string tabName;
            Debug.WriteLine ($"Dugme kliknuto IF button.waiter: {button.Waiter} =  {Globals.ulogovaniKorisnik.IdRadnika.ToString ()}) ili button.waiter jednako prazno");
            if (button.WaiterId == Globals.ulogovaniKorisnik.IdRadnika.ToString() ||  button.Waiter == null)
            {
               
                if (DataContext is OrdersViewModel viewModel)
                {

                    int ID = Convert.ToInt32(button.Tag);
                    if (tab != null)
                    {
                        tabName = tab.Name;
                        string header = (tab.Header as TextBlock)?.Text ?? tab.Header.ToString();
                        sala = tabName;

                        Debug.WriteLine($"Dugme kliknuto u tabu: {header} (ID: {tabName})");
                    }

                    viewModel.IdStola = ID;
                    viewModel.Sala = sala;
                    Debug.WriteLine("Kliknuto je nad dugme koje se nalazi u " + sala + " a viewModel.Sala je " + viewModel.Sala);
                 

                    viewModel.ImeStola = button.TableName;

                    var page = new OrderPage (viewModel);
                    //page.DataContext = new SettingsViewModel ();
                    PageNavigator.NavigateWithFade (page);
                  

                }
            }
            else
            {


                    sto = button.TableName;
        
                string naslov = "GREŠKA";
                string tekst = $"Niste kreirali ovu narudžbu. " + Environment.NewLine + sto + " ne pripada Vama.";
                MyMessageBox myMessageBox = new MyMessageBox();
                ////myMessageBox.Owner = this;
                myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                myMessageBox.MessageTitle.Text = naslov;
                myMessageBox.MessageText.Text = tekst;
                myMessageBox.ShowDialog();
                return;
            }



        }


        public class SolidColorBrushConverter : System.Text.Json.Serialization.JsonConverter<SolidColorBrush>
        {
            public override SolidColorBrush Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {

                using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
                {
                    var colorString = doc.RootElement.GetProperty("Color").GetString();
                    var color = (System.Windows.Media.Color)ColorConverter.ConvertFromString(colorString);
                    return new SolidColorBrush(color);
                }
            }

            public override void Write(Utf8JsonWriter writer, SolidColorBrush value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteString("Color", value.Color.ToString());
                writer.WriteEndObject();
            }
        }

        public class ImageBrushConverter : System.Text.Json.Serialization.JsonConverter<BitmapImage>
        {
            public override BitmapImage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                // Čitanje URI-a slike kao string iz JSON-a
                using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
                {
                    var imageUri = doc.RootElement.GetProperty("ImageUri").GetString();
                    var image = new BitmapImage(new Uri(imageUri));
                    return image;
                }
            }

            public override void Write(Utf8JsonWriter writer, BitmapImage value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteString("ImageUri", value.ToString());
                writer.WriteEndObject();
            }
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new SolidColorBrushConverter (), new ImageBrushConverter () }
            };

            var allTabs = new List<CanvasTabInfo> ();

            Debug.WriteLine ("🔹 Start saving all tabs...");

            foreach(var item in tabControl.Items)
            {
                if(item is TabItem tab && tab.Header?.ToString () != "+")
                {
                    Debug.WriteLine ($"-- Processing tab: {tab.Header}");

                    if(tab.Content is Canvas canvas)
                    {
                        var tabInfo = new CanvasTabInfo
                        {
                            TabHeader = tab.Header?.ToString () ?? "Unnamed",
                            BackgroundColor = tab.Background as SolidColorBrush,
                            Name = tab.Name,
                            Buttons = new List<ButtonInfo> ()
                        };

                        foreach(var child in canvas.Children.OfType<TableButton> ())
                        {
                            if(child == null)
                            {
                                Debug.WriteLine ("---- Warning: child is null");
                                continue;
                            }

                            int id = -1;
                            if(child.Tag != null && int.TryParse (child.Tag.ToString (), out int tagValue))
                                id = tagValue;
                            else
                                Debug.WriteLine ($"---- Warning: Tag is invalid for button {child.TableName}");

                            var borderBrush = child.BorderBrush as SolidColorBrush;
                            if(borderBrush == null)
                                Debug.WriteLine ($"---- Warning: BorderBrush is null for button {child.TableName}");

                            Debug.WriteLine ($"---- Saving button: {child.TableName}, Waiter: {child.Waiter}, Total: {child.Total}, X: {Canvas.GetLeft (child)}, Y: {Canvas.GetTop (child)}");

                            var info = new ButtonInfo
                            {
                                ID = id,
                                X = Canvas.GetLeft (child),
                                Y = Canvas.GetTop (child),
                                Waiter = child.Waiter,
                                WaiterId = child.WaiterId,
                                Width = child.Width,
                                Height = child.Height,
                                Text = child.TableName,
                                ButtonBorderBrush = borderBrush
                            };

                            tabInfo.Buttons.Add (info);
                        }

                        Debug.WriteLine ($"-- Total buttons saved in tab {tab.Header}: {tabInfo.Buttons.Count}");
                        allTabs.Add (tabInfo);
                    }
                    else
                    {
                        Debug.WriteLine ($"-- Warning: Tab content is not Canvas: {tab.Header}");
                    }
                }
            }

            try
            {
                string json = System.Text.Json.JsonSerializer.Serialize (allTabs, options);
                File.WriteAllText ("buttons.json", json);
                Debug.WriteLine ("🔹 Saved buttons.json successfully!");
                Debug.WriteLine (json);
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"❌ Error saving JSON: {ex.Message}");
            }
        }


        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (zauzeteNarudzbe.Count > 0)
            {
                string naslov = "GREŠKA";
                string tekst = $"Imate otvorene narudžbe. " + Environment.NewLine + "Ne možete obrisati sve stolove dok imate otvorenih narudžbi.";
                MyMessageBox myMessageBox1 = new MyMessageBox();
                //myMessageBox1.Owner = this;
                myMessageBox1.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                myMessageBox1.MessageTitle.Text = naslov;
                myMessageBox1.MessageText.Text = tekst;
                myMessageBox1.ShowDialog();
                return;


            }
            YesNoPopup myMessageBox = new YesNoPopup();
            myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            myMessageBox.MessageTitle.Text = "POTVRDA BRISANJA";
            myMessageBox.MessageText.Text = "Da li ste sigurni da želite obrisati sve stolove ?";
            myMessageBox.ShowDialog();
            if (myMessageBox.Kliknuo == "Da")
            {
                if (File.Exists("buttons.json"))
                {
                    File.Delete("buttons.json");
                    if (tabControl.SelectedItem is TabItem selectedTab)
                    {
                        if (selectedTab.Content is Canvas current)
                        {

                            if (current is Canvas canvas)
                            {
                                canvas.Children.Clear();
                                buttons.Clear();
                            }
                        }

                    }

                }

            }
        }

        private void EditText(TableButton button)
        {
            var inputDialog = new MyInputBox
            {
                WindowStyle = WindowStyle.None,
                Owner = MainWindow.Instance,
                Topmost = false,
                ShowActivated = true,
                InputTitle = { Text = "NOVI NAZIV STOLA" },
                InputText = { Text = button.TableName } // Prikaz trenutnog imena
            };

            inputDialog.Closed += (s, e) =>
            {
                if(inputDialog.DialogResult == true)
                {
                    string noviNaziv = inputDialog.InputText.Text;
                    button.TableName = noviNaziv; // odmah update-a UI

                    // Ako koristiš listu ButtonInfo, možeš i nju update-ati:
                    var buttonInfo = buttons.Find (b => b.ID == (int)button.Tag);
                    if(buttonInfo != null)
                        buttonInfo.Text = noviNaziv;
                }
            };

            inputDialog.ShowDialog ();
        }




        private void EditSize(Button button)
        {
            var inputDialog = new SizeInputDialog();
            if (inputDialog.ShowDialog() == true)
            {
                button.Width = Convert.ToDouble(inputDialog.InputTextWidth.Text);
                button.Height = Convert.ToDouble(inputDialog.InputTextHeight.Text);


                var buttonInfo = buttons.Find(b => b.ID == (int)button.Tag);
                if (buttonInfo != null)
                {
                    buttonInfo.Width = Convert.ToDouble(inputDialog.InputTextWidth.Text);
                    buttonInfo.Height = Convert.ToDouble(inputDialog.InputTextHeight.Text);

                }
            }
        }

        private void DeleteButton(TableButton button)
        {
            YesNoPopup myMessageBox = new YesNoPopup();
            myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
           myMessageBox.MessageText.Text = "Da li ste sigurni da želite obrisati " + button.TableName + "?";
            myMessageBox.ShowDialog();
            if (myMessageBox.Kliknuo == "Da")
            {
                var buttonInfo = buttons.Find(b => b.ID == (int)button.Tag);
                if (buttonInfo != null)
                {
                    buttons.Remove(buttonInfo);
                    var current = VisualTreeHelper.GetParent(button);
                    while (current != null && current is not Canvas)
                    {
                        current = VisualTreeHelper.GetParent(current);
                    }
                    if (current is Canvas canvas)
                    {
                        canvas.Children.Remove(button);
                        SaveButton_Click(null, null);
                    }
                }

            }
        }

        public class CanvasTabInfo
        {
            public string TabHeader { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public SolidColorBrush BackgroundColor { get; set; }
            public List<ButtonInfo> Buttons { get; set; } = new();
        }
        public class ButtonInfo
        {
            public int ID { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public string? Text { get; set; }
            public string? Total { get; set; }
            public SolidColorBrush ButtonBorderBrush { get; set; }
            public string? Waiter { get; set; }

            public string? WaiterId { get; set; }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var page = new HomePage ();
            page.DataContext = new HomeViewModel ();
            PageNavigator.NavigateWithFade (page);
        }



        private List<int> zauzeteNarudzbe = new();
        public List<CanvasTabInfo> LoadedSalaConfig { get; set; }
        public HashSet<int> zauzeteNarudzbe2 = new();



        // ------------------ LOAD JSON ------------------
        private void LoadLayoutJson()
        {
            string filePath = "buttons.json";

            Debug.WriteLine ("🔹 Starting LoadLayoutJson...");

            // Ako JSON ne postoji → napravi prazan
            if(!File.Exists (filePath))
            {
                Debug.WriteLine ($"⚠ File '{filePath}' not found. Creating new default layout...");

                LoadedSalaConfig = new List<CanvasTabInfo>
        {
            new CanvasTabInfo
            {
                TabHeader = "Sala 1",
                BackgroundColor = Brushes.OrangeRed,
                Name = $"sala_{Guid.NewGuid():N}",
                Buttons = new List<ButtonInfo>() // Početno prazno
            }
        };

                string defaultJson = System.Text.Json.JsonSerializer.Serialize (
                    LoadedSalaConfig,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Converters = { new SolidColorBrushConverter () }
                    });

                File.WriteAllText (filePath, defaultJson);

                Debug.WriteLine ("✅ Created default buttons.json:");
                Debug.WriteLine (defaultJson);

                return;
            }

            // Ako fajl postoji → pokušaj učitati
            try
            {
                Debug.WriteLine ($"🔹 Loading file '{filePath}'...");

                string json = File.ReadAllText (filePath);
                Debug.WriteLine ("📄 File content:");
                Debug.WriteLine (json);

                var options = new JsonSerializerOptions
                {
                    Converters = { new SolidColorBrushConverter () }
                };

                LoadedSalaConfig = System.Text.Json.JsonSerializer.Deserialize<List<CanvasTabInfo>> (json, options) ?? new List<CanvasTabInfo> ();

                Debug.WriteLine ($"✅ Successfully loaded {LoadedSalaConfig.Count} tab(s).");

                // Debug: prikaz dugmića po tabovima
                foreach(var tab in LoadedSalaConfig)
                {
                    Debug.WriteLine ($"-- Tab: {tab.TabHeader} ({tab.Name}), Background: {tab.BackgroundColor?.ToString () ?? "null"}");
                    Debug.WriteLine ($"-- Buttons count: {tab.Buttons?.Count ?? 0}");

                    if(tab.Buttons != null)
                    {
                        foreach(var btn in tab.Buttons)
                        {
                            Debug.WriteLine ($"---- Button: {btn.Text}, ID: {btn.ID}, X: {btn.X}, Y: {btn.Y}, Width: {btn.Width}, Height: {btn.Height}, Waiter: {btn.Waiter}");
                        }
                    }
                }
            }
            catch(System.Text.Json.JsonException ex)
            {
                Debug.WriteLine ($"❌ JSON parsing error:");
                Debug.WriteLine ($"Message: {ex.Message}");
                Debug.WriteLine ($"Path: {ex.Path}");
                Debug.WriteLine ($"LineNumber: {ex.LineNumber}");
                Debug.WriteLine ($"BytePositionInLine: {ex.BytePositionInLine}");
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"❌ Unexpected error while loading JSON: {ex}");
            }
        }


        private void CreateTabsFromJson()
        {
            Debug.WriteLine ("🔹 Start CreateTabsFromJson");

            if(DataContext is OrdersViewModel viewModel)
            {
                Debug.WriteLine ("✔ DataContext is OrdersViewModel");

                var tabsToRemove = new List<TabItem> ();

                // 1️⃣ Priprema tabova za brisanje
                foreach(var item in tabControl.Items)
                {
                    if(item is TabItem tab && tab.Name?.ToString () != "TabAdd")
                    {
                        Debug.WriteLine ($"➡ Marking tab for removal: {tab.Header} ({tab.Name})");
                        tabsToRemove.Add (tab);
                    }
                }

                // 2️⃣ Brisanje starih tabova
                foreach(var tab in tabsToRemove)
                {
                    Debug.WriteLine ($"⚠ Removing tab: {tab.Header} ({tab.Name})");
                    tabControl.Items.Remove (tab);
                }

                Debug.WriteLine ($"🔹 After removal, tabControl.Items.Count = {tabControl.Items.Count}");

                // 3️⃣ Kreiranje novih tabova iz LoadedSalaConfig
                Debug.WriteLine ($"🔹 LoadedSalaConfig.Count = {LoadedSalaConfig.Count}");
                foreach(var sala in LoadedSalaConfig)
                {
                    Debug.WriteLine ($"-- Creating tab: {sala.TabHeader} ({sala.Name})");

                    var canvas = new Canvas
                    {
                        Background = Brushes.Transparent,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch
                    };

                    canvas.MouseDown += CanvasPanel_MouseDoubleClick;

                    Debug.WriteLine ($"---- Before insert: tabControl.Items.Count = {tabControl.Items.Count}");
                    var tabItem = CreateTab (canvas, sala.Name, sala.TabHeader);
                    tabControl.Items.Insert (tabControl.Items.Count - 1, tabItem);
                    Debug.WriteLine ($"---- After insert: tabControl.Items.Count = {tabControl.Items.Count}");
                }
            }
            else
            {
                Debug.WriteLine ("❌ DataContext is NOT OrdersViewModel");
            }

            tabControl.SelectedIndex = 0;
            Debug.WriteLine ("🔹 Finished CreateTabsFromJson. SelectedIndex set to 0");
        }



        private Button CreateTableButton(ButtonInfo buttonInfo, Canvas canvas)
        {
            var button = new TableButton
            {
                TableName = buttonInfo.Text,
                Waiter = buttonInfo.Waiter,
                WaiterId = buttonInfo.WaiterId,
                BorderBrush = buttonInfo.ButtonBorderBrush,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Tag = buttonInfo.ID
            };

            // Dynamically scale width/height based on Canvas size
            canvas.SizeChanged += (s, e) =>
            {
                double scale = 0.14; // 14% of canvas height
                button.Width = canvas.ActualHeight * scale;
                button.Height = canvas.ActualHeight * scale;
            };

            button.PreviewMouseLeftButtonDown += Button_MouseLeftButtonDown;
            button.PreviewMouseMove += Button_MouseMove;
            button.MouseUp += Button_MouseUp;
            button.MouseRightButtonDown += Button_MouseRightButtonDown;
            button.Click += Button_Click;

            var style = Application.Current.FindResource ("TableButtonStyle") as Style;
            if(style != null)
                button.Style = style;

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center
            };

            var tableTextBlock = new TextBlock
            {
                Name = "TableName",
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                FontSize = 12
            };

            // Bind TextBlock.Text to TableButton.TableName
            tableTextBlock.SetBinding (TextBlock.TextProperty, new System.Windows.Data.Binding (nameof (TableButton.TableName))
            {
                Source = button,
                Mode = System.Windows.Data.BindingMode.TwoWay
            });

            var waiterTextBlock = new TextBlock
            {
                Name = "WaiterName",
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                FontSize = 10,
                Text = button.Waiter
            };

            var totalTextBlock = new TextBlock
            {
                Name = "TotalAmount",
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                FontSize = 10,
                Text = button.Total
            };

            stackPanel.Children.Add (tableTextBlock);
            stackPanel.Children.Add (waiterTextBlock);
            stackPanel.Children.Add (totalTextBlock);

            button.Content = stackPanel;

            return button;
        }





        private void CreateTablesFromJson()
        {
            foreach (var sala in LoadedSalaConfig)
            {
                Debug.WriteLine("  foreach (var sala in LoadedSalaConfig) ------ " + sala.TabHeader);
                var tab = tabControl.Items
                    .OfType<TabItem>()
                    .FirstOrDefault(t => t.Header.ToString() == sala.TabHeader);

                if (tab?.Content is Canvas canvas)
                {
                    foreach (var cfg in sala.Buttons)
                    {
                        Debug.WriteLine(" foreach (var cfg in sala.Buttons) ------ " + cfg.ID);
                        var tableBtn = CreateTableButton(cfg, canvas);
                        canvas.Children.Add(tableBtn);

                        Canvas.SetLeft(tableBtn, cfg.X);
                        Canvas.SetTop(tableBtn, cfg.Y);
                    }
                }
            }
        }
        private async Task UpdateOccupiedTables()
        {
            Debug.WriteLine ("🔹 Start UpdateOccupiedTables");

            using(var db = new AppDbContext ())
            {
                Debug.WriteLine ("✔ Učitavanje zauzetih narudžbi iz baze...");

                // 1️⃣ Dohvati sve stavke narudžbi u memoriju
                var stavke = await db.NarudzbeStavke
                    .Where (x => x.IdNarudzbe != null && x.Sala != null)
                    .ToListAsync ();

                // 2️⃣ Grupiraj po IdNarudzbe i Sala i izračunaj total
                var zauzeti = stavke
                    .GroupBy (x => new { x.IdNarudzbe, x.Sala })
                    .ToDictionary (
                        g => (g.Key.IdNarudzbe.Value, g.Key.Sala),
                        g => new
                        {
                            KonobarId = g.First ().Konobar,
                            Total = g.Sum (x => x.TotalAmount ?? 0) // sada se može jer je u memoriji
                        }
                    );

                Debug.WriteLine ($"✔ Ukupno zauzetih narudžbi: {zauzeti.Count}");

                foreach(var kvp in zauzeti)
                {
                    Debug.WriteLine ($"[Zauzeto] Narudzba:  Sala: {kvp.Key.Sala}, KonobarID: {kvp.Value.KonobarId}, Total: {kvp.Value.Total}");
                }

                // Prolazak kroz tabove i dugmad
                foreach(var tab in tabControl.Items.OfType<TabItem> ())
                {
                    if(tab.Header.ToString () == "+")
                        continue;
                    if(tab.Content is not Canvas canvas)
                        continue;

                    string sala = tab.Name;

                    foreach(var btn in canvas.Children.OfType<TableButton> ())
                    {
                        if(btn.Tag == null)
                            continue;

                        int stolId = (int)btn.Tag;

                        if(zauzeti.TryGetValue ((stolId, sala), out var data))
                        {
                            // Postavljanje modernog crvenog border-a
                            Color modernRed = (Color)ColorConverter.ConvertFromString ("#E53935");
                            btn.BorderBrush = new SolidColorBrush (modernRed);

                            // Dohvati ime konobara
                            string radnik = await db.Radnici
                                                  .Where (r => r.IdRadnika == Convert.ToInt32 (data.KonobarId))
                                                  .Select (r => r.Radnik)
                                                  .FirstOrDefaultAsync ();

                            btn.Waiter = radnik;
                            btn.WaiterId = data.KonobarId;
                            btn.Total = data.Total.ToString ("0.00");

                            // Update TextBlock-a u StackPanelu dugmeta
                            if(btn.Content is StackPanel stack)
                            {
                                var waiterTextBlock = stack.Children
                                    .OfType<TextBlock> ()
                                    .FirstOrDefault (tb => tb.Name == "WaiterName");
                                if(waiterTextBlock != null)
                                    waiterTextBlock.Text = radnik;

                                var totalTextBlock = stack.Children
                                    .OfType<TextBlock> ()
                                    .FirstOrDefault (tb => tb.Name == "TotalAmount");
                                if(totalTextBlock != null)
                                    totalTextBlock.Text = btn.Total;
                            }

                            zauzeteNarudzbe.Add (stolId);
                            Debug.WriteLine ($"✔ Ažuriran stol {stolId} | Konobar: {radnik} | Total: {btn.Total}");
                        }
                        else
                        {
                            Debug.WriteLine ($"[Slobodan] Sala: {sala}, Sto: {stolId}");
                        }
                    }
                }
            }

            Debug.WriteLine ("🔹 Finished UpdateOccupiedTables");
        }




        private void RenameTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem &&
                menuItem.Parent is ContextMenu ctx &&
                ctx.PlacementTarget is TabItem tab &&
                tab.Header.ToString() != "+")
            {
                string oldName = tab.Header.ToString();

             

                var inputDialog = new MyInputBox ();
                
                inputDialog.Owner = MainWindow.Instance;
                inputDialog.Topmost = false;
                inputDialog.ShowActivated = true;
                inputDialog.InputTitle.Text = "NOVI NAZIV SALE";
                inputDialog.Closed += (s, e) =>
                {
                    if (inputDialog.DialogResult == true)
                    {
                        if (!string.IsNullOrWhiteSpace (inputDialog.InputText.Text))
                            tab.Header = inputDialog.InputText.Text;
                    }
                };
                inputDialog.ShowDialog ();
              
            }


        }
         private void DeleteTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem &&
                menuItem.Parent is ContextMenu ctx &&
                ctx.PlacementTarget is TabItem tab &&
                tab.Header.ToString() != "+")
            {
                YesNoPopup myMessageBox = new YesNoPopup ();
                myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                myMessageBox.MessageTitle.Text = "POTVRDA BRISANJA";
                myMessageBox.MessageText.Text = $"Da li ste sigurni da želite obrisati salu \"{tab.Header}\"?";
                myMessageBox.ShowDialog ();
                if (myMessageBox.Kliknuo == "Da")
                {
                      tabControl.Items.Remove(tab);
                }
            }
        }

        private TabItem CreateTab(Canvas canvas, string name, string header)
        {
            if (DataContext is OrdersViewModel viewModel)
            {

                var tab = new TabItem
                {
                    Header = header,
                    Name = name,
                    Width = 150,
                    FontSize = 14,
                    BorderThickness = new Thickness(0),
                    FontWeight = FontWeights.ExtraLight,
                    Padding = new Thickness(5),
                    Content = canvas
                };

                var menuSala = new ContextMenu();
               
                var separatorSala1 = new Separator
                {
                    Height = 1,
                    Margin = new Thickness (5, 2,0,0),
                    Background = Brushes.LightGray
                };
                var renameItemSala = new MenuItem ();
                var stackPanelRenameSala = new StackPanel
                        {
                            Orientation = Orientation.Horizontal
                        };
                var iconSalaRename = new TextBlock
                        {
                            Text = "\uE895", // Edit ikona (hex kod)
                            FontFamily = new FontFamily ("Segoe MDL2 Assets"),
                            FontSize = 16,
                            Margin = new Thickness (0, 0, 8, 0),
                            VerticalAlignment = VerticalAlignment.Center
                        };
                 var textSalaRename = new TextBlock
                        {
                            Text = "Preimenuj salu",
                            VerticalAlignment = VerticalAlignment.Center
                        };
                stackPanelRenameSala.Children.Add (iconSalaRename);
                stackPanelRenameSala.Children.Add (textSalaRename);
                renameItemSala.Header = stackPanelRenameSala;
                renameItemSala.Click += RenameTab_Click;

                var deleteItemSala = new MenuItem ();
                var stackPanelDeleteSala = new StackPanel
                {
                    Orientation = Orientation.Horizontal
                };
                var iconSala = new TextBlock
                {
                    Text = "\uE74D", // Edit ikona (hex kod)
                    FontFamily = new FontFamily ("Segoe MDL2 Assets"),
                    FontSize = 16,
                    Margin = new Thickness (0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                var textSala = new TextBlock
                {
                    Text = "Obriši salu",
                    VerticalAlignment = VerticalAlignment.Center
                };
                stackPanelDeleteSala.Children.Add (iconSala);
                stackPanelDeleteSala.Children.Add (textSala);
                deleteItemSala.Header = stackPanelDeleteSala;
                deleteItemSala.Click += DeleteTab_Click;

                menuSala.Items.Add(renameItemSala);
                menuSala.Items.Add (separatorSala1);
                menuSala.Items.Add(deleteItemSala);
                tab.ContextMenu = menuSala;

                return tab;
            }
            else
            {
                return null;
            }
        }

        private string GenerateNextTabName()
        {
            var existingNames = tabControl.Items.Cast<TabItem>()
                .Select(t => t.Header.ToString())
                .ToList();

            // Pronađi sve postojeće brojeve sala
            var existingNumbers = new List<int>();

            foreach (var name in existingNames)
            {
                if (name.StartsWith("Sala "))
                {
                    // Pokušaj izvući broj
                    if (int.TryParse(name.Substring(5), out int number))
                    {
                        existingNumbers.Add(number);
                    }
                }
            }

            // Ako nema postojećih tabova, kreni od 1
            if (!existingNumbers.Any())
                return "Sala 1";

            // Pronađi sljedeći broj (najveći postojeći + 1)
            int nextNumber = existingNumbers.Max() + 1;
            return $"Sala {nextNumber}";
        }

        private string GetRandomColor(List<Color> usedColors)
        {
            var random = new Random();
            var allColors = new List<string>
                    {
                        "#c95c02", "#701634", "#164570", "#167040", "#8A84E2",
                        "#3B0D11", "#2274A5", "#D90368", "#0E402D", "#F1C40F",
                        "#E71D36", "#2EC4B6", "#FF9F1C", "#011627", "#8B5FBF"
                    };

            var availableColors = allColors.Where(c =>
                !usedColors.Any(used => used == (Color)ColorConverter.ConvertFromString(c))
            ).ToList();

            if (availableColors.Any())
                return availableColors[random.Next(availableColors.Count)];

            return allColors[random.Next(allColors.Count)];
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //File.Delete("buttons.json");
            LoadLayoutJson();        // 1) učitaj JSON
            CreateTabsFromJson();    // 2) stvori tabove
            CreateTablesFromJson();  // 3) stvori stolove
            await UpdateOccupiedTables(); // 4) oboji zauzete

            isInitialized = true;
        }




    }
}
