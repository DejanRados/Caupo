using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Caupo.Licensing;

public sealed class LicenseValidator : IDisposable
{
    private const string Issuer = "https://caupo.app";
    private const string Audience = "caupo-pos";
    private static readonly TimeSpan GracePeriod = TimeSpan.FromDays(7);

    private readonly Dictionary<string, ECDsa> _trustedKeys = new(StringComparer.Ordinal);

    public LicenseValidator(IReadOnlyDictionary<string, string> trustedPublicKeys)
    {
        if (trustedPublicKeys.Count == 0)
            throw new ArgumentException("Nije konfigurisan javni ključ licencnog servera.");

        try
        {
            foreach (var pair in trustedPublicKeys)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    throw new ArgumentException("Neispravan ID potpisnog ključa.");

                ECDsa key = ECDsa.Create();

                try
                {
                    key.ImportFromPem(pair.Value);

                    if (key.KeySize != 256)
                        throw new CryptographicException("Server mora koristiti ECDSA P-256.");

                    _trustedKeys.Add(pair.Key, key);
                }
                catch
                {
                    key.Dispose();
                    throw;
                }
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public LicenseState Validate(string token, string expectedDeviceId, DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 65536 || string.IsNullOrWhiteSpace(expectedDeviceId))
            return Invalid();

        try
        {
            string[] parts = token.Split('.');

            if (parts.Length != 3)
                return Invalid();

            byte[] headerBytes = Decode(parts[0]);
            byte[] payloadBytes = Decode(parts[1]);
            byte[] signature = Decode(parts[2]);

            if (signature.Length != 64)
                return Invalid();

            using JsonDocument header = JsonDocument.Parse(headerBytes);
            using JsonDocument payload = JsonDocument.Parse(payloadBytes);

            JsonElement h = header.RootElement;
            JsonElement p = payload.RootElement;

            if (!h.TryGetProperty("alg", out JsonElement algorithm) || algorithm.GetString() != "ES256"
                || !h.TryGetProperty("kid", out JsonElement keyId) || keyId.ValueKind != JsonValueKind.String
                || !_trustedKeys.TryGetValue(keyId.GetString()!, out ECDsa? key))
                return Invalid();

            byte[] signedBytes = Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]);

            if (!key.VerifyData(signedBytes, signature, HashAlgorithmName.SHA256,
                    DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
                return Invalid();

            if (ReadString(p, "iss") != Issuer || !ValidAudience(p)
                || ReadString(p, "device_id") != expectedDeviceId)
                return Invalid();

            string? licenseId = ReadString(p, "license_id");
            string? deviceId = ReadString(p, "device_id");

            if (string.IsNullOrWhiteSpace(licenseId))
                return Invalid();

            if (!TryUnixTime(p, "iat", out DateTimeOffset issuedAt)
                || !TryUnixTime(p, "nbf", out DateTimeOffset notBefore)
                || !TryUnixTime(p, "exp", out DateTimeOffset expiresAt)
                || !TryPaidUntil(p, out DateTimeOffset paidUntil))
                return Invalid();

            if (notBefore > issuedAt || paidUntil <= issuedAt || issuedAt > utcNow.AddMinutes(5)
                || notBefore > utcNow.AddMinutes(5)
                || expiresAt < paidUntil
                || expiresAt > paidUntil.Add(GracePeriod).AddSeconds(1))
                return Invalid();

            if (utcNow >= expiresAt)
                return new LicenseState(LicenseOperationalStatus.Expired, "Licenca je istekla.", licenseId, deviceId, paidUntil, expiresAt);

            if (utcNow >= paidUntil)
            {
                return new LicenseState(
                    LicenseOperationalStatus.GracePeriod,
                    "Plaćeni period je istekao. Caupo radi u dodatnom sedmodnevnom periodu.",
                    licenseId, deviceId, paidUntil, expiresAt);
            }

            return new LicenseState(LicenseOperationalStatus.Active, "Licenca je važeća.", licenseId, deviceId, paidUntil, expiresAt);
        }
        catch (Exception ex) when (ex is FormatException or JsonException or CryptographicException
            or ArgumentException or InvalidOperationException or OverflowException)
        {
            return Invalid();
        }
    }

    private static bool ValidAudience(JsonElement payload)
    {
        if (!payload.TryGetProperty("aud", out JsonElement audience))
            return false;

        if (audience.ValueKind == JsonValueKind.String)
            return audience.GetString() == Audience;

        if (audience.ValueKind != JsonValueKind.Array)
            return false;

        foreach (JsonElement item in audience.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && item.GetString() == Audience)
                return true;
        }

        return false;
    }

    private static string? ReadString(JsonElement json, string name)
    {
        return json.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static bool TryUnixTime(JsonElement json, string name, out DateTimeOffset result)
    {
        result = default;

        if (!json.TryGetProperty(name, out JsonElement value) || !value.TryGetInt64(out long seconds))
            return false;

        try
        {
            result = DateTimeOffset.FromUnixTimeSeconds(seconds);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static bool TryPaidUntil(JsonElement json, out DateTimeOffset result)
    {
        result = default;
        string? value = ReadString(json, "paid_until");

        return value is not null && value.EndsWith('Z')
            && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out result);
    }

    private static byte[] Decode(string value)
    {
        if (value.Length == 0 || value.Length > 65536)
            throw new FormatException("Neispravan JWT segment.");

        string normalized = value.Replace('-', '+').Replace('_', '/');
        normalized = normalized.PadRight((normalized.Length + 3) / 4 * 4, '=');

        return Convert.FromBase64String(normalized);
    }

    private static LicenseState Invalid() =>
        new(LicenseOperationalStatus.Invalid, "Digitalni potpis ili sadržaj licence nije ispravan.");

    public void Dispose()
    {
        foreach (ECDsa key in _trustedKeys.Values)
            key.Dispose();

        _trustedKeys.Clear();
    }
}