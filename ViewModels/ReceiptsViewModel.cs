using Caupo.Data;
using Caupo.Fiscal;
using Caupo.Fiscal.Common;
using Caupo.Fiscal.Croatia;
using Caupo.Fiscal.Croatia.Helpers;
using Caupo.Fiscal.Croatia.Models;
using Caupo.Fiscal.Federation;
using Caupo.Fiscal.Federation.Models;
using Caupo.Fiscal.RepublikaSrpska.Helpers;
using Caupo.Fiscal.RS;
using Caupo.Fiscal.RS.Models;
using Caupo.Fiscal.Serbia;
using Caupo.Fiscal.Serbia.Models;
using Caupo.Models;
using Caupo.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
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
                OnPropertyChanged(nameof(CanMarkImpossible));
            }
        }

        // ============================================================
        // DRŽAVA / NAČIN ŠTAMPE
        // ============================================================

        private string Country => Properties.Settings.Default.Country ?? string.Empty;

        private bool IsCroatia => string.Equals(Country, "Hrvatska", StringComparison.OrdinalIgnoreCase);
        private bool IsSerbia => string.Equals(Country, "Srbija", StringComparison.OrdinalIgnoreCase);
        private bool IsRs => string.Equals(Country, "RepublikaSrpska", StringComparison.OrdinalIgnoreCase);
        private bool IsFederation => string.Equals(Country, "FederacijaBiH", StringComparison.OrdinalIgnoreCase);

        private bool UsesExternalPrinter =>
            string.Equals(Properties.Settings.Default.ExterniPrinter, "DA", StringComparison.OrdinalIgnoreCase);

        private bool IsFiscalized =>
            string.Equals(SelectedReceipt?.Fiskalizovan, "DA", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(SelectedReceipt?.Fiskalizovan, FiscalizationStatus.Fiscalized.ToString(), StringComparison.OrdinalIgnoreCase);

        private bool IsRefunded =>
            string.Equals(SelectedReceipt?.Reklamiran, "DA", StringComparison.OrdinalIgnoreCase);

        // ============================================================
        // DOZVOLJENE OPERACIJE
        // ============================================================

        public bool CanReprint
        {
            get
            {
                if (SelectedReceipt == null || !IsFiscalized)
                    return false;

                if (IsSerbia || IsRs)
                    return UsesExternalPrinter;

                return IsCroatia;
            }
        }

        public bool CanIssueCopy
        {
            get
            {
                if (SelectedReceipt == null || !IsFiscalized)
                    return false;

                return IsSerbia || IsRs || IsFederation;
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

                return IsCroatia &&
                       string.Equals(
                           SelectedReceipt.Fiskalizovan,
                           FiscalizationStatus.NotFiscalized.ToString(),
                           StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool CanMarkImpossible
        {
            get
            {
                if (SelectedReceipt == null)
                    return false;

                return IsCroatia &&
                       string.Equals(
                           SelectedReceipt.Fiskalizovan,
                           FiscalizationStatus.NotFiscalized.ToString(),
                           StringComparison.OrdinalIgnoreCase);
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

                var receipts = await db.Racuni
                    .AsNoTracking()
                    .OrderByDescending(x => x.BrojRacuna)
                    .ToListAsync();

                var workers = await db.Radnici
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var receipt in receipts)
                {
                    if (!int.TryParse(receipt.Radnik, out int workerId))
                    {
                        receipt.RadnikName = receipt.Radnik ?? string.Empty;
                        continue;
                    }

                    var worker = workers.FirstOrDefault(x => x.IdRadnika == workerId);
                    receipt.RadnikName = worker?.Radnik ?? receipt.Radnik ?? string.Empty;
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

                var items = await db.RacunStavka
                    .AsNoTracking()
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

                if (IsCroatia)
                    return await PonovoStampajHrvatskaAsync();

                if (IsSerbia)
                {
                    if (!UsesExternalPrinter)
                        return FiscalResult.Failed("Ponovni ispis nije dostupan kada L-PFR štampa fiskalni račun. Koristite fiskalnu kopiju računa.");

                    return await PonovoStampajSrbijaAsync();
                }

                if (IsRs)
                {
                    if (!UsesExternalPrinter)
                        return FiscalResult.Failed("Ponovni ispis nije dostupan kada LPFR štampa fiskalni račun. Koristite fiskalnu kopiju računa.");

                    return await PonovoStampajRsAsync();
                }

                return FiscalResult.Failed("Ponovna štampa trenutno nije dostupna za odabranu državu.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[REPRINT] Greška: " + ex);
                return FiscalResult.Failed(ex.Message);
            }
        }

        // ============================================================
        // PONOVNA ŠTAMPA - HRVATSKA
        // ============================================================

        private async Task<FiscalResult> PonovoStampajHrvatskaAsync()
        {
            if (SelectedReceipt == null)
                return FiscalResult.Failed("Nije odabran račun.");

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

        // ============================================================
        // PONOVNA ŠTAMPA - SRBIJA
        // ============================================================

        private async Task<FiscalResult> PonovoStampajSrbijaAsync()
        {
            if (SelectedReceipt == null)
                return FiscalResult.Failed("Nije odabran račun.");

            if (!UsesExternalPrinter)
                return FiscalResult.Failed("Ponovni ispis nije dostupan kada L-PFR štampa fiskalni račun. Koristite fiskalnu kopiju računa.");

            if (SelectedReceipt.DatumFiskalnogDokumenta == null)
                return FiscalResult.Failed("Odabrani račun nema spremljen datum fiskalnog dokumenta.");

            if (string.IsNullOrWhiteSpace(SelectedReceipt.BrojFiskalnogRacuna))
                return FiscalResult.Failed("Odabrani račun nema spremljen PFR broj računa.");

            if (string.IsNullOrWhiteSpace(SelectedReceipt.BrojacFiskalnogRacuna))
                return FiscalResult.Failed("Odabrani račun nema spremljen brojač fiskalnog računa.");

            if (string.IsNullOrWhiteSpace(SelectedReceipt.FiskalniVerificationUrl))
                return FiscalResult.Failed("Odabrani račun nema spremljen Verification URL.");

            await using var db = new AppDbContext();

            var poreskeStope = await db.PoreskeStope
                .AsNoTracking()
                .ToListAsync();

            var reprintItems = new List<SerbiaReceiptReprintItem>();

            foreach (var item in StavkeRacuna)
            {
                var poreskaStopa = poreskeStope.FirstOrDefault(x => x.IdStope == item.PoreskaStopa);

                if (poreskaStopa == null)
                    return FiscalResult.Failed($"Poreska stopa za artikal '{item.Naziv ?? item.Name}' nije pronađena.");

                reprintItems.Add(new SerbiaReceiptReprintItem
                {
                    Name = item.Naziv ?? item.Name ?? string.Empty,
                    Quantity = item.Quantity ?? 0m,
                    UnitPrice = item.UnitPrice ?? 0m,
                    TaxLabel = poreskaStopa.Oznaka ?? string.Empty,
                    TaxName = poreskaStopa.Opis ?? string.Empty,
                    TaxRate = poreskaStopa.Postotak ?? 0m
                });
            }

            var data = new SerbiaReceiptReprintData
            {
                LocalReceiptNumber = SelectedReceipt.BrojRacuna,
                EsirDateTime = SelectedReceipt.DatumFiskalnogDokumenta.Value,
                PfrDateTime = SelectedReceipt.Datum,
                FiscalNumber = SelectedReceipt.BrojFiskalnogRacuna,
                InvoiceCounter = SelectedReceipt.BrojacFiskalnogRacuna,
                VerificationUrl = SelectedReceipt.FiskalniVerificationUrl,
                Cashier = SelectedReceipt.RadnikName ?? SelectedReceipt.Radnik ?? string.Empty,
                PaymentType = MapPaymentType(SelectedReceipt.NacinPlacanja),
                TotalAmount = SelectedReceipt.Iznos ?? IznosRacuna ?? 0m,
                Items = reprintItems
            };

            var printer = new SerbiaReceiptPrinter();

            bool printed = await printer.ReprintAsync(data);

            if (!printed)
                return FiscalResult.Failed("Ponovna štampa srpskog fiskalnog računa nije uspjela.");

            Debug.WriteLine(
                $"[SR REPRINT] Račun {SelectedReceipt.BrojRacuna}, PFR={SelectedReceipt.BrojFiskalnogRacuna} uspješno ponovo isprintan.");

            return new FiscalResult
            {
                Success = true,
                Fiscalized = true,
                FiscalizationStatus = FiscalizationStatus.Fiscalized,
                SavedToDatabase = true,
                Printed = true,
                LocalReceiptNumber = SelectedReceipt.BrojRacuna,
                ReceiptNumber = SelectedReceipt.BrojFiskalnogRacuna,
                FiscalNumber = SelectedReceipt.BrojFiskalnogRacuna,
                FiscalDateTime = SelectedReceipt.Datum
            };
        }

        // ============================================================
        // PONOVNA ŠTAMPA - REPUBLIKA SRPSKA
        // ============================================================

        private async Task<FiscalResult> PonovoStampajRsAsync()
        {
            if (SelectedReceipt == null)
                return FiscalResult.Failed("Nije odabran račun.");

            RsFiscalSettings settings = RsFiscalSettings.FromProperties();

            if (!settings.ExternalPrinter)
                return FiscalResult.Failed("Ponovni ispis nije dostupan kada LPFR štampa fiskalni račun. Koristite fiskalnu kopiju računa.");

            if (SelectedReceipt.DatumFiskalnogDokumenta == null)
                return FiscalResult.Failed("Odabrani račun nema spremljen datum fiskalnog dokumenta.");

            if (string.IsNullOrWhiteSpace(SelectedReceipt.BrojFiskalnogRacuna))
                return FiscalResult.Failed("Odabrani račun nema spremljen PFR broj računa.");

            if (string.IsNullOrWhiteSpace(SelectedReceipt.BrojacFiskalnogRacuna))
                return FiscalResult.Failed("Odabrani račun nema spremljen brojač fiskalnog računa.");

            if (string.IsNullOrWhiteSpace(SelectedReceipt.FiskalniVerificationUrl))
                return FiscalResult.Failed("Odabrani račun nema spremljen Verification URL.");

            await using var db = new AppDbContext();

            var poreskeStope = await db.PoreskeStope
                .AsNoTracking()
                .ToListAsync();

            var reprintItems = new List<RsReceiptReprintItem>();

            foreach (var item in StavkeRacuna)
            {
                if (!item.PoreskaStopa.HasValue)
                    return FiscalResult.Failed($"Artikal '{item.Naziv ?? item.Name}' nema poresku stopu.");

                var poreskaStopa = poreskeStope.FirstOrDefault(x => x.IdStope == item.PoreskaStopa);

                if (poreskaStopa == null)
                    return FiscalResult.Failed($"Poreska stopa za artikal '{item.Naziv ?? item.Name}' nije pronađena.");

                string taxLabel;

                try
                {
                    taxLabel = RsTaxMapper.ToFiscalLabel(item.PoreskaStopa);
                }
                catch (Exception ex)
                {
                    return FiscalResult.Failed($"Neispravna poreska stopa za artikal '{item.Naziv ?? item.Name}': {ex.Message}");
                }

                decimal quantity = item.Quantity ?? 0m;
                decimal unitPrice = item.UnitPrice ?? 0m;

                reprintItems.Add(new RsReceiptReprintItem
                {
                    Name = item.Naziv ?? item.Name ?? string.Empty,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    TotalAmount = Math.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero),
                    TaxRateId = item.PoreskaStopa,
                    TaxLabel = taxLabel,
                    TaxName = poreskaStopa.Opis ?? string.Empty,
                    TaxRate = poreskaStopa.Postotak ?? 0m
                });
            }

            var data = new RsReceiptReprintData
            {
                LocalReceiptNumber = SelectedReceipt.BrojRacuna,
                EsirDateTime = SelectedReceipt.DatumFiskalnogDokumenta.Value,
                PfrDateTime = SelectedReceipt.Datum,
                FiscalReceiptNumber = SelectedReceipt.BrojFiskalnogRacuna,
                TotalCounter = SelectedReceipt.BrojacFiskalnogRacuna,
                VerificationUrl = SelectedReceipt.FiskalniVerificationUrl,
                Cashier = SelectedReceipt.RadnikName ?? SelectedReceipt.Radnik ?? string.Empty,
                PaymentType = MapPaymentType(SelectedReceipt.NacinPlacanja),
                TotalAmount = SelectedReceipt.Iznos ?? IznosRacuna ?? 0m,
                Items = reprintItems
            };

            var printer = new RsReceiptPrinter(settings);

            bool printed = printer.Reprint(data);

            if (!printed)
                return FiscalResult.Failed("Ponovna štampa fiskalnog računa Republike Srpske nije uspjela.");

            Debug.WriteLine(
                $"[RS REPRINT] Račun {SelectedReceipt.BrojRacuna}, PFR={SelectedReceipt.BrojFiskalnogRacuna} uspješno ponovo isprintan.");

            return new FiscalResult
            {
                Success = true,
                Fiscalized = true,
                FiscalizationStatus = FiscalizationStatus.Fiscalized,
                SavedToDatabase = true,
                Printed = true,
                LocalReceiptNumber = SelectedReceipt.BrojRacuna,
                ReceiptNumber = SelectedReceipt.BrojFiskalnogRacuna,
                FiscalNumber = SelectedReceipt.BrojFiskalnogRacuna,
                FiscalDateTime = SelectedReceipt.Datum
            };
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

            if (string.IsNullOrWhiteSpace(SelectedReceipt.BrojFiskalnogRacuna))
                return FiscalResult.Failed("Odabrani račun nema broj fiskalnog računa.");

            if (IsFederation)
                return IzdajKopijuFederacija();

            if (StavkeRacuna.Count == 0)
                return FiscalResult.Failed("Odabrani račun nema stavki.");

            return await IzdajPostojeciRacunAsync("Copy", "Sale");
        }

        private FiscalResult IzdajKopijuFederacija()
        {
            try
            {
                if (SelectedReceipt == null)
                    return FiscalResult.Failed("Nije odabran račun.");

                if (string.IsNullOrWhiteSpace(SelectedReceipt.BrojFiskalnogRacuna))
                    return FiscalResult.Failed("Odabrani račun nema broj fiskalnog računa.");

                FederationFiscalSettings settings = FederationFiscalSettings.FromProperties();
                var client = new FederationFiscalClient(settings);

                FederationFiscalResponse response = client.PrintDuplicate(SelectedReceipt.BrojFiskalnogRacuna);

                if (!response.Fiscalized || !response.Printed)
                    return FiscalResult.Failed(response.ErrorMessage ?? "Štampanje kopije fiskalnog računa nije uspjelo.");

                Debug.WriteLine($"[FEDERACIJA/TRING COPY] Fiskalni račun {SelectedReceipt.BrojFiskalnogRacuna} uspješno kopiran.");

                return new FiscalResult
                {
                    Success = true,
                    Fiscalized = true,
                    FiscalizationStatus = FiscalizationStatus.Fiscalized,
                    SavedToDatabase = true,
                    Printed = true,
                    LocalReceiptNumber = SelectedReceipt.BrojRacuna,
                    ReceiptNumber = SelectedReceipt.BrojFiskalnogRacuna,
                    FiscalNumber = SelectedReceipt.BrojFiskalnogRacuna,
                    FiscalDateTime = SelectedReceipt.Datum
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[FEDERACIJA/TRING COPY] Greška: " + ex);
                return FiscalResult.Failed(ex.Message);
            }
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

            if (string.IsNullOrWhiteSpace(SelectedReceipt.BrojFiskalnogRacuna))
                return FiscalResult.Failed("Odabrani račun nema broj fiskalnog računa.");

            return await IzdajPostojeciRacunAsync("Normal", "Refund");
        }

        // ============================================================
        // HRVATSKA - NAKNADNA FISKALIZACIJA
        // ============================================================

        public async Task<FiscalResult> NaknadnoFiskalizujAsync()
        {
            try
            {
                if (SelectedReceipt == null)
                    return FiscalResult.Failed("Nije odabran račun.");

                if (!IsCroatia)
                    return FiscalResult.Failed("Naknadna fiskalizacija dostupna je samo za Hrvatsku.");

                if (IsFiscalized)
                    return FiscalResult.Failed("Odabrani račun je već fiskalizovan.");

                if (StavkeRacuna.Count == 0)
                    return FiscalResult.Failed("Odabrani račun nema stavki.");

                int localReceiptNumber = SelectedReceipt.BrojRacuna;

                if (Application.Current is not App app || app.CroatiaFiscalWorker == null)
                    return FiscalResult.Failed("Servis naknadne fiskalizacije nije pokrenut.");

                CroatiaFiscalizationResponse response = await app.CroatiaFiscalWorker.FiscalizeManuallyAsync(localReceiptNumber);

                if (!response.Fiscalized || string.IsNullOrWhiteSpace(response.Jir))
                    return FiscalResult.Failed(response.ErrorMessage ?? "CIS nije potvrdio fiskalizaciju računa.");

                return new FiscalResult
                {
                    Success = true,
                    Fiscalized = true,
                    FiscalizationStatus = FiscalizationStatus.Fiscalized,
                    SavedToDatabase = true,
                    Printed = false,
                    LocalReceiptNumber = localReceiptNumber,
                    ReceiptNumber = SelectedReceipt.BrojRacunaHr,
                    FiscalNumber = response.Jir,
                    FiscalDateTime = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[HR MANUAL FISCALIZATION] Greška: " + ex);
                return FiscalResult.Failed(ex.Message);
            }
        }
        // ============================================================
        // HRVATSKA - Prestanak naknadne fiskalizacije (Impossible)
        // ============================================================
        public async Task<FiscalResult> MarkImpossibleAsync(TblRadnici confirmedWorker)
        {
            try
            {
                if (SelectedReceipt == null)
                    return FiscalResult.Failed("Nije odabran račun.");

                if (!IsCroatia)
                    return FiscalResult.Failed("Ova opcija dostupna je samo za Hrvatsku.");

                if (confirmedWorker == null ||
                    !string.Equals(confirmedWorker.Dozvole, "Administrator", StringComparison.OrdinalIgnoreCase))
                {
                    return FiscalResult.Failed("Za ovu akciju potrebna je administratorska autorizacija.");
                }

                if (!string.Equals(
                        SelectedReceipt.Fiskalizovan,
                        FiscalizationStatus.NotFiscalized.ToString(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return FiscalResult.Failed("Račun više nije na čekanju za fiskalizaciju.");
                }

                if (Application.Current is not App app || app.CroatiaFiscalWorker == null)
                    return FiscalResult.Failed("Servis naknadne fiskalizacije nije pokrenut.");

                int localReceiptNumber = SelectedReceipt.BrojRacuna;
                string? receiptNumber = SelectedReceipt.BrojRacunaHr;

                await app.CroatiaFiscalWorker.MarkImpossibleManuallyAsync(localReceiptNumber);

                await AuditLogService.WriteAsync(
                    dogadjaj: "HR_FISCALIZATION_IMPOSSIBLE",
                    detalji: "Prekinuti dalji pokušaji naknadne fiskalizacije.",
                    idRadnika: confirmedWorker.IdRadnika,
                    radnik: confirmedWorker.Radnik,
                    brojRacuna: localReceiptNumber);

                return new FiscalResult
                {
                    Success = true,
                    Fiscalized = false,
                    FiscalizationStatus = FiscalizationStatus.Impossible,
                    SavedToDatabase = true,
                    Printed = false,
                    LocalReceiptNumber = localReceiptNumber,
                    ReceiptNumber = receiptNumber
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[HR IMPOSSIBLE] Greška: " + ex);
                return FiscalResult.Failed(ex.Message);
            }
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

                if (!string.IsNullOrWhiteSpace(SelectedReceipt.Kupac) &&
                    !string.Equals(SelectedReceipt.Kupac, "Gradjani", StringComparison.OrdinalIgnoreCase))
                {
                    await using var db = new AppDbContext();

                    string nazivKupca = SelectedReceipt.Kupac;

                    kupac = await db.Kupci
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Kupac == nazivKupca);
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

                Debug.WriteLine(
                    $"[RECEIPTS FISCAL] InvoiceType={invoiceType}, TransactionType={transactionType}, Success={result.Success}, Fiscalized={result.Fiscalized}, Saved={result.SavedToDatabase}, Printed={result.Printed}, FiscalNumber={result.FiscalNumber}");

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