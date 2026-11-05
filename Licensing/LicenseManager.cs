using Caupo.Licensing.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Caupo.Licensing;

public sealed class LicenseManager : IDisposable
{
    private readonly LicenseDeviceIdentity _device;
    private readonly LicenseApiClient _api;
    private readonly LicenseValidator _validator;
    private readonly LicenseStorage _storage;
    private readonly SemaphoreSlim _sync = new(1, 1);

    private bool _disposed;

    public LicenseState State { get; private set; } = LicenseState.NotActivated();
    public bool CanOperate => EvaluateLocal().CanOperate;
    public string? PendingCheckoutSession => _storage.LoadCheckoutSession();

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
                return State = new LicenseState(LicenseOperationalStatus.Invalid, "Identitet instalacije se promijenio.");

            DateTimeOffset now = DateTimeOffset.UtcNow;

            if (now < stored.LastCheckedUtc.AddMinutes(-5))
                return State = new LicenseState(LicenseOperationalStatus.ClockError, "Windows sat je vraćen unazad. Provjerite vrijeme.");

            LicenseState validated = _validator.Validate(stored.Token, stored.DeviceId, now);

            if (validated.Status is LicenseOperationalStatus.Active or LicenseOperationalStatus.GracePeriod)
                _storage.UpdateLastChecked(now);

            return State = validated;
        }
        catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException
            or System.IO.IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            return State = new LicenseState(LicenseOperationalStatus.Invalid, "Lokalni podaci licence nisu dostupni ili su neispravni.");
        }
    }

    public async Task<CheckoutResponse> StartPurchaseAsync(
        string company, string email, string plan,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _sync.WaitAsync(cancellationToken);

        try
        {
            CheckoutResponse checkout = await _api.StartCheckoutAsync(
                _device, company, email, plan, cancellationToken);

            _storage.SaveCheckoutSession(checkout.SessionId);

            return checkout;
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<LicenseState> RefreshAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _sync.WaitAsync(cancellationToken);

        try
        {
            LicenseState local = EvaluateLocal();
            string? sessionId = _storage.LoadCheckoutSession();

            if (string.IsNullOrWhiteSpace(sessionId))
                return local;

            LicenseClaimResponse response = await _api.GetCheckoutStatusAsync(
                _device, sessionId, cancellationToken);

            if (response.Status == "processing")
                return local;

            if (response.Status == "expired")
                return EvaluateLocal();

            if (response.Status != "active" || string.IsNullOrWhiteSpace(response.Token)
                || string.IsNullOrWhiteSpace(response.DeviceId))
            {
                return State = new LicenseState(
                    LicenseOperationalStatus.Invalid,
                    "Server je vratio neispravnu licencu.");
            }

            LicenseState verified = _validator.Validate(
                response.Token, response.DeviceId, DateTimeOffset.UtcNow);

            if (verified.Status is not (LicenseOperationalStatus.Active or LicenseOperationalStatus.GracePeriod))
                return State = verified;

            if (!string.Equals(verified.LicenseId, response.LicenseId, StringComparison.Ordinal)
                || !string.Equals(verified.DeviceId, response.DeviceId, StringComparison.Ordinal))
            {
                return State = new LicenseState(
                    LicenseOperationalStatus.Invalid,
                    "Podaci potpisane licence nisu usklađeni.");
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

    public async Task<LicenseState> ActivateManualAsync(
        string licenseKey, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _sync.WaitAsync(cancellationToken);

        try
        {
            LicenseClaimResponse response = await _api.ActivateManualAsync(
                _device, licenseKey, cancellationToken);

            if (response.Status != "active" || string.IsNullOrWhiteSpace(response.Token)
                || string.IsNullOrWhiteSpace(response.DeviceId))
            {
                return State = new LicenseState(
                    LicenseOperationalStatus.Invalid,
                    "Server nije odobrio aktivaciju.");
            }

            LicenseState verified = _validator.Validate(
                response.Token, response.DeviceId, DateTimeOffset.UtcNow);

            if (verified.Status is not (LicenseOperationalStatus.Active or LicenseOperationalStatus.GracePeriod)
                || verified.LicenseId != response.LicenseId
                || verified.DeviceId != response.DeviceId)
            {
                return State = new LicenseState(
                    LicenseOperationalStatus.Invalid,
                    "Digitalna provjera licence nije uspjela.");
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

    public async Task<LicenseState> InitializeAsync(CancellationToken cancellationToken = default)
    {
        LicenseState local = EvaluateLocal();

        try
        {
            return await RefreshAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return State = local;
        }
        catch (TaskCanceledException)
        {
            return State = local;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

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