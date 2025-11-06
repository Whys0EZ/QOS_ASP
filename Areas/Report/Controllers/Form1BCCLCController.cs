using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QOS.Areas.Report.Models;
using QOS.Data;
using QOS.Models;
using Dapper;
using OfficeOpenXml;


namespace QOS.Areas.Report.Controllers
{
    [Authorize] // chỉ khi login mới được vào
    [Area("Report")]
    public class Form1BCCLCController : Controller
    {
        private readonly ILogger<Form1BCCLCController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public Form1BCCLCController(ILogger<Form1BCCLCController> logger, IWebHostEnvironment env, IConfiguration configuration, AppDbContext context)
        {
            _logger = logger;
            _env = env;
            _configuration = configuration;
            _context = context;
        }
        public IActionResult Index()
        {
            // return View();
            return RedirectToAction("RP_Form1", "Form1BCCLC");
        }

        public IActionResult RP_Form1(string? Unit, DateTime? dateFrom, DateTime? dateEnd)
        {
            string FactoryName = _configuration.GetValue<string>("AppSettings:FactoryName") ?? "";
            var model = new RP_Form1ViewModel
            {
                Unit_List = _context.Set<Unit_List>().Where(u => u.Factory == FactoryName).OrderBy(u => u.Unit).ToList(),
                Unit = Unit,
                DateFrom = dateFrom ?? DateTime.Now.AddDays(-1),
                DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1)

            };
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string sql;

            if (!string.IsNullOrEmpty(Unit) && Unit != "ALL")
            {
                sql = @"
                SELECT t1.*, t4.FullName 
                FROM Form1_BCCLC t1
                LEFT JOIN User_List t4 ON t1.UserUpdate = t4.UserName
                WHERE t1.Unit = @Unit
                AND t1.LastUpdate BETWEEN @dateF AND @dateT
                ORDER BY t1.LastUpdate DESC";
            }
            else
            {
                sql = @"
                SELECT t1.*, t4.FullName 
                FROM Form1_BCCLC t1
                LEFT JOIN User_List t4 ON t1.UserUpdate = t4.UserName
                WHERE t1.LastUpdate BETWEEN @dateF AND @dateT
                ORDER BY t1.LastUpdate DESC";
            }
            ;
            // Console.WriteLine("SQL: " + sql + " Unit: " + Unit);
            var history = conn.Query<Form1_BCCLC>(sql, new { Unit, dateF = model.DateFrom, dateT = model.DateEnd }).ToList();

            model.History = history; // đưa thẳng vào Model

            return View(model);
        }

        public IActionResult DetailForm1(int id)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string sql = @"
                SELECT t1.*, t4.FullName 
                FROM Form1_BCCLC t1
                LEFT JOIN User_List t4 ON t1.UserUpdate = t4.UserName
                WHERE t1.ID = @ID
                ORDER BY t1.LastUpdate DESC";

            var detail = conn.QueryFirstOrDefault<Form1_BCCLC>(sql, new { ID = id });

            if (detail == null)
            {
                return NotFound();
            }

