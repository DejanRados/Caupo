using Caupo.Data;
using Caupo.Fiscal;
using Caupo.Fiscal.Common;
using Caupo.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{
    public class ReceiptsViewModel : INotifyPropertyChanged
    {
        // ============================================================
        // RAČUNI
        // ============================================================

        public ObservableCollection<TblRacuni> Receipts { get; set; } =
            new ObservableCollection<TblRacuni> ();

        public ObservableCollection<TblRacunStavka> ReceiptItems { get; set; } =
            new ObservableCollection<TblRacunStavka> ();


        // ============================================================
        // NEUTRALNE STAVKE ZA FISKALIZACIJU
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
        // ODABRANI RAČUN
        // ============================================================

        private TblRacuni? _selectedReceipt;

        public TblRacuni? SelectedReceipt
        {
            get => _selectedReceipt;
            set
            {
                if(_selectedReceipt != value)
                {
                    _selectedReceipt = value;
                    OnPropertyChanged (nameof (SelectedReceipt));
                }
            }
        }


        // ============================================================
        // FILTER
        // ============================================================

        private ObservableCollection<TblRacuni> _receiptsFilter =
            new ObservableCollection<TblRacuni> ();

        public ObservableCollection<TblRacuni> ReceiptsFilter
        {
            get => _receiptsFilter;
            set
            {
                _receiptsFilter = value;
                OnPropertyChanged (nameof (ReceiptsFilter));
            }
        }


        private string? _searchText;

        public string? SearchText
        {
            get => _searchText;
            set
            {
                if(_searchText != value)
                {
                    _searchText = value;

                    OnPropertyChanged (nameof (SearchText));

                    Debug.WriteLine (
                        $"SearchText changed to: {_searchText}");

                    FilterItems (_searchText);
                }
            }
        }


        // ============================================================
        // IZNOS RAČUNA
        // ============================================================

        private decimal? _iznosRacuna = 0m;

        public decimal? IznosRacuna
        {
            get => _iznosRacuna;
            set
            {
                _iznosRacuna = value;
                OnPropertyChanged (nameof (IznosRacuna));
            }
        }


        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public ReceiptsViewModel()
        {
            _ = Start ();
        }


        private async Task Start()
        {
            await LoadReceiptsAsync (null);
        }


        // ============================================================
        // UČITAVANJE STAVKI RAČUNA
        // ============================================================

        public async Task LoadReceiptItems(
            TblRacuni? selectedRacun)
        {
            if(selectedRacun == null)
            {
                ReceiptItems.Clear ();
                StavkeRacuna.Clear ();
                IznosRacuna = 0m;

                return;
            }

            try
            {
                using var db =
                    new AppDbContext ();

                IznosRacuna = 0m;


                var receiptItemsFromDb =
                    await db.RacunStavka
                        .Where (
                            a =>
                                a.BrojRacuna ==
                                selectedRacun.BrojRacuna)
                        .OrderBy (
                            a =>
                                a.IdStavke)
                        .ToListAsync ();


                var tempReceiptItems =
                    new List<TblRacunStavka> ();

                var tempStavkeRacuna =
                    new List<RacunStavka> ();


                foreach(var ri in receiptItemsFromDb)
                {
                    tempReceiptItems.Add (
                        ri);

                    IznosRacuna +=
                        ri.Iznos;


                    var stavka =
                        new RacunStavka
                        {
                            Name =
                                ri.ArtiklNormativ,

                            Sifra =
                                ri.Sifra,

                            BrojRacuna =
                                ri.BrojRacuna,

                            Naziv =
                                ri.Artikl,

                            UnitPrice =
                                ri.Cijena,

                            Proizvod =
                                ri.VrstaArtikla,

                            JedinicaMjere =
                                ri.JedinicaMjere,

                            Quantity =
                                ri.Kolicina,

                            // Neutralni lokalni ID poreske stope.
                            // Nema više Labels / TaxLabel.
                            PoreskaStopa =
                                ri.PoreskaStopa
                        };


                    tempStavkeRacuna.Add (
                        stavka);
                }


                ReceiptItems.Clear ();

                foreach(var item in
                        tempReceiptItems)
                {
                    ReceiptItems.Add (
                        item);
                }


                StavkeRacuna.Clear ();

                foreach(var item in
                        tempStavkeRacuna)
                {
                    StavkeRacuna.Add (
                        item);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    "[RECEIPTS] LoadReceiptItems greška:");

                Debug.WriteLine (
                    ex);
            }
        }


        // ============================================================
        // UČITAVANJE RAČUNA
        // ============================================================

        public async Task LoadReceiptsAsync(
            TblRacuni? selected)
        {
            Debug.WriteLine (
                "=== LoadReceiptsAsync START ===");

            try
            {
                using var db =
                    new AppDbContext ();


                var receipts =
                    await db.Racuni
                        .ToListAsync ();


                Receipts.Clear ();
                ReceiptsFilter.Clear ();


                foreach(var receipt in receipts)
                {
                    if(int.TryParse (
                        receipt.Radnik,
                        out int id))
                    {
                        var radnik =
                            await db.Radnici
                                .FirstOrDefaultAsync (
                                    x =>
                                        x.IdRadnika == id);

                        receipt.RadnikName =
                            radnik?.Radnik
                            ?? string.Empty;
                    }


                    Receipts.Add (
                        receipt);

                    ReceiptsFilter.Add (
                        receipt);
                }


                if(selected != null)
                {
                    SelectedReceipt =
                        Receipts.FirstOrDefault (
                            r =>
                                r.BrojRacuna ==
                                selected.BrojRacuna)
                        ?? selected;
                }
                else
                {
                    SelectedReceipt =
                        ReceiptsFilter.FirstOrDefault ();
                }


                await LoadReceiptItems (
                    SelectedReceipt);
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    "EXCEPTION u LoadReceiptsAsync:");

                Debug.WriteLine (
                    ex);
            }

            Debug.WriteLine (
                "=== LoadReceiptsAsync END ===");
        }


        // ============================================================
        // FILTER
        // ============================================================

        public void FilterItems(
            string? searchText)
        {
            string lowerSearch =
                (searchText ?? string.Empty)
                    .ToLower ();


            var filtered =
                Receipts
                    .Where (
                        a =>
                            a.BrojRacuna
                                .ToString ()
                                .ToLower ()
                                .Contains (lowerSearch)

                            ||

                            a.Datum
                                .ToString ("dd.MM.yyyy")
                                .ToLower ()
                                .Contains (lowerSearch)

                            ||

                            (a.BrojFiskalnogRacuna
                                ?? string.Empty)
                                .ToLower ()
                                .Contains (lowerSearch)

                            ||

                            (a.Kupac
                                ?? string.Empty)
                                .ToLower ()
                                .Contains (lowerSearch))
                    .ToList ();


            ReceiptsFilter =
                new ObservableCollection<TblRacuni> (
                    filtered);
        }


        // ============================================================
        // KOPIJA RAČUNA
        // ============================================================

        public async Task<FiscalResult> IzdajKopijuAsync()
        {
            if(SelectedReceipt == null)
            {
                return FiscalResult.Failed (
                    "Nije odabran račun.");
            }


            if(StavkeRacuna.Count == 0)
            {
                return FiscalResult.Failed (
                    "Odabrani račun nema stavki.");
            }


            return await IzdajPostojeciRacunAsync (
                invoiceType: "Copy",
                transactionType: "Sale");
        }


        // ============================================================
        // STORNO / REFUND
        // ============================================================

        public async Task<FiscalResult> StornirajRacunAsync()
        {
            if(SelectedReceipt == null)
            {
                return FiscalResult.Failed (
                    "Nije odabran račun.");
            }


            if(StavkeRacuna.Count == 0)
            {
                return FiscalResult.Failed (
                    "Odabrani račun nema stavki.");
            }


            return await IzdajPostojeciRacunAsync (
                invoiceType: "Training",
                transactionType: "Refund");
        }


        // ============================================================
        // ZAJEDNIČKI COPY / REFUND
        // ============================================================

        private async Task<FiscalResult> IzdajPostojeciRacunAsync(
            string invoiceType,
            string transactionType)
        {
            try
            {
                if(SelectedReceipt == null)
                {
                    return FiscalResult.Failed (
                        "Nije odabran račun.");
                }


                TblKupci? kupac = null;


                if(!string.IsNullOrWhiteSpace (
                    SelectedReceipt.Kupac))
                {
                    using var db =
                        new AppDbContext ();

                    string nazivKupca =
                        SelectedReceipt.Kupac;


                    kupac =
                        await db.Kupci
                            .FirstOrDefaultAsync (
                                k =>
                                    k.Kupac ==
                                    nazivKupca);
                }


                FiscalBuyer? fiscalBuyer =
                    null;


                if(kupac != null)
                {
                    fiscalBuyer =
                        new FiscalBuyer
                        {
                            Name =
                                kupac.Kupac,

                            TaxId =
                                kupac.JIB,

                            Address =
                                kupac.Adresa,

                            City =
                                kupac.Mjesto
                        };
                }


                FiscalPaymentType paymentType =
                    MapPaymentType (
                        SelectedReceipt.NacinPlacanja);


                var request =
                    new FiscalRequest
                    {
                        Items =
                            StavkeRacuna.ToList (),

                        Buyer =
                            fiscalBuyer,

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
                            IznosRacuna ?? 0m,

                        InvoiceType =
                            invoiceType,

                        TransactionType =
                            transactionType,

                        ReferentDocumentNumber =
                            SelectedReceipt
                                .BrojFiskalnogRacuna,

                        ReferentDocumentDateTime =
                            SelectedReceipt
                                .Datum
                    };


                IFiscalService fiscalService =
                    FiscalServiceFactory.Create (
                        Properties.Settings
                            .Default.Country);


                FiscalResult result =
                    await fiscalService
                        .IzdajRacunAsync (
                            request);


                Debug.WriteLine (
                    $"[RECEIPTS FISCAL] " +
                    $"InvoiceType={invoiceType}, " +
                    $"TransactionType={transactionType}, " +
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
                    "[RECEIPTS FISCAL] Greška:");

                Debug.WriteLine (
                    ex);


                return FiscalResult.Failed (
                    ex.Message);
            }
        }


        // ============================================================
        // NAČIN PLAĆANJA
        // ============================================================

        private static FiscalPaymentType MapPaymentType(
            int? paymentType)
        {
            return paymentType switch
            {
                0 =>
                    FiscalPaymentType.Cash,

                1 =>
                    FiscalPaymentType.Card,

                2 =>
                    FiscalPaymentType.Check,

                3 =>
                    FiscalPaymentType.WireTransfer,

                4 =>
                    FiscalPaymentType.Other,

                _ =>
                    throw new FiscalException (
                        $"Nepoznat način plaćanja: {paymentType}.")
            };
        }


        // ============================================================
        // ERROR EVENT
        // ============================================================

        public event EventHandler<string?>?
            ErrorOccurred;


        protected virtual void OnErrorOccurred(
            string? message)
        {
            ErrorOccurred?.Invoke (
                this,
                message);
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