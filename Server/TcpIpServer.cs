using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Caupo.Server
{
    public class TcpIpServer
    {
        private readonly TcpListener _listener;
        private readonly CommandDispatcher _dispatcher;
        public IPEndPoint ListenerEndpoint => _listener.LocalEndpoint as IPEndPoint;
        public TcpIpServer(int port, string connectionString)
        {
            _listener = new TcpListener (IPAddress.Any, port);
            _dispatcher = new CommandDispatcher (connectionString);
        }

        public async Task StartAsync()
        {
            _listener.Start ();
            ClientRegistry.ListAll ();
            Debug.WriteLine ($"Server sluša na portu {((IPEndPoint)_listener.LocalEndpoint).Port}");

            while(true)
            {
                var client = await _listener.AcceptTcpClientAsync ();
                _ = HandleClientAsync (client); // ne blokira
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            var session = new ClientSession
            {
                Client = client,
                Stream = client.GetStream (),
                PartialRequestBuilder = new StringBuilder () // buffer za delimične requeste
            };

            Debug.WriteLine ("[Server] Klijent povezan, čeka LOGIN/REGISTER komandu...");

            var buffer = new byte[8192];

            try
            {
                while(client.Connected)
                {
                    Debug.WriteLine ("[Server] Čeka podatke od klijenta...");

                    // Čitaj podatke
                    int bytesRead = await session.Stream.ReadAsync (buffer, 0, buffer.Length);

                    if(bytesRead == 0)
                    {
                        Debug.WriteLine ("[Server] Klijent se diskonektovao (bytesRead = 0).");
                        break; // klijent se diskonektovao
                    }

                    var chunk = Encoding.UTF8.GetString (buffer, 0, bytesRead);
                    Debug.WriteLine ($"[Server] Primljen chunk ({bytesRead} bytes): {chunk}");

                    // Akumuliraj podatke
                    session.PartialRequestBuilder.Append (chunk);
                    string accumulated = session.PartialRequestBuilder.ToString ();

                    // Procesiraj sve kompletne JSON poruke
                    int endIndex;
                    while((endIndex = accumulated.IndexOf ("###END###")) >= 0)
                    {
                        string singleRequestJson = accumulated.Substring (0, endIndex);
                        accumulated = accumulated.Substring (endIndex + "###END###".Length);

                        try
                        {
                            var request = JsonSerializer.Deserialize<RequestMessage> (singleRequestJson);
                            if(request == null)
                            {
                                Debug.WriteLine ("[Server] Deserializacija nije uspjela - request je null.");
                                continue;
                            }

                            Debug.WriteLine ($"[Server] Dispatch komande: {request.Command}");
                            string result = await _dispatcher.DispatchAsync (request, session);
                            Debug.WriteLine ($"[Server] Dispatch završen za komandu {request.Command}");

                            // Pošalji odgovor sa delimiteromDispatchAsync
                            var responseBytes = Encoding.UTF8.GetBytes (result + "###END###");
                            await session.Stream.WriteAsync (responseBytes, 0, responseBytes.Length);
                        }
                        catch(Exception ex)
                        {
                            Debug.WriteLine ("[Server] Greška pri deserializaciji requesta: " + ex);
                        }
                    }

                    // Sačuvaj ostatak za sljedeće čitanje
                    session.PartialRequestBuilder.Clear ();
                    session.PartialRequestBuilder.Append (accumulated);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine ("[Server] Exception u HandleClientAsync: " + ex);
            }
            finally
            {
                Debug.WriteLine ("[Server] Klijent diskonektovan, uklanjam iz ClientRegistry.");

                if(!string.IsNullOrWhiteSpace (session.DeviceId))
                {
                    ClientRegistry.Remove (session.DeviceId);
                    Debug.WriteLine ($"[Server] Klijent sa DeviceId='{session.DeviceId}' uklonjen.");
                }

                try
                {
                    session.Stream.Close ();
                    Debug.WriteLine ("[Server] Stream zatvoren.");
                }
                catch { }

                try
                {
                    session.Client.Close ();
                    Debug.WriteLine ("[Server] TcpClient zatvoren.");
                }
                catch { }
            }
        }

    }


}
