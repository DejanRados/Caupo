using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Caupo.Server
{
    public class ImageGetHandler : ICommandHandler
    {
        public async Task<string> HandleAsync(Dictionary<string, string> parameters)
        {
            if (!parameters.TryGetValue("filename", out var filename))
                return JsonSerializer.Serialize(new { success = false, message = "filename missing" });

            var fullPath = Path.Combine( filename);
            if (!File.Exists(fullPath))
                return JsonSerializer.Serialize(new { success = false, message = "file not found" });

            var bytes = await File.ReadAllBytesAsync(fullPath);
            var b64 = Convert.ToBase64String(bytes);
            var normalized = filename.Replace('\\', '/');
            var fileName = normalized.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Last();
            foreach (var c in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(c, '_');
            Debug.WriteLine("Extracted filename: " + fileName);
            Debug.WriteLine("-+------------------ Šalje sliku: " + fileName + " --------------------");
            return JsonSerializer.Serialize(new
            {
                success = true,
                filename = fileName,
                data = b64
            });
        }
    }

}
