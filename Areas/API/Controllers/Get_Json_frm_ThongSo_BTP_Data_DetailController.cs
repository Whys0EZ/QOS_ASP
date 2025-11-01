using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using QOS.Areas.API.Models;

namespace QOS.Areas.API.Controllers
{
    [Area("API")]
    [Route("api/[controller]")]
    [ApiController]
    public class Get_Json_frm_ThongSo_BTP_Data_DetailController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<Get_Json_frm_ThongSo_BTP_Data_DetailController> _logger;

        public Get_Json_frm_ThongSo_BTP_Data_DetailController(IConfiguration config, ILogger<Get_Json_frm_ThongSo_BTP_Data_DetailController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }

        [HttpGet]
        public IActionResult Get_Json_frm_ThongSo_BTP_Data_Detail(
            string? Code_G, 
            string? WorkDate, 
            string? FactoryID, 
            string? TypeName,
            string? WorkStageName,
            string? LINE_No, 
            string? MO, 
            string? ColorCode,
            string? Pattern,
            string? TableCode, 
            string? Size, 
            string? SizeNo)
        {
            if (string.IsNullOrEmpty(Code_G))
                return BadRequest(new { error = "Code_G is required" });

            try
            {
                // ✅ SỬ DỤNG HELPER
                string facCode = _config.GetValue<string>("AppSettings:FactoryCode") ?? "";
                var (isValid, factoryID, errorMsg) = Functions.ValidateCodeG(Code_G, facCode);

                if (!isValid)
                {
                    _logger.LogWarning($"Authentication failed: {errorMsg}");
                    return Unauthorized(new { error = errorMsg });
                }

                // ✅ SỬ DỤNG HELPER PARSE
                var (codeGs, myDeviceName, myMAC, loginID, dateF) = Functions.ParseCodeG(Code_G);

                // _logger.LogInformation($"ThongSo_BTP_Detail - FactoryID: {factoryID}, Date: {dateF}");

                // Query database...
                var ThongSoBTPDetail = ThongSo_BTP_Detail(WorkDate, FactoryID, TypeName,WorkStageName, LINE_No, MO, ColorCode, Pattern, TableCode, Size, SizeNo);

                return Ok(new { Json_Get_ThongSo_BTP_Detail = ThongSoBTPDetail });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error in ThongSo_BTP_Detail");
                return StatusCode(500, new { error = "Database error occurred" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ThongSo_BTP_Detail");
                return BadRequest(new { error = "An error occurred processing your request" });
            }
        }

        private List<object> ThongSo_BTP_Detail(string? WorkDate,string? FactoryID,string? TypeName,string? WorkStageName,string? LINE_No,string? MO,string? ColorCode,string? Pattern,string? TableCode,string? Size,string? SizeNo)
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Json_Get_ThongSo_BTP_Detail", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                
                cmd.Parameters.Add("@WorkDate", SqlDbType.DateTime).Value = WorkDate;
                cmd.Parameters.Add("@FactoryID", SqlDbType.NVarChar).Value = FactoryID;
                cmd.Parameters.Add("@TypeName", SqlDbType.NVarChar).Value = TypeName;
                cmd.Parameters.Add("@WorkStageName", SqlDbType.NVarChar).Value = WorkStageName;
                cmd.Parameters.Add("@LINE_No", SqlDbType.NVarChar).Value = LINE_No;
                cmd.Parameters.Add("@MO", SqlDbType.NVarChar).Value = MO;
                cmd.Parameters.Add("@ColorCode", SqlDbType.NVarChar).Value = ColorCode;
                cmd.Parameters.Add("@Pattern", SqlDbType.NVarChar).Value = Pattern;
                cmd.Parameters.Add("@TableCode", SqlDbType.NVarChar).Value = TableCode;
                cmd.Parameters.Add("@Size", SqlDbType.NVarChar).Value = Size;
                cmd.Parameters.Add("@SizeNo", SqlDbType.Int).Value = SizeNo;


                conn.Open();

                // // Log SQL
                // string debugSql = $"EXEC {cmd.CommandText} ";
                // debugSql += string.Join(", ", cmd.Parameters.Cast<SqlParameter>()
                //     .Select(p =>
                //         $"{p.ParameterName} = " +
                //         (p.Value == null || p.Value == DBNull.Value
                //             ? "NULL"
                //             : $"N'{p.Value.ToString().Replace("'", "''")}'")
                //     ));
                // _logger.LogInformation("Debug SQL: {debugSql}", debugSql);


                using SqlDataReader dr = cmd.ExecuteReader();
                
                int i = 1;
                while (dr.Read())
                {
                    list.Add(new 
                    {
                        // No = i,
                        WorkDate = Functions.GetStringValue(dr, "WorkDate"),
                        // LastUpdate = Functions.GetDateTimeValue(dr,"LastUpdate"),

                        FactoryID = Functions.GetStringValue(dr, "FactoryID"),
                        TypeName = Functions.GetStringValue(dr, "TypeName"),
                        CustomerName = Functions.GetStringValue(dr, "CustomerName"),
                        SECT_CODE = Functions.GetStringValue(dr, "SECT_CODE"),
                        LINE_No = Functions.GetStringValue(dr, "LINE_No"),
                        MO = Functions.GetStringValue(dr, "MO"),
                        ColorCode = Functions.GetStringValue(dr, "ColorCode"),
                        Pattern = Functions.GetStringValue(dr, "Pattern"),

                        WorkStageName = Functions.GetStringValue(dr, "WorkStageName"),
                        BatchCode = Functions.GetStringValue(dr, "BatchCode"),
                        RollCode = Functions.GetStringValue(dr, "RollCode"),
                        TableCode = Functions.GetStringValue(dr, "TableCode"),
                        Size = Functions.GetStringValue(dr, "Size"),
                        SizeNo = Functions.GetStringValue(dr, "SizeNo"),
                       
                       
                        Unit = Functions.GetStringValue(dr, "Unit"),
                        // Total_Fault_QTY = Functions.GetIntValue(dr, "Total_Fault_QTY"),
                        
                        Remark = Functions.GetStringValue(dr, "Remark"),
                        
                        UserUpdate = Functions.GetStringValue(dr, "UserUpdate"),
                        
                        
                      
                        CreatedDate = Functions.GetDateTimeValue(dr,"CreatedDate"),
                        LastUpdate = Functions.GetDateTimeValue(dr,"LastUpdate"),
                        data_List = Functions.GetStringValue(dr, "data_List"),
                        
                    });
                    i++;
                }
            }

            return list;
        }

    }
}



// ## **🧪 Test API:**

// GET api/Get_Json_frm_ThongSo_BTP_Data_Detail?Code_G=f4cf3fe4b67244332eecec055742d449REG263ca1464c3480896c1b78a5eac5a6e971f0e3dad99908345f7439f8ffabdffc4_____DeviceName_____MAC_____LoginID_____2025-10-28&Search_txt=&Unit=2U01&Line=201S01&ALL=YES