using Caupo.Data;
using Caupo.Fiscal;
using Caupo.Fiscal.Common;
using Caupo.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Caupo.Fiscal.Croatia;
using Caupo.Fiscal.Croatia.Helpers;
using Caupo.Fiscal.Croatia.Models;
using System.Runtime.CompilerServices;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{
    public class ReceiptsViewModel : INotifyPropertyChanged
    {
        // ============================================================
        // RAČUNI
        // ============================================================

        public ObservableCollection<TblRacuni> Receipts { get; } = new();
        public ObservableCollection<TblRacunStavka> ReceiptItems { get; } = new();

        private ObservableCollection<RacunStavka> _stavkeRacuna = new();

        public ObservableCollection<RacunStavka> StavkeRacuna
        {
            get => _stavkeRacuna;
            set
            {
                _stavkeRacuna = value;
                OnPropertyChanged();
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
                if (_selectedReceipt == value)
                    return;

                _selectedReceipt = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(CanReprint));
                OnPropertyChanged(nameof(CanIssueCopy));
                OnPropertyChanged(nameof(CanRefund));
                OnPropertyChanged(nameof(CanFiscalizeLater));
            }
        }

        // ============================================================
        // DOZVOLJENE OPERACIJE
        // ============================================================

        private string Country => Properties.Settings.Default.Country ?? string.Empty;

        private bool IsCroatia => string.Equals(Country, "Hrvatska", StringComparison.OrdinalIgnoreCase);
        private bool IsSerbia => string.Equals(Country, "Srbija", StringComparison.OrdinalIgnoreCase);
        private bool IsRs => string.Equals(Country, "RepublikaSrpska", StringComparison.OrdinalIgnoreCase);
        private bool IsFederation => string.Equals(Country, "FederacijaBiH", StringComparison.OrdinalIgnoreCase);

        private bool IsFiscalized => string.Equals(SelectedReceipt?.Fiskalizovan, "DA", StringComparison.OrdinalIgnoreCase);
        private bool IsRefunded => string.Equals(SelectedReceipt?.Reklamiran, "DA", StringComparison.OrdinalIgnoreCase);

        public bool CanReprint
        {
            get
            {
                if (SelectedReceipt == null || !IsFiscalized)
                    return false;

                return IsCroatia || IsSerbia || IsRs;
            }
        }

        public bool CanIssueCopy
        {
            get
            {
                if (SelectedReceipt == null || !IsFiscalized)
                    return false;

                return IsSerbia || IsRs;
            }
        }

        public bool CanRefund
        {
            get
            {
                if (SelectedReceipt == null || !IsFiscalized || IsRefunded)
                    return false;

                return true;
            }
        }

        public bool CanFiscalizeLater
        {
            get
            {
                if (SelectedReceipt == null)
                    return false;

                return IsCroatia && !IsFiscalized;
            }
        }

       

        // ============================================================
        // FILTER
        // ============================================================

        private ObservableCollection<TblRacuni> _receiptsFilter = new();

        public ObservableCollection<TblRacuni> ReceiptsFilter
        {
            get => _receiptsFilter;
            set
            {
                _receiptsFilter = value;
                OnPropertyChanged();
            }
        }

        private string? _searchText;

        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                    return;

                _searchText = value;
                OnPropertyChanged();

                FilterItems(_searchText);
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
                if (_iznosRacuna == value)
                    return;

                _iznosRacuna = value;
                OnPropertyChanged();
            }
        }

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public ReceiptsViewModel()
        {
            _ = Start();
        }

        private async Task Start()
        {
            await LoadReceiptsAsync(null);
        }

        // ============================================================
        // UČITAVANJE RAČUNA
        // ============================================================

        public async Task LoadReceiptsAsync(TblRacuni? selected)
        {
            try
            {
                await using var db = new AppDbContext();

                var receipts = await db.Racuni.AsNoTracking().OrderByDescending(x => x.BrojRacuna).ToListAsync();
                var workers = await db.Radnici.AsNoTracking().ToDictionaryAsync(x => x.IdRadnika, x => x.Radnik ?? string.Empty);

                foreach (var receipt in receipts)
                {
                    if (int.TryParse(receipt.Radnik, out int workerId) && workers.TryGetValue(workerId, out string? workerName))
                        receipt.RadnikName = workerName;
                    else
                        receipt.RadnikName = receipt.Radnik ?? string.Empty;
                }

                Receipts.Clear();

                foreach (var receipt in receipts)
                    Receipts.Add(receipt);

                ApplyFilter();

                if (selected != null)
                    SelectedReceipt = Receipts.FirstOrDefault(x => x.BrojRacuna == selected.BrojRacuna);
                else
                    SelectedReceipt = ReceiptsFilter.FirstOrDefault();

                await LoadReceiptItems(SelectedReceipt);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[RECEIPTS] Greška učitavanja računa: " + ex);

                Receipts.Clear();
                ReceiptsFilter.Clear();
                SelectedReceipt = null;

                await LoadReceiptItems(null);
            }
        }

        // ============================================================
        // UČITAVANJE STAVKI
        // ============================================================

        public async Task LoadReceiptItems(TblRacuni? selectedReceipt)
        {
            ReceiptItems.Clear();
            StavkeRacuna.Clear();
            IznosRacuna = 0m;

            if (selectedReceipt == null)
                return;

            try
            {
                await using var db = new AppDbContext();

                var items = await db.RacunStavka.AsNoTracking()
                    .Where(x => x.BrojRacuna == selectedReceipt.BrojRacuna)
                    .OrderBy(x => x.IdStavke)
                    .ToListAsync();

                decimal total = 0m;

                foreach (var item in items)
                {
                    ReceiptItems.Add(item);

                    total += item.Iznos ?? 0m;

                    StavkeRacuna.Add(new RacunStavka
                    {
                        Name = item.ArtiklNormativ,
                        Sifra = item.Sifra,
                        BrojRacuna = item.BrojRacuna,
                        Naziv = item.Artikl,
                        UnitPrice = item.Cijena,
                        Proizvod = item.VrstaArtikla,
                        JedinicaMjere = item.JedinicaMjere,
                        Quantity = item.Kolicina,
                        PoreskaStopa = item.PoreskaStopa,
                        PorezNaPotrosnju = item.PorezNaPotrosnju
                    });
                }

                IznosRacuna = total;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[RECEIPTS] Greška učitavanja stavki: " + ex);

                ReceiptItems.Clear();
                StavkeRacuna.Clear();
                IznosRacuna = 0m;
            }
        }

        // ============================================================
        // FILTER
        // ============================================================

        public void FilterItems(string? searchText)
        {
            _searchText = searchText;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string search = (_searchText ?? string.Empty).Trim();

            IEnumerable<TblRacuni> query = Receipts;

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.BrojRacuna.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.Datum.ToString("dd.MM.yyyy").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (x.BrojFiskalnogRacuna ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (x.Kupac ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            ReceiptsFilter = new ObservableCollection<TblRacuni>(query);
        }

        // ============================================================
        // PONOVNA ŠTAMPA
        // ============================================================

        public async Task<FiscalResult> PonovoStampajAsync()
        {
            try
            {
                if (SelectedReceipt == null)
                    return FiscalResult.Failed("Nije odabran račun.");

                if (!IsFiscalized)
                    return FiscalResult.Failed("Račun nije fiskalizovan.");

                if (StavkeRacuna.Count == 0)
                    return FiscalResult.Failed("Odabrani račun nema stavki.");

                if (!IsCroatia)
                    return FiscalResult.Failed("Ponovna štampa trenutno je dostupna samo za Hrvatsku.");

                if (string.IsNullOrWhiteSpace(SelectedReceipt.BrojRacunaHr))
                    return FiscalResult.Failed("Odabrani račun nema spremljen hrvatski broj računa.");

                FiscalPaymentType paymentType = MapPaymentType(SelectedReceipt.NacinPlacanja);

                FiscalRequest request = new FiscalRequest
                {
                    Items = StavkeRacuna.ToList(),
                    Cashier = new FiscalCashier
                    {
                        Name = SelectedReceipt.RadnikName ?? SelectedReceipt.Radnik ?? string.Empty
                    },
                    PaymentType = paymentType,
                    TotalAmount = SelectedReceipt.Iznos ?? IznosRacuna ?? 0m,
                    InvoiceType = "Normal",
                    TransactionType = "Sale"
                };

                CroatiaFiscalSettings settings = CroatiaFiscalSettings.FromProperties();

                var taxCalculator = new CroatiaTaxCalculator(settings);

                decimal pnpRate = SelectedReceipt.PorezNaPotrosnjuStopa ?? 0m;

                IReadOnlyList<CroatiaTaxSummary> taxes = await taxCalculator.CalculateAsync(
                    StavkeRacuna.ToList(),
                    pnpRate);

                var builtInvoice = new CroatiaBuiltInvoice
                {
                    LocalReceiptNumber = SelectedReceipt.BrojRacuna,
                    ReceiptNumberHr = SelectedReceipt.BrojRacunaHr,
                    IssueDateTime = SelectedReceipt.Datum,
                    Taxes = taxes,
                    TotalAmount = SelectedReceipt.Iznos ?? IznosRacuna ?? 0m
                };

                var fiscalization = new CroatiaFiscalizationResponse
                {
                    Fiscalized = true,
                    Jir = SelectedReceipt.Jir,
                    Zki = SelectedReceipt.Zki
                };

                var printer = new CroatiaReceiptPrinter(settings);

                bool printed = await printer.ReprintAsync(
                    request,
                    builtInvoice,
                    fiscalization);

                if (!printed)
                    return FiscalResult.Failed("Ponovna štampa računa nije uspjela.");

                Debug.WriteLine($"[HR REPRINT] Račun {SelectedReceipt.BrojRacunaHr} uspješno ponovo isprintan.");

                return new FiscalResult
                {
                    Success = true,
                    Fiscalized = true,
                    SavedToDatabase = true,
                    Printed = true,
                    FiscalNumber = SelectedReceipt.BrojRacunaHr
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[HR REPRINT] Greška: " + ex);
                return FiscalResult.Failed(ex.Message);
            }
        }

        // ============================================================
        // FISKALNA KOPIJA
        // ============================================================

        public async Task<FiscalResult> IzdajKopijuAsync()
        {
            if (SelectedReceipt == null)
                return FiscalResult.Failed("Nije odabran račun.");

            if (!CanIssueCopy)
                return FiscalResult.Failed("Fiskalna kopija nije dostupna za odabrani račun.");

            if (StavkeRacuna.Count == 0)
                return FiscalResult.Failed("Odabrani račun nema stavki.");

            return await IzdajPostojeciRacunAsync("Copy", "Sale");
        }

        // ============================================================
        // STORNO / REFUND
        // ============================================================

        public async Task<FiscalResult> StornirajRacunAsync()
        {
            if (SelectedReceipt == null)
                return FiscalResult.Failed("Nije odabran račun.");

            if (!CanRefund)
                return FiscalResult.Failed("Odabrani račun nije moguće stornirati.");

            if (StavkeRacuna.Count == 0)
                return FiscalResult.Failed("Odabrani račun nema stavki.");

            /*
             * Ovo je privremeno zadržan postojeći put.
             * U sljedećem koraku Refund razdvajamo po državi.
             */
            return await IzdajPostojeciRacunAsync("Training", "Refund");
        }

        // ============================================================
        // HRVATSKA - NAKNADNA FISKALIZACIJA
        // ============================================================

        public Task<FiscalResult> NaknadnoFiskalizujAsync()
        {
            if (SelectedReceipt == null)
                return Task.FromResult(FiscalResult.Failed("Nije odabran račun."));

            if (!IsCroatia)
                return Task.FromResult(FiscalResult.Failed("Naknadna fiskalizacija dostupna je samo za Hrvatsku."));

            if (IsFiscalized)
                return Task.FromResult(FiscalResult.Failed("Odabrani račun je već fiskalizovan."));

            if (StavkeRacuna.Count == 0)
                return Task.FromResult(FiscalResult.Failed("Odabrani račun nema stavki."));

            return Task.FromResult(FiscalResult.Failed("Naknadna fiskalizacija Hrvatske još nije povezana sa novim fiscal servisom."));
        }

        // ============================================================
        // POSTOJEĆI FISKALNI DOKUMENT
        // PRIVREMENO: COPY / REFUND
        // ============================================================

        private async Task<FiscalResult> IzdajPostojeciRacunAsync(string invoiceType, string transactionType)
        {
            try
            {
                if (SelectedReceipt == null)
                    return FiscalResult.Failed("Nije odabran račun.");

                TblKupci? kupac = null;

                if (!string.IsNullOrWhiteSpace(SelectedReceipt.Kupac) && !string.Equals(SelectedReceipt.Kupac, "Gradjani", StringComparison.OrdinalIgnoreCase))
                {
                    await using var db = new AppDbContext();
                    string nazivKupca = SelectedReceipt.Kupac;
                    kupac = await db.Kupci.AsNoTracking().FirstOrDefaultAsync(x => x.Kupac == nazivKupca);
                }

                FiscalBuyer? fiscalBuyer = null;

                if (kupac != null)
                {
                    fiscalBuyer = new FiscalBuyer
                    {
                        Name = kupac.Kupac,
                        TaxId = kupac.JIB,
                        Address = kupac.Adresa,
                        City = kupac.Mjesto
                    };
                }

                FiscalRequest request = new FiscalRequest
                {
                    Items = StavkeRacuna.ToList(),
                    Buyer = fiscalBuyer,
                    Cashier = new FiscalCashier
                    {
                        Id = Globals.ulogovaniKorisnik.IdRadnika,
                        Name = Globals.ulogovaniKorisnik.Radnik,
                        IdentificationNumber = Globals.ulogovaniKorisnik.IB
                    },
                    PaymentType = MapPaymentType(SelectedReceipt.NacinPlacanja),
                    TotalAmount = IznosRacuna ?? 0m,
                    InvoiceType = invoiceType,
                    TransactionType = transactionType,
                    ReferentDocumentNumber = SelectedReceipt.BrojFiskalnogRacuna,
                    ReferentDocumentDateTime = SelectedReceipt.Datum
                };

                IFiscalService fiscalService = FiscalServiceFactory.Create(Properties.Settings.Default.Country);
                FiscalResult result = await fiscalService.IzdajRacunAsync(request);

                Debug.WriteLine($"[RECEIPTS FISCAL] InvoiceType={invoiceType}, TransactionType={transactionType}, Success={result.Success}, Fiscalized={result.Fiscalized}, Saved={result.SavedToDatabase}, Printed={result.Printed}, FiscalNumber={result.FiscalNumber}");

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[RECEIPTS FISCAL] Greška: " + ex);
                return FiscalResult.Failed(ex.Message);
            }
        }

        // ============================================================
        // NAČIN PLAĆANJA
        // ============================================================

        private static FiscalPaymentType MapPaymentType(int? paymentType)
        {
            return paymentType switch
            {
                0 => FiscalPaymentType.Cash,
                1 => FiscalPaymentType.Card,
                2 => FiscalPaymentType.Check,
                3 => FiscalPaymentType.WireTransfer,
                4 => FiscalPaymentType.Other,
                _ => throw new FiscalException($"Nepoznat način plaćanja: {paymentType}.")
            };
        }

        // ============================================================
        // PROPERTY CHANGED
        // ============================================================

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}