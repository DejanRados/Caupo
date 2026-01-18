using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Caupo.Server
{
    public static class ClientRegistry
    {
        private static readonly ConcurrentDictionary<string, ClientSession> _clients = new ();
        private static readonly string SavePath = "clients.json"; // fajl za čuvanje sesija

        // ----- JSON Persistence -----
        public static void LoadFromFile()
        {
            if(!File.Exists (SavePath))
                return;

            try
            {
                var json = File.ReadAllText (SavePath);
                var list = JsonSerializer.Deserialize<List<ClientSession>> (json);
                if(list != null)
                {
                    foreach(var session in list)
                    {
                        _clients[session.DeviceId] = session;
                    }
                }

                Debug.WriteLine ($"[ClientRegistry] Učitano {_clients.Count} sesija iz fajla.");
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[ClientRegistry] GREŠKA pri učitavanju fajla: {ex.Message}");
            }
        }

        private static void SaveToFile()
        {
            try
            {
                var list = _clients.Values.ToList ();
                var json = JsonSerializer.Serialize (list, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText (SavePath, json);
                Debug.WriteLine ($"[ClientRegistry] Sačuvano {_clients.Count} sesija u fajl.");
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"[ClientRegistry] GREŠKA pri čuvanju fajla: {ex.Message}");
            }
        }

        // ----- Postojeći kod -----
        public static void Register(ClientSession session)
        {
            if(string.IsNullOrWhiteSpace (session.DeviceId))
            {
                Debug.WriteLine ("[ClientRegistry] GREŠKA: Pokušaj registracije bez DeviceId!");
                return;
            }

            Debug.WriteLine ($"[ClientRegistry] Registrujem klijenta: '{session.DeviceId}'");

            if(_clients.ContainsKey (session.DeviceId))
            {
                Debug.WriteLine ($"[ClientRegistry] Klijent '{session.DeviceId}' već postoji, zamjenjujem");
            }

            _clients[session.DeviceId] = session;
            Debug.WriteLine ($"[ClientRegistry] Klijent '{session.DeviceId}' uspješno registrovan");

            SaveToFile (); // Sačuvaj odmah
        }

        public static ClientSession? Get(string deviceId)
        {
            Debug.WriteLine ($"[ClientRegistry] Tražim klijenta: '{deviceId}'");

            if(_clients.TryGetValue (deviceId, out var session))
            {
                Debug.WriteLine ($"[ClientRegistry] Klijent '{deviceId}' pronađen, Connected: {session.Client?.Connected}");
                return session;
            }

            Debug.WriteLine ($"[ClientRegistry] Klijent '{deviceId}' NIJE pronađen");
            Debug.WriteLine ($"[ClientRegistry] Trenutno dostupni klijenti: {string.Join (", ", _clients.Keys)}");
            return null;
        }

        public static void Remove(string deviceId)
        {
            Debug.WriteLine ($"[ClientRegistry] Pokušavam ukloniti klijenta: '{deviceId}'");

            if(_clients.TryRemove (deviceId, out _))
            {
                Debug.WriteLine ($"[ClientRegistry] Klijent '{deviceId}' uklonjen");
                SaveToFile (); // Sačuvaj odmah
            }
            else
            {
                Debug.WriteLine ($"[ClientRegistry] Klijent '{deviceId}' nije pronađen za uklanjanje");
            }
        }

        public static IEnumerable<ClientSession> GetAll() => _clients.Values;

        public static void ListAll()
        {
            Debug.WriteLine ("[ClientRegistry] ==== SVI REGISTROVANI KLIJENTI ====");
            foreach(var kvp in _clients)
            {
                var connected = kvp.Value.Client?.Connected ?? false;
                Debug.WriteLine ($"  DeviceId: '{kvp.Key}', Connected: {connected}");
            }
            Debug.WriteLine ($"==== UKUPNO: {_clients.Count} ====");
        }
    }


}
