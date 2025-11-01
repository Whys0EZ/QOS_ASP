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
    public class Get_DataFromServerToSQLite_ThongSo_TP_ItemListController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<Get_DataFromServerToSQLite_ThongSo_TP_ItemListController> _logger;

        public Get_DataFromServerToSQLite_ThongSo_TP_ItemListController(IConfiguration config, ILogger<Get_DataFromServerToSQLite_ThongSo_TP_ItemListController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }
        [HttpGet]
        public IActionResult Get_DataFromServerToSQLite_ThongSo_TP_ItemList(string? Code_G)
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
                string StyleName = txt.Length > 4 ? txt[4] : "";

                // Decode Code_G
                string tmp1 = codeGs.Length >= 32 ? codeGs.Substring(0, 32) : "";
                string tmp2 = codeGs.Length > 32 ? codeGs.Substring(0, codeGs.Length - 32) : "";
                string tmp3 = tmp2.Length >= 32 ? tmp2.Substring(tmp2.Length - 32) : "";
                string tmp4 = tmp2.Length > 32 ? tmp2.Substring(32) : "";
                string tmp5 = tmp4.Length > 32 ? tmp4.Substring(0, tmp4.Length - 32) : "";
                string FactoryID = tmp5;
                FactoryID ="REG2";

                string facCode = _config.GetValue<string>("AppSettings:FactoryCode") ?? "";

                 _logger.LogInformation($"GetCustomerData - facCode: {facCode}, factoryID: {FactoryID}");

                // ✅ Validate authentication (Đã XÓA || 2>1)
                if (tmp1 != Functions.MD5Hash(facCode) || tmp3 != Functions.MD5Hash(FactoryID))
                {
                    return Unauthorized(new { error = "Invalid factory or authentication failed" });
                }

                _logger.LogInformation($"GetCustomerData - FactoryID: {FactoryID}, Device: {myDeviceName}");

                
                object response ;
                
                if (tmp1 == Functions.MD5Hash(facCode) && tmp3 == Functions.MD5Hash(FactoryID))
                {
                    
                    response = new
                    {
                        SUM_Info = GetSUM_Info(FactoryID,StyleName),
                        Detail_Info = GetDetail_Info(FactoryID,StyleName),
                    };
                }
                else {
                    response = new
                    {
                        SUM_Info = new List<object>(),
                        Detail_Info = new List<object>()
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
        private List<object> GetSUM_Info(string FactoryID,string StyleName)
        {
            List<object> list = new();
            string sql = @$"Select TypeName,StyleName,MO,SMPL,Remark from Form8_ThongSo_TP_ItemList_SUM where FactoryID=N'{FactoryID}' and StyleName like N'%{StyleName}%' ";

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new(sql, conn))
            {
                // cmd.Parameters.AddWithValue("@FactoryID", FactoryID);

                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        TypeName = dr["TypeName"]?.ToString() ?? "",
                        StyleName = dr["StyleName"]?.ToString() ?? "",
                        MO = dr["MO"]?.ToString() ?? "",
                        SMPL = dr["SMPL"]?.ToString() ?? "",
                        Remark = dr["Remark"]?.ToString() ?? "",
                       
                    });
                }
            }

            return list;
        }
        private List<object> GetDetail_Info(string FactoryID, string StyleName)
        {
            List<object> list = new();
            string sql = @$"Select StyleName,Size,STT,Item_Name,Target_Value,TOL,Size_No from Form8_ThongSo_TP_ItemList_Detail where FactoryID=N'{FactoryID}' and StyleName like N'%{StyleName}%' ";

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new(sql, conn))
            {
                // cmd.Parameters.AddWithValue("@FactoryID", FactoryID);

                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        StyleName = dr["StyleName"]?.ToString() ?? "",
                        Size = dr["Size"]?.ToString() ?? "",
                        STT = dr["STT"]?.ToString() ?? "",
                        ItemName = dr["Item_Name"]?.ToString() ?? "",
                        Target_Value = dr["Target_Value"]?.ToString() ?? "",
                        Size_No = dr["Size_No"]?.ToString() ?? "",
                        TOL = dr["TOL"]?.ToString() ?? "",
                       
                    });
                }
            }

            return list;
        }
    }
}
