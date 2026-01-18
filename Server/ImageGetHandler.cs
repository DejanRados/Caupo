using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Caupo.Server
{
    public class ImageGetHandler : ICommandHandler
    {
        public async Task<string> HandleAsync(Dictionary<string, string> parameters, ClientSession session)
        {
            if(!parameters.TryGetValue ("filename", out var filePath) || string.IsNullOrWhiteSpace (filePath))
            {
                return JsonSerializer.Serialize (new
                {
                    Status = "ERROR",
                    Data = new { message = "filename missing" }
                });
            }

            // Uzmi ime datoteke (samo ime, bez direktorija)
            string fileNameOnly = Path.GetFileName (filePath);

            // Provjeri postoji li datoteka
            if(File.Exists (filePath))
            {
                var bytes = await File.ReadAllBytesAsync (filePath);
                var b64 = Convert.ToBase64String (bytes);

                Debug.WriteLine ("[SERVER ImageGetHandler] Sending image: " + fileNameOnly);

                return JsonSerializer.Serialize (new
                {
                    Status = "OK",
                    Data = new
                    {
                        Filename = fileNameOnly,  // šalji SAMO ime
                        Base64 = b64
                    }
                });
            }
            else
            {
                // fallback placeholder PNG
                byte[] emptyPng = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                return JsonSerializer.Serialize (new
                {
                    Status = "OK",
                    Data = new
                    {
                        Filename = "placeholder.png",
                        Base64 = Convert.ToBase64String (emptyPng)
                    }
                });
            }
        }

    }


}
