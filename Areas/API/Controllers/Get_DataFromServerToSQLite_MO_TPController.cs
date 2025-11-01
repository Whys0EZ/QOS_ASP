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
    public class Get_DataFromServerToSQLite_MO_TPController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<Get_DataFromServerToSQLite_MO_TPController> _logger;

        public Get_DataFromServerToSQLite_MO_TPController(IConfiguration config, ILogger<Get_DataFromServerToSQLite_MO_TPController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }
        [HttpGet]
        public IActionResult Get_DataFromServerToSQLite_MO_TP(string? Code_G)
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
                string loginID = txt.Length > 3 ? txt[3] : "";
                string Mo = txt.Length > 4 ? txt[4] : "";


                // Decode Code_G
                string tmp1 = codeGs.Length >= 32 ? codeGs.Substring(0, 32) : "";
                string tmp2 = codeGs.Length > 32 ? codeGs.Substring(0, codeGs.Length - 32) : "";
                string tmp3 = tmp2.Length >= 32 ? tmp2.Substring(tmp2.Length - 32) : "";
                string tmp4 = tmp2.Length > 32 ? tmp2.Substring(32) : "";
                string tmp5 = tmp4.Length > 32 ? tmp4.Substring(0, tmp4.Length - 32) : "";
                string FactoryID = tmp5;
                FactoryID ="REG2";
                string facCode = _config.GetValue<string>("AppSettings:FactoryCode") ?? "";
                object response ;
                
                if (tmp1 == Functions.MD5Hash(facCode) && tmp3 == Functions.MD5Hash(FactoryID))
                {
                    
                    response = new
                    {
                        ETS_Data_MO_Info = GetETS_Data(FactoryID,Mo)
                    };
                }
                else {
                    response = new
                    {
                        ETS_Data_MO_Info = new List<object>()
                    };
                }

                return Ok(response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetETS_Data");
                return BadRequest(new { error = ex.Message });
            }
        }

        private List<object> GetETS_Data(string FactoryID, string MO)
        {
            List<object> list = new();
            string opMO = (MO.Length >= 9) ? MO.Substring(0, 9) : MO;
            string factoryID = (FactoryID.Length > 3) ? FactoryID.Substring(0, 3) : FactoryID ;
            string sql = @$"Select * from ETS_Data_MO_Infor where FactoryID like N'{factoryID}%' and MO like N'%{opMO}%'";

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new(sql, conn))
            {
               
                // cmd.Parameters.AddWithValue("@UserName", UserName);

                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        MO = dr["MO"]?.ToString() ?? "",
                        Size = dr["Size"]?.ToString() ?? "",
                        ColorCode = dr["ColorCode"]?.ToString() ?? "",
                        StyleCode = Convert.ToString(dr?["StyleCode"])?.Replace("'", " ") ?? "",
                        // Ship_mode = dr["Infor_07"]?.ToString()?.Trim().Replace("'", " ").Replace("&", "-") ?? "",
                    });
                }
            }

            return list;
        }

    }
}