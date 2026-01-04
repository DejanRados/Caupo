using Caupo.Data;
using Caupo.Fiscal;
using Caupo.Models;
using Caupo.Properties;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{
    public class OrderViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<FiskalniRacun.Item> _stavkeRacuna = new ObservableCollection<FiskalniRacun.Item>();
        public ObservableCollection<FiskalniRacun.Item> StavkeRacuna
        {
            get => _stavkeRacuna;
            set
            {
                _stavkeRacuna = value;
                OnPropertyChanged(nameof(StavkeRacuna));


            }
        }

        private ObservableCollection<DatabaseTables.TblNarudzbe> _narudzbe =  new ObservableCollection<TblNarudzbe>();

        public ObservableCollection<DatabaseTables.TblNarudzbe> Narudzbe
        {
            get => _narudzbe;
            set
            {
                _narudzbe = value;
                OnPropertyChanged(nameof(Narudzbe));


            }
        }

        private ObservableCollection<DatabaseTables.TblNarudzbeStavke> _narudzbeStavke = new ObservableCollection<TblNarudzbeStavke>();

        public ObservableCollection<DatabaseTables.TblNarudzbeStavke> NarudzbeStavke
        {
            get => _narudzbeStavke;
            set
            {
                _narudzbeStavke = value;
                OnPropertyChanged(nameof(NarudzbeStavke));


            }
        }


        private TblNarudzbeStavke? _selectedStavka;
        public TblNarudzbeStavke? SelectedStavka
        {
            get { return _selectedStavka; }
            set
            {
                _selectedStavka = value;
                OnPropertyChanged(nameof(SelectedStavka));
            }
        }
        private TblNarudzbeStavke? _selectedStavkaRacunGost;
        public TblNarudzbeStavke? SelectedStavkaRacunGost
        {
            get { return _selectedStavkaRacunGost; }
            set
            {
                _selectedStavkaRacunGost = value;
                OnPropertyChanged(nameof(SelectedStavkaRacunGost));
            }
        }



        private ObservableCollection<DatabaseTables.TblNarudzbeStavke> _gostRacunStavke = new ObservableCollection<TblNarudzbeStavke>();
        public ObservableCollection<DatabaseTables.TblNarudzbeStavke> GostRacunStavke
        {
            get => _gostRacunStavke;
            set
            {
                _gostRacunStavke = value;
                OnPropertyChanged(nameof(GostRacunStavke));


            }
        }

        private ObservableCollection<DatabaseTables.TblNarudzbeStavke> _kuhinjaStavke = new ObservableCollection<TblNarudzbeStavke>();
        public ObservableCollection<DatabaseTables.TblNarudzbeStavke> KuhinjaStavke
        {
            get => _kuhinjaStavke;
            set
            {
                _kuhinjaStavke = value;
                OnPropertyChanged(nameof(KuhinjaStavke));


            }
        }

        private ObservableCollection<DatabaseTables.TblNarudzbeStavke> _sankStavke = new ObservableCollection<TblNarudzbeStavke>();
        public ObservableCollection<DatabaseTables.TblNarudzbeStavke> SankStavke
        {
            get => _sankStavke;
            set
            {
                _sankStavke = value;
                OnPropertyChanged(nameof(SankStavke));


            }
        }

        private string? _imagePathReceiptButton;
        public string? ImagePathReceiptButton
        {
            get { return _imagePathReceiptButton; }
            set
            {
                _imagePathReceiptButton = value;
                OnPropertyChanged(nameof(ImagePathReceiptButton));
            }
        }

        private string? _imagePathSaveButton;
        public string? ImagePathSaveButton
        {
            get { return _imagePathSaveButton; }
            set
            {
                _imagePathSaveButton = value;
                OnPropertyChanged(nameof(ImagePathSaveButton));
            }
        }

        private string? _imagePathDeleteButton;
        public string? ImagePathDeleteButton
        {
            get { return _imagePathDeleteButton; }
            set
            {
                _imagePathDeleteButton = value;
                OnPropertyChanged(nameof(ImagePathDeleteButton));
            }
        }

    

        private int? _idStola;
        public int? IdStola
        {
            get { return _idStola; }
            set
            {
                if (_idStola != value)
                {
                    _idStola = value;
                    OnPropertyChanged(nameof(IdStola));
                }
            }
        }
        private string? _imeStola;
        public string? ImeStola
        {
            get { return _imeStola; }
            set
            {
                if (_imeStola != value)
                {
                    _imeStola = value;
                    OnPropertyChanged(nameof(ImeStola));
                }
            }
        }

        private string? _sala;
        public string? Sala
        {
            get { return _sala; }
            set
            {
                if (_sala != value)
                {
                    _sala = value;
                    OnPropertyChanged(nameof(Sala));
                }
            }
        }
        private string? _imagePathFiskalniButton;
        public string? ImagePathFiskalniButton
        {
            get { return _imagePathFiskalniButton; }
            set
            {
                _imagePathFiskalniButton = value;
                OnPropertyChanged(nameof(ImagePathFiskalniButton));
            }
        }

        private decimal? _totalSum;
        public decimal? TotalSum
        {
            get => _totalSum;
            set
            {
                if (_totalSum != value)
                {
                    _totalSum = value;
                    OnPropertyChanged(nameof(TotalSum));
                }
            }
        }

        private decimal? _totalSumGostRacun;
        public decimal? TotalSumGostRacun
        {
            get => _totalSumGostRacun;
            set
            {
                if (_totalSumGostRacun != value)
                {
                    _totalSumGostRacun = value;
                    OnPropertyChanged(nameof(TotalSumGostRacun));
                }
            }
        }

        private ObservableCollection<TblKupci>? _kupci;
        public ObservableCollection<TblKupci>? Kupci
        {
            get => _kupci;
            set
            {
                _kupci = value;
                OnPropertyChanged(nameof(Kupci));

            }
        }

        private TblKupci? _selectedKupac;
        public TblKupci? SelectedKupac
        {
            get { return _selectedKupac; }
            set
            {
                _selectedKupac = value;
                OnPropertyChanged(nameof(SelectedKupac));
            }
        }

        public async Task UpdateTotalSum()
        {
            await Task.Delay(1);
            TotalSum = Math.Round(NarudzbeStavke.Sum(item => item.TotalAmount ?? 0), 2);
            TotalSumGostRacun = Math.Round(GostRacunStavke.Sum(item => item.TotalAmount ?? 0), 2);
        }

        // public ICommand PrebaciStavkuCommand { get; set; }
        private OrdersViewModel? _ordersViewModel;

        public OrderViewModel(OrdersViewModel? ordersViewModel)
        {
            _ordersViewModel = ordersViewModel;

            _idStola = _ordersViewModel?.IdStola;
            _imeStola = _ordersViewModel?.ImeStola;
            _sala = _ordersViewModel?.Sala;
            StavkeRacuna = _ordersViewModel?.StavkeRacuna ?? new ObservableCollection<FiskalniRacun.Item>();

            NarudzbeStavke = new ObservableCollection<TblNarudzbeStavke>();
            GostRacunStavke = new ObservableCollection<TblNarudzbeStavke>();
            Kupci = new ObservableCollection<TblKupci>();
            Start();

            SelectedKupac = Kupci.FirstOrDefault();
        }

        public async void Start()
        {
            Debug.WriteLine("U ViewModelu imamo sala za ubaciti u bazu: " + _sala);
            await LoadOrdersItemsAsync(_idStola, _sala);
            await LoadKupciAsync();
            await SetColors();
            await UpdateTotalSum();
        }

        public async Task LoadKupciAsync()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var kupci = await db.Kupci
                        .Where(k => !string.IsNullOrEmpty(k.Kupac)) 
                        .ToListAsync();

                    Kupci?.Clear();

                    foreach (var kupac in kupci)
                    {
                        Kupci?.Add(kupac);
                    }
                    SelectedKupac = Kupci.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
        public decimal KolicinaZaPrebaciti { get; set; } = 1;

        private bool CanPrebaciStavku()
        {
            return SelectedStavka != null && KolicinaZaPrebaciti > 0;
        }

        public async Task PrebaciStavku(TblNarudzbeStavke stavka, decimal kolicina)
        {
            SelectedStavka = stavka;
            if (kolicina > stavka.Quantity) {
             kolicina = (decimal)stavka.Quantity;
            }

            var existingItem = GostRacunStavke.FirstOrDefault(s => s.Name == stavka.Name);
            Debug.WriteLine($"{existingItem?.Name}");
            if (existingItem != null)
            {

                existingItem.Quantity += kolicina;
               
                Debug.WriteLine($"{existingItem?.Quantity}");
           
             
            }
            else
            {
                var stavkaZaPrebaciti = new TblNarudzbeStavke
                {
                    Name = stavka.Name,
                    Label = string.Join(", ", stavka.Label),
 
                    UnitPrice = stavka.UnitPrice,
                    Quantity = kolicina,
                    BrojRacuna = stavka.BrojRacuna,
                    Sifra = stavka.Sifra,
                    Proizvod = stavka.Proizvod,
                    JedinicaMjere = stavka.JedinicaMjere,
                    Naziv = stavka.Naziv,
                    Printed = stavka.Printed,
                    Konobar = Globals.ulogovaniKorisnik.IdRadnika.ToString(),
                    IdNarudzbe = stavka.IdNarudzbe
                };
               

                GostRacunStavke.Add(stavkaZaPrebaciti);
               
            }
            stavka.Quantity -= kolicina;
            Debug.WriteLine("---------------- stavka.Quantity   -------------" + stavka.Quantity);
            if (stavka.Quantity == 0)
            {
                NarudzbeStavke.Remove(stavka);
            }
           await UpdateTotalSum();
        }

        public async void VratiStavku(TblNarudzbeStavke stavka, decimal kolicina)
        {
            SelectedStavka = stavka;
            if (kolicina > stavka.Quantity)
            {
                kolicina = (decimal)stavka.Quantity;
            }

            var existingItem = NarudzbeStavke.FirstOrDefault(s => s.Name == stavka.Name);
            Debug.WriteLine($"{existingItem?.Name}");
            if (existingItem != null)
            {

                existingItem.Quantity += kolicina;
                Debug.WriteLine($"{existingItem?.Quantity}");


            }
            else
            {
                var stavkaZaPrebaciti = new TblNarudzbeStavke
                {
                    Name = stavka.Name,
                    Label = string.Join(", ", stavka.Label),

                    UnitPrice = stavka.UnitPrice,
                    Quantity = kolicina,
                    BrojRacuna = stavka.BrojRacuna,
                    Sifra = stavka.Sifra,
                    Proizvod = stavka.Proizvod,
                    JedinicaMjere = stavka.JedinicaMjere,
                    Naziv = stavka.Naziv,
                    Printed = stavka.Printed,
                    Konobar = Globals.ulogovaniKorisnik.IdRadnika.ToString(),
                    IdNarudzbe = stavka.IdNarudzbe
                };


                NarudzbeStavke.Add(stavkaZaPrebaciti);

            }
            stavka.Quantity -= kolicina;
            Debug.WriteLine("---------------- stavka.Quantity   -------------" + stavka.Quantity);
            if (stavka.Quantity == 0)
            {
                GostRacunStavke.Remove(stavka);
            }
            await UpdateTotalSum();
        }

        public async Task SetColors()
        {
            await Task.Delay(1);
            Debug.WriteLine("SetColors");
            string tema = Settings.Default.Tema;

            if (tema == "Tamna")
            {
                ImagePathReceiptButton = "pack://application:,,,/Images/Dark/receipt.svg";
                ImagePathSaveButton = "pack://application:,,,/Images/Dark/save.png";
                ImagePathDeleteButton = "pack://application:,,,/Images/Dark/delete.png";
        


            }
            else
            {
                ImagePathReceiptButton = "pack://application:,,,/Images/Light/receipt.svg";
                ImagePathSaveButton = "pack://application:,,,/Images/Light/save.png";
                ImagePathDeleteButton = "pack://application:,,,/Images/Light/delete.png";

                //FontColorAdv = new System.Windows.Media.Color();
                //FontColorAdv = System.Windows.Media.Color.FromRgb(50, 50, 50);
            }
           
        }

     

        public async Task LoadOrdersItemsAsync(int? ID, string sala)
        {
            try
            {
                using (var db = new AppDbContext())
                {

                    var SankStavke = StavkeRacuna .Where (item => item.Proizvod == 0 && item.Printed != "DA") .ToList ();

                    var KuhinjaStavke = StavkeRacuna.Where(item => item.Proizvod == 1 && item.Printed != "DA").ToList();

                    if (KuhinjaStavke.Any())
                    {
                        var printer = new BlokPrinter(KuhinjaStavke, "Kuhinja", _idStola?.ToString() ?? "0", _imeStola);
                        await printer.Print();
                    }

                    if (SankStavke.Any())
                    {
                        var printer = new BlokPrinter(SankStavke, "Sank", _idStola?.ToString() ?? "0", _imeStola);
                        await printer.Print();
                    }

                    // Snimi sve stavke iz StavkeRacuna u bazu
                    foreach (var item in StavkeRacuna)
                    {
                        var narudzbaStavka = new TblNarudzbeStavke
                        {
                            Name = item.Name,
                            Label = string.Join (", ", item.Labels),
                            UnitPrice = item.UnitPrice,
                            Quantity = item.Quantity,
                            BrojRacuna = item.BrojRacuna,
                            Sifra = item.Sifra,
                            Proizvod = item.Proizvod,
                            JedinicaMjere = item.JedinicaMjere,
                            Naziv = item.Naziv,
                            Printed = "DA",
                            Konobar = Globals.ulogovaniKorisnik.IdRadnika.ToString (),
                            IdNarudzbe = ID,
                            Sala = sala
                        };
                        db.NarudzbeStavke.Add (narudzbaStavka);
                    }

                    await db.SaveChangesAsync ();

                    StavkeRacuna.Clear();

                    var groupedData = (await db.NarudzbeStavke
                             .Where(ns => ns.IdNarudzbe == ID && ns.Sala == sala) 
                             .ToListAsync())
                             .GroupBy(x => x.Naziv)
                             .Select(g => new
                             {
                                 Naziv = g.Key,
                                 Quantity = g.Sum(x => x.Quantity),
                                 UnitPrice = g.First().UnitPrice
                             })
                             .ToList();



                    NarudzbeStavke.Clear();

                    foreach (var item in groupedData)
                    {
                        
                        var first = await db.NarudzbeStavke
                            .Where(x => x.Naziv == item.Naziv && x.IdNarudzbe == ID && x.Sala == sala)
                            .FirstOrDefaultAsync();

                        if (first != null)
                        {
                            NarudzbeStavke.Add(new TblNarudzbeStavke
                            {
                                Name = first.Name,
                                Label = first.Label,
                                UnitPrice = item.UnitPrice,
                                Quantity = item.Quantity,
                                BrojRacuna = first.BrojRacuna,
                                Sifra = first.Sifra,
                                Proizvod = first.Proizvod,
                                JedinicaMjere = first.JedinicaMjere,
                                Naziv = first.Naziv,
                                Printed = first.Printed,
                                Konobar = first.Konobar,
                                IdNarudzbe = first.IdNarudzbe,
                                Sala = first.Sala,
                            });
                        }
                    }
                }
               await UpdateTotalSum ();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public async Task KreirajStavkeRacunaPodjela()
        {
            await Task.Delay(1);
            foreach(var item in GostRacunStavke)
            {
                FiskalniRacun.Item stavka = new FiskalniRacun.Item();
                stavka.Name = item.Name;
                stavka.Sifra = item.Sifra;
                stavka.BrojRacuna = 222;
                stavka.Naziv = item.Naziv;
                stavka.Labels.Add (TaxLabel (item.Sifra.ToString ()));
                stavka.UnitPrice = (decimal?)item.UnitPrice;
                stavka.Proizvod = item.Proizvod;
                stavka.Printed = item.Printed;
                stavka.JedinicaMjere = item.JedinicaMjere;
                stavka.Quantity = item.Quantity;
                StavkeRacuna.Add(stavka);
            }
            await UpdateTotalSum ();
        }

        string? TaxLabel(string sifra)
        {
            using (var db = new AppDbContext ())
            {


                var ps = db.Artikli
                    .Where (a => a.Sifra == sifra)
                    .Select (a => a.PoreskaStopa)
                    .FirstOrDefault ();

                string taxeslabel;
                switch (ps.ToString ())
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

        }
        public async Task KreirajStavkeRacunaUkupno()
        {
            await Task.Delay(1);
            foreach (var item in NarudzbeStavke)
            {
                FiskalniRacun.Item stavka = new FiskalniRacun.Item();
                stavka.Name = item.Name;
                stavka.Sifra = item.Sifra;
                stavka.BrojRacuna = 222;
                stavka.Naziv = item.Naziv;
                stavka.Labels.Add (TaxLabel (item.Sifra.ToString ()));
                stavka.UnitPrice = (decimal?)item.UnitPrice;
                stavka.Proizvod = item.Proizvod;
                stavka.Printed = item.Printed;
                stavka.JedinicaMjere = item.JedinicaMjere;
                stavka.Quantity = item.Quantity;
                StavkeRacuna.Add(stavka);
            }
            await UpdateTotalSum ();
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


}
