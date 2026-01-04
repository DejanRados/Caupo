using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Data.Sqlite;
using System.Data.Entity.Infrastructure.Interception;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
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
            _listener = new TcpListener(IPAddress.Any, port);
            _dispatcher = new CommandDispatcher(connectionString);
        }

        public async Task StartAsync()
        {
            _listener.Start();
            Console.WriteLine($"Server sluša na portu {((IPEndPoint)_listener.LocalEndpoint).Port}");

            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client); // ne blokira
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            Console.WriteLine("➡ Klijent povezan");
            using var stream = client.GetStream();
            var buffer = new byte[8192];

            try
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) return;

                var requestJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var request = JsonSerializer.Deserialize<RequestMessage>(requestJson);
                Debug.WriteLine("Primljeni JSON od klijenta:");
                Debug.WriteLine(requestJson);

                string result = await _dispatcher.DispatchAsync(request);

                var resultObj = JsonSerializer.Deserialize<object>(result);
                var response = new ResponseMessage<object> { Status = "OK", Data = resultObj };
                var responseJson = JsonSerializer.Serialize(response);
                var responseBytes = Encoding.UTF8.GetBytes(responseJson);

                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            }
            catch (Exception ex)
            {
                var error = new ResponseMessage<object> { Status = "ERROR", Data = ex.Message };
                var responseJson = JsonSerializer.Serialize(error);
                var responseBytes = Encoding.UTF8.GetBytes(responseJson);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            }
            finally
            {
                client.Close();
            }
        }
    }
 

}
