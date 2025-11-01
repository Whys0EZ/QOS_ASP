using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QOS.Areas.Report.Models;
using QOS.Data;
using QOS.Models;
using System.Data;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace QOS.Areas.Report.Controllers
{
    [Authorize]
    [Area("Report")]
    public class Form4QualityController : Controller
    {
        private readonly ILogger<Form4QualityController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly AppDbContext _context;
        [TempData]
        public string? MessageStatus { get; set;}

        public Form4QualityController(ILogger<Form4QualityController> logger, IWebHostEnvironment environment, IConfiguration configuration, AppDbContext context)
        {
            _logger = logger ;
            _env = environment ;
            _configuration = configuration ;
            _context = context ;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";

        }
        public ActionResult Index()
        {
            return RedirectToAction("Form4_Quality");
        }
        public ActionResult Form4_Quality(string? Unit, DateTime? dateFrom , DateTime? dateEnd)
        {
            var model = new Form4QualityViewModel {
                Unit_List = GetUnitList(),
                Unit = Unit ?? "ALL",
                DateFrom = dateFrom ?? DateTime.Now,
                DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1)

            };
            model.DefectCodes = LoadDefectCodes(model);
            LoadReportData(model);
            return View(model);
        }

        private List<QOS.Models.Unit_List> GetUnitList()
        {
            try
            {
                var units = _context.Set<QOS.Models.Unit_List>()
                    .Where(u => u.Factory == "REG2")
                    .OrderBy(u => u.Unit)
                    .ToList();

                _logger.LogInformation($"Loaded {units.Count} units from database");
                return units;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading unit list");
                return new List<QOS.Models.Unit_List>();
            }
        }

        private void LoadReportData(Form4QualityViewModel model)
        {
            _logger.LogInformation("=== Loading Report Data ===");
            _logger.LogInformation($"Parameters - DateFrom: {model.DateFrom:yyyy-MM-dd HH:mm:ss}, DateEnd: {model.DateEnd:yyyy-MM-dd HH:mm:ss}, Unit: '{model.Unit}'");

            try
            {
                using var connection = new SqlConnection(_connectionString);
                
                connection.Open();
                _logger.LogInformation("Database connection opened successfully");
                string dateFStr = model.DateFrom.ToString("yyyy-MM-dd");
                string dateTStr = model.DateEnd.ToString("yyyy-MM-dd");
                var userName = User.Identity?.Name;
                var reportDataList = new List<Dictionary<string, object>>();
                var defectCodeList = new Dictionary<string, DefectCode>();

                using (var command = new SqlCommand("RP_BCCLM_Report", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Date_F", dateFStr);
                    command.Parameters.AddWithValue("@Date_T", dateTStr);
                    command.Parameters.AddWithValue("@UserName", userName);
                    command.Parameters.AddWithValue("@Unit1", model.Unit == "ALL" ? "" : model.Unit);
                    

                    using var reader = command.ExecuteReader();
                    
                    {
                        while(reader.Read())
                        {
                            var rowData = new Dictionary<string, object>();
                            // rowData["ID_L"] = reader["ID_L"] != DBNull.Value ? Convert.ToInt32(reader["ID_L"]) : 0;
                            rowData["Unit"] = reader["Unit"]?.ToString() ?? "";
                            rowData["Line"] = reader["Line"]?.ToString() ?? "";
                            rowData["WorkDate"] = reader["WorkDate"] == DBNull.Value ? "" : Convert.ToDateTime(reader["WorkDate"]).ToString("yyyy-MM-dd");
                            rowData["MO"] = reader["MO"]?.ToString() ?? "";
                            rowData["StyleCode"] = reader["StyleCode"]?.ToString() ?? "";
                            rowData["Operation_Code"] = reader["Operation_Code"]?.ToString() ?? "";
                            rowData["Operation_Name_VN"] = reader["Operation_Name_VN"]?.ToString() ?? "";
                            rowData["Check_Qty"] = reader["Check_Qty"] != DBNull.Value ? Convert.ToInt32(reader["Check_Qty"]) : 0;
                            rowData["Fault"] = reader["Fault"] != DBNull.Value ? Convert.ToInt32(reader["Fault"]) : 0;

                           
                            rowData["Fault_Code"] = reader["Fault_Code"]?.ToString() ?? "";

                            

                            string faultCodeStr = reader["Fault_Code"]?.ToString() ?? "";
                            var faultCodesArray = faultCodeStr.Split(';', StringSplitOptions.RemoveEmptyEntries);

                            var defectNames = new List<string>();

                            foreach (var faultCode in faultCodesArray)
                            {
                                if (model.DefectCodes.TryGetValue(faultCode, out var defect))
                                {
                                    defectNames.Add(defect.Fault_Name ?? "");
                                }
                            }

                            string defectName = string.Join(";", defectNames);

                            rowData["Defect_Name"] = string.Join(";", defectNames); // ✅ Tên lỗi thực tế

                            reportDataList.Add(rowData);

                        }
                       
                        model.ReportData = reportDataList;
                    }
                }

                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading report data");
                
                throw;
            }
        }
        private Dictionary<string, DefectCode> LoadDefectCodes(Form4QualityViewModel model)
        {
            var defectDict = new Dictionary<string, DefectCode>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("RP_TongHopLoiChuyenMay", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Date_From", model.DateFrom.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@Date_To", model.DateEnd.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@Unit", string.IsNullOrEmpty(model.Unit) ? "ALL" : model.Unit);
                cmd.Parameters.AddWithValue("@Line","ALL");
                cmd.Parameters.AddWithValue("@Defected_Type","ALL");
                cmd.Parameters.AddWithValue("@Top_Defected","ALL");
                cmd.Parameters.AddWithValue("@Seach","ALL");

                conn.Open();
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var code = reader["Fault_Code"]?.ToString()?.Trim();
                    var name = reader["Fault_Name_VN"]?.ToString()?.Trim();
                    

                    if (!string.IsNullOrEmpty(code) && !defectDict.ContainsKey(code))
                    {
                        defectDict[code] = new DefectCode
                        {
                            Fault_Code = code,
                            Fault_Name = name
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading DefectCodes from stored procedure");
            }

            return defectDict;
        }

        public IActionResult ExportExcel(string? Unit, DateTime? dateFrom, DateTime? dateEnd)
        {
            var model = new Form4QualityViewModel {
                Unit_List = GetUnitList(),
                Unit = Unit ?? "ALL",
                DateFrom = dateFrom ?? DateTime.Now,
                DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1)

            };
            model.DefectCodes = LoadDefectCodes(model);
            LoadReportData(model);
            string dateFStr = model.DateFrom.ToString("yyyy-MM-dd");
            string dateTStr = model.DateEnd.ToString("yyyy-MM-dd");
            var userName = User.Identity?.Name;


            // if (model.DynamicTable == null || model.DynamicTable.Rows.Count == 0)
            // {
            //     TempData["Message"] = "Không có dữ liệu để xuất Excel.";
            //     return RedirectToAction("Form4_Quality");
            // }

            // Tạo Excel file (ví dụ với ClosedXML hoặc EPPlus)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Đường dẫn tới file template
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Report", "Format_Quality.xlsx");
            
            if (!System.IO.File.Exists(templatePath))
            {
                MessageStatus = "Không tìm thấy file mẫu báo cáo.";
                return RedirectToAction("Form4_Quality");
            }
            //  using var package = new ExcelPackage(new FileInfo(templatePath));
            using (var package = new ExcelPackage(new FileInfo(templatePath)))
            {
                // var worksheet = package.Workbook.Worksheets.Add("Report");
                var worksheet = package.Workbook.Worksheets[0];

                int row = 3;
                // int col = 1;
                // worksheet.Cells[1, 1].Value = "Báo cáo Inline" ;
                worksheet.Cells["A1"].Value = "Báo cáo Inline - " + (Unit ?? "ALL");
                worksheet.Cells["E1"].Value = $"{(dateFrom ?? DateTime.Now):dd/MM/yyyy}" ;
                worksheet.Cells["F1"].Value = $" {(dateEnd ?? DateTime.Now):dd/MM/yyyy}" ;
                // ==== Header ====
              
                

                // ==== Format chung ====
                worksheet.Cells.AutoFitColumns();
                // worksheet.View.FreezePanes(2, 3); // cố định header + 2 cột đầu
                worksheet.Cells.Style.Font.Name = "Calibri";
                worksheet.Cells.Style.Font.Size = 11;
                worksheet.Cells[1,1].Style.Font.Size = 14;
                worksheet.Cells[2,1].Style.Font.Size = 14;
                worksheet.Cells[1,1].Style.Font.Bold = true;
                worksheet.Cells[2,1].Style.Font.Bold = true;

                // Lấy dữ liệu báo cáo
                using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                using var cmd = new SqlCommand("RP_BCCLM_Report", conn);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Date_F", dateFStr);
                cmd.Parameters.AddWithValue("@Date_T", dateTStr);
                cmd.Parameters.AddWithValue("@UserName", userName);
                cmd.Parameters.AddWithValue("@Unit1", model.Unit == "ALL" ? "" : model.Unit);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    
                    

                    while (reader.Read())
                    {
                        worksheet.Cells[row, 1].Value = row - 2; // STT
                        worksheet.Cells[row, 2].Value = reader["Unit"].ToString();
                        worksheet.Cells[row, 3].Value = reader["Line"].ToString();
                        worksheet.Cells[row, 4].Value = reader["WorkDate"].ToString();
                        worksheet.Cells[row, 5].Value = reader["MO"].ToString();
                        worksheet.Cells[row, 6].Value = reader["StyleCode"].ToString();
                        worksheet.Cells[row, 7].Value = reader["Operation_Code"].ToString();
                        worksheet.Cells[row, 8].Value = reader["Operation_Name_VN"].ToString();
                        worksheet.Cells[row, 9].Value = reader["Check_QTY"].ToString();
                        worksheet.Cells[row, 10].Value = reader["Fault"].ToString();
                        // reader["Check_Qty"] != DBNull.Value ? Convert.ToInt32(reader["Check_Qty"]) : 0;
                        var checkQty = reader["Check_Qty"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Check_Qty"]);
                        var totalDefect = reader["Fault"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Fault"]);

                        double OQL = (checkQty == 0) ? 0 : (totalDefect / checkQty) * 100;
                        OQL = Math.Round(OQL, 2); // 🔹 chỉ giữ 2 số thập phân


                        string faultCodeStr = reader["Fault_Code"]?.ToString() ?? "";
                        var faultCodesArray = faultCodeStr.Split(';', StringSplitOptions.RemoveEmptyEntries);

                        var defectNames = new List<string>();

                        foreach (var faultCode in faultCodesArray)
                        {
                            if (model.DefectCodes.TryGetValue(faultCode, out var defect))
                            {
                                defectNames.Add(defect.Fault_Name ?? "");
                            }
                        }

                        string defectName = string.Join(";", defectNames);

                        worksheet.Cells[row, 11].Value = OQL ;

                        worksheet.Cells[row, 12].Value = defectNames;
                        row ++;

                    }
                }
                

                // ==== Xuất ra file ====
                var fileName = $"Report_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                var stream = new MemoryStream(package.GetAsByteArray());
                return File(stream.ToArray(), 
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                            fileName);
            }
            

        }
    }
}