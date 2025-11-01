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
    public class feature_phanquyenuserController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<feature_phanquyenuserController> _logger;

        public feature_phanquyenuserController(IConfiguration config, ILogger<feature_phanquyenuserController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }
        [HttpGet]
        public IActionResult feature_phanquyenuser(string? Code_G)
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
                string UserName = tmp5;

                var response = new
                {
                    Phan_quyen = Phan_quyen(UserName)
                };
                return Ok(response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCustomerData");
                return BadRequest(new { error = ex.Message });
            }
        }

        private List<object> Phan_quyen(string UserName)
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("select UserName,B_F0,B_F01 from User_Per where UserName = @UserName  ", conn))
            {
               
                cmd.Parameters.AddWithValue("@UserName", UserName);

                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        B_F0 = (dr["B_F0"].ToString() == "True" || dr["B_F0"].ToString() == "1") ? "1" : "0",
                        B_F01 = (dr["B_F01"].ToString() == "True" || dr["B_F01"].ToString() == "1") ? "1" : "0",
                        UserName = dr["UserName"]?.ToString() ?? "",
                    });
                }
            }

            return list;
        }

    }
}