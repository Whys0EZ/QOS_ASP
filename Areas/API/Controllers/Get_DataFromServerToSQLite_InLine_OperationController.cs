using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace QOS.Areas.API.Controllers
{
    [Area("API")]
    [Route("api/[controller]")]
    [ApiController]
    public class Get_DataFromServerToSQLite_InLine_OperationController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<Get_DataFromServerToSQLite_InLine_OperationController> _logger;

        public Get_DataFromServerToSQLite_InLine_OperationController(IConfiguration config, ILogger<Get_DataFromServerToSQLite_InLine_OperationController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }
        [HttpGet]
        public IActionResult Get_DataFromServerToSQLite_InLine_Operation(string? Code_G)
        {
            if (string.IsNullOrEmpty(Code_G))
                return BadRequest(new { error = "Code_G is required" });

            try
            {
                // Parse Code_G - Dùng "_____" (5 gạch dưới) giống PHP
                string[] txt = Code_G.Split("_____");
                
                string codeGs = txt.Length > 0 ? txt[0] : "";
                string myDeviceName = txt.Length > 1 ? txt[1] : "";
                string myMAC = txt.Length > 2 ? txt[2] : "";
                string LoginID = txt.Length > 3 ? txt[3] : "";
                string MO = txt.Length > 4 ? txt[4] : "";


                // Decode Code_G
                string tmp1 = codeGs.Length >= 32 ? codeGs.Substring(0, 32) : "";
                string tmp2 = codeGs.Length > 32 ? codeGs.Substring(0, codeGs.Length - 32) : "";
                string tmp3 = tmp2.Length >= 32 ? tmp2.Substring(tmp2.Length - 32) : "";
                string tmp4 = tmp2.Length > 32 ? tmp2.Substring(32) : "";
                string tmp5 = tmp4.Length > 32 ? tmp4.Substring(0, tmp4.Length - 32) : "";
                string FactoryID = tmp5;
                FactoryID = "REG2";

                _logger.LogInformation(" Get Get_DataFromServerToSQLite_InLine_Operation: LoginID " + LoginID + "  MO: " + MO + "  FactoryID : " + FactoryID );

                string facCode = _config.GetValue<string>("AppSettings:FactoryCode") ?? "";
                object response ;
                
                if (tmp1 == Functions.MD5Hash(facCode) && tmp3 == Functions.MD5Hash(FactoryID))
                {
                    
                    response = new
                    {
                        InLine_Operation = InLine_Operation(MO)
                    };
                }
                else {
                    response = new
                    {
                        InLine_Operation = new List<object>()
                    };
                }
                return Ok(response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCustomerData");
                return BadRequest(new { error = ex.Message });
            }
        }

        private List<object> InLine_Operation(string MO)
        {
            List<object> list = new();
            int i = 0;
            string opMO = (MO.Length >= 9) ? MO.Substring(0, 9) : MO;

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select isnull(MO,@MO) as MO,Operation_Code, Operation_Name_VN from Operation_Code where MO =@MO  AND Form4_Active=1 ORDER BY Operation_Code ASC", conn))
            {
            
                cmd.Parameters.AddWithValue("@MO", MO);
                // cmd.Parameters.AddWithValue("@opMO", opMO);

                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        MO = dr["MO"]?.ToString() ?? "",
                        Operation_Code = dr["Operation_Code"]?.ToString() ?? "",
                        Operation_Name = dr["Operation_Name_VN"]?.ToString() ?? "",
                        WorkDate = DateTime.Now.ToString("yyyy-MM-dd"),
                        WorkDate_del = DateTime.Now.Date.AddDays(-30).AddTicks(-1).ToString("dd/MM/yyyy"),
                    });
                    i ++;
                }
            }

            if(i < 1)
            {
                // string opMOs = (MO.Length >= 8) ? MO.Substring(0, 8) : MO;
                string sql = $@"Select isnull(MO,@MO) as MO,Operation_Code, Operation_Name_VN from Operation_Code where MO like N'%{opMO}%'  AND Form4_Active=1 ORDER BY Operation_Code ASC";
                using (SqlConnection conn = new(_connectionString))
                using (SqlCommand cmd = new(sql, conn))
                {
                 
                    cmd.Parameters.AddWithValue("@MO", MO);
                    // cmd.Parameters.AddWithValue("@opMO", opMO);
                    
                // string debugSql = cmd.CommandText;
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
                            MO = dr["MO"]?.ToString() ?? "",
                            Operation_Code = dr["Operation_Code"]?.ToString() ?? "",
                            Operation_Name = dr["Operation_Name_VN"]?.ToString() ?? "",
                            WorkDate = DateTime.Now.ToString("yyyy-MM-dd"),
                            WorkDate_del = DateTime.Now.Date.AddDays(-30).AddTicks(-1).ToString("dd/MM/yyyy"),
                        });
                        
                    }
                }
            }
            

            return list;
        }

    }
}

// /api/Get_DataFromServerToSQLite_InLine_Operation?Code_G=f4cf3fe4b67244332eecec055742d449REG263ca1464c3480896c1b78a5eac5a6e97e4da3b7fbbce2345d7772b0674a318d5_____LenovoTB-8504X_____d0:f8:8c:ea:e9:cc_____admin_____V2511411

