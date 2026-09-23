using Caupo.Data;
using Caupo.Helpers;
using Caupo.Models;
using Caupo.Services;
using Caupo.UserControls;
using Caupo.ViewModels;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for OrdersPage.xaml
    /// </summary>
    public partial class OrdersPage : UserControl
    {

        #region Fields

        private TableButton? draggedButton;
        private bool isInitialized;
        private bool _hasUnsavedChanges;
        private bool _isCreatingRoom;
        private readonly OrdersViewModel ordersViewModel;


        private Popup? tableFloatingPopup;
        private TableFloatingMenuBar? tableFloatingMenu;
        private TableButton? floatingMenuTable;

        private Popup? tableOrderPopup;
        private TableOrderMenuBar? tableOrderMenu;
        private TableButton? orderMenuTable;
        private bool _isSelectingMoveTarget;
        private int? _moveSourceTableId;
        private string? _moveSourceRoom;
        private string? _moveSourceTableName;

        private bool _isSelectingMergeTarget;
        private int? _mergeSourceTableId;
        private string? _mergeSourceRoom;
        private string? _mergeSourceTableName;
        private string? _mergeSourceWaiterId;

        private CancellationTokenSource? longPressCancellation;
        private TableButton? longPressButton;
        private Point longPressStartPoint;
        private bool longPressTriggered;
        private const int LongPressMilliseconds = 700;

        private Popup? roomFloatingPopup;
        private RoomFloatingMenuBar? roomFloatingMenu;
        private TabItem? floatingMenuRoom;

        #endregion

        #region Constructor

        public OrdersPage(ObservableCollection<RacunStavka>? stavkeRacuna = null)
        {
            ordersViewModel = new OrdersViewModel(stavkeRacuna);
            DataContext = ordersViewModel;

            InitializeComponent();
            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik.Radnik;
            Unloaded += OrdersPage_Unloaded;
        }

        #endregion

        #region Layout Edit Mode

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
                CloseTableOrderMenu();
            }
        }

        #endregion

        #region Rooms - Tabs

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

        #endregion

        #region Tables - Create and Layout

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

        private TableButton CreateTableButton(ButtonInfo buttonInfo)
        {
            var button = new TableButton
            {
                TableName = buttonInfo.Text,
                Width = buttonInfo.Width,
                Height = buttonInfo.Height,
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

        private int GenerateTableId()
        {
            int id = Environment.TickCount & int.MaxValue;

            if (id == 0)
                id = 1;

            var usedIds = tabControl.Items
                .OfType<TabItem>()
                .Where(tab => tab.Content is Canvas)
                .SelectMany(tab => ((Canvas)tab.Content).Children.OfType<TableButton>())
                .Select(button => int.TryParse(button.Tag?.ToString(), out int existingId) ? existingId : 0)
                .Where(existingId => existingId > 0)
                .ToHashSet();

            while (usedIds.Contains(id))
                id = id == int.MaxValue ? 1 : id + 1;

            return id;
        }

        private string GenerateTableName(Canvas canvas)
        {
            int number = 1;

            var existingNames = canvas.Children
                .OfType<TableButton>()
                .Select(button => button.TableName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            while (existingNames.Contains($"STO{number}"))
                number++;

            return $"STO{number}";
        }

        private void AddTable(Canvas canvas, Point position)
        {


            var button = new TableButton
            {
                TableName = GenerateTableName(canvas),
                Waiter = null,
                WaiterId = null,
                Total = null,
                Height = 120,
                Width = 120,
                Tag = GenerateTableId(),
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

        #endregion

        #region Table Layout Floating Menu

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
            return button.IsOccupied;
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

        #endregion

        #region Room Floating Menu

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
            return tab.Content is Canvas canvas && canvas.Children.OfType<TableButton>().Any(b => b.IsOccupied);
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

        #endregion

        #region Table Order Floating Menu

        private void Button_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TableButton button)
                return;

            e.Handled = true;

            if (_isLayoutEditMode)
            {
                OpenTableFloatingMenu(button);
                return;
            }

            if (!button.IsOccupied)
                return;

            string trenutniKonobarId = Globals.ulogovaniKorisnik.IdRadnika.ToString();

            if (!string.IsNullOrWhiteSpace(button.WaiterId) && button.WaiterId != trenutniKonobarId)
            {
                ShowMessage("GREŠKA", $"Niste kreirali ovu narudžbu.{Environment.NewLine}{button.TableName} ne pripada Vama.");
                return;
            }

            OpenTableOrderMenu(button);
        }

        private void Button_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private async void OpenTableOrderMenu(TableButton button)
        {
            CloseTableFloatingMenu();
            CloseRoomFloatingMenu();
            CloseTableOrderMenu();

            orderMenuTable = button;
            tableOrderMenu = new TableOrderMenuBar();
            tableOrderMenu.SetTable(button.TableName ?? "Sto");

            try
            {
                await using var db = new AppDbContext();

                var waiters = await db.Radnici
                    .Where(x => x.IdRadnika != Globals.ulogovaniKorisnik.IdRadnika)
                    .OrderBy(x => x.Radnik)
                    .ToListAsync();

                tableOrderMenu.SetWaiters(waiters);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ORDERS] Greška pri učitavanju konobara: " + ex);
                ShowMessage("GREŠKA", "Nije moguće učitati listu konobara.");
                CloseTableOrderMenu();
                return;
            }

            tableOrderMenu.MoveTableRequested += TableOrderMenu_MoveTableRequested;
            tableOrderMenu.MergeTablesRequested += TableOrderMenu_MergeTablesRequested;
            tableOrderMenu.ChangeWaiterStarted += TableOrderMenu_ChangeWaiterStarted;
            tableOrderMenu.ChangeWaiterCancelled += TableOrderMenu_ChangeWaiterCancelled;
            tableOrderMenu.ChangeWaiterRequested += TableOrderMenu_ChangeWaiterRequested;
            tableOrderMenu.FinishRequested += TableOrderMenu_FinishRequested;

            OrderMenuOverlay.Visibility = Visibility.Visible;

            tableOrderPopup = new Popup
            {
                Child = tableOrderMenu,
                PlacementTarget = button,
                Placement = PlacementMode.Right,
                HorizontalOffset = 6,
                VerticalOffset = 0,
                StaysOpen = true,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade
            };

            tableOrderPopup.Closed += TableOrderPopup_Closed;
            tableOrderPopup.IsOpen = true;
        }

        private void TableOrderMenu_FinishRequested(object? sender, EventArgs e)
        {
            CancelChangeWaiterStatus();
            CloseTableOrderMenu();
        }

        private void CloseTableOrderMenu()
        {
            OrderMenuOverlay.Visibility = Visibility.Collapsed;

            if (tableOrderPopup != null)
            {
                tableOrderPopup.Closed -= TableOrderPopup_Closed;
                tableOrderPopup.IsOpen = false;
            }

            DetachTableOrderMenuEvents();
            tableOrderPopup = null;
            tableOrderMenu = null;
            orderMenuTable = null;
        }

        private void TableOrderPopup_Closed(object? sender, EventArgs e)
        {
            OrderMenuOverlay.Visibility = Visibility.Collapsed;
            DetachTableOrderMenuEvents();
            tableOrderPopup = null;
            tableOrderMenu = null;
            orderMenuTable = null;
        }

        private void DetachTableOrderMenuEvents()
        {
            if (tableOrderMenu == null)
                return;

            tableOrderMenu.MoveTableRequested -= TableOrderMenu_MoveTableRequested;
            tableOrderMenu.MergeTablesRequested -= TableOrderMenu_MergeTablesRequested;
            tableOrderMenu.ChangeWaiterStarted -= TableOrderMenu_ChangeWaiterStarted;
            tableOrderMenu.ChangeWaiterCancelled -= TableOrderMenu_ChangeWaiterCancelled;
            tableOrderMenu.ChangeWaiterRequested -= TableOrderMenu_ChangeWaiterRequested;
            tableOrderMenu.FinishRequested -= TableOrderMenu_FinishRequested;
        }

        #endregion

        #region Change Waiter

        private void TableOrderMenu_ChangeWaiterStarted(object? sender, EventArgs e)
        {
            MoveTableStatusText.Text = "Prebacujete narudžbu drugom konobaru";
            MoveTableStatusBar.Visibility = Visibility.Visible;
        }

        private void TableOrderMenu_ChangeWaiterCancelled(object? sender, EventArgs e)
        {
            CancelChangeWaiterStatus();
        }

        private async void TableOrderMenu_ChangeWaiterRequested(object? sender, WaiterSelectedEventArgs e)
        {
            if (orderMenuTable == null)
                return;

            if (!int.TryParse(orderMenuTable.Tag?.ToString(), out int tableId))
            {
                ShowMessage("GREŠKA", "Sto nema ispravan identifikator.");
                return;
            }

            var tab = FindParentTab(orderMenuTable);

            if (tab == null || string.IsNullOrWhiteSpace(tab.Name))
            {
                ShowMessage("GREŠKA", "Nije moguće odrediti salu kojoj sto pripada.");
                return;
            }

            string room = tab.Name;
            string tableName = orderMenuTable.TableName ?? "Sto";

            CloseTableOrderMenu();

            bool authenticated = await AuthenticateWaiterTransferAsync(e.WaiterId, e.WaiterName);

            if (!authenticated)
            {
                CancelChangeWaiterStatus();
                return;
            }

            try
            {
                await using var db = new AppDbContext();

                var stavke = await db.NarudzbeStavke
                    .Where(x => x.IdNarudzbe == tableId && x.Sala == room)
                    .ToListAsync();

                if (stavke.Count == 0)
                {
                    ShowMessage("GREŠKA", "Narudžba više ne postoji.");
                    CancelChangeWaiterStatus();
                    await UpdateOccupiedTables();
                    return;
                }

                string targetWaiterId = e.WaiterId.ToString();

                foreach (var stavka in stavke)
                    stavka.Konobar = targetWaiterId;

                await db.SaveChangesAsync();

                DatabaseBackupService.StartBackup(Globals.CurrentDbPath);

                CancelChangeWaiterStatus();
                await UpdateOccupiedTables();

                ShowMessage("OBAVJEŠTENJE", $"{tableName} je uspješno prebačen konobaru {e.WaiterName}.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ORDERS] Greška pri promjeni konobara: " + ex);
                CancelChangeWaiterStatus();
                ShowMessage("GREŠKA", $"Narudžbu nije moguće prebaciti drugom konobaru:{Environment.NewLine}{ex.Message}");
            }
        }

        private async Task<bool> AuthenticateWaiterTransferAsync(int waiterId, string waiterName)
        {
            const int maxPokusaja = 3;
            int pokusaj = 0;

            while (pokusaj < maxPokusaja)
            {
                var input = new MyInputBox
                {
                    Owner = Window.GetWindow(this)
                };

                input.InputTitle.Text = $"Unesite PIN konobara {waiterName}";

                if (input.ShowDialog() != true)
                    return false;

                string pin = input.result?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(pin))
                    continue;

                await using var db = new AppDbContext();

                bool valid = await db.Radnici.AnyAsync(x => x.IdRadnika == waiterId && x.Lozinka == pin);

                if (valid)
                    return true;

                pokusaj++;

                int preostalo = maxPokusaja - pokusaj;

                if (preostalo == 0)
                {
                    ShowMessage(
                        "GREŠKA",
                        $"Pogrešan PIN.{Environment.NewLine}Nemate dozvolu za prebacivanje narudžbe drugom konobaru.");

                    CancelChangeWaiterStatus();
                    PageNavigator.NavigateWithFade(new HomePage());

                    return false;
                }

                ShowMessage(
                    "GREŠKA",
                    $"Pogrešan PIN.{Environment.NewLine}Preostalo pokušaja: {preostalo}.");
            }

            return false;
        }

        private void CancelChangeWaiterStatus()
        {
            MoveTableStatusBar.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Merge Tables

        private void TableOrderMenu_MergeTablesRequested(object? sender, EventArgs e)
        {
            if (orderMenuTable == null)
                return;

            if (!int.TryParse(orderMenuTable.Tag?.ToString(), out int sourceTableId))
            {
                ShowMessage("GREŠKA", "Sto nema ispravan identifikator.");
                return;
            }

            var sourceTab = FindParentTab(orderMenuTable);

            if (sourceTab == null || string.IsNullOrWhiteSpace(sourceTab.Name))
            {
                ShowMessage("GREŠKA", "Nije moguće odrediti salu kojoj sto pripada.");
                return;
            }

            if (string.IsNullOrWhiteSpace(orderMenuTable.WaiterId))
            {
                ShowMessage("GREŠKA", "Nije moguće odrediti konobara koji je otvorio narudžbu.");
                return;
            }

            _mergeSourceTableId = sourceTableId;
            _mergeSourceRoom = sourceTab.Name;
            _mergeSourceTableName = orderMenuTable.TableName;
            _mergeSourceWaiterId = orderMenuTable.WaiterId;
            _isSelectingMergeTarget = true;

            CloseTableOrderMenu();

            MoveTableStatusText.Text = $"Spajate {_mergeSourceTableName} sa — odaberite drugi sto";
            MoveTableStatusBar.Visibility = Visibility.Visible;
        }

        private async Task MergeTablesAsync(TableButton targetButton)
        {
            if (!_isSelectingMergeTarget ||
                _mergeSourceTableId == null ||
                string.IsNullOrWhiteSpace(_mergeSourceRoom) ||
                string.IsNullOrWhiteSpace(_mergeSourceWaiterId))
                return;

            if (!int.TryParse(targetButton.Tag?.ToString(), out int targetTableId))
            {
                ShowMessage("GREŠKA", "Odabrani sto nema ispravan identifikator.");
                return;
            }

            var targetTab = FindParentTab(targetButton);

            if (targetTab == null || string.IsNullOrWhiteSpace(targetTab.Name))
            {
                ShowMessage("GREŠKA", "Nije moguće odrediti salu odabranog stola.");
                return;
            }

            string targetRoom = targetTab.Name;
            string targetTableName = targetButton.TableName ?? "Sto";

            if (targetTableId == _mergeSourceTableId.Value && targetRoom == _mergeSourceRoom)
            {
                ShowMessage("GREŠKA", "Odaberite drugi sto.");
                return;
            }

            if (!targetButton.IsOccupied)
            {
                ShowMessage("GREŠKA", $"{targetTableName} nije zauzet. Za spajanje odaberite zauzeti sto.");
                return;
            }

            if (string.IsNullOrWhiteSpace(targetButton.WaiterId))
            {
                ShowMessage("GREŠKA", $"Nije moguće odrediti konobara za {targetTableName}.");
                return;
            }

            string targetWaiterId = targetButton.WaiterId;

            try
            {
                await using var db = new AppDbContext();

                var targetStavke = await db.NarudzbeStavke
                    .Where(x => x.IdNarudzbe == targetTableId && x.Sala == targetRoom)
                    .ToListAsync();

                if (targetStavke.Count == 0)
                {
                    ShowMessage("GREŠKA", $"{targetTableName} više nema otvorenu narudžbu.");
                    CancelMergeTableSelection();
                    await UpdateOccupiedTables();
                    return;
                }

                var sourceStavke = await db.NarudzbeStavke
                    .Where(x => x.IdNarudzbe == _mergeSourceTableId.Value && x.Sala == _mergeSourceRoom)
                    .ToListAsync();

                if (sourceStavke.Count == 0)
                {
                    ShowMessage("GREŠKA", "Narudžba koju želite spojiti više ne postoji.");
                    CancelMergeTableSelection();
                    await UpdateOccupiedTables();
                    return;
                }

                if (_mergeSourceWaiterId != targetWaiterId)
                {
                    bool authenticated = await AuthenticateTargetWaiterAsync(targetWaiterId, targetButton.Waiter);

                    if (!authenticated)
                        return;
                }

                foreach (var stavka in sourceStavke)
                {
                    stavka.IdNarudzbe = targetTableId;
                    stavka.Sala = targetRoom;
                    stavka.Konobar = targetWaiterId;
                }

                await db.SaveChangesAsync();

                DatabaseBackupService.StartBackup(Globals.CurrentDbPath);

                CancelMergeTableSelection();

                await UpdateOccupiedTables();

                ordersViewModel.IdStola = targetTableId;
                ordersViewModel.Sala = targetRoom;
                ordersViewModel.ImeStola = targetTableName;

                var page = new OrderPage(ordersViewModel.IdStola, ordersViewModel.ImeStola, ordersViewModel.Sala, ordersViewModel.StavkeRacuna, false);
                PageNavigator.NavigateWithFade(page);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ORDERS] Greška pri spajanju stolova: " + ex);
                ShowMessage("GREŠKA", $"Narudžbe nije moguće spojiti:{Environment.NewLine}{ex.Message}");
            }
        }

        private async Task<bool> AuthenticateTargetWaiterAsync(string targetWaiterId, string? targetWaiterName)
        {
            if (!int.TryParse(targetWaiterId, out int idRadnika))
            {
                ShowMessage("GREŠKA", "Konobar ciljnog stola nema ispravan identifikator.");
                return false;
            }

            const int maxPokusaja = 3;
            int pokusaj = 0;

            while (pokusaj < maxPokusaja)
            {
                var input = new MyInputBox
                {
                    Owner = Window.GetWindow(this)
                };

                input.InputTitle.Text = string.IsNullOrWhiteSpace(targetWaiterName)
                    ? "Unesite PIN konobara ciljnog stola"
                    : $"Unesite PIN konobara {targetWaiterName}";

                if (input.ShowDialog() != true)
                    return false;

                string pin = input.result?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(pin))
                    continue;

                await using var db = new AppDbContext();

                bool valid = await db.Radnici.AnyAsync(x => x.IdRadnika == idRadnika && x.Lozinka == pin);

                if (valid)
                    return true;

                pokusaj++;

                int preostalo = maxPokusaja - pokusaj;

                if (preostalo == 0)
                {
                    ShowMessage(
                        "GREŠKA",
                        $"Pogrešan PIN.{Environment.NewLine}Nemate dozvolu za spajanje ovih stolova.");

                    CancelMergeTableSelection();

                    PageNavigator.NavigateWithFade(new HomePage());

                    return false;
                }
                ShowMessage(
                    "GREŠKA",
                    $"Pogrešan PIN.{Environment.NewLine}Preostalo pokušaja: {preostalo}.");
            }

            return false;
        }

        private void CancelMergeTableSelection()
        {
            _isSelectingMergeTarget = false;
            _mergeSourceTableId = null;
            _mergeSourceRoom = null;
            _mergeSourceTableName = null;
            _mergeSourceWaiterId = null;

            MoveTableStatusBar.Visibility = Visibility.Collapsed;
        }

        #endregion


        #region Move Order To Another Table

        private void TableOrderMenu_MoveTableRequested(object? sender, EventArgs e)
        {
            if (orderMenuTable == null)
                return;

            if (!int.TryParse(orderMenuTable.Tag?.ToString(), out int sourceTableId))
            {
                ShowMessage("GREŠKA", "Sto nema ispravan identifikator.");
                return;
            }

            var sourceTab = FindParentTab(orderMenuTable);

            if (sourceTab == null || string.IsNullOrWhiteSpace(sourceTab.Name))
            {
                ShowMessage("GREŠKA", "Nije moguće odrediti salu kojoj sto pripada.");
                return;
            }

            _moveSourceTableId = sourceTableId;
            _moveSourceRoom = sourceTab.Name;
            _moveSourceTableName = orderMenuTable.TableName;
            _isSelectingMoveTarget = true;

            CloseTableOrderMenu();

            MoveTableStatusText.Text = $"Prebacujete narudžbu sa {_moveSourceTableName} — odaberite novi sto";
            MoveTableStatusBar.Visibility = Visibility.Visible;
        }

        private async Task MoveOrderToTableAsync(TableButton targetButton)
        {
            if (!_isSelectingMoveTarget || _moveSourceTableId == null || string.IsNullOrWhiteSpace(_moveSourceRoom))
                return;

            if (!int.TryParse(targetButton.Tag?.ToString(), out int targetTableId))
            {
                ShowMessage("GREŠKA", "Odabrani sto nema ispravan identifikator.");
                return;
            }

            var targetTab = FindParentTab(targetButton);

            if (targetTab == null || string.IsNullOrWhiteSpace(targetTab.Name))
            {
                ShowMessage("GREŠKA", "Nije moguće odrediti salu odabranog stola.");
                return;
            }

            string targetRoom = targetTab.Name;
            string targetTableName = targetButton.TableName ?? "Sto";

            if (targetTableId == _moveSourceTableId.Value && targetRoom == _moveSourceRoom)
            {
                ShowMessage("GREŠKA", "Odaberite drugi sto.");
                return;
            }

            if (targetButton.IsOccupied)
            {
                ShowMessage("GREŠKA", $"{targetTableName} je zauzet.");
                return;
            }

            try
            {
                await using var db = new AppDbContext();

                bool targetOccupied = await db.NarudzbeStavke
                    .AnyAsync(x => x.IdNarudzbe == targetTableId && x.Sala == targetRoom);

                if (targetOccupied)
                {
                    ShowMessage("GREŠKA", $"{targetTableName} je u međuvremenu zauzet.");
                    CancelMoveTableSelection();
                    await UpdateOccupiedTables();
                    return;
                }

                var stavke = await db.NarudzbeStavke
                    .Where(x => x.IdNarudzbe == _moveSourceTableId.Value && x.Sala == _moveSourceRoom)
                    .ToListAsync();

                if (stavke.Count == 0)
                {
                    ShowMessage("GREŠKA", "Narudžba više ne postoji.");
                    CancelMoveTableSelection();
                    await UpdateOccupiedTables();
                    return;
                }

                foreach (var stavka in stavke)
                {
                    stavka.IdNarudzbe = targetTableId;
                    stavka.Sala = targetRoom;
                }

                await db.SaveChangesAsync();

                DatabaseBackupService.StartBackup(Globals.CurrentDbPath);

                CancelMoveTableSelection();

                await UpdateOccupiedTables();

                ordersViewModel.IdStola = targetTableId;
                ordersViewModel.Sala = targetRoom;
                ordersViewModel.ImeStola = targetTableName;

                var page = new OrderPage(ordersViewModel.IdStola, ordersViewModel.ImeStola, ordersViewModel.Sala, ordersViewModel.StavkeRacuna, false);
                PageNavigator.NavigateWithFade(page);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ORDERS] Greška pri promjeni stola: " + ex);
                ShowMessage("GREŠKA", $"Narudžbu nije moguće prebaciti na drugi sto:{Environment.NewLine}{ex.Message}");
            }
        }

        private void CancelMoveTableSelection()
        {
            _isSelectingMoveTarget = false;
            _moveSourceTableId = null;
            _moveSourceRoom = null;
            _moveSourceTableName = null;

            MoveTableStatusBar.Visibility = Visibility.Collapsed;
        }

        private void CancelMoveTableButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isSelectingMergeTarget)
            {
                CancelMergeTableSelection();
                return;
            }

            if (_isSelectingMoveTarget)
            {
                CancelMoveTableSelection();
                return;
            }

            CancelChangeWaiterStatus();
            CloseTableOrderMenu();
        }

        #endregion

        #region Table Long Press

        private void StartLongPress(TableButton button, MouseButtonEventArgs e)
        {
            CancelLongPress();

            if (!button.IsOccupied)
                return;

            longPressButton = button;
            longPressStartPoint = e.GetPosition(button);
            longPressTriggered = false;

            longPressCancellation = new CancellationTokenSource();
            _ = WaitForLongPressAsync(button, longPressCancellation.Token);
        }

        private async Task WaitForLongPressAsync(TableButton button, CancellationToken token)
        {
            try
            {
                await Task.Delay(LongPressMilliseconds, token);

                if (token.IsCancellationRequested || longPressButton != button)
                    return;

                longPressTriggered = true;

                await Dispatcher.InvokeAsync(() =>
                {
                    string trenutniKonobarId = Globals.ulogovaniKorisnik.IdRadnika.ToString();

                    if (!string.IsNullOrWhiteSpace(button.WaiterId) && button.WaiterId != trenutniKonobarId)
                    {
                        ShowMessage("GREŠKA", $"Niste kreirali ovu narudžbu.{Environment.NewLine}{button.TableName} ne pripada Vama.");
                        return;
                    }

                    OpenTableOrderMenu(button);
                });
            }
            catch (TaskCanceledException)
            {
            }
        }

        private void CheckLongPressMovement(TableButton button, MouseEventArgs e)
        {
            if (longPressButton != button || longPressCancellation == null)
                return;

            Point current = e.GetPosition(button);

            if (Math.Abs(current.X - longPressStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(current.Y - longPressStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                CancelLongPress();
            }
        }

        private void CancelLongPress()
        {
            longPressCancellation?.Cancel();
            longPressCancellation?.Dispose();
            longPressCancellation = null;
            longPressButton = null;
        }

        #endregion

        #region Table Mouse and Drag

        private Point dragStartPoint;
        private bool isDragging;

        private void Button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TableButton button)
                return;

            if (!_isLayoutEditMode)
            {
                if (_isSelectingMoveTarget || _isSelectingMergeTarget)
                    return;

                string trenutniKonobarId = Globals.ulogovaniKorisnik.IdRadnika.ToString();

                if (!string.IsNullOrWhiteSpace(button.WaiterId) && button.WaiterId != trenutniKonobarId)
                    return;

                StartLongPress(button, e);
                return;
            }

            draggedButton = button;

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
            if (!_isLayoutEditMode && sender is TableButton button)
            {
                CheckLongPressMovement(button, e);
                return;
            }

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
            if (!_isLayoutEditMode)
            {
                CancelLongPress();

                if (longPressTriggered)
                {
                    e.Handled = true;

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        longPressTriggered = false;
                    }), System.Windows.Threading.DispatcherPriority.Input);
                }

                return;
            }

            if (draggedButton == null)
                return;

            bool wasDragging = isDragging;

            draggedButton.ReleaseMouseCapture();
            draggedButton = null;
            isDragging = false;

            if (wasDragging)
                _hasUnsavedChanges = true;
        }

        #endregion

        #region Open Table

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

        private async void Button_Click(object sender, RoutedEventArgs e)
        {

            Debug.WriteLine($"[ORDERS] Button_Click {DateTime.Now:HH:mm:ss.fff}");
            if (longPressTriggered)
            {
                longPressTriggered = false;
                return;
            }

            if (isDragging || sender is not TableButton button)
                return;

            if (_isSelectingMoveTarget)
            {
                await MoveOrderToTableAsync(button);
                return;
            }

            if (_isSelectingMergeTarget)
            {
                await MergeTablesAsync(button);
                return;
            }

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

        #endregion

        #region Messages

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

        #endregion

        #region Layout Save and Load

        public List<CanvasTabInfo> LoadedSalaConfig { get; set; } = [];

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Effect = new BlurEffect { Radius = 8 };

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var allTabs = new List<CanvasTabInfo>();

                foreach (var item in tabControl.Items)
                {
                    if (item is not TabItem tab || tab.Name == "TabAdd" || tab.Content is not Canvas canvas)
                        continue;

                    var tabInfo = new CanvasTabInfo
                    {
                        TabHeader = tab.Header?.ToString() ?? "Sala",
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
                            Width = child.Width,
                            Height = child.Height,
                            Text = child.TableName,
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
                        Name = $"sala_{Guid.NewGuid():N}",
                        Buttons = new List<ButtonInfo>()
                    }
                };

                string defaultJson = JsonSerializer.Serialize(
                    LoadedSalaConfig,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
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



                LoadedSalaConfig = JsonSerializer.Deserialize<List<CanvasTabInfo>>(json) ?? new List<CanvasTabInfo>();

                Debug.WriteLine($"✅ Successfully loaded {LoadedSalaConfig.Count} tab(s).");

                foreach (var tab in LoadedSalaConfig)
                {
                    Debug.WriteLine($"-- Tab: {tab.TabHeader} ({tab.Name})");
                    Debug.WriteLine($"-- Buttons count: {tab.Buttons?.Count ?? 0}");

                    if (tab.Buttons != null)
                    {
                        foreach (var btn in tab.Buttons)
                            Debug.WriteLine($"---- Button: {btn.Text}, ID: {btn.ID}, X: {btn.X}, Y: {btn.Y}, Width: {btn.Width}, Height: {btn.Height}");
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

        public class CanvasTabInfo
        {
            public string TabHeader { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;

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

        }

        #endregion

        #region Delete Layout

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Effect = new BlurEffect { Radius = 8 };

            try
            {
                var zauzeti = await ordersViewModel.LoadOccupiedTablesAsync();

                if (zauzeti.Count > 0)
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
            catch (Exception ex)
            {
                Debug.WriteLine("[ORDERS] Greška pri provjeri narudžbi prije brisanja svih stolova i sala: " + ex);
                ShowMessage("GREŠKA", $"Nije moguće provjeriti otvorene narudžbe:{Environment.NewLine}{ex.Message}");
            }
            finally
            {
                MainContent.Effect = null;
            }
        }

        #endregion

        #region Occupied Tables

        private async Task UpdateOccupiedTables()
        {
            var zauzeti = await ordersViewModel.LoadOccupiedTablesAsync();
            var mapa = zauzeti.ToDictionary(x => (x.IdStola, x.Sala));




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



                }
            }
        }

        #endregion

        #region Navigation

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

            CancelLongPress();
            CloseTableOrderMenu();
            CloseTableFloatingMenu();
            CloseRoomFloatingMenu();

            var page = new HomePage();
            page.DataContext = new HomeViewModel();
            PageNavigator.NavigateWithFade(page);
        }

        #endregion

        #region Page Lifecycle

        private void OrdersPage_Unloaded(object sender, RoutedEventArgs e)
        {
            CancelLongPress();
            CancelMoveTableSelection();
            CancelMergeTableSelection();
            CancelChangeWaiterStatus();
            CloseTableOrderMenu();
            CloseTableFloatingMenu();
            CloseRoomFloatingMenu();
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

        #endregion

    }
}
