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
    public class Get_DataFromServerToSQLite_Fault_CodeController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<Get_DataFromServerToSQLite_Fault_CodeController> _logger;

        public Get_DataFromServerToSQLite_Fault_CodeController(IConfiguration config, ILogger<Get_DataFromServerToSQLite_Fault_CodeController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }

        [HttpGet]
        public IActionResult Get_DataFromServerToSQLite_Fault_Code(string? Code_G)
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
                _logger.LogInformation($"Fault_Code - myDeviceName: {myDeviceName}, factoryID: {factoryID} Type_c: {Type_c}");
                
                Type_c =Type_c ?? "Form4_Active";

                _logger.LogInformation($"Fault_Code - myDeviceName: {myDeviceName}, Type_c: {Type_c}");

                var response = new
                {
                    array_Fault_Code = Fault_Code(factoryID,Type_c)
                };
                return Ok(response);
            }
            
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchBCCPI");
                return BadRequest(new { error = "An error occurred processing your request" });
            }
        }
        private List<object> Fault_Code(string FactoryID, string Type_c)
        {
            List<object> list = new();
            string sql = $@"
                    SELECT 
                        Fault_Code,
                        ISNULL(Fault_Type, '') AS Fault_Type,
                        ISNULL(Fault_Level, '1') AS Fault_Level,
                        ISNULL(Fault_Name_VN, '') AS Fault_Name_VN,
                        ISNULL(Fault_Name_EN, '') AS Fault_Name_EN,
                        CASE WHEN Form1_Active = 1 THEN 1 ELSE 0 END AS Form1_Active,
                        CASE WHEN Form2_Active = 1 THEN 1 ELSE 0 END AS Form2_Active,
                        CASE WHEN Form3_Active = 1 THEN 1 ELSE 0 END AS Form3_Active,
                        CASE WHEN Form4_Active = 1 THEN 1 ELSE 0 END AS Form4_Active,
                        CASE WHEN Form5_Active = 1 THEN 1 ELSE 0 END AS Form5_Active,
                        CASE WHEN Form6_Active = 1 THEN 1 ELSE 0 END AS Form6_Active
                    FROM Fault_Code
                    WHERE {Type_c} = 1
                    AND Fault_Code IS NOT NULL
                    AND LEN(Fault_Code) > 0
                    AND Factory = @FactoryID
                    ORDER BY Fault_Level, Fault_Code, Fault_Name_VN";

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new(sql, conn))
   
            // using (SqlCommand cmd = new(@"SELECT 
            //                                 Fault_Code,
            //                                 ISNULL(Fault_Type, '') AS Fault_Type,
            //                                 ISNULL(Fault_Level, '1') AS Fault_Level,
            //                                 ISNULL(Fault_Name_VN, '') AS Fault_Name_VN,
            //                                 ISNULL(Fault_Name_EN, '') AS Fault_Name_EN,
            //                                 CASE WHEN Form1_Active = 1 THEN 1 ELSE 0 END AS Form1_Active,
            //                                 CASE WHEN Form2_Active = 1 THEN 1 ELSE 0 END AS Form2_Active,
            //                                 CASE WHEN Form3_Active = 1 THEN 1 ELSE 0 END AS Form3_Active,
            //                                 CASE WHEN Form4_Active = 1 THEN 1 ELSE 0 END AS Form4_Active,
            //                                 CASE WHEN Form5_Active = 1 THEN 1 ELSE 0 END AS Form5_Active,
            //                                 CASE WHEN Form6_Active = 1 THEN 1 ELSE 0 END AS Form6_Active
            //                             FROM Fault_Code
            //                             WHERE 
            //                                 @Type_c = '1'
            //                                 AND Fault_Code IS NOT NULL
            //                                 AND LEN(Fault_Code) > 0
            //                                 AND Factory = @FactoryID
            //                             ORDER BY Fault_Level, Fault_Code, Fault_Name_VN", conn))
            {
                cmd.Parameters.AddWithValue("@Type_c", Type_c);
                cmd.Parameters.AddWithValue("@FactoryID", FactoryID);
                
                //  string debugSql = cmd.CommandText;
                // foreach (SqlParameter p in cmd.Parameters)
                // {
                //     string val = p.Value == null || p.Value == DBNull.Value
                //         ? "NULL"
                //         : $"N'{p.Value.ToString().Replace("'", "''")}'";
                //     debugSql = debugSql.Replace(p.ParameterName, val);
                // }

                // _logger.LogInformation("=== Debug SQL ===\n{sql}", debugSql);

                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        Fault_Code = dr["Fault_Code"]?.ToString() ?? "",
                        Fault_Type = dr["Fault_Type"]?.ToString() ?? "",
                        Fault_Level = dr["Fault_Level"]?.ToString() ?? "",
                        Fault_Name_VN = dr["Fault_Name_VN"]?.ToString() ?? "",
                        Fault_Name_EN = dr["Fault_Name_EN"]?.ToString() ?? "",
                        Form1_Active = dr["Form1_Active"]?.ToString() ?? "",
                        Form2_Active = dr["Form2_Active"]?.ToString() ?? "",
                        Form3_Active = dr["Form3_Active"]?.ToString() ?? "",
                        Form4_Active = dr["Form4_Active"]?.ToString() ?? "",
                        Form5_Active = dr["Form5_Active"]?.ToString() ?? "",
                        Form6_Active = dr["Form6_Active"]?.ToString() ?? "",

                    });
                }
            }

            return list;
        }

    }
}


// GET /api/Get_DataFromServerToSQLite_Fault_Code?Code_G=f4cf3fe4b67244332eecec055742d449REG263ca1464c3480896c1b78a5eac5a6e97e2ef524fbf3d9fe611d5a8e90fefdc9c_____LenovoTB-8504X_____d0:f8:8c:ea:e9:cc_____Form1_Active

