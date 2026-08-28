using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.IO;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Data
{
    public class AppDbContext : DbContext
    {
        private readonly string _dbPath;


        private const int DatabaseTimeoutSeconds = 10;

  
        private const int MaxSaveRetries = 5;

        public AppDbContext()
        {
            _dbPath = ResolveDatabasePath ();

            Debug.WriteLine ($"[DB] Database path: {_dbPath}");
        }

        /// <summary>
        /// Određuje koju bazu Caupo koristi.
        /// Primarno koristi Globals.CurrentDbPath.
        ///
        /// Fallback trenutno ostavljamo radi kompatibilnosti sa postojećim
        /// instalacijama. Kada uvedemo novi DsoftData first-run sistem,
        /// ovaj fallback možemo ukloniti.
        /// </summary>
        private static string ResolveDatabasePath()
        {
            if(!string.IsNullOrWhiteSpace (Globals.CurrentDbPath))
            {
                return Globals.CurrentDbPath;
            }

            string fallbackPath = Path.Combine (
                AppContext.BaseDirectory,
                "Data",
                "sysFormWPF.db"
            );

            Debug.WriteLine (
                $"[DB] Globals.CurrentDbPath nije postavljen. " +
                $"Koristim fallback: {fallbackPath}"
            );

            return fallbackPath;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if(optionsBuilder.IsConfigured)
                return;

            if(string.IsNullOrWhiteSpace (_dbPath))
            {
                throw new InvalidOperationException (
                    "Putanja do baze podataka nije definisana."
                );
            }

            if(!File.Exists (_dbPath))
            {
                throw new FileNotFoundException (
                    $"Baza podataka nije pronađena: {_dbPath}",
                    _dbPath
                );
            }

            var connectionStringBuilder = new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
  
                Mode = SqliteOpenMode.ReadWrite,

                Cache = SqliteCacheMode.Default,

                DefaultTimeout = DatabaseTimeoutSeconds
            };

            optionsBuilder.UseSqlite (connectionStringBuilder.ToString ());

#if DEBUG
            optionsBuilder.EnableDetailedErrors ();
#endif
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // TblFirma trenutno nema primary key.
            modelBuilder.Entity<TblFirma> ().HasNoKey ();

            base.OnModelCreating (modelBuilder);
        }

        // =========================================================
        // SAVE CHANGES - SYNCHRONOUS
        // =========================================================

        public override int SaveChanges()
        {
            return SaveChangesWithRetry ();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            return SaveChangesWithRetry (acceptAllChangesOnSuccess);
        }

        private int SaveChangesWithRetry(
            bool acceptAllChangesOnSuccess = true)
        {
            for(int attempt = 1; attempt <= MaxSaveRetries; attempt++)
            {
                try
                {
                    return base.SaveChanges (acceptAllChangesOnSuccess);
                }
                catch(SqliteException ex) when(IsDatabaseLocked (ex))
                {
                    if(attempt >= MaxSaveRetries)
                    {
                        Debug.WriteLine (
                            $"[DB] SaveChanges nije uspio nakon " +
                            $"{MaxSaveRetries} pokušaja. " +
                            $"SQLite error: {ex.SqliteErrorCode} - {ex.Message}"
                        );

                        throw;
                    }

                    int delay = GetRetryDelay (attempt);

                    Debug.WriteLine (
                        $"[DB] Baza je zauzeta. " +
                        $"SaveChanges pokušaj {attempt}/{MaxSaveRetries}. " +
                        $"Ponovni pokušaj za {delay} ms."
                    );

                    Thread.Sleep (delay);
                }
            }

            // Teoretski nedostižno.
            throw new InvalidOperationException (
                "Neočekivana greška prilikom spremanja baze."
            );
        }

        // =========================================================
        // SAVE CHANGES - ASYNCHRONOUS
        // =========================================================

        public override Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            return SaveChangesWithRetryAsync (
                acceptAllChangesOnSuccess: true,
                cancellationToken
            );
        }

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            return SaveChangesWithRetryAsync (
                acceptAllChangesOnSuccess,
                cancellationToken
            );
        }

        private async Task<int> SaveChangesWithRetryAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken)
        {
            for(int attempt = 1; attempt <= MaxSaveRetries; attempt++)
            {
                try
                {
                    return await base.SaveChangesAsync (
                        acceptAllChangesOnSuccess,
                        cancellationToken
                    );
                }
                catch(SqliteException ex) when(IsDatabaseLocked (ex))
                {
                    if(attempt >= MaxSaveRetries)
                    {
                        Debug.WriteLine (
                            $"[DB] SaveChangesAsync nije uspio nakon " +
                            $"{MaxSaveRetries} pokušaja. " +
                            $"SQLite error: {ex.SqliteErrorCode} - {ex.Message}"
                        );

                        throw;
                    }

                    int delay = GetRetryDelay (attempt);

                    Debug.WriteLine (
                        $"[DB] Baza je zauzeta. " +
                        $"SaveChangesAsync pokušaj {attempt}/{MaxSaveRetries}. " +
                        $"Ponovni pokušaj za {delay} ms."
                    );

                    await Task.Delay (delay, cancellationToken);
                }
            }

   
            throw new InvalidOperationException (
                "Neočekivana greška prilikom spremanja baze."
            );
        }

        /// <summary>
        /// SQLITE_BUSY   = 5
        /// SQLITE_LOCKED = 6
        /// </summary>
        private static bool IsDatabaseLocked(SqliteException ex)
        {
            return ex.SqliteErrorCode == 5 ||
                   ex.SqliteErrorCode == 6;
        }

        /// <summary>
        /// Progresivno čekanje:
        ///
        /// pokušaj 1 -> 250 ms
        /// pokušaj 2 -> 500 ms
        /// pokušaj 3 -> 750 ms
        /// pokušaj 4 -> 1000 ms
        /// </summary>
        private static int GetRetryDelay(int attempt)
        {
            return 250 * attempt;
        }

        // =========================================================
        // TABLES
        // =========================================================

        public DbSet<TblRadnici> Radnici { get; set; }

        public DbSet<TblArtikli> Artikli { get; set; }

        public DbSet<TblBrojBlokaSank> BrojBloka { get; set; }

        public DbSet<TblBrojPokretanja> BrojPokretanja { get; set; }

        public DbSet<TblDobavljaci> Dobavljaci { get; set; }

        public DbSet<TblFaktura> Faktura { get; set; }

        public DbSet<TblFakturaStavka> FakturaStavka { get; set; }

        public DbSet<TblFirma> Firma { get; set; }

        public DbSet<TblJediniceMjere> JediniceMjere { get; set; }

        public DbSet<TblKategorije> Kategorije { get; set; }

        public DbSet<TblKnjigaKuhinje> KnjigaKuhinje { get; set; }

        public DbSet<TblKnjigaSanka> KnjigaSanka { get; set; }

        public DbSet<TblKuhinja> Kuhinja { get; set; }

        public DbSet<TblKuhinjaStavke> KuhinjaStavke { get; set; }

        public DbSet<TblKupci> Kupci { get; set; }

        public DbSet<TblKupciNarudzba> KupciNarudzba { get; set; }

        public DbSet<TblKupciNarudzbeStavka> KupciNarudzbaStavke { get; set; }

        public DbSet<TblNarudzbe> Narudzbe { get; set; }

        public DbSet<TblNarudzbeStavke> NarudzbeStavke { get; set; }

        public DbSet<TblNormativ> Normativ { get; set; }

        public DbSet<TblNormativPica> NormativPica { get; set; }

        public DbSet<TblOtpis> Otpis { get; set; }

        public DbSet<TblOtpisStavka> OtpisStavka { get; set; }

        public DbSet<TblPoreskeStope> PoreskeStope { get; set; }

        public DbSet<TblRacuni> Racuni { get; set; }

        public DbSet<TblRacunStavka> RacunStavka { get; set; }

        public DbSet<TblReklamiraniRacun> ReklamiraniRacun { get; set; }

        public DbSet<TblReklamiraniStavka> ReklamiraniStavka { get; set; }

        public DbSet<TblRepromaterijal> Repromaterijal { get; set; }

        public DbSet<TblUlaz> Ulaz { get; set; }

        public DbSet<TblUlazStavke> UlazStavke { get; set; }

        public DbSet<TblUlazRepromaterijal> UlazRepromaterijal { get; set; }

        public DbSet<TblUlazRepromaterijalStavka> UlazRepromaterijalStavka { get; set; }

        public DbSet<TblUplateDobavljacima> UplateDobavljacima { get; set; }

        public DbSet<TblUplateKupaca> UplateKupaca { get; set; }
    }
}