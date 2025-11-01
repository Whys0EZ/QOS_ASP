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
    public class feature_frm_BC_Checker84_SearchController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<feature_frm_BC_Checker84_SearchController> _logger;

        public feature_frm_BC_Checker84_SearchController(IConfiguration config, ILogger<feature_frm_BC_Checker84_SearchController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }

        [HttpGet]
        public IActionResult feature_frm_BC_Checker84_Search(
            string? Code_G, 
            string? Search_txt, 
            string? Unit, 
            string? Line,
            string? ALL)
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
                var (codeGs, myDeviceName, myMAC, loginID, dateF) = Functions.ParseCodeG(Code_G);

                // _logger.LogInformation($"SearchChecker84 - FactoryID: {factoryID}, Date: {dateF}");

                // Query database...
                var Checker84List = GetChecker84Data(factoryID, dateF, Unit ?? "",Line ?? "", Search_txt ?? "", ALL ?? "");

                return Ok(new { BC_DiChuyen_Search = Checker84List });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error in SearchChecker84");
                return StatusCode(500, new { error = "Database error occurred" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchChecker84");
                return BadRequest(new { error = "An error occurred processing your request" });
            }
        }

        private List<object> GetChecker84Data(string factoryID, string dateF, string unit,string line, string searchTxt, string all)
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Json_RP_BC_Checker84_SUM", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@FactoryID", SqlDbType.NVarChar).Value = factoryID;
                cmd.Parameters.Add("@Date_F", SqlDbType.DateTime).Value = dateF;
                cmd.Parameters.Add("@Unit", SqlDbType.NVarChar).Value = unit;
                cmd.Parameters.Add("@Line", SqlDbType.NVarChar).Value = line;
                cmd.Parameters.Add("@Search", SqlDbType.NVarChar).Value = searchTxt;
                cmd.Parameters.Add("@ALL", SqlDbType.NVarChar).Value = all;

                conn.Open();

                // // Log SQL
                // string debugSql = $"EXEC {cmd.CommandText} ";
                // debugSql += string.Join(", ", cmd.Parameters.Cast<SqlParameter>()
                //     .Select(p =>
                //         $"{p.ParameterName} = " +
                //         (p.Value == null || p.Value == DBNull.Value
                //             ? "NULL"
                //             : $"N'{p.Value.ToString().Replace("'", "''")}'")
                //     ));
                // _logger.LogInformation("Debug SQL: {debugSql}", debugSql);


                using SqlDataReader dr = cmd.ExecuteReader();
                
                int i = 1;
                while (dr.Read())
                {
                    list.Add(new 
                    {
                        No = i,
                        Led = Functions.GetStringValue(dr, "Led_"),
                        LastUpdate = Functions.GetDateTimeValue(dr,"LastUpdate"),

                        MO = Functions.GetStringValue(dr, "MO"),
                        Sewer = Functions.GetStringValue(dr, "Sewer"),
                        Audit_Time = Functions.GetStringValue(dr, "Audit_Time").Trim(),
                        Color = Functions.GetStringValue(dr, "Color").Trim(),
                        Size = Functions.GetStringValue(dr, "Size").Trim(),
                        UserUpdate = Functions.GetStringValue(dr, "UserUpdate").Trim(),
                        Unit = Functions.GetStringValue(dr, "Unit"),
                        Line = Functions.GetStringValue(dr, "Line"),
                        Sup = Functions.GetStringValue(dr, "Sup").Trim(),
                       
                        QTY = Functions.GetIntValue(dr, "QTY"),
                        Check_QTY = Functions.GetIntValue(dr, "Check_QTY"),
                        Total_Fault_QTY = Functions.GetIntValue(dr, "Total_Fault_QTY"),
                        
                        Fault_Detail = Functions.GetStringValue(dr, "Fault_Detail"),
                        Operation = Functions.GetStringValue(dr, "Operation"),
                        Remark = Functions.GetStringValue(dr, "Remark"),
                        
                        
                        Re_Audit = Functions.GetBoolValue(dr, "Re_Audit"),
                        Re_Audit_Time = Functions.GetDateTimeValue(dr,"Re_Audit_Time"),
                        
                        cl_List = Functions.GetStringValue(dr, "cl_List"),
                        cl_Size = Functions.GetStringValue(dr, "cl_Size"),
                        RowColor_Set = Functions.GetStringValue(dr, "RowColor_Set"),
                        RowColor_V = Functions.GetStringValue(dr, "RowColor_V"),
                        RowClick_V = Functions.GetStringValue(dr, "RowClick_V")
                    });
                    i++;
                }
            }

            return list;
        }

    }
}



// ## **🧪 Test API:**

// GET api/feature_frm_BC_Checker84_Search?Code_G=f4cf3fe4b67244332eecec055742d449REG263ca1464c3480896c1b78a5eac5a6e971f0e3dad99908345f7439f8ffabdffc4_____DeviceName_____MAC_____LoginID_____2025-10-28&Search_txt=&Unit=2U01&Line=201S01&ALL=YES