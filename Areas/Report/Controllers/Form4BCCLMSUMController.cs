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
    public class Form4BCCLMSUMController : Controller
    {
        private readonly ILogger<Form4BCCLMSUMController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly AppDbContext _context;

        public Form4BCCLMSUMController(ILogger<Form4BCCLMSUMController> logger, IWebHostEnvironment environment, IConfiguration configuration, AppDbContext context)
        {
            _logger = logger ;
            _env = environment ;
            _configuration = configuration ;
            _context = context ;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";

        }
        public ActionResult Index()
        {
            return RedirectToAction("RP_Form4_BCCLM_SUM");
        }
        public ActionResult RP_Form4_BCCLM_SUM(string? Unit, DateTime? dateFrom , DateTime? dateEnd)
        {
            var model = new Form4BCCLMSUMViewModel {
                Unit_List = GetUnitList(),
                Unit = Unit ?? "ALL",
                DateFrom = dateFrom ?? DateTime.Now,
                DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1)

            };
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

        private void LoadReportData(Form4BCCLMSUMViewModel model)
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

                using (var command = new SqlCommand("RP_BCCLM_SUM", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Date_F", dateFStr);
                    command.Parameters.AddWithValue("@Date_T", dateTStr);
                    command.Parameters.AddWithValue("@UserName", userName);
                    command.Parameters.AddWithValue("@Unit", model.Unit == "ALL" ? "" : model.Unit);
                    

                    using var reader = command.ExecuteReader();
                    
                    {
                        // Dữ liệu trả về có thể có cột động → dùng DataTable
                        var dataTable = new DataTable();
                        dataTable.Load(reader);

                        _logger.LogInformation($"✅ Loaded {dataTable.Rows.Count} rows and {dataTable.Columns.Count} columns");

                        // Gán vào model
                        model.DynamicTable = dataTable;
                    }
                }

                // 2️⃣ Đọc danh sách cột sau khi reader đầu tiên đã đóng
                model.ColumnHeaders = new List<string>();
                string query = @"SELECT Column_name FROM TBM_ColumnName 
                                WHERE UserName = @UserName 
                                ORDER BY Column_name";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@UserName", userName ?? "system");
                    using (var reader_query = cmd.ExecuteReader())
                    {
                        while (reader_query.Read())
                        {
                            model.ColumnHeaders.Add(reader_query["Column_name"]?.ToString() ?? "");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading report data");
                model.Message = $"Lỗi tải dữ liệu: {ex.Message}";
                throw;
            }
        }

        public IActionResult ExportExcel(string? Unit, DateTime? dateFrom, DateTime? dateEnd)
        {
            var model = new Form4BCCLMSUMViewModel {
                Unit_List = GetUnitList(),
                Unit = Unit ?? "ALL",
                DateFrom = dateFrom ?? DateTime.Now,
                DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1)

            };
            LoadReportData(model);

            if (model.DynamicTable == null || model.DynamicTable.Rows.Count == 0)
            {
                TempData["Message"] = "Không có dữ liệu để xuất Excel.";
                return RedirectToAction("RP_Form4BCCLMSUM");
            }

            // Tạo Excel file (ví dụ với ClosedXML hoặc EPPlus)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // // Đường dẫn tới file template
            // var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Report", "B7_Summary_Endline_Defects.xlsx");
            
            // if (!System.IO.File.Exists(templatePath))
            // {
            //     MessageStatus = "Không tìm thấy file mẫu báo cáo.";
            //     return RedirectToAction("RP_Summary_Defects_KCM");
            // }

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Report");

                int row = 3;
                int col = 1;
                worksheet.Cells[1, 1].Value = "Tổng Hợp Lỗi Chuyền May";
                worksheet.Cells[2, 1].Value = model.Unit;
                // ==== Header ====
                worksheet.Cells[row, col++].Value = "STT";
                worksheet.Cells[row, col++].Value = "Line";

                foreach (var header in model.ColumnHeaders)
                {
                    worksheet.Cells[row, col++].Value = header;
                }

                worksheet.Cells[row, col].Value = "Tổng";

                using (var range = worksheet.Cells[row, 1, row, col])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                // ==== Data ====
                int stt = 1;
                foreach (DataRow dataRow in model.DynamicTable.Rows)
                {
                    row++;
                    col = 1;
                    double total = 0;

                    worksheet.Cells[row, col++].Value = stt++;
                    worksheet.Cells[row, col++].Value = dataRow["Line"]?.ToString();

                    foreach (var header in model.ColumnHeaders)
                    {
                        var value = dataRow[header];
                        if (value != DBNull.Value && double.TryParse(value.ToString(), out double val))
                        {
                            worksheet.Cells[row, col].Value = val;
                            total += val;
                        }
                        else
                        {
                            worksheet.Cells[row, col].Value = "";
                        }
                        col++;
                    }

                    worksheet.Cells[row, col].Value = total;
                }
                worksheet.Cells[1, 1, 1, col].Merge = true;
                

                // ==== Total row (Grand Total) ====
                row++;
                worksheet.Cells[row, 1].Value = "Tổng cộng";
                worksheet.Cells[row, 1, row, 2].Merge = true;
                worksheet.Cells[row, 1, row, 2].Style.Font.Bold = true;
                worksheet.Cells[row, 1, row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                int totalColStart = 3;
                int totalColEnd = 2 + model.ColumnHeaders.Count + 1;

                for (int c = totalColStart; c <= totalColEnd; c++)
                {
                    string colLetter = worksheet.Cells[1, c].Address.Substring(0, 1);
                    worksheet.Cells[row, c].Formula = $"SUM({colLetter}2:{colLetter}{row - 1})";
                    worksheet.Cells[row, c].Style.Font.Bold = true;
                }

                // ==== Format chung ====
                worksheet.Cells.AutoFitColumns();
                worksheet.View.FreezePanes(2, 3); // cố định header + 2 cột đầu
                worksheet.Cells.Style.Font.Name = "Calibri";
                worksheet.Cells.Style.Font.Size = 12;
                worksheet.Cells[1,1].Style.Font.Size = 16;
                worksheet.Cells[2,1].Style.Font.Size = 16;
                worksheet.Cells[1,1].Style.Font.Bold = true;
                worksheet.Cells[2,1].Style.Font.Bold = true;
                

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