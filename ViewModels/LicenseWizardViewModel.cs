using Caupo.Licensing;
using Caupo.Licensing.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LicenseManager = Caupo.Licensing.LicenseManager;

namespace Caupo.ViewModels;

public sealed class LicenseWizardViewModel : INotifyPropertyChanged
{
    private readonly LicenseManager _licenseManager;
    private PaddlePlan? _selectedPlan;
    private string _quantityText = "1";
    private int _renewalSelection;
    private int _step;
    private bool _busy;
    private string _statusMessage = string.Empty;
    private string _licenseKey = string.Empty;
    private string? _requestToken;
    private string? _requestContent;
    private string? _selectedFiscalRegion;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<PaddlePlan> Plans { get; } = new();
    public PaddleCustomer Customer { get; } = new();
    public string Currency { get; private set; } = "EUR";
    public CheckoutResponse? Checkout { get; private set; }

    public LicenseWizardViewModel(LicenseManager licenseManager)
    {
        _licenseManager = licenseManager;
    }

    public int Step
    {
        get => _step;
        private set
        {
            if (_step == value) return;
            _step = value;
            Notify();
            Notify(nameof(IsWelcome));
            Notify(nameof(IsPlans));
            Notify(nameof(IsCustomer));
            Notify(nameof(IsReview));
            Notify(nameof(IsCheckout));
            Notify(nameof(IsActivation));
            Notify(nameof(StepTitle));
        }
    }

    public bool IsWelcome => Step == 0;
    public bool IsPlans => Step == 1;
    public bool IsCustomer => Step == 2;
    public bool IsReview => Step == 3;
    public bool IsCheckout => Step == 4;
    public bool IsActivation => Step == 5;

    public string StepTitle => Step switch
    {
        0 => "Licenciranje Caupa",
        1 => "Odaberite licencu",
        2 => "Podaci kupca",
        3 => "Pregled kupovine",
        4 => "Plaćanje",
        5 => "Aktivacija licence",
        _ => "Licenciranje"
    };

    public bool Busy
    {
        get => _busy;
        private set
        {
            if (_busy == value) return;
            _busy = value;
            Notify();
            Notify(nameof(NotBusy));
        }
    }

