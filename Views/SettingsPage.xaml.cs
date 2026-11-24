
using Caupo.Helpers;
using Caupo.Licensing;
using Caupo.Licensing.Models;
using Caupo.Services;
using Caupo.ViewModels;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using LicenseManager = Caupo.Licensing.LicenseManager;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : UserControl
    {
        private bool _initializingCashRegisterType;
        private KitchenKeyboardService? _temporaryKeyboardService;
        private KitchenKeyboardService? _selectionService;

        public SettingsPage()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
            InitializeCashRegisterType();
            UpdateKitchenKeyboardStatus();
            Unloaded += SettingsPage_Unloaded;
        }

        // =====================================================
        // Tastatura za kuhinjski ekran
        // =====================================================

        private void UpdateKitchenKeyboardStatus()
        {
            bool configured = !string.IsNullOrWhiteSpace(Properties.Settings.Default.KitchenKeyboardId);
            txtKitchenKeyboardStatus.Text = configured ? "Odabrana" : "Nije odabrana";
            btnRemoveKitchenKeyboard.IsEnabled = configured;
            btnSelectKitchenKeyboard.Content = configured ? "Promijeni tastaturu" : "Odaberi tastaturu";
        }

        private void SelectKitchenKeyboard_Click(object sender, RoutedEventArgs e)
        {
            if (_selectionService != null) return;

            Window? window = Window.GetWindow(this);
            if (window == null) return;

            try
            {
                KitchenKeyboardService? service = KitchenKeyboardService.Active;

                if (service == null)
                {
                    _temporaryKeyboardService = new KitchenKeyboardService();
                    _temporaryKeyboardService.Start(window);
                    service = _temporaryKeyboardService;
                }

                _selectionService = service;
                _selectionService.KeyboardDetected += KitchenKeyboard_Detected;
                _selectionService.BeginKeyboardSelection();

                txtKitchenKeyboardStatus.Text = "Pritisnite F1...";
                btnSelectKitchenKeyboard.IsEnabled = false;
                btnRemoveKitchenKeyboard.IsEnabled = false;
            }
            catch (Exception ex)
            {
                EndKitchenKeyboardSelection();

                MessageBox.Show(
                    "Nije moguće pokrenuti odabir tastature.\n\n" + ex.Message,
                    "Kuhinjska tastatura", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void KitchenKeyboard_Detected(object? sender, string identifier)
        {
            EndKitchenKeyboardSelection();

            var popup = new YesNoPopup
            {
                Owner = Window.GetWindow(this),
                PopupTitle = "Kuhinjska tastatura",
                PopupMessage = "Tastatura je prepoznata.\n\nŽelite li je koristiti za kuhinjski ekran?",
                ConfirmText = "Da",
                CancelText = "Ne"
            };

            if (popup.ShowDialog() == true)
            {
                Properties.Settings.Default.KitchenKeyboardId = identifier;
                Properties.Settings.Default.Save();
                KitchenKeyboardService.Active?.SetKeyboardIdentifier(identifier);
            }

            UpdateKitchenKeyboardStatus();
        }

        private void RemoveKitchenKeyboard_Click(object sender, RoutedEventArgs e)
        {
            if (_selectionService != null) return;

            Properties.Settings.Default.KitchenKeyboardId = string.Empty;
            Properties.Settings.Default.Save();
            KitchenKeyboardService.Active?.SetKeyboardIdentifier(string.Empty);

            UpdateKitchenKeyboardStatus();
        }

        private void EndKitchenKeyboardSelection()
        {
            if (_selectionService != null)
            {
                _selectionService.KeyboardDetected -= KitchenKeyboard_Detected;
                _selectionService.CancelKeyboardSelection();
                _selectionService = null;
            }

            if (_temporaryKeyboardService != null)
            {
                _temporaryKeyboardService.Dispose();
                _temporaryKeyboardService = null;
            }

            btnSelectKitchenKeyboard.IsEnabled = true;
            UpdateKitchenKeyboardStatus();
        }

        private void SettingsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            EndKitchenKeyboardSelection();
        }

        // =====================================================
        // TIP KASE
        // =====================================================

        private void InitializeCashRegisterType()
        {
            _initializingCashRegisterType = true;
            string currentType = CashRegisterConfigurationService.CashRegisterType;

            foreach (object item in cmbCashRegisterType.Items)
            {
                if (item is not ComboBoxItem comboItem) continue;

                string tag = comboItem.Tag?.ToString() ?? string.Empty;

                if (string.Equals(tag, currentType, StringComparison.OrdinalIgnoreCase))
                {
                    cmbCashRegisterType.SelectedItem = comboItem;
                    break;
                }
            }

            DbPathButton.Visibility = CashRegisterConfigurationService.IsSecondaryCashRegister
                ? Visibility.Visible : Visibility.Collapsed;

            UpdateCashRegisterStatus();
            _initializingCashRegisterType = false;
        }

        private void CashRegisterType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initializingCashRegisterType) return;
            if (cmbCashRegisterType.SelectedItem is not ComboBoxItem selectedItem) return;

            string selectedType = selectedItem.Tag?.ToString()
                ?? CashRegisterConfigurationService.MainCashRegister;

            if (string.Equals(selectedType, CashRegisterConfigurationService.MainCashRegister,
                StringComparison.OrdinalIgnoreCase))
            {
                bool wasSecondary = CashRegisterConfigurationService.IsSecondaryCashRegister;

                DbPathButton.Visibility = Visibility.Collapsed;
                CashRegisterConfigurationService.SetMainCashRegister();
                UpdateDbPathInViewModel(DataFolderService.LocalDatabasePath);
                UpdateCashRegisterStatus();

                Debug.WriteLine("[SETTINGS] Izabrana glavna kasa.");
                if (wasSecondary) AskForRestart("Tip kase je promijenjen na Glavnu kasu.");
                return;
            }

            // Dodatnu kasu spremamo tek nakon uspješnog povezivanja.
            DbPathButton.Visibility = Visibility.Visible;
            txtCashRegisterStatus.Text = "Dodatna kasa nije još povezana. Kliknite „Pronađi“.";
            Debug.WriteLine("[SETTINGS] Izabrana dodatna kasa - čekam povezivanje.");
        }

        // =====================================================
        // ZATVARANJE SETTINGS STRANICE
        // =====================================================

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var page = new HomePage { DataContext = new HomeViewModel() };
            PageNavigator.NavigateWithFade(page);
        }

        // =====================================================
        // PRONAĐI I POVEŽI GLAVNU KASU
        // =====================================================

        private async void DbPathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DbPathButton.IsEnabled = false;
                txtCashRegisterStatus.Text = "Tražim glavnu kasu na mreži...";
                Debug.WriteLine("[SETTINGS] Tražim glavnu kasu...");

                MainCashRegisterInfo? main = await CashRegisterConfigurationService
                    .FindAndConfigureSecondaryCashRegisterAsync(3000);

                if (main == null)
                {
                    txtCashRegisterStatus.Text = "Glavna kasa nije pronađena na mreži.";

                    MessageBox.Show(
                        "Glavna kasa nije pronađena na mreži.\n\n"
                        + "Provjerite da li je glavna kasa uključena "
                        + "i da li su oba računara spojena na istu mrežu.",
                        "Caupo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                UpdateDbPathInViewModel(main.DatabasePath);
                UpdateCashRegisterStatus();

                Debug.WriteLine("[SETTINGS] Dodatna kasa uspješno povezana.");
                Debug.WriteLine($"[SETTINGS] Glavna kasa: {main.MachineName}");
                Debug.WriteLine($"[SETTINGS] IP: {main.IpAddress}");
                Debug.WriteLine($"[SETTINGS] DbPath: {main.DatabasePath}");

                MessageBox.Show(
                    "Dodatna kasa je uspješno povezana.\n\n"
                    + $"Glavna kasa: {main.MachineName}\n"
                    + $"Baza: {main.DatabasePath}",
                    "Caupo", MessageBoxButton.OK, MessageBoxImage.Information);

                AskForRestart("Dodatna kasa je uspješno podešena.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SETTINGS] Greška pri povezivanju: {ex}");
                txtCashRegisterStatus.Text = "Glavna kasa nije pronađena ili nije dostupna.";
                MessageBox.Show(ex.Message, "Caupo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                DbPathButton.IsEnabled = true;
            }
        }

        // =====================================================
        // STATUS KASE
        // =====================================================

        private void UpdateCashRegisterStatus()
        {
            if (CashRegisterConfigurationService.IsMainCashRegister)
            {
                txtCashRegisterStatus.Text = "Glavna kasa - lokalna baza podataka.";
                return;
            }

            string dbPath = Properties.Settings.Default.DbPath?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(dbPath))
            {
                txtCashRegisterStatus.Text = "Dodatna kasa nije povezana.";
                return;
            }

            string machineName = ExtractMachineNameFromUncPath(dbPath);

            txtCashRegisterStatus.Text = string.IsNullOrWhiteSpace(machineName)
                ? "Dodatna kasa - mrežna baza podataka."
                : $"Dodatna kasa - povezana sa {machineName}.";
        }

        private static string ExtractMachineNameFromUncPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            if (!path.StartsWith(@"\\", StringComparison.Ordinal)) return string.Empty;

            string trimmed = path.TrimStart('\\');
            int slashIndex = trimmed.IndexOf('\\');

            return slashIndex <= 0 ? string.Empty : trimmed.Substring(0, slashIndex);
        }

        // =====================================================
        // RESTART NAKON PROMJENE TIPA KASE
        // =====================================================

        private void AskForRestart(string message)
        {
            MessageBoxResult result = MessageBox.Show(
                message + "\n\n"
                + "Caupo je potrebno ponovo pokrenuti "
                + "da bi promjena potpuno stupila na snagu.\n\n"
                + "Ponovo pokrenuti sada?",
                "Caupo", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;
            RestartCaupo();
        }

        private static void RestartCaupo()
        {
            try
            {
                string? exePath = Environment.ProcessPath;

                if (string.IsNullOrWhiteSpace(exePath))
                    throw new InvalidOperationException("Nije moguće pronaći Caupo.exe.");

                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true,
                    WorkingDirectory = AppContext.BaseDirectory
                };

                Process.Start(startInfo);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SETTINGS] Restart greška: {ex}");

                MessageBox.Show(
                    "Caupo nije moguće automatski ponovo pokrenuti.\n\n" + ex.Message,
                    "Caupo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =====================================================
        // DBPATH -> VIEWMODEL/BINDING
        // =====================================================

        private void UpdateDbPathInViewModel(string databasePath)
        {
            if (DataContext is SettingsViewModel vm) vm.DbPath = databasePath;

            // Ako DbPath nema OnPropertyChanged(), osvježi binding.
            BindingExpression? binding = txtDBPath.GetBindingExpression(TextBox.TextProperty);
            binding?.UpdateTarget();
        }

        // =====================================================
        // LICENCIRANJE
        // =====================================================

        private LicenseManager? _licensingManager;
        private CustomerLicense? _localCustomerLicense;
        private int _localSubscriptionLicenseCount;
        private PaddleCustomer? _licensingCustomer;
        private string? _licensingTransactionId;
        private string? _licensingOperation;
        private bool _licensingBusy;
        private bool _licensingPaymentCompleted;
        private bool _licensingCheckoutInitializing;
        private System.Threading.CancellationTokenSource? _licensingPollCts;

        private async void LicenciranjeTab_Selected(object sender, RoutedEventArgs e)
        {
            if (!ReferenceEquals(e.OriginalSource, LicenciranjeTab) || _licensingManager != null) return;

            try
            {
                _licensingManager = new LicenseManager();
                await LoadLicensingAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LICENSING] Inicijalizacija: {ex}");
                LicenciranjePorukaText.Text = "Licenciranje nije dostupno: " + ex.Message;
            }
        }

        private async Task LoadLicensingAsync()
        {
            LicenseManager? manager = _licensingManager;
            if (manager == null || _licensingBusy) return;

            SetLicensingBusy(true);

            try
            {
                LicenseState local = manager.EvaluateLocal();

                LicencaStatusText.Text = local.Status.ToString();
                LicencaIsticeText.Text = FormatLicenseDate(local.PaidUntil);
                LicencaPlanText.Text = "—";
                LicencaPretplataText.Text = "—";
                LicencaPocetakText.Text = "—";
                LicencaPreostaloText.Text = "—";
                LicencaUredjajText.Text = "—";
                OstaleLicenceLista.ItemsSource = null;
                OstaleLicenceBrojText.Text = "—";

                _localCustomerLicense = null;
                _localSubscriptionLicenseCount = 0;
                OtkaziAutomatskuObnovuButton.IsEnabled = false;
                _licensingCustomer = null;
                DodatneLicencePlanCombo.ItemsSource = null;

                if (!local.CanOperate || string.IsNullOrWhiteSpace(local.LicenseId))
                {
                    LicenciranjePorukaText.Text = local.Message;
                    return;
                }

                CustomerLicensesResponse response = await manager.GetCustomerLicensesAsync();
                if (!IsLoaded || !ReferenceEquals(manager, _licensingManager)) return;

                UpdateCustomerLicenses(local, response);

                PaddleCatalogResponse catalog = await manager.GetCatalogAsync();
                if (!IsLoaded || !ReferenceEquals(manager, _licensingManager)) return;

                int? selectedPlan = (DodatneLicencePlanCombo.SelectedItem as PaddlePlan)?.Id;
                DodatneLicencePlanCombo.ItemsSource = catalog.Plans;
                DodatneLicencePlanCombo.SelectedItem = catalog.Plans.FirstOrDefault(x => x.Id == selectedPlan)
                    ?? catalog.Plans.FirstOrDefault();

                LicenciranjePorukaText.Text = "Podaci licenci su osvježeni.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LICENSING] Učitavanje: {ex}");
                LicenciranjePorukaText.Text = "Učitavanje licenci nije uspjelo: " + ex.Message;
            }
            finally
            {
                SetLicensingBusy(false);
            }
        }

        private void UpdateCustomerLicenses(LicenseState local, CustomerLicensesResponse response)
        {
            _licensingCustomer = response.Customer;
            _localCustomerLicense = response.Licenses.FirstOrDefault(x =>
                string.Equals(x.LicensePublicId, local.LicenseId, StringComparison.OrdinalIgnoreCase));

            if (_localCustomerLicense == null)
                throw new InvalidOperationException("Server nije vratio licencu ovog računara.");

            CustomerLicense current = _localCustomerLicense;
            CustomerLicenseSubscription? subscription = current.Subscription;

            _localSubscriptionLicenseCount = subscription == null ? 0 : response.Licenses.Count(x =>
                x.Subscription?.Id == subscription.Id);

            LicencaPlanText.Text = current.Plan.Name;
            LicencaStatusText.Text = GetLicenseDisplayStatus(current);
            LicencaStatusIcon.Foreground = GetLicenseStatusBrush(current);
            LicencaIsticeText.Text = FormatServerDate(current.ExpiresAt);
            LicencaPocetakText.Text = FormatServerDate(current.PeriodStart);
            LicencaUredjajText.Text = string.IsNullOrWhiteSpace(current.DeviceName)
                ? Environment.MachineName : current.DeviceName;

            LicencaPreostaloText.Text = "—";

            if (DateTimeOffset.TryParse(current.ExpiresAt, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out DateTimeOffset expiry))
            {
                TimeSpan remaining = expiry - DateTimeOffset.UtcNow;
                LicencaPreostaloText.Text = remaining <= TimeSpan.Zero ? "Istekla"
                    : $"{(int)Math.Ceiling(remaining.TotalDays)} dana";
            }

            LicencaPretplataText.Text = subscription == null ? "Ručna obnova"
                : subscription.CancelAtPeriodEnd ? "Automatska obnova otkazana za sljedeći period"
                : subscription.Status switch
                {
                    "active" => "Automatska obnova aktivna",
                    "trialing" => "Probna pretplata",
                    "past_due" => "Plaćanje obnove nije uspjelo",
                    "canceled" => "Automatska obnova otkazana",
                    _ => "Status obnove nije dostupan"
                };

            ObnoviLicencuButton.Content = current.CanResubscribe
                ? "Ponovo aktiviraj pretplatu" : "Obnovi licencu";

            var others = response.Licenses.Where(x =>
                !string.Equals(x.LicensePublicId, local.LicenseId, StringComparison.OrdinalIgnoreCase)).ToList();

            OstaleLicenceLista.ItemsSource = others.Select(x => new
            {
                x.Plan,
                x.ExpiresDisplay,
                DisplayStatus = GetLicenseDisplayStatus(x),
                StatusBrush = GetLicenseStatusBrush(x),
                DeviceDescription = string.IsNullOrWhiteSpace(x.DevicePublicId)
                    ? "Nije aktivirana"
                    : string.IsNullOrWhiteSpace(x.DeviceName) ? "Naziv kase nije dostupan" : x.DeviceName
            }).ToList();

            OstaleLicenceBrojText.Text = others.Count == 0 ? "Nema drugih licenci kupca"
                : $"Licenci: {others.Count} | Ukupno: {response.Licenses.Count}";

            UpdateLicensingButtons();
        }

        private static string GetLicenseDisplayStatus(CustomerLicense license)
        {
            if (DateTimeOffset.TryParse(license.ExpiresAt, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out DateTimeOffset expiry)
                && expiry <= DateTimeOffset.UtcNow)
                return "Istekla";

            return license.Status == "active" ? "Važeća" : "Nevažeća";
        }

        private static System.Windows.Media.Brush GetLicenseStatusBrush(CustomerLicense license) =>
            GetLicenseDisplayStatus(license) == "Važeća"
                ? System.Windows.Media.Brushes.ForestGreen
                : System.Windows.Media.Brushes.IndianRed;

        private async void OsvjeziLicenciranje_Click(object sender, RoutedEventArgs e) =>
            await LoadLicensingAsync();

        private async void OtvoriUpravljanjePlacanjima_Click(object sender, RoutedEventArgs e)
        {
            LicenseManager? manager = _licensingManager;

            if (manager == null || _licensingBusy || _licensingCustomer == null
                || _licensingTransactionId != null) return;

            SetLicensingBusy(true);

            try
            {
                string url = await manager.GetBillingPortalUrlAsync();

                if (!IsLoaded || !ReferenceEquals(manager, _licensingManager)) return;

                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });

                LicenciranjePorukaText.Text = "Stranica za upravljanje plaćanjima je otvorena.";
            }
            catch (HttpRequestException ex)
            {
                string message = $"HTTP {(int?)ex.StatusCode}: {ex.Message}";
                Debug.WriteLine($"[LICENSING] Upravljanje plaćanjima: {message}");
                ShowLicensingError(message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LICENSING] Otvaranje plaćanja: {ex.GetType().Name}");
                ShowLicensingError("Upravljanje plaćanjima trenutno nije dostupno.");
            }
            finally
            {
                SetLicensingBusy(false);
            }
        }

        private async void OtkaziAutomatskuObnovu_Click(object sender, RoutedEventArgs e)
        {
            LicenseManager? manager = _licensingManager;
            CustomerLicenseSubscription? subscription = _localCustomerLicense?.Subscription;

            if (manager == null || subscription == null || _licensingBusy || _licensingTransactionId != null
                || subscription.CancelAtPeriodEnd || subscription.Status is not ("active" or "past_due")) return;

            string scope = _localSubscriptionLicenseCount > 1
                ? $"Ova pretplata obuhvata {_localSubscriptionLicenseCount} licenci istog kupca. "
                    + "Otkazivanje zaustavlja automatsku obnovu SVIH licenci u toj pretplati."
                : "Otkazivanje zaustavlja buduću automatsku obnovu ove licence.";

            var popup = new YesNoPopup
            {
                Owner = Window.GetWindow(this),
                PopupTitle = "Caupo - Automatska obnova",
                PopupMessage = "Želite li otkazati automatsku obnovu pretplate?\n\n" + scope
                    + "\n\nVeć plaćene licence ostaju važeće do isteka svojih perioda.",
                ConfirmText = "Otkaži obnovu",
                CancelText = "Odustani"
            };

            if (popup.ShowDialog() != true) return;

            SetLicensingBusy(true);

            try
            {
                LicenseApiClient.SubscriptionCancellationResponse result =
                    await manager.CancelLocalSubscriptionAsync(subscription.Id);

                if (!IsLoaded || !ReferenceEquals(manager, _licensingManager)) return;

                SetLicensingBusy(false);
                await LoadLicensingAsync();

                if (IsLoaded)
                    LicenciranjePorukaText.Text = result.Status == "canceled"
                        ? "Automatska obnova je otkazana. Licenca ostaje važeća do isteka plaćenog perioda."
                        : "Otkazivanje automatske obnove je potvrđeno za kraj plaćenog perioda. "
                            + "Licenca ostaje aktivna; promjena statusa može se prikazati nakon obrade Paddle obavijesti.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LICENSING] Otkazivanje pretplate: {ex}");
                ShowLicensingError("Otkazivanje automatske obnove nije potvrđeno.\n\n" + ex.Message);
            }
            finally
            {
                SetLicensingBusy(false);
            }
        }

        private async void ObnoviLicencu_Click(object sender, RoutedEventArgs e)
        {
            LicenseManager? manager = _licensingManager;
            CustomerLicense? license = _localCustomerLicense;

            if (manager == null || license == null || _licensingBusy || _licensingTransactionId != null
                || (!license.CanRenew && !license.CanResubscribe)) return;

            SetLicensingBusy(true);

            try
            {
                string requestToken = LicenseApiClient.NewRequestToken();
                bool resubscribe = license.CanResubscribe;

                CheckoutResponse checkout = resubscribe
                    ? await manager.StartSubscriptionRenewalAsync(license.LicenseId, license.Plan.Id, requestToken)
                    : await manager.StartLicenseRenewalAsync(license.LicenseId, license.Plan.Id, requestToken);

                if (IsLoaded && ReferenceEquals(manager, _licensingManager))
                    await OpenLicensingCheckoutAsync(checkout, resubscribe ? "subscription_renewal" : "license_renewal");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LICENSING] Obnova: {ex}");
                ShowLicensingError("Obnova licence nije pokrenuta.\n\n" + ex.Message);
            }
            finally
            {
                SetLicensingBusy(false);
            }
        }

        private async void KupiDodatneLicence_Click(object sender, RoutedEventArgs e)
        {
            LicenseManager? manager = _licensingManager;
            if (manager == null || _licensingBusy || _licensingTransactionId != null) return;

            if (!TryGetAdditionalPurchase(out PaddlePlan? plan, out int quantity, out string renewal, out _))
            {
                ShowLicensingError("Odaberite ispravan plan, količinu i način plaćanja.");
                return;
            }

            SetLicensingBusy(true);

            try
            {
                CheckoutResponse checkout = await manager.StartAdditionalLicensesAsync(
                    plan!.Id, quantity, renewal, LicenseApiClient.NewRequestToken());

                if (IsLoaded && ReferenceEquals(manager, _licensingManager))
                    await OpenLicensingCheckoutAsync(checkout, "additional_licenses");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LICENSING] Kupovina: {ex}");
                ShowLicensingError("Kupovina nije pokrenuta.\n\n" + ex.Message);
            }
            finally
            {
                SetLicensingBusy(false);
            }
        }

        private void DodatneLicenceIzbor_Changed(object sender, SelectionChangedEventArgs e) =>
            UpdateAdditionalPrice();

        private void DodatneLicenceKolicina_Changed(object sender, TextChangedEventArgs e) =>
            UpdateAdditionalPrice();

        private bool TryGetAdditionalPurchase(out PaddlePlan? plan, out int quantity,
            out string renewal, out long unitAmount)
        {
            plan = DodatneLicencePlanCombo?.SelectedItem as PaddlePlan;
            quantity = 0;
            unitAmount = 0;
            renewal = (DodatneLicenceObnovaCombo?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;

            if (plan == null || !int.TryParse(DodatneLicenceKolicinaText?.Text, out quantity)
                || quantity is < 1 or > 10 || renewal is not ("subscription" or "one_time")) return false;

            int selectedQuantity = quantity;
            PaddlePriceTier? tier = plan.Tiers.FirstOrDefault(x =>
                selectedQuantity >= x.Min && (x.Max == null || selectedQuantity <= x.Max));

            if (tier == null || tier.Amount <= 0) return false;

            string? price = renewal == "subscription" ? tier.RecurringPriceId : tier.OneTimePriceId;
            if (string.IsNullOrWhiteSpace(price)) return false;

            unitAmount = tier.Amount;
            return true;
        }

        private void UpdateAdditionalPrice()
        {
            if (DodatneLicenceCijenaText == null || KupiDodatneLicenceButton == null) return;

            if (!TryGetAdditionalPurchase(out _, out int quantity, out _, out long unitAmount))
            {
                DodatneLicenceCijenaText.Text = "Odaberite plan i broj licenci.";
                KupiDodatneLicenceButton.IsEnabled = false;
                return;
            }

            var culture = CultureInfo.GetCultureInfo("de-DE");

            DodatneLicenceCijenaText.Text =
                $"{(unitAmount / 100m).ToString("N2", culture)} EUR × {quantity} = "
                + $"{(unitAmount * quantity / 100m).ToString("N2", culture)} EUR";

            KupiDodatneLicenceButton.IsEnabled = !_licensingBusy && _licensingManager != null
                && _licensingCustomer != null && _licensingTransactionId == null;
        }

        private async Task OpenLicensingCheckoutAsync(CheckoutResponse checkout, string operation)
        {
            if (_licensingCustomer == null || _licensingTransactionId != null)
                throw new InvalidOperationException("Plaćanje nije dostupno.");

            _licensingCheckoutInitializing = true;
            _licensingTransactionId = checkout.TransactionId;
            _licensingOperation = operation;
            _licensingPaymentCompleted = false;

            LicenciranjeCheckoutPanel.Visibility = Visibility.Visible;
            LicenciranjeCheckoutStatusText.Text = "Otvaranje plaćanja...";
            LicenciranjePaddleWebView.Height = 700;

            UpdateLicensingButtons();

            try
            {
                await LicenciranjePaddleWebView.EnsureCoreWebView2Async();
                if (!IsLoaded || _licensingTransactionId != checkout.TransactionId) return;

                LicenciranjePaddleWebView.CoreWebView2.WebMessageReceived -= LicensingPaddle_WebMessageReceived;
                LicenciranjePaddleWebView.CoreWebView2.WebMessageReceived += LicensingPaddle_WebMessageReceived;

                string theme = App.CurrentTheme == "Svijetla" ? "light" : "dark";

                LicenciranjePaddleWebView.CoreWebView2.Navigate(
                    "https://caupo.app/checkout/wpf.php?_ptxn="
                    + Uri.EscapeDataString(checkout.TransactionId) + "#" + theme);
            }
            catch
            {
                ResetLicensingCheckout();
                throw;
            }
            finally
            {
                _licensingCheckoutInitializing = false;
            }
        }

        private void LicensingPaddle_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string? transactionId = _licensingTransactionId;
            if (transactionId == null) return;

            if (!Uri.TryCreate(e.Source, UriKind.Absolute, out Uri? source)
                || source.Scheme != Uri.UriSchemeHttps
                || !string.Equals(source.Host, "caupo.app", StringComparison.OrdinalIgnoreCase)
                || source.AbsolutePath != "/checkout/wpf.php"
                || source.Query != "?_ptxn=" + transactionId) return;

            try
            {
                using JsonDocument document = JsonDocument.Parse(e.WebMessageAsJson);
                JsonElement root = document.RootElement;

                if (!root.TryGetProperty("type", out JsonElement type)
                    || !root.TryGetProperty("transaction_id", out JsonElement transaction)
                    || transaction.GetString() != transactionId) return;

                switch (type.GetString())
                {
                    case "caupo-checkout-ready":
                        SendLicensingCustomer();
                        break;

                    case "caupo-checkout-opened":
                        LicenciranjeCheckoutStatusText.Text = string.Empty;
                        break;

                    case "caupo-checkout-resize":
                        if (root.TryGetProperty("height", out JsonElement height)
                            && height.TryGetDouble(out double pixels) && double.IsFinite(pixels)
                            && pixels >= 200 && pixels <= 5000)
                            LicenciranjePaddleWebView.Height = Math.Ceiling(pixels);
                        break;

                    case "caupo-checkout-completed":
                        if (_licensingPaymentCompleted) break;

                        _licensingPaymentCompleted = true;
                        LicenciranjeCheckoutStatusText.Text =
                            "Plaćanje je prihvaćeno. Čeka se potvrda Caupo servera...";

                        _licensingPollCts?.Cancel();
                        _licensingPollCts?.Dispose();
                        _licensingPollCts = new System.Threading.CancellationTokenSource();

                        _ = ConfirmLicensingPurchaseAsync(
                            transactionId, _licensingOperation!, _licensingPollCts.Token);
                        break;

                    case "caupo-checkout-closed":
                        if (!_licensingPaymentCompleted)
                            LicenciranjeCheckoutStatusText.Text = "Plaćanje je zatvoreno.";
                        break;

                    case "caupo-checkout-error":
                        LicenciranjeCheckoutStatusText.Text =
                            root.TryGetProperty("message", out JsonElement message)
                                ? message.GetString() ?? "Greška pri plaćanju."
                                : "Greška pri plaćanju.";
                        break;
                }
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"[LICENSING] WebView poruka: {ex}");
            }
        }

        private async Task ConfirmLicensingPurchaseAsync(string transactionId, string operation,
            System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                LicenseManager? manager = _licensingManager;
                if (manager == null) return;

                // Najviše dva minuta automatske provjere, bez ponovnog pokretanja kupovine.
                for (int attempt = 0; attempt < 60; attempt++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        CustomerLicensesResponse response = await manager.GetCustomerLicensesAsync(
                            transactionId, cancellationToken);

                        if (cancellationToken.IsCancellationRequested
                            || _licensingTransactionId != transactionId || !IsLoaded) return;

                        CustomerOrderStatus? order = response.Order;

                        if (order?.Confirmed == true && order.OrderType == operation)
                        {
                            int issued = order.IssuedCount;
                            ResetLicensingCheckout();

                            if (operation is "license_renewal" or "subscription_renewal")
                                await manager.RefreshAsync();

                            await LoadLicensingAsync();

                            if (IsLoaded)
                                LicenciranjePorukaText.Text = operation switch
                                {
                                    "additional_licenses" => $"Kupovina potvrđena. Izdato je {issued} dodatnih licenci.",
                                    "subscription_renewal" => "Nova automatska pretplata je potvrđena. Licenca je produžena.",
                                    _ => "Obnova licence je potvrđena. Podaci su osvježeni."
                                };

                            return;
                        }

                        if (order?.Status == "failed")
                        {
                            LicenciranjeCheckoutStatusText.Text =
                                "Obrada narudžbe nije uspjela. Provjerite status uplate.";
                            return;
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        Debug.WriteLine($"[LICENSING] Provjera servera: {ex.Message}");
                    }

                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                }

                if (IsLoaded && _licensingTransactionId == transactionId)
                    LicenciranjeCheckoutStatusText.Text =
                        "Plaćanje je evidentirano, ali potvrda izdavanja još nije stigla. "
                        + "Možete zatvoriti prozor i kasnije osvježiti licence.";
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LICENSING] Potvrda kupovine: {ex}");

                if (IsLoaded && _licensingTransactionId == transactionId)
                    LicenciranjeCheckoutStatusText.Text =
                        "Provjera narudžbe nije uspjela: " + ex.Message;
            }
        }

        private void SendLicensingCustomer()
        {
            if (_licensingTransactionId == null || _licensingCustomer == null
                || LicenciranjePaddleWebView.CoreWebView2 == null) return;

            PaddleCustomer c = _licensingCustomer;

            string request = JsonSerializer.Serialize(new
            {
                type = "caupo-start-checkout",
                transaction_id = _licensingTransactionId,
                customer = new
                {
                    company_name = c.CompanyName,
                    contact_name = c.ContactName,
                    email = c.Email,
                    phone = c.Phone,
                    address = c.Address,
                    city = c.City,
                    postal_code = c.PostalCode,
                    country_code = c.CountryCode,
                    tax_number = c.TaxNumber
                }
            });

            LicenciranjePaddleWebView.CoreWebView2.PostWebMessageAsJson(request);
        }

        private async void ZatvoriLicenciranjeCheckout_Click(object sender, RoutedEventArgs e)
        {
            if (_licensingCheckoutInitializing) return;

            bool completed = _licensingPaymentCompleted;
            ResetLicensingCheckout();

            if (completed) await LoadLicensingAsync();
        }

        private void ResetLicensingCheckout()
        {
            _licensingPollCts?.Cancel();
            _licensingPollCts = null;
            _licensingTransactionId = null;
            _licensingOperation = null;
            _licensingPaymentCompleted = false;

            LicenciranjeCheckoutPanel.Visibility = Visibility.Collapsed;

            if (LicenciranjePaddleWebView.CoreWebView2 != null)
                LicenciranjePaddleWebView.CoreWebView2.Navigate("about:blank");

            UpdateLicensingButtons();
        }

        private void SetLicensingBusy(bool busy)
        {
            _licensingBusy = busy;
            UpdateLicensingButtons();
        }

        private void UpdateLicensingButtons()
        {
            if (ObnoviLicencuButton == null || KupiDodatneLicenceButton == null
                || OtvoriUpravljanjePlacanjimaButton == null) return;

            ObnoviLicencuButton.IsEnabled = !_licensingBusy && _licensingTransactionId == null
                && (_localCustomerLicense?.CanRenew == true || _localCustomerLicense?.CanResubscribe == true);

            CustomerLicenseSubscription? subscription = _localCustomerLicense?.Subscription;

            OtkaziAutomatskuObnovuButton.IsEnabled = !_licensingBusy && _licensingTransactionId == null
                && subscription != null && !subscription.CancelAtPeriodEnd
                && subscription.Status is "active" or "past_due";

            OtvoriUpravljanjePlacanjimaButton.IsEnabled = !_licensingBusy && _licensingTransactionId == null
                && _licensingManager != null && _licensingCustomer != null && _localCustomerLicense != null;

            UpdateAdditionalPrice();
        }

        private static string FormatLicenseDate(DateTimeOffset? date) =>
            date?.ToLocalTime().ToString("dd.MM.yyyy. HH:mm", CultureInfo.CurrentCulture) ?? "—";

        private static string FormatServerDate(string? date) =>
            DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime value)
                ? value.ToLocalTime().ToString("dd.MM.yyyy. HH:mm", CultureInfo.CurrentCulture)
                : "—";

        private void ShowLicensingError(string message)
        {
            LicenciranjePorukaText.Text = message;

            var dialog = new MyMessageBox();
            Window? owner = Window.GetWindow(this);

            if (owner != null)
            {
                dialog.Owner = owner;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }

            dialog.MessageTitle.Text = "Caupo - Licenciranje";
            dialog.MessageText.Text = message;
            dialog.ShowDialog();
        }

        private void DisposeLicensing()
        {
            _licensingPollCts?.Cancel();
            _licensingPollCts = null;
            _licensingTransactionId = null;

            if (LicenciranjePaddleWebView.CoreWebView2 != null)
                LicenciranjePaddleWebView.CoreWebView2.WebMessageReceived -= LicensingPaddle_WebMessageReceived;

            LicenciranjePaddleWebView.Dispose();
            _licensingManager?.Dispose();
            _licensingManager = null;
        }
    }
}
