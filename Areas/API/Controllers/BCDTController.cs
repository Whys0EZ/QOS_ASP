using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using QOS.Areas.API.Models;

namespace QOS.Areas.API.Controllers
{
    [Area("API")]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class BCDTController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<BCDTController> _logger;

        public BCDTController(IConfiguration config, ILogger<BCDTController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }

        [HttpGet]
        public IActionResult SearchBCDT(
            string? Code_G, 
            string? Search_txt, 
            string? Unit, 
            string? ALL)
        {
            if (string.IsNullOrEmpty(Code_G))
                return BadRequest(new { error = "Code_G is required" });

            try
            {
                // Parse parameters
                Search_txt ??= "";
                Unit ??= "";
                ALL ??= "";

                // Parse Code_G
                string[] txt = Code_G.Split("_____");
                
                string codeGs = txt.Length > 0 ? txt[0] : "";
                string myDeviceName = txt.Length > 1 ? txt[1] : "";
                string myMAC = txt.Length > 2 ? txt[2] : "";
                string loginID = txt.Length > 3 ? txt[3] : "";
                string dateF = txt.Length > 4 ? txt[4] : "";

                // Decode Code_G
                if (string.IsNullOrEmpty(codeGs) || codeGs.Length < 64)
                    return BadRequest(new { error = "Invalid Code_G format" });

                string tmp1 = codeGs.Substring(0, 32);
                string tmp2 = codeGs.Substring(0, codeGs.Length - 32);
                string tmp3 = tmp2.Length >= 32 ? tmp2.Substring(tmp2.Length - 32) : "";
                string tmp4 = tmp2.Length > 32 ? tmp2.Substring(32) : "";
                string tmp5 = tmp4.Length > 32 ? tmp4.Substring(0, tmp4.Length - 32) : "";
                string factoryID = tmp5;

                string facCode = _config.GetValue<string>("AppSettings:FactoryCode") ?? "";

                // Validate authentication
                if (tmp1 != MD5Hash(facCode) || tmp3 != MD5Hash(factoryID))
                {
                    _logger.LogWarning($"Authentication failed for FactoryID: {factoryID}");
                    return Unauthorized(new { error = "Invalid factory or authentication failed" });
                }

                _logger.LogInformation($"SearchBCDT - FactoryID: {factoryID}, Date: {dateF}, Unit: {Unit}, Search: {Search_txt}");

                var response = new BCDTSearchResponse
                {
                    BCDT_Search = GetBCDTData(factoryID, dateF, Unit, Search_txt, ALL)
                };

                return Ok(response);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error in SearchBCDT");
                return StatusCode(500, new { error = "Database error occurred" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchBCDT");
                return BadRequest(new { error = "An error occurred processing your request" });
            }
        }

        private List<BCDTModel> GetBCDTData(string factoryID, string dateF, string unit, string searchTxt, string all)
        {
            List<BCDTModel> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Json_RP_BCDT_SUM", conn))
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
                    list.Add(new BCDTModel
                    {
                        No = i,
                        Led = GetStringValue(dr, "Led_"),
                        LastUpdate = FormatDateTime(dr["LastUpdate"]),
                        ID = GetStringValue(dr, "ID"),
                        Report_ID = GetStringValue(dr, "Report_ID"),
                        Unit = GetStringValue(dr, "Unit"),
                        Cut_Leader = GetStringValue(dr, "Cut_Leader"),
                        MO = GetStringValue(dr, "MO").Trim(),
                        Color = GetStringValue(dr, "Color").Trim(),
                        Roll = GetStringValue(dr, "Roll").Trim(),
                        Lot = GetStringValue(dr, "Lot").Trim(),
                        QTY = Functions.GetIntValue(dr, "QTY"),
                        Layer_QTY = Functions.GetIntValue(dr, "Layer_QTY"),
                        CutTableRatio = GetStringValue(dr, "CutTableRatio").Trim(),
                        Cut_QTY = Functions.GetIntValue(dr, "Cut_QTY"),
                        Cut_Table_Height = GetStringValue(dr, "Cut_Table_Height"),
                        Cut_Table_Long = GetStringValue(dr, "Cut_Table_Long").Trim(),
                        vai_ke = Functions.GetBoolValue(dr, "vai_ke"),
                        noi_vai = Functions.GetBoolValue(dr, "noi_vai"),
                        cang_vai = Functions.GetBoolValue(dr, "cang_vai"),
                        sai_mat = Functions.GetBoolValue(dr, "sai_mat"),
                        hep_kho = Functions.GetBoolValue(dr, "hep_kho"),
                        ban_vai = Functions.GetBoolValue(dr, "ban_vai"),
                        vi_tri = Functions.GetBoolValue(dr, "vi_tri"),
                        vai_nghieng = Functions.GetBoolValue(dr, "vai_nghieng"),
                        song_vai = Functions.GetBoolValue(dr, "song_vai"),
                        thang_ke = Functions.GetBoolValue(dr, "thang_ke"),
                        khac_mau = Functions.GetBoolValue(dr, "khac_mau"),
                        quen_bam = Functions.GetBoolValue(dr, "quen_bam"),
                        bam_sau = Functions.GetBoolValue(dr, "bam_sau"),
                        xoc_xech = Functions.GetBoolValue(dr, "xoc_xech"),
                        khong_cat = Functions.GetBoolValue(dr, "khong_cat"),
                        khong_gon = Functions.GetBoolValue(dr, "khong_gon"),
                        doi_ke = Functions.GetBoolValue(dr, "doi_ke"),
                        doi_xung = Functions.GetBoolValue(dr, "doi_xung"),
                        so_lop = Functions.GetBoolValue(dr, "so_lop"),
                        so_btp = Functions.GetBoolValue(dr, "so_btp"),
                        Size_Parameter_Cat = GetStringValue(dr, "Size_Parameter_Cat").Trim(),
                        Size_Parameter_CPI = GetStringValue(dr, "Size_Parameter_CPI").Trim(),
                        Size_Parameter_TP = GetStringValue(dr, "Size_Parameter_TP").Trim(),
                        Remark = GetStringValue(dr, "Remark").Trim(),
                        Re_Audit = Functions.GetBoolValue(dr, "Re_Audit"),
                        Audit_Time = Functions.GetIntValue(dr, "Audit_Time"),
                        UserUpdate = GetStringValue(dr, "UserUpdate"),
                        Photo_URL = GetStringValue(dr, "Photo_URL").Trim(),
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

// GET /api/BCDT/SearchBCDT?Code_G=abc123_____DeviceName_____MAC_____LoginID_____2024-10-28&Search_txt=MO123&Unit=Unit1&ALL=1