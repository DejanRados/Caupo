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
            public string ColorYellowBackgroundHex { get; set; } = "#3A2F1A";
            public string ColorYellowBorderHex { get; set; } = "#F59F00";
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
                1 => "#493B22",
                _ => "#294B37"
            });

            public Brush HeaderBorder => MakeBrush(ColorState switch
            {
                2 => "#874343",
                1 => "#896A32",
                _ => "#3D7950"
            });

            public Brush ItemBackground => MakeBrush(ColorState switch
            {
                2 => "#512B2B",
                1 => "#514128",
                _ => "#30583F"
            });

            public Brush ItemBorder => MakeBrush(ColorState switch
            {
                2 => "#975050",
                1 => "#99783C",
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

        public ObservableCollection<DisplayOrder> Orders { get; set; } = new();

        private readonly DispatcherTimer timer;
        private readonly SemaphoreSlim _operationGate = new(1, 1);

        private int? _selectedOrderNumber;
        private int? _selectedItemId;
        private bool _finishingKeyboardSelection;

        public KitchenDisplayViewModel()
        {
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
        }

        private void RefreshKeyboardSelection()
        {
            var order = _selectedOrderNumber.HasValue
                ? Orders.FirstOrDefault(o => o.Number == _selectedOrderNumber.Value)
                : null;

            // Dok F5 završava narudžbu ili stavku, ne brišemo
            // identitet selekcije zbog privremenog IsBusy stanja.
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
                    ReferenceEquals(currentOrder, order) &&
                    !currentOrder.IsBusy;

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
            var available = Orders
                .Where(o => o.CanFinish)
                .ToList();

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
                    o.Number == _selectedOrderNumber.Value &&
                    o.CanFinish)
                : null;

            if (order == null)
            {
                order = Orders.FirstOrDefault(o =>
                    o.CanFinish &&
                    o.Items.Any(i => i.CanFinish));

                if (order == null)
                    return;

                _selectedOrderNumber = order.Number;
            }

            var available = order.Items
                .Where(i => i.CanFinish)
                .ToList();

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
            if (_finishingKeyboardSelection ||
                !_selectedOrderNumber.HasValue)
                return;

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
                return;

            // Pamtimo redoslijed prije završavanja.
            // MasonryPanel može prerasporediti kartice nakon uklanjanja.
            var orderNumbers = Orders
                .Where(o => o.CanFinish)
                .Select(o => o.Number)
                .ToList();

            int oldOrderIndex = orderNumbers.IndexOf(order.Number);
            int oldItemIndex = item != null
                ? order.Items.IndexOf(item)
                : -1;

            _finishingKeyboardSelection = true;

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

                // Ako je narudžba još prisutna, ostajemo na njoj.
                if (Orders.Contains(order))
                {
                    _selectedOrderNumber = order.Number;

                    if (item != null)
                    {
                        // Ako završavanje nije uspjelo, zadržavamo
                        // istu stavku umjesto preskakanja.
                        if (!item.IsFinished)
                        {
                            _selectedItemId = item.IdStavke;
                        }
                        else
                        {
                            // Sljedeća nezavršena stavka iza prethodne.
                            var nextItem = order.Items
                                .Skip(oldItemIndex + 1)
                                .FirstOrDefault(i => i.CanFinish);

                            // Ako nema sljedeće, vraćamo se na početak
                            // liste nezavršenih stavki iste narudžbe.
                            nextItem ??= order.Items
                                .FirstOrDefault(i => i.CanFinish);

                            // Ako nema stavki, selektujemo zaglavlje.
                            _selectedItemId = nextItem?.IdStavke;
                        }
                    }
                    else
                    {
                        // Narudžba nije uklonjena.
                        // Ne prelazimo dalje ako završavanje nije uspjelo.
                        _selectedItemId = null;
                    }
                }
                else
                {
                    // Narudžba je uklonjena.
                    // Tražimo prvu narednu narudžbu prema starom redoslijedu.
                    int? nextOrderNumber = null;

                    if (oldOrderIndex >= 0)
                    {
                        for (int offset = 1; offset < orderNumbers.Count; offset++)
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

                    // Ako su u međuvremenu dodane nove narudžbe,
                    // a nijedna stara nije ostala, biramo prvu novu.
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
                    .Where(k => db.KuhinjaStavke.Any(
                        s => s.IdKuhinje == k.IdKuhinje &&
                             s.Zavrseno == "NE"))
                    .OrderBy(k => k.Datum)
                    .ToListAsync();

                var ids = headers
                    .Select(h => h.IdKuhinje)
                    .ToList();

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

                Orders.Clear();

                foreach (var order in loaded)
                    Orders.Add(order);

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
            if (order == null)
                return;

            _ = LoadPendingOrdersAsync();
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
            if (order == null ||
                order.IsBusy ||
                !Orders.Contains(order))
                return;

            order.IsBusy = true;
            await _operationGate.WaitAsync();

            try
            {
                using var db = new AppDbContext();

                var remaining = await db.KuhinjaStavke
                    .Where(s =>
                        s.IdKuhinje == order.Number &&
                        s.Zavrseno == "NE")
                    .ToListAsync();

                if (remaining.Count == 0)
                {
                    Orders.Remove(order);
                    return;
                }

                foreach (var item in remaining)
                    item.Zavrseno = "DA";

                await db.SaveChangesAsync();

                var names = remaining
                    .Select(s => $"{s.Kolicina}x  {s.Artikl}")
                    .ToList();

                Orders.Remove(order);

                ShowCompletedPopup(order, names);
                await SendNotificationAsync(order, names, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[KUHINJA] Završavanje narudžbe {order.Number}: {ex}");
            }
            finally
            {
                order.IsBusy = false;
                _operationGate.Release();
            }
        }

        // =====================================================
        // ZAVRŠAVANJE POJEDINAČNE STAVKE
        // =====================================================

        public async Task FinishItemAsync(
            OrderItem item,
            DisplayOrder order)
        {
            if (item == null ||
                order == null ||
                item.IdStavke <= 0 ||
                item.IsFinished ||
                order.IsBusy ||
                !Orders.Contains(order))
                return;

            order.IsBusy = true;
            await _operationGate.WaitAsync();

            try
            {
                using var db = new AppDbContext();

                var dbItem = await db.KuhinjaStavke
                    .FirstOrDefaultAsync(s =>
                        s.IdStavke == item.IdStavke &&
                        s.IdKuhinje == order.Number);

                if (dbItem == null)
                    return;

                if (dbItem.Zavrseno == "DA")
                {
                    item.IsFinished = true;

                    if (!await db.KuhinjaStavke.AnyAsync(s =>
                        s.IdKuhinje == order.Number &&
                        s.Zavrseno == "NE"))
                    {
                        Orders.Remove(order);
                    }

                    return;
                }

                if (dbItem.Zavrseno != "NE")
                    return;

                dbItem.Zavrseno = "DA";
                await db.SaveChangesAsync();

                item.IsFinished = true;

                var names = new List<string>
                {
                    $"{dbItem.Kolicina}x  {dbItem.Artikl}"
                };

                var hasRemaining =
                    await db.KuhinjaStavke.AnyAsync(s =>
                        s.IdKuhinje == order.Number &&
                        s.Zavrseno == "NE");

                if (!hasRemaining)
                    Orders.Remove(order);

                ShowCompletedPopup(order, names);

                await SendNotificationAsync(
                    order,
                    names,
                    !hasRemaining);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[KUHINJA] Završavanje stavke {item.IdStavke}: {ex}");
            }
            finally
            {
                order.IsBusy = false;
                _operationGate.Release();
            }
        }

        // =====================================================
        // ZVUK I POPUP
        // =====================================================

        private static void ShowCompletedPopup(
            DisplayOrder order,
            List<string> names)
        {
            try
            {
                var uri = new Uri(
                    "pack://application:,,,/Caupo;component/Sounds/notificationbell.mp3");

                var player = new MediaPlayer();

                player.MediaEnded += (s, e) =>
                    player.Close();

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
                Debug.WriteLine(
                    $"[KUHINJA] Zvuk: {ex.Message}");

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
                new Caupo.Views.OrderCompletedPopup(
                    order.Waiter,
                    order.TableName,
                    order.Number.ToString(),
                    names).Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[KUHINJA] Popup: {ex}");
            }
        }

        // =====================================================
        // ANDROID NOTIFIKACIJE
        // =====================================================

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
                    bytes,
                    0,
                    bytes.Length);

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