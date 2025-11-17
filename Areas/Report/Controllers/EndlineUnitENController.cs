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
using QOS.Services;

namespace QOS.Areas.Report.Controllers
{
    [Authorize] // chỉ khi login mới được vào
    [Area("Report")]
    public class EndlineUnitENController : Controller
    {
        private readonly ILogger<EndlineUnitENController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly AppDbContext _context;
        private readonly AppSettings _appSettings;
        private readonly CommonDataService _commonData;

        public EndlineUnitENController(ILogger<EndlineUnitENController> logger, IWebHostEnvironment env, IConfiguration configuration, AppDbContext context, IOptions<AppSettings> appSettings, CommonDataService commonData)
        {
            _logger = logger;
            _env = env;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Missing connection string: DefaultConnection");
            _configuration =configuration;
            _context = context;
            _appSettings = appSettings.Value;
            _commonData = commonData;
            
        }
        public IActionResult Index()
        {
            // return View();
            return RedirectToAction("EndlineUnitEN", "EndlineUnitEN");
        }
        [TempData]
        public string? MessageStatus { get; set;}
        [HttpGet]
        public IActionResult EndlineUnitEN(string? Unit, string? Mo, string? Color, DateTime? dateFrom, DateTime? dateEnd)
        {
            _logger.LogInformation("=== EndlineUnitEN GET Request ===" + _appSettings.FactoryName );
            // _logger.LogInformation($"Parameters - Unit: '{Unit}', DateFrom: {dateFrom}, DateEnd: {dateEnd}, Line: '{Line}' ");
            try
            {
                var model = new EndlineUnitViewModel
                {
                    Unit_List = _commonData.GetUnitList(),
                    Customer_List = _commonData.GetZoneList(),
                    
                    Unit = Unit ?? "ALL",
                    Mo = Mo ?? "",
                    Color = Color ?? "",
                    DateFrom = dateFrom ?? DateTime.Now,
                    DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1),
                    ReportData = new List<Dictionary<string, object>>(),
                    // DefectStats = new Dictionary<string, DefectStat>()
                    
                };
                // model.DefectCodes = LoadDefectCodes(model);
                LoadReportData(model);
                // _logger.LogInformation($"Model created - Units available: {model.Unit_List.Count}");
                // LoadReportData(model);
                return View(model);
            }
            catch (Exception ex)
            {
                
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return View(new EndlineUnitViewModel { Unit_List = _commonData.GetUnitList() });
            }
        }
        

