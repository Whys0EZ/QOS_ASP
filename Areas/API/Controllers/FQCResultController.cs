using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using QOS.Areas.API.Models;

namespace QOS.Areas.API.Controllers
{
    [Area("API")]
    [Route("api/[controller]")]
    [ApiController]
    public class FQCResultController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<FQCResultController> _logger;

        public FQCResultController(IConfiguration config, ILogger<FQCResultController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }

        [HttpPost]
        public IActionResult FQCResult([FromBody] FQCResultModel request)
        {
            if (request == null)
                return BadRequest(new { error = "Invalid request data" });

            try
            {
                string result = "";

                switch (request.Act?.ToLower())
                {
                    case "insert":
                        result = InsertFQCResult(request);
                        break;
                    default:
                        return BadRequest(new { error = "Invalid action. Use 'Insert'" });
                }

                if (result == "success")
                    return Ok(new { message = "success" });
                else
                    return StatusCode(500, new { error = result });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, $"Database error in SaveFQCResult - Action: {request.Act}");
                return StatusCode(500, new { error = "Database error occurred" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in SaveFQCResult - Action: {request.Act}");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        private string InsertFQCResult(FQCResultModel request)
        {
            // ✅ Tạo ID_Result giống PHP
            string idResult = $"{request.ID_Data}_{request.ModuleName}_{request.SO}_{request.mStyle}_{request.PO}_{request.ResultStatus}";
            DateTime workDate = DateTime.Now;

            using (SqlConnection conn = new(_connectionString))
            {
                string query = @"
                    INSERT INTO FQC_UQ_Result
                    (
                        [ID_Result], [Customer], [WorkDate], [ModuleName], [ID_Report],
                        [ID_Data], [ResultStatus], [No_Carton], [SO], [Size_Name],
                        [Style], [Item_No], [PO], [Qty], [AQL],
                        [Operation], [Update_Date], [shipMode], [Unit], [Fault],
                        [Destination], [PRO], [Remedies], [Audit_Time], [CreatedBy],
                        [UserUpdate], [LastUpdate], [Remark], [Re_Audit], [Total_Fault_QTY],
                        [Industry], [OQL], [Check_Qty]
                    )
                    VALUES
                    (
                        @ID_Result, @Customer, @WorkDate, @ModuleName, @ID_Report,
                        @ID_Data, @ResultStatus, @No_Carton, @SO, @Size_Name,
                        @Style, @Item_No, @PO, @Qty, @AQL,
                        @Operation, @Update_Date, @shipMode, @Unit, @Fault,
                        @Destination, @PRO, @Remedies, @Audit_Time, @CreatedBy,
                        @UserUpdate, @LastUpdate, @Remark, @Re_Audit, @Total_Fault_QTY,
                        @Industry, @OQL, @Check_Qty
                    )";

                using (SqlCommand cmd = new(query, conn))
                {
                    // ✅ Generated fields
                    cmd.Parameters.AddWithValue("@ID_Result", idResult);
                    cmd.Parameters.AddWithValue("@WorkDate", workDate);
                    cmd.Parameters.AddWithValue("@LastUpdate", workDate);
                    
                    // ✅ Basic Info
                    cmd.Parameters.AddWithValue("@Customer", request.Customer ?? "");
                    cmd.Parameters.AddWithValue("@ModuleName", request.ModuleName ?? "");
                    cmd.Parameters.AddWithValue("@ID_Report", request.ID_Report ?? "");
                    cmd.Parameters.AddWithValue("@ID_Data", request.ID_Data ?? "");
                    cmd.Parameters.AddWithValue("@ResultStatus", request.ResultStatus ?? "");
                    cmd.Parameters.AddWithValue("@No_Carton", request.No_Carton ?? "");
                    
                    // ✅ Order Info
                    cmd.Parameters.AddWithValue("@SO", request.SO ?? "");
                    cmd.Parameters.AddWithValue("@Size_Name", request.SizeName ?? "");
                    cmd.Parameters.AddWithValue("@Style", request.mStyle ?? "");
                    cmd.Parameters.AddWithValue("@Item_No", request.Item ?? "");
                    cmd.Parameters.AddWithValue("@PO", request.PO ?? "");
                    cmd.Parameters.AddWithValue("@Qty", request.Qty);
                    cmd.Parameters.AddWithValue("@AQL", request.AQL ?? "");
                    cmd.Parameters.AddWithValue("@Operation", request.Operation ?? "");
                    cmd.Parameters.AddWithValue("@Update_Date", request.Update ?? "");
                    cmd.Parameters.AddWithValue("@shipMode", request.shipMode ?? "");
                    cmd.Parameters.AddWithValue("@Unit", request.Production ?? "");  // Unit = Production
                    cmd.Parameters.AddWithValue("@Fault", request.Fault ?? "");
                    cmd.Parameters.AddWithValue("@Destination", request.Destination ?? "");
                    cmd.Parameters.AddWithValue("@PRO", request.Production ?? "");  // PRO = Production
                    cmd.Parameters.AddWithValue("@Remedies", request.Remedies ?? "");
                    
                    // ✅ Audit Info
                    cmd.Parameters.AddWithValue("@Audit_Time", request.Audit_time);
                    cmd.Parameters.AddWithValue("@CreatedBy", request.CreatedBy ?? "");
                    cmd.Parameters.AddWithValue("@UserUpdate", request.UserUpdate ?? "");
                    cmd.Parameters.AddWithValue("@Remark", request.Remark ?? "");
                    cmd.Parameters.AddWithValue("@Re_Audit", request.Re_Audit);
                    cmd.Parameters.AddWithValue("@Total_Fault_QTY", request.Total_Fault_QTY);
                    
                    // ✅ Additional Info
                    cmd.Parameters.AddWithValue("@Industry", request.Industry ?? "");
                    cmd.Parameters.AddWithValue("@OQL", request.OQL ?? "");
                    cmd.Parameters.AddWithValue("@Check_Qty", request.Check_Qty);

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    
                    if (rowsAffected > 0)
                    {
                        _logger.LogInformation($"FQC Result inserted - ID: {idResult}");
                        return "success";
                    }
                    else
                    {
                        return "error: No rows inserted";
                    }
                }
            }
        }
    }
}