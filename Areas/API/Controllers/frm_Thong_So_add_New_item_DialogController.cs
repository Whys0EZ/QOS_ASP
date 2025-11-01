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
    public class frm_Thong_So_add_New_item_DialogController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<frm_Thong_So_add_New_item_DialogController> _logger;

        public frm_Thong_So_add_New_item_DialogController(IConfiguration config, ILogger<frm_Thong_So_add_New_item_DialogController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }
        [HttpGet]
        public IActionResult frm_Thong_So_add_New_item_Dialog(string? Code_G,string? QueryData)
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
                string mType = txt.Length > 5 ? txt[5] : "";


                // Decode Code_G
                string tmp1 = codeGs.Length >= 32 ? codeGs.Substring(0, 32) : "";
                string tmp2 = codeGs.Length > 32 ? codeGs.Substring(0, codeGs.Length - 32) : "";
                string tmp3 = tmp2.Length >= 32 ? tmp2.Substring(tmp2.Length - 32) : "";
                string tmp4 = tmp2.Length > 32 ? tmp2.Substring(32) : "";
                string tmp5 = tmp4.Length > 32 ? tmp4.Substring(0, tmp4.Length - 32) : "";
                string FactoryID = tmp5;
                string facCode = _config.GetValue<string>("AppSettings:FactoryCode") ?? "";
                object response ;
                
                if (tmp1 == Functions.MD5Hash(facCode) && tmp3 == Functions.MD5Hash(FactoryID))
                {
                    
                    response = new
                    {
                        feature = ThongSo_BTP(FactoryID,loginID,mType,Mo,QueryData)
                    };
                }
                else {
                    response = new
                    {
                        feature = new List<object>()
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

        private List<object> ThongSo_BTP(string? FactoryID,string? loginID,string? mType,string? Mo,string? QueryData)
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("CreateNewItem_ThongSo_BTP ", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FactoryID", FactoryID);
                cmd.Parameters.AddWithValue("@UserName", loginID);
                cmd.Parameters.AddWithValue("@TypeName", mType);
                cmd.Parameters.AddWithValue("@MO", Mo);
                cmd.Parameters.AddWithValue("@ItemName", QueryData);

                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        
                        STT = dr["STT"]?.ToString() ?? "",
                        ItemName = dr["ItemName"]?.ToString() ?? "",
                    });
                }
            }
            // Nếu không có dữ liệu nào => thêm dòng NG
                if (list.Count == 0)
                {
                    list.Add(new { STT = "0", ItemName = "NG" });
                }

            return list;
        }

    }
}