        private Dictionary<string, DefectCode_EndlineUnit> LoadDefectCodes(EndlineUnitViewModel model)
        {
            var defectDict = new Dictionary<string, DefectCode_EndlineUnit>();

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
                        defectDict[code] = new DefectCode_EndlineUnit
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

        private void LoadReportData(EndlineUnitViewModel model)
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

                using (var command = new SqlCommand("RP_BaoCaoChatLuongChecker_KCC_KH", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Date_F", dateFStr);
                    command.Parameters.AddWithValue("@Date_T", dateTStr);
                    
                    command.Parameters.AddWithValue("@Unit", Unit);
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

                            // rowData["WorkDate"] = reader["WorkDate"] == DBNull.Value ? "" : Convert.ToDateTime(reader["WorkDate"]).ToString("yyyy-MM-dd");
                            rowData["QTY"] = reader["QTY"] != DBNull.Value ? Convert.ToInt32(reader["QTY"]) : 0;
                            rowData["Check_Qty"] = reader["Check_Qty"] != DBNull.Value ? Convert.ToInt32(reader["Check_Qty"]) : 0;
                            rowData["Total_Fault_QTY"] = reader["Total_Fault_QTY"] != DBNull.Value ? Convert.ToInt32(reader["Total_Fault_QTY"]) : 0;
                            rowData["TotalQTY"] = reader["TotalQTY"] != DBNull.Value ? Convert.ToInt32(reader["TotalQTY"]) : 0;
                            rowData["Fault_Level_1_Sum"] = reader["Fault_Level_1_Sum"] != DBNull.Value ? Convert.ToInt32(reader["Fault_Level_1_Sum"]) : 0;
                            rowData["Fault_Level_2_Sum"] = reader["Fault_Level_2_Sum"] != DBNull.Value ? Convert.ToInt32(reader["Fault_Level_2_Sum"]) : 0;
                            // rowData["Fault_Level_3_Sum"] = reader["Fault_Level_3_Sum"] != DBNull.Value ? Convert.ToInt32(reader["Fault_Level_3_Sum"]) : 0;

                            // rowData["Fault_Detail"] = reader["Fault_Detail"]?.ToString() ?? "";
                            // rowData["UserUpdate"] = reader["UserUpdate"]?.ToString() ?? "";
                            // rowData["UserUpdate_Name"] = reader["UserUpdate_Name"]?.ToString() ?? "";
                            // rowData["Sewer_Name"] = reader["Sewer_Name"]?.ToString() ?? "";
                            // rowData["TotalQty"] = reader["TotalQty"] != DBNull.Value ? Convert.ToInt32(reader["TotalQty"]) : 0;
                            // rowData["LastUpdate"] = reader["LastUpdate"] == DBNull.Value ? "" : Convert.ToDateTime(reader["LastUpdate"]).ToString("yyyy-MM-dd");

                            var checkQty_total = reader["Check_Qty"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Check_Qty"]);
                            var totalDefect_total = reader["Total_Fault_QTY"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Total_Fault_QTY"]);

                            double OQL_total = (checkQty_total == 0) ? 0 : (totalDefect_total / checkQty_total) * 100;
                            OQL_total = Math.Round(OQL_total, 0); // 🔹 chỉ giữ 2 số thập phân
                            rowData["OQL_total"] = OQL_total;

                   
                    
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
        
        public IActionResult EndlineSewer(DateTime? dateFrom, DateTime? dateEnd, string? Unit, string? Mo, string? Color, string? Line)
        {
            try
            {
                var model = new EndlineUnitViewModel
                {
                    Unit_List = _commonData.GetUnitList(),

                    Customer_List = _commonData.GetZoneList(),
                    
                    Unit = Unit ?? "ALL",
                    Line = Line ?? "ALL",
                    Mo = Mo ?? "",
                    Color = Color ?? "",
                    DateFrom = dateFrom ?? DateTime.Now,
                    DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1),
                    ReportData = new List<Dictionary<string, object>>(),
                    // DefectStats = new Dictionary<string, DefectStat>()
                    
                };
                // model.DefectCodes = LoadDefectCodes(model);
                LoadReportDataSewer(model);
                model.Line_List = _commonData.GetLineList(model.Unit);
                // _logger.LogInformation($"Model created - Units available: {model.Unit_List.Count}");
                // LoadReportData(model);
                return View(model);
            }
            catch (Exception ex)
            {
                
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return View(new EndlineUnitViewModel { Unit_List = _commonData.GetUnitList() });
            }

        }

        private void LoadReportDataSewer(EndlineUnitViewModel model)
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
                
                var Unit = model.Unit;
                var Line = model.Line;
                var mo = string.IsNullOrEmpty(model.Mo) ? "" : model.Mo;
                var color = string.IsNullOrEmpty(model.Color) ? "" : model.Color;
                var reportDataList = new List<Dictionary<string, object>>();
                var defectCodeList = new Dictionary<string, DefectCode>();

                using (var command = new SqlCommand("RP_BaoCaoChatLuongChecker_KCC_Unit_KH", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Date_F", dateFStr);
                    command.Parameters.AddWithValue("@Date_T", dateTStr);
                    
                    command.Parameters.AddWithValue("@Unit", Unit);
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
                            rowData["Sewer"] = reader["Sewer"]?.ToString() ?? "";
                            rowData["Sewer_Name"] = reader["Sewer_Name"]?.ToString() ?? "";

                            // rowData["WorkDate"] = reader["WorkDate"] == DBNull.Value ? "" : Convert.ToDateTime(reader["WorkDate"]).ToString("yyyy-MM-dd");
                            // rowData["QTY"] = reader["QTY"] != DBNull.Value ? Convert.ToInt32(reader["QTY"]) : 0;
                            rowData["Check_Qty"] = reader["Check_Qty"] != DBNull.Value ? Convert.ToInt32(reader["Check_Qty"]) : 0;
                            rowData["Total_Fault_QTY"] = reader["Total_Fault_QTY"] != DBNull.Value ? Convert.ToInt32(reader["Total_Fault_QTY"]) : 0;
                            rowData["TotalQTY"] = reader["TotalQTY"] != DBNull.Value ? Convert.ToInt32(reader["TotalQTY"]) : 0;
                            rowData["Fault_Level_1_Sum"] = reader["Fault_Level_1_Sum"] != DBNull.Value ? Convert.ToInt32(reader["Fault_Level_1_Sum"]) : 0;
                            rowData["Fault_Level_2_Sum"] = reader["Fault_Level_2_Sum"] != DBNull.Value ? Convert.ToInt32(reader["Fault_Level_2_Sum"]) : 0;
                            // rowData["Fault_Level_3_Sum"] = reader["Fault_Level_3_Sum"] != DBNull.Value ? Convert.ToInt32(reader["Fault_Level_3_Sum"]) : 0;

                            var checkQty_total = reader["Check_Qty"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Check_Qty"]);
                            var totalDefect_total = reader["Total_Fault_QTY"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Total_Fault_QTY"]);

                            double OQL_total = (checkQty_total == 0) ? 0 : (totalDefect_total / checkQty_total) * 100;
                            OQL_total = Math.Round(OQL_total, 0); // 🔹 chỉ giữ 2 số thập phân
                            rowData["OQL_total"] = OQL_total;

                   
                    
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

        public IActionResult ExportExcelSewer(string? unit,string? Mo,string? Color, DateTime? dateFrom, DateTime? dateEnd, string? line)
        {
            var model = new EndlineUnitViewModel
                {
                    Unit_List = _commonData.GetUnitList(),
                    
                    Unit = unit ?? "ALL",
                    Line = line ?? "ALL",
                    Mo = Mo ?? "",
                    Color = Color ?? "",
                    DateFrom = dateFrom ?? DateTime.Now,
                    DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1)
                    
                };

            string dateFStr = model.DateFrom.ToString("yyyy-MM-dd");
            string dateTStr = model.DateEnd.ToString("yyyy-MM-dd");
            
            var Unit = model.Unit;
            var Line = model.Line;
            var mo = string.IsNullOrEmpty(model.Mo) ? "" : model.Mo;
            var color = string.IsNullOrEmpty(model.Color) ? "" : model.Color;
            // Tạo Excel file (ví dụ với ClosedXML hoặc EPPlus)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Đường dẫn tới file template
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Report", "RP_BaoCaoChatLuongChecker_KCC_Sewer.xlsx");
            
            if (!System.IO.File.Exists(templatePath))
            {
                MessageStatus = "Không tìm thấy file mẫu báo cáo.";
                return RedirectToAction("EndlineUnit");
            }
            //  using var package = new ExcelPackage(new FileInfo(templatePath));
            using (var package = new ExcelPackage(new FileInfo(templatePath)))
            {
                // var worksheet = package.Workbook.Worksheets.Add("Report");
                var worksheet = package.Workbook.Worksheets[0];
                int lastRow = 2;

                // Lấy dữ liệu báo cáo
                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                connection.Open();
                using (var command = new SqlCommand("RP_BaoCaoChatLuongChecker_KCC_Unit_KH", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Date_F", dateFStr);
                    command.Parameters.AddWithValue("@Date_T", dateTStr);
                    
                    command.Parameters.AddWithValue("@Unit", Unit);
                    command.Parameters.AddWithValue("@Line", Line);
                    command.Parameters.AddWithValue("@MO", mo);
                    command.Parameters.AddWithValue("@Color", color);
                        

                        using var reader = command.ExecuteReader();
                        {
                            
                            while (reader.Read())
                            {
                                // Lấy dòng cuối hiện tại trong sheet (nếu bạn đang thêm tiếp)
                                // int lastRow = worksheet.Dimension?.End.Row + 1 ?? 2; // nếu sheet mới thì bắt đầu từ dòng 2
                                // _logger.LogInformation( "Unit + count: " + lastRow + "-- " + reader["Unit"]?.ToString() );
                                // Ghi dữ liệu vào các ô tương ứng
                                worksheet.Cells[lastRow, 1].Value = lastRow -1;
                                worksheet.Cells[lastRow, 2].Value = reader["Unit"]?.ToString() ?? "";
                                worksheet.Cells[lastRow, 3].Value = (reader["Sewer"]?.ToString() ?? "") + " - " + (reader["Sewer_Name"]?.ToString() ?? "");
                                worksheet.Cells[lastRow, 4].Value = reader["TotalQTY"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalQTY"]);
                                worksheet.Cells[lastRow, 5].Value = reader["Check_Qty"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Check_Qty"]);
                                worksheet.Cells[lastRow, 6].Value = reader["Total_Fault_QTY"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Total_Fault_QTY"]);

                                // Nếu có các cột level 3, 1, 2
                                // worksheet.Cells[lastRow, 7].Value = reader["Fault_Level_3_Sum"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Fault_Level_3_Sum"]);
                                worksheet.Cells[lastRow, 8].Value = reader["Fault_Level_1_Sum"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Fault_Level_1_Sum"]);
                                worksheet.Cells[lastRow, 9].Value = reader["Fault_Level_2_Sum"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Fault_Level_2_Sum"]);

                                var checkQty_total = reader["Check_Qty"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Check_Qty"]);
                                var totalDefect_total = reader["Total_Fault_QTY"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Total_Fault_QTY"]);

                                double OQL_total = (checkQty_total == 0) ? 0 : (totalDefect_total / checkQty_total);
                                OQL_total = Math.Round(OQL_total, 2); // 🔹 chỉ giữ 2 số thập phân

                                // OQL chia 100 để ra tỷ lệ
                                worksheet.Cells[lastRow, 10].Value = OQL_total;
                                // worksheet.Cells[lastRow, 9].Style.Numberformat.Format = "0.00%"; // hiển thị dạng phần trăm
                                lastRow ++;

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

        public IActionResult EndlineSewerDetail(DateTime? dateFrom, DateTime? dateEnd, string? Unit, string? Mo, string? Color, string? Sewer)
        {
            try
            {
                var model = new EndlineUnitViewModel
                {
                    Unit_List = _commonData.GetUnitList(),

                    Customer_List = _commonData.GetZoneList(),
                    
                    Unit = Unit ?? "ALL",
                    Line = Sewer ?? "ALL",
                    Mo = Mo ?? "",
                    Color = Color ?? "",
                    DateFrom = dateFrom ?? DateTime.Now,
                    DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1),
                    ReportData = new List<Dictionary<string, object>>(),
                    // DefectStats = new Dictionary<string, DefectStat>()
                    
                };
                // model.DefectCodes = LoadDefectCodes(model);
                LoadReportDataSewerDetail(model);
               
                return View(model);
            }
            catch (Exception ex)
            {
                
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return View(new EndlineUnitViewModel { Unit_List = _commonData.GetUnitList() });
            }

        }
        private void LoadReportDataSewerDetail(EndlineUnitViewModel model)
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
                
                var Unit = model.Unit;
                var Line = model.Line;
                var mo = string.IsNullOrEmpty(model.Mo) ? "" : model.Mo;
                var color = string.IsNullOrEmpty(model.Color) ? "" : model.Color;
                var reportDataList = new List<Dictionary<string, object>>();
                var defectCodeList = new Dictionary<string, DefectCode>();

                using (var command = new SqlCommand("RP_BaoCaoChatLuongChecker_KCC_Sewer_Date_KH", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Date_F", dateFStr);
                    command.Parameters.AddWithValue("@Date_T", dateTStr);
                    
                    command.Parameters.AddWithValue("@Unit", Unit);
                    command.Parameters.AddWithValue("@Line", "ALL");
                    command.Parameters.AddWithValue("@Sewer", Line);
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
                            rowData["Sewer_Name"] = reader["Sewer_Name"]?.ToString() ?? "";

                            // rowData["WorkDate"] = reader["WorkDate"] == DBNull.Value ? "" : Convert.ToDateTime(reader["WorkDate"]).ToString("yyyy-MM-dd");
                            rowData["Qty"] = reader["Qty"] != DBNull.Value ? Convert.ToInt32(reader["Qty"]) : 0;
                            rowData["Check_Qty"] = reader["Check_Qty"] != DBNull.Value ? Convert.ToInt32(reader["Check_Qty"]) : 0;
                            rowData["Total_Fault_QTY"] = reader["Total_Fault_QTY"] != DBNull.Value ? Convert.ToInt32(reader["Total_Fault_QTY"]) : 0;
                            // rowData["TotalQTY"] = reader["TotalQTY"] != DBNull.Value ? Convert.ToInt32(reader["TotalQTY"]) : 0;
                            rowData["Fault_Level_1_Sum"] = reader["Fault_Level_1_Sum"] != DBNull.Value ? Convert.ToInt32(reader["Fault_Level_1_Sum"]) : 0;
                            rowData["Fault_Level_2_Sum"] = reader["Fault_Level_2_Sum"] != DBNull.Value ? Convert.ToInt32(reader["Fault_Level_2_Sum"]) : 0;
                            // rowData["Fault_Level_3_Sum"] = reader["Fault_Level_3_Sum"] != DBNull.Value ? Convert.ToInt32(reader["Fault_Level_3_Sum"]) : 0;

                            rowData["LastUpdate"] = reader["LastUpdate"] == DBNull.Value ? "" : Convert.ToDateTime(reader["LastUpdate"]).ToString("yyyy-MM-dd");

                            var checkQty_total = reader["Check_Qty"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Check_Qty"]);
                            var totalDefect_total = reader["Total_Fault_QTY"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Total_Fault_QTY"]);

                            double OQL_total = (checkQty_total == 0) ? 0 : (totalDefect_total / checkQty_total) * 100;
                            OQL_total = Math.Round(OQL_total, 0); // 🔹 chỉ giữ 2 số thập phân
                            rowData["OQL_total"] = OQL_total;

                   
                    
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
        public IActionResult ExportExcelSewerDetail(DateTime? dateFrom, DateTime? dateEnd, string? unit, string? Mo, string? Color, string? Sewer)
        {
            var model = new EndlineUnitViewModel
                {
                    Unit_List = _commonData.GetUnitList(),
                    Customer_List = _commonData.GetZoneList(),
                    Unit = unit ?? "ALL",
                    Line = Sewer ?? "ALL",
                    Mo = Mo ?? "",
                    Color = Color ?? "",
                    DateFrom = dateFrom ?? DateTime.Now,
                    DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1),
                    
                };

            string dateFStr = model.DateFrom.ToString("yyyy-MM-dd");
            string dateTStr = model.DateEnd.ToString("yyyy-MM-dd");
            
            var Unit = model.Unit;
            var Line = model.Line;
            var mo = string.IsNullOrEmpty(model.Mo) ? "" : model.Mo;
            var color = string.IsNullOrEmpty(model.Color) ? "" : model.Color;
            // Tạo Excel file (ví dụ với ClosedXML hoặc EPPlus)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Đường dẫn tới file template
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Report", "RP_BaoCaoChecker_KCC_Sewer_Detail.xlsx");
            
            if (!System.IO.File.Exists(templatePath))
            {
                MessageStatus = "Không tìm thấy file mẫu báo cáo.";
                return RedirectToAction("EndlineUnit");
            }
            //  using var package = new ExcelPackage(new FileInfo(templatePath));
            using (var package = new ExcelPackage(new FileInfo(templatePath)))
            {
                // var worksheet = package.Workbook.Worksheets.Add("Report");
                var worksheet = package.Workbook.Worksheets[0];
                int lastRow = 2;

                // Lấy dữ liệu báo cáo
                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                connection.Open();
                using (var command = new SqlCommand("RP_BaoCaoChatLuongChecker_KCC_Sewer_Date_KH", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Date_F", dateFStr);
                    command.Parameters.AddWithValue("@Date_T", dateTStr);
                    command.Parameters.AddWithValue("@Unit", Unit);
                    command.Parameters.AddWithValue("@Line", "ALL");
                    command.Parameters.AddWithValue("@Sewer", Line);
                    command.Parameters.AddWithValue("@MO", mo);
                    command.Parameters.AddWithValue("@Color", color);
                        

                        using var reader = command.ExecuteReader();
                        {
                            
                            while (reader.Read())
                            {
                                // Ghi dữ liệu vào các ô tương ứng
                                worksheet.Cells[lastRow, 1].Value = lastRow -1;
                                worksheet.Cells[lastRow, 2].Value = reader["Unit"]?.ToString() ?? "";
                                worksheet.Cells[lastRow, 3].Value = reader["Line"]?.ToString() ?? "";
                                worksheet.Cells[lastRow, 4].Value = (reader["Sewer"]?.ToString() ?? "") + " - " + (reader["Sewer_Name"]?.ToString() ?? "");
                                // worksheet.Cells[lastRow, 4].Value = reader["TotalQTY"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalQTY"]);
                                worksheet.Cells[lastRow, 5].Value = reader["Check_Qty"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Check_Qty"]);
                                worksheet.Cells[lastRow, 6].Value = reader["Total_Fault_QTY"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Total_Fault_QTY"]);

                                // Nếu có các cột level 3, 1, 2
                                // worksheet.Cells[lastRow, 7].Value = reader["Fault_Level_3_Sum"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Fault_Level_3_Sum"]);
                                worksheet.Cells[lastRow, 8].Value = reader["Fault_Level_1_Sum"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Fault_Level_1_Sum"]);
                                worksheet.Cells[lastRow, 9].Value = reader["Fault_Level_2_Sum"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Fault_Level_2_Sum"]);

                                var checkQty_total = reader["Check_Qty"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Check_Qty"]);
                                var totalDefect_total = reader["Total_Fault_QTY"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Total_Fault_QTY"]);

                                double OQL_total = (checkQty_total == 0) ? 0 : (totalDefect_total / checkQty_total);
                                OQL_total = Math.Round(OQL_total, 2); // 🔹 chỉ giữ 2 số thập phân

                                // OQL chia 100 để ra tỷ lệ
                                worksheet.Cells[lastRow, 10].Value = OQL_total;
                                // worksheet.Cells[lastRow, 9].Style.Numberformat.Format = "0.00%"; // hiển thị dạng phần trăm

                                worksheet.Cells[lastRow, 11].Value = reader["LastUpdate"] == DBNull.Value ? "" : Convert.ToDateTime(reader["LastUpdate"]).ToString("yyyy-MM-dd");
                                lastRow ++;

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

        public IActionResult EndlineSewerDetailViewImg(DateTime? dateFrom, DateTime? dateEnd, string? Unit, string? Mo, string? Color, string? Sewer,string? DefectedType)
        {
            try
            {
                var model = new EndlineUnitViewModel
                {
                    Unit_List = _commonData.GetUnitList(),
                    Customer_List = _commonData.GetZoneList(),   
                    Unit = Unit ?? "ALL",
                    Line = Sewer ?? "ALL",
                    Mo = Mo ?? "",
                    Color = Color ?? "",
                    DateFrom = dateFrom ?? DateTime.Now,
                    DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1),
                    ReportData = new List<Dictionary<string, object>>(),
                    ReportDataSewerImg = new List<Dictionary<string, object>>(),
                    DefectedType = DefectedType ?? "ALL"
                    
                };
                // model.DefectCodes = LoadDefectCodes(model);
                LoadReportDataSewerDetailImg(model);
                LoadReportDataSewerDetail(model);
               
                return View(model);
            }
            catch (Exception ex)
            {
                
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return View(new EndlineUnitViewModel { Unit_List = _commonData.GetUnitList() });
            }

        }

        private void LoadReportDataSewerDetailImg(EndlineUnitViewModel model)
        {
            model.DefectCodes = LoadDefectCodes(model);
            try
            {
                using var connection = new SqlConnection(_connectionString);
                
                connection.Open();
                // _logger.LogInformation("Database connection opened successfully");
                string dateFStr = model.DateFrom.ToString("yyyy-MM-dd");
                string dateTStr = model.DateEnd.ToString("yyyy-MM-dd");
                var Unit = model.Unit;
                var Line = model.Line;
                var mo = string.IsNullOrEmpty(model.Mo) ? "" : model.Mo;
                var color = string.IsNullOrEmpty(model.Color) ? "" : model.Color;
                var reportDataList = new List<Dictionary<string, object>>();
                var defectCodeList = new Dictionary<string, DefectCode>();

                using (var command = new SqlCommand("RP_BaoCaoChatLuongChecker_KCC_Sewer_KH", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Date_F", dateFStr);
                    command.Parameters.AddWithValue("@Date_T", dateTStr);
                    command.Parameters.AddWithValue("@Unit", Unit);
                    command.Parameters.AddWithValue("@Line", "ALL");
                    command.Parameters.AddWithValue("@Sewer", Line);
                    command.Parameters.AddWithValue("@MO", mo);
                    command.Parameters.AddWithValue("@Color", color);
                    command.Parameters.AddWithValue("@Defect_Type", model.DefectedType);

                    using var reader = command.ExecuteReader();
                    
                    {
                        while(reader.Read())
                        {
                            var rowData = new Dictionary<string, object>();
                            // rowData["ID_L"] = reader["ID_L"] != DBNull.Value ? Convert.ToInt32(reader["ID_L"]) : 0;
                            rowData["Unit"] = reader["Unit"]?.ToString() ?? "";
                            rowData["Line"] = reader["Line"]?.ToString() ?? "";
                            rowData["Sewer"] = reader["Sewer"]?.ToString() ?? "";
                            rowData["Sewer_Name"] = reader["Sewer_Name"]?.ToString() ?? "";
                            rowData["MO"] = reader["MO"]?.ToString() ?? "";
                            rowData["Color"] = reader["Color"]?.ToString() ?? "";
                            rowData["Size"] = reader["Size"]?.ToString() ?? "";

                            // rowData["WorkDate"] = reader["WorkDate"] == DBNull.Value ? "" : Convert.ToDateTime(reader["WorkDate"]).ToString("yyyy-MM-dd");
                            rowData["Qty"] = reader["Qty"] != DBNull.Value ? Convert.ToInt32(reader["Qty"]) : 0;
                            rowData["Check_Qty"] = reader["Check_Qty"] != DBNull.Value ? Convert.ToInt32(reader["Check_Qty"]) : 0;
                            rowData["Total_Fault_QTY"] = reader["Total_Fault_QTY"] != DBNull.Value ? Convert.ToInt32(reader["Total_Fault_QTY"]) : 0;
                            rowData["TotalQty"] = reader["TotalQty"] != DBNull.Value ? Convert.ToInt32(reader["TotalQty"]) : 0;
                            // rowData["Fault_Level_1_Sum"] = reader["Fault_Level_1_Sum"] != DBNull.Value ? Convert.ToInt32(reader["Fault_Level_1_Sum"]) : 0;
                            // rowData["Fault_Level_2_Sum"] = reader["Fault_Level_2_Sum"] != DBNull.Value ? Convert.ToInt32(reader["Fault_Level_2_Sum"]) : 0;
                            // rowData["Fault_Level_3_Sum"] = reader["Fault_Level_3_Sum"] != DBNull.Value ? Convert.ToInt32(reader["Fault_Level_3_Sum"]) : 0;

                            rowData["Fault_Detail"] = reader["Fault_Detail"]?.ToString() ?? "";
                            rowData["UserUpdate"] = reader["UserUpdate"]?.ToString() ?? "";
                            rowData["UserUpdate_Name"] = reader["UserUpdate_Name"]?.ToString() ?? "";
                            // rowData["Photo_URL"] = reader["Photo_URL"]?.ToString() ?? "";
                            rowData["LastUpdate"] = reader["LastUpdate"] == DBNull.Value ? "" : Convert.ToDateTime(reader["LastUpdate"]).ToString("yyyy-MM-dd");

                            var checkQty_total = reader["Check_Qty"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Check_Qty"]);
                            var totalDefect_total = reader["Total_Fault_QTY"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Total_Fault_QTY"]);

                            double OQL_total = (checkQty_total == 0) ? 0 : (totalDefect_total / checkQty_total) * 100;
                            OQL_total = Math.Round(OQL_total, 0); // 🔹 chỉ giữ 2 số thập phân
                            rowData["OQL_total"] = OQL_total;

                            var detailInfo = reader["Fault_Detail"]?.ToString() ?? "";
                            // _logger.LogInformation("=== Fault_Detail ===" + detailInfo);
                            var faultCodesArray = detailInfo.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(code => code.Trim()).ToArray();
                            
                            string defectName = "";
                            string defectNameEN = "";

                            foreach (var faultCode in faultCodesArray)
                            {
                                // Ví dụ faultCode = "F51-1-2"
                                // var faultCodeParts = faultCode.Split('-');
                                // if (faultCodeParts.Length > 0 && !string.IsNullOrWhiteSpace(faultCodeParts[0]))
                                // {
                                //     string code = faultCodeParts[0];

                                    // Tìm trong danh sách arrDefects
                                    if (model.DefectCodes.TryGetValue(faultCode, out var defect))
                                    {
                                        defectName += defect.Fault_Name_VN + ";";
                                        defectNameEN += defect.Fault_Name_EN + ";";
                                    }
                                // }
                            }

                            // Xóa dấu ";" ở cuối
                            defectName = defectName.TrimEnd(';');
                            defectNameEN = defectNameEN.TrimEnd(';');

                            rowData["defectName"] = defectName; // ✅ Tên lỗi thực tế
                            rowData["defectNameEN"] = defectNameEN;

                            reportDataList.Add(rowData);

                        }
                       
                        model.ReportDataSewerImg = reportDataList;
                    }
                }

                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading report data");
                
                throw;
            }
        }

        public IActionResult ExportExcelSewerDetailImg(DateTime? dateFrom, DateTime? dateEnd, string? unit, string? Mo, string? Color, string? Sewer,string? DefectedType)
        {
            var model = new EndlineUnitViewModel
                {
                    Unit = unit ?? "ALL",
                    Line = Sewer ?? "ALL",
                    Mo = Mo ?? "",
                    Color = Color ?? "",
                    DateFrom = dateFrom ?? DateTime.Now,
                    DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1),
                    DefectedType = DefectedType ?? "ALL"
                    
                };
            model.DefectCodes = LoadDefectCodes(model);

            string dateFStr = model.DateFrom.ToString("yyyy-MM-dd");
            string dateTStr = model.DateEnd.ToString("yyyy-MM-dd");
            var Unit = model.Unit;
            var Line = model.Line;
            var mo = string.IsNullOrEmpty(model.Mo) ? "" : model.Mo;
            var color = string.IsNullOrEmpty(model.Color) ? "" : model.Color;
            // Tạo Excel file (ví dụ với ClosedXML hoặc EPPlus)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Đường dẫn tới file template
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Report", "RP_BaoCaoChecker_KCC_Sewer_Detail_D.xlsx");
            
            if (!System.IO.File.Exists(templatePath))
            {
                MessageStatus = "Không tìm thấy file mẫu báo cáo.";
                return RedirectToAction("EndlineUnit");
            }
            //  using var package = new ExcelPackage(new FileInfo(templatePath));
            using (var package = new ExcelPackage(new FileInfo(templatePath)))
            {
                // var worksheet = package.Workbook.Worksheets.Add("Report");
                var worksheet = package.Workbook.Worksheets[0];
                int lastRow = 2;

                // Lấy dữ liệu báo cáo
                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                connection.Open();
 
                using (var command = new SqlCommand("RP_BaoCaoChatLuongChecker_KCC_Sewer_KH", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Date_F", dateFStr);
                    command.Parameters.AddWithValue("@Date_T", dateTStr);
                    command.Parameters.AddWithValue("@Unit", Unit);
                    command.Parameters.AddWithValue("@Line", "ALL");
                    command.Parameters.AddWithValue("@Sewer", Line);
                    command.Parameters.AddWithValue("@MO", mo);
                    command.Parameters.AddWithValue("@Color", color);
                    command.Parameters.AddWithValue("@Defect_Type", model.DefectedType);
                        
                        using var reader = command.ExecuteReader();
                        {
                            
                            while (reader.Read())
                            {
                                // Ghi dữ liệu vào các ô tương ứng
                                worksheet.Cells[lastRow, 1].Value = lastRow -1;
                                worksheet.Cells[lastRow, 2].Value = reader["Unit"]?.ToString() ?? "";
                                worksheet.Cells[lastRow, 3].Value = reader["Line"]?.ToString() ?? "";
                                worksheet.Cells[lastRow, 4].Value = reader["MO"]?.ToString() ?? "";
                                worksheet.Cells[lastRow, 5].Value = reader["Color"]?.ToString() ?? "";
                                worksheet.Cells[lastRow, 6].Value = reader["Size"]?.ToString() ?? "";
                                worksheet.Cells[lastRow, 7].Value = (reader["Sewer"]?.ToString() ?? "") + " - " + (reader["Sewer_Name"]?.ToString() ?? "");
                                worksheet.Cells[lastRow, 8].Value = reader["TotalQty"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalQty"]);
                                worksheet.Cells[lastRow, 9].Value = reader["Check_Qty"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Check_Qty"]);
                                worksheet.Cells[lastRow, 10].Value = reader["Total_Fault_QTY"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Total_Fault_QTY"]);
                                var checkQty_total = reader["Check_Qty"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Check_Qty"]);
                                var totalDefect_total = reader["Total_Fault_QTY"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Total_Fault_QTY"]);

                                double OQL_total = (checkQty_total == 0) ? 0 : (totalDefect_total / checkQty_total);
                                OQL_total = Math.Round(OQL_total, 2); // 🔹 chỉ giữ 2 số thập phân

                                // OQL chia 100 để ra tỷ lệ
                                worksheet.Cells[lastRow, 11].Value = OQL_total;
                                // worksheet.Cells[lastRow, 9].Style.Numberformat.Format = "0.00%"; // hiển thị dạng phần trăm
                                worksheet.Cells[lastRow, 12].Value = reader["UserUpdate_Name"]?.ToString() ?? "";
                                worksheet.Cells[lastRow, 14].Value = reader["LastUpdate"] == DBNull.Value ? "" : Convert.ToDateTime(reader["LastUpdate"]).ToString("yyyy-MM-dd");

                                var detailInfo = reader["Fault_Detail"]?.ToString() ?? "";
                                var faultCodesArray = detailInfo.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(code => code.Trim()).ToArray();
                                string defectName = "";
                                string defectNameEN = "";
                                foreach (var faultCode in faultCodesArray)
                                {
                                    // Tìm trong danh sách arrDefects
                                    if (model.DefectCodes.TryGetValue(faultCode, out var defect))
                                    {
                                        defectName += defect.Fault_Name_VN + ";";
                                        defectNameEN += defect.Fault_Name_EN + ";";
                                    }
                                }
                                // Xóa dấu ";" ở cuối
                                defectName = defectName.TrimEnd(';');
                                defectNameEN = defectNameEN.TrimEnd(';');
                                worksheet.Cells[lastRow, 13].Value = defectName;
                                lastRow ++;

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
}