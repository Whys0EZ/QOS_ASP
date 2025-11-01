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
    public class Get_MO_Infor_From_ETS_TagController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<Get_MO_Infor_From_ETS_TagController> _logger;

        public Get_MO_Infor_From_ETS_TagController(IConfiguration config, ILogger<Get_MO_Infor_From_ETS_TagController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }
        [HttpGet]
        public IActionResult Get_MO_Infor_From_ETS_Tag(string? Code_G)
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
                string Unit = txt.Length > 4 ? txt[4] : "";


                // Decode Code_G
                string tmp1 = codeGs.Length >= 32 ? codeGs.Substring(0, 32) : "";
                string tmp2 = codeGs.Length > 32 ? codeGs.Substring(0, codeGs.Length - 32) : "";
                string tmp3 = tmp2.Length >= 32 ? tmp2.Substring(tmp2.Length - 32) : "";
                string tmp4 = tmp2.Length > 32 ? tmp2.Substring(32) : "";
                string tmp5 = tmp4.Length > 32 ? tmp4.Substring(0, tmp4.Length - 32) : "";
                string FactoryID = tmp5;

                _logger.LogInformation(" Get MO_Infor: CardNo " + CardNo + "  Unit: " + Unit + "  FactoryID : " + FactoryID );
                var response = new
                {
                    MO_Infor = MO_Infor(CardNo, Unit, FactoryID)
                };
                return Ok(response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCustomerData");
                return BadRequest(new { error = ex.Message });
            }
        }

        private List<object> MO_Infor(string CardNo , string Unit, string FactoryID)
        {
            List<object> list = new();
            int i = 0;

            if(int.TryParse(CardNo, out _))
            {
                using (SqlConnection conn = new(_connectionString))
                using (SqlCommand cmd = new("select top 1 * from ETS_Data_TagList where CardNo = @CardNo  ", conn))
                {
                
                    cmd.Parameters.AddWithValue("@CardNo", CardNo);

                    conn.Open();
                    using SqlDataReader dr = cmd.ExecuteReader();
                    
                    while (dr.Read())
                    {
                        list.Add(new
                        {
                            MO = dr["MO"]?.ToString() ?? "",
                            SizeCode = dr["SizeCode"]?.ToString() ?? "",
                            ColorCode = dr["ColorCode"]?.ToString() ?? "",
                            StyleCode = dr["StyleCode"]?.ToString() ?? "",
                            Lot_Batch = dr["Lot_Batch"]?.ToString() ?? "",
                            Qty = dr["Qty"]?.ToString() ?? "",
                        });
                        i ++;
                    }
                }

                if(i == 0)
                {
                    using (SqlConnection conn = new(_connectionString))
                    using (SqlCommand cmd = new("Json_Get_MO_Infor_from_ETS_SUB_Card", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@FactoryID", FactoryID);
                        cmd.Parameters.AddWithValue("@Unit", Unit);
                        cmd.Parameters.AddWithValue("@CardNo", CardNo);

                        conn.Open();
                        using SqlDataReader dr = cmd.ExecuteReader();
                        
                        while (dr.Read())
                        {
                            list.Add(new
                            {
                                MO = dr["MO"]?.ToString() ?? "",
                                SizeCode = dr["SizeCode"]?.ToString() ?? "",
                                ColorCode = dr["ColorCode"]?.ToString() ?? "",
                                StyleCode = dr["StyleCode"]?.ToString() ?? "",
                                Lot_Batch = dr["Lot_Batch"]?.ToString() ?? "",
                                CutQty = dr["CutQty"]?.ToString() ?? "",
                            });
                           
                        }
                    }
                }
            }
            else
            {
                _logger.LogInformation(" Get MO_Infor else: CardNo " + CardNo + "  Unit: " + Unit + "  FactoryID : " + FactoryID );
                using (SqlConnection conn = new(_connectionString))
                using (SqlCommand cmd = new("select  top 1 * from ETS_Data_MO_Infor where MO= @CardNo  ", conn))
                {
                
                    cmd.Parameters.AddWithValue("@CardNo", CardNo);

                    conn.Open();
                    using SqlDataReader dr = cmd.ExecuteReader();
                    
                    while (dr.Read())
                    {
                        list.Add(new
                        {
                            MO = dr["MO"]?.ToString() ?? "",
                            SizeCode = dr["SizeCode"]?.ToString() ?? "",
                            ColorCode = dr["ColorCode"]?.ToString() ?? "",
                            StyleCode = dr["StyleCode"]?.ToString() ?? "",
                            Lot_Batch =  "",
                            Qty =  "",
                        });
                    }
                }

            }

            return list;
        }

        private string Cut_Zero(string a)
        {
            if (string.IsNullOrEmpty(a))
                return "";

            // Bỏ các ký tự '0' ở đầu chuỗi
            int i = 0;
            while (i < a.Length && a[i] == '0')
                i++;

            // Nếu toàn 0, trả "0" thay vì rỗng
            return i == a.Length ? "0" : a.Substring(i);
        }

    }
}