using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Merchandiser.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RapportingController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public RapportingController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public class MissionRapport
        {
            public int MissionId { get; set; }
            public int IdClient { get; set; }
            public string RaisonSocial { get; set; } = string.Empty;
            public string MissionDate { get; set; } = string.Empty;
            public string Article { get; set; } = string.Empty;
            public string Adresse { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public double Prix { get; set; }
            public int Contenance { get; set; }
            public string Marque { get; set; } = string.Empty;
            public int IdLigne { get; set; }
        }

        public class MissionRapportQte
        {
            public int MissionId { get; set; }
            public int IdClient { get; set; }
            public string RaisonSocial { get; set; } = string.Empty;
            public string MissionDate { get; set; } = string.Empty;
            public string Article { get; set; } = string.Empty;
            public string Adresse { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public int Qte { get; set; }
            public int Contenance { get; set; }
            public string Marque { get; set; } = string.Empty;
            public int IdLigne { get; set; }
        }

        public class MissionRapportFacing
        {
            public int MissionId { get; set; }
            public int IdClient { get; set; }
            public string RaisonSocial { get; set; } = string.Empty;
            public string MissionDate { get; set; } = string.Empty;
            public string Adresse { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string ImgApres { get; set; } = string.Empty;
            public string ImgAvant { get; set; } = string.Empty;
            public string Gamme { get; set; } = string.Empty;
            public int IdLigne { get; set; }
        }

        [HttpGet("GetRapport")]
        public async Task<IActionResult> GetRapport([FromQuery] string date = null, int? user_id = null, int? mission_id = null)
        {
            try
            {
                string query = @"
                    SELECT l.IdLigne, l.Prix, l.Contenance, m.mission_id, m.user_id, m.mission_date, m.client,
                    c.RaisonSocial AS RaisonSocial, c.Adresse AS Adresse, a.Designation AS Article, r.Intitule AS Marque, u.name AS UserName
                    FROM LigneMarquePrix l
                    INNER JOIN mission m ON m.mission_id = l.IdMission 
                    INNER JOIN Client c ON c.IdClient = m.client  
                    INNER JOIN Article a ON a.Id = l.IdArticle
                    INNER JOIN Marque r ON r.IdMarque = l.IdMarque
                    INNER JOIN dbo.Users u ON m.user_id = u.user_id";
                if (!string.IsNullOrEmpty(date))
                {
                    query += " WHERE CONVERT(date, m.mission_date) = @Date";
                }

                if (user_id.HasValue)
                {
                    query += !string.IsNullOrEmpty(date) ? " AND m.user_id = @User_id" : " WHERE m.user_id = @User_id";
                }

                if (mission_id.HasValue)
                {
                    query += !string.IsNullOrEmpty(date) || user_id.HasValue ? " AND m.mission_id = @Mission_id" : " WHERE m.mission_id = @Mission_id";
                }

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
                        if (user_id.HasValue)
                        {
                            myCommand.Parameters.AddWithValue("@User_id", user_id.Value);
                        }
                        if (mission_id.HasValue)
                        {
                            myCommand.Parameters.AddWithValue("@Mission_id", mission_id.Value);
                        }

                        using (SqlDataReader myReader = await myCommand.ExecuteReaderAsync())
                        {
                            var missions = new List<MissionRapport>();
                            while (await myReader.ReadAsync())
                            {
                                missions.Add(new MissionRapport
                                {
                                    IdLigne = (int)myReader["IdLigne"],
                                    Article = (string)myReader["Article"],
                                    Prix = (double)myReader["Prix"],
                                    Contenance = (int)myReader["Contenance"],
                                    IdClient = (int)myReader["client"],
                                    Marque = (string)myReader["Marque"],
                                    UserName = (string)myReader["UserName"],
                                    MissionId = (int)myReader["mission_id"],
                                    RaisonSocial = (string)myReader["RaisonSocial"],
                                    Adresse = (string)myReader["Adresse"],
                                    MissionDate = ((DateTime)myReader["mission_date"]).ToString("yyyy-MM-dd"), // Formatage de la date
                                });
                            }
                            return Ok(missions);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Database error occurred." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("GetRapportQte")]
        public async Task<IActionResult> GetRapportQte([FromQuery] string date = null, int? user_id = null, int? mission_id = null)
        {
            try
            {
                string query = @"
                    SELECT l.IdLigne, l.Qte, l.Contenance, m.mission_id, m.user_id, m.mission_date, m.client,
                    c.RaisonSocial AS RaisonSocial, c.Adresse AS Adresse, a.Designation AS Article, r.Intitule AS Marque,u.name AS UserName
                    FROM LigneMarqueQte l
                    INNER JOIN mission m ON m.mission_id = l.IdMission 
                    INNER JOIN Client c ON c.IdClient = m.client  
                    INNER JOIN Article a ON a.Id = l.IdArticle
                    INNER JOIN Marque r ON r.IdMarque = l.IdMarque
                    INNER JOIN dbo.Users u ON m.user_id = u.user_id";

                if (!string.IsNullOrEmpty(date))
                {
                    query += " WHERE CONVERT(date, m.mission_date) = @Date";
                }

                if (user_id.HasValue)
                {
                    query += !string.IsNullOrEmpty(date) ? " AND m.user_id = @User_id" : " WHERE m.user_id = @User_id";
                }

                if (mission_id.HasValue)
                {
                    query += !string.IsNullOrEmpty(date) || user_id.HasValue ? " AND m.mission_id = @Mission_id" : " WHERE m.mission_id = @Mission_id";
                }

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
                        if (user_id.HasValue)
                        {
                            myCommand.Parameters.AddWithValue("@User_id", user_id.Value);
                        }
                        if (mission_id.HasValue)
                        {
                            myCommand.Parameters.AddWithValue("@Mission_id", mission_id.Value);
                        }

                        using (SqlDataReader myReader = await myCommand.ExecuteReaderAsync())
                        {
                            var missions = new List<MissionRapportQte>();
                            while (await myReader.ReadAsync())
                            {
                                missions.Add(new MissionRapportQte
                                {
                                    IdLigne = (int)myReader["IdLigne"],
                                    Article = (string)myReader["Article"],
                                    Qte = (int)myReader["Qte"],
                                    Contenance = (int)myReader["Contenance"],
                                    IdClient = (int)myReader["client"],
                                    Marque = (string)myReader["Marque"],
                                    UserName = (string)myReader["UserName"],
                                    MissionId = (int)myReader["mission_id"],
                                    RaisonSocial = (string)myReader["RaisonSocial"],
                                    Adresse = (string)myReader["Adresse"],
                                    MissionDate = ((DateTime)myReader["mission_date"]).ToString("yyyy-MM-dd"), // Formatage de la date
                                });
                            }
                            return Ok(missions);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Database error occurred." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("GetRapportFacing")]
        public async Task<IActionResult> GetRapportFacing([FromQuery] string? date = null, int? user_id = null, int? mission_id = null)
        {
            try
            {
                string query = @"
                    SELECT f.IdLigne, f.ImgAvant, f.ImgApres, m.mission_id, m.user_id, m.mission_date, m.client,
                    c.RaisonSocial AS RaisonSocial, c.Adresse AS Adresse, g.Intitule AS Gamme,u.name AS UserName
                    FROM Facing f
                    INNER JOIN mission m ON m.mission_id = f.IdMission 
                    INNER JOIN Client c ON c.IdClient = m.client  
                    INNER JOIN Gamme g ON g.Id = f.Id
                    INNER JOIN dbo.Users u ON m.user_id = u.user_id";

                if (!string.IsNullOrEmpty(date))
                {
                    query += " WHERE CONVERT(date, m.mission_date) = @Date";
                }

                if (user_id.HasValue)
                {
                    query += !string.IsNullOrEmpty(date) ? " AND m.user_id = @User_id" : " WHERE m.user_id = @User_id";
                }

                if (mission_id.HasValue)
                {
                    query += !string.IsNullOrEmpty(date) || user_id.HasValue ? " AND m.mission_id = @Mission_id" : " WHERE m.mission_id = @Mission_id";
                }

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
                        if (user_id.HasValue)
                        {
                            myCommand.Parameters.AddWithValue("@User_id", user_id.Value);
                        }
                        if (mission_id.HasValue)
                        {
                            myCommand.Parameters.AddWithValue("@Mission_id", mission_id.Value);
                        }

                        using (SqlDataReader myReader = await myCommand.ExecuteReaderAsync())
                        {
                            var missions = new List<MissionRapportFacing>();
                            while (await myReader.ReadAsync())
                            {
                                missions.Add(new MissionRapportFacing
                                {
                                    IdLigne = (int)myReader["IdLigne"],
                                    ImgAvant = (string)myReader["ImgAvant"],
                                    ImgApres = (string)myReader["ImgApres"],
                                    IdClient = (int)myReader["client"],
                                    MissionId = (int)myReader["mission_id"],
                                    RaisonSocial = (string)myReader["RaisonSocial"],
                                    UserName = (string)myReader["UserName"],
                                    Adresse = (string)myReader["Adresse"],
                                    Gamme = (string)myReader["Gamme"],
                                    MissionDate = ((DateTime)myReader["mission_date"]).ToString("yyyy-MM-dd"), // Formatage de la date
                                });
                            }
                            return Ok(missions);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Database error occurred." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }
}
