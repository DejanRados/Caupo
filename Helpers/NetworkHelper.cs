using System.Net.NetworkInformation;

namespace Caupo.Helpers
{
    public static class NetworkHelper
    {
        public static bool HasInternet()
        {
            if(!NetworkInterface.GetIsNetworkAvailable ())
                return false;

            try
            {
                using var ping = new Ping ();
                var reply = ping.Send ("8.8.8.8", 1500); // Google DNS
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }
    }

}
