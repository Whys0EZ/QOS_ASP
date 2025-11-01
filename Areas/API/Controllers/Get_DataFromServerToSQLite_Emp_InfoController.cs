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
    public class Get_DataFromServerToSQLite_Emp_InfoController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<Get_DataFromServerToSQLite_Emp_InfoController> _logger;

        public Get_DataFromServerToSQLite_Emp_InfoController(IConfiguration config, ILogger<Get_DataFromServerToSQLite_Emp_InfoController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }
        [HttpGet]
        public IActionResult Get_DataFromServerToSQLite_Emp_Info(string? Code_G)
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
                string CardNo = txt.Length > 3 ? Functions.Cut_Zero(txt[3]) : "";
                string Type_c = txt.Length > 4 ? txt[4] : "Card_No";


                // Decode Code_G
                string tmp1 = codeGs.Length >= 32 ? codeGs.Substring(0, 32) : "";
                string tmp2 = codeGs.Length > 32 ? codeGs.Substring(0, codeGs.Length - 32) : "";
                string tmp3 = tmp2.Length >= 32 ? tmp2.Substring(tmp2.Length - 32) : "";
                string tmp4 = tmp2.Length > 32 ? tmp2.Substring(32) : "";
                string tmp5 = tmp4.Length > 32 ? tmp4.Substring(0, tmp4.Length - 32) : "";
                string FactoryID = tmp5;
                string facCode = _config.GetValue<string>("AppSettings:FactoryCode") ?? "";
                object response ;
                // _logger.LogInformation(" Get Emp_Infor: CardNo " + CardNo + "  Type_c: " + Type_c + "  FactoryID : " + FactoryID );
                if (tmp1 == Functions.MD5Hash(facCode) && tmp3 == Functions.MD5Hash(FactoryID) && CardNo !="")
                {
                    
                    response = new
                    {
                        Emp_Infor = Emp_Infor(FactoryID)
                    };
                }
                else {
                    response = new
                    {
                        Emp_Infor = new List<object>()
                    };
                }

                return Ok(response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Emp_Infor");
                return BadRequest(new { error = ex.Message });
            }
        }
        private List<object> Emp_Infor(string FactoryID)
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Json_Get_Emp_Infor_all", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
               
                cmd.Parameters.AddWithValue("@FactoryID", FactoryID);
               

                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        
                        Emp_No = dr["Emp_No"]?.ToString() ?? "",
                        FullName = dr["FullName"]?.ToString() ?? "",
                        Card_No = dr["Card_No"]?.ToString() ?? "",
                    });
                }
            }

            return list;
        }
    }
}