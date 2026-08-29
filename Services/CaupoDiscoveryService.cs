using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Caupo.Services
{
    public class CaupoDiscoveryService : IDisposable
    {
        private const int DiscoveryPort = 45678;

        private const string DiscoveryRequest =
            "CAUPO_FIND_MAIN";

        private const string DiscoveryResponsePrefix =
            "CAUPO_MAIN|";

        private UdpClient? _listener;

        private CancellationTokenSource? _listenerCts;


        // =====================================================
        // GLAVNA KASA
        // =====================================================

        public void StartMainCashRegisterListener()
        {
            if(_listener != null)
            {
                Debug.WriteLine (
                    "[DISCOVERY] Listener već radi.");

                return;
            }


            try
            {
                _listenerCts =
                    new CancellationTokenSource ();


                _listener =
                    new UdpClient ();


                _listener.Client.SetSocketOption (
                    SocketOptionLevel.Socket,
                    SocketOptionName.ReuseAddress,
                    true);


                _listener.Client.Bind (
                    new IPEndPoint (
                        IPAddress.Any,
                        DiscoveryPort));


                Debug.WriteLine (
                    $"[DISCOVERY] Glavna kasa sluša UDP port {DiscoveryPort}.");


                _ =
                    ListenAsync (
                        _listenerCts.Token);
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[DISCOVERY] Ne mogu pokrenuti listener: {ex}");
            }
        }


        private async Task ListenAsync(
            CancellationToken cancellationToken)
        {
            if(_listener == null)
                return;


            while(!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult result =
                        await _listener.ReceiveAsync (
                            cancellationToken);


                    string message =
                        Encoding.UTF8.GetString (
                            result.Buffer);


                    Debug.WriteLine (
                        $"[DISCOVERY] Primljeno od {result.RemoteEndPoint}: {message}");


                    if(!string.Equals (
                        message,
                        DiscoveryRequest,
                        StringComparison.Ordinal))
                    {
                        continue;
                    }


                    string uncPath =
                        $@"\\{Environment.MachineName}\DsoftData\sysFormWPF.db";


                    string response =
                        DiscoveryResponsePrefix +
                        uncPath;


                    byte[] responseBytes =
                        Encoding.UTF8.GetBytes (
                            response);


                    await _listener.SendAsync (
                        responseBytes,
                        responseBytes.Length,
                        result.RemoteEndPoint);


                    Debug.WriteLine (
                        $"[DISCOVERY] Poslan odgovor: {response}");
                }
                catch(OperationCanceledException)
                {
                    break;
                }
                catch(ObjectDisposedException)
                {
                    break;
                }
                catch(Exception ex)
                {
                    Debug.WriteLine (
                        $"[DISCOVERY] Listener greška: {ex}");
                }
            }


            Debug.WriteLine (
                "[DISCOVERY] Listener zaustavljen.");
        }


        // =====================================================
        // DODATNA KASA
        // =====================================================

        public async Task<List<MainCashRegisterInfo>>
      FindMainCashRegistersAsync(
          int timeoutMilliseconds = 2500)
        {
            var found =
                new Dictionary<
                    string,
                    MainCashRegisterInfo> (
                    StringComparer.OrdinalIgnoreCase);


            using var client =
                new UdpClient ();


            client.EnableBroadcast =
                true;


            byte[] requestBytes =
                Encoding.UTF8.GetBytes (
                    DiscoveryRequest);


            var broadcastEndpoint =
                new IPEndPoint (
                    IPAddress.Broadcast,
                    DiscoveryPort);


            Debug.WriteLine (
                "[DISCOVERY] Tražim glavnu kasu...");


            await client.SendAsync (
                requestBytes,
                requestBytes.Length,
                broadcastEndpoint);


            Debug.WriteLine (
                "[DISCOVERY] Broadcast poslan.");


            Stopwatch stopwatch =
                Stopwatch.StartNew ();


            while(stopwatch.ElapsedMilliseconds <
                  timeoutMilliseconds)
            {
                int remainingMilliseconds =
                    timeoutMilliseconds -
                    (int)stopwatch.ElapsedMilliseconds;


                if(remainingMilliseconds <= 0)
                {
                    break;
                }


                Task<UdpReceiveResult> receiveTask =
                    client.ReceiveAsync ();


                Task timeoutTask =
                    Task.Delay (
                        remainingMilliseconds);


                Task completedTask =
                    await Task.WhenAny (
                        receiveTask,
                        timeoutTask);


                // =====================================================
                // TIMEOUT
                // =====================================================

                if(completedTask == timeoutTask)
                {
                    Debug.WriteLine (
                        "[DISCOVERY] Vrijeme za traženje je isteklo.");

                    break;
                }


                // =====================================================
                // ODGOVOR
                // =====================================================

                UdpReceiveResult result;

                try
                {
                    result =
                        await receiveTask;
                }
                catch(Exception ex)
                {
                    Debug.WriteLine (
                        $"[DISCOVERY] Greška kod primanja odgovora: {ex}");

                    break;
                }


                string response =
                    Encoding.UTF8.GetString (
                        result.Buffer);


                Debug.WriteLine (
                    $"[DISCOVERY] Odgovor od {result.RemoteEndPoint}: {response}");


                if(!response.StartsWith (
                    DiscoveryResponsePrefix,
                    StringComparison.Ordinal))
                {
                    continue;
                }


                string uncPath =
                    response.Substring (
                        DiscoveryResponsePrefix.Length)
                    .Trim ();


                if(string.IsNullOrWhiteSpace (
                    uncPath))
                {
                    continue;
                }


                string machineName =
                    ExtractMachineName (
                        uncPath);


                // =====================================================
                // NE PRIHVATAJ SAMU SEBE
                // =====================================================

                if(string.Equals (
                    machineName,
                    Environment.MachineName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine (
                        "[DISCOVERY] Ignorišem vlastiti odgovor.");

                    continue;
                }


                var info =
                    new MainCashRegisterInfo
                    {
                        MachineName =
                            machineName,

                        IpAddress =
                            result.RemoteEndPoint
                                .Address
                                .ToString (),

                        DatabasePath =
                            uncPath
                    };


                found[uncPath] =
                    info;


                Debug.WriteLine (
                    $"[DISCOVERY] Pronađena glavna kasa: " +
                    $"{machineName} | " +
                    $"{info.IpAddress} | " +
                    $"{uncPath}");
            }


            stopwatch.Stop ();


            Debug.WriteLine (
                $"[DISCOVERY] Završeno. Pronađeno: {found.Count}");


            return found.Values.ToList ();
        }


        private static string ExtractMachineName(
            string uncPath)
        {
            if(string.IsNullOrWhiteSpace (
                uncPath))
            {
                return string.Empty;
            }


            string trimmed =
                uncPath.Trim ();


            if(!trimmed.StartsWith (
                @"\\",
                StringComparison.Ordinal))
            {
                return string.Empty;
            }


            string withoutPrefix =
                trimmed.Substring (2);


            int slashIndex =
                withoutPrefix.IndexOf (
                    '\\');


            if(slashIndex <= 0)
            {
                return withoutPrefix;
            }


            return withoutPrefix.Substring (
                0,
                slashIndex);
        }


        public void Stop()
        {
            try
            {
                _listenerCts?.Cancel ();
            }
            catch
            {
            }


            try
            {
                _listener?.Dispose ();
            }
            catch
            {
            }


            _listener = null;


            try
            {
                _listenerCts?.Dispose ();
            }
            catch
            {
            }


            _listenerCts = null;


            Debug.WriteLine (
                "[DISCOVERY] Servis zaustavljen.");
        }


        public void Dispose()
        {
            Stop ();
        }
    }



    public class MainCashRegisterInfo
    {
        public string MachineName
        {
            get;
            set;
        } = string.Empty;


        public string IpAddress
        {
            get;
            set;
        } = string.Empty;


        public string DatabasePath
        {
            get;
            set;
        } = string.Empty;


        public override string ToString()
        {
            return
                $"{MachineName} ({IpAddress})";
        }
    }
}