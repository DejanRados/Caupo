using Caupo.Data;
using System.Diagnostics;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Services
{
    public static class AuditLogService
    {
        public static async Task WriteAsync(
            string dogadjaj,
            string? detalji = null,
            int? idRadnika = null,
            string? radnik = null,
            int? brojRacuna = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await using var db = new AppDbContext();

                var audit = new TblAuditLog
                {
                    Datum = DateTime.Now,
                    Dogadjaj = dogadjaj,
                    Detalji = detalji,
                    IdRadnika = idRadnika,
                    Radnik = radnik,
                    BrojRacuna = brojRacuna
                };

                await db.AuditLog.AddAsync(audit, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);

                Debug.WriteLine(
                    $"[AUDIT] {audit.Datum:yyyy-MM-dd HH:mm:ss} | " +
                    $"{audit.Dogadjaj} | Radnik={audit.Radnik} | " +
                    $"BrojRacuna={audit.BrojRacuna}");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[AUDIT] Greška pri upisu audit loga: " + ex);
                throw;
            }
        }
    }
}