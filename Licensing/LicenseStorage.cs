using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Caupo.Licensing;

public sealed class LicenseStorage
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Caupo-Licensing-Storage-v2");
    private readonly string _directory;

    public LicenseStorage()
    {
        _directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Caupo", "Licensing");

        Directory.CreateDirectory(_directory);
    }

    public sealed class StoredLicense
    {
        public string Token { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public DateTimeOffset LastCheckedUtc { get; set; }
    }

    public void SaveCheckoutSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || sessionId.Length > 255)
            throw new ArgumentException("Neispravan Checkout session ID.", nameof(sessionId));

        Save("checkout.dat", sessionId);
    }

    public string? LoadCheckoutSession() => Load("checkout.dat");

    public void ClearCheckoutSession()
    {
        string path = Path.Combine(_directory, "checkout.dat");

        if (File.Exists(path))
            File.Delete(path);
    }

    public void SaveBoundLicense(string token, string deviceId, string publicKey, DateTimeOffset lastCheckedUtc)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 65536
            || string.IsNullOrWhiteSpace(deviceId) || deviceId.Length > 128
            || string.IsNullOrWhiteSpace(publicKey) || publicKey.Length > 1024)
            throw new ArgumentException("Neispravna licenca.");

        var license = new StoredLicense
        {
            Token = token,
            DeviceId = deviceId,
            PublicKey = publicKey,
            LastCheckedUtc = lastCheckedUtc
        };

        Save("license.dat", JsonSerializer.Serialize(license));
    }

    public StoredLicense? LoadBoundLicense()
    {
        string? json = Load("license.dat");

        if (json is null)
            return null;

        return JsonSerializer.Deserialize<StoredLicense>(json);
    }

    public void UpdateLastChecked(DateTimeOffset utcNow)
    {
        StoredLicense? stored = LoadBoundLicense();

        if (stored is null)
            return;

        if (utcNow <= stored.LastCheckedUtc)
            return;

        stored.LastCheckedUtc = utcNow;
        Save("license.dat", JsonSerializer.Serialize(stored));
    }

    private void Save(string filename, string value)
    {
        string path = Path.Combine(_directory, filename);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        byte[] plain = Encoding.UTF8.GetBytes(value);

        try
        {
            byte[] protectedBytes = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);

            try
            {
                File.WriteAllBytes(temporary, protectedBytes);
                File.Move(temporary, path, true);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plain);
        }
    }

    private string? Load(string filename)
    {
        string path = Path.Combine(_directory, filename);

        if (!File.Exists(path))
            return null;

        byte[] encrypted = File.ReadAllBytes(path);
        byte[] plain = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);

        try
        {
            return Encoding.UTF8.GetString(plain);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plain);
        }
    }
}