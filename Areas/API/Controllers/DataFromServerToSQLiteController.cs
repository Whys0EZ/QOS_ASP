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
    public class DataFromServerToSQLiteController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<DataFromServerToSQLiteController> _logger;

        public DataFromServerToSQLiteController(IConfiguration config, ILogger<DataFromServerToSQLiteController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }
        [HttpGet]
        public IActionResult DataFromServerToSQLite(string? Code_G)
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
                if (tmp1 != MD5Hash(facCode) || tmp3 != MD5Hash(factoryID))
                {
                    return Unauthorized(new { error = "Invalid factory or authentication failed" });
                }

                _logger.LogInformation($"GetCustomerData - FactoryID: {factoryID}, Device: {myDeviceName}");

                var response = new
                {
                    CustomerList = GetCustomerList(factoryID),
                    ThongSo_BTP_TypeList = GetTypeList(factoryID),
                    Unit_Line_List = GetUniLineList(factoryID,loginID),
                    WorkStage_List = WorkStage_List(),
                    Size_List = Size_List(),
                    SizeNo_List =SizeNo_List(),
                    ThongSo_BTP_ItemList = ThongSo_BTP_ItemList(factoryID),
                    Option_Value =Option_Value(),
                    AQL =AQL(),
                    AQL_UQ = AQL_UQ(),
                    Dung_sai_inch = Dung_sai_inch(),
                    TRACKING_Module = TRACKING_Module(),
                    TRACKING_GroupContactList = TRACKING_GroupContactList(),
                    TRACKING_InforSetup_DataType = TRACKING_InforSetup_DataType(),
                    TRACKING_InforSetup_Index = TRACKING_InforSetup_Index(),
                    TRACKING_InforSetup_Name = TRACKING_InforSetup_Name(),
                    TRACKINIG_ResultSetup_Name = TRACKINIG_ResultSetup_Name(),
                    TRACKINIG_ResultSetup_Index = TRACKINIG_ResultSetup_Index(),
                    TRACKINIG_ResultSetup_SelectionData = TRACKINIG_ResultSetup_SelectionData(),
                    TRACKINIG_ResultSetup_DataType = TRACKINIG_ResultSetup_DataType(),
                    

                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCustomerData");
                return BadRequest(new { error = ex.Message });
            }
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
                        ID = dr["ID"]?.ToString() ?? "",
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
                        Factory = dr["Factory"]?.ToString() ?? "",
                        NO_Position = dr["NO_Position"]?.ToString() ?? "",
                        Unit2 = dr["Unit2"]?.ToString() ?? ""
                    });
                }
            }

            return list;
        }

        // ------------------------- WorkStage
        private List<object> WorkStage_List()
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
        private List<object> Size_List()
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

        // ------------------------- Size No List

        private List<object> SizeNo_List()
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

        // ------------------------- ThongSo_BTP_ItemList
        private List<object> ThongSo_BTP_ItemList(string FactoryID)
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

        // ------------------------- Option_Value
        private List<object> Option_Value()
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select * from Option_Value", conn))
            {
                

                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        FormID = dr["FormID"]?.ToString() ?? "",
                        ItemID = dr["ItemID"]?.ToString() ?? "",
                        ItemName = dr["ItemName"]?.ToString() ?? "",
                       
                       
                    });
                }
            }

            return list;
        }

        // ------------------------- AQL
        private List<object> AQL()
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select * from AQL where Active=1", conn))
            {
               
                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        AQL_Code = dr["AQL_Code"]?.ToString() ?? "",
                        Qty_From = dr["Qty_From"]?.ToString() ?? "",
                        Qty_To = dr["Qty_To"]?.ToString() ?? "",
                        Qty_Check = dr["Qty_Check"]?.ToString() ?? "",
                        Qty_Fault = dr["Qty_Fault"]?.ToString() ?? "",
                        typecode = dr["typecode"]?.ToString() ?? "",
                       
                    });
                }
            }

            return list;
        }

        // ------------------------- AQL_UQ
        private List<object> AQL_UQ()
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select * from AQL where Active=0", conn))
            {
               
                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        AQL_Code = dr["AQL_Code"]?.ToString() ?? "",
                        Qty_From = dr["Qty_From"]?.ToString() ?? "",
                        Qty_To = dr["Qty_To"]?.ToString() ?? "",
                        Qty_Check = dr["Qty_Check"]?.ToString() ?? "",
                        Qty_Fault = dr["Qty_Fault"]?.ToString() ?? "",
                        typecode = dr["typecode"]?.ToString() ?? "",
                       
                    });
                }
            }

            return list;
        }

        ///------------------------Dung_sai_inch
        private List<object> Dung_sai_inch()
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select * from Dung_sai where Don_vi = 'inch' ", conn))
            {
               
                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        ID = dr["ID"]?.ToString() ?? "",
                        Thong_so = dr["Thong_so"]?.ToString()?.Trim() ?? "",
                        Don_vi = dr["Don_vi"]?.ToString()?.Trim() ?? "",
                       
                    });
                }
            }

            return list;
        }

        // ------------------------- TRACKING_Module
        private List<object> TRACKING_Module()
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

        // ------------------------- TRACKING_GroupContactList
        private List<object> TRACKING_GroupContactList()
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select STT,ModuleName,GroupID from TRACKING_GroupContactList ", conn))
            {
               
                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        STT = dr["STT"]?.ToString() ?? "",
                        ModuleName = dr["ModuleName"]?.ToString() ?? "",
                        GroupID = dr["GroupID"]?.ToString() ?? "",
                     
                    });
                }
            }
            return list;
        }

        // ------------------------- TRACKING_InforSetup_DataType
        private List<object> TRACKING_InforSetup_DataType()
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select ModuleName,Infor_01,Infor_02,Infor_03,Infor_04,Infor_05,Infor_06,Infor_07,Infor_08,Infor_09,Infor_10,Infor_11,Infor_12,Infor_13,Infor_14,Infor_15 from TRACKING_InforSetup_DataType ", conn))
            {
               
                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        
                        ModuleName = dr["ModuleName"]?.ToString() ?? "",
                        Infor_01 = dr["Infor_01"]?.ToString() ?? "",
                        Infor_02 = dr["Infor_02"]?.ToString() ?? "",
                        Infor_03 = dr["Infor_03"]?.ToString() ?? "",
                        Infor_04 = dr["Infor_04"]?.ToString() ?? "",
                        Infor_05 = dr["Infor_05"]?.ToString() ?? "",
                        Infor_06 = dr["Infor_06"]?.ToString() ?? "",
                        Infor_07 = dr["Infor_07"]?.ToString() ?? "",
                        Infor_08 = dr["Infor_08"]?.ToString() ?? "",
                        Infor_09 = dr["Infor_09"]?.ToString() ?? "",
                        Infor_10 = dr["Infor_10"]?.ToString() ?? "",
                        Infor_11 = dr["Infor_11"]?.ToString() ?? "",
                        Infor_12 = dr["Infor_12"]?.ToString() ?? "",
                        Infor_13 = dr["Infor_13"]?.ToString() ?? "",
                        Infor_14 = dr["Infor_14"]?.ToString() ?? "",
                        Infor_15 = dr["Infor_15"]?.ToString() ?? "",
                     
                    });
                }
            }
            return list;
        }

        // ------------------------- TRACKING_InforSetup_Index
        private List<object> TRACKING_InforSetup_Index()
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select ModuleName,Infor_01,Infor_02,Infor_03,Infor_04,Infor_05,Infor_06,Infor_07,Infor_08,Infor_09,Infor_10,Infor_11,Infor_12,Infor_13,Infor_14,Infor_15 from TRACKING_InforSetup_Index ", conn))
            {
               
                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        
                        ModuleName = dr["ModuleName"]?.ToString() ?? "",
                        Infor_01 = dr["Infor_01"]?.ToString() ?? "",
                        Infor_02 = dr["Infor_02"]?.ToString() ?? "",
                        Infor_03 = dr["Infor_03"]?.ToString() ?? "",
                        Infor_04 = dr["Infor_04"]?.ToString() ?? "",
                        Infor_05 = dr["Infor_05"]?.ToString() ?? "",
                        Infor_06 = dr["Infor_06"]?.ToString() ?? "",
                        Infor_07 = dr["Infor_07"]?.ToString() ?? "",
                        Infor_08 = dr["Infor_08"]?.ToString() ?? "",
                        Infor_09 = dr["Infor_09"]?.ToString() ?? "",
                        Infor_10 = dr["Infor_10"]?.ToString() ?? "",
                        Infor_11 = dr["Infor_11"]?.ToString() ?? "",
                        Infor_12 = dr["Infor_12"]?.ToString() ?? "",
                        Infor_13 = dr["Infor_13"]?.ToString() ?? "",
                        Infor_14 = dr["Infor_14"]?.ToString() ?? "",
                        Infor_15 = dr["Infor_15"]?.ToString() ?? "",
                     
                    });
                }
            }
            return list;
        }

        // ------------------------- TRACKING_InforSetup_Name
        private List<object> TRACKING_InforSetup_Name()
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select ModuleName,Infor_01,Infor_02,Infor_03,Infor_04,Infor_05,Infor_06,Infor_07,Infor_08,Infor_09,Infor_10,Infor_11,Infor_12,Infor_13,Infor_14,Infor_15 from TRACKING_InforSetup_Name ", conn))
            {
               
                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        
                        ModuleName = dr["ModuleName"]?.ToString() ?? "",
                        Infor_01 = dr["Infor_01"]?.ToString() ?? "",
                        Infor_02 = dr["Infor_02"]?.ToString() ?? "",
                        Infor_03 = dr["Infor_03"]?.ToString() ?? "",
                        Infor_04 = dr["Infor_04"]?.ToString() ?? "",
                        Infor_05 = dr["Infor_05"]?.ToString() ?? "",
                        Infor_06 = dr["Infor_06"]?.ToString() ?? "",
                        Infor_07 = dr["Infor_07"]?.ToString() ?? "",
                        Infor_08 = dr["Infor_08"]?.ToString() ?? "",
                        Infor_09 = dr["Infor_09"]?.ToString() ?? "",
                        Infor_10 = dr["Infor_10"]?.ToString() ?? "",
                        Infor_11 = dr["Infor_11"]?.ToString() ?? "",
                        Infor_12 = dr["Infor_12"]?.ToString() ?? "",
                        Infor_13 = dr["Infor_13"]?.ToString() ?? "",
                        Infor_14 = dr["Infor_14"]?.ToString() ?? "",
                        Infor_15 = dr["Infor_15"]?.ToString() ?? "",
                     
                    });
                }
            }
            return list;
        }

        // ------------------------- TRACKINIG_ResultSetup_Name
        private List<object> TRACKINIG_ResultSetup_Name()
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select ModuleName,Infor_01,Infor_02,Infor_03,Infor_04,Infor_05 from TRACKINIG_ResultSetup_Name ", conn))
            {
               
                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        
                        ModuleName = dr["ModuleName"]?.ToString() ?? "",
                        Infor_01 = dr["Infor_01"]?.ToString() ?? "",
                        Infor_02 = dr["Infor_02"]?.ToString() ?? "",
                        Infor_03 = dr["Infor_03"]?.ToString() ?? "",
                        Infor_04 = dr["Infor_04"]?.ToString() ?? "",
                        Infor_05 = dr["Infor_05"]?.ToString() ?? "",
                        
                     
                    });
                }
            }
            return list;
        }

        // ------------------------- TRACKINIG_ResultSetup_Index
        private List<object> TRACKINIG_ResultSetup_Index()
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select ModuleName,Infor_01,Infor_02,Infor_03,Infor_04,Infor_05 from TRACKINIG_ResultSetup_Index ", conn))
            {
               
                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        
                        ModuleName = dr["ModuleName"]?.ToString() ?? "",
                        Infor_01 = dr["Infor_01"]?.ToString() ?? "",
                        Infor_02 = dr["Infor_02"]?.ToString() ?? "",
                        Infor_03 = dr["Infor_03"]?.ToString() ?? "",
                        Infor_04 = dr["Infor_04"]?.ToString() ?? "",
                        Infor_05 = dr["Infor_05"]?.ToString() ?? "",
                        
                     
                    });
                }
            }
            return list;
        }

        // ------------------------- TRACKINIG_ResultSetup_SelectionData
        private List<object> TRACKINIG_ResultSetup_SelectionData()
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select ModuleName,Infor_01,Infor_02,Infor_03,Infor_04,Infor_05 from TRACKINIG_ResultSetup_SelectionData ", conn))
            {
               
                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        
                        ModuleName = dr["ModuleName"]?.ToString() ?? "",
                        Infor_01 = dr["Infor_01"]?.ToString() ?? "",
                        Infor_02 = dr["Infor_02"]?.ToString() ?? "",
                        Infor_03 = dr["Infor_03"]?.ToString() ?? "",
                        Infor_04 = dr["Infor_04"]?.ToString() ?? "",
                        Infor_05 = dr["Infor_05"]?.ToString() ?? "",
                        
                     
                    });
                }
            }
            return list;
        }
        // ------------------------- TRACKINIG_ResultSetup_DataType
        private List<object> TRACKINIG_ResultSetup_DataType()
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select ModuleName,Infor_01,Infor_02,Infor_03,Infor_04,Infor_05 from TRACKINIG_ResultSetup_DataType ", conn))
            {
               
                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        
                        ModuleName = dr["ModuleName"]?.ToString() ?? "",
                        Infor_01 = dr["Infor_01"]?.ToString() ?? "",
                        Infor_02 = dr["Infor_02"]?.ToString() ?? "",
                        Infor_03 = dr["Infor_03"]?.ToString() ?? "",
                        Infor_04 = dr["Infor_04"]?.ToString() ?? "",
                        Infor_05 = dr["Infor_05"]?.ToString() ?? "",
                        
                     
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