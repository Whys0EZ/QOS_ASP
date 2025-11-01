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
    public class FQC_feature_frm_FQC_SearchController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<FQC_feature_frm_FQC_SearchController> _logger;

        public FQC_feature_frm_FQC_SearchController(IConfiguration config, ILogger<FQC_feature_frm_FQC_SearchController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }

        [HttpGet]
        public IActionResult FQC_feature_frm_FQC_Search(
            string? Code_G, 
            string? Search_txt, 
            string? Unit)
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

                // _logger.LogInformation($"SearchFQC - FactoryID: {factoryID}, Date: {dateF}");

                // Query database...
                var FQCList = GetFQCData(factoryID, dateF, Unit ?? "", Search_txt ?? "");

                return Ok(new { FQC_Search = FQCList });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error in SearchFQC");
                return StatusCode(500, new { error = "Database error occurred" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchFQC");
                return BadRequest(new { error = "An error occurred processing your request" });
            }
        }

        private List<object> GetFQCData(string factoryID, string dateF, string unit, string searchTxt)
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Json_RP_FQC_Search", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@FactoryID", SqlDbType.NVarChar).Value = factoryID;
                cmd.Parameters.Add("@Date_F", SqlDbType.DateTime).Value = dateF;
                cmd.Parameters.Add("@Unit", SqlDbType.NVarChar).Value = unit;
                // cmd.Parameters.Add("@Line", SqlDbType.NVarChar).Value = line;
                cmd.Parameters.Add("@Search", SqlDbType.NVarChar).Value = searchTxt;
                // cmd.Parameters.Add("@ALL", SqlDbType.NVarChar).Value = all;

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
                        
                        RowColor_V = Functions.GetStringValue(dr, "RowColor_V"),
                        RowColor_Set = Functions.GetStringValue(dr, "RowColor_Set"),
                        RowClick_V = Functions.GetStringValue(dr, "RowClick_V"),
                
                        No = Functions.GetStringValue(dr, "No"),
                        LastUpdate = Functions.GetDateTimeValue(dr,"LastUpdate"),

                        ResultStatus = Functions.GetStringValue(dr, "ResultStatus"),
                        ModuleName = Functions.GetStringValue(dr, "ModuleName"),
                        Infor_01 = Functions.GetStringValue(dr, "Infor_01"),
                        Infor_02 = Functions.GetStringValue(dr, "Infor_02"),
                        Infor_03 = Functions.GetStringValue(dr, "Infor_03"),
                        Infor_04 = Functions.GetStringValue(dr, "Infor_04"),
                        Infor_05 = Functions.GetStringValue(dr, "Infor_05"),
                        Infor_06 = Functions.GetStringValue(dr, "Infor_06"),
                        Infor_07 = Functions.GetStringValue(dr, "Infor_07").Trim(),
                        Infor_08 = Functions.GetStringValue(dr, "Infor_08"),
                        Infor_09 = Functions.GetStringValue(dr, "Infor_09"),
                        Infor_10 = Functions.GetStringValue(dr, "Infor_10"),
                        Infor_11 = Functions.GetStringValue(dr, "Infor_11"),
                        cl_List = Functions.GetStringValue(dr, "cl_List"),
                        cl_Size = Functions.GetStringValue(dr, "cl_Size")
                        
                    });
                    i++;
                }
            }

            return list;
        }

    }
}



// ## **🧪 Test API:**

// GET api/FQC_feature_frm_FQC_Search?Code_G=f4cf3fe4b67244332eecec055742d449REG263ca1464c3480896c1b78a5eac5a6e971f0e3dad99908345f7439f8ffabdffc4_____DeviceName_____MAC_____LoginID_____2025-10-28&Search_txt=&Unit=2U01&Line=201S01&ALL=YES