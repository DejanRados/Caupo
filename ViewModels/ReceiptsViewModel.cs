using Caupo.Data;
using Caupo.Fiscal;
using Caupo.Properties;
using Caupo.Views;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static Caupo.Data.DatabaseTables;
using static Caupo.ViewModels.KnjigaKuhinjeViewModel;

namespace Caupo.ViewModels
{
    public class ReceiptsViewModel : INotifyPropertyChanged
    {

        public ObservableCollection<DatabaseTables.TblRacuni> Receipts { get; set; } = new ObservableCollection<DatabaseTables.TblRacuni>();
        public ObservableCollection<DatabaseTables.TblRacunStavka> ReceiptItems { get; set; } = new ObservableCollection<DatabaseTables.TblRacunStavka>();

        public ObservableCollection<FiskalniRacun.Item>? _stavkeRacuna;
        public ObservableCollection<FiskalniRacun.Item>? StavkeRacuna
        {
            get => _stavkeRacuna;
            set
            {
                _stavkeRacuna = value;
                OnPropertyChanged (nameof (StavkeRacuna));

            }
        }

        private DatabaseTables.TblRacuni? _selectedReceipt;
        public DatabaseTables.TblRacuni? SelectedReceipt
        {
            get => _selectedReceipt;
            set
            {
                if (_selectedReceipt != value)
                {
                    _selectedReceipt = value;
                    OnPropertyChanged(nameof(SelectedReceipt));
                    
                }
            }
        }

        private ObservableCollection<TblRacuni>? receiptsFilter;
        public ObservableCollection<TblRacuni>? ReceiptsFilter
        {
            get => receiptsFilter;
            set
            {
                receiptsFilter = value;
                OnPropertyChanged(nameof(ReceiptsFilter));
            }
        }

        public string? _searchText;
        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    Debug.WriteLine($"SearchText changed to: ");  // Dodaj log za testiranje
                    FilterItems(_searchText);
                }
            }
        }

        private decimal? iznosRacuna = 0;
        public decimal? IznosRacuna
        {
            get { return iznosRacuna; }
            set
            {
                iznosRacuna = value;
                OnPropertyChanged(nameof(IznosRacuna));
            }
        }



        public ReceiptsViewModel()
        {
            
            Receipts = new ObservableCollection<TblRacuni>();
            ReceiptsFilter = new ObservableCollection<TblRacuni>();
            ReceiptItems = new ObservableCollection<TblRacunStavka>();
            StavkeRacuna = new ObservableCollection<FiskalniRacun.Item> ();
            Debug.WriteLine("SearchText = " + SearchText);
            _ = Start();
        }



        private async Task Start()
        {
            await  LoadReceiptsAsync(null);
          
        }

        string? TaxLabel(string ps)
        {

            string taxeslabel;
            switch (ps)
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

        public async Task LoadReceiptItems(TblRacuni selectedRacun)
        {
            try
            {
                using var db = new AppDbContext ();

                IznosRacuna = 0;

                // Efikasno dohvaćanje samo stavki za odabrani račun
                var receiptItemsFromDb = await db.RacunStavka
                    .Where (a => a.BrojRacuna == selectedRacun.BrojRacuna)
                    .OrderBy (a => a.IdStavke)
                    .ToListAsync ();

                // Privremene kolekcije za batch update
                var tempReceiptItems = new List<TblRacunStavka> ();
                var tempStavkeRacuna = new List<FiskalniRacun.Item> ();

                foreach (var ri in receiptItemsFromDb)
                {
                    tempReceiptItems.Add (ri);
                    IznosRacuna += ri.Iznos;

                    var stavka = new FiskalniRacun.Item
                    {
                        Name = ri.ArtiklNormativ,
                        Sifra = ri.Sifra,
                        BrojRacuna = 222, // ili odakle treba
                        Naziv = ri.Artikl,
                        UnitPrice = (decimal?)ri.Cijena,
                        Proizvod = ri.VrstaArtikla,
                        JedinicaMjere = ri.JedinicaMjere,
                        Quantity = ri.Kolicina
                    };
                    stavka.Labels.Add (TaxLabel (ri.PoreskaStopa.ToString ()));

                    tempStavkeRacuna.Add (stavka);
                }

                // Batch update ObservableCollection
                ReceiptItems.Clear ();
                foreach (var item in tempReceiptItems)
                    ReceiptItems.Add (item);

                StavkeRacuna.Clear ();
                foreach (var item in tempStavkeRacuna)
                    StavkeRacuna.Add (item);
            }
            catch (Exception ex)
            {
                Debug.WriteLine (ex);
            }
        }



        public event EventHandler<string?>? ErrorOccurred;
        protected virtual void OnErrorOccurred(string? message)
        {
            ErrorOccurred?.Invoke(this, message);
        }



        public async Task LoadReceiptsAsync(TblRacuni selected)
        {
            Debug.WriteLine ("=== LoadReceiptsAsync START ===");

            try
            {
                using var db = new AppDbContext ();
                Debug.WriteLine ("DbContext kreiran");

                var receipts = await db.Racuni.ToListAsync ();
                Debug.WriteLine ($"Racuni učitani: count = {receipts?.Count}");

                Receipts.Clear ();
                ReceiptsFilter?.Clear ();
                Debug.WriteLine ("Receipts i ReceiptsFilter očišćene");

                foreach (var receipt in receipts)
                {
                    Receipts.Add (receipt);
                    ReceiptsFilter?.Add (receipt);

                    if (int.TryParse (receipt.Radnik, out int id))
                    {
                        var radnik = await db.Radnici.FirstOrDefaultAsync (x => x.IdRadnika == id);
                        receipt.RadnikName = radnik?.Radnik ?? string.Empty;
                    }
                }

              
            

                if (selected != null)
                {
                    SelectedReceipt = selected;
                    Debug.WriteLine ("Poziv LoadReceiptItems");
                    await LoadReceiptItems (selected);
                    Debug.WriteLine ("LoadReceiptItems završen");
                }
                else
                {
                    SelectedReceipt = ReceiptsFilter?.FirstOrDefault ();
                    await LoadReceiptItems (SelectedReceipt);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine ("EXCEPTION u LoadReceiptsAsync:");
                Debug.WriteLine (ex);
            }

            Debug.WriteLine ("=== LoadReceiptsAsync END ===");
        }





        public void FilterItems(string? searchtext)
        {
            Debug.WriteLine("Okida search");

            var lowerSearch = (searchtext ?? string.Empty).ToLower();

            var filtered = Receipts
                .Where(a =>
                    (a.BrojRacuna.ToString() ?? string.Empty).ToLower().Contains(lowerSearch) ||
                    (a.Datum.ToString("dd.MM.yyyy") ?? string.Empty).ToLower().Contains(lowerSearch) ||
                    (a.BrojFiskalnogRacuna ?? string.Empty).ToLower().Contains(lowerSearch) ||
                    (a.Kupac ?? string.Empty).ToLower().Contains(lowerSearch)
                )
                .ToList();

            ReceiptsFilter = new ObservableCollection<DatabaseTables.TblRacuni>(filtered);
        }



        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
