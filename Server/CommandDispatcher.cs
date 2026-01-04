using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Caupo.Server
{
    public class CommandDispatcher
    {
        private readonly Dictionary<string, ICommandHandler> _handlers;

        public CommandDispatcher(string connectionString)
        {
            _handlers = new Dictionary<string, ICommandHandler>
        {
            { "LOGIN", new LoginHandler(connectionString) },
            { "ARTIKLI", new ArtikliHandler(connectionString) },
            { "IMAGE_GET", new ImageGetHandler() },
            { "RACUN_IZDAJ", new IzdajRacunHandler() },
            { "BLOK", new BlokHandler() },

        };
        }

        public async Task<string> DispatchAsync(RequestMessage req)
        {
            Debug.WriteLine("--- DispatchAsync  ---  dobija: " + req.Command);
            if (_handlers.TryGetValue(req.Command, out var handler))
            {
                Debug.WriteLine("--- _handlers.TryGetValue : " + req.Command);
                return await handler.HandleAsync(req.Parameters);
            }
            return "Nepoznata komanda";
        }
    }

}
