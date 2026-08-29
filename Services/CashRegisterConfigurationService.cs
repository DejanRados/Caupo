using Caupo.Properties;
using System.Diagnostics;
using System.IO;

namespace Caupo.Services
{
    public static class CashRegisterConfigurationService
    {
        public const string MainCashRegister =
            "Main";

        public const string SecondaryCashRegister =
            "Secondary";


        public static string CashRegisterType
        {
            get
            {
                string value =
                    Settings.Default.CashRegisterType?.Trim ()
                    ?? string.Empty;

                if(string.Equals (
                    value,
                    SecondaryCashRegister,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return SecondaryCashRegister;
                }

                return MainCashRegister;
            }
        }


        public static bool IsMainCashRegister =>
            string.Equals (
                CashRegisterType,
                MainCashRegister,
                StringComparison.OrdinalIgnoreCase);


        public static bool IsSecondaryCashRegister =>
            string.Equals (
                CashRegisterType,
                SecondaryCashRegister,
                StringComparison.OrdinalIgnoreCase);


        // =====================================================
        // STARTUP
        // =====================================================

        public static void PrepareStartupDatabasePath()
        {
            if(IsMainCashRegister)
            {
                SetMainCashRegister ();

                Debug.WriteLine (
                    "[CASH REGISTER] Tip kase: Glavna kasa");

                Debug.WriteLine (
                    $"[CASH REGISTER] Lokalna baza: {Settings.Default.DbPath}");

                return;
            }


            string databasePath =
                Settings.Default.DbPath?.Trim ()
                ?? string.Empty;


            if(string.IsNullOrWhiteSpace (
                databasePath))
            {
                throw new InvalidOperationException (
                    "Dodatna kasa nema podešenu putanju do baze podataka.");
            }


            if(!databasePath.StartsWith (
                @"\\",
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException (
                    "Dodatna kasa nema ispravnu mrežnu putanju do baze podataka.");
            }


            if(!File.Exists (
                databasePath))
            {
                throw new FileNotFoundException (
                    "Baza podataka glavne kase trenutno nije dostupna.",
                    databasePath);
            }


            Globals.CurrentDbPath =
                databasePath;


            Debug.WriteLine (
                "[CASH REGISTER] Tip kase: Dodatna kasa");

            Debug.WriteLine (
                $"[CASH REGISTER] Mrežna baza: {databasePath}");
        }


        // =====================================================
        // GLAVNA KASA
        // =====================================================

        public static void SetMainCashRegister()
        {
            Settings.Default.CashRegisterType =
                MainCashRegister;

            Settings.Default.DbPath =
                DataFolderService.LocalDatabasePath;

            Settings.Default.Save ();


            Globals.CurrentDbPath =
                DataFolderService.LocalDatabasePath;


            Debug.WriteLine (
                "[CASH REGISTER] Podešena glavna kasa.");

            Debug.WriteLine (
                $"[CASH REGISTER] DbPath = {Globals.CurrentDbPath}");
        }


        // =====================================================
        // DODATNA KASA
        // =====================================================

        public static async Task<MainCashRegisterInfo?>
            FindAndConfigureSecondaryCashRegisterAsync(
                int timeoutMilliseconds = 3000)
        {
            using var discovery =
                new CaupoDiscoveryService ();


            List<MainCashRegisterInfo> found =
                await discovery
                    .FindMainCashRegistersAsync (
                        timeoutMilliseconds);


            if(found.Count == 0)
            {
                Debug.WriteLine (
                    "[CASH REGISTER] Glavna kasa nije pronađena.");

                return null;
            }


            /*
             * U jednom Caupo sistemu postoji samo
             * jedna glavna kasa.
             */

            MainCashRegisterInfo main =
                found[0];


            await ValidateRemoteDatabaseAsync (
                main.DatabasePath);


            Settings.Default.CashRegisterType =
                SecondaryCashRegister;

            Settings.Default.DbPath =
                main.DatabasePath;

            Settings.Default.Save ();


            Globals.CurrentDbPath =
                main.DatabasePath;


            Debug.WriteLine (
                "[CASH REGISTER] Podešena dodatna kasa.");

            Debug.WriteLine (
                $"[CASH REGISTER] Glavna kasa: {main.MachineName}");

            Debug.WriteLine (
                $"[CASH REGISTER] IP: {main.IpAddress}");

            Debug.WriteLine (
                $"[CASH REGISTER] DbPath = {main.DatabasePath}");


            return main;
        }


        // =====================================================
        // PROVJERA REMOTE BAZE
        // =====================================================

        private static async Task ValidateRemoteDatabaseAsync(
            string databasePath)
        {
            if(string.IsNullOrWhiteSpace (
                databasePath))
            {
                throw new InvalidOperationException (
                    "Glavna kasa je vratila praznu putanju do baze.");
            }


            if(!databasePath.StartsWith (
                @"\\",
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException (
                    "Glavna kasa je vratila neispravnu mrežnu putanju.");
            }


            if(!File.Exists (
                databasePath))
            {
                throw new FileNotFoundException (
                    "Glavna kasa je pronađena, ali baza podataka nije dostupna.",
                    databasePath);
            }


            string? databaseFolder =
                Path.GetDirectoryName (
                    databasePath);


            if(string.IsNullOrWhiteSpace (
                databaseFolder))
            {
                throw new InvalidOperationException (
                    "Putanja do mrežnog foldera baze nije ispravna.");
            }


            string testFile =
                Path.Combine (
                    databaseFolder,
                    $".caupo_test_{Guid.NewGuid ():N}.tmp");


            try
            {
                Debug.WriteLine (
                    $"[CASH REGISTER] Test pisanja: {testFile}");


                await File.WriteAllTextAsync (
                    testFile,
                    "CAUPO");


                File.Delete (
                    testFile);


                Debug.WriteLine (
                    "[CASH REGISTER] Test pisanja uspješan.");
            }
            catch
            {
                try
                {
                    if(File.Exists (
                        testFile))
                    {
                        File.Delete (
                            testFile);
                    }
                }
                catch
                {
                }


                throw new InvalidOperationException (
                    "Glavna kasa je pronađena, ali pristup bazi nije moguć. " +
                    "Provjerite mrežnu vezu između kasa.");
            }
        }
    }
}
