using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using static Merchandiser.Controllers.RapportPrixController;

namespace Merchandiser.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RapportQteController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public RapportQteController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

   

        public class AddLigneMarqueQteRequest
        {
            public string IdArticle { get; set; } = string.Empty;
            public int IdMarque { get; set; }
            public int Contenance { get; set; }
            public decimal Qte { get; set; }
            public int IdClient { get; set; }
            public int IdMission { get; set; }
        }

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

        // POST api/Article/AddLigneMarqueQte
        [HttpPost("AddLigneMarqueQte")]
        public async Task<IActionResult> AddLigneMarqueQte([FromBody] AddLigneMarqueQteRequest request)
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


            string query = "INSERT INTO dbo.LigneMarqueQte (IdArticle,IdClient, IdMarque, Contenance, Qte,IdMission) " +
                           "VALUES (@IdArticle, @IdClient, @IdMarque, @Contenance, @Qte,@IdMission)";
            string sqlDatasource = _configuration.GetConnectionString("merchandiser")
              ?? throw new InvalidOperationException("Connection string 'merchandiser' not found.");

            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                await myCon.OpenAsync();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    myCommand.Parameters.AddWithValue("@IdArticle", request.IdArticle);
                    myCommand.Parameters.AddWithValue("@IdClient", idClient);
                    myCommand.Parameters.AddWithValue("@IdMarque", request.IdMarque);
                    myCommand.Parameters.AddWithValue("@Contenance", request.Contenance);
                    myCommand.Parameters.AddWithValue("@Qte", request.Qte);
                    myCommand.Parameters.AddWithValue("@IdMission", request.IdMission);

                    await myCommand.ExecuteNonQueryAsync();
                }
            }

            return Ok("Détails ajoutés avec succès.");
        }
      
    }
}
