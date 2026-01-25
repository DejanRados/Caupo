using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.IO;

namespace Caupo.Services
{


    public class DatabaseBackupService : IDisposable
    {
        private readonly string _dbPath;
        private readonly string _backupPath;
        private Timer _timer;

        public DatabaseBackupService(string dbPath, string backupPath)
        {
            _dbPath = dbPath;
            _backupPath = backupPath;
        }

        public void Start()
        {
            // 15 minuta
            _timer = new Timer (BackupDatabase, null, TimeSpan.Zero, TimeSpan.FromMinutes (5));
        }

        private void BackupDatabase(object state)
        {
            try
            {
                string backupFile = Path.Combine (_backupPath, "sysFormWPF.dll");

                // Ako fajl postoji, obriši ga da bi VACUUM INTO mogao da piše
                if(File.Exists (backupFile))
                {
                    File.Delete (backupFile);
                    Debug.WriteLine ($"♻️ Stari backup obrisan: {backupFile}");
                }

                Debug.WriteLine ($"✅ Backup source: {Globals.CurrentDbPath}");

                using(var source = new SqliteConnection ($"Data Source={Globals.CurrentDbPath}"))
                {
                    source.Open ();

                    using(var command = source.CreateCommand ())
                    {
                        command.CommandText = $"VACUUM INTO '{backupFile}';";
                        command.ExecuteNonQuery ();
                    }
                }

                Debug.WriteLine ($"✅ Backup uspešno napravljen: {backupFile}");
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"❌ Backup failed: {ex.Message}");
            }
        }



        public void Dispose()
        {
            _timer?.Dispose ();
        }
    }

}
