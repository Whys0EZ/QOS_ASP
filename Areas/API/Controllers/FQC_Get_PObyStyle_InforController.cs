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
    public class FQC_Get_PObyStyle_InforController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<FQC_Get_PObyStyle_InforController> _logger;

        public FQC_Get_PObyStyle_InforController(IConfiguration config, ILogger<FQC_Get_PObyStyle_InforController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }
        [HttpGet]
        [HttpGet("[action]")]
        public IActionResult FQC_Get_PObyStyle_Infor(string? Code_G)
        {
            if (string.IsNullOrEmpty(Code_G))
                return BadRequest(new { error = "Code_G is required" });

            try
            {
                // Parse Code_G - Dùng "_____" (5 gạch dưới) giống PHP
                string[] txt = Code_G.Split("_____");
                
                string firstValue = txt.Length > 0 ? txt[0] : "";
                string secondValue = txt.Length > 1 ? txt[1] : "";
                // string thirdValue = txt.Length > 2 ? txt[2] : "";
                // string fourthValue = txt.Length > 3 ? txt[3] : "";

                // string FactoryID = "REG2";

                string facCode = _config.GetValue<string>("AppSettings:FactoryCode") ?? "";
                object response ;
                // _logger.LogInformation(" Get FQC_Get_PObyStyle_Infor: MO " + MO + "  FactoryID : " + FactoryID );
                    
                response = new
                {
                    ETS_Data_PobyStyle_Info = ETS_Data_PobyStyle_Info(firstValue, secondValue)
                };
            

                return Ok(response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ETS_Data_PobyStyle_Info");
                return BadRequest(new { error = ex.Message });
            }
        }
        private List<object> ETS_Data_PobyStyle_Info(string firstValue,string secondValue)
        {
            string sql = $@"Select * from TRACKINIG_UploadData where  Infor_01 = '{firstValue}' and Infor_02 =  '{secondValue}' ";
            List<object> list = new();
            
            
            // string sql = $@"Select * from ETS_Data_PobyStyle_Infor where FactoryID like N'{factoryID}%' and MO like N'%{opMO}%' ";
            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new(sql, conn))
            {
                
                // cmd.Parameters.AddWithValue("@FactoryID", FactoryID);
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
                        
                        SO = dr["Infor_01"]?.ToString() ?? "",
                        Style = dr["Infor_02"]?.ToString() ?? "",
                        PO = dr["Infor_04"]?.ToString() ?? "",
                        Qty = dr["Infor_05"]?.ToString() ?? "",
                        Des = dr["Infor_08"]?.ToString() ?? "",
                        Pro = dr["Infor_09"]?.ToString() ?? "",
                        New_date = dr["Infor_06"]?.ToString() ?? "",
                       
                        Ship_mode = dr["Infor_07"]?.ToString()?.Trim().Replace("'", " ").Replace("&", "-") ?? "",
                        
                        
                    });
                }
            }

            return list;
        }
    }
}