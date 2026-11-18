using Caupo.Licensing.Models;
using System;
using System.Collections.Generic;
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

            LicenseState validated = _validator.Validate(stored.Token, stored.DeviceId, now);

            if (validated.Status is LicenseOperationalStatus.Active or LicenseOperationalStatus.GracePeriod)
                _storage.UpdateLastChecked(now);

            return State = validated;
        }
        catch (Exception ex) when (ex is CryptographicException or System.IO.IOException
            or UnauthorizedAccessException or JsonException)
        {
            return State = new LicenseState(
                LicenseOperationalStatus.Invalid, "Lokalni podaci licence nisu dostupni ili su neispravni.");
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

    public async Task<LicenseState> ActivateAsync(
        string licenseKey, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _sync.WaitAsync(cancellationToken);

        try
        {
            LicenseClaimResponse response = await _api.ActivateAsync(
                _device, licenseKey, cancellationToken);

            if (response.Status != "active" || string.IsNullOrWhiteSpace(response.Token)
                || string.IsNullOrWhiteSpace(response.DeviceId))
            {
                return State = new LicenseState(
                    LicenseOperationalStatus.Invalid, "Server nije odobrio aktivaciju.");
            }

            LicenseState verified = _validator.Validate(
                response.Token, response.DeviceId, DateTimeOffset.UtcNow);

            if (verified.Status is not (LicenseOperationalStatus.Active or LicenseOperationalStatus.GracePeriod)
                || verified.LicenseId != response.LicenseId
                || verified.DeviceId != response.DeviceId)
            {
                return State = new LicenseState(
                    LicenseOperationalStatus.Invalid, "Digitalna provjera licence nije uspjela.");
            }

            _storage.SaveBoundLicense(
                response.Token, response.DeviceId, _device.PublicKey, DateTimeOffset.UtcNow);

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

    public Task<LicenseState> InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(EvaluateLocal());
    }

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
