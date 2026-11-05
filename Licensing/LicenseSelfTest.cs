using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Caupo.Licensing;

public static class LicenseSelfTest
{
    public static string Run()
    {
        var log = new StringBuilder();
        int passed = 0;
        int failed = 0;

        void Check(string name, Action test)
        {
            try
            {
                test();
                log.AppendLine($"PASS  {name}");
                passed++;
            }
            catch (Exception ex)
            {
                log.AppendLine($"FAIL  {name}: {ex.Message}");
                failed++;
            }
        }

        Check("Trajni identitet uređaja", () =>
        {
            using var first = LicenseDeviceIdentity.LoadOrCreate();
            using var second = LicenseDeviceIdentity.LoadOrCreate();

            if (first.PublicKey != second.PublicKey || first.FingerprintSha256 != second.FingerprintSha256)
                throw new Exception("Identitet se promijenio.");
        });

        Check("ECDSA P-256 potpis", () =>
        {
            using var device = LicenseDeviceIdentity.LoadOrCreate();
            using var key = ECDsa.Create();

            key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(device.PublicKey), out _);

            const string session = "cs_test_selftest";
            const long timestamp = 1793890000;
            const string nonce = "1234567890abcdef1234567890abcdef";

            string message = $"caupo-claim\n{session}\n{timestamp}\n{nonce}";
            byte[] signature = Convert.FromBase64String(device.SignClaim(session, timestamp, nonce));

            if (!key.VerifyData(Encoding.UTF8.GetBytes(message), signature, HashAlgorithmName.SHA256,
                    DSASignatureFormat.Rfc3279DerSequence))
                throw new Exception("Provjera potpisa nije uspjela.");

            if (key.VerifyData(Encoding.UTF8.GetBytes(message + "changed"), signature, HashAlgorithmName.SHA256,
                    DSASignatureFormat.Rfc3279DerSequence))
                throw new Exception("Izmijenjena poruka je prihvaćena.");
        });

        Check("DPAPI pohrana Checkout sesije", () =>
        {
            var storage = new LicenseStorage();
            string? original = storage.LoadCheckoutSession();
            string testSession = "cs_test_" + Guid.NewGuid().ToString("N");

            try
            {
                storage.SaveCheckoutSession(testSession);

                if (storage.LoadCheckoutSession() != testSession)
                    throw new Exception("Sesija nije ispravno učitana.");
            }
            finally
            {
                if (original is null)
                    storage.ClearCheckoutSession();
                else
                    storage.SaveCheckoutSession(original);
            }
        });

        Check("Odbijanje neispravnog JWT-a", () =>
        {
            using var trusted = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var keys = new System.Collections.Generic.Dictionary<string, string>
            {
                ["selftest"] = trusted.ExportSubjectPublicKeyInfoPem()
            };

            using var validator = new LicenseValidator(keys);
            LicenseState result = validator.Validate("invalid.jwt.token", "test-device", DateTimeOffset.UtcNow);

            if (result.Status != LicenseOperationalStatus.Invalid || result.CanOperate)
                throw new Exception("Neispravna licenca nije odbijena.");
        });

        log.AppendLine();
        log.AppendLine($"REZULTAT: {passed} PASS, {failed} FAIL");
        return log.ToString();
    }
}