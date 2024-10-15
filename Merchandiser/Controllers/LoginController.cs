using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
namespace Merchandiser.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> LoginUser([FromQuery] string login, [FromQuery] string password)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                return BadRequest(new { message = "Invalid login request" });
            }

            try
            {
                var connectionString = _configuration.GetConnectionString("Merchandiser");
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT [user_id], [name], [password] FROM [dbo].[Users] WHERE [login] = @login";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", login);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var storedPassword = reader["password"].ToString();

                                if (storedPassword == password)
                                {
                                    return Ok(new { message = "Login Successful." });
                                }
                                else
                                {
                                    return Unauthorized(new { message = "Invalid login credentials." });
                                }
                            }
                            else
                            {
                                return BadRequest(new { message = "User not found." });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }
}