    public bool NotBusy => !Busy;

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage == value) return;
            _statusMessage = value;
            Notify();
        }
    }

    public PaddlePlan? SelectedPlan
    {
        get => _selectedPlan;
        set
        {
            if (_selectedPlan == value) return;
            _selectedPlan = value;
            Notify();
            NotifyPrice();
        }
    }

    public string QuantityText
    {
        get => _quantityText;
        set
        {
            if (_quantityText == value) return;
            _quantityText = value;
            Notify();
            NotifyPrice();
        }
    }

    public int RenewalSelection
    {
        get => _renewalSelection;
        set
        {
            if (_renewalSelection == value) return;
            _renewalSelection = value;
            Notify();
            Notify(nameof(RenewalDescription));
        }
    }

    public string Renewal => RenewalSelection == 0 ? "subscription" : "one_time";

    public string RenewalDescription => RenewalSelection == 0
        ? "Paddle automatski naplaćuje svaki naredni period."
        : "Nema automatske naplate. Sami odlučujete kada ćete ponovo platiti produženje licence.";

    public string CountryCode
    {
        get => Customer.CountryCode;
        set
        {
            string code = value ?? string.Empty;
            if (Customer.CountryCode == code) return;

            Customer.CountryCode = code;
            SelectedFiscalRegion = null;
            Notify();
            Notify(nameof(ShowBiHFiscalRegion));
        }
    }

    public bool ShowBiHFiscalRegion => CountryCode == "BA";

    public string? SelectedFiscalRegion
    {
        get => _selectedFiscalRegion;
        set
        {
            if (_selectedFiscalRegion == value) return;
            _selectedFiscalRegion = value;
            Notify();
        }
    }

    public string LicenseKey
    {
        get => _licenseKey;
        set
        {
            _licenseKey = value;
            Notify();
        }
    }

    public string SelectedPlanDescription => SelectedPlan?.Name ?? string.Empty;

    public string PriceDescription
    {
        get
        {
            if (!TryGetPrice(out long amount, out int quantity))
                return "Odaberite plan i ispravan broj licenci.";

            var culture = CultureInfo.GetCultureInfo("de-DE");
            string unit = (amount / 100m).ToString("N2", culture);
            string total = (amount * quantity / 100m).ToString("N2", culture);
            return $"{unit} {Currency} × {quantity} = {total} {Currency}";
        }
    }

    public string? ValidatePlan()
    {
        if (SelectedPlan is null) return "Odaberite plan.";

        if (!int.TryParse(QuantityText, out int quantity) || quantity is < 1 or > 10)
            return "Broj licenci mora biti između 1 i 10.";

        PaddlePriceTier? tier = FindTier(quantity);
        if (tier is null) return "Za odabrani broj licenci nije dostupna cijena.";

        if (RenewalSelection == 0 && string.IsNullOrWhiteSpace(tier.RecurringPriceId))
            return "Pretplata trenutno nije dostupna za ovaj plan.";

        if (RenewalSelection == 1 && string.IsNullOrWhiteSpace(tier.OneTimePriceId))
            return "Plaćanje bez pretplate trenutno nije dostupno za ovaj plan.";

        return null;
    }

    public string? ValidateCustomer()
    {
        Customer.CompanyName = Customer.CompanyName.Trim();
        Customer.ContactName = Customer.ContactName.Trim();
        Customer.Email = Customer.Email.Trim();
        Customer.Phone = Customer.Phone.Trim();
        Customer.Address = Customer.Address.Trim();
        Customer.City = Customer.City.Trim();
        Customer.PostalCode = Customer.PostalCode.Trim();
        Customer.CountryCode = Customer.CountryCode.Trim().ToUpperInvariant();
        Customer.TaxNumber = Customer.TaxNumber.Trim();

        if (string.IsNullOrWhiteSpace(Customer.CompanyName)) return "Unesite naziv firme.";
        if (string.IsNullOrWhiteSpace(Customer.ContactName)) return "Unesite kontakt osobu.";
        if (string.IsNullOrWhiteSpace(Customer.Email)) return "Unesite e-mail adresu.";

        try
        {
            var address = new MailAddress(Customer.Email);
            if (!string.Equals(address.Address, Customer.Email, StringComparison.OrdinalIgnoreCase))
                return "E-mail adresa nije ispravna.";
        }
        catch (FormatException)
        {
            return "E-mail adresa nije ispravna.";
        }

        if (string.IsNullOrWhiteSpace(Customer.Phone)) return "Unesite broj telefona.";
        if (string.IsNullOrWhiteSpace(Customer.Address)) return "Unesite adresu.";
        if (string.IsNullOrWhiteSpace(Customer.City)) return "Unesite grad ili mjesto.";
        if (string.IsNullOrWhiteSpace(Customer.PostalCode)) return "Unesite poštanski broj.";
        if (Customer.CountryCode is not ("BA" or "HR" or "RS")) return "Odaberite državu.";

        if (Customer.CountryCode == "BA"
            && SelectedFiscalRegion is not ("FederacijaBiH" or "RepublikaSrpska"))
            return "Odaberite entitet u Bosni i Hercegovini.";

        if (string.IsNullOrWhiteSpace(Customer.TaxNumber)) return "Unesite poreski broj.";

        return null;
    }

    public async Task OpenPurchaseAsync(CancellationToken cancellationToken = default)
    {
        Busy = true;
        StatusMessage = "Učitavam cjenovnik...";

        try
        {
            PaddleCatalogResponse catalog = await _licenseManager.GetCatalogAsync(cancellationToken);
            Currency = catalog.Currency;
            Plans.Clear();

            foreach (PaddlePlan plan in catalog.Plans.Where(p => p.Tiers.Count > 0))
                Plans.Add(plan);

            SelectedPlan = Plans.FirstOrDefault();
            NotifyPrice();

            if (SelectedPlan is null)
            {
                StatusMessage = "Trenutno nema dostupnih licenci.";
                return;
            }

            StatusMessage = string.Empty;
            Step = 1;
        }
        finally
        {
            Busy = false;
        }
    }

    public void OpenActivation()
    {
        StatusMessage = string.Empty;
        Step = 5;
    }

    public void Next()
    {
        StatusMessage = string.Empty;

        if (Step == 1 && ValidatePlan() is null)
            Step = 2;
        else if (Step == 2 && ValidateCustomer() is null)
            Step = 3;
    }

    public void Back()
    {
        StatusMessage = string.Empty;
        Step = Step switch
        {
            1 => 0,
            2 => 1,
            3 => 2,
            4 => 3,
            5 => 0,
            _ => Step
        };
    }

    public void ReturnToReview()
    {
        if (Step == 4) Back();
    }

    public async Task<CheckoutResponse> StartCheckoutAsync(CancellationToken cancellationToken = default)
    {
        string? error = ValidatePlan() ?? ValidateCustomer();
        if (error is not null) throw new InvalidOperationException(error);

        if (SelectedPlan is null || !int.TryParse(QuantityText, out int quantity))
            throw new InvalidOperationException("Podaci narudžbe nisu ispravni.");

        Busy = true;
        StatusMessage = "Pripremam plaćanje...";

        try
        {
            string content = JsonSerializer.Serialize(new
            {
                customer = Customer,
                plan_id = SelectedPlan.Id,
                quantity,
                renewal = Renewal
            });

            if (_requestToken is null || _requestContent != content)
            {
                _requestToken = LicenseApiClient.NewRequestToken();
                _requestContent = content;
                Checkout = null;
            }

            Checkout = await _licenseManager.StartPurchaseAsync(
                Customer, SelectedPlan.Id, quantity, Renewal, _requestToken, cancellationToken);

            Step = 4;
            StatusMessage = string.Empty;
            return Checkout;
        }
        finally
        {
            Busy = false;
        }
    }

    public bool IsSingleLicensePurchase =>
        int.TryParse(QuantityText, out int quantity) && quantity == 1;

    public async Task<bool> CompleteCheckoutAsync(
        string transactionId, CancellationToken cancellationToken = default)
    {
        if (!IsSingleLicensePurchase || Checkout?.TransactionId != transactionId || _requestToken is null)
            throw new InvalidOperationException("Kupovina nije povezana s ovom Caupo aplikacijom.");

        Busy = true;
        StatusMessage = "Čekam potvrdu uplate i izdavanje licence...";

        try
        {
            for (int attempt = 0; attempt < 45; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                LicenseApiClient.WpfLicenseResult result = await _licenseManager.GetWpfLicenseAsync(
                    transactionId, _requestToken, cancellationToken);

                if (result.Status == "ready")
                {
                    StatusMessage = "Aktiviram licencu na ovom računaru...";

                    LicenseState state = await _licenseManager.ActivateAsync(
                        result.LicenseKey, cancellationToken);

                    if (!state.CanOperate)
                        throw new InvalidOperationException(state.Message);

                    SaveCustomerSettings();
                    StatusMessage = "Licenca je uspješno aktivirana.";
                    return true;
                }

                if (result.Status != "processing")
                    throw new InvalidOperationException("Server je vratio nepoznat status kupovine.");

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }

            StatusMessage = "Uplata je prihvaćena, ali licenca još nije dostupna.";
            return false;
        }
        finally
        {
            Busy = false;
        }
    }

    private void SaveCustomerSettings()
    {
        var settings = Caupo.Properties.Settings.Default;

        settings.Firma = Customer.CompanyName;
        settings.KontaktOsoba = Customer.ContactName;
        settings.Email = Customer.Email;
        settings.Telefon = Customer.Phone;
        settings.Adresa = Customer.Address;
        settings.Mjesto = Customer.City;
        settings.PostanskiBroj = Customer.PostalCode;
        settings.DrzavaKod = Customer.CountryCode;
        settings.JIB = Customer.TaxNumber;

        settings.Country = Customer.CountryCode switch
        {
            "BA" => SelectedFiscalRegion ?? throw new InvalidOperationException("Entitet nije odabran."),
            "HR" => "Hrvatska",
            "RS" => "Srbija",
            _ => throw new InvalidOperationException("Država nije ispravna.")
        };

        settings.Save();
    }

    public async Task<LicenseState> ActivateAsync(CancellationToken cancellationToken = default)
    {
        Busy = true;
        StatusMessage = "Provjeravam licencu...";

        try
        {
            LicenseState result = await _licenseManager.ActivateAsync(LicenseKey, cancellationToken);
            StatusMessage = result.Message;
            return result;
        }
        finally
        {
            Busy = false;
        }
    }

    private PaddlePriceTier? FindTier(int quantity) =>
        SelectedPlan?.Tiers.FirstOrDefault(t => quantity >= t.Min && (t.Max is null || quantity <= t.Max));

    private bool TryGetPrice(out long amount, out int quantity)
    {
        amount = 0;
        if (!int.TryParse(QuantityText, out quantity) || quantity is < 1 or > 10)
            return false;

        PaddlePriceTier? tier = FindTier(quantity);
        if (tier is null) return false;

        amount = tier.Amount;
        return true;
    }

    private void NotifyPrice()
    {
        Notify(nameof(SelectedPlanDescription));
        Notify(nameof(PriceDescription));
    }

    private void Notify([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
