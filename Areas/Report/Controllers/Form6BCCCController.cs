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


namespace QOS.Areas.Report.Controllers
{
    [Authorize] // chỉ khi login mới được vào
    [Area("Report")]
    public class Form6BCCCController : Controller
    {
        private readonly ILogger<Form6BCCCController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly AppDbContext _context;

        public Form6BCCCController(ILogger<Form6BCCCController> logger, IWebHostEnvironment env, IConfiguration configuration, AppDbContext context)
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
            return RedirectToAction("RP_Form6", "Form6BCCC");
        }
        [TempData]
        public string? MessageStatus { get; set;}
        [HttpGet]
        public IActionResult RP_Form6(string? Unit, DateTime? dateFrom, DateTime? dateEnd)
        {
            _logger.LogInformation("=== RP_Form6 GET Request ===");
            _logger.LogInformation($"Parameters - Unit: '{Unit}', DateFrom: {dateFrom}, DateEnd: {dateEnd}");
            try
            {
                var model = new RP_Form6ViewModel
                {
                    Unit_List = GetUnitList(),
                    Unit = Unit ?? "ALL",
                    DateFrom = dateFrom ?? DateTime.Now,
                    DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1),
                    ReportData = new List<Dictionary<string, object>>()
                    
                };
                _logger.LogInformation($"Model created - Units available: {model.Unit_List.Count}");

                LoadReportData(model); // đã viết
                _logger.LogInformation($"Model created - ReportData available: {model.ReportData.Count}");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RP_Form6 GET");
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return View(new RP_Form6ViewModel { Unit_List = GetUnitList() });
            }
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
        private void LoadReportData(RP_Form6ViewModel model)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("Form6_BCKCC_SUM_Report", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Date_F", model.DateFrom);
            cmd.Parameters.AddWithValue("@Date_T", model.DateEnd);
            cmd.Parameters.AddWithValue("@Unit", model.Unit ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Factory", "REG2");

            conn.Open();
            using var reader = cmd.ExecuteReader();

            var result = new List<Dictionary<string, object>>();
            while (reader.Read())
            {
                var dict = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dict[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                result.Add(dict);
            }
            model.ReportData = result;

            // Mapping sang chart
                if (model.Unit == "ALL" || string.IsNullOrEmpty(model.Unit))
                {
                    // === Trường hợp ALL: lấy dữ liệu theo hàng (Unit vs OQL_TT, OQL_Target) ===
                    foreach (var row in model.ReportData)
                    {
                        var unit_v = row.ContainsKey("Unit") ? row["Unit"]?.ToString() : null;

                        if (row.ContainsKey("OQL_TT") && double.TryParse(row["OQL_TT"]?.ToString(), out var oqlTT))
                        {
                            model.DataPointsREG.Add(new ChartPoint { Label = unit_v, Y = Math.Round(oqlTT * 100.0, 2) });
                        }

                        if (row.ContainsKey("OQL_Target") && double.TryParse(row["OQL_Target"]?.ToString(), out var oqlTarget))
                        {
                            model.DataPointsUnitTarget.Add(new ChartPoint { Label = unit_v,  Y = Math.Round(oqlTarget * 100.0, 2) });
                        }
                    }
                }
                else
                {
                    // === Trường hợp chọn 1 Unit cụ thể: lấy dữ liệu theo cột động ===
                    var row = model.ReportData.FirstOrDefault(); // Vì SP thường trả 1 dòng với nhiều cột CL
                    if (row != null)
                    {
                        foreach (var kvp in row)
                        {
                            // Bỏ qua cột mặc định như Unit, OQL_TT, OQL_Target
                            if (kvp.Key == "Unit" || kvp.Key == "OQL_TT" || kvp.Key == "OQL_Target") continue;

                            if (kvp.Value == null) continue;
                            var parts = kvp.Value.ToString().Split('_'); 
                            if (parts.Length < 4) continue;

                            var color = parts[0];   // red / green / yellow
                            var value = double.Parse(parts[1]); // số %
                            var target = double.Parse(parts[2]); // target
                            var code = parts[3];   // ví dụ 201S11

                            model.DataPointsREG.Add(new ChartPoint
                            {
                                Label = kvp.Key,
                                Y = Math.Round(value * 100, 2)
                            });

                            model.DataPointsUnitTarget.Add(new ChartPoint
                            {
                                Label = kvp.Key,
                                Y = Math.Round(target * 100, 2)
                            });
                        }
                    }
                }
        }

        [HttpGet]
        public IActionResult GetLineHistory(string lineCode, DateTime dateFrom, DateTime dateEnd)
        {
            _logger.LogInformation($"=== Get Line History - Line: '{lineCode}', From: {dateFrom:yyyy-MM-dd}, To: {dateEnd:yyyy-MM-dd} ===");

            try
            {
                var historyData = GetLineHistoryData(lineCode, dateFrom, dateEnd);
                _logger.LogInformation($"Line history loaded - {historyData.Count} records");
                
                if (historyData == null || !historyData.Any())
                {
                    return PartialView("_LineHistory", Enumerable.Empty<LineHistoryDataForm6>());
                }

                return PartialView("_LineHistory", historyData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting line history for {LineCode}", lineCode);
                return Json(new { success = false, message = ex.Message });
            }
        }
        private List<LineHistoryDataForm6> GetLineHistoryData(string lineCode, DateTime dateFrom, DateTime dateEnd)
        {
            var historyList = new List<LineHistoryDataForm6>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = @"
                    SELECT  
                        t1.*, t4.FullName,  
                        ROW_NUMBER() OVER(PARTITION BY t1.Report_ID, t1.Line  
                                                ORDER BY t1.Line ASC, t1.LastUpdate DESC) AS rk
                    FROM Form6_BCKCC t1 LEFT JOIN User_List t4 ON t1.UserUpdate=t4.UserName 
                    WHERE t1.Line = @LineCode 
                        AND CAST(t1.LastUpdate as DATE) >= CAST(@DateFrom as DATE)
                        AND CAST(t1.LastUpdate as DATE) <= CAST(@DateEnd as DATE)
                    
                    ORDER BY t1.LastUpdate DESC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@LineCode", lineCode);
                command.Parameters.AddWithValue("@DateFrom", dateFrom);
                command.Parameters.AddWithValue("@DateEnd", dateEnd);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int totalFault = reader["Total_Fault_QTY"] != DBNull.Value ? Convert.ToInt32(reader["Total_Fault_QTY"]) : 0;
                    int checkQty   = reader["Check_QTY"] != DBNull.Value ? Convert.ToInt32(reader["Check_QTY"]) : 0;
                    historyList.Add(new LineHistoryDataForm6
                    {
                        ID = reader["ID"]  != DBNull.Value ? Convert.ToInt32(reader["ID"]) : 0,
                        Report_ID = reader["Report_ID"]?.ToString(),
                        MO = reader["MO"]?.ToString(),
                        Color = reader["Color"]?.ToString(),
                        AQL = reader["AQL"]?.ToString(),
                        Total_Fault_QTY = reader["Total_Fault_QTY"] != DBNull.Value ? Convert.ToInt32(reader["Total_Fault_QTY"]) : 0,
                        QTY = reader["QTY"] != DBNull.Value ? Convert.ToInt32(reader["QTY"]) : 0,
                        Check_QTY = reader["Check_QTY"] != DBNull.Value ? Convert.ToInt32(reader["Check_QTY"]) : 0,
                        Status = reader["Led"]?.ToString(),
                        OQL = (checkQty > 0 ) ? Math.Round((double)totalFault / checkQty, 2) : 0,
                        Audit_Time = reader["Audit_Time"]?.ToString(),
                        UserUpdate = reader["UserUpdate"]?.ToString(),
                        FullName = reader["FullName"]?.ToString(),
                        LastUpdate = reader["LastUpdate"] != DBNull.Value ? Convert.ToDateTime(reader["LastUpdate"]) : DateTime.MinValue,
                        RowNum = reader["rk"] != DBNull.Value ? Convert.ToInt32(reader["rk"]) : 0
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting line history for {LineCode}", lineCode);
                throw;
            }

            return historyList;
        }
        
        public IActionResult DetailForm6(int id)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string sql = @"
                SELECT t1.*, t4.FullName 
                FROM Form6_BCKCC t1
                LEFT JOIN User_List t4 ON t1.UserUpdate = t4.UserName
                WHERE t1.ID = @ID
            ";
            var detail = conn.QueryFirstOrDefault<Form6_Detail>(sql, new { ID = id });

            if (detail == null)
            {
                return NotFound("Không tìm thấy báo cáo với ID đã chọn.");
            }

            // Lấy danh sách lỗi từ bảng FaultCode
            string sqlFault = @"SELECT Fault_Code AS FaultCode,
                                    Fault_Name_VN AS FaultNameVN,
                                    Fault_Level AS FaultLevel
                                FROM Fault_Code
                                WHERE Form6_Active = 1
                                ORDER BY Fault_Level ASC, Fault_Name_VN ASC";

            var faults = conn.Query<Form6FaultViewModel>(sqlFault).ToList();
            // var faults = conn.Query<Form6FaultViewModel>("SELECT * FROM FaultCode ORDER BY FaultCode").ToList();

            // Tách lỗi từ trường Fault_Detail (giống PHP explode)
            List<Form6SelectedFault> selectedFaults = new();
            if (!string.IsNullOrEmpty(detail.Fault_Detail))
            {
                var arrFault = detail.Fault_Detail.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var f in arrFault)
                {
                    var parts = f.Split('-');
                    if (parts.Length >= 3)
                    {
                        selectedFaults.Add(new Form6SelectedFault
                        {
                            FaultCode = parts[0],
                            FaultQty = int.TryParse(parts[2], out var q) ? q : 0
                        });
                    }
                }
            }

            var model = new Form6DetailViewModel
            {
                Detail = detail,
                Faults = faults,
                SelectedFaults = selectedFaults
            };

            return PartialView("_tableRP_Form6", model);
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

        public IActionResult ExportExcel ( string? Unit, DateTime? dateFrom, DateTime? dateEnd)
        {
            // Tạo Excel file (ví dụ với ClosedXML hoặc EPPlus)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Đường dẫn tới file template
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Report", "RP_Form6_BCKCC.xlsx");
            
            if (!System.IO.File.Exists(templatePath))
            {
                MessageStatus = "Không tìm thấy file mẫu báo cáo.";
                return RedirectToAction("RP_Form6");
            }

            using var package = new ExcelPackage(new FileInfo(templatePath));
            var worksheet = package.Workbook.Worksheets[0];

            // Ghi tiêu đề báo cáo
            worksheet.Cells["A1"].Value = "BÁO CÁO KIỂM TRA CHẤT LƯỢNG CUỐI CHUYỀN - " + (Unit ?? "ALL");
            worksheet.Cells["A2"].Value = $"Từ ngày: {(dateFrom ?? DateTime.Now):dd/MM/yyyy}  Đến ngày: {(dateEnd ?? DateTime.Now):dd/MM/yyyy}";
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A2"].Style.Font.Bold = true;
            worksheet.Cells["A2"].Style.Font.Size = 12;

            // Lấy dữ liệu báo cáo
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("Form6_BCKCC_SUM_Report", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            
            cmd.Parameters.AddWithValue("@Date_F", dateFrom);
            cmd.Parameters.AddWithValue("@Date_T", dateEnd);
            cmd.Parameters.AddWithValue("@Unit", Unit);
            cmd.Parameters.AddWithValue("@Factory", "REG2");

            conn.Open();
            using (var reader = cmd.ExecuteReader())
            {
                int row = 4;
                int col;

                string[] nameList = null;

                while (reader.Read())
                {
                    worksheet.Cells[row, 1].Value = row - 3; // STT
                    worksheet.Cells[row, 2].Value = reader["Unit"].ToString();

                    // Dòng tiêu đề CL (chỉ chạy 1 lần ở row=4)
                    if (row == 4)
                    {
                        nameList = reader["CL"].ToString().Split(',');
                        col = 2;
                        foreach (var cl in nameList)
                        {
                            if (!string.IsNullOrWhiteSpace(cl))
                            {
                                col++;
                                worksheet.Cells[3, col].Value = cl;
                                
                            }
                        }
                    }

                    // Ghi dữ liệu theo Name_List
                    col = 2;
                    foreach (var cl in nameList)
                    {
                        if (!string.IsNullOrWhiteSpace(cl))
                        {
                            col++;
                            var val = reader[cl]?.ToString();
                            if (!string.IsNullOrEmpty(val))
                            {
                                var tmp = val.Split('_');
                                if (tmp.Length > 1)
                                    if (double.TryParse(tmp[1], out double numVal))
                                    {
                                        worksheet.Cells[row, col].Value = numVal;          // giữ số gốc
                                        worksheet.Cells[row, col].Style.Numberformat.Format = "0.00%"; 
                                    }
                                    else
                                    {
                                        worksheet.Cells[row, col].Value = tmp[1]; // fallback nếu không parse được
                                    } 
                            }
                        }
                    }

                    // Cột OQL_TT
                    worksheet.Cells[row, col + 1].Value = reader["OQL_TT"];
                    worksheet.Cells[row, col + 1].Style.Numberformat.Format = "0.00%";
                    row++;
                }
            }
            // --- PHẦN 2: Detail ---
            var worksheet2 = package.Workbook.Worksheets["Detail"];

            worksheet2.Cells["A2"].Value = (dateFrom ?? DateTime.Now).ToString("dd-MMM-yyyy");
            worksheet2.Cells["B2"].Value = (dateEnd ?? DateTime.Now).ToString("dd-MMM-yyyy");

            string sqlDetail;
            if (string.IsNullOrEmpty(Unit) || Unit == "ALL")
            {
                sqlDetail = @"
                SELECT t1.*, t4.FullName,
                        ROW_NUMBER() OVER(PARTITION BY t1.Report_ID, t1.Line
                                            ORDER BY t1.Line ASC, t1.LastUpdate DESC) AS rk
                FROM Form6_BCKCC t1
                LEFT JOIN User_List t4 ON t1.UserUpdate = t4.UserName
                WHERE CAST(t1.LastUpdate as DATE) >= @DateF
                    AND CAST(t1.LastUpdate as DATE) <= @DateT
                    
                ORDER BY t1.LastUpdate DESC";
            }
            else
            {
                sqlDetail = @"
                SELECT t1.*, t4.FullName,
                        ROW_NUMBER() OVER(PARTITION BY t1.Report_ID, t1.Line
                                            ORDER BY t1.Line ASC, t1.LastUpdate DESC) AS rk
                FROM Form6_BCKCC t1
                LEFT JOIN User_List t4 ON t1.UserUpdate = t4.UserName
                WHERE CAST(t1.LastUpdate as DATE) >= @DateF
                    AND CAST(t1.LastUpdate as DATE) <= @DateT
                    AND t1.Unit = @Unit
                ORDER BY t1.LastUpdate DESC";
            }

            using (var cmd2 = new SqlCommand(sqlDetail, conn))
            {
                cmd2.Parameters.AddWithValue("@DateF", dateFrom);
                cmd2.Parameters.AddWithValue("@DateT", dateEnd);
                cmd2.Parameters.AddWithValue("@Unit", Unit);
                if (conn.State != ConnectionState.Open) conn.Open();
                using (var reader = cmd2.ExecuteReader())
                {
                    int row = 4;
                    while (reader.Read())
                    {
                        worksheet2.Cells[row, 1].Value = row - 3; // STT
                        worksheet2.Cells[row, 2].Value = reader["Unit"].ToString();
                        worksheet2.Cells[row, 3].Value = reader["Line"].ToString();
                        worksheet2.Cells[row, 4].Value = reader["Report_ID"].ToString();
                        worksheet2.Cells[row, 5].Value = reader["MO"].ToString();
                        worksheet2.Cells[row, 6].Value = reader["Color"].ToString();
                        worksheet2.Cells[row, 7].Value = reader["AQL"].ToString();
                        worksheet2.Cells[row, 8].Value = reader["QTY"].ToString();
                        worksheet2.Cells[row, 9].Value = reader["Total_Fault_QTY"] + " / " + reader["Check_QTY"];
                        worksheet2.Cells[row, 10].Value = reader["Audit_Time"].ToString();

                        if (Convert.ToInt32(reader["Check_QTY"]) != 0)
                        {
                            double faultRate = Convert.ToDouble(reader["Total_Fault_QTY"]) /
                                                Convert.ToDouble(reader["Check_QTY"]);
                            worksheet2.Cells[row, 11].Value = Math.Round(faultRate, 3);
                        }

                        worksheet2.Cells[row, 12].Value = reader["UserUpdate"] + " - " + reader["FullName"];
                        worksheet2.Cells[row, 13].Value = reader["LastUpdate"].ToString();

                        row++;
                    }
                }
            }
            
        


            // Tạo file Excel để tải về
            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Report_EndLine_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }

       
    }
}