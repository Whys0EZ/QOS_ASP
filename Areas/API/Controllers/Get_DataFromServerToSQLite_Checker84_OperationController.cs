using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using QOS.Areas.API.Models;

namespace QOS.Areas.API.Controllers
{
    [Area("API")]
    [Route("api/[controller]")]
    [ApiController]
    public class Get_DataFromServerToSQLite_Checker84_OperationController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<Get_DataFromServerToSQLite_Checker84_OperationController> _logger;

        public Get_DataFromServerToSQLite_Checker84_OperationController(IConfiguration config, ILogger<Get_DataFromServerToSQLite_Checker84_OperationController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }

        [HttpGet]
        public IActionResult Get_DataFromServerToSQLite_Checker84_Operation(string? Code_G)
        {
            if (string.IsNullOrEmpty(Code_G))
                return BadRequest(new { error = "Code_G is required" });

            try
            {
                // ✅ SỬ DỤNG HELPER
                string facCode = _config.GetValue<string>("AppSettings:FactoryCode") ?? "";
                var (isValid, factoryID, errorMsg) = Functions.ValidateCodeG(Code_G, facCode);

                if (!isValid)
                {
                    _logger.LogWarning($"Authentication failed: {errorMsg}");
                    return Unauthorized(new { error = errorMsg });
                }

                // ✅ SỬ DỤNG HELPER PARSE
                var (codeGs, myDeviceName, myMAC, Type_c, dateF) = Functions.ParseCodeG(Code_G);

                _logger.LogInformation($"Checker84_Operation - myDeviceName: {myDeviceName}, Type_c: {Type_c}");

                // Query database...
                // var Checker84_Operation = Checker84_Operation();
                // return Ok(new { Checker84_Operation = Checker84_Operation });

                var response = new
                {
                    Checker84_Operation = Checker84_Operation()
                };
                return Ok(response);
            }
            
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchBCCPI");
                return BadRequest(new { error = "An error occurred processing your request" });
            }
        }
        private List<object> Checker84_Operation()
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select MO,Operation_Code, Operation_Name_VN,CMD from Operation_Code where CMD=1 ORDER BY Operation_Code ASC", conn))
            {
               
                // cmd.Parameters.AddWithValue("@UserName", UserName);

                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        MO = dr["MO"]?.ToString() ?? "",
                        Operation_Code = dr["Operation_Code"]?.ToString() ?? "",
                        Operation_Name = dr["Operation_Name_VN"]?.ToString() ?? "",
                        CMD = dr["CMD"]?.ToString() ?? "",
                        WorkDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    });
                }
            }

            return list;
        }

    }
}


// GET /api/Get_DataFromServerToSQLite_Checker84_Operation?Code_G=f4cf3fe4b67244332eecec055742d449REG263ca1464c3480896c1b78a5eac5a6e9772b32a1f754ba1c09b3695e0cb6cde7f_____LenovoTB-8504X_____d0:f8:8c:ea:e9:cc_____CMD
