using Caupo.Data;
using Caupo.Server;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Media;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Caupo.ViewModels
{
    public class KitchenDisplayViewModel : INotifyPropertyChanged
    {
        public class OrderItem
        {
            public string Name { get; set; }
            public string Note { get; set; }
            public bool Visible => !string.IsNullOrWhiteSpace (Note);
            public decimal? Quantity { get; set; }
        }

        public class DisplayOrder : INotifyPropertyChanged
        {
            public int Number { get; set; }
            public string Waiter { get; set; }
            public string TableName { get; set; }
            public DateTime OrderTime { get; set; }
            public ObservableCollection<OrderItem> Items { get; set; }

            public string ColorGreenBackgroundHex { get; set; } = "#1E3A2B";
            public string ColorGreenBorderHex { get; set; } = "#2F9E44";
            public string ColorYellowBackgroundHex { get; set; } = " #3A2F1A";
            public string ColorYellowBorderHex { get; set; } = "#F59F00";
            public string ColorRedBackgroundHex { get; set; } = "#3A1E1E";
            public string ColorRedBorderHex { get; set; } = "#E03131";

            private Brush _background = Brushes.LightGreen;
            public Brush Background
            {
                get
                {
                    if(Elapsed.TotalMinutes >= 20)
                        return (SolidColorBrush)(new BrushConverter ().ConvertFrom (ColorRedBackgroundHex));
                    else if(Elapsed.TotalMinutes >= 10)
                        return (SolidColorBrush)(new BrushConverter ().ConvertFrom (ColorYellowBackgroundHex));
                    else
                        return (SolidColorBrush)(new BrushConverter ().ConvertFrom (ColorGreenBackgroundHex));
                }
            }

            private Brush _border = Brushes.LightGreen;
            public Brush Border
            {
                get
                {
                    if(Elapsed.TotalMinutes >= 20)
                        return (SolidColorBrush)(new BrushConverter ().ConvertFrom (ColorRedBorderHex));
                    else if(Elapsed.TotalMinutes >= 10)
                        return (SolidColorBrush)(new BrushConverter ().ConvertFrom (ColorYellowBorderHex));
                    else
                        return (SolidColorBrush)(new BrushConverter ().ConvertFrom (ColorGreenBorderHex));
                }
            }

            private TimeSpan _elapsed;
            public TimeSpan Elapsed
            {
                get => _elapsed;
                set
                {
                    _elapsed = value;
                    OnPropertyChanged ();
                    OnPropertyChanged (nameof (Background));
                    OnPropertyChanged (nameof (Border));
                }
            }

            private bool _isNote = false;
            public bool IsNote
            {
                get => _isNote;
                set
                {
                    if(_isNote != value)
                    {
                        _isNote = value;

                        OnPropertyChanged (nameof (IsNote));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string name = null) =>
                PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (name));
        }

        public ObservableCollection<DisplayOrder> Orders { get; set; } = new ObservableCollection<DisplayOrder> ();

        private DispatcherTimer timer;



        public KitchenDisplayViewModel()
        {

            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds (1) };
            timer.Tick += (s, e) =>
            {
                foreach(var order in Orders)
                    order.Elapsed = DateTime.Now - order.OrderTime;
            };
            timer.Start ();

            _ = LoadPendingOrdersAsync ();
        }

        public async Task LoadPendingOrdersAsync()
        {
            Orders.Clear (); // očisti stare

            using(var db = new AppDbContext ())
            {
                // Učitaj sve narudžbe koje nisu završene
                var kuhinjaOrders = await db.Kuhinja
                    .Where (k => db.KuhinjaStavke.Any (s => s.IdKuhinje == k.IdKuhinje && s.Zavrseno == "NE"))
                    .ToListAsync ();

                foreach(var order in kuhinjaOrders)
                {
                    var stavke = await db.KuhinjaStavke
                        .Where (s => s.IdKuhinje == order.IdKuhinje && s.Zavrseno == "NE")
                        .ToListAsync ();
                    Debug.WriteLine ("Ime stola: " + order.NazivStola);
                    var displayOrder = new DisplayOrder
                    {
                        Number = order.IdKuhinje,
                        Waiter = order.Radnik,
                        TableName = order.NazivStola,
                        OrderTime = order.Datum,
                        Items = new ObservableCollection<OrderItem> (
                            stavke.Select (s => new OrderItem
                            {
                                Name = s.Artikl,
                                Note = s.Note,
                                Quantity = s.Kolicina
                            }))
                    };

                    Orders.Add (displayOrder);
                }
            }
        }


        public void AddOrder(DisplayOrder order)
        {
            order.OrderTime = DateTime.Now;
            order.Elapsed = TimeSpan.Zero;
            Orders.Add (order);
        }

        public async void RemoveOrder(DisplayOrder order)
        {


            try
            {
                using(var db = new AppDbContext ())
                {
                    var stavke = await db.KuhinjaStavke
                        .Where (s => s.IdKuhinje == order.Number)
                        .ToListAsync ();
                    Debug.WriteLine ("Traži Idkuhinje:" + order.Number);
                    foreach(var s in stavke)
                    {
                        s.Zavrseno = "DA";
                    }

                    await db.SaveChangesAsync ();

                    var stavkeList = order.Items.Select (i => $"{i.Quantity}x  {i.Name}").ToList ();

                    try
                    {

                        string uriString = $"pack://application:,,,/Caupo;component/Sounds/notificationbell.mp3";

                        var mediaPlayer = new MediaPlayer ();
                        mediaPlayer.Open (new Uri (uriString));
                        mediaPlayer.MediaEnded += (s, e) => mediaPlayer.Close ();
                        mediaPlayer.Play ();
                        mediaPlayer.MediaFailed += (s, e) =>
                        {
                            Debug.WriteLine (e.ErrorException?.Message);
                        };

                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine ($"Error playing sound: {ex.Message}");
                        // Fallback na SystemSounds
                        SystemSounds.Beep.Play ();
                    }


                    // Prikaži popup na WPF-u
                    Application.Current.Dispatcher.Invoke (() =>
                    {
                        Debug.WriteLine ("Application.Current.Dispatcher.Invoke (() => Otvara popup");
                        var popup = new Caupo.Views.OrderCompletedPopup (
                            order.Waiter,
                            order.TableName,
                            order.Number.ToString (),
                            stavkeList);
                        popup.Show ();
                    });

                    // 2️⃣ Pripremi uniformni JSON za Android notifikaciju
                    var notifData = new
                    {
                        type = "KUHINJA",
                        orderNumber = order.Number,
                        tableName = order.TableName,
                        message = $"Narudžba broj {order.Number} za {order.TableName}   je spremna "
                    };

                    // Wrapamo u objekt sa Status/Data, isto kao u handlerima
                    var notifWrapper = new
                    {
                        Status = "OK",
                        Data = notifData
                    };

                    // Serijaliziramo i dodajemo \n kao delimiter
                    var notifJson = JsonSerializer.Serialize (notifWrapper) + "\n";

                    // 3️⃣ Pošalji notifikaciju Android uređaju
                    // 3️⃣ Pošalji notifikaciju Android uređaju
                    Debug.WriteLine ($"[Notification] Tražim Android sesiju za waiter: '{order.Waiter}'");

                    // Prikaži sve registrovanje klijente prije traženja
                    ClientRegistry.ListAll ();

                    var androidSession = ClientRegistry.Get (order.Waiter); // order.Waiter = userId

                    if(androidSession != null)
                    {
                        Debug.WriteLine ($"[Notification] Sesija pronađena za '{order.Waiter}'");
                        Debug.WriteLine ($"[Notification] Client.Connected: {androidSession.Client?.Connected}");

                        if(androidSession.Client?.Connected == true)
                        {
                            var bytes = Encoding.UTF8.GetBytes (notifJson);
                            await androidSession.Stream.WriteAsync (bytes, 0, bytes.Length);
                            Debug.WriteLine ("[Notification] Poslana ORDER_READY notifikacija Androidu: " + notifJson);
                        }
                        else
                        {
                            Debug.WriteLine ($"[Notification] Klijent '{order.Waiter}' nije povezan, uklanjam iz registry");
                            ClientRegistry.Remove (order.Waiter);
                        }
                    }
                    else
                    {
                        Debug.WriteLine ($"[Notification] Android session nije pronađen za userId: '{order.Waiter}'");
                        Debug.WriteLine ($"[Notification] Dostupni klijenti: {string.Join (", ", GetRegisteredClients ())}");
                    }



                    Orders.Remove (order);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"Greška pri ažuriranju baze: {ex.Message}");
            }
        }


        private List<string> GetRegisteredClients()
        {
            return ClientRegistry.GetAll ()
                .Select (s => $"'{s.DeviceId}' (Connected: {s.Client?.Connected})")
                .ToList ();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (name));
    }

}
