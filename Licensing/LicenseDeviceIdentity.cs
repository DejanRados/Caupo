using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
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

        try
        {
            PublicKey = Convert.ToBase64String(_key.ExportSubjectPublicKeyInfo());
            FingerprintSha256 = CreateFingerprint(PublicKey);
        }
        catch
        {
            _key.Dispose();
            throw;
        }
    }

    public static LicenseDeviceIdentity LoadOrCreate()
    {
        string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Caupo", "Licensing");

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
            machineGuid = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography", "MachineGuid", null) as string;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
        {
            // Koristi identitet instalacije ako Windows ne dozvoljava čitanje.
        }

        string source = string.IsNullOrWhiteSpace(machineGuid)
            ? "Caupo-V2-Installation\n" + publicKey
            : "Caupo-V2-Machine\n" + machineGuid.Trim();

        string processorId = ReadHardwareValues("SELECT ProcessorId FROM Win32_Processor", "ProcessorId");
        string motherboardSerial = ReadHardwareValues("SELECT SerialNumber FROM Win32_BaseBoard", "SerialNumber");
        string diskSerial = ReadSystemDiskSerial();

        source += "\nCPU:" + processorId
            + "\nBOARD:" + motherboardSerial
            + "\nDISK:" + diskSerial;

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant();
    }

    private static string ReadHardwareValues(string query, string property)
    {
        var values = new SortedSet<string>(StringComparer.Ordinal);

        using var searcher = new ManagementObjectSearcher(query);
        using var results = searcher.Get();

        foreach (ManagementObject item in results)
        {
            using (item)
            {
                string? value = NormalizeHardwareValue(item[property]?.ToString());

                if (value is not null)
                    values.Add(value);
            }
        }

        if (values.Count == 0)
            throw new InvalidOperationException($"Nije moguće očitati hardverski identifikator: {property}.");

        return string.Join("|", values);
    }

    private static string ReadSystemDiskSerial()
    {
        string? systemDrive = Path.GetPathRoot(Environment.SystemDirectory)?.TrimEnd('\\');

        if (string.IsNullOrWhiteSpace(systemDrive))
            throw new InvalidOperationException("Nije moguće odrediti sistemski disk.");

        var serials = new SortedSet<string>(StringComparer.Ordinal);

        using var partitionsSearcher = new ManagementObjectSearcher(
            $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{systemDrive}'}} WHERE AssocClass=Win32_LogicalDiskToPartition");

        using var partitions = partitionsSearcher.Get();

        foreach (ManagementObject partition in partitions)
        {
            using (partition)
            {
                using var disks = partition.GetRelated("Win32_DiskDrive");

                foreach (ManagementObject disk in disks)
                {
                    using (disk)
                    {
                        string? serial = NormalizeHardwareValue(disk["SerialNumber"]?.ToString());

                        if (serial is not null)
                            serials.Add(serial);
                    }
                }
            }
        }

        if (serials.Count == 0)
            throw new InvalidOperationException("Nije moguće očitati serijski broj sistemskog diska.");

        return string.Join("|", serials);
    }

    private static string? NormalizeHardwareValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim().ToUpperInvariant();

        if (value is "UNKNOWN" or "NONE" or "N/A" or "NOT AVAILABLE"
            or "NOT SPECIFIED" or "DEFAULT STRING" or "TO BE FILLED BY O.E.M."
            or "SYSTEM SERIAL NUMBER" or "INVALID" or "0")
            return null;

        bool allZeros = true;

        foreach (char character in value)
        {
            if (character != '0' && character != '-' && !char.IsWhiteSpace(character))
            {
                allZeros = false;
                break;
            }
        }

        return allZeros ? null : value;
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

    public string SignLicenseCheck(string licenseId, string deviceId, long timestamp, string nonce)
    {
        string message = $"caupo-check-license-v1\n{licenseId}\n{deviceId}\n{FingerprintSha256}\n{timestamp}\n{nonce}";
        return Sign(message);
    }

    public string SignRegisteredRequest(string operation, string deviceId, long timestamp, string nonce, string payloadHash)
    {
        if (operation is not ("new_purchase" or "additional_licenses" or "license_renewal"
            or "subscription_renewal" or "customer_licenses" or "subscription_cancel"))
            throw new ArgumentException("Nepoznata operacija licenciranja.", nameof(operation));

        if (!Guid.TryParseExact(deviceId, "D", out _) || string.IsNullOrWhiteSpace(nonce)
            || nonce.Length != 32 || !IsLowerHex(nonce)
            || string.IsNullOrWhiteSpace(payloadHash) || payloadHash.Length != 64 || !IsLowerHex(payloadHash))
            throw new ArgumentException("Neispravni podaci za digitalno potpisivanje.");

        string message = $"caupo-purchase-v1\n{operation}\n{deviceId}\n{FingerprintSha256}\n{timestamp}\n{nonce}\n{payloadHash}";
        return Sign(message);
    }

    private static bool IsLowerHex(string value)
    {
        foreach (char c in value)
        {
            if (c is not (>= '0' and <= '9' or >= 'a' and <= 'f'))
                return false;
        }

        return true;
    }

    private string Sign(string message)
    {
        byte[] signature = _key.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        return Convert.ToBase64String(signature);
    }

    public void Dispose() => _key.Dispose();
}