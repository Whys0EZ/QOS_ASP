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
    public class SummaryFQCController : Controller
    {
        private readonly ILogger<SummaryFQCController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly AppDbContext _context;

        public SummaryFQCController(ILogger<SummaryFQCController> logger, IWebHostEnvironment env, IConfiguration configuration, AppDbContext context)
        {
            _logger = logger;
            _env = env;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Missing connection string: DefaultConnection");
            _configuration =configuration;
            _context = context;
        }
        public IActionResult Index()
        {
            // return View();
            return RedirectToAction("SummaryFQC", "SummaryFQC");
        }
        [TempData]
        public string? MessageStatus { get; set;}
        [HttpGet]
        public IActionResult SummaryFQC(string? topDefected, string? typeCode, string? Unit, string? Mo, string? styleCode, DateTime? dateFrom, DateTime? dateEnd)
        {
            // _logger.LogInformation("=== SummaryFQC GET Request ===");
            // _logger.LogInformation($"Parameters - Unit: '{Unit}', DateFrom: {dateFrom}, DateEnd: {dateEnd}" +
            //     $", Mo: '{Mo}', StyleCode: '{styleCode}', TypeCode: '{typeCode}', TopDefected: '{topDefected}'");
            try
            {
                var model = new SummaryFQCViewModel
                {
                    Unit_List = GetUnitList(),
                    TopDefected = topDefected ?? "5",
                    TypeCode = typeCode ?? "ALL",
                    Unit = string.IsNullOrEmpty(Unit) ? "" : Unit,
                    Mo = Mo ?? "",
                    StyleCode = styleCode ?? "",
                    DateFrom = dateFrom ?? DateTime.Now,
                    DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1),
                    ReportData = new List<Dictionary<string, object>>(),
                    DefectStats = new Dictionary<string, DefectStat_FQC>()
                    
                };
                
                // _logger.LogInformation($"Model created - Units available: {model.Unit_List.Count}");
                LoadReportData(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SummaryFQC GET");
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return View(new SummaryFQCViewModel { Unit_List = GetUnitList() });
            }
        }
        private List<string> GetUnitList()
        {
            var units = new List<string>();
            try
            {
                var sql = @"SELECT DISTINCT Unit FROM FQC_UQ_Result_SUM ORDER BY Unit ASC";
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var value = reader["Unit"] == DBNull.Value ? "" : reader["Unit"].ToString();
                                units.Add(value ?? "");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading unit list");
            }

            return units;
        }

   
        private void LoadReportData(SummaryFQCViewModel model)
        {
            try
            {
                // _logger.LogInformation("=== LoadReportData Start ===");
                
                // Prepare parameters for stored procedure
                var dateFrom = model.DateFrom.ToString("yyyy-MM-dd");
                var dateEnd = model.DateEnd.ToString("yyyy-MM-dd");
                var unit = string.IsNullOrEmpty(model.Unit) ? "" : model.Unit;
                
                var mo = string.IsNullOrEmpty(model.Mo) ? "" : model.Mo;
                var styleCode = string.IsNullOrEmpty(model.StyleCode) ? "" : model.StyleCode;
                var defectedType = string.IsNullOrEmpty(model.TypeCode) ? "ALL" : model.TypeCode;
                var top = model.TopDefected.ToString();
                var search = "";

                // _logger.LogInformation($"SP Parameters: DateFrom={dateFrom}, DateTo={dateEnd}, Unit={unit},  MO={mo}, StyleCode={styleCode}, Type={defectedType}, Top={top}");

                // Execute stored procedure
                var sql = @"EXEC RP_TonghoploiFQC_MO @Date_From, @Date_To, @Unit, @SO, @StyleCode, @Defected_Type, @Top_Defected, @Search";
                
                var parameters = new[]
                {
                    new Microsoft.Data.SqlClient.SqlParameter("@Date_From", dateFrom),
                    new Microsoft.Data.SqlClient.SqlParameter("@Date_To", dateEnd),
                    new Microsoft.Data.SqlClient.SqlParameter("@Unit", unit),
                    
                    new Microsoft.Data.SqlClient.SqlParameter("@SO", mo),
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
                var defectSummary = new Dictionary<string, DefectStat_FQC>();

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
                            defectSummary[defectKey] = new DefectStat_FQC
                            {
                                Name = defectKey,
                                Count = faultQty,
                                Percentage = 0 // Will calculate later
                            };
                        }
                    }
                }
                reader.Close();

                // _logger.LogInformation($"SP returned {reportDataList.Count} records");

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

                // _logger.LogInformation($"Statistics calculated - Total: {model.TotalDefects}, Defect Types: {model.DefectStats.Count}");
                
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
                model.DefectStats = new Dictionary<string, DefectStat_FQC>();
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
        public IActionResult ExportToExcel( string? topDefected, string? typeCode, string? Unit, string? Mo, string? styleCode, DateTime? dateFrom, DateTime? dateEnd, string faultCodes)
        {
            var model = new SummaryFQCViewModel
                {
                    Unit_List = GetUnitList(),
                    TopDefected = topDefected ?? "5",
                    TypeCode = typeCode ?? "ALL",
                    Unit = string.IsNullOrEmpty(Unit) ? "" : Unit,
                    
                    Mo = Mo ?? "",
                    StyleCode = styleCode ?? "",
                    DateFrom = dateFrom ?? DateTime.Now,
                    DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1),
                    ReportData = new List<Dictionary<string, object>>(),
                    DefectStats = new Dictionary<string, DefectStat_FQC>()
                    
                };
               
                LoadReportData(model);

            int countfaultCodes = faultCodes.Split(',').Length;
            string[] codes = faultCodes.Split(',');
            int col = 0;

            // _logger.LogInformation(" Fault Code: " + faultCodes);
            
            _logger.LogInformation($"Parameters - Unit: '{Unit}', DateFrom: {dateFrom}, DateEnd: {dateEnd}" +
                $", Mo: '{Mo}', StyleCode: '{styleCode}', TypeCode: '{typeCode}', TopDefected: '{topDefected}' ,' Fault Code: ' + '{faultCodes}'");

            // Tạo Excel file (ví dụ với ClosedXML hoặc EPPlus)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Đường dẫn tới file template
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Report", "B7_Summary_FQC_Defects.xlsx");
            
            if (!System.IO.File.Exists(templatePath))
            {
                MessageStatus = "Không tìm thấy file mẫu báo cáo.";
                return RedirectToAction("SummaryFQC");
            }

            using var package = new ExcelPackage(new FileInfo(templatePath));
            var worksheet = package.Workbook.Worksheets[0];

            // Ghi tiêu đề báo cáo
            worksheet.Cells["A2"].Value = Unit ;
            worksheet.Cells["B2"].Value = (dateFrom ?? DateTime.Now).ToString("dd/MM/yyyy");
            worksheet.Cells["D2"].Value = (dateEnd ?? DateTime.Now).ToString("dd/MM/yyyy");
            if(Unit == "ALL")
            {
                worksheet.Cells["I3"].Value = "Unit" ;
            } else 
            {
                worksheet.Cells["B3"].Value = "Inspt Date" ; 
                worksheet.Cells["C3"].Value = "SO" ; 
                worksheet.Cells["D3"].Value = "Style" ; 
                worksheet.Cells["E3"].Value = "PO" ; 
                worksheet.Cells["F3"].Value = "shipMode" ; 
                worksheet.Cells["G3"].Value = "Status" ; 
                worksheet.Cells["H3"].Value = "Operation" ; 
                worksheet.Cells["I3"].Value = "Unit" ;
            }
            int i = 0;
            foreach (var code in codes)
            {
                col = i + 16;
                worksheet.Cells[3, col].Value = model.ReportData.Where(r => r["Fault_Code"].ToString() == code).Select(r => r["Fault_Name_VN"]).FirstOrDefault()?.ToString() ?? code;
                i++;
            }
            // worksheet.Cells["B3"].Style.Font.Bold = true;
            // worksheet.Cells[""].Style.Font.Size = 16;
            // worksheet.Cells["A2"].Style.Font.Bold = true;
            // worksheet.Cells["A2"].Style.Font.Size = 12;

            // Lấy dữ liệu báo cáo
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("RP_Tonghoploi_FQC_DownloadExcelDetail_SO_TEST", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            
            cmd.Parameters.AddWithValue("@Date_From", dateFrom);
            cmd.Parameters.AddWithValue("@Date_To", dateEnd);
            cmd.Parameters.AddWithValue("@Unit", Unit);
       
            cmd.Parameters.AddWithValue("@SO", string.IsNullOrEmpty(Mo) ? "" : Mo);
            cmd.Parameters.AddWithValue("@StyleCode", string.IsNullOrEmpty(styleCode) ? "" : styleCode);
            cmd.Parameters.AddWithValue("@Defected_Type", typeCode);
            cmd.Parameters.AddWithValue("@Top_Defected", "ALL");
            cmd.Parameters.AddWithValue("@DefectList", faultCodes);
            

            conn.Open();
            int row = 4;
            col = 17;
            int lastColIndex = col + countfaultCodes - 1;
            string lastColLetter = GetExcelColumnName(lastColIndex);
            using (var reader = cmd.ExecuteReader())
            {

                while (reader.Read())
                {
                    // worksheet.Cells[row, 1].Value = row - 3; // STT
                    worksheet.Cells[row, 2].Value = reader["WorkDate"].ToString();
                    worksheet.Cells[row, 3].Value = reader["SO"].ToString();
                    worksheet.Cells[row, 4].Value = reader["Style"].ToString();
                    worksheet.Cells[row, 5].Value = reader["PO"].ToString();
                    worksheet.Cells[row, 6].Value = reader["shipMode"].ToString();
                    worksheet.Cells[row, 7].Value = reader["Status"].ToString();
                    worksheet.Cells[row, 8].Value = reader["Operation"].ToString();
                    worksheet.Cells[row, 9].Value = reader["Unit"].ToString();
                    worksheet.Cells[row, 10].Value = reader["Destination"].ToString();
                    worksheet.Cells[row, 11].Value = reader["Qty"].ToString();
                    worksheet.Cells[row, 12].Value = reader["Check_QTY"];
                    

                    for (int k = 0; k < countfaultCodes; k++)
                    {
                        int cols = 16 + k; // cột G là 7, nên bắt đầu từ 7(nếu A=1)
                        string code = codes[k];

                        if (!reader.IsDBNull(reader.GetOrdinal(code)))
                            worksheet.Cells[row, cols].Value = reader[code];
                    }
                    // Tổng Defect theo hàng
                    worksheet.Cells[$"M{row}"].Formula = $"SUM(P{row}:{lastColLetter}{row})";

                    // % Defect = D/C
                    worksheet.Cells[$"O{row}"].Formula = $"M{row}/L{row}";
                    // col++;
                    row++;
                }

            }
            worksheet.Cells["A" + (row+1)].Value = "Total";
            worksheet.Cells["L" + (row+1)].Formula = $"SUM(L4:L{row})";
            worksheet.Cells["M" + (row+1)].Formula = $"SUM(M4:M{row})";
            worksheet.Cells["N" + (row+1)].Formula = $"SUM(N4:N{row})";
            worksheet.Cells["O" + (row+1)].Formula = $"M{row+1}/L{row+1}";
            worksheet.Cells["A" + (row+2)].Value = "OQL % based on total defect";
            for(int j = 0; j < countfaultCodes; j++)
            {
                int currentCol = j + 16;
                string colLetter = GetExcelColumnName(currentCol);
                worksheet.Cells[row + 1, currentCol].Formula = $"SUM({colLetter}4:{colLetter}{row})";
                worksheet.Cells[row + 2, currentCol].Formula = $"{colLetter}{row + 1}/L{row + 1}";
            }

            // Tô màu nền từ G2 đến cột cuối cùng (dòng 2)
            var headerRange = worksheet.Cells[$"P3:{lastColLetter}3"];
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
            worksheet.Cells[$"L4:M{row + 1}"].Style.Numberformat.Format = "#,###";
            worksheet.Cells[$"P4:{lastColLetter}{row + 1}"].Style.Numberformat.Format = "#,###";
            worksheet.Cells[$"P{row + 2}:{lastColLetter}{row + 2}"].Style.Numberformat.Format = "0.00%";

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
                $"Report_FQC_{Unit}_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
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