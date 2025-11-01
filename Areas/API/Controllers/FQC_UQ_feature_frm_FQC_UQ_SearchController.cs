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
    public class FQC_UQ_feature_frm_FQC_UQ_SearchController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<FQC_UQ_feature_frm_FQC_UQ_SearchController> _logger;

        public FQC_UQ_feature_frm_FQC_UQ_SearchController(IConfiguration config, ILogger<FQC_UQ_feature_frm_FQC_UQ_SearchController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }

        [HttpGet]
        public IActionResult FQC_UQ_feature_frm_FQC_UQ_Search(
            string? Code_G, 
            string? Search_txt, 
            string? Unit,
            string? Industry)
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

                // _logger.LogInformation($"SearchFQC - FactoryID: {factoryID}, Date: {dateF}");

                // Query database...
                var FQCList = GetFQCData(factoryID, dateF, Unit ?? "", Search_txt ?? "",Industry ?? "");

                return Ok(new { FQC_UQ_Search = FQCList });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error in SearchFQC");
                return StatusCode(500, new { error = "Database error occurred" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchFQC");
                return BadRequest(new { error = "An error occurred processing your request" });
            }
        }

        private List<object> GetFQCData(string factoryID, string dateF, string unit, string searchTxt, string industry)
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Json_RP_FQC_UQ_Search", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@FactoryID", SqlDbType.NVarChar).Value = factoryID;
                cmd.Parameters.Add("@Date_F", SqlDbType.DateTime).Value = dateF;
                cmd.Parameters.Add("@Unit", SqlDbType.NVarChar).Value = unit;
                // cmd.Parameters.Add("@Line", SqlDbType.NVarChar).Value = line;
                cmd.Parameters.Add("@Search", SqlDbType.NVarChar).Value = searchTxt;
                cmd.Parameters.Add("@Industry", SqlDbType.NVarChar).Value = industry;

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
                        No = i,  
                        RowColor_V = Functions.GetStringValue(dr, "RowColor_V"),
                        RowColor_Set = Functions.GetStringValue(dr, "RowColor_Set"),
                        RowClick_V = Functions.GetStringValue(dr, "RowClick_V"),
                        LastUpdate = Functions.GetDateTimeValue(dr,"LastUpdate"),
                        Industry = Functions.GetStringValue(dr, "Industry"),

                        ResultStatus = Functions.GetStringValue(dr, "ResultStatus"),
                        Customer = Functions.GetStringValue(dr, "Customer"),
                        ModuleName = Functions.GetStringValue(dr, "ModuleName"),
                        PRO = Functions.GetStringValue(dr, "PRO"),
                        Remark = Functions.GetStringValue(dr, "Remark"),
                        SO = Functions.GetStringValue(dr, "SO"),
                        Style = Functions.GetStringValue(dr, "Style"),
                        Item_No = Functions.GetStringValue(dr, "Item_No"),
                        PO = Functions.GetStringValue(dr, "PO"),
                        Qty = Functions.GetStringValue(dr, "Qty"),
                        Destination = Functions.GetStringValue(dr, "Destination"),
                        Update_Date = Functions.GetStringValue(dr, "Update_Date"),
                        shipMode = Functions.GetStringValue(dr, "shipMode"),
                        Remedies = Functions.GetStringValue(dr, "Remedies"),
                        Operation = Functions.GetStringValue(dr, "Operation"),
                        No_Carton = Functions.GetStringValue(dr, "No_Carton"),
                        Audit_Time = Functions.GetStringValue(dr, "Audit_Time"),
                        Fault = Functions.GetStringValue(dr, "Fault"),
                        Re_Audit = Functions.GetStringValue(dr, "Re_Audit"),
                        AQL = Functions.GetStringValue(dr, "AQL"),
                        ID_Report = Functions.GetStringValue(dr, "ID_Report"),
                        Size_Name = Functions.GetStringValue(dr, "Size_Name"),
                        CreatedBy = Functions.GetStringValue(dr, "CreatedBy"),
                        Led = Functions.GetStringValue(dr, "Led"),
                        Total_Fault_QTY = Functions.GetStringValue(dr, "Total_Fault_QTY"),
                        IMG_Result = Functions.GetStringValue(dr, "IMG_Result"),

                        cl_List = Functions.GetStringValue(dr, "cl_List"),
                        cl_Size = Functions.GetStringValue(dr, "cl_Size")
                        
                    });
                    i++;
                }
            }

            return list;
        }

    }
}



// ## **🧪 Test API:**

// GET api/FQC_UQ_feature_frm_FQC_UQ_Search?Code_G=f4cf3fe4b67244332eecec055742d449REG263ca1464c3480896c1b78a5eac5a6e971f0e3dad99908345f7439f8ffabdffc4_____DeviceName_____MAC_____LoginID_____2025-10-28&Search_txt=&Unit=2U01&Line=201S01&ALL=YES