using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Caupo.Licensing;

public static class LicenseSelfTest
{
    public static string Run()
    {
        var log = new StringBuilder();
        int passed = 0, failed = 0;

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

        void Expect(bool condition, string message)
        {
            if (!condition)
                throw new Exception(message);
        }

        Check("Trajni identitet uređaja", () =>
        {
            using var first = LicenseDeviceIdentity.LoadOrCreate();
            using var second = LicenseDeviceIdentity.LoadOrCreate();

            Expect(first.PublicKey == second.PublicKey, "Promijenjen javni ključ.");
            Expect(first.FingerprintSha256 == second.FingerprintSha256, "Promijenjen fingerprint.");
        });

        Check("ECDSA P-256 potpis uređaja", () =>
        {
            using var device = LicenseDeviceIdentity.LoadOrCreate();
            using var key = ECDsa.Create();
            key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(device.PublicKey), out _);

            const string session = "cs_test_selftest";
            const long timestamp = 1793890000;
            const string nonce = "1234567890abcdef1234567890abcdef";

            string message = $"caupo-claim\n{session}\n{timestamp}\n{nonce}";
            byte[] signature = Convert.FromBase64String(device.SignClaim(session, timestamp, nonce));

            Expect(key.VerifyData(Encoding.UTF8.GetBytes(message), signature, HashAlgorithmName.SHA256,
                DSASignatureFormat.Rfc3279DerSequence), "Potpis nije prihvaćen.");

            Expect(!key.VerifyData(Encoding.UTF8.GetBytes(message + "changed"), signature, HashAlgorithmName.SHA256,
                DSASignatureFormat.Rfc3279DerSequence), "Izmijenjena poruka je prihvaćena.");
        });

        Check("DPAPI pohrana Checkout sesije", () =>
        {
            var storage = new LicenseStorage();
            string? original = storage.LoadCheckoutSession();
            string test = "cs_test_" + Guid.NewGuid().ToString("N");

            try
            {
                storage.SaveCheckoutSession(test);
                Expect(storage.LoadCheckoutSession() == test, "Sesija nije ispravno učitana.");
            }
            finally
            {
                if (original is null)
                    storage.ClearCheckoutSession();
                else
                    storage.SaveCheckoutSession(original);
            }
        });

        using var serverKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var validator = new LicenseValidator(new Dictionary<string, string>
        {
            ["selftest"] = serverKey.ExportSubjectPublicKeyInfoPem()
        });

        const string deviceId = "test-device";
        DateTimeOffset now = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        DateTimeOffset paidUntil = now.AddDays(30);
        string validToken = CreateToken(serverKey, deviceId, now, paidUntil);

        Check("Odbijanje neispravnog JWT-a", () =>
        {
            LicenseState result = validator.Validate("invalid.jwt.token", deviceId, now);
            Expect(result.Status == LicenseOperationalStatus.Invalid && !result.CanOperate,
                "Neispravan JWT nije odbijen.");
        });

        Check("Prihvatanje ispravnog ES256 JWT-a", () =>
        {
            LicenseState result = validator.Validate(validToken, deviceId, now);
            Expect(result.Status == LicenseOperationalStatus.Active, "Važeći JWT nije prihvaćen.");
            Expect(result.CanOperate, "Važeća licenca ne dozvoljava rad.");
            Expect(result.LicenseId == "test-license", "Pogrešan identitet licence.");
        });

        Check("Odbijanje drugog uređaja", () =>
        {
            LicenseState result = validator.Validate(validToken, "other-device", now);
            Expect(result.Status == LicenseOperationalStatus.Invalid && !result.CanOperate,
                "Licenca je prihvaćena na drugom uređaju.");
        });

        Check("Odbijanje izmijenjenog JWT-a", () =>
        {
            string[] parts = validToken.Split('.');
            byte[] altered = Encoding.UTF8.GetBytes("{\"license_id\":\"changed\"}");
            string forged = parts[0] + "." + Base64Url(altered) + "." + parts[2];

            LicenseState result = validator.Validate(forged, deviceId, now);
            Expect(result.Status == LicenseOperationalStatus.Invalid && !result.CanOperate,
                "Izmijenjeni JWT je prihvaćen.");
        });

        Check("Sedmodnevni dodatni period", () =>
        {
            LicenseState result = validator.Validate(validToken, deviceId, paidUntil.AddDays(3));
            Expect(result.Status == LicenseOperationalStatus.GracePeriod, "Dodatni period nije prepoznat.");
            Expect(result.CanOperate && result.RequiresWarning, "Dodatni period nema očekivana prava.");
            Expect(result.GraceUntil == paidUntil.AddDays(7), "Pogrešan kraj dodatnog perioda.");
        });

        Check("Blokada nakon dodatnih 7 dana", () =>
        {
            LicenseState result = validator.Validate(validToken, deviceId, paidUntil.AddDays(7));
            Expect(result.Status == LicenseOperationalStatus.Expired, "Istek nije prepoznat.");
            Expect(!result.CanOperate, "Istekla licenca dozvoljava rad.");
        });

        log.AppendLine();
        log.AppendLine($"REZULTAT: {passed} PASS, {failed} FAIL");
        return log.ToString();
    }

    private static string CreateToken(ECDsa key, string deviceId, DateTimeOffset now, DateTimeOffset paidUntil)
    {
        var header = new
        {
            alg = "ES256",
            typ = "JWT",
            kid = "selftest"
        };

        var payload = new
        {
            iss = "https://caupo.app",
            aud = "caupo-pos",
            iat = now.AddMinutes(-1).ToUnixTimeSeconds(),
            nbf = now.AddMinutes(-1).ToUnixTimeSeconds(),
            exp = paidUntil.AddDays(7).ToUnixTimeSeconds(),
            license_id = "test-license",
            device_id = deviceId,
            paid_until = paidUntil.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        string signingInput = Base64Url(JsonSerializer.SerializeToUtf8Bytes(header)) + "."
            + Base64Url(JsonSerializer.SerializeToUtf8Bytes(payload));

        byte[] signature = key.SignData(Encoding.ASCII.GetBytes(signingInput),
            HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        return signingInput + "." + Base64Url(signature);
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}