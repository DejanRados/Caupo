using Caupo.Data;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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
            public decimal? Quantity { get; set; }
        }

        public class DisplayOrder : INotifyPropertyChanged
        {
            public int Number { get; set; }
            public string Waiter { get; set; }
            public string TableName { get; set; }
            public DateTime OrderTime { get; set; }
            public ObservableCollection<OrderItem> Items { get; set; }

            public string ColorGreenHex { get; set; } = "#419141";
            public string ColorYellowHex { get; set; } = "#cfbb23";
            public string ColorRedHex { get; set; } = "#ad1818";

            private Brush _background = Brushes.LightGreen;
            public Brush Background
            {
                get
                {
                    if (Elapsed.TotalMinutes >= 20)
                        return (SolidColorBrush)(new BrushConverter ().ConvertFrom (ColorRedHex));
                    else if (Elapsed.TotalMinutes >= 10)
                        return (SolidColorBrush)(new BrushConverter ().ConvertFrom (ColorYellowHex));
                    else
                        return (SolidColorBrush)(new BrushConverter ().ConvertFrom (ColorGreenHex));
                }
               }
            
            private TimeSpan _elapsed;
            public TimeSpan Elapsed
            {
                get => _elapsed;
                set { _elapsed = value;
                    OnPropertyChanged ();
                    OnPropertyChanged (nameof (Background));
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
                foreach (var order in Orders)
                    order.Elapsed = DateTime.Now - order.OrderTime;
            };
            timer.Start ();

            _ = LoadPendingOrdersAsync ();
        }

        public async Task LoadPendingOrdersAsync()
        {
            Orders.Clear (); // očisti stare

            using (var db = new AppDbContext ())
            {
                // Učitaj sve narudžbe koje nisu završene
                var kuhinjaOrders = await db.Kuhinja
                    .Where (k => db.KuhinjaStavke.Any (s => s.IdKuhinje == k.IdKuhinje && s.Zavrseno == "NE"))
                    .ToListAsync ();

                foreach (var order in kuhinjaOrders)
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
                using (var db = new AppDbContext ())
                {
                    var stavke = await db.KuhinjaStavke
                        .Where (s => s.IdKuhinje == order.Number)                             
                        .ToListAsync ();
                    Debug.WriteLine ("Traži Idkuhinje:" + order.Number);
                    foreach (var s in stavke)
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
                    catch (Exception ex)
                    {
                        Debug.WriteLine ($"Error playing sound: {ex.Message}");
                        // Fallback na SystemSounds
                        SystemSounds.Beep.Play ();
                    }
                  

                    // Prikaži popup
                    Application.Current.Dispatcher.Invoke (() =>
                    {
                        Debug.WriteLine (" Application.Current.Dispatcher.Invoke (() =>   Otvara popup");
                        var popup = new Caupo.Views.OrderCompletedPopup (order.Waiter, order.TableName, order.Number.ToString(), stavkeList);
                        popup.Show ();
                    });
                    Orders.Remove (order);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine ($"Greška pri ažuriranju baze: {ex.Message}");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (name));
    }

}
