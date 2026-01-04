using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Windows;
using static Caupo.Data.DatabaseTables;



namespace Caupo.Data
{
    public class AppDbContext : DbContext
    {

        private readonly string DbPath;

        public AppDbContext()
        {

            // DbPath =  Path.Combine (Properties.Settings.Default.DbPath, "sysFormWPF.db");
            DbPath = Globals.CurrentDbPath;
            Debug.WriteLine ($"[DEBUG]  DbPath Globals.CurrentDbPath: {DbPath}");
            File.WriteAllText ("crash.log", $"[DEBUG]  DbPath Globals.CurrentDbPath: {DbPath}");
            if(string.IsNullOrEmpty (DbPath))
            {
                // fallback – ako user još nije odabrao
                DbPath = Path.Combine (Directory.GetCurrentDirectory (), "Data", "sysFormWPF.db");
                Debug.WriteLine ($"[DEBUG] Fallback DbPath: {DbPath}");
                File.WriteAllText ("crash.log", $"[DEBUG] Fallback DbPath: {DbPath}");
            }

        }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if(!File.Exists (DbPath))
            {
                Debug.WriteLine ($"[ERROR] Database file does not exist: {DbPath}");
                MessageBox.Show ($"Database not found at: {DbPath}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // EF Core pravi konekciju iz connection stringa
            options.UseSqlite ($"Data Source={DbPath}");

           
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ako neka tablica nema ključ
            modelBuilder.Entity<TblFirma> ().HasNoKey ();

            base.OnModelCreating (modelBuilder);
        }

        // Nadjačaj SaveChanges sa retry logikom
        public override int SaveChanges()
        {
            return SaveChangesWithRetryAsync (false).GetAwaiter ().GetResult ();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return SaveChangesWithRetryAsync (true, cancellationToken);
        }

        private async Task<int> SaveChangesWithRetryAsync(bool isAsync, CancellationToken cancellationToken = default)
        {
            int retries = 3;
            while(true)
            {
                try
                {
                    if(isAsync)
                        return await base.SaveChangesAsync (cancellationToken);
                    else
                        return base.SaveChanges ();
                }
                catch(SqliteException ex) when(ex.SqliteErrorCode == 5) // database is locked
                {
                    retries--;
                    if(retries <= 0)
                        throw;

                    await Task.Delay (200, cancellationToken);
                }
            }
        }


        public DbSet<DatabaseTables.TblRadnici> Radnici { get; set; }
        public DbSet<DatabaseTables.TblArtikli> Artikli { get; set; }
        public DbSet<DatabaseTables.TblBrojBlokaSank> BrojBloka { get; set; }
        public DbSet<DatabaseTables.TblBrojPokretanja> BrojPokretanja { get; set; }
        public DbSet<DatabaseTables.TblDobavljaci> Dobavljaci { get; set; }
        public DbSet<DatabaseTables.TblFaktura> Faktura { get; set; }
        public DbSet<DatabaseTables.TblFakturaStavka> FakturaStavka { get; set; }
        public DbSet<DatabaseTables.TblFirma> Firma { get; set; }
        public DbSet<DatabaseTables.TblJediniceMjere> JediniceMjere { get; set; }
        public DbSet<DatabaseTables.TblKategorije> Kategorije { get; set; }
        public DbSet<DatabaseTables.TblKnjigaKuhinje> KnjigaKuhinje { get; set; }
        public DbSet<DatabaseTables.TblKnjigaSanka> KnjigaSanka { get; set; }
        public DbSet<DatabaseTables.TblKuhinja> Kuhinja { get; set; }
        public DbSet<DatabaseTables.TblKuhinjaStavke> KuhinjaStavke { get; set; }
        public DbSet<DatabaseTables.TblKupci> Kupci { get; set; }
        public DbSet<DatabaseTables.TblKupciNarudzba> KupciNarudzba { get; set; }
        public DbSet<DatabaseTables.TblKupciNarudzbeStavka> KupciNarudzbaStavke { get; set; }
        public DbSet<DatabaseTables.TblNarudzbe> Narudzbe { get; set; }
        public DbSet<DatabaseTables.TblNarudzbeStavke> NarudzbeStavke { get; set; }
        public DbSet<DatabaseTables.TblNormativ> Normativ { get; set; }
        public DbSet<DatabaseTables.TblNormativPica> NormativPica { get; set; }
        public DbSet<DatabaseTables.TblOtpis> Otpis { get; set; }
        public DbSet<DatabaseTables.TblOtpisStavka> OtpisStavka { get; set; }
        public DbSet<DatabaseTables.TblPoreskeStope> PoreskeStope { get; set; }
        public DbSet<DatabaseTables.TblRacuni> Racuni { get; set; }
        public DbSet<DatabaseTables.TblRacunStavka> RacunStavka { get; set; }
        public DbSet<DatabaseTables.TblReklamiraniRacun> ReklamiraniRacun { get; set; }
        public DbSet<DatabaseTables.TblReklamiraniStavka> ReklamiraniStavka { get; set; }
        public DbSet<DatabaseTables.TblRepromaterijal> Repromaterijal { get; set; }
        public DbSet<DatabaseTables.TblUlaz> Ulaz { get; set; }
        public DbSet<DatabaseTables.TblUlazStavke> UlazStavke { get; set; }
        public DbSet<DatabaseTables.TblUlazRepromaterijal> UlazRepromaterijal { get; set; }
        public DbSet<DatabaseTables.TblUlazRepromaterijalStavka> UlazRepromaterijalStavka { get; set; }
        public DbSet<DatabaseTables.TblUplateDobavljacima> UplateDobavljacima { get; set; }
        public DbSet<DatabaseTables.TblUplateKupaca> UplateKupaca { get; set; }




    }

}
