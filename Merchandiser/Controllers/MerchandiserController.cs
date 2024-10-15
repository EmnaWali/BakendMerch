using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Net.Sockets;

[ApiController]
[Route("api/[controller]")]
public class MerchandiserController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public MerchandiserController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("GetMerchandisers")]
    public async Task<IActionResult> GetMerchandisers()
    {
     
        string query = "SELECT name, login, user_id FROM Users U WHERE U.role_id = 2";

        string sqlDatasource = _configuration.GetConnectionString("merchandiser")
                                ?? throw new InvalidOperationException("Connection string 'merchandiser' not found.");

        using (SqlConnection myCon = new SqlConnection(sqlDatasource))
        {
            await myCon.OpenAsync();
            using (SqlCommand myCommand = new SqlCommand(query, myCon))
            {
                using (SqlDataReader reader = await myCommand.ExecuteReaderAsync())
                {
                    var users = new List<User>();
                    while (await reader.ReadAsync())
                    {
                        users.Add(new User
                        {
                            Name = reader.GetString(0),
                            Login = reader.GetString(1),
                            User_id = reader.GetInt32(2)
                        });
                    }

                    return Ok(users);
                }
            }
        }
    }

    [HttpGet("GetClientsByMerch/{userid}")]
    public async Task<IActionResult> GetClientsByMerch(int userid)
    {
        string query = @"SELECT DISTINCT C.ClientCode, C.RaisonSocial, C.Gouvernorat, C.Adresse, C.IdClient
                 FROM Client C 
                 INNER JOIN Users U ON C.Merch = U.login
                 WHERE U.user_id = @userid";

        string sqlDatasource = _configuration.GetConnectionString("merchandiser")
                                ?? throw new InvalidOperationException("Connection string 'merchandiser' not found.");

        using (SqlConnection myCon = new SqlConnection(sqlDatasource))
        {
            await myCon.OpenAsync();
            using (SqlCommand myCommand = new SqlCommand(query, myCon))
            {
                myCommand.Parameters.AddWithValue("@userid", userid);

                using (SqlDataReader reader = await myCommand.ExecuteReaderAsync())
                {
                    var clients = new List<Client>();
                    while (await reader.ReadAsync())
                    {
                        clients.Add(new Client
                        { 
                            ClientCode = reader.GetString(0),
                            RaisonSocial = reader.GetString(1),
                            Gouvernorat = reader.GetString(2),
                            Adresse = reader.GetString(3),
                            IdClient= reader.GetInt32(4)
                        });
                    }

                    if (clients.Count == 0)
                    {
                        return NotFound($"Aucun client trouvé pour le login : {@userid}");
                    }

                    return Ok(clients);
                }
            }
        }
    }
}

public class User
{
    public string Name { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public int User_id { get; set; }
}

public class Client
{
    public string ClientCode { get; set; } = string.Empty;
    public string RaisonSocial { get; set; } = string.Empty;
    public string Gouvernorat { get; set; } = string.Empty;
    public string Adresse { get; set; } = string.Empty;
    public int IdClient { get; set; }
    
    
    
}
