using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QOS.Data;
using QOS.Models;
using QOS.Areas.Report.Models;
using OfficeOpenXml;
using System.Data;
using System.Text.Json;
using QOS.Areas.Function.Filters;
using System.Linq;
using Dapper;

namespace QOS.Areas.Report.Controllers
{
    [Authorize]
    [Area("Report")]
    public class FQCTrackingController : Controller
    {

        private readonly ILogger<FQCTrackingController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public FQCTrackingController(ILogger<FQCTrackingController> logger, IWebHostEnvironment env, IConfiguration configuration, AppDbContext context)
        {
            _logger = logger;
            _env = env;
            _configuration =configuration;
            _context = context;
        }
        public IActionResult Index()
        {
            return RedirectToAction("FQCTracking", "FQCTracking");
        }
        public IActionResult FQCTracking(string? Customer,string? Industry, string? Operation, DateTime? dateFrom, DateTime? dateEnd, string? Search)
        {
            var model = new FQCTrackingViewModel
            {
                
                Customer_List = GetCustomerList(),
                Customer = Customer ?? "ALL",
                Industry = Industry ?? "ALL",
                Operation= Operation ?? "ALL",
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
                    // var value = reader["Infor_10"] == DBNull.Value ? "" : reader["Infor_10"].ToString();
                    var value = Convert.ToString(reader["Infor_10"]) ?? "";
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
        private void LoadReportData(FQCTrackingViewModel model)
        {
            try
            {
                // Prepare parameters for stored procedure
                var dateFrom = model.DateFrom.ToString("yyyy-MM-dd");
                var dateEnd = model.DateEnd.ToString("yyyy-MM-dd");
                // var customer = string.IsNullOrEmpty(model.Customer) ? "ALL" : model.Customer;
                var customer = model.Customer;
                var operation = model.Operation;
                var industry = model.Industry;
                var search = model.Searching;
                var reportDataList = new List<Dictionary<string, object>>();
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                    {
                        connection.Open();
                        // Execute stored procedure
                        var sql = @"EXEC TRACKING_FQC_Select_data_to_Report_OP @Operation,@Industry, @Customer,@Seach, @Date_F, @Date_T";
                
                        using (var command = new SqlCommand(sql, connection))
                            {
                                command.Parameters.AddWithValue("@Operation", operation ?? "");
                                command.Parameters.AddWithValue("@Industry", industry ?? "");
                                command.Parameters.AddWithValue("@Customer", customer ?? "");
                                command.Parameters.AddWithValue("@Seach", search ?? "");
                                command.Parameters.AddWithValue("@Date_F", dateFrom);
                                command.Parameters.AddWithValue("@Date_T", dateEnd);

                                // Log SQL
                                // string debugSql = sql;
                                // foreach (SqlParameter p in command.Parameters)
                                // {
                                //     string value = p.Value == null || p.Value == DBNull.Value
                                //         ? "NULL"
                                //         : $"N'{p.Value.ToString().Replace("'", "''")}'";
                                //     debugSql = debugSql.Replace(p.ParameterName, value);
                                // }

                                // _logger.LogInformation("Debug SQL: {debugSql}", debugSql);


                                using (var reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        var rowData = new Dictionary<string, object>
                                        {
                                            ["ID_Result"] = reader["ID_Result"]?.ToString() ?? "",
                                            ["Industry"] = reader["Industry"]?.ToString() ?? "",
                                            ["Operation"] = reader["Operation"]?.ToString() ?? "",
                                            ["ResultStatus"] = reader["ResultStatus"]?.ToString() ?? "",
                                            ["Customer"] = reader["Customer"]?.ToString() ?? "",
                                            ["SO"] = reader["SO"]?.ToString() ?? "",
                                            ["Style"] = reader["Style"]?.ToString() ?? "",
                                            ["PO"] = reader["PO"]?.ToString() ?? "",
                                            ["Qty"] = reader["Qty"]?.ToString() ?? "",
                                            ["Update_Date"] = reader["Update_Date"]?.ToString() ?? "",
                                            ["shipMode"] = reader["shipMode"]?.ToString() ?? "",
                                            ["Destination"] = reader["Destination"]?.ToString() ?? "",
                                            ["PRO"] = reader["PRO"]?.ToString() ?? "",
                                            ["Audit_Time"] = reader["Audit_Time"]?.ToString() ?? "",
                                            ["Total_Fault_QTY"] = reader["Total_Fault_QTY"]?.ToString() ?? "",
                                            ["Check_Qty"] = reader["Check_Qty"]?.ToString() ?? "",
                                            
                                            ["UserUpdate"] = reader["UserUpdate"]?.ToString() ?? "",
                                            ["WorkDate"] = reader["WorkDate"] == DBNull.Value ? DateTime.MinValue : reader["WorkDate"]
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

        public IActionResult DetailFQCTracking(string id, string user, string auditTime)
        {
            // _logger.LogInformation($"DetailFQCTracking called with ID: {id}, User: {user}, AuditTime: {auditTime}");
            FQCTracking_Detail? detail = null;
            List<FQCTrackingFaultViewModel> faults = new();
            List<FQCTrackingSelectedFault> selectedFaults = new();

            using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
            {
                connection.Open();

                // 1️⃣ Lấy thông tin chi tiết
                var sql = @"SELECT TOP 1 * 
                            FROM FQC_UQ_Result  
                            WHERE ID_Result=@ID AND UserUpdate=@User AND Audit_Time=@AuditTime
                            ORDER BY LastUpdate DESC";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@ID", id ?? "");
                    command.Parameters.AddWithValue("@User", user ?? "");
                    command.Parameters.AddWithValue("@AuditTime", auditTime ?? "");

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            detail = new FQCTracking_Detail
                            {
                                ID_Result = reader["ID_Result"]?.ToString() ?? "",
                                WorkDate = reader["WorkDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["WorkDate"]),
                                ID_Report = reader["ID_Report"]?.ToString() ?? "",
                                ResultStatus = reader["ResultStatus"]?.ToString() ?? "",
                                IMG_Result = reader["IMG_Result"]?.ToString() ?? "",
                                Size_Name = reader["Size_Name"]?.ToString() ?? "",
                                SO = reader["SO"]?.ToString() ?? "",
                                Style = reader["Style"]?.ToString() ?? "",
                                Item_No = reader["Item_No"]?.ToString() ?? "",
                                PO = reader["PO"]?.ToString() ?? "",
                                Unit = reader["Unit"]?.ToString() ?? "",
                                Operation = reader["Operation"]?.ToString() ?? "",
                                Update_Date = reader["Update_Date"]?.ToString() ?? "",
                                shipMode = reader["shipMode"]?.ToString() ?? "",
                                QTY = reader["QTY"] == DBNull.Value ? 0 : Convert.ToInt32(reader["QTY"]),
                                Check_QTY = reader["Check_QTY"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Check_QTY"]),
                              
                                OQL = reader["OQL"] == DBNull.Value ? 0 : Convert.ToDouble(reader["OQL"]),
                                Total_Fault_QTY = reader["Total_Fault_QTY"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Total_Fault_QTY"]),
                                
                                Fault = reader["Fault"]?.ToString() ?? "",
                                Destination = reader["Destination"]?.ToString() ?? "",
                                CreatedBy = reader["CreatedBy"]?.ToString() ?? "",
                                Re_Audit = reader["Re_Audit"] != DBNull.Value && Convert.ToBoolean(reader["Re_Audit"]),
                                Audit_Time = reader["Audit_Time"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Audit_Time"]),
                                LastUpdate = reader["LastUpdate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["LastUpdate"]),
                                // Photo_URL = reader["Photo_URL"]?.ToString() ?? "",
                                UserUpdate = reader["UserUpdate"]?.ToString() ?? ""
                            };
                        }
                    }
                }

                // 2️⃣ Lấy danh sách lỗi
                string sqlFault = @"
                    SELECT Fault_Code AS FaultCode,
                        Fault_Name_VN AS FaultNameVN,
                        Fault_Level AS FaultLevel
                    FROM Fault_Code
                    WHERE Form2_Active = 1
                    ORDER BY Fault_Level ASC, Fault_Name_VN ASC";

                faults = connection.Query<FQCTrackingFaultViewModel>(sqlFault).ToList();

                // 3️⃣ Parse danh sách lỗi đã chọn
                if (detail != null && !string.IsNullOrEmpty(detail.Fault))
                {
                    var arrFault = detail.Fault.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var f in arrFault)
                    {
                        var parts = f.Split('-');
                        if (parts.Length >= 3)
                        {
                            selectedFaults.Add(new FQCTrackingSelectedFault
                            {
                                FaultCode = parts[0],
                                FaultQty = int.TryParse(parts[2], out var q) ? q : 0
                            });
                        }
                    }
                }
            }

            // 4️⃣ Trả về PartialView
            var model = new FQCTrackingDetailViewModel
            {
                Detail = detail ?? new FQCTracking_Detail(),
                Faults = faults,
                SelectedFaults = selectedFaults
            };

            return PartialView("_tableRP_FQC", model);
        }


        [Permission("B_F4")]
        public IActionResult DeleteReport(int reportId)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string sql_Tranfer = @" INSERT INTO Form6_BCKCC_Delete SELECT * , @UserName,GETDATE() FROM Form6_BCKCC WHERE ID = @ID ";
            string sql_Delete = @" DELETE FROM Form6_BCKCC WHERE ID = @ID";
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


        public IActionResult ExportExcel(string? Customer, DateTime? dateFrom, DateTime? dateEnd, string? Search)
        {
            // Tạo Excel file (ví dụ với ClosedXML hoặc EPPlus)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Đường dẫn tới file template
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Report", "BCCLBC.xlsx");

            using var package = new ExcelPackage(new FileInfo(templatePath));
            var worksheet = package.Workbook.Worksheets[0]; // Lấy sheet đầu tiên, hoặc by name: ["Sheet1"]
            var model = new FQCTrackingViewModel
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