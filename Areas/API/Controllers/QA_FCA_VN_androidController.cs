using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using QOS.Areas.API.Models;
namespace QOS.Areas.API.Controllers
{
    [Area("API")]
    [Route("api/[controller]")]
    [ApiController]
    public class QA_FCA_VN_androidController : ControllerBase
    {
        private readonly ILogger<QA_FCA_VN_androidController> _logger;
        private readonly string _connectionString;

        public QA_FCA_VN_androidController(IConfiguration config, ILogger<QA_FCA_VN_androidController> logger)
        {
            _logger = logger;
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        [HttpPost]
        public IActionResult QA_FCA_VN_android([FromForm] FCAModel data)
        {
            try
            {
                _logger.LogInformation($"Received FCA data: {System.Text.Json.JsonSerializer.Serialize(data)}");

                if (data.Act != "Insert")
                {
                    return BadRequest(new { KQ = "NG: Invalid Act" });
                }
                else 
                {
                    string result = "";
                    result = InsertFCAData(data);

                    if (result == "success")
                        return Ok(new { message = "success" });
                    else
                        return StatusCode(500, new { error = result });
                    
                }

                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting FCA data");
                return Ok("fail");
            }
        }

        private string InsertFCAData(FCAModel request)
        {
            using (SqlConnection conn = new(_connectionString))
            {
                string query = @"
                    INSERT INTO TRACKINIG_Result(
							  [ID_Result]
							  ,[Customer]
							  ,[WorkDate]
							  ,[ModuleName]
							  ,[ID_Data]
							  ,[ResultStatus]
                              ,[IMG_Result]
							  ,[Infor_01]
							  ,[Infor_02]
							  ,[Infor_03]
							  ,[Infor_04]
							  ,[Infor_05]
							  ,[Infor_06]
							  ,[Infor_07]
							  ,[Infor_08]
							  ,[Infor_09]
							  ,[Infor_10]
							  ,[Infor_11]
							  ,[CreatedBy]	
							  ,[UserUpdate]	
							  ,[LastUpdate]						  
					           )   
                    VALUES
                    (
                        @ID_Result, @Customer, @WorkDate, @ModuleName, @ID_Data,
                        @ResultStatus, @ACDate,@Infor_01, @Infor_02, @Infor_03, @Infor_04, @Infor_05,
                        @Infor_06, @Infor_07,@Infor_08,@Infor_09,@Infor_10, @Infor_11, 
                        @CreatedBy, @UserUpdate, @WorkDate
                        
                    )";

                using (SqlCommand cmd = new(query, conn))
                {
                    AddBCCPIParameters(cmd, request);
                 
                    cmd.Parameters.AddWithValue("@ID_Result", request.ID_Data + "_" + request.ModuleName + "_" + request.Infor_01 + "_" + request.Infor_02 + "_" + request.Infor_04 + "_" + request.ResultStatus);
                    cmd.Parameters.AddWithValue("@WorkDate", DateTime.Now);

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    
                    return rowsAffected > 0 ? "success" : "error";
                }
            }
        }

        private void AddBCCPIParameters(SqlCommand cmd, FCAModel request)
        {
            
            cmd.Parameters.AddWithValue("@Customer", request.Customer ?? "");
            // cmd.Parameters.AddWithValue("@WorkDate", request.WorkDate ?? "");
            cmd.Parameters.AddWithValue("@ModuleName", request.ModuleName ?? "");
            cmd.Parameters.AddWithValue("@ID_Data", request.ID_Data ?? "");
            cmd.Parameters.AddWithValue("@ResultStatus", request.ResultStatus ?? "");
            cmd.Parameters.AddWithValue("@ACDate", request.ACDate ?? "");
            cmd.Parameters.AddWithValue("@Infor_01", request.Infor_01 ?? "");
            cmd.Parameters.AddWithValue("@Infor_02", request.Infor_02 ?? "");
            cmd.Parameters.AddWithValue("@Infor_03", request.Infor_03 ?? "");
            cmd.Parameters.AddWithValue("@Infor_04", request.Infor_04 ?? "");
            cmd.Parameters.AddWithValue("@Infor_05", request.Infor_05 ?? "");
            cmd.Parameters.AddWithValue("@Infor_06", request.Infor_06 ?? "");
            cmd.Parameters.AddWithValue("@Infor_07", request.Infor_07 ?? "");
            cmd.Parameters.AddWithValue("@Infor_08", request.Infor_08 ?? "");
            cmd.Parameters.AddWithValue("@Infor_09", request.Infor_09 ?? "");
            cmd.Parameters.AddWithValue("@Infor_10", request.Infor_10 ?? "");
            cmd.Parameters.AddWithValue("@Infor_11", request.Infor_11 ?? "");

            cmd.Parameters.AddWithValue("@CreatedBy", request.CreatedBy ?? "");
            cmd.Parameters.AddWithValue("@UserUpdate", request.UserUpdate ?? "");
        }
    }
}