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
    public class EndlineUnitController : Controller
    {
        private readonly ILogger<EndlineUnitController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly AppDbContext _context;
        private readonly AppSettings _appSettings;
        private readonly CommonDataService _commonData;

        public EndlineUnitController(ILogger<EndlineUnitController> logger, IWebHostEnvironment env, IConfiguration configuration, AppDbContext context, IOptions<AppSettings> appSettings, CommonDataService commonData)
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
            return RedirectToAction("EndlineUnit", "EndlineUnit");
        }
        [TempData]
        public string? MessageStatus { get; set;}
        [HttpGet]
        public IActionResult EndlineUnit(string? Unit, string? Mo, string? Color, DateTime? dateFrom, DateTime? dateEnd)
        {
            _logger.LogInformation("=== EndlineUnit GET Request ===" + _appSettings.FactoryName );
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
                
                var Unit = model.Unit;
                var Line = "ALL";
                var mo = string.IsNullOrEmpty(model.Mo) ? "" : model.Mo;
                var color = string.IsNullOrEmpty(model.Color) ? "" : model.Color;
                var reportDataList = new List<Dictionary<string, object>>();
                var defectCodeList = new Dictionary<string, DefectCode>();

                using (var command = new SqlCommand("RP_BaoCaoChatLuongChecker_KCC", connection))
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
                            rowData["Fault_Level_3_Sum"] = reader["Fault_Level_3_Sum"] != DBNull.Value ? Convert.ToInt32(reader["Fault_Level_3_Sum"]) : 0;

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
        public IActionResult ExportExcel(string? Unit,string? Mo,string? Color, DateTime? dateFrom, DateTime? dateEnd)
        {
            var model = new EndlineUnitViewModel
                {
                    Unit_List = _commonData.GetUnitList(),
                    
                    Unit = Unit ?? "ALL",
                    Mo = Mo ?? "",
                    Color = Color ?? "",
                    DateFrom = dateFrom ?? DateTime.Now,
                    DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1)
                    
                };

            // model.DefectCodes = LoadDefectCodes(model);
            // LoadReportData(model);
            string dateFStr = model.DateFrom.ToString("yyyy-MM-dd");
            string dateTStr = model.DateEnd.ToString("yyyy-MM-dd");
            var mo = string.IsNullOrEmpty(model.Mo) ? "" : model.Mo;
            var color = string.IsNullOrEmpty(model.Color) ? "" : model.Color;
            var Line = "ALL";
            // Tạo Excel file (ví dụ với ClosedXML hoặc EPPlus)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Đường dẫn tới file template
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Report", "RP_BaoCaoChatLuongChecker_KCC.xlsx");
            
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
                using (var command = new SqlCommand("RP_BaoCaoChatLuongChecker_KCC", connection))
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
                                worksheet.Cells[lastRow, 3].Value = reader["TotalQTY"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalQTY"]);
                                worksheet.Cells[lastRow, 4].Value = reader["Check_Qty"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Check_Qty"]);
                                worksheet.Cells[lastRow, 5].Value = reader["Total_Fault_QTY"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Total_Fault_QTY"]);

                                // Nếu có các cột level 3, 1, 2
                                worksheet.Cells[lastRow, 6].Value = reader["Fault_Level_3_Sum"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Fault_Level_3_Sum"]);
                                worksheet.Cells[lastRow, 7].Value = reader["Fault_Level_1_Sum"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Fault_Level_1_Sum"]);
                                worksheet.Cells[lastRow, 8].Value = reader["Fault_Level_2_Sum"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Fault_Level_2_Sum"]);

                                var checkQty_total = reader["Check_Qty"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Check_Qty"]);
                                var totalDefect_total = reader["Total_Fault_QTY"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Total_Fault_QTY"]);

                                double OQL_total = (checkQty_total == 0) ? 0 : (totalDefect_total / checkQty_total);
                                OQL_total = Math.Round(OQL_total, 2); // 🔹 chỉ giữ 2 số thập phân

                                // OQL chia 100 để ra tỷ lệ
                                worksheet.Cells[lastRow, 9].Value = OQL_total;
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
    }
}