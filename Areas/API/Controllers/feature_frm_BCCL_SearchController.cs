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
    public class feature_frm_BCCL_SearchController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<feature_frm_BCCL_SearchController> _logger;

        public feature_frm_BCCL_SearchController(IConfiguration config, ILogger<feature_frm_BCCL_SearchController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }

        [HttpGet]
        public IActionResult feature_frm_BCCL_Search(
            string? Code_G, 
            string? Search_txt, 
            string? Unit, 
            string? ALL)
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

                _logger.LogInformation($"SearchBCCLC - FactoryID: {factoryID}, Date: {dateF}");

                // Query database...
                var BCCLCList = GetBCCLCData(factoryID, dateF, Unit ?? "", Search_txt ?? "", ALL ?? "");

                return Ok(new { BCCLC_Search = BCCLCList });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error in SearchBCCLC");
                return StatusCode(500, new { error = "Database error occurred" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchBCCLC");
                return BadRequest(new { error = "An error occurred processing your request" });
            }
        }

        private List<BCCLCModel> GetBCCLCData(string factoryID, string dateF, string unit, string searchTxt, string all)
        {
            List<BCCLCModel> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Json_RP_BCCL_SUM", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@FactoryID", SqlDbType.NVarChar).Value = factoryID;
                cmd.Parameters.Add("@Date_F", SqlDbType.DateTime).Value = dateF;
                cmd.Parameters.Add("@Unit", SqlDbType.NVarChar).Value = unit;
                cmd.Parameters.Add("@Search", SqlDbType.NVarChar).Value = searchTxt;
                cmd.Parameters.Add("@ALL", SqlDbType.NVarChar).Value = all;

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
                    list.Add(new BCCLCModel
                    {
                        No = i,
                        Led = GetStringValue(dr, "Led_"),
                        LastUpdate = Functions.GetDateTimeValue(dr,"LastUpdate"),
                        ID = GetStringValue(dr, "ID"),
                        Report_ID = GetStringValue(dr, "Report_ID"),
                        MO = GetStringValue(dr, "MO"),
                        Cut_Leader = GetStringValue(dr, "Cut_Leader"),
                        Cut_Table = GetStringValue(dr, "CutTableName").Trim(),
                        Cut_Lot = GetStringValue(dr, "Cut_Lot").Trim(),
                        CutTableName = GetStringValue(dr, "CutTableName").Trim(),
                        Lay_Height = GetStringValue(dr, "Lay_Height").Trim(),
                        Table_Long = Functions.GetStringValue(dr, "Table_Long"),
                        Table_Width = Functions.GetStringValue(dr, "Table_Width"),
                        CutTableRatio = GetStringValue(dr, "CutTableRatio").Trim(),
                        Batch = Functions.GetStringValue(dr, "Batch"),
                        Cut_QTY = Functions.GetIntValue(dr, "Cut_QTY"),
                        Shading = Functions.GetBoolValue(dr, "Shading"),
                        Wave = Functions.GetBoolValue(dr, "Wave"),
                        Narrow_Width = Functions.GetBoolValue(dr, "Narrow_Width"),
                        Spreading = Functions.GetBoolValue(dr, "Spreading"),
                        DS_L_Min = Functions.GetStringValue(dr, "DS_L_Min"),
                        DS_L_Max = Functions.GetStringValue(dr, "DS_L_Max"),
                        DS_W_Min = Functions.GetStringValue(dr, "DS_W_Min"),
                        DS_W_Max = Functions.GetStringValue(dr, "DS_W_Max"),
                        Size_Parameter = Functions.GetStringValue(dr, "Size_Parameter"),
                        Notch = Functions.GetStringValue(dr, "Notch"),
                        Unclean = Functions.GetStringValue(dr, "Unclean"),
                        Straigh = Functions.GetStringValue(dr, "Straigh"),
                        Shape = Functions.GetStringValue(dr, "Shape"),
                        Edge = Functions.GetStringValue(dr, "Edge"),
                        Stripe = Functions.GetStringValue(dr, "Stripe"),
                        Remark = Functions.GetStringValue(dr, "Remark"),
                        Number_Check = Functions.GetStringValue(dr, "Audit_Time"),
                        Re_Check = Functions.GetStringValue(dr, "Re_Check"),
                        
                        Re_Audit = Functions.GetBoolValue(dr, "Re_Audit"),
                        Audit_Time = Functions.GetDateTimeValue(dr, "Audit_Time"),
                        UserUpdate = GetStringValue(dr, "UserUpdate"),
                        Photo_URL = GetStringValue(dr, "Photo_URL").Trim(),
                        Unit = GetStringValue(dr, "Unit"),
                        Color = GetStringValue(dr, "Color"),
                        // DS_L_Min_2 = Functions.GetStringValue(dr, "DS_L_Min_2"),
                        // DS_L_Max_2 = Functions.GetStringValue(dr, "DS_L_Max_2"),
                        // DS_W_Min_2 = Functions.GetStringValue(dr, "DS_W_Min_2"),
                        // DS_W_Max_2 = Functions.GetStringValue(dr, "DS_W_Max_2"),
                        cl_List = GetStringValue(dr, "cl_List"),
                        cl_Size = GetStringValue(dr, "cl_Size"),
                        RowColor_Set = GetStringValue(dr, "RowColor_Set"),
                        RowColor_V = GetStringValue(dr, "RowColor_V"),
                        RowClick_V = GetStringValue(dr, "RowClick_V")
                    });
                    i++;
                }
            }

            return list;
        }

        private string GetStringValue(SqlDataReader dr, string columnName)
        {
            try
            {
                return dr[columnName]?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }

        private string FormatDateTime(object dateValue)
        {
            if (dateValue == null || dateValue == DBNull.Value)
                return "";

            if (DateTime.TryParse(dateValue.ToString(), out DateTime dt))
            {
                return dt.ToString("dd-MMM-yyyy HH:mm:ss");
            }

            return dateValue.ToString() ?? "";
        }

        private static string MD5Hash(string input)
        {
            using var md5 = MD5.Create();
            byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }
}



// ## **🧪 Test API:**

// GET api/feature_frm_BCCL_Search_New?Code_G=f4cf3fe4b67244332eecec055742d449REG263ca1464c3480896c1b78a5eac5a6e971f0e3dad99908345f7439f8ffabdffc4_____DeviceName_____MAC_____LoginID_____2025-10-28&Search_txt=&Unit=2U08&ALL=YES