using Caupo.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;

namespace Caupo.Server
{
    public class LoginHandler : ICommandHandler
    {
        private readonly string _connectionString;

        public LoginHandler(string cs) => _connectionString = cs;

        public async Task<string> HandleAsync(Dictionary<string, string> parameters, ClientSession session)
        {
            Debug.WriteLine ("------------- LoginHandler.HandleAsync start -----------");

            if(!parameters.TryGetValue ("pin", out var pin))
            {
                return JsonSerializer.Serialize (new
                {
                    Status = "OK",
                    Data = new { success = false, message = "Parametar PIN nedostaje" }
                });
            }

            Debug.WriteLine ("------------- LoginHandler dobija PIN: " + pin);

            try
            {
                using var db = new AppDbContext ();

                var radnik = await db.Radnici
                                     .Where (r => r.Lozinka == pin)
                                     .Select (r => new { r.Radnik })
                                     .FirstOrDefaultAsync ();

                if(radnik == null)
                {
                    Debug.WriteLine ("------------- LoginHandler: Pogrešna lozinka");
                    return JsonSerializer.Serialize (new
                    {
                        Status = "OK",
                        Data = new { success = false, message = "Pogrešna lozinka" }
                    });
                }

                if(string.IsNullOrWhiteSpace (radnik.Radnik))
                {
                    Debug.WriteLine ("[LoginHandler] Radnik.Radnik je null ili prazan!");
                    return JsonSerializer.Serialize (new
                    {
                        Status = "OK",
                        Data = new { success = false, message = "Pogrešan korisnik" }
                    });
                }

                Debug.WriteLine ("------------- LoginHandler: Login uspješan - " + radnik.Radnik);

                // ⚡ Registrujemo session nakon provjere da DeviceId nije null
                session.DeviceId = radnik.Radnik; // userId = radnik

                // Debug prije registracije
                Debug.WriteLine ($"[LoginHandler] Session.DeviceId postavljen na: '{session.DeviceId}'");
                Debug.WriteLine ($"[LoginHandler] Session.Client.Connected: {session.Client?.Connected}");

                // Registracija sesije
                ClientRegistry.Register (session);

                // Potvrda registracije
                Debug.WriteLine ($"[LoginHandler] Klijent registrovan u ClientRegistry: DeviceId='{session.DeviceId}'");

                // Prikaži sve registrovanje klijente
                ClientRegistry.ListAll ();

                return JsonSerializer.Serialize (new
                {
                    Status = "OK",
                    Data = new
                    {
                        success = true,
                        radnik = new { Radnik = radnik.Radnik }
                    }
                });
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[LoginHandler] Greška: {ex}");
                return JsonSerializer.Serialize (new
                {
                    Status = "OK",
                    Data = new { success = false, message = "Greška pri login" }
                });
            }
        }
    }
}