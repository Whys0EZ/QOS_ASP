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
    public class Get_SO_Infor_From_FQCController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<Get_SO_Infor_From_FQCController> _logger;

        public Get_SO_Infor_From_FQCController(IConfiguration config, ILogger<Get_SO_Infor_From_FQCController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }
        [HttpGet]
        [HttpGet("[action]")]
        public IActionResult Get_SO_Infor_From_FQC(string? Code_G)
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
                string MO = txt.Length > 3 ? txt[3] : "";
               


                // Decode Code_G
                string tmp1 = codeGs.Length >= 32 ? codeGs.Substring(0, 32) : "";
                string tmp2 = codeGs.Length > 32 ? codeGs.Substring(0, codeGs.Length - 32) : "";
                string tmp3 = tmp2.Length >= 32 ? tmp2.Substring(tmp2.Length - 32) : "";
                string tmp4 = tmp2.Length > 32 ? tmp2.Substring(32) : "";
                string tmp5 = tmp4.Length > 32 ? tmp4.Substring(0, tmp4.Length - 32) : "";
                string FactoryID = "REG2";

                string facCode = _config.GetValue<string>("AppSettings:FactoryCode") ?? "";
                object response ;
                // _logger.LogInformation(" Get Get_SO_Infor_From_FQC: MO " + MO + "  FactoryID : " + FactoryID );
                    
                if (tmp1 == Functions.MD5Hash(facCode) && tmp3 == Functions.MD5Hash(FactoryID))
                {
                    
                    response = new
                    {
                        Data_SO_Info = Data_SO_Info(MO)
                    };
                }
                else {
                    response = new
                    {
                        Data_SO_Info = new List<object>()
                    };
                }
            

                return Ok(response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Data_SO_Info");
                return BadRequest(new { error = ex.Message });
            }
        }
        private List<object> Data_SO_Info(string MO)
        {
            // string opMO = (MO.Length >= 9) ? MO.Substring(0, 9) : MO;
            List<object> list = new();
            
            string sql = $@"Select * from TRACKINIG_UploadData where  Infor_01 = '{MO}'  ";
            
            
            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new(sql, conn))
            {
                
                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        
                        Infor_01 = dr["Infor_01"]?.ToString() ?? "",
                        Infor_02 = dr["Infor_02"]?.ToString() ?? "",
                        Infor_03 = dr["Infor_03"]?.ToString()?.Trim().Replace("'", " ").Replace("&", "-") ?? "",
                        Infor_04 = dr["Infor_04"]?.ToString() ?? "",
                        Infor_05 = dr["Infor_05"]?.ToString() ?? "",
                        Infor_06 = dr["Infor_06"]?.ToString() ?? "",
                        Infor_07 = dr["Infor_07"]?.ToString()?.Trim().Replace("'", " ").Replace("&", "-") ?? "",
                        
                    });
                }
            }

            return list;
        }
    }
}