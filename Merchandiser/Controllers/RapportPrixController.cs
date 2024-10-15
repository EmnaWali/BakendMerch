using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Merchandiser.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RapportPrixController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public RapportPrixController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public class Article
        {
            public int Id { get; set; }
            public string Designation { get; set; } = string.Empty;
            public string EAN { get; set; } = string.Empty;
        }

        public class Marque
        {
            public int IdMarque { get; set; }
            public string Intitule { get; set; } = string.Empty;
        }

        public class AddLigneMarquePrixRequest
        {
            public int IdArticle { get; set; }
            public int IdMarque { get; set; }
            public int Contenance { get; set; }
            public decimal Prix { get; set; }
            public int IdMission { get; set; }
        }

        // GET api/RapportPrix/GetIdClientByMissionId/{missionId}
        [HttpGet("GetIdClientByMissionId/{missionId}")]
        public async Task<IActionResult> GetIdClientByMissionId(int missionId)
        {
            if (missionId <= 0)
            {
                return BadRequest("ID de mission invalide.");
            }

            string sqlDatasource = _configuration.GetConnectionString("Merchandiser")
                                   ?? throw new InvalidOperationException("Connection string 'Merchandiser' not found.");

            string query = "SELECT client FROM dbo.mission WHERE mission_id = @MissionId";
            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                await myCon.OpenAsync();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    myCommand.Parameters.AddWithValue("@MissionId", missionId);
                    object result = await myCommand.ExecuteScalarAsync();
                    if (result == null || result == DBNull.Value)
                    {
                        return NotFound("Mission non trouvée.");
                    }
                    return Ok(Convert.ToInt32(result));
                }
            }
        }


        private async Task<bool> IdClientExists(int idClient)
        {
            string query = "SELECT COUNT(1) FROM dbo.Client WHERE IdClient = @IdClient";
            string sqlDatasource = _configuration.GetConnectionString("Merchandiser")
                                   ?? throw new InvalidOperationException("Connection string 'Merchandiser' not found.");

            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                await myCon.OpenAsync();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    myCommand.Parameters.AddWithValue("@IdClient", idClient);
                    int count = (int)await myCommand.ExecuteScalarAsync();
                    return count > 0;
                }
            }
        }

        [HttpPost("AddLigneMarquePrix")]
       
        public async Task<IActionResult> AddLigneMarquePrix([FromBody] AddLigneMarquePrixRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Récupérer l'IdClient basé sur IdMission
            var idClientResult = await GetIdClientByMissionId(request.IdMission);
            if (idClientResult is not OkObjectResult okObjectResult || !(okObjectResult.Value is int idClient))
            {
                return BadRequest("Mission ou client non trouvé.");
            }

            if (!await IdClientExists(idClient))
            {
                return BadRequest("Client invalide.");
            }

            string query = @"
    INSERT INTO dbo.LigneMarquePrix (IdArticle, IdClient, IdMarque, Contenance, Prix, IdMission)
    VALUES (@IdArticle, @IdClient, @IdMarque, @Contenance, @Prix, @IdMission)";

            string sqlDatasource = _configuration.GetConnectionString("Merchandiser")
                ?? throw new InvalidOperationException("Connection string 'Merchandiser' not found.");

            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                await myCon.OpenAsync();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    myCommand.Parameters.AddWithValue("@IdArticle", request.IdArticle);
                    myCommand.Parameters.AddWithValue("@IdClient", idClient);
                    myCommand.Parameters.AddWithValue("@IdMarque", request.IdMarque);
                    myCommand.Parameters.AddWithValue("@Contenance", request.Contenance);
                    myCommand.Parameters.AddWithValue("@Prix", request.Prix);
                    myCommand.Parameters.AddWithValue("@IdMission", request.IdMission);

                    await myCommand.ExecuteNonQueryAsync();
                }
            }

            return Ok("Détails ajoutés avec succès.");
        }

        [HttpGet("GetArticles")]
        public async Task<IActionResult> GetArticles()
        {
            string query = "SELECT Id, Designation, EAN FROM dbo.Article";
            string sqlDatasource = _configuration.GetConnectionString("Merchandiser")
                ?? throw new InvalidOperationException("Connection string 'Merchandiser' not found.");

            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                await myCon.OpenAsync();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    using (SqlDataReader reader = await myCommand.ExecuteReaderAsync())
                    {
                        var articles = new List<Article>();
                        while (await reader.ReadAsync())
                        {
                            articles.Add(new Article
                            {
                                Id = reader.GetInt32(0),
                                Designation = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                EAN = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            });
                        }

                        return Ok(articles);
                    }
                }
            }
        }

        [HttpGet("GetMarques")]
        public async Task<IActionResult> GetMarques()
        {
            string query = "SELECT IdMarque, Intitule FROM dbo.Marque";
            string sqlDatasource = _configuration.GetConnectionString("Merchandiser")
                ?? throw new InvalidOperationException("Connection string 'Merchandiser' not found.");

            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                await myCon.OpenAsync();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    using (SqlDataReader reader = await myCommand.ExecuteReaderAsync())
                    {
                        var marques = new List<Marque>();
                        while (await reader.ReadAsync())
                        {
                            marques.Add(new Marque
                            {
                                IdMarque = reader.GetInt32(0),
                                Intitule = reader.GetString(1),
                            });
                        }

                        return Ok(marques);
                    }
                }
            }
        }
    }
}
