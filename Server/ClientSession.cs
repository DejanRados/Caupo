using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
