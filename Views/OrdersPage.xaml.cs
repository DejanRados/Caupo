using Caupo.Data;
using Caupo.Helpers;
using Caupo.Models;
using Caupo.UserControls;
using Caupo.ViewModels;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for OrdersPage.xaml
    /// </summary>
    public partial class OrdersPage : UserControl
    {
        

        private TableButton? draggedButton;
        private Point clickPosition;
        private bool isInitialized;
        private bool _hasUnsavedChanges;
        private bool _isCreatingRoom;
        private readonly OrdersViewModel ordersViewModel;
        

        private Popup? tableFloatingPopup;
        private TableFloatingMenuBar? tableFloatingMenu;
        private TableButton? floatingMenuTable;

        private Popup? roomFloatingPopup;
        private RoomFloatingMenuBar? roomFloatingMenu;
        private TabItem? floatingMenuRoom;

        public OrdersPage(ObservableCollection<RacunStavka>? stavkeRacuna = null)
        {
            ordersViewModel = new OrdersViewModel(stavkeRacuna);
            DataContext = ordersViewModel;

            InitializeComponent();
            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik.Radnik;
        }

        private bool _isLayoutEditMode;

        private void EditLayoutButton_Click(object sender, RoutedEventArgs e)
        {
            SetLayoutEditMode(true);
        }

        private void SetLayoutEditMode(bool enabled)
        {
            _isLayoutEditMode = enabled;

            EditLayoutButton.Visibility = enabled ? Visibility.Collapsed : Visibility.Visible;
            LayoutEditPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            //TabAdd.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;

            if (!enabled)
            {
                CloseTableFloatingMenu();
                CloseRoomFloatingMenu();
            }
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized || _isCreatingRoom)
                return;

            if (tabControl.SelectedItem is not TabItem selectedTab || selectedTab.Name != "TabAdd")
                return;

            if (!_isLayoutEditMode)
            {
                var firstRoom = tabControl.Items
                    .OfType<TabItem>()
                    .FirstOrDefault(t => t.Name != "TabAdd");

                if (firstRoom != null)
                    tabControl.SelectedItem = firstRoom;

                return;
            }

            try
            {
                _isCreatingRoom = true;

                var canvas = new Canvas
                {
                    Background = Brushes.Transparent,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                canvas.MouseDown += CanvasPanel_MouseDoubleClick;

                string header = GenerateNextTabName();
                string name = $"sala_{Guid.NewGuid():N}";
                var newTab = CreateTab(canvas, name, header);

                tabControl.Items.Insert(tabControl.Items.Count - 1, newTab);
                _hasUnsavedChanges = true;

                tabControl.Dispatcher.BeginInvoke(new Action(() =>
                {
                    tabControl.SelectedItem = newTab;
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            finally
            {
                _isCreatingRoom = false;
            }
        }

        private void CanvasPanel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!_isLayoutEditMode)
                return;

            if (e.ClickCount != 2 || sender is not Canvas canvas)
                return;

            draggedButton = null;

            Point position = e.GetPosition(canvas);
            AddTable(canvas, position);
        }

        private void Button_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isLayoutEditMode)
                return;

            if (sender is not TableButton button)
                return;

            e.Handled = true;
            OpenTableFloatingMenu(button);
        }

        private void Button_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void OpenTableFloatingMenu(TableButton button)
        {
            CloseRoomFloatingMenu();
            CloseTableFloatingMenu();

            floatingMenuTable = button;
            tableFloatingMenu = new TableFloatingMenuBar();
            tableFloatingMenu.SetTable(button.TableName ?? "Sto", button.Width, button.Height, !IsTableOccupied(button));
            tableFloatingMenu.NameSaved += TableFloatingMenu_NameSaved;
            tableFloatingMenu.SizeSaved += TableFloatingMenu_SizeSaved;
            tableFloatingMenu.DeleteConfirmed += TableFloatingMenu_DeleteConfirmed;
            tableFloatingMenu.FinishRequested += TableFloatingMenu_FinishRequested;

            tableFloatingPopup = new Popup
            {
                Child = tableFloatingMenu,
                PlacementTarget = button,
                Placement = PlacementMode.Right,
                HorizontalOffset = 6,
                VerticalOffset = 0,
                StaysOpen = true,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade
            };

            tableFloatingPopup.Closed += TableFloatingPopup_Closed;
            tableFloatingPopup.IsOpen = true;
        }

        private bool IsTableOccupied(TableButton button)
        {
            return int.TryParse(button.Tag?.ToString(), out int id) && zauzeteNarudzbe.Contains(id);
        }

        private void TableFloatingMenu_NameSaved(object? sender, string newName)
        {
            if (floatingMenuTable == null)
                return;

            floatingMenuTable.TableName = newName;
            _hasUnsavedChanges = true;
        }

        private void TableFloatingMenu_SizeSaved(object? sender, TableSizeChangedEventArgs e)
        {
            if (floatingMenuTable == null)
                return;

            floatingMenuTable.Width = e.Width;
            floatingMenuTable.Height = e.Height;
            _hasUnsavedChanges = true;
        }

        private void TableFloatingMenu_DeleteConfirmed(object? sender, EventArgs e)
        {
            var button = floatingMenuTable;

            if (button == null)
                return;

            if (IsTableOccupied(button))
                return;

            var current = VisualTreeHelper.GetParent(button);

            while (current != null && current is not Canvas)
                current = VisualTreeHelper.GetParent(current);

            if (current is Canvas canvas)
                canvas.Children.Remove(button);

            _hasUnsavedChanges = true;
            CloseTableFloatingMenu();
        }

        private void TableFloatingMenu_FinishRequested(object? sender, EventArgs e)
        {
            CloseTableFloatingMenu();
        }

        private void CloseTableFloatingMenu()
        {
            if (tableFloatingPopup != null)
            {
                tableFloatingPopup.Closed -= TableFloatingPopup_Closed;
                tableFloatingPopup.IsOpen = false;
            }

            DetachTableFloatingMenuEvents();
            tableFloatingPopup = null;
            tableFloatingMenu = null;
            floatingMenuTable = null;
        }

        private void TableFloatingPopup_Closed(object? sender, EventArgs e)
        {
            DetachTableFloatingMenuEvents();
            tableFloatingPopup = null;
            tableFloatingMenu = null;
            floatingMenuTable = null;
        }

        private void DetachTableFloatingMenuEvents()
        {
            if (tableFloatingMenu == null)
                return;

            tableFloatingMenu.NameSaved -= TableFloatingMenu_NameSaved;
            tableFloatingMenu.SizeSaved -= TableFloatingMenu_SizeSaved;
            tableFloatingMenu.DeleteConfirmed -= TableFloatingMenu_DeleteConfirmed;
            tableFloatingMenu.FinishRequested -= TableFloatingMenu_FinishRequested;
        }

        private Point dragStartPoint;
        private bool isDragging;

        private void Button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isLayoutEditMode)
                return;

            draggedButton = sender as TableButton;

            if (draggedButton == null)
                return;

            var current = VisualTreeHelper.GetParent(draggedButton);

            while (current != null && current is not Canvas)
                current = VisualTreeHelper.GetParent(current);

            if (current is Canvas canvas)
            {
                dragStartPoint = e.GetPosition(canvas);
                isDragging = false;
                draggedButton.CaptureMouse();
                e.Handled = true;
            }
        }

        private void Button_MouseMove(object sender, MouseEventArgs e)
        {
            if (draggedButton != null && e.LeftButton == MouseButtonState.Pressed)
            {
                var current = VisualTreeHelper.GetParent(draggedButton);

                while (current != null && current is not Canvas)
                    current = VisualTreeHelper.GetParent(current);

                if (current is Canvas canvas)
                {
                    Point currentPosition = e.GetPosition(canvas);

                    if (!isDragging &&
                        (Math.Abs(currentPosition.X - dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                         Math.Abs(currentPosition.Y - dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance))
                    {
                        isDragging = true;
                    }

                    if (isDragging)
                    {
                        double newLeft = currentPosition.X - draggedButton.ActualWidth / 2;
                        double newTop = currentPosition.Y - draggedButton.ActualHeight / 2;

                        newLeft = Math.Max(0, newLeft);
                        newTop = Math.Max(0, newTop);
                        newLeft = Math.Min(canvas.ActualWidth - draggedButton.ActualWidth, newLeft);
                        newTop = Math.Min(canvas.ActualHeight - draggedButton.ActualHeight, newTop);

                        Canvas.SetLeft(draggedButton, newLeft);
                        Canvas.SetTop(draggedButton, newTop);
                    }
                }
            }
        }

        private void Button_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (draggedButton == null)
                return;

            bool wasDragging = isDragging;

            draggedButton.ReleaseMouseCapture();
            draggedButton = null;
            isDragging = false;

            if (wasDragging)
                _hasUnsavedChanges = true;
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
            if (isDragging || sender is not TableButton button)
                return;

            if (!int.TryParse(button.Tag?.ToString(), out int idStola))
            {
                ShowMessage("GREŠKA", "Sto nema ispravan identifikator.");
                return;
            }

            var tab = FindParentTab(button);

            if (tab == null || string.IsNullOrWhiteSpace(tab.Name))
            {
                ShowMessage("GREŠKA", "Nije moguće odrediti salu kojoj sto pripada.");
                return;
            }

            string trenutniKonobarId = Globals.ulogovaniKorisnik.IdRadnika.ToString();

            if (!string.IsNullOrWhiteSpace(button.WaiterId) && button.WaiterId != trenutniKonobarId)
            {
                ShowMessage("GREŠKA", $"Niste kreirali ovu narudžbu.{Environment.NewLine}{button.TableName} ne pripada Vama.");
                return;
            }

            if (DataContext is not OrdersViewModel viewModel)
                return;

            viewModel.IdStola = idStola;
            viewModel.Sala = tab.Name;
            viewModel.ImeStola = button.TableName;

            bool hasNewItems = viewModel.StavkeRacuna.Count > 0;

            var page = new OrderPage(viewModel.IdStola, viewModel.ImeStola, viewModel.Sala, viewModel.StavkeRacuna, hasNewItems);

            PageNavigator.NavigateWithFade(page);
        }

        private void ShowMessage(string title, string message)
        {
            MainContent.Effect = new BlurEffect { Radius = 8 };

            try
            {
                var dialog = new MyMessageBox
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                dialog.MessageTitle.Text = title;
                dialog.MessageText.Text = message;
                dialog.ShowDialog();
            }
            finally
            {
                MainContent.Effect = null;
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
            MainContent.Effect = new BlurEffect { Radius = 8 };

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new SolidColorBrushConverter(), new ImageBrushConverter() }
                };

                var allTabs = new List<CanvasTabInfo>();

                foreach (var item in tabControl.Items)
                {
                    if (item is not TabItem tab || tab.Name == "TabAdd" || tab.Content is not Canvas canvas)
                        continue;

                    var tabInfo = new CanvasTabInfo
                    {
                        TabHeader = tab.Header?.ToString() ?? "Sala",
                        BackgroundColor = tab.Background as SolidColorBrush,
                        Name = tab.Name,
                        Buttons = []
                    };

                    foreach (var child in canvas.Children.OfType<TableButton>())
                    {
                        if (!int.TryParse(child.Tag?.ToString(), out int id))
                            throw new InvalidDataException($"Sto \"{child.TableName}\" nema ispravan identifikator.");

                        double x = Canvas.GetLeft(child);
                        double y = Canvas.GetTop(child);

                        if (double.IsNaN(x))
                            x = 0;

                        if (double.IsNaN(y))
                            y = 0;

                        tabInfo.Buttons.Add(new ButtonInfo
                        {
                            ID = id,
                            X = x,
                            Y = y,
                            Waiter = child.Waiter,
                            WaiterId = child.WaiterId,
                            Width = child.Width,
                            Height = child.Height,
                            Text = child.TableName,
                            Total = child.Total,
                            ButtonBorderBrush = child.BorderBrush as SolidColorBrush
                        });
                    }

                    allTabs.Add(tabInfo);
                }

                string json = JsonSerializer.Serialize(allTabs, options);
                string filePath = Path.GetFullPath("buttons.json");
                string tempPath = filePath + ".tmp";
                string backupPath = filePath + ".bak";

                File.WriteAllText(tempPath, json);

                if (File.Exists(filePath))
                    File.Replace(tempPath, filePath, backupPath, true);
                else
                    File.Move(tempPath, filePath);
                _hasUnsavedChanges = false;
                SetLayoutEditMode(false);
                ShowMessage("OBAVJEŠTENJE", "Uspješno ste spremili raspored stolova.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ORDERS] Greška pri spremanju rasporeda: " + ex);
                ShowMessage("GREŠKA", $"Raspored stolova nije spremljen:{Environment.NewLine}{ex.Message}");
            }
            finally
            {
                MainContent.Effect = null;
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Effect = new BlurEffect { Radius = 8 };

            try
            {
                if (zauzeteNarudzbe.Count > 0)
                {
                    var dialog = new MyMessageBox
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    };

                    dialog.MessageTitle.Text = "GREŠKA";
                    dialog.MessageText.Text = $"Imate otvorene narudžbe.{Environment.NewLine}Ne možete obrisati sve stolove dok imate otvorenih narudžbi.";
                    dialog.ShowDialog();
                    return;
                }

                var confirm = new YesNoPopup
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    PopupTitle = "POTVRDA BRISANJA",
                    PopupMessage = "Da li ste sigurni da želite obrisati sve stolove i sale?",
                    ConfirmText = "Obriši",
                    CancelText = "Odustani"
                };

                confirm.ShowDialog();

                if (confirm.Kliknuo != "Da")
                    return;

                try
                {
                    _isCreatingRoom = true;

                    var tabsToRemove = tabControl.Items
                        .OfType<TabItem>()
                        .Where(t => t.Name != "TabAdd")
                        .ToList();

                    foreach (var tab in tabsToRemove)
                        tabControl.Items.Remove(tab);

                    var canvas = new Canvas
                    {
                        Background = Brushes.Transparent,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch
                    };

                    canvas.MouseDown += CanvasPanel_MouseDoubleClick;

                    string name = $"sala_{Guid.NewGuid():N}";
                    var newTab = CreateTab(canvas, name, "Sala 1");

                    tabControl.Items.Insert(0, newTab);
                    tabControl.SelectedItem = newTab;

                    _hasUnsavedChanges = true;
                }
                finally
                {
                    _isCreatingRoom = false;
                }
            }
            finally
            {
                MainContent.Effect = null;
            }
        }



        public class CanvasTabInfo
        {
            public string TabHeader { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public SolidColorBrush? BackgroundColor { get; set; }
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
            public SolidColorBrush? ButtonBorderBrush { get; set; }
            public string? Waiter { get; set; }
            public string? WaiterId { get; set; }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                MainContent.Effect = new BlurEffect { Radius = 8 };

                var dialog = new YesNoPopup
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    PopupTitle = "NESPREMLJENE PROMJENE",
                    PopupMessage = "Imate nespremljene promjene rasporeda. Želite li ih spremiti prije izlaska?",
                    ConfirmText = "Spremi",
                    CancelText = "Odustani"
                };

                dialog.ShowDialog();
                MainContent.Effect = null;

                if (dialog.Kliknuo == "Da")
                {
                    SaveButton_Click(sender, e);

                    if (_hasUnsavedChanges)
                        return;
                }
            }

            var page = new HomePage();
            page.DataContext = new HomeViewModel();
            PageNavigator.NavigateWithFade(page);
        }

        private List<int> zauzeteNarudzbe = new();
        public List<CanvasTabInfo> LoadedSalaConfig { get; set; } = [];
        public HashSet<int> zauzeteNarudzbe2 = new();

        private void LoadLayoutJson()
        {
            string filePath = "buttons.json";

            Debug.WriteLine("🔹 Starting LoadLayoutJson...");

            if (!File.Exists(filePath))
            {
                Debug.WriteLine($"⚠ File '{filePath}' not found. Creating new default layout...");

                LoadedSalaConfig = new List<CanvasTabInfo>
                {
                    new CanvasTabInfo
                    {
                        TabHeader = "Sala 1",
                        BackgroundColor = Brushes.OrangeRed,
                        Name = $"sala_{Guid.NewGuid():N}",
                        Buttons = new List<ButtonInfo>()
                    }
                };

                string defaultJson = System.Text.Json.JsonSerializer.Serialize(
                    LoadedSalaConfig,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Converters = { new SolidColorBrushConverter() }
                    });

                File.WriteAllText(filePath, defaultJson);

                Debug.WriteLine("✅ Created default buttons.json:");
                Debug.WriteLine(defaultJson);
                return;
            }

            try
            {
                Debug.WriteLine($"🔹 Loading file '{filePath}'...");

                string json = File.ReadAllText(filePath);
                Debug.WriteLine("📄 File content:");
                Debug.WriteLine(json);

                var options = new JsonSerializerOptions
                {
                    Converters = { new SolidColorBrushConverter() }
                };

                LoadedSalaConfig = System.Text.Json.JsonSerializer.Deserialize<List<CanvasTabInfo>>(json, options) ?? new List<CanvasTabInfo>();

                Debug.WriteLine($"✅ Successfully loaded {LoadedSalaConfig.Count} tab(s).");

                foreach (var tab in LoadedSalaConfig)
                {
                    Debug.WriteLine($"-- Tab: {tab.TabHeader} ({tab.Name}), Background: {tab.BackgroundColor?.ToString() ?? "null"}");
                    Debug.WriteLine($"-- Buttons count: {tab.Buttons?.Count ?? 0}");

                    if (tab.Buttons != null)
                    {
                        foreach (var btn in tab.Buttons)
                            Debug.WriteLine($"---- Button: {btn.Text}, ID: {btn.ID}, X: {btn.X}, Y: {btn.Y}, Width: {btn.Width}, Height: {btn.Height}, Waiter: {btn.Waiter}");
                    }
                }
            }
            catch (JsonException ex)
            {
                LoadedSalaConfig = [];
                Debug.WriteLine("[ORDERS] Neispravan buttons.json: " + ex);
                throw new InvalidDataException("Raspored stolova je oštećen ili nije ispravan JSON.", ex);
            }
            catch (Exception ex)
            {
                LoadedSalaConfig = [];
                Debug.WriteLine("[ORDERS] Greška pri učitavanju buttons.json: " + ex);
                throw;
            }
        }

        private void CreateTabsFromJson()
        {
            Debug.WriteLine("🔹 Start CreateTabsFromJson");

            if (DataContext is OrdersViewModel viewModel)
            {
                Debug.WriteLine("✔ DataContext is OrdersViewModel");

                var tabsToRemove = new List<TabItem>();

                foreach (var item in tabControl.Items)
                {
                    if (item is TabItem tab && tab.Name?.ToString() != "TabAdd")
                    {
                        Debug.WriteLine($"➡ Marking tab for removal: {tab.Header} ({tab.Name})");
                        tabsToRemove.Add(tab);
                    }
                }

                foreach (var tab in tabsToRemove)
                {
                    Debug.WriteLine($"⚠ Removing tab: {tab.Header} ({tab.Name})");
                    tabControl.Items.Remove(tab);
                }

                Debug.WriteLine($"🔹 After removal, tabControl.Items.Count = {tabControl.Items.Count}");
                Debug.WriteLine($"🔹 LoadedSalaConfig.Count = {LoadedSalaConfig.Count}");

                foreach (var sala in LoadedSalaConfig)
                {
                    Debug.WriteLine($"-- Creating tab: {sala.TabHeader} ({sala.Name})");

                    var canvas = new Canvas
                    {
                        Background = Brushes.Transparent,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch
                    };

                    canvas.MouseDown += CanvasPanel_MouseDoubleClick;

                    Debug.WriteLine($"---- Before insert: tabControl.Items.Count = {tabControl.Items.Count}");
                    var tabItem = CreateTab(canvas, sala.Name, sala.TabHeader);
                    tabControl.Items.Insert(tabControl.Items.Count - 1, tabItem);
                    Debug.WriteLine($"---- After insert: tabControl.Items.Count = {tabControl.Items.Count}");
                }
            }
            else
            {
                Debug.WriteLine(" DataContext is NOT OrdersViewModel");
            }

            tabControl.SelectedIndex = 0;
            Debug.WriteLine("🔹 Finished CreateTabsFromJson. SelectedIndex set to 0");
        }

        private TableButton CreateTableButton(ButtonInfo buttonInfo)
        {
            var button = new TableButton
            {
                TableName = buttonInfo.Text,
                Waiter = buttonInfo.Waiter,
                WaiterId = buttonInfo.WaiterId,
                Total = buttonInfo.Total,
                Width = buttonInfo.Width,
                Height = buttonInfo.Height,
                BorderBrush = buttonInfo.ButtonBorderBrush,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Tag = buttonInfo.ID
            };

            button.PreviewMouseLeftButtonDown += Button_MouseLeftButtonDown;
            button.PreviewMouseMove += Button_MouseMove;
            button.MouseUp += Button_MouseUp;
            button.MouseRightButtonDown += Button_MouseRightButtonDown;
            button.PreviewMouseRightButtonUp += Button_PreviewMouseRightButtonUp;
            button.Click += Button_Click;

            if (Application.Current.TryFindResource("TableButtonStyle") is Style style)
                button.Style = style;

            return button;
        }

        private void CreateTablesFromJson()
        {
            foreach (var sala in LoadedSalaConfig)
            {
                Debug.WriteLine("  foreach (var sala in LoadedSalaConfig) ------ " + sala.TabHeader);

                var tab = tabControl.Items
                    .OfType<TabItem>()
                    .FirstOrDefault(t => t.Name == sala.Name);

                if (tab?.Content is Canvas canvas)
                {
                    foreach (var cfg in sala.Buttons)
                    {
                        Debug.WriteLine(" foreach (var cfg in sala.Buttons) ------ " + cfg.ID);
                        var tableBtn = CreateTableButton(cfg);
                        canvas.Children.Add(tableBtn);
                        Canvas.SetLeft(tableBtn, cfg.X);
                        Canvas.SetTop(tableBtn, cfg.Y);
                    }
                }
            }
        }

        private async Task UpdateOccupiedTables()
        {
            var zauzeti = await ordersViewModel.LoadOccupiedTablesAsync();
            var mapa = zauzeti.ToDictionary(x => (x.IdStola, x.Sala));

            zauzeteNarudzbe.Clear();
            zauzeteNarudzbe2.Clear();

            foreach (var tab in tabControl.Items.OfType<TabItem>())
            {
                if (tab.Name == "TabAdd" || tab.Content is not Canvas canvas)
                    continue;

                foreach (var btn in canvas.Children.OfType<TableButton>())
                {
                    if (!int.TryParse(btn.Tag?.ToString(), out int stolId))
                        continue;

                    // Uvijek prvo potpuno resetuj runtime stanje stola
                    btn.Waiter = null;
                    btn.WaiterId = null;
                    btn.Total = null;
                    btn.IsOccupied = false;
                    btn.ClearValue(Border.BorderBrushProperty);

                    if (Application.Current.TryFindResource("TableButtonStyle") is Style style)
                        btn.Style = style;

                    // Tek onda trenutno stanje iz baze
                    if (!mapa.TryGetValue((stolId, tab.Name), out var data))
                        continue;

                    btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935"));
                    btn.Waiter = data.Konobar;
                    btn.WaiterId = data.KonobarId;
                    btn.Total = data.Total.ToString("0.00");
                    btn.IsOccupied = true;

                    zauzeteNarudzbe.Add(stolId);
                    zauzeteNarudzbe2.Add(stolId);
                }
            }
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;

                child = VisualTreeHelper.GetParent(child);
            }

            return null;
        }
        private void TabControl_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isLayoutEditMode)
                return;

            var tab = FindVisualParent<TabItem>(e.OriginalSource as DependencyObject);

            if (tab == null || tab.Name == "TabAdd")
                return;

            e.Handled = true;
            OpenRoomFloatingMenu(tab);
        }

        private void Tab_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void OpenRoomFloatingMenu(TabItem tab)
        {
            CloseTableFloatingMenu();
            CloseRoomFloatingMenu();

            floatingMenuRoom = tab;
            roomFloatingMenu = new RoomFloatingMenuBar();
            roomFloatingMenu.SetRoom(tab.Header?.ToString() ?? "Sala", !RoomHasOccupiedTables(tab));
            roomFloatingMenu.NameSaved += RoomFloatingMenu_NameSaved;
            roomFloatingMenu.DeleteConfirmed += RoomFloatingMenu_DeleteConfirmed;
            roomFloatingMenu.FinishRequested += RoomFloatingMenu_FinishRequested;

            roomFloatingPopup = new Popup
            {
                Child = roomFloatingMenu,
                PlacementTarget = tab,
                Placement = PlacementMode.Bottom,
                HorizontalOffset = 0,
                VerticalOffset = 6,
                StaysOpen = true,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade
            };

            roomFloatingPopup.Closed += RoomFloatingPopup_Closed;
            roomFloatingPopup.IsOpen = true;
        }

        private bool RoomHasOccupiedTables(TabItem tab)
        {
            return tab.Content is Canvas canvas && canvas.Children.OfType<TableButton>().Any(b => int.TryParse(b.Tag?.ToString(), out int id) && zauzeteNarudzbe.Contains(id));
        }

        private void RoomFloatingMenu_NameSaved(object? sender, string newName)
        {
            if (floatingMenuRoom == null)
                return;

            floatingMenuRoom.Header = newName;
            _hasUnsavedChanges = true;
        }

        private async void RoomFloatingMenu_DeleteConfirmed(object? sender, EventArgs e)
        {
            var tab = floatingMenuRoom;

            if (tab == null)
                return;

            try
            {
                var zauzeti = await ordersViewModel.LoadOccupiedTablesAsync();
                bool hasOpenOrders = zauzeti.Any(x => x.Sala == tab.Name);

                if (hasOpenOrders)
                {
                    ShowMessage("GREŠKA", "Sala sadrži otvorene narudžbe i ne može biti obrisana.");
                    await UpdateOccupiedTables();
                    return;
                }

                CloseRoomFloatingMenu();
                tabControl.Items.Remove(tab);
                _hasUnsavedChanges = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ORDERS] Greška pri provjeri narudžbi prije brisanja sale: " + ex);
                ShowMessage("GREŠKA", $"Nije moguće provjeriti otvorene narudžbe:{Environment.NewLine}{ex.Message}");
            }
        }

        private void RoomFloatingMenu_FinishRequested(object? sender, EventArgs e)
        {
            CloseRoomFloatingMenu();
        }

        private void CloseRoomFloatingMenu()
        {
            if (roomFloatingPopup != null)
            {
                roomFloatingPopup.Closed -= RoomFloatingPopup_Closed;
                roomFloatingPopup.IsOpen = false;
            }

            DetachRoomFloatingMenuEvents();
            roomFloatingPopup = null;
            roomFloatingMenu = null;
            floatingMenuRoom = null;
        }

        private void RoomFloatingPopup_Closed(object? sender, EventArgs e)
        {
            DetachRoomFloatingMenuEvents();
            roomFloatingPopup = null;
            roomFloatingMenu = null;
            floatingMenuRoom = null;
        }

        private void DetachRoomFloatingMenuEvents()
        {
            if (roomFloatingMenu == null)
                return;

            roomFloatingMenu.NameSaved -= RoomFloatingMenu_NameSaved;
            roomFloatingMenu.DeleteConfirmed -= RoomFloatingMenu_DeleteConfirmed;
            roomFloatingMenu.FinishRequested -= RoomFloatingMenu_FinishRequested;
        }



        private TabItem CreateTab(Canvas canvas, string name, string header)
        {
            var tab = new TabItem
            {
                Header = header,
                Name = name,
                Width = 150,
                FontSize = 14,
                BorderThickness = new Thickness(1),
                FontWeight = FontWeights.ExtraLight,
                Padding = new Thickness(5),
                Content = canvas
            };

            tab.MouseRightButtonDown += TabControl_PreviewMouseRightButtonDown;
            tab.PreviewMouseRightButtonUp += Tab_PreviewMouseRightButtonUp;
            return tab;
        }

        private string GenerateNextTabName()
        {
            var existingNames = tabControl.Items.Cast<TabItem>().Select(t => t.Header.ToString()).ToList();
            var existingNumbers = new List<int>();

            foreach (var name in existingNames)
            {
                if (name.StartsWith("Sala "))
                {
                    if (int.TryParse(name.Substring(5), out int number))
                        existingNumbers.Add(number);
                }
            }

            if (!existingNumbers.Any())
                return "Sala 1";

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

            var availableColors = allColors.Where(c => !usedColors.Any(used => used == (Color)ColorConverter.ConvertFromString(c))).ToList();

            if (availableColors.Any())
                return availableColors[random.Next(availableColors.Count)];

            return allColors[random.Next(allColors.Count)];
        }
        private void AddTableButton_Click(object sender, RoutedEventArgs e)
        {
            if (tabControl.SelectedItem is not TabItem tab || tab.Name == "TabAdd" || tab.Content is not Canvas canvas)
            {
                ShowMessage("GREŠKA", "Odaberite salu u koju želite dodati sto.");
                return;
            }

            Point position = FindFreeTablePosition(canvas, 120, 120);
            AddTable(canvas, position);
        }

        private void AddTable(Canvas canvas, Point position)
        {
            int buttonCount = canvas.Children.OfType<TableButton>().Count();

            var button = new TableButton
            {
                TableName = "STO" + (buttonCount + 1),
                Waiter = null,
                WaiterId = null,
                Total = null,
                Height = 120,
                Width = 120,
                Tag = Environment.TickCount,
                IsHitTestVisible = true
            };

            button.PreviewMouseLeftButtonDown += Button_MouseLeftButtonDown;
            button.PreviewMouseMove += Button_MouseMove;
            button.MouseUp += Button_MouseUp;
            button.MouseRightButtonDown += Button_MouseRightButtonDown;
            button.PreviewMouseRightButtonUp += Button_PreviewMouseRightButtonUp;
            button.Click += Button_Click;

            if (Application.Current.TryFindResource("TableButtonStyle") is Style style)
                button.Style = style;

            Canvas.SetLeft(button, position.X);
            Canvas.SetTop(button, position.Y);
            canvas.Children.Add(button);

            _hasUnsavedChanges = true;
        }

        private Point FindFreeTablePosition(Canvas canvas, double width, double height)
        {
            const double margin = 20;
            const double spacing = 15;

            double availableWidth = canvas.ActualWidth;

            if (availableWidth <= 0)
                availableWidth = 1000;

            for (double y = margin; y < Math.Max(canvas.ActualHeight, 700); y += height + spacing)
            {
                for (double x = margin; x + width <= availableWidth; x += width + spacing)
                {
                    bool occupied = canvas.Children
                        .OfType<TableButton>()
                        .Any(button =>
                        {
                            double left = Canvas.GetLeft(button);
                            double top = Canvas.GetTop(button);

                            if (double.IsNaN(left))
                                left = 0;

                            if (double.IsNaN(top))
                                top = 0;

                            return x < left + button.Width + spacing &&
                                   x + width + spacing > left &&
                                   y < top + button.Height + spacing &&
                                   y + height + spacing > top;
                        });

                    if (!occupied)
                        return new Point(x, y);
                }
            }

            return new Point(margin, margin);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!isInitialized)
                {
                    LoadLayoutJson();
                    CreateTabsFromJson();
                    CreateTablesFromJson();

                    _hasUnsavedChanges = false;
                    isInitialized = true;

                    SetLayoutEditMode(false);
                }

                await UpdateOccupiedTables();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ORDERS] Greška pri učitavanju OrdersPage: " + ex);
                ShowMessage("GREŠKA", $"Nije moguće učitati stolove i narudžbe:{Environment.NewLine}{ex.Message}");
            }
        }
    }
}
