using Caupo.Licensing.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Caupo.Licensing;

public sealed class LicenseManager : IDisposable
{
    private const string ServerKeyId = "caupo-751c6702ded63a08d785b5f5";
    private const string ServerPublicKey = """
        -----BEGIN PUBLIC KEY-----
        MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEwFl+AbiwbYorPTHMAIN5BuV781N7
        wOSmGrniyaoTLrXiGNFvoMKFXJCVaDI4fSwoVuxOMbkrUYVyjSSSP/dUfA==
        -----END PUBLIC KEY-----
        """;

    private readonly LicenseDeviceIdentity _device;
    private readonly LicenseApiClient _api;
    private readonly LicenseValidator _validator;
    private readonly LicenseStorage _storage;
    private readonly SemaphoreSlim _sync = new(1, 1);
    private bool _disposed;

    public LicenseState State { get; private set; } = LicenseState.NotActivated();
    public PaddleCustomer? LastActivatedCustomer { get; private set; }
    public bool CanOperate => EvaluateLocal().CanOperate;

    public LicenseManager() : this(new Dictionary<string, string>
    {
        [ServerKeyId] = ServerPublicKey
    })
    {
    }

    public LicenseManager(IReadOnlyDictionary<string, string> trustedServerKeys)
    {
        _device = LicenseDeviceIdentity.LoadOrCreate();
        _api = new LicenseApiClient();
        _validator = new LicenseValidator(trustedServerKeys);
        _storage = new LicenseStorage();
    }

    public LicenseState EvaluateLocal()
    {
        ThrowIfDisposed();

        try
        {
            LicenseStorage.StoredLicense? stored = _storage.LoadBoundLicense();

            if (stored is null)
                return State = LicenseState.NotActivated();

            if (!string.Equals(stored.PublicKey, _device.PublicKey, StringComparison.Ordinal))
                return State = new LicenseState(
                    LicenseOperationalStatus.Invalid, "Identitet instalacije se promijenio.");

            DateTimeOffset now = DateTimeOffset.UtcNow;

            if (now < stored.LastCheckedUtc.AddMinutes(-5))
                return State = new LicenseState(
                    LicenseOperationalStatus.ClockError, "Windows sat je vraćen unazad. Provjerite vrijeme.");

            LicenseState validated = _validator.Validate(
                stored.Token, stored.DeviceId, _device.FingerprintSha256, now);

            if (validated.Status is LicenseOperationalStatus.Active or LicenseOperationalStatus.GracePeriod)
                _storage.UpdateLastChecked(now);

            return State = validated;
        }
        catch (Exception ex) when (ex is CryptographicException or IOException
            or UnauthorizedAccessException or JsonException)
        {
            return State = new LicenseState(
                LicenseOperationalStatus.Invalid, "Lokalni podaci licence nisu dostupni ili su neispravni.");
        }
    }

    public async Task<LicenseState> RefreshAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _sync.WaitAsync(cancellationToken);

        try
        {
            LicenseState local = EvaluateLocal();

            if (local.Status is LicenseOperationalStatus.NotActivated
                or LicenseOperationalStatus.Invalid or LicenseOperationalStatus.ClockError
                || string.IsNullOrWhiteSpace(local.LicenseId)
                || string.IsNullOrWhiteSpace(local.DeviceId))
                return local;

            try
            {
                LicenseApiClient.LicenseCheckResponse response = await _api.CheckLicenseAsync(
                    _device, local.LicenseId, local.DeviceId, cancellationToken);

                if (response.Status == "inactive")
                    return State = new LicenseState(
                        LicenseOperationalStatus.Invalid, "Licenca više nije aktivna na serveru.");

                if (response.Status != "active")
                    return State = local;

                if (!string.Equals(response.LicenseId, local.LicenseId, StringComparison.Ordinal)
                    || !string.Equals(response.DeviceId, local.DeviceId, StringComparison.Ordinal)
                    || string.IsNullOrWhiteSpace(response.Token))
                    return State = local;

                DateTimeOffset now = DateTimeOffset.UtcNow;
                LicenseState verified = _validator.Validate(
                    response.Token, response.DeviceId, _device.FingerprintSha256, now);

                if (verified.Status != LicenseOperationalStatus.Active
                    || verified.LicenseId != local.LicenseId
                    || verified.DeviceId != local.DeviceId)
                    return State = local;

                _storage.SaveBoundLicense(response.Token, response.DeviceId, _device.PublicKey, now);
                return State = verified;
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or IOException
                or CryptographicException or UnauthorizedAccessException
                or TaskCanceledException && !cancellationToken.IsCancellationRequested)
            {
                return State = local;
            }
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<PaddleCatalogResponse> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return await _api.GetCatalogAsync(cancellationToken);
    }

