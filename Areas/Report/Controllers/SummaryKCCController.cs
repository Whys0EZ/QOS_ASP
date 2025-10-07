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
    public class SummaryKCCController : Controller
    {
        private readonly ILogger<SummaryKCCController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly AppDbContext _context;

        public SummaryKCCController(ILogger<SummaryKCCController> logger, IWebHostEnvironment env, IConfiguration configuration, AppDbContext context)
        {
            _logger = logger;
            _env = env;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _configuration =configuration;
            _context = context;
        }
        public IActionResult Index()
        {
            // return View();
            return RedirectToAction("RP_Summary_Defects_KCC", "SummaryKCC");
        }
        [TempData]
        public string? MessageStatus { get; set;}
        [HttpGet]
        public IActionResult RP_Summary_Defects_KCC(string? topDefected, string? typeCode, string? Unit, string? Line, string? Mo, string? styleCode, DateTime? dateFrom, DateTime? dateEnd)
        {
            _logger.LogInformation("=== RP_Summary_Defects_KCC GET Request ===");
            _logger.LogInformation($"Parameters - Unit: '{Unit}', DateFrom: {dateFrom}, DateEnd: {dateEnd}" +
                $", Line: '{Line}', Mo: '{Mo}', StyleCode: '{styleCode}', TypeCode: '{typeCode}', TopDefected: '{topDefected}'");
            try
            {
                var model = new SummaryKCCViewModel
                {
                    Unit_List = GetUnitList(),
                    TopDefected = topDefected ?? "5",
                    TypeCode = typeCode ?? "ALL",
                    Unit = Unit ?? "ALL",
                    Line = Line ?? "ALL",
                    Mo = Mo ?? "",
                    StyleCode = styleCode ?? "",
                    DateFrom = dateFrom ?? DateTime.Now,
                    DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1),
                    ReportData = new List<Dictionary<string, object>>(),
                    DefectStats = new Dictionary<string, DefectStat>()
                    
                };
                _logger.LogInformation($"Model created - Units available: {model.Unit_List.Count}");
                LoadReportData(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RP_Summary_Defects_KCC GET");
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return View(new SummaryKCCViewModel { Unit_List = GetUnitList() });
            }
        }
        private List<Unit_List> GetUnitList()
        {
            try
            {
                var units = _context.Set<Unit_List>()
                    .Where(u => u.Factory == "REG2")
                    .OrderBy(u => u.Unit)
                    .ToList();

                _logger.LogInformation($"Loaded {units.Count} units from database");
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
            _logger.LogInformation($"GetLinesByUnit called with unitId: {unitId}");
            
            try
            {
                if (string.IsNullOrEmpty(unitId) || unitId == "ALL")
                {
                    return Json(new List<object>());
                }

                // Assuming you have a Line_List table with Unit field
                var lines = _context.Set<Line_List>()
                    .Where(l => l.Unit == unitId && l.Factory == "REG2")
                    .OrderBy(l => l.Line)
                    .Select(l => new { 
                        value = l.Line, 
                        text = l.Line 
                    })
                    .ToList();

                _logger.LogInformation($"Found {lines.Count} lines for unit {unitId}");
                return Json(lines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting lines for unit {unitId}");
                return Json(new List<object>());
            }
        }

        private void LoadReportData(SummaryKCCViewModel model)
        {
            try
            {
                _logger.LogInformation("=== LoadReportData Start ===");
                
                // Prepare parameters for stored procedure
                var dateFrom = model.DateFrom.ToString("yyyy-MM-dd");
                var dateEnd = model.DateEnd.ToString("yyyy-MM-dd");
                var unit = string.IsNullOrEmpty(model.Unit) ? "ALL" : model.Unit;
                var line = string.IsNullOrEmpty(model.Line) ? "" : model.Line;
                var mo = string.IsNullOrEmpty(model.Mo) ? "" : model.Mo;
                var styleCode = string.IsNullOrEmpty(model.StyleCode) ? "" : model.StyleCode;
                var defectedType = string.IsNullOrEmpty(model.TypeCode) ? "ALL" : model.TypeCode;
                var top = model.TopDefected.ToString();
                var search = "";

                _logger.LogInformation($"SP Parameters: DateFrom={dateFrom}, DateTo={dateEnd}, Unit={unit}, Line={line}, MO={mo}, StyleCode={styleCode}, Type={defectedType}, Top={top}");

                // Execute stored procedure
                var sql = @"EXEC RP_ThongHopLoiCuoiChuyen_MO @Date_From, @Date_To, @Unit, @Line, @MO, @StyleCode, @Defected_Type, @Top_Defected, @Search";
                
                var parameters = new[]
                {
                    new Microsoft.Data.SqlClient.SqlParameter("@Date_From", dateFrom),
                    new Microsoft.Data.SqlClient.SqlParameter("@Date_To", dateEnd),
                    new Microsoft.Data.SqlClient.SqlParameter("@Unit", unit),
                    new Microsoft.Data.SqlClient.SqlParameter("@Line", line),
                    new Microsoft.Data.SqlClient.SqlParameter("@MO", mo),
                    new Microsoft.Data.SqlClient.SqlParameter("@StyleCode", styleCode),
                    new Microsoft.Data.SqlClient.SqlParameter("@Defected_Type", defectedType),
                    new Microsoft.Data.SqlClient.SqlParameter("@Top_Defected", top),
                    new Microsoft.Data.SqlClient.SqlParameter("@Search", search)
                };

                // Get raw data from stored procedure
                var connection = _context.Database.GetDbConnection();
                var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Parameters.AddRange(parameters);
                
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    connection.Open();
                }

                var reader = command.ExecuteReader();
                var reportDataList = new List<Dictionary<string, object>>();
                var defectSummary = new Dictionary<string, DefectStat>();

                while (reader.Read())
                {
                    // Read data based on actual column names from SP
                    var rowData = new Dictionary<string, object>();
                    
                    // Map columns: Fault_Code, Fault_QTY, Fault_Level, Fault_Name_EN, Fault_Name_VN
                    var faultCode = reader["Fault_Code"]?.ToString() ?? "";
                    var faultQty = reader["Fault_QTY"] != DBNull.Value ? Convert.ToInt32(reader["Fault_QTY"]) : 0;
                    var faultLevel = reader["Fault_Level"]?.ToString() ?? "";
                    var faultNameEN = reader["Fault_Name_EN"]?.ToString() ?? "";
                    var faultNameVN = reader["Fault_Name_VN"]?.ToString() ?? "";

                    rowData["Fault_Code"] = faultCode;
                    rowData["Fault_QTY"] = faultQty;
                    rowData["Fault_Level"] = faultLevel;
                    rowData["Fault_Name_EN"] = faultNameEN;
                    rowData["Fault_Name_VN"] = faultNameVN;

                    reportDataList.Add(rowData);

                    // Aggregate by Fault_Name_VN for statistics
                    var defectKey = faultNameVN;
                    if (!string.IsNullOrEmpty(defectKey))
                    {
                        if (defectSummary.ContainsKey(defectKey))
                        {
                            defectSummary[defectKey].Count += faultQty;
                        }
                        else
                        {
                            defectSummary[defectKey] = new DefectStat
                            {
                                Name = defectKey,
                                Count = faultQty,
                                Percentage = 0 // Will calculate later
                            };
                        }
                    }
                }
                reader.Close();

                _logger.LogInformation($"SP returned {reportDataList.Count} records");

                // Calculate total and percentages
                model.TotalDefects = defectSummary.Sum(d => d.Value.Count);
                
                foreach (var stat in defectSummary.Values)
                {
                    stat.Percentage = model.TotalDefects > 0
                        ? Math.Round((decimal)stat.Count / model.TotalDefects * 100, 2)
                        : 0;
                }

                // Sort by count descending and take top N
                if (!string.Equals(model.TopDefected, "ALL", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(model.TopDefected, out int topN))
                {
                    model.DefectStats = defectSummary
                        .OrderByDescending(d => d.Value.Count)
                        .Take(topN)
                        .ToDictionary(d => d.Key, d => d.Value);
                }
                else
                {
                    // Nếu chọn ALL thì lấy hết
                    model.DefectStats = defectSummary
                        .OrderByDescending(d => d.Value.Count)
                        .ToDictionary(d => d.Key, d => d.Value);
                }

                model.ReportData = reportDataList;

                _logger.LogInformation($"Statistics calculated - Total: {model.TotalDefects}, Defect Types: {model.DefectStats.Count}");
                
                // Log sample data for debugging
                // if (model.DefectStats.Any())
                // {
                //     _logger.LogInformation("Top Defects:");
                //     foreach (var stat in model.DefectStats.Take(3))
                //     {
                //         _logger.LogInformation($"  - {stat.Key}: {stat.Value.Count} ({stat.Value.Percentage}%)");
                //     }
                // }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing stored procedure");
                model.ReportData = new List<Dictionary<string, object>>();
                model.DefectStats = new Dictionary<string, DefectStat>();
                model.TotalDefects = 0;
                throw;
            }
        }
        // Helper class for reading SP data
        private class DefectDataFromSP
        {
            public string DefectType { get; set; } = string.Empty;
            public int Quantity { get; set; }
        }


        [HttpGet]
        public IActionResult ExportToExcel( string? topDefected, string? typeCode, string? Unit, string? Line, string? Mo, string? styleCode, DateTime? dateFrom, DateTime? dateEnd, string faultCodes)
        {
            var model = new SummaryKCCViewModel
                {
                    Unit_List = GetUnitList(),
                    TopDefected = topDefected ?? "5",
                    TypeCode = typeCode ?? "ALL",
                    Unit = Unit ?? "ALL",
                    Line = Line ?? "ALL",
                    Mo = Mo ?? "",
                    StyleCode = styleCode ?? "",
                    DateFrom = dateFrom ?? DateTime.Now,
                    DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1),
                    ReportData = new List<Dictionary<string, object>>(),
                    DefectStats = new Dictionary<string, DefectStat>()
                    
                };
               
                LoadReportData(model);

            int countfaultCodes = faultCodes.Split(',').Length;
            string[] codes = faultCodes.Split(',');
            int col = 0;

            _logger.LogInformation(" Fault Code: " + faultCodes);
            
            _logger.LogInformation($"Parameters - Unit: '{Unit}', DateFrom: {dateFrom}, DateEnd: {dateEnd}" +
                $", Line: '{Line}', Mo: '{Mo}', StyleCode: '{styleCode}', TypeCode: '{typeCode}', TopDefected: '{topDefected}' ,' Fault Code: ' + '{faultCodes}'");

            // Tạo Excel file (ví dụ với ClosedXML hoặc EPPlus)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Đường dẫn tới file template
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Report", "B7_Summary_Endline_Defects.xlsx");
            
            if (!System.IO.File.Exists(templatePath))
            {
                MessageStatus = "Không tìm thấy file mẫu báo cáo.";
                return RedirectToAction("RP_Summary_Defects_KCC");
            }

            using var package = new ExcelPackage(new FileInfo(templatePath));
            var worksheet = package.Workbook.Worksheets[0];

            // Ghi tiêu đề báo cáo
            worksheet.Cells["A2"].Value = Unit ;
            worksheet.Cells["B2"].Value = (dateFrom ?? DateTime.Now).ToString("dd/MM/yyyy");
            worksheet.Cells["D2"].Value = (dateEnd ?? DateTime.Now).ToString("dd/MM/yyyy");
            if(Unit == "ALL")
            {
                worksheet.Cells["B3"].Value = "Unit" ;
            } else 
            {
                worksheet.Cells["B3"].Value = "Line" ; 
            }
            int i = 0;
            foreach (var code in codes)
            {
                col = i + 7;
                worksheet.Cells[3, col].Value = model.ReportData.Where(r => r["Fault_Code"].ToString() == code).Select(r => r["Fault_Name_VN"]).FirstOrDefault()?.ToString() ?? code;
                i++;
            }
            // worksheet.Cells["B3"].Style.Font.Bold = true;
            // worksheet.Cells[""].Style.Font.Size = 16;
            // worksheet.Cells["A2"].Style.Font.Bold = true;
            // worksheet.Cells["A2"].Style.Font.Size = 12;

            // Lấy dữ liệu báo cáo
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("RP_ThongHopLoiCuoiChuyen_DownloadExcelDetail_MO", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            
            cmd.Parameters.AddWithValue("@Date_From", dateFrom);
            cmd.Parameters.AddWithValue("@Date_To", dateEnd);
            cmd.Parameters.AddWithValue("@Unit", Unit);
            cmd.Parameters.AddWithValue("@Line", Line);
            cmd.Parameters.AddWithValue("@MO", Mo);
            cmd.Parameters.AddWithValue("@StyleCode", styleCode);
            cmd.Parameters.AddWithValue("@Defected_Type", typeCode);
            cmd.Parameters.AddWithValue("@Top_Defected", topDefected);
            cmd.Parameters.AddWithValue("@DefectList", faultCodes);
            

            conn.Open();
            int row = 4;
            col = 7;
            int lastColIndex = col + countfaultCodes - 1;
            string lastColLetter = GetExcelColumnName(lastColIndex);
            using (var reader = cmd.ExecuteReader())
            {

                while (reader.Read())
                {
                    // worksheet.Cells[row, 1].Value = row - 3; // STT
                    worksheet.Cells[row, 2].Value = reader["Unit"].ToString();
                    worksheet.Cells[row, 3].Value = reader["Check_QTY"];
                    

                    for (int k = 0; k < countfaultCodes; k++)
                    {
                        int cols = 7 + k; // cột G là 7, nên bắt đầu từ 7(nếu A=1)
                        string code = codes[k];

                        if (!reader.IsDBNull(reader.GetOrdinal(code)))
                            worksheet.Cells[row, cols].Value = reader[code];
                    }
                    // Tổng Defect theo hàng
                    worksheet.Cells[$"D{row}"].Formula = $"SUM(G{row}:{lastColLetter}{row})";

                    // % Defect = D/C
                    worksheet.Cells[$"F{row}"].Formula = $"D{row}/C{row}";
                    // col++;
                    row++;
                }

            }
            worksheet.Cells["A" + (row+1)].Value = "Total";
            worksheet.Cells["C" + (row+1)].Formula = $"SUM(C4:C{row})";
            worksheet.Cells["D" + (row+1)].Formula = $"SUM(D4:D{row})";
            worksheet.Cells["E" + (row+1)].Formula = $"SUM(E4:E{row})";
            worksheet.Cells["F" + (row+1)].Formula = $"D{row+1}/C{row+1}";
            worksheet.Cells["A" + (row+2)].Value = "OQL % based on total defect";
            for(int j = 0; j < countfaultCodes; j++)
            {
                int currentCol = j + 7;
                worksheet.Cells[row + 1, currentCol].Formula = $"SUM({currentCol}4:{currentCol}{row})";
                worksheet.Cells[row + 2, currentCol].Formula = $"{currentCol}{row + 1}/D{row + 1}";
            }

            // Tô màu nền từ G2 đến cột cuối cùng (dòng 2)
            var headerRange = worksheet.Cells[$"G3:{lastColLetter}3"];
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(189, 215, 238)); // xanh nhạt như hình

            // Thêm viền
            headerRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            headerRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            headerRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            headerRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;

            // Căn giữa & in đậm
            headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            headerRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            headerRange.Style.Font.Bold = true;
            headerRange.Style.WrapText = true;


            // 1️⃣ Style viền (tương đương $BStyle)
            var borderStyle = worksheet.Cells[$"B4:{lastColLetter}{row}"].Style;
            borderStyle.Border.Top.Style = ExcelBorderStyle.Thin;
            borderStyle.Border.Bottom.Style = ExcelBorderStyle.Thin;
            borderStyle.Border.Left.Style = ExcelBorderStyle.Thin;
            borderStyle.Border.Right.Style = ExcelBorderStyle.Thin;

            worksheet.Cells[$"A{row + 1}:{lastColLetter}{row + 2}"].Style.Border.Top.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[$"A{row + 1}:{lastColLetter}{row + 2}"].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[$"A{row + 1}:{lastColLetter}{row + 2}"].Style.Border.Left.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[$"A{row + 1}:{lastColLetter}{row + 2}"].Style.Border.Right.Style = ExcelBorderStyle.Thin;

            // 2️⃣ Áp màu nền tương đương $color2 (vì color1 bị comment trong PHP)
            worksheet.Cells[$"G{row + 2}:{lastColLetter}{row + 2}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[$"G{row + 2}:{lastColLetter}{row + 2}"].Style.Fill.BackgroundColor.SetColor(Color.LightYellow);

            // 3️⃣ Font màu đỏ cho vùng A(row+1) : col(row+2)
            worksheet.Cells[$"A{row + 1}:{lastColLetter}{row + 2}"].Style.Font.Color.SetColor(Color.Red);
           

            // 4️⃣ Number Format
            worksheet.Cells[$"C4:D{row + 1}"].Style.Numberformat.Format = "#,###";
            worksheet.Cells[$"G4:{lastColLetter}{row + 1}"].Style.Numberformat.Format = "#,###";
            worksheet.Cells[$"G{row + 2}:{lastColLetter}{row + 2}"].Style.Numberformat.Format = "0.00%";

            // 5️⃣ Merge cells
            worksheet.Cells["G2:I2"].Merge = true;
            worksheet.Cells["J2:L2"].Merge = true;

            // 6️⃣ Gán giá trị
            if (!string.IsNullOrEmpty(Mo))
            {
                worksheet.Cells["J2"].Value = $"MO = {Mo}";
            }
            if (!string.IsNullOrEmpty(styleCode))
            {
                worksheet.Cells["G2"].Value = $"StyleCode = {styleCode}";
            }
            // Cập nhật công thức
            // hiện giá trị ngay cả khi chưa Enable Editing
            package.Workbook.Calculate();
            package.Workbook.CalcMode = ExcelCalcMode.Automatic;

            // Tạo file Excel để tải về
            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Report_EndLine_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }
            
        private static string GetExcelColumnName(int columnNumber)
        {
            int dividend = columnNumber;
            string columnName = string.Empty;
            int modulo;

            while (dividend > 0)
            {
                modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }

            return columnName;
        }
    }
}