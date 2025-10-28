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
using Microsoft.Extensions.Options;

namespace QOS.Areas.Report.Controllers
{
    [Authorize] // chỉ khi login mới được vào
    [Area("Report")]
    public class EndlineReportController : Controller
    {
        private readonly ILogger<EndlineReportController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly AppDbContext _context;
        private readonly AppSettings _appSettings;

        public EndlineReportController(ILogger<EndlineReportController> logger, IWebHostEnvironment env, IConfiguration configuration, AppDbContext context, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _env = env;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Missing connection string: DefaultConnection");
            _configuration =configuration;
            _context = context;
            _appSettings = appSettings.Value;
            
        }
        public IActionResult Index()
        {
            // return View();
            return RedirectToAction("EndlineReport", "EndlineReport");
        }
        [TempData]
        public string? MessageStatus { get; set;}
        [HttpGet]
        public IActionResult EndlineReport(string? Unit, string? Mo, string? Color, DateTime? dateFrom, DateTime? dateEnd)
        {
            _logger.LogInformation("=== EndlineReport GET Request ===" + _appSettings.FactoryName );
            // _logger.LogInformation($"Parameters - Unit: '{Unit}', DateFrom: {dateFrom}, DateEnd: {dateEnd}, Line: '{Line}' ");
            try
            {
                var model = new EndlineReportViewModel
                {
                    Unit_List = GetUnitList(),
                    
                    Unit = Unit ?? "ALL",
                    Mo = Mo ?? "",
                    Color = Color ?? "",
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
                
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return View(new EndlineReportViewModel { Unit_List = GetUnitList() });
            }
        }
        private List<Unit_List> GetUnitList()
        {
            try
            {
                var units = _context.Set<Unit_List>()
                    .Where(u => u.Factory == _appSettings.FactoryName)
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

        private Dictionary<string, DefectCode_EndlineReport> LoadDefectCodes(EndlineReportViewModel model)
        {
            var defectDict = new Dictionary<string, DefectCode_EndlineReport>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("RP_ThongHopLoiCuoiChuyen_MO", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Date_From", model.DateFrom.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@Date_To", model.DateEnd.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@Unit", string.IsNullOrEmpty(model.Unit) ? "ALL" : model.Unit);
                cmd.Parameters.AddWithValue("@Line","ALL");
                cmd.Parameters.AddWithValue("@MO","");
                cmd.Parameters.AddWithValue("@StyleCode","");
                cmd.Parameters.AddWithValue("@Defected_Type","ALL");
                cmd.Parameters.AddWithValue("@Top_Defected","ALL");
                cmd.Parameters.AddWithValue("@Seach","");

                conn.Open();
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var code = reader["Fault_Code"]?.ToString()?.Trim();
                    var name_VN = reader["Fault_Name_VN"]?.ToString()?.Trim();
                    var name_EN = reader["Fault_Name_EN"]?.ToString()?.Trim();
                    // _logger.LogInformation("=== Loading Report Data name_VN ===" + name_VN);

                    if (!string.IsNullOrEmpty(code) && !defectDict.ContainsKey(code))
                    {
                        defectDict[code] = new DefectCode_EndlineReport
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

        private void LoadReportData(EndlineReportViewModel model)
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
                var Line = "ALL";
                var mo = string.IsNullOrEmpty(model.Mo) ? "" : model.Mo;
                var color = string.IsNullOrEmpty(model.Color) ? "" : model.Color;
                var reportDataList = new List<Dictionary<string, object>>();
                var defectCodeList = new Dictionary<string, DefectCode>();

                using (var command = new SqlCommand("RP_BaoCaoChatLuongChecker_DownloadExcel", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Date_F", dateFStr);
                    command.Parameters.AddWithValue("@Date_T", dateTStr);
                    
                    command.Parameters.AddWithValue("@Unit", model.Unit == "ALL" ? "" : model.Unit);
                    command.Parameters.AddWithValue("@Line", Line);
                    command.Parameters.AddWithValue("@MO", mo);
                    command.Parameters.AddWithValue("@Color", color);
                    

                    using var reader = command.ExecuteReader();
                    
                    {
                        while(reader.Read())
                        {
                            var rowData = new Dictionary<string, object>();
                            // rowData["ID_L"] = reader["ID_L"] != DBNull.Value ? Convert.ToInt32(reader["ID_L"]) : 0;
                            rowData["Unit"] = reader["Unit"]?.ToString() ?? "";
                            rowData["Line"] = reader["Line"]?.ToString() ?? "";
                            rowData["Sewer"] = reader["Sewer"]?.ToString() ?? "";
                            rowData["MO"] = reader["MO"]?.ToString() ?? "";
                            rowData["Color"] = reader["Color"]?.ToString() ?? "";
                            rowData["Size"] = reader["Size"]?.ToString() ?? "";
                            // rowData["WorkDate"] = reader["WorkDate"] == DBNull.Value ? "" : Convert.ToDateTime(reader["WorkDate"]).ToString("yyyy-MM-dd");
                            rowData["Qty"] = reader["Qty"] != DBNull.Value ? Convert.ToInt32(reader["Qty"]) : 0;
                            rowData["Check_Qty"] = reader["Check_Qty"] != DBNull.Value ? Convert.ToInt32(reader["Check_Qty"]) : 0;
                            rowData["Total_Fault_QTY"] = reader["Total_Fault_QTY"] != DBNull.Value ? Convert.ToInt32(reader["Total_Fault_QTY"]) : 0;

                            rowData["Fault_Detail"] = reader["Fault_Detail"]?.ToString() ?? "";
                            rowData["UserUpdate"] = reader["UserUpdate"]?.ToString() ?? "";
                            rowData["UserUpdate_Name"] = reader["UserUpdate_Name"]?.ToString() ?? "";
                            rowData["Sewer_Name"] = reader["Sewer_Name"]?.ToString() ?? "";
                            rowData["TotalQty"] = reader["TotalQty"] != DBNull.Value ? Convert.ToInt32(reader["TotalQty"]) : 0;
                            rowData["LastUpdate"] = reader["LastUpdate"] == DBNull.Value ? "" : Convert.ToDateTime(reader["LastUpdate"]).ToString("yyyy-MM-dd");
                           

                            var detailInfo = reader["Fault_Detail"]?.ToString() ?? "";
                            var faultCodesArray = detailInfo.Split(';', StringSplitOptions.RemoveEmptyEntries);

                            string defectName = "";
                            string defectNameEN = "";

                            foreach (var faultCode in faultCodesArray)
                            {
                                // Ví dụ faultCode = "F51-1-2"
                                var faultCodeParts = faultCode.Split('-');
                                // _logger.LogInformation("faultCodeParts: " + faultCodeParts[0]);
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
                            // _logger.LogInformation("defectName: " + defectName);

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

        public IActionResult ExportExcel(string? Unit,string? Mo,string? Color, DateTime? dateFrom, DateTime? dateEnd)
        {
            var model = new EndlineReportViewModel
                {
                    Unit_List = GetUnitList(),
                    
                    Unit = Unit ?? "ALL",
                    Mo = Mo ?? "",
                    Color = Color ?? "",
                    DateFrom = dateFrom ?? DateTime.Now,
                    DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1)
                    
                };

            model.DefectCodes = LoadDefectCodes(model);
            // LoadReportData(model);
            string dateFStr = model.DateFrom.ToString("yyyy-MM-dd");
            string dateTStr = model.DateEnd.ToString("yyyy-MM-dd");
            var mo = string.IsNullOrEmpty(model.Mo) ? "" : model.Mo;
            var color = string.IsNullOrEmpty(model.Color) ? "" : model.Color;
            var Line = "ALL";
            using var conn_total = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            using var command = new SqlCommand("RP_BaoCaoChatLuongChecker_DownloadExcel_Total", conn_total);

            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Date_F", dateFStr);
            command.Parameters.AddWithValue("@Date_T", dateTStr);
            command.Parameters.AddWithValue("@Unit", model.Unit == "ALL" ? "" : model.Unit);
            command.Parameters.AddWithValue("@Line", Line);
            command.Parameters.AddWithValue("@MO", mo);
            command.Parameters.AddWithValue("@Color", color);

            conn_total.Open();
            var reportDataList = new List<Dictionary<string, object>>();
            using (var reader = command.ExecuteReader())
            {
                
                while (reader.Read())
                {
                    var rowData = new Dictionary<string, object>();
                    rowData["Unit"] = reader["Unit"]?.ToString() ?? "";
                    rowData["Sewer"] = reader["Sewer"]?.ToString() ?? "";
                    rowData["Qty"] = reader["Qty"] != DBNull.Value ? Convert.ToInt32(reader["Qty"]) : 0;
                    rowData["Check_Qty"] = reader["Check_Qty"] != DBNull.Value ? Convert.ToInt32(reader["Check_Qty"]) : 0;
                    rowData["Total_Fault_QTY"] = reader["Total_Fault_QTY"] != DBNull.Value ? Convert.ToInt32(reader["Total_Fault_QTY"]) : 0;
                    rowData["TotalQty"] = reader["TotalQty"] != DBNull.Value ? Convert.ToInt32(reader["TotalQty"]) : 0;
                    rowData["Sewer_Name"] = reader["Sewer_Name"]?.ToString() ?? "";

                    var checkQty_total = reader["Check_Qty"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Check_Qty"]);
                    var totalDefect_total = reader["Total_Fault_QTY"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Total_Fault_QTY"]);

                    double OQL_total = (checkQty_total == 0) ? 0 : (totalDefect_total / checkQty_total) * 100;
                    OQL_total = Math.Round(OQL_total, 2); // 🔹 chỉ giữ 2 số thập phân
                    rowData["OQL_total"] = OQL_total;

                    reportDataList.Add(rowData);
                }
            }
            // Tạo Excel file (ví dụ với ClosedXML hoặc EPPlus)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Đường dẫn tới file template
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Report", "RP_EndLine_Detail_Report.xlsx");
            
            if (!System.IO.File.Exists(templatePath))
            {
                MessageStatus = "Không tìm thấy file mẫu báo cáo.";
                return RedirectToAction("EndlineReport");
            }
            //  using var package = new ExcelPackage(new FileInfo(templatePath));
            using (var package = new ExcelPackage(new FileInfo(templatePath)))
            {
                // var worksheet = package.Workbook.Worksheets.Add("Report");
                var worksheet = package.Workbook.Worksheets[0];
             

                int row = 9;
                int ro_count = 2;
                // worksheet.Cells[1, 1].Value = "Báo cáo Inline" ;
                worksheet.Cells["B6"].Value = (Unit ?? "ALL");
                worksheet.Cells["B7"].Value = (Line ?? "ALL");
                worksheet.Cells["E6"].Value = $"{(dateFrom ?? DateTime.Now):dd/MM/yyyy}" ;
                worksheet.Cells["F6"].Value = $"{(dateEnd ?? DateTime.Now):dd/MM/yyyy}" ;
                // ==== Header ====
              
                

                // ==== Format chung ====
                // worksheet.Cells.AutoFitColumns();
                // worksheet.View.FreezePanes(2, 3); // cố định header + 2 cột đầu
                worksheet.Cells.Style.Font.Name = "Calibri";
                // worksheet.Cells.Style.Font.Size = 11;
                // worksheet.Cells[1,1].Style.Font.Size = 14;
                // worksheet.Cells[2,1].Style.Font.Size = 14;
                // worksheet.Cells[1,1].Style.Font.Bold = true;
                // worksheet.Cells[2,1].Style.Font.Bold = true;

                // Lấy dữ liệu báo cáo
                using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                using var cmd = new SqlCommand("RP_BaoCaoChatLuongChecker_DownloadExcel", conn);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Date_F", dateFStr);
                cmd.Parameters.AddWithValue("@Date_T", dateTStr);
                cmd.Parameters.AddWithValue("@Unit", model.Unit == "ALL" ? "" : model.Unit);
                cmd.Parameters.AddWithValue("@Line", Line);
                cmd.Parameters.AddWithValue("@MO", mo);
                cmd.Parameters.AddWithValue("@Color", color);

                conn.Open();
                string lastSewer = null;
                var UserUpdate_Name = "";

                using (var reader = cmd.ExecuteReader())
                {
                    
                    while (reader.Read())
                    {
                        string sewer = reader["Sewer"] == DBNull.Value ? null : reader["Sewer"].ToString();
                        if(lastSewer != null && lastSewer != sewer)
                        {
                            foreach (var data_total in reportDataList)
                            {
                                if (lastSewer == data_total["Sewer"].ToString() )
                                {
                                    
                                    worksheet.Cells[ro_count, 1].Value = "Total";
                                    worksheet.Cells[ro_count, 2].Value = data_total["Unit"].ToString();
                                    worksheet.Cells[ro_count, 3].Value = "";
                                    worksheet.Cells[ro_count, 4].Value = "";
                                    worksheet.Cells[ro_count, 7].Value = data_total["Sewer"].ToString();
                                    worksheet.Cells[ro_count, 8].Value = data_total["Sewer_Name"].ToString();
                                    worksheet.Cells[ro_count, 9].Value = data_total["TotalQty"].ToString();
                                    worksheet.Cells[ro_count, 10].Value = data_total["Check_Qty"].ToString();
                                    worksheet.Cells[ro_count, 11].Value = data_total["Total_Fault_QTY"].ToString();
                                    worksheet.Cells[ro_count, 12].Value = data_total["OQL_total"].ToString();
                                    worksheet.Cells[ro_count, 13].Value = UserUpdate_Name;

                                    // Định dạng vùng A..O (cột A tới O)
                                    using (var range = worksheet.Cells[$"A{ro_count}:O{ro_count}"])
                                    {
                                        // In đậm chữ
                                        range.Style.Font.Bold = true;

                                        // Căn giữa
                                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                                        // Tô màu nền cam nhạt (HEX: #FFE0B3)
                                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                        range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#FFE0B3"));
                                    }
                                }
                                
                            }
                            ro_count++;
                        }
                        worksheet.Cells[ro_count, 1].Value = ro_count - 1; 
                        worksheet.Cells[ro_count, 2].Value = reader["Unit"].ToString();
                        worksheet.Cells[ro_count, 3].Value = reader["Line"].ToString();
                        worksheet.Cells[ro_count, 4].Value = reader["MO"].ToString();
                        worksheet.Cells[ro_count, 5].Value = reader["Color"].ToString();
                        worksheet.Cells[ro_count, 6].Value = reader["Size"].ToString();

                        worksheet.Cells[ro_count, 7].Value = reader["Sewer"].ToString();
                        worksheet.Cells[ro_count, 8].Value = reader["Sewer_Name"].ToString();
                        worksheet.Cells[ro_count, 9].Value = reader["TotalQty"].ToString();
                        worksheet.Cells[ro_count, 10].Value = reader["Check_Qty"].ToString();
                        worksheet.Cells[ro_count, 11].Value = reader["Total_Fault_QTY"].ToString();
                        worksheet.Cells[ro_count, 13].Value = reader["UserUpdate"].ToString()  + "  - " + reader["UserUpdate_Name"].ToString();
                        worksheet.Cells[ro_count, 15].Value = reader["LastUpdate"].ToString();

                        // reader["Check_Qty"] != DBNull.Value ? Convert.ToInt32(reader["Check_Qty"]) : 0;
                        var checkQty = reader["Check_Qty"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Check_Qty"]);
                        var totalDefect = reader["Total_Fault_QTY"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Total_Fault_QTY"]);

                        double OQL = (checkQty == 0) ? 0 : (totalDefect / checkQty) * 100;
                        OQL = Math.Round(OQL, 2); // 🔹 chỉ giữ 2 số thập phân

                        var detailInfo = reader["Fault_Detail"]?.ToString() ?? "";
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


                        worksheet.Cells[ro_count, 12].Value = OQL +"%" ;

                        // _logger.LogInformation("defectName : " + defectName);
                        worksheet.Cells[ro_count, 14].Value = defectName;
                        ro_count++;
                        lastSewer = sewer;
                        UserUpdate_Name = reader["UserUpdate_Name"] == DBNull.Value ? "" : reader["UserUpdate_Name"].ToString();
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