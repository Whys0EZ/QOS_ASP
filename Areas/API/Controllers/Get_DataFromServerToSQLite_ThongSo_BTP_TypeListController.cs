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
    public class Get_DataFromServerToSQLite_ThongSo_BTP_TypeListController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<Get_DataFromServerToSQLite_ThongSo_BTP_TypeListController> _logger;

        public Get_DataFromServerToSQLite_ThongSo_BTP_TypeListController(IConfiguration config, ILogger<Get_DataFromServerToSQLite_ThongSo_BTP_TypeListController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }
        [HttpGet]
        public IActionResult Get_DataFromServerToSQLite_ThongSo_BTP_TypeList(string? Code_G)
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
                // string MO = txt.Length > 4 ? txt[4] : "";


                // Decode Code_G
                string tmp1 = codeGs.Length >= 32 ? codeGs.Substring(0, 32) : "";
                string tmp2 = codeGs.Length > 32 ? codeGs.Substring(0, codeGs.Length - 32) : "";
                string tmp3 = tmp2.Length >= 32 ? tmp2.Substring(tmp2.Length - 32) : "";
                string tmp4 = tmp2.Length > 32 ? tmp2.Substring(32) : "";
                string tmp5 = tmp4.Length > 32 ? tmp4.Substring(0, tmp4.Length - 32) : "";
                string FactoryID = tmp5;
                FactoryID = "REG2";

                string facCode = _config.GetValue<string>("AppSettings:FactoryCode") ?? "";
                object response ;
                _logger.LogInformation(" Get ThongSo_BTP_ItemList:  FactoryID : " + FactoryID );
                if (tmp1 == Functions.MD5Hash(facCode) && tmp3 == Functions.MD5Hash(FactoryID))
                {
                    
                    response = new
                    {
                        ThongSo_BTP_ItemList = ThongSo_BTP_ItemList( FactoryID)
                    };
                }
                else {
                    response = new
                    {
                        ThongSo_BTP_ItemList = new List<object>()
                    };
                }

                return Ok(response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ThongSo_BTP_ItemList");
                return BadRequest(new { error = ex.Message });
            }
        }
        private List<object> ThongSo_BTP_ItemList( string FactoryID)
        {
            // string factoryID = FactoryID.Length > 3 ? FactoryID.Substring(0, 3) : FactoryID;
            // string opMO = (MO.Length > 8) ? MO.Substring(0, MO.Length - 3) : MO;
            List<object> list = new();
            // string sql = $@"Select * from ETS_Data_MO_Infor where FactoryID like N'{factoryID}%' and MO like N'%{opMO}%' ";
            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select TypeName,STT,VN_Name + case when EN_Name is NULL then '' else  '/' +  EN_Name end as ItemName,MO from Form7_ThongSo_BTP_ItemList where FactoryID=@FactoryID and Sync_Flag=1 order by TypeName,STT", conn))
            {
                
               
                cmd.Parameters.AddWithValue("@FactoryID", FactoryID);
                // cmd.Parameters.AddWithValue("@MO", MO);
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
                        
                        TypeName = dr["TypeName"]?.ToString() ?? "",
                        STT = dr["STT"]?.ToString() ?? "",
                        ItemName = dr["ItemName"]?.ToString() ?? "",
                        MO = dr["MO"]?.ToString() ?? "",
                    });
                }
            }

            return list;
        }
    }
}