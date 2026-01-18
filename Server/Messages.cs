using System.Text.Json.Serialization;

namespace Caupo.Server
{

    public class RequestMessage
    {
        [JsonPropertyName ("command")]
        public string Command { get; set; }

        [JsonPropertyName ("parameters")]
        public Dictionary<string, string> Parameters { get; set; }
    }


    public class ResponseMessage<T>
    {
        public string Status { get; set; }
        public T Data { get; set; }
    }
}