    public async Task<CheckoutResponse> StartPurchaseAsync(
        PaddleCustomer customer, int planId, int quantity, string renewal, string requestToken,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _sync.WaitAsync(cancellationToken);

        try
        {
            return await _api.StartCheckoutAsync(
                customer, planId, quantity, renewal, requestToken, cancellationToken);
        }
        finally
        {
            _sync.Release();
        }
    }

    public Task<LicenseApiClient.WpfLicenseResult> GetWpfLicenseAsync(
        string transactionId, string requestToken, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _api.GetWpfLicenseAsync(transactionId, requestToken, cancellationToken);
    }

    public async Task<CustomerLicensesResponse> GetCustomerLicensesAsync(
        string? transactionId = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _sync.WaitAsync(cancellationToken);

        try
        {
            LicenseState local = RequireLocalLicense();
            return await LoadCustomerLicensesAsync(local, transactionId, cancellationToken);
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<string> GetBillingPortalUrlAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _sync.WaitAsync(cancellationToken);

        try
        {
            LicenseState local = RequireLocalLicense();
            return await _api.GetBillingPortalUrlAsync(_device, local.DeviceId!, cancellationToken);
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<LicenseApiClient.SubscriptionCancellationResponse> CancelLocalSubscriptionAsync(
        int subscriptionId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _sync.WaitAsync(cancellationToken);

        try
        {
            LicenseState local = RequireLocalLicense();
            CustomerLicensesResponse overview = await LoadCustomerLicensesAsync(local, null, cancellationToken);

            CustomerLicense? license = overview.Licenses.FirstOrDefault(x =>
                string.Equals(x.LicensePublicId, local.LicenseId, StringComparison.OrdinalIgnoreCase));

            CustomerLicenseSubscription? subscription = license?.Subscription;

            if (subscription == null || subscription.Id != subscriptionId || subscription.CancelAtPeriodEnd
                || subscription.Status is not ("active" or "past_due"))
                throw new InvalidOperationException("Automatska obnova lokalne licence nije dostupna za otkazivanje.");

            LicenseApiClient.SubscriptionCancellationResponse result = await _api.CancelSubscriptionAsync(
                _device, local.DeviceId!, subscriptionId, cancellationToken);

            if (result.SubscriptionId != subscriptionId
                || result.Status is not ("cancellation_scheduled" or "canceled"))
                throw new InvalidOperationException("Server nije potvrdio otkazivanje automatske obnove.");

            return result;
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<CheckoutResponse> StartLicenseRenewalAsync(
        int licenseId, int planId, string requestToken, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _sync.WaitAsync(cancellationToken);

        try
        {
            LicenseState local = RequireLocalLicense();
            CustomerLicensesResponse overview = await LoadCustomerLicensesAsync(local, null, cancellationToken);

            CustomerLicense? license = overview.Licenses.FirstOrDefault(x =>
                x.LicenseId == licenseId && string.Equals(x.LicensePublicId, local.LicenseId,
                    StringComparison.OrdinalIgnoreCase));

            if (license == null || license.Plan.Id != planId || !license.CanRenew)
                throw new InvalidOperationException("Ručna obnova nije dostupna za lokalnu licencu.");

            return await _api.StartLicenseRenewalAsync(
                _device, local.DeviceId!, licenseId, planId, requestToken, cancellationToken);
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<CheckoutResponse> StartSubscriptionRenewalAsync(
        int licenseId, int planId, string requestToken, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _sync.WaitAsync(cancellationToken);

        try
        {
            LicenseState local = RequireLocalLicense();
            CustomerLicensesResponse overview = await LoadCustomerLicensesAsync(local, null, cancellationToken);

            CustomerLicense? license = overview.Licenses.FirstOrDefault(x =>
                x.LicenseId == licenseId && string.Equals(x.LicensePublicId, local.LicenseId,
                    StringComparison.OrdinalIgnoreCase));

            if (license == null || license.Plan.Id != planId || !license.CanResubscribe)
                throw new InvalidOperationException("Ponovna aktivacija pretplate nije dostupna za lokalnu licencu.");

            return await _api.StartSubscriptionRenewalAsync(
                _device, local.DeviceId!, licenseId, planId, requestToken, cancellationToken);
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<CheckoutResponse> StartAdditionalLicensesAsync(
        int planId, int quantity, string renewal, string requestToken,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _sync.WaitAsync(cancellationToken);

        try
        {
            LicenseState local = RequireLocalLicense();
            return await _api.StartAdditionalLicensesAsync(
                _device, local.DeviceId!, planId, quantity, renewal, requestToken, cancellationToken);
        }
        finally
        {
            _sync.Release();
        }
    }

    private LicenseState RequireLocalLicense()
    {
        LicenseState local = EvaluateLocal();

        if (!local.CanOperate || string.IsNullOrWhiteSpace(local.LicenseId)
            || string.IsNullOrWhiteSpace(local.DeviceId))
            throw new InvalidOperationException("Potrebna je aktivirana licenca ovog računara.");

        return local;
    }

    private async Task<CustomerLicensesResponse> LoadCustomerLicensesAsync(
        LicenseState local, string? transactionId, CancellationToken cancellationToken)
    {
        CustomerLicensesResponse result = await _api.GetCustomerLicensesAsync(
            _device, local.DeviceId!, transactionId, cancellationToken);

        if (result.Customer is null || result.Licenses is null || !result.Licenses.Any(x =>
            string.Equals(x.LicensePublicId, local.LicenseId, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Server nije vratio lokalnu licencu kupca.");

        if (transactionId != null && (result.Order == null
            || !string.Equals(result.Order.TransactionId, transactionId, StringComparison.Ordinal)))
            throw new InvalidOperationException("Server nije potvrdio traženu transakciju.");

        return result;
    }

    public async Task<LicenseState> ActivateAsync(
        string licenseKey, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _sync.WaitAsync(cancellationToken);

        try
        {
            LastActivatedCustomer = null;

            LicenseClaimResponse response = await _api.ActivateAsync(
                _device, licenseKey, cancellationToken);

            if (response.Status != "active" || string.IsNullOrWhiteSpace(response.Token)
                || string.IsNullOrWhiteSpace(response.DeviceId))
            {
                return State = new LicenseState(
                    LicenseOperationalStatus.Invalid, "Server nije odobrio aktivaciju.");
            }

            LicenseState verified = _validator.Validate(
                response.Token, response.DeviceId, _device.FingerprintSha256, DateTimeOffset.UtcNow);

            if (verified.Status is not (LicenseOperationalStatus.Active or LicenseOperationalStatus.GracePeriod)
                || verified.LicenseId != response.LicenseId
                || verified.DeviceId != response.DeviceId)
            {
                return State = new LicenseState(
                    LicenseOperationalStatus.Invalid, "Digitalna provjera licence nije uspjela.");
            }

            _storage.SaveBoundLicense(
                response.Token, response.DeviceId, _device.PublicKey, DateTimeOffset.UtcNow);

            LastActivatedCustomer = response.Customer;
            return State = verified;
        }
        finally
        {
            _sync.Release();
        }
    }

    public Task<LicenseState> ActivateManualAsync(
        string licenseKey, CancellationToken cancellationToken = default) =>
        ActivateAsync(licenseKey, cancellationToken);

    public Task<LicenseState> InitializeAsync(CancellationToken cancellationToken = default) =>
        RefreshAsync(cancellationToken);

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _api.Dispose();
        _validator.Dispose();
        _device.Dispose();
        _sync.Dispose();
    }
}