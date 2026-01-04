using System;
using System.Management; // Dodajte referencu na System.Management
using System.Security.Cryptography;
using System.Text;

namespace Caupo.Helpers
{
    public static class HardwareHelper
    {
        public static string GetHardwareFingerprint()
        {
            try
            {
                // Kombiniraj više hardware komponenti
                var fingerprint = new StringBuilder ();

                // 1. Disk serijski broj (glavni disk)
                fingerprint.Append (GetDiskSerial ());

                // 2. Motherboard serijski
                fingerprint.Append (GetMotherboardSerial ());

                // 3. CPU ID
                fingerprint.Append (GetProcessorId ());

                // 4. MAC adresa (prva mrežna kartica)
                fingerprint.Append (GetMacAddress ());

                // Ako je fingerprint prazan (VM ili error), koristi alternative
                if(string.IsNullOrEmpty (fingerprint.ToString ().Trim ()))
                {
                    fingerprint.Append (Environment.MachineName);
                    fingerprint.Append (Environment.UserName);
                }

                // Hash za konzistentnost i privatnost
                return ComputeHash (fingerprint.ToString ());
            }
            catch(Exception ex)
            {
                // Fallback ako Management ne radi
                Console.WriteLine ($"Hardware fingerprint error: {ex.Message}");
                return ComputeHash (Environment.MachineName + Environment.UserName);
            }
        }

        private static string GetDiskSerial()
        {
            try
            {
                using(var searcher = new ManagementObjectSearcher ("SELECT SerialNumber FROM Win32_DiskDrive WHERE Index = 0"))
                {
                    foreach(ManagementObject disk in searcher.Get ())
                    {
                        return disk["SerialNumber"]?.ToString ()?.Trim () ?? "";
                    }
                }
            }
            catch {  }
            return "";
        }

        private static string GetMotherboardSerial()
        {
            try
            {
                using(var searcher = new ManagementObjectSearcher ("SELECT SerialNumber FROM Win32_BaseBoard"))
                {
                    foreach(ManagementObject board in searcher.Get ())
                    {
                        return board["SerialNumber"]?.ToString ()?.Trim () ?? "";
                    }
                }
            }
            catch {  }
            return "";
        }

        private static string GetProcessorId()
        {
            try
            {
                using(var searcher = new ManagementObjectSearcher ("SELECT ProcessorId FROM Win32_Processor"))
                {
                    foreach(ManagementObject cpu in searcher.Get ())
                    {
                        return cpu["ProcessorId"]?.ToString ()?.Trim () ?? "";
                    }
                }
            }
            catch {  }
            return "";
        }

        private static string GetMacAddress()
        {
            try
            {
                using(var searcher = new ManagementObjectSearcher ("SELECT MACAddress FROM Win32_NetworkAdapter WHERE MACAddress IS NOT NULL"))
                {
                    foreach(ManagementObject adapter in searcher.Get ())
                    {
                        var mac = adapter["MACAddress"]?.ToString ();
                        if(!string.IsNullOrEmpty (mac))
                            return mac.Replace (":", "");
                    }
                }
            }
            catch { }
            return "";
        }

        private static string ComputeHash(string input)
        {
            using(var sha256 = SHA256.Create ())
            {
                var bytes = Encoding.UTF8.GetBytes (input);
                var hash = sha256.ComputeHash (bytes);
                return BitConverter.ToString (hash).Replace ("-", "").ToLower ().Substring (0, 32);
            }
        }
    }
}