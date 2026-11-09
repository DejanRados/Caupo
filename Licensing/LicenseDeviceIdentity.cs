
using Microsoft.Win32;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Caupo.Licensing;

public sealed class LicenseDeviceIdentity : IDisposable
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Caupo-Licensing-Device-v2");
    private readonly ECDsa _key;

    public string PublicKey { get; }
    public string FingerprintSha256 { get; }

    private LicenseDeviceIdentity(ECDsa key)
    {
        _key = key;
        PublicKey = Convert.ToBase64String(_key.ExportSubjectPublicKeyInfo());
        FingerprintSha256 = CreateFingerprint(PublicKey);
    }

    public static LicenseDeviceIdentity LoadOrCreate()
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Caupo", "Licensing");

        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "device.key");

        if (File.Exists(path))
            return new LicenseDeviceIdentity(LoadKey(path));

        using ECDsa generated = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] privateKey = generated.ExportPkcs8PrivateKey();

        try
        {
            byte[] encrypted = ProtectedData.Protect(privateKey, Entropy, DataProtectionScope.CurrentUser);
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                File.WriteAllBytes(temporary, encrypted);
                try
                {
                    File.Move(temporary, path);
                }
                catch (IOException) when (File.Exists(path))
                {
                    // Drugi proces je već kreirao identitet instalacije.
                }
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKey);
        }

        return new LicenseDeviceIdentity(LoadKey(path));
    }

    private static ECDsa LoadKey(string path)
    {
        byte[] encrypted = File.ReadAllBytes(path);
        byte[] privateKey = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);

        try
        {
            ECDsa key = ECDsa.Create();
            try
            {
                key.ImportPkcs8PrivateKey(privateKey, out int bytesRead);
                if (bytesRead != privateKey.Length || key.KeySize != 256)
                    throw new CryptographicException("Neispravan identitet Caupo uređaja.");

                return key;
            }
            catch
            {
                key.Dispose();
                throw;
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKey);
        }
    }

    private static string CreateFingerprint(string publicKey)
    {
        string? machineGuid = null;

        try
        {
            machineGuid = Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography",
                "MachineGuid", null) as string;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
        {
            // Koristi stabilni identitet instalacije ako Windows ne dozvoljava čitanje.
        }

        string source = string.IsNullOrWhiteSpace(machineGuid)
            ? "Caupo-V2-Installation\n" + publicKey
            : "Caupo-V2-Machine\n" + machineGuid.Trim();

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant();
    }

    public string SignCheckout(string plan, string fingerprint, string company, string email, long timestamp, string nonce)
    {
        string message = $"caupo-checkout\n{plan}\n{fingerprint}\n{company}\n{email}\n{timestamp}\n{nonce}";
        return Sign(message);
    }

    public string SignClaim(string sessionId, long timestamp, string nonce)
    {
        return Sign($"caupo-claim\n{sessionId}\n{timestamp}\n{nonce}");
    }

    public string SignActivation(string licenseKey, long timestamp, string nonce)
    {
        string message = $"caupo-activate\n{licenseKey.Trim().ToUpperInvariant()}\n{FingerprintSha256}\n{PublicKey}\n{timestamp}\n{nonce}";
        return Sign(message);
    }

    private string Sign(string message)
    {
        byte[] signature = _key.SignData(
            Encoding.UTF8.GetBytes(message),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.Rfc3279DerSequence);

        return Convert.ToBase64String(signature);
    }

    public void Dispose() => _key.Dispose();
}
