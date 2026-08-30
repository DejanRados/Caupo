using Caupo.Data;
using Caupo.Fiscal;
using Caupo.Fiscal.Common;
using Caupo.Models;
using Caupo.Properties;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{
    public class OrderViewModel : INotifyPropertyChanged
    {
        // ============================================================
        // STAVKE ZA FISKALIZACIJU
        // ============================================================

        private ObservableCollection<RacunStavka> _stavkeRacuna =
            new ObservableCollection<RacunStavka> ();

        public ObservableCollection<RacunStavka> StavkeRacuna
        {
            get => _stavkeRacuna;
            set
            {
                _stavkeRacuna = value;
                OnPropertyChanged (nameof (StavkeRacuna));
            }
        }


        // ============================================================
        // NARUDŽBE
        // ============================================================

        private ObservableCollection<TblNarudzbe> _narudzbe =
            new ObservableCollection<TblNarudzbe> ();

        public ObservableCollection<TblNarudzbe> Narudzbe
        {
            get => _narudzbe;
            set
            {
                _narudzbe = value;
                OnPropertyChanged (nameof (Narudzbe));
            }
        }


        private ObservableCollection<TblNarudzbeStavke> _narudzbeStavke =
            new ObservableCollection<TblNarudzbeStavke> ();

        public ObservableCollection<TblNarudzbeStavke> NarudzbeStavke
        {
            get => _narudzbeStavke;
            set
            {
                _narudzbeStavke = value;
                OnPropertyChanged (nameof (NarudzbeStavke));
            }
        }


        private TblNarudzbeStavke? _selectedStavka;

        public TblNarudzbeStavke? SelectedStavka
        {
            get => _selectedStavka;
            set
            {
                _selectedStavka = value;
                OnPropertyChanged (nameof (SelectedStavka));
            }
        }


        private TblNarudzbeStavke? _selectedStavkaRacunGost;

        public TblNarudzbeStavke? SelectedStavkaRacunGost
        {
            get => _selectedStavkaRacunGost;
            set
            {
                _selectedStavkaRacunGost = value;
                OnPropertyChanged (nameof (SelectedStavkaRacunGost));
            }
        }


        // ============================================================
        // GOST RAČUN
        // ============================================================

        private ObservableCollection<TblNarudzbeStavke> _gostRacunStavke =
            new ObservableCollection<TblNarudzbeStavke> ();

        public ObservableCollection<TblNarudzbeStavke> GostRacunStavke
        {
            get => _gostRacunStavke;
            set
            {
                _gostRacunStavke = value;
                OnPropertyChanged (nameof (GostRacunStavke));
            }
        }


        // ============================================================
        // KUHINJA / ŠANK
        // ============================================================

        private ObservableCollection<TblNarudzbeStavke> _kuhinjaStavke =
            new ObservableCollection<TblNarudzbeStavke> ();

        public ObservableCollection<TblNarudzbeStavke> KuhinjaStavke
        {
            get => _kuhinjaStavke;
            set
            {
                _kuhinjaStavke = value;
                OnPropertyChanged (nameof (KuhinjaStavke));
            }
        }


        private ObservableCollection<TblNarudzbeStavke> _sankStavke =
            new ObservableCollection<TblNarudzbeStavke> ();

        public ObservableCollection<TblNarudzbeStavke> SankStavke
        {
            get => _sankStavke;
            set
            {
                _sankStavke = value;
                OnPropertyChanged (nameof (SankStavke));
            }
        }


        // ============================================================
        // IKONE
        // ============================================================

        private string? _imagePathReceiptButton;

        public string? ImagePathReceiptButton
        {
            get => _imagePathReceiptButton;
            set
            {
                _imagePathReceiptButton = value;
                OnPropertyChanged (nameof (ImagePathReceiptButton));
            }
        }


        private string? _imagePathSaveButton;

        public string? ImagePathSaveButton
        {
            get => _imagePathSaveButton;
            set
            {
                _imagePathSaveButton = value;
                OnPropertyChanged (nameof (ImagePathSaveButton));
            }
        }


        private string? _imagePathDeleteButton;

        public string? ImagePathDeleteButton
        {
            get => _imagePathDeleteButton;
            set
            {
                _imagePathDeleteButton = value;
                OnPropertyChanged (nameof (ImagePathDeleteButton));
            }
        }


        private string? _imagePathFiskalniButton;

        public string? ImagePathFiskalniButton
        {
            get => _imagePathFiskalniButton;
            set
            {
                _imagePathFiskalniButton = value;
                OnPropertyChanged (nameof (ImagePathFiskalniButton));
            }
        }


        // ============================================================
        // STO / SALA
        // ============================================================

        private int? _idStola;

        public int? IdStola
        {
            get => _idStola;
            set
            {
                if(_idStola != value)
                {
                    _idStola = value;
                    OnPropertyChanged (nameof (IdStola));
                }
            }
        }


        private string? _imeStola;

        public string? ImeStola
        {
            get => _imeStola;
            set
            {
                if(_imeStola != value)
                {
                    _imeStola = value;
                    OnPropertyChanged (nameof (ImeStola));
                }
            }
        }


        private string? _sala;

        public string? Sala
        {
            get => _sala;
            set
            {
                if(_sala != value)
                {
                    _sala = value;
                    OnPropertyChanged (nameof (Sala));
                }
            }
        }


        // ============================================================
        // UKUPNO
        // ============================================================

        private decimal? _totalSum;

        public decimal? TotalSum
        {
            get => _totalSum;
            set
            {
                if(_totalSum != value)
                {
                    _totalSum = value;
                    OnPropertyChanged (nameof (TotalSum));
                }
            }
        }


        private decimal? _totalSumGostRacun;

        public decimal? TotalSumGostRacun
        {
            get => _totalSumGostRacun;
            set
            {
                if(_totalSumGostRacun != value)
                {
                    _totalSumGostRacun = value;
                    OnPropertyChanged (nameof (TotalSumGostRacun));
                }
            }
        }


        // ============================================================
        // KUPCI
        // ============================================================

        private ObservableCollection<TblKupci>? _kupci;

        public ObservableCollection<TblKupci>? Kupci
        {
            get => _kupci;
            set
            {
                _kupci = value;
                OnPropertyChanged (nameof (Kupci));
            }
        }


        private TblKupci? _selectedKupac;

        public TblKupci? SelectedKupac
        {
            get => _selectedKupac;
            set
            {
                _selectedKupac = value;
                OnPropertyChanged (nameof (SelectedKupac));
            }
        }


        public decimal KolicinaZaPrebaciti { get; set; } = 1;


        private readonly OrdersViewModel? _ordersViewModel;


        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public OrderViewModel(
            OrdersViewModel? ordersViewModel)
        {
            _ordersViewModel =
                ordersViewModel;

            _idStola =
                _ordersViewModel?.IdStola;

            _imeStola =
                _ordersViewModel?.ImeStola;

            _sala =
                _ordersViewModel?.Sala;

            StavkeRacuna =
                _ordersViewModel?.StavkeRacuna
                ?? new ObservableCollection<RacunStavka> ();

            NarudzbeStavke =
                new ObservableCollection<TblNarudzbeStavke> ();

            GostRacunStavke =
                new ObservableCollection<TblNarudzbeStavke> ();

            Kupci =
                new ObservableCollection<TblKupci> ();

            Start ();
        }


        // ============================================================
        // START
        // ============================================================

        public async void Start()
        {
            try
            {
                Debug.WriteLine (
                    "U ViewModelu imamo sala za ubaciti u bazu: " +
                    _sala);

                await LoadOrdersItemsAsync (
                    _idStola,
                    _sala);

                await LoadKupciAsync ();

                await SetColors ();

                await UpdateTotalSum ();
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    "[ORDER VM] Start greška: " +
                    ex);
            }
        }


        // ============================================================
        // KUPCI
        // ============================================================

        public async Task LoadKupciAsync()
        {
            try
            {
                using var db =
                    new AppDbContext ();

                var kupci =
                    await db.Kupci
                        .Where (
                            k =>
                                !string.IsNullOrEmpty (
                                    k.Kupac))
                        .ToListAsync ();

                Kupci?.Clear ();

                foreach(var kupac in kupci)
                {
                    Kupci?.Add (
                        kupac);
                }

                SelectedKupac =
                    Kupci?.FirstOrDefault ();
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    ex.ToString ());
            }
        }


        // ============================================================
        // UKUPNO
        // ============================================================

        public async Task UpdateTotalSum()
        {
            await Task.Delay (1);

            TotalSum =
                Math.Round (
                    NarudzbeStavke.Sum (
                        item =>
                            item.TotalAmount ?? 0m),
                    2);

            TotalSumGostRacun =
                Math.Round (
                    GostRacunStavke.Sum (
                        item =>
                            item.TotalAmount ?? 0m),
                    2);
        }


        // ============================================================
        // PREBACIVANJE NA GOST RAČUN
        // ============================================================

        public async Task PrebaciStavku(
            TblNarudzbeStavke stavka,
            decimal kolicina)
        {
            if(stavka == null)
                return;

            if(kolicina <= 0)
                return;

            SelectedStavka =
                stavka;

            decimal dostupnaKolicina =
                stavka.Quantity ?? 0m;

            if(kolicina >
               dostupnaKolicina)
            {
                kolicina =
                    dostupnaKolicina;
            }

            if(kolicina <= 0)
                return;


            var existingItem =
                GostRacunStavke
                    .FirstOrDefault (
                        s =>
                            s.Sifra == stavka.Sifra &&
                            s.Name == stavka.Name);


            if(existingItem != null)
            {
                existingItem.Quantity =
                    (existingItem.Quantity ?? 0m)
                    + kolicina;
            }
            else
            {
                var stavkaZaPrebaciti =
                    new TblNarudzbeStavke
                    {
                        Name =
                            stavka.Name,

                        Label =
                            stavka.Label,

                        UnitPrice =
                            stavka.UnitPrice,

                        Quantity =
                            kolicina,

                        BrojRacuna =
                            stavka.BrojRacuna,

                        Sifra =
                            stavka.Sifra,

                        Proizvod =
                            stavka.Proizvod,

                        JedinicaMjere =
                            stavka.JedinicaMjere,

                        Naziv =
                            stavka.Naziv,

                        Printed =
                            stavka.Printed,

                        Konobar =
                            Globals
                                .ulogovaniKorisnik
                                .IdRadnika
                                .ToString (),

                        IdNarudzbe =
                            stavka.IdNarudzbe,

                        Sala =
                            stavka.Sala
                    };

                GostRacunStavke.Add (
                    stavkaZaPrebaciti);
            }


            stavka.Quantity =
                dostupnaKolicina -
                kolicina;


            if((stavka.Quantity ?? 0m) <= 0m)
            {
                NarudzbeStavke.Remove (
                    stavka);
            }


            await UpdateTotalSum ();
        }


        // ============================================================
        // VRAĆANJE SA GOST RAČUNA
        // ============================================================

        public async Task VratiStavku(
            TblNarudzbeStavke stavka,
            decimal kolicina)
        {
            if(stavka == null)
                return;

            if(kolicina <= 0)
                return;

            decimal dostupnaKolicina =
                stavka.Quantity ?? 0m;

            if(kolicina >
               dostupnaKolicina)
            {
                kolicina =
                    dostupnaKolicina;
            }

            if(kolicina <= 0)
                return;


            var existingItem =
                NarudzbeStavke
                    .FirstOrDefault (
                        s =>
                            s.Sifra == stavka.Sifra &&
                            s.Name == stavka.Name);


            if(existingItem != null)
            {
                existingItem.Quantity =
                    (existingItem.Quantity ?? 0m)
                    + kolicina;
            }
            else
            {
                var stavkaZaPrebaciti =
                    new TblNarudzbeStavke
                    {
                        Name =
                            stavka.Name,

                        Label =
                            stavka.Label,

                        UnitPrice =
                            stavka.UnitPrice,

                        Quantity =
                            kolicina,

                        BrojRacuna =
                            stavka.BrojRacuna,

                        Sifra =
                            stavka.Sifra,

                        Proizvod =
                            stavka.Proizvod,

                        JedinicaMjere =
                            stavka.JedinicaMjere,

                        Naziv =
                            stavka.Naziv,

                        Printed =
                            stavka.Printed,

                        Konobar =
                            Globals
                                .ulogovaniKorisnik
                                .IdRadnika
                                .ToString (),

                        IdNarudzbe =
                            stavka.IdNarudzbe,

                        Sala =
                            stavka.Sala
                    };

                NarudzbeStavke.Add (
                    stavkaZaPrebaciti);
            }


            stavka.Quantity =
                dostupnaKolicina -
                kolicina;


            if((stavka.Quantity ?? 0m) <= 0m)
            {
                GostRacunStavke.Remove (
                    stavka);
            }


            await UpdateTotalSum ();
        }


        // ============================================================
        // BOJE
        // ============================================================

        public async Task SetColors()
        {
            await Task.Delay (1);

            string tema =
                Settings.Default.Tema;

            if(tema == "Tamna")
            {
                ImagePathReceiptButton =
                    "pack://application:,,,/Images/Dark/receipt.svg";

                ImagePathSaveButton =
                    "pack://application:,,,/Images/Dark/save.png";

                ImagePathDeleteButton =
                    "pack://application:,,,/Images/Dark/delete.png";
            }
            else
            {
                ImagePathReceiptButton =
                    "pack://application:,,,/Images/Light/receipt.svg";

                ImagePathSaveButton =
                    "pack://application:,,,/Images/Light/save.png";

                ImagePathDeleteButton =
                    "pack://application:,,,/Images/Light/delete.png";
            }
        }


        // ============================================================
        // UČITAVANJE / SNIMANJE NARUDŽBE
        // ============================================================

        public async Task LoadOrdersItemsAsync(
            int? id,
            string? sala)
        {
            try
            {
                // ----------------------------------------------------
                // 1. Print novih blokova
                // ----------------------------------------------------

                var sankStavke =
                    StavkeRacuna
                        .Where (
                            item =>
                                item.Proizvod == 0 &&
                                item.Printed != "DA")
                        .ToList ();

                var kuhinjaStavke =
                    StavkeRacuna
                        .Where (
                            item =>
                                item.Proizvod == 1 &&
                                item.Printed != "DA")
                        .ToList ();


                if(kuhinjaStavke.Any ())
                {
                    var printer =
                        new BlokPrinter (
                            kuhinjaStavke,
                            "Kuhinja",
                            _idStola?.ToString ()
                                ?? "0",
                            _imeStola ?? "");

                    await printer.Print ();
                }


                if(sankStavke.Any ())
                {
                    var printer =
                        new BlokPrinter (
                            sankStavke,
                            "Sank",
                            _idStola?.ToString ()
                                ?? "0",
                            _imeStola ?? "");

                    await printer.Print ();
                }


                // ----------------------------------------------------
                // 2. Snimi nove stavke u bazu
                // ----------------------------------------------------

                if(StavkeRacuna.Any ())
                {
                    using var db =
                        new AppDbContext ();

                    foreach(var item in
                            StavkeRacuna)
                    {
                        var narudzbaStavka =
                            new TblNarudzbeStavke
                            {
                                Name =
                                    item.Name,

                                // Lokalni ID poreske stope.
                                Label =
                                    item.PoreskaStopa
                                        ?.ToString ()
                                    ?? "",

                                UnitPrice =
                                    item.UnitPrice,

                                Quantity =
                                    item.Quantity,

                                BrojRacuna =
                                    item.BrojRacuna,

                                Sifra =
                                    item.Sifra,

                                Proizvod =
                                    item.Proizvod,

                                JedinicaMjere =
                                    item.JedinicaMjere,

                                Naziv =
                                    item.Naziv,

                                Printed =
                                    "DA",

                                Konobar =
                                    Globals
                                        .ulogovaniKorisnik
                                        .IdRadnika
                                        .ToString (),

                                IdNarudzbe =
                                    id,

                                Sala =
                                    sala
                            };

                        db.NarudzbeStavke.Add (
                            narudzbaStavka);
                    }

                    await db.SaveChangesAsync ();

                    StavkeRacuna.Clear ();
                }


                // ----------------------------------------------------
                // 3. Ponovo učitaj stanje narudžbe
                // ----------------------------------------------------

                await ReloadNarudzbeStavkeAsync ();

                await UpdateTotalSum ();
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    "[ORDER VM] LoadOrdersItemsAsync: " +
                    ex);
            }
        }


        private async Task ReloadNarudzbeStavkeAsync()
        {
            using var db =
                new AppDbContext ();


            var podaci =
                await db.NarudzbeStavke
                    .Where (
                        x =>
                            x.IdNarudzbe == IdStola &&
                            x.Sala == Sala)
                    .ToListAsync ();


            var groupedData =
                podaci
                    .GroupBy (
                        x =>
                            new
                            {
                                x.Sifra,
                                x.Naziv,
                                x.Name,
                                x.UnitPrice,
                                x.Label,
                                x.Proizvod,
                                x.JedinicaMjere
                            })
                    .Select (
                        g =>
                            new TblNarudzbeStavke
                            {
                                Name =
                                    g.Key.Name,

                                Label =
                                    g.Key.Label,

                                UnitPrice =
                                    g.Key.UnitPrice,

                                Quantity =
                                    g.Sum (
                                        x =>
                                            x.Quantity ?? 0m),

                                BrojRacuna =
                                    g.First ()
                                        .BrojRacuna,

                                Sifra =
                                    g.Key.Sifra,

                                Proizvod =
                                    g.Key.Proizvod,

                                JedinicaMjere =
                                    g.Key.JedinicaMjere,

                                Naziv =
                                    g.Key.Naziv,

                                Printed =
                                    g.First ()
                                        .Printed,

                                Konobar =
                                    g.First ()
                                        .Konobar,

                                IdNarudzbe =
                                    g.First ()
                                        .IdNarudzbe,

                                Sala =
                                    g.First ()
                                        .Sala
                            })
                    .ToList ();


            NarudzbeStavke.Clear ();

            foreach(var item in
                    groupedData)
            {
                NarudzbeStavke.Add (
                    item);
            }
        }


        // ============================================================
        // KREIRANJE NEUTRALNIH STAVKI ZA GOST RAČUN
        // ============================================================

        public async Task KreirajStavkeRacunaPodjela()
        {
            StavkeRacuna.Clear ();

            foreach(var item in
                    GostRacunStavke)
            {
                var stavka =
                    new RacunStavka
                    {
                        Name =
                            item.Name,

                        Sifra =
                            item.Sifra,

                        BrojRacuna =
                            item.BrojRacuna,

                        Naziv =
                            item.Naziv,

                        PoreskaStopa =
                            GetPoreskaStopaId (
                                item.Sifra),

                        UnitPrice =
                            item.UnitPrice,

                        Proizvod =
                            item.Proizvod,

                        Printed =
                            item.Printed,

                        JedinicaMjere =
                            item.JedinicaMjere,

                        Quantity =
                            item.Quantity
                    };

                StavkeRacuna.Add (
                    stavka);
            }

            await UpdateTotalSum ();
        }


        // ============================================================
        // KREIRANJE NEUTRALNIH STAVKI ZA CIJELI RAČUN
        // ============================================================

        public async Task KreirajStavkeRacunaUkupno()
        {
            StavkeRacuna.Clear ();

            foreach(var item in
                    NarudzbeStavke)
            {
                var stavka =
                    new RacunStavka
                    {
                        Name =
                            item.Name,

                        Sifra =
                            item.Sifra,

                        BrojRacuna =
                            item.BrojRacuna,

                        Naziv =
                            item.Naziv,

                        PoreskaStopa =
                            GetPoreskaStopaId (
                                item.Sifra),

                        UnitPrice =
                            item.UnitPrice,

                        Proizvod =
                            item.Proizvod,

                        Printed =
                            item.Printed,

                        JedinicaMjere =
                            item.JedinicaMjere,

                        Quantity =
                            item.Quantity
                    };

                StavkeRacuna.Add (
                    stavka);
            }

            await UpdateTotalSum ();
        }


        // ============================================================
        // PORESKA STOPA
        // ============================================================

        private int? GetPoreskaStopaId(
            string? sifra)
        {
            if(string.IsNullOrWhiteSpace (
                sifra))
            {
                return null;
            }

            using var db =
                new AppDbContext ();

            return db.Artikli
                .Where (
                    a =>
                        a.Sifra == sifra)
                .Select (
                    a =>
                        a.PoreskaStopa)
                .FirstOrDefault ();
        }


        // ============================================================
        // FISKALIZACIJA
        // ============================================================

        public async Task<FiscalResult> IzdajRacunAsync(
            int selectedNacinPlacanjaIndex,
            bool gostRacun)
        {
            try
            {
                if(gostRacun)
                {
                    if(GostRacunStavke.Count == 0)
                    {
                        return FiscalResult.Failed (
                            "Nema stavki za izdavanje računa.");
                    }

                    await KreirajStavkeRacunaPodjela ();
                }
                else
                {
                    if(NarudzbeStavke.Count == 0)
                    {
                        return FiscalResult.Failed (
                            "Nema stavki za izdavanje računa.");
                    }

                    await KreirajStavkeRacunaUkupno ();
                }


                if(StavkeRacuna.Count == 0)
                {
                    return FiscalResult.Failed (
                        "Nema pripremljenih stavki za fiskalizaciju.");
                }


                FiscalPaymentType paymentType =
                    selectedNacinPlacanjaIndex switch
                    {
                        0 => FiscalPaymentType.Cash,
                        1 => FiscalPaymentType.Card,
                        2 => FiscalPaymentType.Check,
                        3 => FiscalPaymentType.WireTransfer,

                        _ => throw new FiscalException (
                            "Nepoznat način plaćanja.")
                    };


                FiscalBuyer? buyer =
                    null;

                if(SelectedKupac != null)
                {
                    buyer =
                        new FiscalBuyer
                        {
                            Name =
                                SelectedKupac.Kupac,

                            TaxId =
                                SelectedKupac.JIB,

                            Address =
                                SelectedKupac.Adresa,

                            City =
                                SelectedKupac.Mjesto
                        };
                }


                var request =
                    new FiscalRequest
                    {
                        Items =
                            StavkeRacuna.ToList (),

                        Buyer =
                            buyer,

                        Cashier =
                            new FiscalCashier
                            {
                                Id =
                                    Globals
                                        .ulogovaniKorisnik
                                        .IdRadnika,

                                Name =
                                    Globals
                                        .ulogovaniKorisnik
                                        .Radnik,

                                IdentificationNumber =
                                    Globals
                                        .ulogovaniKorisnik
                                        .IB
                            },

                        PaymentType =
                            paymentType,

                        TotalAmount =
                            gostRacun
                                ? TotalSumGostRacun ?? 0m
                                : TotalSum ?? 0m,

                        InvoiceType =
                            "Normal",

                        TransactionType =
                            "Sale"
                    };


                IFiscalService fiscalService =
                    FiscalServiceFactory.Create (
                        Settings.Default.Country);


                FiscalResult result =
                    await fiscalService
                        .IzdajRacunAsync (
                            request);


                Debug.WriteLine (
                    $"[ORDER FISKALNI] " +
                    $"Success={result.Success}, " +
                    $"Fiscalized={result.Fiscalized}, " +
                    $"Saved={result.SavedToDatabase}, " +
                    $"Printed={result.Printed}, " +
                    $"FiscalNumber={result.FiscalNumber}");


                return result;
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    "[ORDER FISKALNI] Greška: " +
                    ex);

                return FiscalResult.Failed (
                    ex.Message);
            }
        }


        // ============================================================
        // ZAVRŠETAK PODIJELJENOG RAČUNA
        // ============================================================

        public async Task ZavrsiGostRacunAsync()
        {
            try
            {
                using var db =
                    new AppDbContext ();


                foreach(var gostStavka in
                        GostRacunStavke.ToList ())
                {
                    decimal preostaloZaBrisanje =
                        gostStavka.Quantity ?? 0m;

                    if(preostaloZaBrisanje <= 0m)
                        continue;


                    var dbStavke =
                        await db.NarudzbeStavke
                            .Where (
                                x =>
                                    x.IdNarudzbe == IdStola &&
                                    x.Sala == Sala &&
                                    x.Sifra == gostStavka.Sifra)
                            .OrderBy (
                                x =>
                                    x.IdStavke)
                            .ToListAsync ();


                    foreach(var dbStavka in
                            dbStavke)
                    {
                        if(preostaloZaBrisanje <= 0m)
                            break;

                        decimal dbKolicina =
                            dbStavka.Quantity ?? 0m;


                        if(preostaloZaBrisanje >=
                           dbKolicina)
                        {
                            preostaloZaBrisanje -=
                                dbKolicina;

                            db.NarudzbeStavke.Remove (
                                dbStavka);
                        }
                        else
                        {
                            dbStavka.Quantity =
                                dbKolicina -
                                preostaloZaBrisanje;

                            preostaloZaBrisanje =
                                0m;
                        }
                    }
                }


                await db.SaveChangesAsync ();


                GostRacunStavke.Clear ();

                StavkeRacuna.Clear ();


                await ReloadNarudzbeStavkeAsync ();

                await UpdateTotalSum ();
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    "[ORDER] Greška pri završavanju gost računa: " +
                    ex);

                throw;
            }
        }


        // ============================================================
        // ZAVRŠETAK CIJELOG RAČUNA
        // ============================================================

        public async Task ZavrsiCijeliRacunAsync()
        {
            try
            {
                using var db =
                    new AppDbContext ();


                var stavkeZaBrisanje =
                    await db.NarudzbeStavke
                        .Where (
                            x =>
                                x.IdNarudzbe == IdStola &&
                                x.Sala == Sala)
                        .ToListAsync ();


                if(stavkeZaBrisanje.Any ())
                {
                    db.NarudzbeStavke.RemoveRange (
                        stavkeZaBrisanje);

                    await db.SaveChangesAsync ();
                }


                StavkeRacuna.Clear ();

                GostRacunStavke.Clear ();

                NarudzbeStavke.Clear ();


                await UpdateTotalSum ();
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    "[ORDER] Greška pri završavanju cijelog računa: " +
                    ex);

                throw;
            }
        }


        // ============================================================
        // PROPERTY CHANGED
        // ============================================================

        public event PropertyChangedEventHandler?
            PropertyChanged;


        protected virtual void OnPropertyChanged(
            [CallerMemberName]
            string? propertyName = null)
        {
            PropertyChanged?.Invoke (
                this,
                new PropertyChangedEventArgs (
                    propertyName));
        }
    }
}