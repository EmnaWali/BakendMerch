using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Data.SqlClient;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;

namespace Merchandiser.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PointageController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public PointageController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // POST api/Pointage/AddPointage
        [HttpPost("AddPointage")]
        public async Task<IActionResult> AddPointage([FromBody] AddPointageRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request cannot be null.");
            }

            string query = "INSERT INTO dbo.pointage (date_pointage, heure_arrivee, heure_depart, mission_id, user_id, etat, commentaire) " +
                           "VALUES (@DatePointage, @HeureArrivee, @HeureDepart, @MissionId, @UserId, @Etat, @Commentaire)";

            string sqlDatasource = _configuration.GetConnectionString("Merchandiser")
                                   ?? throw new InvalidOperationException("Connection string 'Merchandiser' not found.");

            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                await myCon.OpenAsync();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    myCommand.Parameters.AddWithValue("@DatePointage", request.DatePointage);
                    myCommand.Parameters.AddWithValue("@HeureArrivee", TimeSpan.Parse(request.HeureArrivee));
                    myCommand.Parameters.AddWithValue("@HeureDepart", TimeSpan.Parse(request.HeureDepart));
                    myCommand.Parameters.AddWithValue("@MissionId", request.MissionId);
                    myCommand.Parameters.AddWithValue("@UserId", request.UserId);
                    myCommand.Parameters.AddWithValue("@Etat", string.IsNullOrEmpty(request.Etat) ? (object)DBNull.Value : request.Etat);
                    myCommand.Parameters.AddWithValue("@Commentaire", string.IsNullOrEmpty(request.Commentaire) ? (object)DBNull.Value : request.Commentaire);

                    await myCommand.ExecuteNonQueryAsync();
                }
            }

            return Ok("Pointage added successfully.");
        }
        // PUT api/Pointage/UpdatePointage/{id}
        [HttpPut("UpdatePointage/{id}")]
        public async Task<IActionResult> UpdatePointage(int id, [FromBody] UpdatePointageRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request cannot be null.");
            }

            string getExistingValuesQuery = "SELECT date_pointage, heure_arrivee, heure_depart, etat, commentaire " +
                                            "FROM dbo.pointage WHERE id_pointage = @IdPointage";

            string sqlDatasource = _configuration.GetConnectionString("Merchandiser")
                                       ?? throw new InvalidOperationException("Connection string 'Merchandiser' not found.");

            var existingValues = new UpdatePointageRequest();

            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                await myCon.OpenAsync();
                using (SqlCommand myCommand = new SqlCommand(getExistingValuesQuery, myCon))
                {
                    myCommand.Parameters.AddWithValue("@IdPointage", id);

                    using (SqlDataReader reader = await myCommand.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            existingValues.DatePointage = reader.GetDateTime(0);
                            existingValues.HeureArrivee = reader.IsDBNull(1) ? "00:00:00" : reader.GetTimeSpan(1).ToString(@"hh\:mm\:ss");
                            existingValues.HeureDepart = reader.IsDBNull(2) ? "00:00:00" : reader.GetTimeSpan(2).ToString(@"hh\:mm\:ss");
                            existingValues.Etat = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                            existingValues.Commentaire = reader.IsDBNull(4) ? null : reader.GetString(4);
                        }
                        else
                        {
                            return NotFound("Pointage not found.");
                        }
                    }
                }
            }

            string updateQuery = "UPDATE dbo.pointage SET " +
                                 "date_pointage = @DatePointage, " +
                                 "heure_arrivee = @HeureArrivee, " +
                                 "heure_depart = @HeureDepart, " +
                                 "etat = @Etat, " +
                                 "commentaire = @Commentaire " +
                                 "WHERE id_pointage = @IdPointage";

            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                await myCon.OpenAsync();
                using (SqlCommand myCommand = new SqlCommand(updateQuery, myCon))
                {
                    myCommand.Parameters.AddWithValue("@DatePointage", request.DatePointage == default ? existingValues.DatePointage : request.DatePointage);
                    myCommand.Parameters.AddWithValue("@HeureArrivee", string.IsNullOrEmpty(request.HeureArrivee) ? existingValues.HeureArrivee : TimeSpan.Parse(request.HeureArrivee));
                    myCommand.Parameters.AddWithValue("@HeureDepart", string.IsNullOrEmpty(request.HeureDepart) ? existingValues.HeureDepart : TimeSpan.Parse(request.HeureDepart));
                    myCommand.Parameters.AddWithValue("@Etat", string.IsNullOrEmpty(request.Etat) ? existingValues.Etat : request.Etat);
                    myCommand.Parameters.AddWithValue("@Commentaire", string.IsNullOrEmpty(request.Commentaire) ? (object)DBNull.Value : request.Commentaire);
                    myCommand.Parameters.AddWithValue("@IdPointage", id);

                    await myCommand.ExecuteNonQueryAsync();
                }
            }

            return Ok(new { Message = "Pointage updated successfully." });
        }


        // GET api/Pointage/GetPointageDetailsByMissionId/{missionId}
        [HttpGet("GetPointageDetailsByMissionId/{missionId}")]
        public async Task<IActionResult> GetPointageDetailsByMissionId(int missionId)
        {
            if (missionId <= 0)
            {
                return BadRequest("ID de mission invalide.");
            }

            string sqlDatasource = _configuration.GetConnectionString("Merchandiser")
                                   ?? throw new InvalidOperationException("Connection string 'Merchandiser' not found.");

            int userId;
            string getUserIdQuery = "SELECT user_id FROM dbo.mission WHERE mission_id = @MissionId";
            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                await myCon.OpenAsync();
                using (SqlCommand myCommand = new SqlCommand(getUserIdQuery, myCon))
                {
                    myCommand.Parameters.AddWithValue("@MissionId", missionId);
                    object result = await myCommand.ExecuteScalarAsync();
                    if (result == null || result == DBNull.Value)
                    {
                        return NotFound("Mission not found.");
                    }
                    userId = Convert.ToInt32(result);
                }
            }

            if (!await UserExists(userId))
            {
                return BadRequest("Utilisateur non valide.");
            }

            string query = "SELECT p.id_pointage, p.date_pointage, p.heure_arrivee, p.heure_depart, " +
                           "p.etat, p.commentaire, c.RaisonSocial AS RaisonSocial,c.Adresse AS Adresse, m.mission_description, p.user_id " +
                           "FROM dbo.pointage p " +
                           "JOIN dbo.mission m ON p.mission_id = m.mission_id " +
                           "JOIN dbo.Client c ON m.client = c.IdClient " +
                           "WHERE p.mission_id = @MissionId";

            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                await myCon.OpenAsync();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    myCommand.Parameters.AddWithValue("@MissionId", missionId);

                    using (SqlDataReader reader = await myCommand.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var pointage = new
                            {
                                IdPointage = reader.GetInt32(0),
                                DatePointage = reader.GetDateTime(1).ToString("yyyy-MM-dd"),
                                HeureArrivee = reader.IsDBNull(2) ? "00:00:00" : reader.GetTimeSpan(2).ToString(@"hh\:mm\:ss"),
                                HeureDepart = reader.IsDBNull(3) ? "00:00:00" : reader.GetTimeSpan(3).ToString(@"hh\:mm\:ss"),
                                Etat = reader.GetString(4),
                                Commentaire = reader.IsDBNull(5) ? null : reader.GetString(5),
                                RaisonSocial = reader.GetString(6),
                                Adresse = reader.GetString(7),
                                MissionAffectee = reader.GetString(8),
                                UserId = reader.GetInt32(9)
                            };

                            return Ok(pointage);
                        }
                        else
                        {
                            var defaultPointage = new AddPointageRequest
                            {
                                DatePointage = DateTime.Now.Date,
                                HeureArrivee = "00:00:00",
                                HeureDepart = "00:00:00",
                                MissionId = missionId,
                                UserId = userId,
                                Etat = "Non spécifié",
                                Commentaire = null
                            };

                            await AddPointage(defaultPointage);

                            return await GetPointageDetailsByMissionId(missionId);
                        }
                    }
                }
            }
        }


        private async Task<bool> UserExists(int userId)
        {
            string query = "SELECT COUNT(1) FROM dbo.Users WHERE user_id = @user_id";
            string sqlDatasource = _configuration.GetConnectionString("Merchandiser")
                                   ?? throw new InvalidOperationException("Connection string 'Merchandiser' not found.");

            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                await myCon.OpenAsync();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    myCommand.Parameters.AddWithValue("@user_id", userId);
                    int count = (int)await myCommand.ExecuteScalarAsync();
                    return count > 0;
                }
            }
        }

        public class AddPointageRequest
        {
            public DateTime DatePointage { get; set; }
            public string HeureArrivee { get; set; } = "00:00:00"; // Valeur par défaut
            public string HeureDepart { get; set; } = "00:00:00"; // Valeur par défaut
            public int MissionId { get; set; }
            public int UserId { get; set; }
            public string Etat { get; set; } = string.Empty;
            public string? Commentaire { get; set; }  // Nullable
        }

        public class UpdatePointageRequest
        {
            public DateTime DatePointage { get; set; }
            public string HeureArrivee { get; set; } = "00:00:00"; // Valeur par défaut
            public string HeureDepart { get; set; } = "00:00:00"; // Valeur par défaut
            public string Etat { get; set; } = string.Empty;
            public string? Commentaire { get; set; }  // Nullable
        }
    }
}
