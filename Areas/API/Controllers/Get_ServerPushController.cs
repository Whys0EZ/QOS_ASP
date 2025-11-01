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
    public class Get_ServerPushController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<Get_ServerPushController> _logger;

        public Get_ServerPushController(IConfiguration config, ILogger<Get_ServerPushController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }
        [HttpGet]
        public IActionResult Get_ServerPush(string? Code_G)
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
                // string Mo = txt.Length > 4 ? txt[4] : "";


                // Decode Code_G
                string tmp1 = codeGs.Length >= 32 ? codeGs.Substring(0, 32) : "";
                string tmp2 = codeGs.Length > 32 ? codeGs.Substring(0, codeGs.Length - 32) : "";
                string tmp3 = tmp2.Length >= 32 ? tmp2.Substring(tmp2.Length - 32) : "";
                string tmp4 = tmp2.Length > 32 ? tmp2.Substring(32) : "";
                string tmp5 = tmp4.Length > 32 ? tmp4.Substring(0, tmp4.Length - 32) : "";
                string FactoryID = tmp5;

                var response = new
                {
                    System_Push = System_Push(FactoryID)
                };
                return Ok(response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCustomerData");
                return BadRequest(new { error = ex.Message });
            }
        }

        private List<object> System_Push(string FactoryID)
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select top 1 * from System_Push where FactoyID= = @FactoryID  ", conn))
            {
               
                cmd.Parameters.AddWithValue("@FactoryID", FactoryID);

                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        DB_Ver = dr["DB_Ver"]?.ToString() ?? "",
                        App_Ver = dr["App_Ver"]?.ToString() ?? "",
                        LineList = dr["LineList"]?.ToString() ?? "",
                        ThongSo_BTP_TypeList = dr["ThongSo_BTP_TypeList"]?.ToString() ?? "",
                        CustomerList = dr["CustomerList"]?.ToString() ?? "",
                        WorkStage_List = dr["WorkStage_List"]?.ToString() ?? "",
                        Size_List = dr["Size_List"]?.ToString() ?? "",
                        SizeNo_List = dr["SizeNo_List"]?.ToString() ?? "",
                        ThongSo_BTP_ItemList = dr["ThongSo_BTP_ItemList"]?.ToString() ?? "",
                    });
                }
            }

            return list;
        }

    }
}