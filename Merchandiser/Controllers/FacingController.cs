using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;
using static Merchandiser.Controllers.RapportPrixController;

namespace Merchandiser.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacingController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public FacingController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public class Gamme
        {
            public int Id { get; set; }
            public string Intitule { get; set; }
        }

        [HttpGet("GetGamme")]
        public async Task<IActionResult> GetGamme()
        {
            string query = "SELECT Id, Intitule FROM dbo.Gamme";
            string sqlDatasource = _configuration.GetConnectionString("merchandiser")
                                  ?? throw new InvalidOperationException("Connection string 'merchandiser' not found.");

            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                await myCon.OpenAsync();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    using (SqlDataReader reader = await myCommand.ExecuteReaderAsync())
                    {
                        var gammes = new List<Gamme>();
                        while (await reader.ReadAsync())
                        {
                            gammes.Add(new Gamme
                            {
                                Id = reader.GetInt32(0),
                                Intitule = reader.GetString(1),
                            });
                        }

                        return Ok(gammes);
                    }
                }
            }
        }

        public class AddFacingRequest
        {
            public string ImgAvant { get; set; } = string.Empty;
            public string ImgApres { get; set; } = string.Empty;
            public int Id { get; set; } // Id pour Facing, pas Client
            public int IdClient { get; set; }
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


        [HttpPost("AddFacing")]
        public async Task<IActionResult> AddFacing([FromBody] AddFacingRequest request)
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

            string query = "INSERT INTO dbo.Facing (ImgAvant, ImgApres, IdClient, Id,IdMission) " +
                           "VALUES (@ImgAvant, @ImgApres, @IdClient, @Id,@IdMission)";
            string sqlDatasource = _configuration.GetConnectionString("merchandiser")
              ?? throw new InvalidOperationException("Connection string 'merchandiser' not found.");

            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                await myCon.OpenAsync();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    myCommand.Parameters.AddWithValue("@ImgAvant", request.ImgAvant);
                    myCommand.Parameters.AddWithValue("@ImgApres", request.ImgApres);
                    myCommand.Parameters.AddWithValue("@IdClient", idClient);
                    myCommand.Parameters.AddWithValue("@Id", request.Id);
                    myCommand.Parameters.AddWithValue("@IdMission", request.IdMission);
                    await myCommand.ExecuteNonQueryAsync();
                }
            }

            return Ok(new { message = "Facing entry added successfully" });
        }
      

    }
}
