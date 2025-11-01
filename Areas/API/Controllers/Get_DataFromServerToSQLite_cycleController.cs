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
    public class Get_DataFromServerToSQLite_cycleController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<Get_DataFromServerToSQLite_cycleController> _logger;

        public Get_DataFromServerToSQLite_cycleController(IConfiguration config, ILogger<Get_DataFromServerToSQLite_cycleController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }
        [HttpGet]
        public IActionResult Get_DataFromServerToSQLite_cycle(string? Code_G,string? LineList,string? ThongSo_BTP_TypeList,string? CustomerList,string? WorkStage_List,string? Size_List,string? SizeNo_List,string? ThongSo_BTP_ItemList, string? TRACKING_Module)
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

                // Decode Code_G
                string tmp1 = codeGs.Length >= 32 ? codeGs.Substring(0, 32) : "";
                string tmp2 = codeGs.Length > 32 ? codeGs.Substring(0, codeGs.Length - 32) : "";
                string tmp3 = tmp2.Length >= 32 ? tmp2.Substring(tmp2.Length - 32) : "";
                string tmp4 = tmp2.Length > 32 ? tmp2.Substring(32) : "";
                string tmp5 = tmp4.Length > 32 ? tmp4.Substring(0, tmp4.Length - 32) : "";
                string factoryID = tmp5;

                string facCode = _config.GetValue<string>("AppSettings:FactoryCode") ?? "";

                 _logger.LogInformation($"GetCustomerData - facCode: {facCode}, factoryID: {factoryID}");

                // ✅ Validate authentication (Đã XÓA || 2>1)
                if (tmp1 != MD5Hash(facCode) && tmp3 != MD5Hash(factoryID))
                {
                    return Unauthorized(new { error = "Invalid factory or authentication failed" });
                }

                _logger.LogInformation($"GetCustomerData - FactoryID: {factoryID}, Device: {myDeviceName}");
                // if (tmp1 == MD5Hash(facCode) && tmp3 == MD5Hash(factoryID))
                // {
                var response = new
                {
                    SizeNo_List =GetSizeNo_List(),
                    CustomerList = GetCustomerList(factoryID),
                    ThongSo_BTP_TypeList = GetTypeList(factoryID),
                    Unit_Line_List = GetUniLineList(factoryID,loginID),
                    WorkStage_List = GetWorkStage_List(),
                    Size_List = GetSize_List(),
                    
                    ThongSo_BTP_ItemList = GetThongSo_BTP_ItemList(factoryID),
                    TRACKING_Module =GetTRACKING_Module(),

                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCustomerData");
                return BadRequest(new { error = ex.Message });
            }
        }
                // ------------------------- Size No List

        private List<object> GetSizeNo_List()
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select * from SizeNo_List order by SizeNo", conn))
            {
                

                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        SizeNo = dr["SizeNo"]?.ToString() ?? "",
                        
                       
                    });
                }
            }

            return list;
        }

        //---------------------------Customer
        private List<object> GetCustomerList(string factoryID)
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Json_Get_Customer_Infor", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FactoryID", factoryID);

                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        CustomerName = dr["CustomerName"]?.ToString() ?? ""
                    });
                }
            }

            return list;
        }

        // ------------------------------Type
        private List<object> GetTypeList(string factoryID)
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Json_frm_ThongSo_BTP_Get_Type", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FactoryID", factoryID);

                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        
                        TypeName = dr["TypeName"]?.ToString() ?? ""
                    });
                }
            }

            return list;
        }

        // ------------------------- Unit - Line
        private List<object> GetUniLineList(string factoryID ,string loginID)
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Json_frm_ThongSo_BTP_Get_Unit_Line", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FactoryID", factoryID);
                cmd.Parameters.AddWithValue("@UserName", loginID);

                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        Unit = dr["Unit"]?.ToString() ?? "",
                        Line = dr["Line"]?.ToString() ?? "",
                       
                    });
                }
            }

            return list;
        }

        // ------------------------- WorkStage
        private List<object> GetWorkStage_List()
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select * from WorkStage_List", conn))
            {
                
                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        FormID = dr["FormID"]?.ToString() ?? "",
                        STT = dr["STT"]?.ToString() ?? "",
                        WorkstageName = dr["WorkstageName"]?.ToString() ?? ""
                       
                    });
                }
            }

            return list;
        }

        // ------------------------- Size List
        private List<object> GetSize_List()
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select * from Size_List order by STT", conn))
            {
                

                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        STT = dr["STT"]?.ToString() ?? "",
                        Size = dr["Size"]?.ToString() ?? "",
                       
                    });
                }
            }

            return list;
        }



        // ------------------------- ThongSo_BTP_ItemList
        private List<object> GetThongSo_BTP_ItemList(string FactoryID)
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select TypeName,STT,VN_Name + case when EN_Name is NULL then '' else  '/' +  EN_Name end as ItemName,MO from Form7_ThongSo_BTP_ItemList where FactoryID=@FactoryID and Sync_Flag=1 order by TypeName,STT", conn))
            {
                cmd.Parameters.AddWithValue("@FactoryID", FactoryID);

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

        // ------------------------- TRACKING_Module
        private List<object> GetTRACKING_Module()
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select ModuleName from TRACKING_Module ", conn))
            {
               
                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        ModuleName = dr["ModuleName"]?.ToString() ?? "",
                     
                    });
                }
            }
            return list;
        }

        private static string MD5Hash(string input)
        {
            using var md5 = MD5.Create();
            byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }
}