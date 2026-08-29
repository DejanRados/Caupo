using Caupo.Properties;
using System.Diagnostics;
using System.IO;

namespace Caupo.Services
{
    public static class DataFolderService
    {
        private const string DefaultDataFolder =
            @"C:\DsoftData";

        private const string DatabaseFileName =
            "sysFormWPF.db";


        public static string LocalDataFolder =>
            DefaultDataFolder;


        public static string LocalDatabasePath =>
            Path.Combine (
                DefaultDataFolder,
                DatabaseFileName);


        public static string CurrentDatabasePath
        {
            get
            {
                string dbPath =
                    Settings.Default.DbPath;

                if(string.IsNullOrWhiteSpace (dbPath))
                {
                    return LocalDatabasePath;
                }

                return dbPath.Trim ();
            }
        }


        public static void Initialize()
        {
            Debug.WriteLine (
                "[DATA] Inicijalizacija data foldera...");


            // =====================================================
            // 1. KREIRAJ C:\DsoftData
            // =====================================================

            if(!Directory.Exists (DefaultDataFolder))
            {
                Debug.WriteLine (
                    $"[DATA] Kreiram folder: {DefaultDataFolder}");

                Directory.CreateDirectory (
                    DefaultDataFolder);
            }
            else
            {
                Debug.WriteLine (
                    $"[DATA] Folder već postoji: {DefaultDataFolder}");
            }


            // =====================================================
            // 2. PROVJERI POSTOJI LI LOKALNA BAZA
            // =====================================================

            if(File.Exists (LocalDatabasePath))
            {
                Debug.WriteLine (
                    $"[DATA] Baza već postoji: {LocalDatabasePath}");

                return;
            }


            // =====================================================
            // 3. POČETNA BAZA IZ DATA FOLDERA APLIKACIJE
            // =====================================================

            string sourceDatabase =
                Path.Combine (
                    AppContext.BaseDirectory,
                    "Data",
                    DatabaseFileName);


            Debug.WriteLine (
                $"[DATA] Tražim početnu bazu: {sourceDatabase}");


            if(!File.Exists (sourceDatabase))
            {
                throw new FileNotFoundException (
                    "Početna baza nije pronađena.",
                    sourceDatabase);
            }


            // =====================================================
            // 4. KOPIRAJ SAMO AKO NE POSTOJI
            // =====================================================

            File.Copy (
                sourceDatabase,
                LocalDatabasePath,
                overwrite: false);


            Debug.WriteLine (
                $"[DATA] Baza kopirana u: {LocalDatabasePath}");
        }


        public static void EnsureDefaultDbPath()
        {
            string currentPath =
                Settings.Default.DbPath?.Trim ()
                ?? string.Empty;


          

            bool isEmpty =
                string.IsNullOrWhiteSpace (
                    currentPath);


            bool isDevelopmentPath =
                currentPath.Contains (
                    @"\bin\Debug\",
                    StringComparison.OrdinalIgnoreCase)
                ||
                currentPath.Contains (
                    @"\bin\Release\",
                    StringComparison.OrdinalIgnoreCase);


            if(!isEmpty &&
               !isDevelopmentPath)
            {
                Debug.WriteLine (
                    $"[DATA] Postojeći DbPath zadržan: {currentPath}");

                return;
            }


            Settings.Default.DbPath =
                LocalDatabasePath;

            Settings.Default.Save ();


            Debug.WriteLine (
                $"[DATA] DbPath postavljen na: {LocalDatabasePath}");
        }
    }
}