using Caupo.Server;
using System.IO;
using System.Text.Json;

public class ImagesBatchHandler : ICommandHandler
{
    public async Task<string> HandleAsync(
        Dictionary<string, string> parameters,
        ClientSession session)
    {
        try
        {
            if(!parameters.TryGetValue ("filenames", out var filenamesJson))
            {
                return JsonSerializer.Serialize (
                    new ResponseMessage<ImageBatchResponse>
                    {
                        Status = "Error",
                        Data = new ImageBatchResponse
                        {
                            Success = false
                        }
                    });
            }

            var filenames = JsonSerializer.Deserialize<List<string>> (filenamesJson);
            if(filenames == null || filenames.Count == 0)
            {
                return JsonSerializer.Serialize (
                    new ResponseMessage<ImageBatchResponse>
                    {
                        Status = "OK",
                        Data = new ImageBatchResponse
                        {
                            Success = true
                        }
                    });
            }

            var response = new ImageBatchResponse
            {
                Success = true
            };

            foreach(var path in filenames.Distinct ())
            {
                if(string.IsNullOrWhiteSpace (path) || !File.Exists (path))
                    continue;

                var bytes = await File.ReadAllBytesAsync (path);
                var base64 = Convert.ToBase64String (bytes);

                var filename = Path.GetFileName (path);

                response.Images.Add (new ImageBatchItem
                {
                    Filename = filename,
                    Base64 = base64
                });
            }

            return JsonSerializer.Serialize (
                new ResponseMessage<ImageBatchResponse>
                {
                    Status = "OK",
                    Data = response
                });
        }
        catch(Exception ex)
        {
            return JsonSerializer.Serialize (
                new ResponseMessage<string>
                {
                    Status = "Error",
                    Data = ex.Message
                });
        }
    }

    public class ImageBatchItem
    {
        public string Filename { get; set; }
        public string Base64 { get; set; }
    }

    public class ImageBatchResponse
    {
        public bool Success { get; set; }
        public List<ImageBatchItem> Images { get; set; } = new ();
    }

}
