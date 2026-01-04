using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caupo.Server
{
    public interface ICommandHandler
    {
        Task<string> HandleAsync(Dictionary<string, string> parameters);
    }
}
