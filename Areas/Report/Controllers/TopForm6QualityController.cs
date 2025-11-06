using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QOS.Areas.Report.Models;
using QOS.Data;
using QOS.Models;
using Dapper;
using OfficeOpenXml;
using System.Data;
using System.Text.Json;
using QOS.Areas.Function.Filters;
using OfficeOpenXml.Style;
using System.Drawing;


namespace QOS.Areas.Report.Controllers
{
    [Authorize] // chỉ khi login mới được vào
    [Area("Report")]
    public class TopForm6QualityController : Controller
    {
        private readonly ILogger<TopForm6QualityController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly AppDbContext _context;
        private readonly string _factoryName;

        public TopForm6QualityController(ILogger<TopForm6QualityController> logger, IWebHostEnvironment env, IConfiguration configuration, AppDbContext context)
        {
            _logger = logger;
            _env = env;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Missing connection string: DefaultConnection");
            _configuration =configuration;
            _context = context;
            _factoryName = _configuration.GetValue<string>("AppSettings:FactoryName") ?? "";
        }
        public IActionResult Index()
        {
            // return View();
            return RedirectToAction("TopForm6Quality", "TopForm6Quality");
        }
        [TempData]
        public string? MessageStatus { get; set;}
        [HttpGet]
        public IActionResult TopForm6Quality(string? Unit, string? Line, DateTime? dateFrom, DateTime? dateEnd)
        {
            // _logger.LogInformation("=== TopForm6Quality GET Request ===");
            // _logger.LogInformation($"Parameters - Unit: '{Unit}', DateFrom: {dateFrom}, DateEnd: {dateEnd}, Line: '{Line}' ");
            try
            {
                var model = new TopForm4QualityViewModel
                {
                    Unit_List = GetUnitList(),
                    
                    Unit = Unit ?? "ALL",
                    Line = Line ?? "ALL",
                   
                    DateFrom = dateFrom ?? DateTime.Now,
                    DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1),
                    ReportData = new List<Dictionary<string, object>>(),
                    // DefectStats = new Dictionary<string, DefectStat>()
                    
                };
                model.DefectCodes = LoadDefectCodes(model);
                LoadReportData(model);
                // _logger.LogInformation($"Model created - Units available: {model.Unit_List.Count}");
                // LoadReportData(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RP_Summary_Defects_KCC GET");
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return View(new TopForm4QualityViewModel { Unit_List = GetUnitList() });
            }
        }
        private List<Unit_List> GetUnitList()
        {
            try
            {
                var units = _context.Set<Unit_List>()
                    .Where(u => u.Factory == _factoryName)
                    .OrderBy(u => u.Unit)
                    .ToList();

                // _logger.LogInformation($"Loaded {units.Count} units from database");
                return units;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading unit list");
                return new List<Unit_List>();
            }
        }

