using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;
using Caupo.Data;

namespace Caupo.Server
{
    public class LoginHandler : ICommandHandler
    {
        private readonly string _connectionString;

        public LoginHandler(string cs) => _connectionString = cs;

        public async Task<string> HandleAsync(Dictionary<string, string> parameters)
        {
            Debug.WriteLine("------------- HandleAsync -----------   ");
            if (!parameters.TryGetValue("pin", out var pin))
                return JsonSerializer.Serialize(new { success = false, message = "Parametar PIN nedostaje" });
            Debug.WriteLine("------------- HandleAsync -----------  dobija: " + pin);
            using var db = new AppDbContext();

            var radnik = await db.Radnici
                                 .Where(r => r.Lozinka == pin)
                                 .Select(r => new { r.Radnik })
                                 .FirstOrDefaultAsync();

            if (radnik == null)
            {
                Debug.WriteLine("------------- HandleAsync -----------  (radnik == null)");
                return JsonSerializer.Serialize(new { success = false, message = "Pogrešna lozinka" });
            }
            else
            {
                Debug.WriteLine("------------- HandleAsync -----------  (radnik @= null) - " + radnik.Radnik);
                var radnikObj = new
                {
                    success = true,
                    radnik = new
                    {
                        Radnik = radnik.Radnik 
                    }
                };

                return JsonSerializer.Serialize(radnikObj);
            }
        }
    }
}
