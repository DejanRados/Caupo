using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;

namespace Caupo.Server
{
    public class ClientSession
    {
        [JsonIgnore]
        public TcpClient Client { get; init; }
        [JsonIgnore]
        public NetworkStream Stream { get; init; }
        public string DeviceId { get; set; }

        public StringBuilder PartialRequestBuilder { get; set; }
        //public string Role { get; set; } // KITCHEN, CASH, ANDROID
    }
}
