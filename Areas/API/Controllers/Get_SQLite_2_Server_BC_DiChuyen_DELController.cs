using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using QOS.Areas.API.Models;

namespace QOS.Areas.API.Controllers
{
    [Area("API")]
    [Route("api/[controller]")]
    [ApiController]
    public class Get_SQLite_2_Server_BC_DiChuyen_DELController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<Get_SQLite_2_Server_BC_DiChuyen_DELController> _logger;

        public Get_SQLite_2_Server_BC_DiChuyen_DELController(IConfiguration config,IWebHostEnvironment environment, ILogger<Get_SQLite_2_Server_BC_DiChuyen_DELController> logger)
        {
            _config = config;
            _environment = environment;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }

        [HttpPost]
        public IActionResult Get_SQLite_2_Server_BC_DiChuyen_DEL(
            [FromQuery] string? Code_G,
            [FromQuery] string? Report_ID,
            [FromQuery] string? Audit_Time_V,
            [FromForm] DeletePhotoRequest? request)
        {
            var results = new List<object>();
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
                    results.Add(new { KQ = "NG2" + factoryID + "_" + (request?.Img_Name ?? "") + "_" + facCode });
                    return Ok(results);
                }

                // ✅ Parse Code_G để lấy LoginID
                var (codeGs, myDeviceName, myMAC, loginID, dateF) = Functions.ParseCodeG(Code_G);

                // ✅ Delete report

                var rs = DeleteReportFromDatabase(Report_ID, Audit_Time_V ?? "", loginID);
                results.Add(new { KQ = rs });
                
                // ✅ Xóa ảnh nếu có
                if (!string.IsNullOrEmpty(request?.Img_Name))
                {
                    
                    string photoResult  = DeleteImageList(request.Img_Name);
                    results.Add(new { KQ = photoResult });
                }

                return Ok(results);
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
                using (SqlCommand cmd = new("Json_BC_DiChuyen_Delete_Report", conn))
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
        private string DeleteImageList( string Img_Name )
        {
            // ✅ Xóa ảnh
                string imagePath = Path.Combine(
                    _environment.WebRootPath, 
                    "upload", 
                    "Photos", 
                    "Form4_BCCLM");

                string textCut = "_###_";

                string result = Functions.DeleteImgList(Img_Name,imagePath,textCut,_logger);
                return result;
        }
    }
}