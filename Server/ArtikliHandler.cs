using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Text.Json;
using System.Threading.Tasks;
using Caupo.Data;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Server
{
    public class ArtikliHandler : ICommandHandler
    {
        private readonly string _connectionString;

        public ArtikliHandler(string cs) => _connectionString = cs;

        public async Task<string> HandleAsync(Dictionary<string, string> parameters)
        {
            try
            {
                using var db = new AppDbContext();
                var artikli = await db.Artikli.ToListAsync();
                Debug.WriteLine("---------- Na serveru  artikli.Count: " + artikli.Count);
                return JsonSerializer.Serialize(new { success = true, artikli });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("---------- Na serveru artikli: " +ex.ToString());
                return JsonSerializer.Serialize(new { success = false, message = ex.Message });
            }
        }
    }
}
