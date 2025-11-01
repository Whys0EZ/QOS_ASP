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
    public class FQC_Get_Barcode_InforController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<FQC_Get_Barcode_InforController> _logger;

        public FQC_Get_Barcode_InforController(IConfiguration config, ILogger<FQC_Get_Barcode_InforController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }
        [HttpGet]
        [HttpGet("[action]")]
        public IActionResult FQC_Get_Barcode_Infor(string? Code_G, string? SO)
        {
            if (string.IsNullOrEmpty(Code_G))
                return BadRequest(new { error = "Code_G is required" });

            try
            {
                    
                var response = new
                {
                    Get_Data_Barcode_Info = Get_Data_Barcode_Info(Code_G)
                };
            

                return Ok(response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Get_Data_Barcode_Info");
                return BadRequest(new { error = ex.Message });
            }
        }
        private List<object> Get_Data_Barcode_Info(string Code_G)
        {
            // string sql = $@"Select * from TRACKINIG_UploadData where  Infor_01 = '{firstValue}' ";
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new("Get_Bacode_FQC_TEST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Bacode", Code_G);

                    // Đọc kết quả từ SP
                    var table = new DataTable();
                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(table);
                    }

                    // Duyệt từng dòng dữ liệu từ SP
                    foreach (DataRow row in table.Rows)
                    {
                        string MO = row["So_No"]?.ToString()?.Trim() ?? "";
                        string PO = row["Po_No"]?.ToString()?.Trim() ?? "";
                        string Item = row["Item"]?.ToString()?.Trim() ?? "";
                        string Warehouse_Desc = row["Warehouse_Desc"]?.ToString()?.Trim() ?? "";

                        string queryF;

                        if (string.IsNullOrEmpty(MO) || MO == "1234")
                            queryF = $"SELECT TOP 1 * FROM TRACKINIG_UploadData WHERE Infor_04 = N'{PO}' AND Infor_01 != '#REF!'";
                        else
                            queryF = $"SELECT TOP 1 * FROM TRACKINIG_UploadData WHERE Infor_01 = N'{MO}' AND Infor_04 = N'{PO}' AND Infor_01 != '#REF!'";

                        if (Warehouse_Desc.Contains("NIKE"))
                            queryF += $" AND Infor_03 = '{Item}'";

                        using (var cmdF = new SqlCommand(queryF, conn))
                        using (var rsF = cmdF.ExecuteReader())
                        {
                            while (rsF.Read())
                            {
                                list.Add(new
                                {
                                    SO = rsF["Infor_01"]?.ToString()?.Trim(),
                                    Style = rsF["Infor_02"]?.ToString()?.Trim(),
                                    PO = rsF["Infor_04"]?.ToString()?.Trim(),
                                    Qty = rsF["Infor_05"]?.ToString()?.Trim(),
                                    Des = rsF["Infor_08"]?.ToString()?.Trim(),
                                    Pro = rsF["Infor_09"]?.ToString()?.Trim(),
                                    New_date = rsF["Infor_06"]?.ToString()?.Trim(),
                                    Item = rsF["Infor_03"]?.ToString()?.Trim().Replace("'", " ").Replace("&", "-"),
                                    Ship_mode = rsF["Infor_07"]?.ToString()?.Trim().Replace("'", " ").Replace("&", "-"),
                                    ID = rsF["ID"]?.ToString()?.Trim()
                                });
                            }
                        }
                        
                    }
                }
            }

            return list;
        }
    }
}


// /api/FQC_Get_Barcode_Infor?Code_G=00035837802823901148&SO=123