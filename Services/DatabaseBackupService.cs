using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.IO;

namespace Caupo.Services
{
    public static class DatabaseBackupService
    {
        private const int LockWaitAttempts = 3;
        private const int LockWaitDelayMs = 100;

        /// <summary>
        /// Pokreće backup u pozadini i odmah vraća kontrolu pozivaocu.
        /// Izdavanje računa ne čeka završetak backupa.
        /// </summary>
        public static void StartBackup(string dbPath)
        {
            if (string.IsNullOrWhiteSpace(dbPath))
            {
                Debug.WriteLine("[DB BACKUP] Putanja baze nije definisana.");
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await BackupNowAsync(dbPath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DB BACKUP] Pozadinski backup nije uspio: {ex}");
                }
            });
        }

        /// <summary>
        /// Pravi konzistentan SQLite backup.
        /// Ova metoda se izvršava u pozadini preko StartBackup().
        /// </summary>
        private static async Task<bool> BackupNowAsync(string dbPath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(dbPath))
            {
                Debug.WriteLine($"[DB BACKUP] Baza ne postoji: {dbPath}");
                return false;
            }

            string databaseDirectory = Path.GetDirectoryName(dbPath) ?? throw new InvalidOperationException("Nije moguće odrediti direktorij baze.");

            string lastFile = Path.Combine(databaseDirectory, "sysFormWPF_backup_last.db");
            string previousFile = Path.Combine(databaseDirectory, "sysFormWPF_backup_previous.db");
            string lockFile = Path.Combine(databaseDirectory, "sysFormWPF_backup.lock");
            string tempFile = Path.Combine(databaseDirectory, $"sysFormWPF_backup_{Guid.NewGuid():N}.tmp");

            FileStream? lockStream = null;

            try
            {
                lockStream = await AcquireLockAsync(lockFile, cancellationToken);

                if (lockStream == null)
                {
                    Debug.WriteLine("[DB BACKUP] Druga kasa trenutno pravi backup. Ovaj backup je preskočen.");
                    return false;
                }

                Debug.WriteLine($"[DB BACKUP] Backup pokrenut. Source: {dbPath}");

                var sourceConnectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = dbPath,
                    Mode = SqliteOpenMode.ReadWrite,
                    DefaultTimeout = 10,
                    Pooling = false
                };

                await using (var source = new SqliteConnection(sourceConnectionString.ToString()))
                {
                    await source.OpenAsync(cancellationToken);

                    await using var command = source.CreateCommand();
                    command.CommandText = "VACUUM INTO $backupFile;";
                    command.Parameters.AddWithValue("$backupFile", tempFile);

                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                if (!await CheckBackupAsync(tempFile, cancellationToken))
                {
                    Debug.WriteLine("[DB BACKUP] Novi backup nije prošao quick_check.");
                    return false;
                }

                if (File.Exists(lastFile))
                    File.Copy(lastFile, previousFile, true);

                File.Move(tempFile, lastFile, true);

                Debug.WriteLine($"[DB BACKUP] Backup uspješno završen: {lastFile}");

                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[DB BACKUP] Backup je otkazan.");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB BACKUP] Backup nije uspio: {ex}");
                return false;
            }
            finally
            {
                if (lockStream != null)
                    await lockStream.DisposeAsync();

                TryDeleteFile(tempFile);
                TryDeleteFile(lockFile);
            }
        }

        public static async Task<bool> RestoreLatestBackupAsync(string dbPath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dbPath))
                return false;

            string databaseDirectory = Path.GetDirectoryName(dbPath) ?? throw new InvalidOperationException("Nije moguće odrediti direktorij baze.");

            string lastFile = Path.Combine(databaseDirectory, "sysFormWPF_backup_last.db");
            string previousFile = Path.Combine(databaseDirectory, "sysFormWPF_backup_previous.db");

            try
            {
                string? backupFile = null;

                if (File.Exists(lastFile) && await CheckBackupAsync(lastFile, cancellationToken))
                    backupFile = lastFile;
                else if (File.Exists(previousFile) && await CheckBackupAsync(previousFile, cancellationToken))
                    backupFile = previousFile;

                if (backupFile == null)
                {
                    Debug.WriteLine("[DB RESTORE] Nije pronađen ispravan backup.");
                    return false;
                }

                SqliteConnection.ClearAllPools();

                File.Copy(backupFile, dbPath, true);

                Debug.WriteLine($"[DB RESTORE] Baza uspješno vraćena iz: {backupFile}");
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB RESTORE] Restore nije uspio: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Mrežni lock sprečava da dvije Caupo kase istovremeno
        /// rotiraju last/previous backup iste baze.
        /// Čekanje se odvija isključivo u background tasku.
        /// </summary>
        private static async Task<FileStream?> AcquireLockAsync(string lockFile, CancellationToken cancellationToken)
        {
            for (int attempt = 1; attempt <= LockWaitAttempts; attempt++)
            {
                try
                {
                    return new FileStream(lockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException)
                {
                    if (attempt == LockWaitAttempts)
                        return null;

                    await Task.Delay(LockWaitDelayMs, cancellationToken);
                }
            }

            return null;
        }

        /// <summary>
        /// Brza provjera novog backupa.
        /// Puni integrity_check ćemo koristiti kod recovery scenarija,
        /// ne nakon svakog računa.
        /// </summary>
        private static async Task<bool> CheckBackupAsync(string databasePath, CancellationToken cancellationToken)
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadOnly,
                DefaultTimeout = 10,
                Pooling = false
            };

            await using var connection = new SqliteConnection(connectionString.ToString());
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA quick_check;";

            object? result = await command.ExecuteScalarAsync(cancellationToken);

            return string.Equals(result?.ToString(), "ok", StringComparison.OrdinalIgnoreCase);
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB BACKUP] Nije moguće obrisati {path}: {ex.Message}");
            }
        }
    }
}