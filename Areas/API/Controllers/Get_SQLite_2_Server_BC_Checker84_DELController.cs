using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using QOS.Areas.API.Models;


namespace QOS.Areas.API.Controllers
{
    [Area("API")]
    [Route("api/[controller]")]
    [ApiController]
    public class Get_SQLite_2_Server_BC_Checker84_DELController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<Get_SQLite_2_Server_BC_Checker84_DELController> _logger;

        public Get_SQLite_2_Server_BC_Checker84_DELController(IConfiguration config, ILogger<Get_SQLite_2_Server_BC_Checker84_DELController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }

        [HttpPost]
        public IActionResult Get_SQLite_2_Server_BC_Checker84_DEL(
            [FromQuery] string? Code_G,
            [FromQuery] string? Report_ID,
            [FromQuery] string? Audit_Time_V,
            [FromForm] DeletePhotoRequest? request)
        {
            if (string.IsNullOrEmpty(Code_G))
                return BadRequest(new { KQ = "NG: Code_G is required" });

            if (string.IsNullOrEmpty(Report_ID))
                return BadRequest(new { KQ = "NG: Report_ID is required" });

            try
            {
                // ✅ Validate Code_G sử dụng Helper
                string facCode = _config.GetValue<string>("AppSettings:FactoryCode") ?? "";
                var (isValid, factoryID, errorMsg) = Functions.ValidateCodeG(Code_G, facCode);

                if (!isValid)
                {
                    _logger.LogWarning($"Authentication failed: {errorMsg}");
                    return Ok(new { KQ = errorMsg });
                }

                // ✅ Parse Code_G để lấy LoginID
                var (codeGs, myDeviceName, myMAC, loginID, dateF) = Functions.ParseCodeG(Code_G);

                // ✅ Delete report
                var result = DeleteReportFromDatabase(Report_ID, Audit_Time_V ?? "", loginID);

                return Ok(new { KQ = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteReport");
                return Ok(new { KQ = "NG: " + ex.Message });
            }
        }

        private string DeleteReportFromDatabase(string reportID, string auditTimeV, string loginID)
        {
            try
            {
                using (SqlConnection conn = new(_connectionString))
                using (SqlCommand cmd = new("Json_BC_Checker84_Delete_Report", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Report_ID", reportID);
                    cmd.Parameters.AddWithValue("@Audit_Time", auditTimeV);
                    cmd.Parameters.AddWithValue("@UserUpdate", loginID);

                    conn.Open();
                    
                    using SqlDataReader dr = cmd.ExecuteReader();
                    
                    // Đọc kết quả từ stored procedure
                    string result = "";
                    while (dr.Read())
                    {
                        result = dr["kq"]?.ToString() ?? "";
                    }

                    if (string.IsNullOrEmpty(result))
                    {
                        result = "OK: Report deleted successfully";
                    }

                    _logger.LogInformation($"Report deleted - Report_ID: {reportID}, LoginID: {loginID}, Result: {result}");

                    return result;
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, $"Database error deleting report {reportID}");
                return $"NG: Database error - {ex.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting report {reportID}");
                return $"NG: {ex.Message}";
            }
        }
    }
}