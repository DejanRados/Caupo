using Caupo.Data;
using Caupo.Server;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using static Caupo.Data.DatabaseTables;

public class ArtikliHandler : ICommandHandler
{
    private readonly string _connectionString;

    public ArtikliHandler(string cs) => _connectionString = cs;

    public async Task<string> HandleAsync(Dictionary<string, string> parameters, ClientSession session)
    {
        try
        {
            using var db = new AppDbContext ();
            var artikli = await db.Artikli.ToListAsync ();

            var response = new ResponseMessage<List<TblArtikli>>
            {
                Status = "OK",
                Data = artikli
            };

            return JsonSerializer.Serialize (response);
        }
        catch(Exception ex)
        {
            var response = new ResponseMessage<string>
            {
                Status = "Error",
                Data = ex.Message
            };

            return JsonSerializer.Serialize (response);
        }
    }
}
