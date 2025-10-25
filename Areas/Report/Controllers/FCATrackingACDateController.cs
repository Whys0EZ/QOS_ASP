using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QOS.Data;
using QOS.Models;
using QOS.Areas.Report.Models;
using OfficeOpenXml;

namespace QOS.Areas.Report.Controllers
{
    [Authorize]
    [Area("Report")]
    public class FCATrackingACDateController : Controller
    {

        private readonly ILogger<FCATrackingACDateController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public FCATrackingACDateController(ILogger<FCATrackingACDateController> logger, IWebHostEnvironment env, IConfiguration configuration, AppDbContext context)
        {
            _logger = logger;
            _env = env;
            _configuration =configuration;
            _context = context;
        }
        public IActionResult Index()
        {
            return RedirectToAction("FCATrackingACDate", "FCATrackingACDate");
        }
        public IActionResult FCATrackingACDate(string? Customer, DateTime? dateFrom, DateTime? dateEnd, string? Search)
        {
            var model = new FCATrackingViewModel
            {
                
                Customer_List = GetCustomerList(),
                Customer = Customer ?? "ALL",
                DateFrom = dateFrom ?? DateTime.Now,
                DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1),
                ReportData = new List<Dictionary<string, object>>(),
                Searching = Search
            };

            _logger.LogInformation($"Parameters - Customer: '{Customer}', DateFrom: {dateFrom}, DateEnd: {dateEnd}, Searching: {Search} " );
            LoadReportData(model);
            return View(model);
        }
        private List<string> GetCustomerList()
        {
            try
            {
                var sql =@"Select distinct Infor_10 from TRACKINIG_UploadData";
                var connection = _context.Database.GetDbConnection();
                var command = connection.CreateCommand();
                command.CommandText = sql;
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    connection.Open();
                }

                var reader = command.ExecuteReader();
                var customers = new List<string>();
                while (reader.Read())
                {
                    // Ép kiểu an toàn, tránh lỗi DBNull
                    var value = reader["Infor_10"] == DBNull.Value ? "" : reader["Infor_10"].ToString();
                    // if (!string.IsNullOrWhiteSpace(value))
                        customers.Add(value);
                }
                
                return customers;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer list");
                return new List<string>();
            }

        }
        private void LoadReportData(FCATrackingViewModel model)
        {
            try
            {
                // Prepare parameters for stored procedure
                var dateFrom = model.DateFrom.ToString("yyyy-MM-dd");
                var dateEnd = model.DateEnd.ToString("yyyy-MM-dd");
                // var customer = string.IsNullOrEmpty(model.Customer) ? "ALL" : model.Customer;
                var customer = model.Customer;
                var search = model.Searching;
                var reportDataList = new List<Dictionary<string, object>>();
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                    {
                        connection.Open();
                        // Execute stored procedure
                        var sql = @"EXEC TRACKING_Select_data_to_Report_ACDate @Customer,@Seach, @Date_AC ";
                
                        using (var command = new SqlCommand(sql, connection))
                            {
                                command.Parameters.AddWithValue("@Customer", customer ?? "");
                                command.Parameters.AddWithValue("@Seach", search ?? "");
                                command.Parameters.AddWithValue("@Date_AC", dateFrom);
                                

                                // Log SQL
                                string debugSql = sql;
                                foreach (SqlParameter p in command.Parameters)
                                {
                                    string value = p.Value == null || p.Value == DBNull.Value
                                        ? "NULL"
                                        : $"N'{p.Value.ToString().Replace("'", "''")}'";
                                    debugSql = debugSql.Replace(p.ParameterName, value);
                                }

                                _logger.LogInformation("Debug SQL: {debugSql}", debugSql);


                                using (var reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        var rowData = new Dictionary<string, object>
                                        {
                                            ["ResultStatus"] = reader["ResultStatus"]?.ToString() ?? "",
                                            ["Customer"] = reader["Customer"]?.ToString() ?? "",
                                            ["Infor_01"] = reader["Infor_01"]?.ToString() ?? "",
                                            ["Infor_02"] = reader["Infor_02"]?.ToString() ?? "",
                                            ["Infor_03"] = reader["Infor_03"]?.ToString() ?? "",
                                            ["Infor_04"] = reader["Infor_04"]?.ToString() ?? "",
                                            ["Infor_05"] = reader["Infor_05"]?.ToString() ?? "",
                                            ["Infor_06"] = reader["Infor_06"]?.ToString() ?? "",
                                            ["Infor_07"] = reader["Infor_07"]?.ToString() ?? "",
                                            ["Infor_08"] = reader["Infor_08"]?.ToString() ?? "",
                                            ["Infor_09"] = reader["Infor_09"]?.ToString() ?? "",
                                            ["Infor_10"] = reader["Infor_10"]?.ToString() ?? "",
                                            ["UserUpdate"] = reader["UserUpdate"]?.ToString() ?? "",
                                            ["WorkDate"] = reader["WorkDate"] == DBNull.Value ? "" : reader["WorkDate"]
                                        };

                                        reportDataList.Add(rowData);
                                    }
                                }
                            }
                    }
               

                model.ReportData = reportDataList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer list");
                model.ReportData = new List<Dictionary<string, object>>();
            }

        }
        public IActionResult ExportExcel(string? Customer, DateTime? dateFrom, DateTime? dateEnd, string? Search)
        {
            // Tạo Excel file (ví dụ với ClosedXML hoặc EPPlus)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Đường dẫn tới file template
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Report", "BCCLBC.xlsx");

            using var package = new ExcelPackage(new FileInfo(templatePath));
            var worksheet = package.Workbook.Worksheets[0]; // Lấy sheet đầu tiên, hoặc by name: ["Sheet1"]
            var model = new FCATrackingViewModel
            {
                
                Customer = Customer ?? "",
                DateFrom = dateFrom ?? DateTime.Now,
                DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1),
                Searching = Search
            };
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            // string sql;

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