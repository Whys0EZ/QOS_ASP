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


namespace QOS.Areas.Report.Controllers
{
    [Authorize] // chỉ khi login mới được vào
    [Area("Report")]
    public class Form3BCDTController : Controller
    {
        private readonly ILogger<Form3BCDTController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public Form3BCDTController(ILogger<Form3BCDTController> logger, IWebHostEnvironment env, IConfiguration configuration, AppDbContext context)
        {
            _logger = logger;
            _env = env;
            _configuration = configuration;
            _context = context;
        }
        public IActionResult Index()
        {
            // return View();
            return RedirectToAction("RP_Form3", "Form3BCDT");
        }

        public IActionResult RP_Form3(string? Unit, DateTime? dateFrom, DateTime? dateEnd)
        {
            string FactoryName = _configuration.GetValue<string>("AppSettings:FactoryName") ?? "";
            var model = new RP_Form3ViewModel
            {
                Unit_List = _context.Set<Unit_List>().Where(u => u.Factory == FactoryName).OrderBy(u => u.Unit).ToList(),
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
                FROM Form3_BCDT t1
                LEFT JOIN User_List t4 ON t1.UserUpdate = t4.UserName
                WHERE t1.Unit = @Unit
                AND CAST(t1.LastUpdate AS DATE) >= CAST(@dateF AS DATE) AND CAST(t1.LastUpdate AS DATE) <= CAST(@dateT AS DATE)
                ORDER BY t1.LastUpdate DESC";
            }
            else
            {
                sql = @"
                SELECT t1.*, t4.FullName 
                FROM Form3_BCDT t1
                LEFT JOIN User_List t4 ON t1.UserUpdate = t4.UserName
                WHERE CAST(t1.LastUpdate AS DATE) >= CAST(@dateF AS DATE) AND CAST(t1.LastUpdate AS DATE) <= CAST(@dateT AS DATE)
                ORDER BY t1.LastUpdate DESC";
            }
            ;
            // Console.WriteLine("DateEnd: " + model.DateEnd + " Unit: " + model.DateFrom);
            var history = conn.Query<Form3_BCDT>(sql, new { Unit, dateF = model.DateFrom, dateT = model.DateEnd }).ToList();

            model.History = history; // đưa thẳng vào Model

            return View(model);
        }

        public IActionResult DetailForm3(int id)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string sql = @"
                SELECT t1.*, t4.FullName 
                FROM Form3_BCDT t1
                LEFT JOIN User_List t4 ON t1.UserUpdate = t4.UserName
                WHERE t1.ID = @ID
                ORDER BY t1.LastUpdate DESC";

            var detail = conn.QueryFirstOrDefault<Form3_BCDT>(sql, new { ID = id });

            if (detail == null)
            {
                return NotFound();
            }

