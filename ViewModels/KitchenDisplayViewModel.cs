using Caupo.Data;
using Caupo.Server;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Caupo.ViewModels
{
    public class KitchenDisplayViewModel : INotifyPropertyChanged
    {
        public class OrderItem : INotifyPropertyChanged
        {
            public int IdStavke { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Note { get; set; } = string.Empty;
            public bool Visible => !string.IsNullOrWhiteSpace(Note);
            public decimal? Quantity { get; set; }

            private bool _isFinished;
            public bool IsFinished
            {
                get => _isFinished;
                set
                {
                    if (_isFinished == value) return;
                    _isFinished = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanFinish));
                }
            }

            private bool _isBusy;
            public bool IsBusy
            {
                get => _isBusy;
                set
                {
                    if (_isBusy == value) return;
                    _isBusy = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanFinish));
                }
            }

            private bool _isKeyboardSelected;
            public bool IsKeyboardSelected
            {
                get => _isKeyboardSelected;
                set
                {
                    if (_isKeyboardSelected == value) return;
                    _isKeyboardSelected = value;
                    OnPropertyChanged();
                }
            }

            public bool CanFinish => !IsFinished && !IsBusy && IdStavke > 0;

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged([CallerMemberName] string? name = null) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public class DisplayOrder : INotifyPropertyChanged
        {
            public int Number { get; set; }
            public string Waiter { get; set; } = string.Empty;
            public string TableName { get; set; } = string.Empty;
            public DateTime OrderTime { get; set; }
            public ObservableCollection<OrderItem> Items { get; set; } = new();

            public string ColorGreenBackgroundHex { get; set; } = "#1E3A2B";
            public string ColorGreenBorderHex { get; set; } = "#2F9E44";
            public string ColorYellowBackgroundHex { get; set; } = "#EDB705";
            public string ColorYellowBorderHex { get; set; } = "#BA8F02";
            public string ColorRedBackgroundHex { get; set; } = "#3A1E1E";
            public string ColorRedBorderHex { get; set; } = "#E03131";

            private TimeSpan _elapsed;
            public TimeSpan Elapsed
            {
                get => _elapsed;
                set
                {
                    var oldState = ColorState;
                    _elapsed = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ElapsedText));

                    if (oldState != ColorState)
                    {
                        OnPropertyChanged(nameof(Background));
                        OnPropertyChanged(nameof(Border));
                        OnPropertyChanged(nameof(HeaderBackground));
                        OnPropertyChanged(nameof(HeaderBorder));
                        OnPropertyChanged(nameof(ItemBackground));
                        OnPropertyChanged(nameof(ItemBorder));
                    }
                }
            }

            private int ColorState =>
                Elapsed.TotalMinutes >= 20 ? 2 :
                Elapsed.TotalMinutes >= 10 ? 1 : 0;

            public string ElapsedText =>
                $"{(int)Math.Max(0, Elapsed.TotalHours):00}:{Elapsed.Minutes:00}:{Elapsed.Seconds:00}";

            private static Brush MakeBrush(string hex) =>
                new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

            public Brush Background => MakeBrush(ColorState switch
            {
                2 => ColorRedBackgroundHex,
                1 => ColorYellowBackgroundHex,
                _ => ColorGreenBackgroundHex
            });

            public Brush Border => MakeBrush(ColorState switch
            {
                2 => ColorRedBorderHex,
                1 => ColorYellowBorderHex,
                _ => ColorGreenBorderHex
            });

           

            public Brush HeaderBackground => MakeBrush(ColorState switch
            {
                2 => "#482626",
                1 => "#D6A505",
                _ => "#294B37"
            });

            public Brush HeaderBorder => MakeBrush(ColorState switch
            {
                2 => "#874343",
                1 => "#BA8F02",
                _ => "#3D7950"
            });

            public Brush ItemBackground => MakeBrush(ColorState switch
            {
                2 => "#512B2B",
                1 => "#D6A505",
                _ => "#30583F"
            });

            public Brush ItemBorder => MakeBrush(ColorState switch
            {
                2 => "#975050",
                1 => "#BA8F02",
                _ => "#4B8860"
            });

            private bool _isBusy;
            public bool IsBusy
            {
                get => _isBusy;
                set
                {
                    if (_isBusy == value) return;
                    _isBusy = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanFinish));

                    foreach (var item in Items)
                        item.IsBusy = value;
                }
            }

            private bool _isKeyboardSelected;
            public bool IsKeyboardSelected
            {
                get => _isKeyboardSelected;
                set
                {
                    if (_isKeyboardSelected == value) return;
                    _isKeyboardSelected = value;
                    OnPropertyChanged();
                }
            }

            public bool CanFinish => !IsBusy;

            private bool _isNote;
            public bool IsNote
            {
                get => _isNote;
                set
                {
                    if (_isNote == value) return;
                    _isNote = value;
                    OnPropertyChanged();
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Komande su jedinstvena ulazna tačka za tastaturu, miš i touch.
        private sealed class KitchenCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Predicate<object?> _canExecute;

            public KitchenCommand(Action<object?> execute, Predicate<object?> canExecute)
            {
                _execute = execute;
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter) => _canExecute(parameter);

            public void Execute(object? parameter)
            {
                if (CanExecute(parameter))
                    _execute(parameter);
            }

            public event EventHandler? CanExecuteChanged;

            public void Refresh() =>
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public ICommand PreviousOrderCommand { get; }
        public ICommand NextOrderCommand { get; }
        public ICommand PreviousItemCommand { get; }
        public ICommand NextItemCommand { get; }
        public ICommand FinishSelectionCommand { get; }
        public ICommand FinishOrderCommand { get; }
        public ICommand FinishItemCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand ConfirmNewOrderCommand { get; }

        private readonly List<KitchenCommand> _commands = new();

        private KitchenCommand Register(
            Action<object?> execute,
            Predicate<object?> canExecute)
        {
            var command = new KitchenCommand(execute, canExecute);
            _commands.Add(command);
            return command;
        }

        private void RefreshCommands()
        {
            foreach (var command in _commands)
                command.Refresh();
        }

        public ObservableCollection<DisplayOrder> Orders { get; set; } = new();

        // Nove narudžbe čekaju F12; potvrđene se prikazuju hronološki.
        private readonly Queue<DisplayOrder> _pendingOrders = new();
        private readonly HashSet<int> _pendingOrderNumbers = new();
        private bool _initialOrdersLoaded;

        // Historija posljednjih 10 završavanja, bez obzira na F5, miš ili touch.
        private readonly Stack<(DisplayOrder Order, List<int> ItemIds)> _undoHistory = new();

        public event EventHandler? PendingOrdersChanged;

        public DisplayOrder? PendingOrder =>
            _pendingOrders.Count == 0 ? null : _pendingOrders.Peek();

        public bool HasPendingOrders => _pendingOrders.Count > 0;

        public bool CanUndo =>
            _undoHistory.Count > 0 && !HasPendingOrders && !_undoInProgress;

        private void NotifyPendingOrdersChanged()
        {
            OnPropertyChanged(nameof(PendingOrder));
            OnPropertyChanged(nameof(HasPendingOrders));
            OnPropertyChanged(nameof(CanUndo));
            PendingOrdersChanged?.Invoke(this, EventArgs.Empty);
            RefreshCommands();
        }

        private void InsertOrderSorted(DisplayOrder order)
        {
            if (Orders.Any(o => o.Number == order.Number))
                return;

            int index = 0;

            while (index < Orders.Count &&
                   (Orders[index].OrderTime < order.OrderTime ||
                    (Orders[index].OrderTime == order.OrderTime &&
                     Orders[index].Number < order.Number)))
            {
                index++;
            }

            Orders.Insert(index, order);
            RefreshCommands();
        }

        public bool ConfirmPendingOrder()
        {
            Application.Current.Dispatcher.VerifyAccess();

            if (_pendingOrders.Count == 0)
                return false;

            var order = _pendingOrders.Dequeue();
            _pendingOrderNumbers.Remove(order.Number);

            InsertOrderSorted(order);
            NotifyPendingOrdersChanged();

            return true;
        }

        private void RecordUndo(DisplayOrder order, List<int> ids)
        {
            if (ids.Count == 0)
                return;

            _undoHistory.Push((order, ids));
            OnPropertyChanged(nameof(CanUndo));
            RefreshCommands();

            // Čuvamo samo posljednjih 10 uspješnih završavanja.
            if (_undoHistory.Count <= 10)
                return;

            var latest = _undoHistory.ToArray().Take(10).Reverse().ToArray();
            _undoHistory.Clear();

            foreach (var action in latest)
                _undoHistory.Push(action);
        }



        public async Task UndoLastFinishAsync()
        {
            if (!CanUndo)
                return;

            _undoInProgress = true;
            OnPropertyChanged(nameof(CanUndo));
            RefreshCommands();

            await _operationGate.WaitAsync();

            try
            {
                if (HasPendingOrders || _undoHistory.Count == 0)
                    return;

                var action = _undoHistory.Peek();

                using var db = new AppDbContext();

                var items = await db.KuhinjaStavke
                    .Where(s => s.IdKuhinje == action.Order.Number &&
                                action.ItemIds.Contains(s.IdStavke) &&
                                s.Zavrseno == "DA")
                    .ToListAsync();

                // Vraćamo samo stavke koje je završila konkretna akcija.
                // Djelimičan Undo ne dozvoljavamo.
                if (items.Count != action.ItemIds.Count)
                {
                    Debug.WriteLine($"[KUHINJA] Undo odbijen: promijenjeno stanje bloka {action.Order.Number}.");
                    return;
                }

                foreach (var item in items)
                    item.Zavrseno = "NE";

                await db.SaveChangesAsync();

                var names = items.Select(s => $"{s.Kolicina}x  {s.Artikl}").ToList();

                _undoHistory.Pop();
                OnPropertyChanged(nameof(CanUndo));
                RefreshCommands();

                foreach (var item in action.Order.Items.Where(i => action.ItemIds.Contains(i.IdStavke)))
                    item.IsFinished = false;

                InsertOrderSorted(action.Order);
                RefreshKeyboardSelection();

                try
                {
                    Caupo.MainWindow.Instance?.ShowKitchenNotification(
                        action.Order.Waiter,
                        action.Order.TableName,
                        action.Order.Number,
                        names,
                        action.ItemIds,
                        true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[KUHINJA] Undo notifikacija: {ex}");
                }

                await SendUndoCorrectionAsync(action.Order, names);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[KUHINJA] Undo greška: {ex}");
            }
            finally
            {
                _operationGate.Release();
                _undoInProgress = false;
                OnPropertyChanged(nameof(CanUndo));
                RefreshCommands();
            }
        }



        private readonly DispatcherTimer timer;
        private readonly SemaphoreSlim _operationGate = new(1, 1);

        private int? _selectedOrderNumber;
        private int? _selectedItemId;
        private bool _finishingKeyboardSelection;
        private bool _undoInProgress;

        public KitchenDisplayViewModel()
        {
            PreviousOrderCommand = Register(
                _ => SelectPreviousOrder(),
                _ => !HasPendingOrders && Orders.Any(o => o.CanFinish));

            NextOrderCommand = Register(
                _ => SelectNextOrder(),
                _ => !HasPendingOrders && Orders.Any(o => o.CanFinish));

            PreviousItemCommand = Register(
                _ => SelectPreviousItem(),
                _ => !HasPendingOrders && Orders.Any(o => o.Items.Any(i => i.CanFinish)));

            NextItemCommand = Register(
                _ => SelectNextItem(),
                _ => !HasPendingOrders && Orders.Any(o => o.Items.Any(i => i.CanFinish)));

            FinishSelectionCommand = Register(
                _ => _ = FinishKeyboardSelectionAsync(),
                _ => !HasPendingOrders &&
                     !_finishingKeyboardSelection &&
                     _selectedOrderNumber.HasValue);

            FinishOrderCommand = Register(
                p => _ = FinishOrderAsync((DisplayOrder)p!),
                p => p is DisplayOrder o &&
                     !HasPendingOrders &&
                     o.CanFinish &&
                     Orders.Contains(o));

            FinishItemCommand = Register(
                p => _ = FinishItemFromCommandAsync((OrderItem)p!),
                p => p is OrderItem i &&
                     !HasPendingOrders &&
                     i.CanFinish &&
                     Orders.Any(o => o.Items.Contains(i) && o.CanFinish));

            UndoCommand = Register(
                _ => _ = UndoLastFinishAsync(),
                _ => CanUndo);

            ConfirmNewOrderCommand = Register(
                _ => ConfirmPendingOrder(),
                _ => HasPendingOrders);

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            timer.Tick += (s, e) =>
            {
                foreach (var order in Orders)
                    order.Elapsed = DateTime.Now - order.OrderTime;
            };

            Orders.CollectionChanged += Orders_CollectionChanged;

            timer.Start();
            _ = LoadPendingOrdersAsync();
        }

        // =====================================================
        // SELEKCIJA TASTATUROM
        // =====================================================

        private void Orders_CollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Reset)
                RefreshKeyboardSelection();

            RefreshCommands();
        }

        private void RefreshKeyboardSelection()
        {
            var order = _selectedOrderNumber.HasValue
                ? Orders.FirstOrDefault(o => o.Number == _selectedOrderNumber.Value)
                : null;

            // Tokom F5 čuvamo identitet selekcije uprkos privremenom IsBusy stanju.
            if (!_finishingKeyboardSelection)
            {
                if (order == null || order.IsBusy)
                {
                    order = null;
                    _selectedOrderNumber = null;
                    _selectedItemId = null;
                }

                if (order != null &&
                    _selectedItemId.HasValue &&
                    !order.Items.Any(i =>
                        i.IdStavke == _selectedItemId.Value && i.CanFinish))
                {
                    _selectedItemId = null;
                }
            }

            foreach (var currentOrder in Orders)
            {
                bool selectedOrder =
                    ReferenceEquals(currentOrder, order) && !currentOrder.IsBusy;

                currentOrder.IsKeyboardSelected =
                    selectedOrder && !_selectedItemId.HasValue;

                foreach (var item in currentOrder.Items)
                {
                    item.IsKeyboardSelected =
                        selectedOrder &&
                        _selectedItemId.HasValue &&
                        item.IdStavke == _selectedItemId.Value &&
                        item.CanFinish;
                }
            }
        }

        private void SelectOrder(int direction)
        {
            var available = Orders.Where(o => o.CanFinish).ToList();

            if (available.Count == 0)
            {
                _selectedOrderNumber = null;
                _selectedItemId = null;
                RefreshKeyboardSelection();
                return;
            }

            int index = available.FindIndex(
                o => o.Number == _selectedOrderNumber);

            index = index < 0
                ? (direction > 0 ? 0 : available.Count - 1)
                : (index + direction + available.Count) % available.Count;

            _selectedOrderNumber = available[index].Number;
            _selectedItemId = null;

            RefreshKeyboardSelection();
        }

        private void SelectItem(int direction)
        {
            var order = _selectedOrderNumber.HasValue
                ? Orders.FirstOrDefault(o =>
                    o.Number == _selectedOrderNumber.Value && o.CanFinish)
                : null;

            if (order == null)
            {
                order = Orders.FirstOrDefault(o =>
                    o.CanFinish && o.Items.Any(i => i.CanFinish));

                if (order == null)
                    return;

                _selectedOrderNumber = order.Number;
            }

            var available = order.Items.Where(i => i.CanFinish).ToList();

            if (available.Count == 0)
            {
                _selectedItemId = null;
                RefreshKeyboardSelection();
                return;
            }

            int index = available.FindIndex(
                i => i.IdStavke == _selectedItemId);

            index = index < 0
                ? (direction > 0 ? 0 : available.Count - 1)
                : (index + direction + available.Count) % available.Count;

            _selectedItemId = available[index].IdStavke;
            RefreshKeyboardSelection();
        }

        // F1
        public void SelectPreviousOrder()
        {
            SelectOrder(-1);
        }

        // F2
        public void SelectNextOrder()
        {
            SelectOrder(1);
        }

        // F3
        public void SelectPreviousItem()
        {
            SelectItem(-1);
        }

        // F4
        public void SelectNextItem()
        {
            SelectItem(1);
        }

        // =====================================================
        // F5 - ZAVRŠAVANJE SELEKTOVANOG ELEMENTA
        // =====================================================

        public async Task FinishKeyboardSelectionAsync()
        {
            if (HasPendingOrders ||
                _finishingKeyboardSelection ||
                !_selectedOrderNumber.HasValue)
            {
                return;
            }

            var order = Orders.FirstOrDefault(
                o => o.Number == _selectedOrderNumber.Value);

            if (order == null || !order.CanFinish)
                return;

            OrderItem? item = _selectedItemId.HasValue
                ? order.Items.FirstOrDefault(
                    i => i.IdStavke == _selectedItemId.Value)
                : null;

            if (_selectedItemId.HasValue &&
                (item == null || !item.CanFinish))
            {
                return;
            }

            // Pamtimo redoslijed prije završavanja radi pravilne naredne selekcije.
            var orderNumbers = Orders
                .Where(o => o.CanFinish)
                .Select(o => o.Number)
                .ToList();

            int oldOrderIndex = orderNumbers.IndexOf(order.Number);
            int oldItemIndex = item != null ? order.Items.IndexOf(item) : -1;

            _finishingKeyboardSelection = true;
            RefreshCommands();

            try
            {
                if (item != null)
                {
                    await FinishItemAsync(item, order);
                }
                else
                {
                    await FinishOrderAsync(order);
                }

                if (Orders.Contains(order))
                {
                    _selectedOrderNumber = order.Number;

                    if (item != null)
                    {
                        if (!item.IsFinished)
                        {
                            // Ako spremanje nije uspjelo, ostajemo na istoj stavci.
                            _selectedItemId = item.IdStavke;
                        }
                        else
                        {
                            var nextItem = order.Items
                                .Skip(oldItemIndex + 1)
                                .FirstOrDefault(i => i.CanFinish);

                            nextItem ??= order.Items.FirstOrDefault(
                                i => i.CanFinish);

                            _selectedItemId = nextItem?.IdStavke;
                        }
                    }
                    else
                    {
                        _selectedItemId = null;
                    }
                }
                else
                {
                    // Biramo narednu postojeću narudžbu prema prethodnom redoslijedu.
                    int? nextOrderNumber = null;

                    if (oldOrderIndex >= 0)
                    {
                        for (int offset = 1;
                             offset < orderNumbers.Count;
                             offset++)
                        {
                            int index =
                                (oldOrderIndex + offset) % orderNumbers.Count;

                            int candidate = orderNumbers[index];

                            if (Orders.Any(o =>
                                o.Number == candidate && o.CanFinish))
                            {
                                nextOrderNumber = candidate;
                                break;
                            }
                        }
                    }

                    nextOrderNumber ??= Orders
                        .FirstOrDefault(o => o.CanFinish)?.Number;

                    _selectedOrderNumber = nextOrderNumber;
                    _selectedItemId = null;
                }
            }
            finally
            {
                _finishingKeyboardSelection = false;
                RefreshKeyboardSelection();
                RefreshCommands();
            }
        }

        // =====================================================
        // UČITAVANJE NARUDŽBI
        // =====================================================

        public async Task LoadPendingOrdersAsync()
        {
            await _operationGate.WaitAsync();

            try
            {
                using var db = new AppDbContext();

                var headers = await db.Kuhinja.AsNoTracking()
                    .Where(k => db.KuhinjaStavke.Any(s =>
                        s.IdKuhinje == k.IdKuhinje && s.Zavrseno == "NE"))
                    .OrderBy(k => k.Datum)
                    .ThenByDescending(k => k.IdKuhinje)
                    .ToListAsync();

                var ids = headers.Select(h => h.IdKuhinje).ToList();

                var allItems = await db.KuhinjaStavke.AsNoTracking()
                    .Where(s => ids.Contains(s.IdKuhinje))
                    .OrderBy(s => s.IdStavke)
                    .ToListAsync();

                var loaded = headers.Select(h => new DisplayOrder
                {
                    Number = h.IdKuhinje,
                    Waiter = h.Radnik ?? string.Empty,
                    TableName = h.NazivStola ?? string.Empty,
                    OrderTime = h.Datum,
                    Elapsed = DateTime.Now - h.Datum,
                    Items = new ObservableCollection<OrderItem>(
                        allItems
                            .Where(s => s.IdKuhinje == h.IdKuhinje)
                            .Select(s => new OrderItem
                            {
                                IdStavke = s.IdStavke,
                                Name = s.Artikl ?? string.Empty,
                                Note = s.Note ?? string.Empty,
                                Quantity = s.Kolicina,
                                IsFinished = s.Zavrseno == "DA"
                            }))
                }).ToList();

                if (!_initialOrdersLoaded)
                {
                    foreach (var order in loaded
                        .OrderBy(o => o.OrderTime)
                        .ThenBy(o => o.Number))
                    {
                        if (!_pendingOrderNumbers.Contains(order.Number))
                            InsertOrderSorted(order);
                    }

                    _initialOrdersLoaded = true;
                }

                RefreshKeyboardSelection();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[KUHINJA] Učitavanje nije uspjelo: {ex}");
            }
            finally
            {
                _operationGate.Release();
            }
        }

        public void AddOrder(DisplayOrder order)
        {
            if (order == null || order.Number <= 0)
                return;

            Application.Current.Dispatcher.VerifyAccess();

            if (_pendingOrderNumbers.Contains(order.Number))
                return;

            if (Orders.Any(o => o.Number == order.Number))
                return;

            _pendingOrderNumbers.Add(order.Number);
            _pendingOrders.Enqueue(order);
            NotifyPendingOrdersChanged();
        }

        public async void RemoveOrder(DisplayOrder order)
        {
            await FinishOrderAsync(order);
        }

        // =====================================================
        // ZAVRŠAVANJE CIJELE NARUDŽBE
        // =====================================================


        public async Task FinishOrderAsync(DisplayOrder order)
        {
            if (HasPendingOrders || order == null || order.IsBusy || !Orders.Contains(order))
                return;

            order.IsBusy = true;
            RefreshCommands();
            await _operationGate.WaitAsync();

            try
            {
                using var db = new AppDbContext();

                var remaining = await db.KuhinjaStavke
                    .Where(s => s.IdKuhinje == order.Number && s.Zavrseno == "NE")
                    .ToListAsync();

                if (remaining.Count == 0)
                {
                    Orders.Remove(order);
                    return;
                }

                foreach (var item in remaining)
                    item.Zavrseno = "DA";

                await db.SaveChangesAsync();

                var itemIds = remaining.Select(s => s.IdStavke).ToList();
                RecordUndo(order, itemIds);

                var names = remaining.Select(s => $"{s.Kolicina}x  {s.Artikl}").ToList();

                Orders.Remove(order);

                ShowCompletedPopup(order, names, itemIds);
                await SendNotificationAsync(order, names, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[KUHINJA] Završavanje narudžbe {order.Number}: {ex}");
            }
            finally
            {
                order.IsBusy = false;
                _operationGate.Release();
                RefreshCommands();
            }
        }


        // Komanda dobija stavku iz DataTemplate; narudžbu pronalazi u modelu.
        private async Task FinishItemFromCommandAsync(OrderItem item)
        {
            var order = Orders.FirstOrDefault(
                o => o.Items.Contains(item));

            if (order != null)
                await FinishItemAsync(item, order);
        }

        // =====================================================
        // ZAVRŠAVANJE POJEDINAČNE STAVKE
        // =====================================================


        public async Task FinishItemAsync(OrderItem item, DisplayOrder order)
        {
            if (HasPendingOrders || item == null || order == null || item.IdStavke <= 0 ||
                item.IsFinished || order.IsBusy || !Orders.Contains(order))
                return;

            order.IsBusy = true;
            RefreshCommands();
            await _operationGate.WaitAsync();

            try
            {
                using var db = new AppDbContext();

                var dbItem = await db.KuhinjaStavke
                    .FirstOrDefaultAsync(s => s.IdStavke == item.IdStavke && s.IdKuhinje == order.Number);

                if (dbItem == null)
                    return;

                if (dbItem.Zavrseno == "DA")
                {
                    item.IsFinished = true;

                    if (!await db.KuhinjaStavke.AnyAsync(s =>
                        s.IdKuhinje == order.Number && s.Zavrseno == "NE"))
                    {
                        Orders.Remove(order);
                    }

                    return;
                }

                if (dbItem.Zavrseno != "NE")
                    return;

                dbItem.Zavrseno = "DA";
                await db.SaveChangesAsync();

                var itemIds = new List<int> { dbItem.IdStavke };
                RecordUndo(order, itemIds);

                item.IsFinished = true;

                var names = new List<string> { $"{dbItem.Kolicina}x  {dbItem.Artikl}" };

                var hasRemaining = await db.KuhinjaStavke.AnyAsync(s =>
                    s.IdKuhinje == order.Number && s.Zavrseno == "NE");

                if (!hasRemaining)
                    Orders.Remove(order);

                ShowCompletedPopup(order, names, itemIds);
                await SendNotificationAsync(order, names, !hasRemaining);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[KUHINJA] Završavanje stavke {item.IdStavke}: {ex}");
            }
            finally
            {
                order.IsBusy = false;
                _operationGate.Release();
                RefreshCommands();
            }
        }


        // =====================================================
        // ZVUK I POPUP
        // =====================================================



        private static void ShowCompletedPopup(DisplayOrder order, List<string> names, IEnumerable<int> itemIds)
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Caupo;component/Sounds/notificationbell.mp3");
                var player = new MediaPlayer();

                player.MediaEnded += (s, e) => player.Close();
                player.MediaFailed += (s, e) =>
                {
                    Debug.WriteLine(e.ErrorException);
                    player.Close();
                };

                player.Open(uri);
                player.Play();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[KUHINJA] Zvuk: {ex.Message}");

                try
                {
                    SystemSounds.Beep.Play();
                }
                catch
                {
                }
            }

            try
            {
                Caupo.MainWindow.Instance?.ShowKitchenNotification(
                    order.Waiter, order.TableName, order.Number, names, itemIds);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[KUHINJA] Notifikacija: {ex}");
            }
        }



        // =====================================================
        // ANDROID NOTIFIKACIJE
        // =====================================================

        private static async Task SendUndoCorrectionAsync(
            DisplayOrder order,
            List<string> names)
        {
            try
            {
                string message =
                    $"ISPRAVKA IZ KUHINJE: Prethodna obavijest o završetku za narudžbu broj {order.Number}, " +
                    $"{order.TableName}, poslana je greškom. Ponovo je u pripremi: {string.Join(", ", names)}.";

                var payload = new
                {
                    Status = "OK",
                    Data = new
                    {
                        type = "KUHINJA",
                        orderNumber = order.Number,
                        tableName = order.TableName,
                        message
                    }
                };

                var session = ClientRegistry.Get(order.Waiter);

                if (session == null)
                {
                    Debug.WriteLine(
                        $"[KUHINJA] Undo: nema Android sesije za '{order.Waiter}'.");
                    return;
                }

                if (session.Client?.Connected != true)
                {
                    ClientRegistry.Remove(order.Waiter);
                    return;
                }

                var json = JsonSerializer.Serialize(payload) + "\n";
                var bytes = Encoding.UTF8.GetBytes(json);

                await session.Stream.WriteAsync(
                    bytes, 0, bytes.Length);

                Debug.WriteLine(
                    $"[KUHINJA] Android Undo ispravka poslana: {json}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[KUHINJA] Android Undo ispravka nije poslana: {ex}");
            }
        }

        private static async Task SendNotificationAsync(
            DisplayOrder order,
            List<string> names,
            bool complete)
        {
            try
            {
                var message = complete
                    ? $"Narudžba broj {order.Number} za {order.TableName} je spremna. Gotovo: {string.Join(", ", names)}"
                    : $"Za narudžbu broj {order.Number}, {order.TableName}, spremno: {string.Join(", ", names)}";

                var payload = new
                {
                    Status = "OK",
                    Data = new
                    {
                        type = "KUHINJA",
                        orderNumber = order.Number,
                        tableName = order.TableName,
                        message
                    }
                };

                var json = JsonSerializer.Serialize(payload) + "\n";
                var session = ClientRegistry.Get(order.Waiter);

                if (session == null)
                {
                    Debug.WriteLine(
                        $"[KUHINJA] Nema Android sesije za '{order.Waiter}'");
                    return;
                }

                if (session.Client?.Connected != true)
                {
                    ClientRegistry.Remove(order.Waiter);
                    return;
                }

                var bytes = Encoding.UTF8.GetBytes(json);

                await session.Stream.WriteAsync(
                    bytes, 0, bytes.Length);

                Debug.WriteLine(
                    $"[KUHINJA] Android poruka poslana: {json}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[KUHINJA] Android notifikacija nije poslana: {ex}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(name));
        }
    }
}