            return PartialView("_tableRP_Form1", detail);
        }
        public IActionResult DeleteReport(int reportId)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string sql_Tranfer = @" INSERT INTO Form1_BCCLC_Delete SELECT * , @UserName,GETDATE() FROM FORM1_BCCLC WHERE ID = @ID ";
            string sql_Delete = @" DELETE FROM Form1_BCCLC WHERE ID = @ID";
            var userName = User.Identity?.Name;
            Console.WriteLine("report ID: " + reportId + "User : " + userName);
            int result = conn.Execute(sql_Tranfer, new { ID = reportId, UserName = userName });
            if (result > 0)
            {
                int result2 = conn.Execute(sql_Delete, new { ID = reportId });
                if (result2 > 0)
                {
                    return Json(new { success = true, message = "Xóa báo cáo thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa báo cáo." });
                }
            }
            else
            {
                return Json(new { success = false, message = "Không tìm thấy báo cáo để xóa." });
            }
        }

        public IActionResult ExportExcel(string? Unit, DateTime? dateFrom, DateTime? dateEnd)
        {
            // Tạo Excel file (ví dụ với ClosedXML hoặc EPPlus)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Đường dẫn tới file template
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Report", "BCCLBC.xlsx");

            using var package = new ExcelPackage(new FileInfo(templatePath));
            var worksheet = package.Workbook.Worksheets[0]; // Lấy sheet đầu tiên, hoặc by name: ["Sheet1"]
            var model = new RP_Form1ViewModel
            {
                Unit = Unit,
                DateFrom = dateFrom ?? DateTime.Now.AddDays(-7),
                DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1)

            };
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string sql;

            if (!string.IsNullOrEmpty(Unit) && Unit != "ALL")
            {
                sql = @"
                SELECT t1.*, t4.FullName 
                FROM Form1_BCCLC t1
                LEFT JOIN User_List t4 ON t1.UserUpdate = t4.UserName
                WHERE t1.Unit = @Unit
                AND t1.LastUpdate BETWEEN @dateF AND @dateT
                ORDER BY t1.LastUpdate DESC";
            }
            else
            {
                sql = @"
                SELECT t1.*, t4.FullName 
                FROM Form1_BCCLC t1
                LEFT JOIN User_List t4 ON t1.UserUpdate = t4.UserName
                WHERE t1.LastUpdate BETWEEN @dateF AND @dateT
                ORDER BY t1.LastUpdate DESC";
            }
            ;
            // Console.WriteLine("SQL: " + sql + " Unit: " + Unit);
            var history = conn.Query<Form1_BCCLC>(sql, new { Unit, dateF = model.DateFrom, dateT = model.DateEnd }).ToList();


            // Ghi dữ liệu vào template
            worksheet.Cells["A2"].Value = dateFrom;                          // Ghi Unit vào ô B2
            worksheet.Cells["B2"].Value = dateEnd;
           

            // Ví dụ ghi dữ liệu vào bảng từ dòng 7
            int startRow = 4;
            int count = 1;
           

            int row = startRow;
            foreach (var item in history)
            {
                worksheet.Cells[row, 1].Value = count;
                worksheet.Cells[row, 2].Value = item.Report_ID;
                worksheet.Cells[row, 3].Value = item.Unit;
                worksheet.Cells[row, 4].Value = item.Cut_Leader;
                worksheet.Cells[row, 5].Value = item.CutTableName;
                worksheet.Cells[row, 6].Value = item.Lay_Height;
                worksheet.Cells[row, 7].Value = item.Table_Long;
                worksheet.Cells[row, 8].Value = item.Table_Width;
                worksheet.Cells[row, 9].Value = item.CutTableRatio;
                worksheet.Cells[row, 10].Value = item.Cut_Lot;
                worksheet.Cells[row, 11].Value = item.MO;
                worksheet.Cells[row, 12].Value = item.Color;
                worksheet.Cells[row, 13].Value = item.Batch;
                worksheet.Cells[row, 14].Value = item.Cut_QTY;

                   // boolean -> 1/0
                worksheet.Cells[row, 15].Value = item.Shading == true ? "1" : "0";
                worksheet.Cells[row, 16].Value = item.Wave == true ? "1" : "0";
                worksheet.Cells[row, 17].Value = item.Narrow_Width == true ? "1" : "0";
                worksheet.Cells[row, 18].Value = item.Spreading == true ? "1" : "0";

                worksheet.Cells[row, 19].Value = item.DS_L_Min;
                worksheet.Cells[row, 20].Value = item.DS_L_Max;
                worksheet.Cells[row, 21].Value = item.DS_W_Min;
                worksheet.Cells[row, 22].Value = item.DS_W_Max;
                worksheet.Cells[row, 23].Value = item.Size_Parameter;
                worksheet.Cells[row, 24].Value = item.Notch;
                worksheet.Cells[row, 25].Value = item.Unclean;
                worksheet.Cells[row, 26].Value = item.Straigh;
                worksheet.Cells[row, 27].Value = item.Shape;
                worksheet.Cells[row, 28].Value = item.Edge;
                worksheet.Cells[row, 29].Value = item.Stripe;
                worksheet.Cells[row, 30].Value = item.Remark;
                worksheet.Cells[row, 31].Value = item.Audit_Time;
                worksheet.Cells[row, 32].Value = item.UserUpdate;
                worksheet.Cells[row, 33].Value = item.FullName;
                worksheet.Cells[row, 34].Value = item.LastUpdate?.ToString("dd/MM/yyyy HH:mm") ?? "";
                row++;
                count++;
            }

            // Xuất file
            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Report_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }
    }
}