            return PartialView("_tableRP_Form3", detail);
        }
        public IActionResult DeleteReport(string reportId, int audit_Time)
        {
            try
            {
                using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();
                var userName = User.Identity?.Name;
                // Console.WriteLine("ID: " + reportId + "Audit: " + audit_Time + "name: " + userName);
                using (var cmd = new SqlCommand("Delet_BC_Form3_BCDT", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    // cmd.Parameters.AddWithValue("@FactoryID", "FactoryID");
                    cmd.Parameters.AddWithValue("@Report_ID", reportId);
                    cmd.Parameters.AddWithValue("@Audit_Time", audit_Time);
                    cmd.Parameters.AddWithValue("@UserUpdate", userName);

                    cmd.ExecuteNonQuery(); // không dùng rowsAffected nữa

                    return Json(new { success = true });
                    // int rowsAffected = cmd.ExecuteNonQuery();

                    // if (rowsAffected > 0)
                    // {
                    //     return Json(new { success = true });
                    // }
                    // else
                    // {
                    //     return Json(new { success = false, message = "Không tìm thấy report cần xóa." });
                    // }

                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }


            // Console.WriteLine("report ID: " + reportId + "User : " + userName);


        }

        public IActionResult ExportExcel(string? Unit, DateTime? dateFrom, DateTime? dateEnd)
        {
            // Tạo Excel file (ví dụ với ClosedXML hoặc EPPlus)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Đường dẫn tới file template
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Report", "Form2_BCCPI.xlsx");

            using var package = new ExcelPackage(new FileInfo(templatePath));
            var worksheet = package.Workbook.Worksheets[0]; // Lấy sheet đầu tiên, hoặc by name: ["Sheet1"]
            var model = new RP_Form3ViewModel
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
                FROM Form3_BCDT t1
                LEFT JOIN User_List t4 ON t1.UserUpdate = t4.UserName
                WHERE t1.Unit = @Unit
                AND t1.LastUpdate BETWEEN @dateF AND @dateT
                ORDER BY t1.LastUpdate DESC";
            }
            else
            {
                sql = @"
                SELECT t1.*, t4.FullName 
                FROM Form3_BCDT t1
                LEFT JOIN User_List t4 ON t1.UserUpdate = t4.UserName
                WHERE t1.LastUpdate BETWEEN @dateF AND @dateT
                ORDER BY t1.LastUpdate DESC";
            }
            ;
            // Console.WriteLine("SQL: " + sql + " Unit: " + Unit);
            var history = conn.Query<Form3_BCDT>(sql, new { Unit, dateF = model.DateFrom, dateT = model.DateEnd }).ToList();


            // Ghi dữ liệu vào template
            worksheet.Cells["A2"].Value = dateFrom;                          // Ghi Unit vào ô B2
            worksheet.Cells["B2"].Value = dateEnd;


            // Ví dụ ghi dữ liệu vào bảng từ dòng 7
            int startRow = 4;
            int count = 1;


            int row = startRow;
            foreach (var item in history)
            {
                // worksheet.Cells[row, 1].Value = count;
                // worksheet.Cells[row, 2].Value = item.Report_ID;
                // worksheet.Cells[row, 3].Value = item.AQL;
                // worksheet.Cells[row, 4].Value = item.Unit;
                // worksheet.Cells[row, 5].Value = item.Cut_Leader;
                // worksheet.Cells[row, 6].Value = item.CPI_Leader;
                // worksheet.Cells[row, 7].Value = item.CPI;
                // worksheet.Cells[row, 8].Value = item.Rap;
                // worksheet.Cells[row, 9].Value = item.CutTableName;
                // worksheet.Cells[row, 10].Value = item.MO;
                // worksheet.Cells[row, 11].Value = item.Color;
                // worksheet.Cells[row, 12].Value = item.Batch;
                // worksheet.Cells[row, 13].Value = item.QTY;
                // worksheet.Cells[row, 14].Value = item.Check_QTY;
                // worksheet.Cells[row, 15].Value = item.Fault_AQL_QTY;
                // worksheet.Cells[row, 16].Value = item.Fault_QTY;
                // worksheet.Cells[row, 17].Value = item.Passed== true ? "Pass" : "False" ;

                // // boolean -> 1/0
                // worksheet.Cells[row, 18].Value = item.Hole == true ? "1" : "0";
                // worksheet.Cells[row, 19].Value = item.Shading == true ? "1" : "0";
                // worksheet.Cells[row, 20].Value = item.Yarn == true ? "1" : "0";
                // worksheet.Cells[row, 21].Value = item.Slub == true ? "1" : "0";
                // worksheet.Cells[row, 22].Value = item.Dirty == true ? "1" : "0";

                // worksheet.Cells[row, 23].Value = item.DS_L_Min;
                // worksheet.Cells[row, 24].Value = item.DS_L_Max;
                // worksheet.Cells[row, 25].Value = item.DS_W_Min;
                // worksheet.Cells[row, 26].Value = item.DS_W_Max;
                // worksheet.Cells[row, 27].Value = item.Size_Parameter;

                // worksheet.Cells[row, 28].Value = item.Notch;
                // worksheet.Cells[row, 29].Value = item.Straigh;
                // worksheet.Cells[row, 30].Value = item.Shape;
                // worksheet.Cells[row, 31].Value = item.Edge;
                // worksheet.Cells[row, 32].Value = item.Stripe;
                // worksheet.Cells[row, 33].Value = item.Remark;
                // worksheet.Cells[row, 34].Value = item.Audit_Time;
                // worksheet.Cells[row, 35].Value = item.UserUpdate;
                // worksheet.Cells[row, 36].Value = item.FullName;
                // worksheet.Cells[row, 37].Value = item.LastUpdate?.ToString("dd/MM/yyyy HH:mm") ?? "";
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