        // API endpoint to get lines by unit
        [HttpGet]
        public JsonResult GetLinesByUnit(string unitId)
        {
            // _logger.LogInformation($"GetLinesByUnit called with unitId: {unitId}");
            
            try
            {
                if (string.IsNullOrEmpty(unitId) || unitId == "ALL")
                {
                    return Json(new List<object>());
                }

                // Assuming you have a Line_List table with Unit field
                var lines = _context.Set<Line_List>()
                    .Where(l => l.Unit == unitId && l.Factory == _factoryName)
                    .OrderBy(l => l.Line)
                    .Select(l => new { 
                        value = l.Line, 
                        text = l.Line 
                    })
                    .ToList();

                // _logger.LogInformation($"Found {lines.Count} lines for unit {unitId}");
                return Json(lines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting lines for unit {unitId}");
                return Json(new List<object>());
            }
        }

        private Dictionary<string, DefectCode_TopForm4> LoadDefectCodes(TopForm4QualityViewModel model)
        {
            var defectDict = new Dictionary<string, DefectCode_TopForm4>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("RP_ThongHopLoiCuoiChuyen", conn);
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
                    var name_VN = reader["Fault_Name_VN"]?.ToString()?.Trim();
                    var name_EN = reader["Fault_Name_EN"]?.ToString()?.Trim();
                    

                    if (!string.IsNullOrEmpty(code) && !defectDict.ContainsKey(code))
                    {
                        defectDict[code] = new DefectCode_TopForm4
                        {
                            Fault_Code = code,
                            Fault_Name_VN = name_VN,
                            Fault_Name_EN = name_EN
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

        private void LoadReportData(TopForm4QualityViewModel model)
        {
            // _logger.LogInformation("=== Loading Report Data ===");
            // _logger.LogInformation($"Parameters - DateFrom: {model.DateFrom:yyyy-MM-dd HH:mm:ss}, DateEnd: {model.DateEnd:yyyy-MM-dd HH:mm:ss}, Unit: '{model.Unit}'");

            try
            {
                using var connection = new SqlConnection(_connectionString);
                
                connection.Open();
                // _logger.LogInformation("Database connection opened successfully");
                string dateFStr = model.DateFrom.ToString("yyyy-MM-dd");
                string dateTStr = model.DateEnd.ToString("yyyy-MM-dd");
                var userName = User.Identity?.Name;
                var Unit = model.Unit;
                var Line = model.Line;
                var reportDataList = new List<Dictionary<string, object>>();
                var defectCodeList = new Dictionary<string, DefectCode>();

                using (var command = new SqlCommand("RP_BCKCC_Report_Top5", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Date_F", dateFStr);
                    command.Parameters.AddWithValue("@Date_T", dateTStr);
                    
                    command.Parameters.AddWithValue("@Unit", model.Unit == "ALL" ? "" : model.Unit);
                    command.Parameters.AddWithValue("@Line", Line);
                    

                    using var reader = command.ExecuteReader();
                    
                    {
                        while(reader.Read())
                        {
                            var rowData = new Dictionary<string, object>();
                      
                            rowData["Unit"] = reader["Unit"]?.ToString() ?? "";
                            rowData["Line"] = reader["Line"]?.ToString() ?? "";
   
                            rowData["Qty"] = reader["Qty"] != DBNull.Value ? Convert.ToInt32(reader["Qty"]) : 0;
           
                            rowData["Total_Fault_Qty"] = reader["Total_Fault_Qty"] != DBNull.Value ? Convert.ToInt32(reader["Total_Fault_Qty"]) : 0;
                            rowData["FullName"] = reader["FullName"]?.ToString() ?? "";
                            rowData["Detail_infor"] = reader["Detail_infor"]?.ToString() ?? "";
                            rowData["Total_Qty"] = reader["Total_Qty"] != DBNull.Value ? Convert.ToInt32(reader["Total_Qty"]) : 0;
                           

                            var detailInfo = reader["Detail_infor"]?.ToString() ?? "";
                            var faultCodesArray = detailInfo.Split(';', StringSplitOptions.RemoveEmptyEntries);

                            string defectName = "";
                            string defectNameEN = "";

                            foreach (var faultCode in faultCodesArray)
                            {
                                // Ví dụ faultCode = "F51-1-2"
                                var faultCodeParts = faultCode.Split('-');
                                if (faultCodeParts.Length > 0 && !string.IsNullOrWhiteSpace(faultCodeParts[0]))
                                {
                                    string code = faultCodeParts[0];

                                    // Tìm trong danh sách arrDefects
                                    if (model.DefectCodes.TryGetValue(code, out var defect))
                                    {
                                        defectName += defect.Fault_Name_VN + ";";
                                        defectNameEN += defect.Fault_Name_EN + ";";
                                    }
                                }
                            }

                            // Xóa dấu ";" ở cuối
                            defectName = defectName.TrimEnd(';');
                            defectNameEN = defectNameEN.TrimEnd(';');

                            rowData["defectName"] = defectName; // ✅ Tên lỗi thực tế
                            rowData["defectNameEN"] = defectNameEN;

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

        public IActionResult ExportExcel(string? Unit,string? Line, DateTime? dateFrom, DateTime? dateEnd)
        {
            var model = new TopForm4QualityViewModel
                {
                    Unit_List = GetUnitList(),
                    Unit = Unit ?? "ALL",
                    Line = Line ?? "ALL",
                    DateFrom = dateFrom ?? DateTime.Now,
                    DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1),
                    
                    
                };

            model.DefectCodes = LoadDefectCodes(model);
            // LoadReportData(model);
            string dateFStr = model.DateFrom.ToString("yyyy-MM-dd");
            string dateTStr = model.DateEnd.ToString("yyyy-MM-dd");

            // Tạo Excel file (ví dụ với ClosedXML hoặc EPPlus)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Đường dẫn tới file template
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Report", "RP_Top_Defect_Endline.xlsx");
            
            if (!System.IO.File.Exists(templatePath))
            {
                MessageStatus = "Không tìm thấy file mẫu báo cáo.";
                return RedirectToAction("TopForm6Quality");
            }
            //  using var package = new ExcelPackage(new FileInfo(templatePath));
            using (var package = new ExcelPackage(new FileInfo(templatePath)))
            {
                // var worksheet = package.Workbook.Worksheets.Add("Report");
                var worksheet = package.Workbook.Worksheets[0];

                int row = 9;
                // int col = 1;
                // worksheet.Cells[1, 1].Value = "Báo cáo Inline" ;
                worksheet.Cells["B6"].Value = (Unit ?? "ALL");
                worksheet.Cells["B7"].Value = (Line ?? "ALL");
                worksheet.Cells["E6"].Value = $"{(dateFrom ?? DateTime.Now):dd/MM/yyyy} - {(dateEnd ?? DateTime.Now):dd/MM/yyyy}" ;
                // worksheet.Cells["F6"].Value = $"{(dateEnd ?? DateTime.Now):dd/MM/yyyy}" ;
                // ==== Header ====
              
                

                // ==== Format chung ====
                worksheet.Cells.AutoFitColumns();
                // worksheet.View.FreezePanes(2, 3); // cố định header + 2 cột đầu
                worksheet.Cells.Style.Font.Name = "Calibri";
                // worksheet.Cells.Style.Font.Size = 11;
                // worksheet.Cells[1,1].Style.Font.Size = 14;
                // worksheet.Cells[2,1].Style.Font.Size = 14;
                // worksheet.Cells[1,1].Style.Font.Bold = true;
                // worksheet.Cells[2,1].Style.Font.Bold = true;

                // Lấy dữ liệu báo cáo
                using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                using var cmd = new SqlCommand("RP_BCKCC_Report_Top5", conn);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Date_F", dateFStr);
                cmd.Parameters.AddWithValue("@Date_T", dateTStr);
                cmd.Parameters.AddWithValue("@Unit", model.Unit == "ALL" ? "" : model.Unit);
                cmd.Parameters.AddWithValue("@Line", Line);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    
                    while (reader.Read())
                    {
                        worksheet.Cells["E7"].Value = reader["Total_Qty"].ToString(); 
                        worksheet.Cells[row, 1].Value = reader["FullName"].ToString();
                       
                        worksheet.Cells[row, 2].Value = reader["Qty"].ToString();
                        worksheet.Cells[row, 3].Value = reader["Total_Fault_Qty"].ToString();
                       
                        var checkQty = reader["Total_Qty"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Total_Qty"]);
                        var totalDefect = reader["Total_Fault_Qty"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Total_Fault_Qty"]);

                        double OQL = (checkQty == 0) ? 0 : (totalDefect / checkQty) * 100;
                        OQL = Math.Round(OQL, 2); // 🔹 chỉ giữ 2 số thập phân

                        var detailInfo = reader["Detail_infor"]?.ToString() ?? "";
                        var faultCodesArray = detailInfo.Split(';', StringSplitOptions.RemoveEmptyEntries);

                        string defectName = "";
                        string defectNameEN = "";

                        foreach (var faultCode in faultCodesArray)
                        {
                            // Ví dụ faultCode = "F51-1-2"
                            var faultCodeParts = faultCode.Split('-');
                            if (faultCodeParts.Length > 0 && !string.IsNullOrWhiteSpace(faultCodeParts[0]))
                            {
                                string code = faultCodeParts[0];

                                // Tìm trong danh sách arrDefects
                                if (model.DefectCodes.TryGetValue(code, out var defect))
                                {
                                    defectName += defect.Fault_Name_VN + ";";
                                    defectNameEN += defect.Fault_Name_EN + ";";
                                }
                            }
                        }

                        // Xóa dấu ";" ở cuối
                        defectName = defectName.TrimEnd(';');
                        defectNameEN = defectNameEN.TrimEnd(';');


                        worksheet.Cells[row, 4].Value = OQL +"%" ;

                        // _logger.LogInformation("defectName : " + defectName);
                        worksheet.Cells[row, 5].Value = defectName;
                        worksheet.Cells[row, 6].Value = defectNameEN;
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