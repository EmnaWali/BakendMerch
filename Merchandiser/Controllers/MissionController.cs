using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Merchandiser.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MissionController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public MissionController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET api/Mission/GetMissions
        [HttpGet("GetMissions")]
        public async Task<JsonResult> GetMissions([FromQuery] string date = null)
        {
            string query = @"
                SELECT m.mission_id, 
                       c.ClientCode AS ClientCode, 
                       c.Gouvernorat AS Gouvernorat,
                       c.Adresse AS Adresse,
                       c.RaisonSocial AS RaisonSocial,
                       u.name AS UserName, 
                       m.mission_description, 
                       m.mission_date, 
                       m.mission_time
                FROM dbo.mission m
                INNER JOIN dbo.Client c ON m.client = c.IdClient
                INNER JOIN dbo.Users u ON m.user_id = u.user_id";

            if (!string.IsNullOrEmpty(date))
            {
                query += " WHERE CONVERT(date, m.mission_date) = @Date";
            }

            DataTable table = new DataTable();
            string sqlDatasource = _configuration.GetConnectionString("merchandiser")
                                   ?? throw new InvalidOperationException("Connection string 'merchandiser' not found.");
            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                await myCon.OpenAsync();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    if (!string.IsNullOrEmpty(date))
                    {
                        myCommand.Parameters.AddWithValue("@Date", date);
                    }

                    using (SqlDataReader myReader = await myCommand.ExecuteReaderAsync())
                    {
                        table.Load(myReader);
                    }
                }
            }

            var missions = table.AsEnumerable().Select(row => new
            {
                MissionId = row["mission_id"],
                ClientCode = row["ClientCode"],
                Gouvernorat = row["Gouvernorat"],
                Adresse = row["Adresse"],
                RaisonSocial = row["RaisonSocial"],
                UserName = row["UserName"],
                MissionDescription = row["mission_description"],
                MissionDate = Convert.ToDateTime(row["mission_date"]).ToString("yyyy-MM-dd"),
                MissionTime = TimeSpan.TryParse(row["mission_time"].ToString(), out TimeSpan timeSpan) ? timeSpan.ToString(@"hh\:mm\:ss") : null
            });

            return new JsonResult(missions);
        }

        // POST api/Mission/AddMission
        [HttpPost("AddMission")]
        public async Task<IActionResult> AddMission([FromForm] int client, [FromForm] string missionDescription, [FromForm] int userId, [FromForm] string missionDate, [FromForm] string missionTime)
        {
            if (!DateTime.TryParse(missionDate, out DateTime parsedDate))
                return BadRequest(new { message = "Invalid date format." });

            if (!TimeSpan.TryParse(missionTime, out TimeSpan parsedTime))
                return BadRequest(new { message = "Invalid time format." });

            string query = "INSERT INTO dbo.mission (client, mission_description, user_id, mission_date, mission_time) VALUES (@Client, @MissionDescription, @UserId, @MissionDate, @MissionTime)";
            string sqlDatasource = _configuration.GetConnectionString("merchandiser")
                                   ?? throw new InvalidOperationException("Connection string 'merchandiser' not found.");

            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                await myCon.OpenAsync();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    myCommand.Parameters.AddWithValue("@Client", client);
                    myCommand.Parameters.AddWithValue("@MissionDescription", missionDescription);
                    myCommand.Parameters.AddWithValue("@UserId", userId);
                    myCommand.Parameters.AddWithValue("@MissionDate", parsedDate);
                    myCommand.Parameters.AddWithValue("@MissionTime", parsedTime);

                    try
                    {
                        await myCommand.ExecuteNonQueryAsync();
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
                    }
                }
            }

            return Ok(new { message = "Mission added successfully." });
        }


        // PUT api/Mission/UpdateMission
        // PUT api/Mission/UpdateMission
        [HttpPut("UpdateMission")]
        public async Task<IActionResult> UpdateMission([FromQuery] int missionId, [FromForm] int client, [FromForm] string missionDescription, [FromForm] string missionDate, [FromForm] string missionTime)
        {
            // Valider le format de la date
            if (!DateTime.TryParse(missionDate, out DateTime parsedDate))
            {
                return BadRequest(new { message = "Format de date invalide." });
            }

            // Valider le format de l'heure
            if (!TimeSpan.TryParse(missionTime, out TimeSpan parsedTime))
            {
                return BadRequest(new { message = "Format de l'heure invalide." });
            }

            string query = "UPDATE dbo.mission SET client = @Client, mission_description = @MissionDescription, mission_date = @MissionDate, mission_time = @MissionTime WHERE mission_id = @MissionId";
            string sqlDatasource = _configuration.GetConnectionString("merchandiser")
                                   ?? throw new InvalidOperationException("Connection string 'merchandiser' not found.");

            try
            {
                using (SqlConnection myCon = new SqlConnection(sqlDatasource))
                {
                    await myCon.OpenAsync();
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {
                        myCommand.Parameters.AddWithValue("@Client", client);
                        myCommand.Parameters.AddWithValue("@MissionDescription", missionDescription);
                        myCommand.Parameters.AddWithValue("@MissionDate", parsedDate);
                        myCommand.Parameters.AddWithValue("@MissionTime", parsedTime);
                        myCommand.Parameters.AddWithValue("@MissionId", missionId);

                        int affectedRows = await myCommand.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            return Ok(new { message = "Mission mise à jour avec succès." });
                        }
                        else
                        {
                            return NotFound(new { message = "Mission non trouvée." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur interne du serveur : {ex.Message}" });
            }
        }


        // DELETE api/Mission/DeleteMission
        [HttpDelete("DeleteMission")]
        public async Task<IActionResult> DeleteMission([FromQuery] int missionId)
        {
            string query = "DELETE FROM dbo.mission WHERE mission_id = @MissionId";
            string sqlDatasource = _configuration.GetConnectionString("merchandiser")
                                   ?? throw new InvalidOperationException("Connection string 'merchandiser' not found.");
            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                myCon.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    myCommand.Parameters.AddWithValue("@MissionId", missionId);
                    await myCommand.ExecuteNonQueryAsync();
                }
            }
            return Ok("Mission deleted successfully.");
        }

    }
}
