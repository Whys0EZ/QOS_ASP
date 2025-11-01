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
    public class FQC_Get_PO_InforController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<FQC_Get_PO_InforController> _logger;

        public FQC_Get_PO_InforController(IConfiguration config, ILogger<FQC_Get_PO_InforController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }
        [HttpGet]
        [HttpGet("[action]")]
        public IActionResult FQC_Get_PO_Infor(string? Code_G)
        {
            if (string.IsNullOrEmpty(Code_G))
                return BadRequest(new { error = "Code_G is required" });

            try
            {
                // Parse Code_G - Dùng "_____" (5 gạch dưới) giống PHP
                string[] txt = Code_G.Split("_____");
                
                string firstValue = txt.Length > 0 ? txt[0] : "";
                string secondValue = txt.Length > 1 ? txt[1] : "";
                string thirdValue = txt.Length > 2 ? txt[2] : "";
                string fourthValue = txt.Length > 3 ? txt[3] : "";
               


                // Decode Code_G
                // string tmp1 = codeGs.Length >= 32 ? codeGs.Substring(0, 32) : "";
                // string tmp2 = codeGs.Length > 32 ? codeGs.Substring(0, codeGs.Length - 32) : "";
                // string tmp3 = tmp2.Length >= 32 ? tmp2.Substring(tmp2.Length - 32) : "";
                // string tmp4 = tmp2.Length > 32 ? tmp2.Substring(32) : "";
                // string tmp5 = tmp4.Length > 32 ? tmp4.Substring(0, tmp4.Length - 32) : "";
                // string FactoryID = "REG2";

                string facCode = _config.GetValue<string>("AppSettings:FactoryCode") ?? "";
                object response ;
                // _logger.LogInformation(" Get FQC_Get_PO_Infor: MO " + MO + "  FactoryID : " + FactoryID );
                    
                response = new
                {
                    Get_Data_PO_Info = Get_Data_PO_Info(firstValue, secondValue ,thirdValue,fourthValue)
                };
            

                return Ok(response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Get_Data_PO_Info");
                return BadRequest(new { error = ex.Message });
            }
        }
        private List<object> Get_Data_PO_Info(string firstValue,string secondValue , string thirdValue,string fourthValue)
        {
            string sql ="";
            List<object> list = new();
            if(fourthValue != "")
            {
                 sql = $@"Select * from TRACKINIG_UploadData where  Infor_01 = '{firstValue}'   and Infor_02 =  '{secondValue}'  and Infor_04 =  '{thirdValue}'  and Infor_03 = '{fourthValue}' ";
            }
            else
            {
                sql = $@"Select * from TRACKINIG_UploadData where  Infor_01 = '{firstValue}'   and Infor_02 =  '{secondValue}'  and Infor_04 =  '{thirdValue}' ";
            }
            
            // string sql = $@"Select * from Get_Data_PO_Infor where FactoryID like N'{factoryID}%' and MO like N'%{opMO}%' ";
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
                        Item = dr["Infor_03"]?.ToString()?.Trim().Replace("'", " ").Replace("&", "-") ?? "",
                        Customer = dr["Infor_10"]?.ToString() ?? "",
                        Ship_mode = dr["Infor_07"]?.ToString()?.Trim().Replace("'", " ").Replace("&", "-") ?? "",
                        ID = dr["ID"]?.ToString() ?? "",
                        
                    });
                }
            }

            return list;
        }
    }
}