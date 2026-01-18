namespace Caupo.Server
{

    public interface ICommandHandler
    {
        Task<string> HandleAsync(Dictionary<string, string> parameters, ClientSession session);
    }


}
