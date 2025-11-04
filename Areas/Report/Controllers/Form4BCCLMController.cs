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


namespace QOS.Areas.Report.Controllers
{
    [Authorize] // chỉ khi login mới được vào
    [Area("Report")]
    public class Form4BCCLMController : Controller
    {
        private readonly ILogger<Form4BCCLMController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly AppDbContext _context;

        public Form4BCCLMController(ILogger<Form4BCCLMController> logger, IWebHostEnvironment env, IConfiguration configuration, AppDbContext context)
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
            return RedirectToAction("RP_Form4", "Form4BCCLM");
        }
        [TempData]
        public string? MessageStatus { get; set;}
        [HttpGet]
        public IActionResult RP_Form4(string? Unit, DateTime? dateFrom, DateTime? dateEnd)
        {
            _logger.LogInformation("=== RP_Form4 GET Request ===");
            _logger.LogInformation($"Parameters - Unit: '{Unit}', DateFrom: {dateFrom}, DateEnd: {dateEnd}");

            try
            {
                var model = new RP_Form4ViewModel
                {
                    Unit_List = GetUnitList(),
                    Unit = Unit ?? "ALL",
                    DateFrom = dateFrom ?? DateTime.Now,
                    DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1)
                };

                _logger.LogInformation($"Model created - Units available: {model.Unit_List.Count}");
                LoadReportData(model);
                // Load data if parameters provided
                // if (HasSearchParameters(Unit, dateFrom, dateEnd))
                // {
                //     LoadReportData(model);
                //     // ViewData["Searched"] = true;
                //     _logger.LogInformation($"Data loaded - Found {model.ReportUnits.Count} units");
                // }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RP_Form4 GET");
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return View(new RP_Form4ViewModel { Unit_List = GetUnitList() });
            }
        }

        [HttpPost]
        public IActionResult RP_Form4(RP_Form4ViewModel model)
        {
            _logger.LogInformation("=== RP_Form4 POST Request ===");
            _logger.LogInformation($"Model - Unit: '{model.Unit}', DateFrom: {model.DateFrom:yyyy-MM-dd}, DateEnd: {model.DateEnd:yyyy-MM-dd}");

            try
            {
                model.Unit_List = GetUnitList();
                LoadReportData(model);
                ViewData["Searched"] = true;
                
                _logger.LogInformation($"POST completed - Found {model.ReportUnits.Count} units");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RP_Form4 POST");
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                model.Unit_List = GetUnitList();
                return View(model);
            }
        }
        [HttpGet]
        public IActionResult RP_Form4_Unit(string? Unit, DateTime? dateFrom, DateTime? dateEnd)
        {
            _logger.LogInformation($"GET Model - Unit: '{Unit}', DateFrom: {dateFrom:yyyy-MM-dd}, DateEnd: {dateEnd:yyyy-MM-dd}");
            if (string.IsNullOrEmpty(Unit))
            {
                MessageStatus = "Unit không được để trống";
                return RedirectToAction("RP_Form4");
            }
            try
            {
                var model = new RP_Form4_UnitViewModel
                {
                    Unit_List = GetUnitList(),
                    Unit = Unit,
                    DateFrom = dateFrom ?? DateTime.Now.AddDays(-1),
                    DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1),
                    ColumnHeaders = new List<string>(), // Initialize to prevent null
                    LineDetails = new List<LineDetailRow>() // Initialize to prevent null
                };

                LoadUnitDetailData(model);
                ViewData["Searched"] = true;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RP_Form4_Unit");
                MessageStatus = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("RP_Form4");
            }
        }
        // [HttpPost]
        // public IActionResult RP_Form4_Unit(RP_Form4_UnitViewModel model)
        // {
        //     _logger.LogInformation("=== RP_Form4_Unit POST Request ===");
        //     _logger.LogInformation($"Model - Unit: '{model.Unit}', DateFrom: {model.DateFrom:yyyy-MM-dd}, DateEnd: {model.DateEnd:yyyy-MM-dd}");

        //     try
        //     {
        //         model.Unit_List = GetUnitList();
        //         LoadUnitDetailData(model);
        //         ViewData["Searched"] = true;
                
                
        //         return View(model);
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error in RP_Form4_Unit POST");
        //         MessageStatus = $"Có lỗi xảy ra Post: {ex.Message}";
        //         model.Unit_List = GetUnitList();
        //         return View(model);
        //     }
        // }

        [HttpPost]
        public IActionResult ExportExcel(string? unit, DateTime dateFrom, DateTime dateEnd)
        {
            _logger.LogInformation($"=== Export Excel - Unit: '{unit}', From: {dateFrom:yyyy-MM-dd}, To: {dateEnd:yyyy-MM-dd} ===");

            try
            {
                var model = new RP_Form4ViewModel
                {
                    Unit = unit ?? "ALL",
                    DateFrom = dateFrom,
                    DateEnd = dateEnd
                };

                LoadReportData(model);

                var excelData = GenerateExcelData(model);
                var fileName = $"BCCLM_{dateFrom:yyyyMMdd}_{dateEnd:yyyyMMdd}.csv";
                
                _logger.LogInformation($"Excel exported successfully - {model.ReportUnits.Count} units, {model.ReportUnits.Sum(u => u.Lines.Count)} lines");
                
                return File(excelData, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting Excel");
                TempData["ErrorMessage"] = $"Lỗi xuất Excel: {ex.Message}";
                return RedirectToAction("RP_Form4");
            }
        }
        // [HttpPost]
        // public IActionResult ExportExcel_Unit(string? unit, DateTime dateFrom, DateTime dateEnd)
        // {
        //     _logger.LogInformation($"=== Export Excel - Unit: '{unit}', From: {dateFrom:yyyy-MM-dd}, To: {dateEnd:yyyy-MM-dd} ===");

        //     try
        //     {
        //         var model = new RP_Form4_UnitViewModel
        //         {
        //             Unit_List = GetUnitList(),
        //             Unit = unit,
        //             DateFrom = dateFrom ,
        //             DateEnd = dateEnd ,
        //             ColumnHeaders = new List<string>(), // Initialize to prevent null
        //             LineDetails = new List<LineDetailRow>() // Initialize to prevent null
        //         };

        //         LoadUnitDetailData(model);

        //         var excelData = GenerateExcelData_Unit(model);
        //         var fileName = $"BCCLM_{dateFrom:yyyyMMdd}_{dateEnd:yyyyMMdd}.csv";
                
        //         _logger.LogInformation($"Excel exported successfully - {model.ReportUnits.Count} units, {model.ReportUnits.Sum(u => u.Lines.Count)} lines");
                
        //         return File(excelData, "text/csv", fileName);
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error exporting Excel");
        //         TempData["ErrorMessage"] = $"Lỗi xuất Excel: {ex.Message}";
        //         return RedirectToAction("RP_Form4");
        //     }
        // }

        [HttpGet]
        public IActionResult GetLineHistory(string lineCode, DateTime dateFrom, DateTime dateEnd)
        {
            _logger.LogInformation($"=== Get Line History - Line: '{lineCode}', From: {dateFrom:yyyy-MM-dd}, To: {dateEnd:yyyy-MM-dd} ===");

            try
            {
                var historyData = GetLineHistoryData(lineCode, dateFrom, dateEnd);
                _logger.LogInformation($"Line history loaded - {historyData.Count} records");
                
                // _logger.LogInformation("Line history data: {Json}", 
                //     JsonSerializer.Serialize(historyData, new JsonSerializerOptions
                //     {
                //         WriteIndented = true // format dễ đọc
                //     }));

                if (historyData == null || !historyData.Any())
                {
                    return PartialView("_LineHistory", Enumerable.Empty<LineHistoryData>());
                }

                return PartialView("_LineHistory", historyData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting line history for {LineCode}", lineCode);
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetSewerLineHistory(string lineCode,string position, DateTime dateFrom, DateTime dateEnd)
        {
            _logger.LogInformation($"=== Get GetSewerLineHistory History - Line: '{lineCode}',position: '{position}', From: {dateFrom:yyyy-MM-dd}, To: {dateEnd:yyyy-MM-dd} ===");

            try
            {
                var historyData = GetSewerLineHistoryData(lineCode,position, dateFrom, dateEnd);
                _logger.LogInformation($"Line history loaded - {historyData.Count} records");
                
                // _logger.LogInformation("Line history data: {Json}", 
                //     JsonSerializer.Serialize(historyData, new JsonSerializerOptions
                //     {
                //         WriteIndented = true // format dễ đọc
                //     }));

                if (historyData == null || !historyData.Any())
                {
                    return PartialView("_LineHistory", Enumerable.Empty<LineHistoryData>());
                }

                return PartialView("_LineHistory", historyData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting line history for {LineCode}", lineCode);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult TestSQL(DateTime? dateFrom = null, DateTime? dateEnd = null, string? unit = null)
        {
            dateFrom ??= DateTime.Now.AddDays(-7);
            dateEnd ??= DateTime.Now.Date.AddDays(1).AddTicks(-1);
            unit ??= "ALL";

            _logger.LogInformation("=== Testing SQL Stored Procedure ===");

            try
            {
                var result = new
                {
                    ConnectionTest = TestConnection(),
                    StoredProcedureTest = TestStoredProcedure(dateFrom.Value, dateEnd.Value, unit),
                    Parameters = new
                    {
                        DateFrom = dateFrom.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                        DateEnd = dateEnd.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                        Unit = unit,
                        FactoryID = "REG2"
                    }
                };

                return Json(new { success = true, result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing SQL");
                return Json(new { success = false, error = ex.Message });
            }
        }

        // #region Private Methods

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

        private bool HasSearchParameters(string? unit, DateTime? dateFrom, DateTime? dateEnd)
        {
            _logger.LogInformation($"HasSearchParameters - Unit: '{unit}', DateFrom: {dateFrom}, DateEnd: {dateEnd}");
            return !string.IsNullOrEmpty(unit) || dateFrom.HasValue || dateEnd.HasValue;
        }

        private void LoadReportData(RP_Form4ViewModel model)
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

                using var command = new SqlCommand("RP_BaoCaoChatLuongChuyenMay_Unit", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Date_From", dateFStr);
                command.Parameters.AddWithValue("@Date_To", dateTStr);
                command.Parameters.AddWithValue("@Line_Type", model.Unit == "ALL" ? "" : model.Unit);
                command.Parameters.AddWithValue("@Factory", "REG2");

                using var reader = command.ExecuteReader();
                var reportUnits = new List<ReportUnit>();
                int rowCount = 0;

                while (reader.Read())
                {
                    rowCount++;
                    var unit = reader["Unit"]?.ToString() ?? "";
                    var lineLed = reader["Line_Led"]?.ToString() ?? "";

                    // _logger.LogInformation($"Row {rowCount} - Unit: '{unit}', Line_Led: '{lineLed}'");

                    var reportUnit = new ReportUnit
                    {
                        Unit = unit,
                        LineLed = lineLed,
                        Lines = ParseLineData(lineLed)
                    };

                    reportUnits.Add(reportUnit);
                }

                model.ReportUnits = reportUnits;
                _logger.LogInformation($"Data loading completed - {rowCount} units, {reportUnits.Sum(u => u.Lines.Count)} total lines");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading report data");
                model.Message = $"Lỗi tải dữ liệu: {ex.Message}";
                throw;
            }
        }
        private void LoadUnitDetailData(RP_Form4_UnitViewModel  model)
        {
            _logger.LogInformation("=== Loading Report LoadUnitDetailData ===");
            _logger.LogInformation($"Parameters - DateFrom: {model.DateFrom:yyyy-MM-dd HH:mm:ss}, DateEnd: {model.DateEnd:yyyy-MM-dd HH:mm:ss}, Unit: '{model.Unit}'");

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var command = new SqlCommand("RP_BaoCaoChatLuongChuyenMay_Unit_Detail", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // IMPORTANT: kiểm tra tên param của SP. Thay đổi @Date_F / @Date_T nếu SP dùng tên khác.
                command.Parameters.Add("@Date_From", SqlDbType.VarChar).Value = model.DateFrom.ToString("yyyy-MM-dd");
                command.Parameters.Add("@Date_To",   SqlDbType.VarChar).Value = model.DateEnd.ToString("yyyy-MM-dd");
                command.Parameters.Add("@Unit",      SqlDbType.VarChar).Value = (model.Unit == "ALL" ? "" : model.Unit);

                using var reader = command.ExecuteReader();

                if (reader == null)
                {
                    _logger.LogWarning("SqlDataReader is null (no result set).");
                    model.Message = "Stored procedure không trả result set.";
                    return;
                }

                // Build availableColumns safely: ưu tiên schemaTable, nếu null fallback dùng reader.GetName
                HashSet<string> availableColumns = new(StringComparer.OrdinalIgnoreCase);
                var schemaTable = reader.GetSchemaTable();
                if (schemaTable != null)
                {
                    availableColumns = schemaTable.Rows
                        .Cast<DataRow>()
                        .Select(r => r["ColumnName"]?.ToString() ?? "")
                        .Where(n => !string.IsNullOrEmpty(n))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var name = reader.GetName(i);
                        if (!string.IsNullOrEmpty(name)) availableColumns.Add(name);
                    }
                }
                _logger.LogInformation("Available columns from resultset: {Cols}", string.Join(",", availableColumns));

                var lineDetails = new List<LineDetailRow>();
                bool firstRow = true;

                while (reader.Read())
                {
                    // xử lý header CL ở bản ghi đầu (nếu có)
                    if (firstRow)
                    {
                        if (availableColumns.Contains("CL"))
                        {
                            var clRaw = reader["CL"] != DBNull.Value ? reader["CL"].ToString()! : "";
                            model.ColumnHeaders = clRaw
                                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .ToList();
                            _logger.LogInformation("ColumnHeaders from CL: {Headers}", string.Join(",", model.ColumnHeaders));
                        }
                        else
                        {
                            // fallback: lấy các column khả dụng, loại bỏ các cột meta như Line, CL, ...
                            model.ColumnHeaders = availableColumns
                                .Where(c => !string.Equals(c, "Line", StringComparison.OrdinalIgnoreCase)
                                        && !string.Equals(c, "CL", StringComparison.OrdinalIgnoreCase))
                                .OrderBy(c => c) // hoặc theo thứ tự bạn muốn
                                .ToList();

                            _logger.LogWarning("CL column not found. Using available columns as headers: {Headers}", string.Join(",", model.ColumnHeaders));
                        }

                        firstRow = false;
                    }

                    var row = new LineDetailRow
                    {
                        Line = availableColumns.Contains("Line") && reader["Line"] != DBNull.Value ? reader["Line"].ToString()! : ""
                    };

                    // Gán từng cột an toàn
                    foreach (var col in model.ColumnHeaders)
                    {
                        try
                        {
                            if (availableColumns.Contains(col))
                            {
                                var val = reader[col];
                                row.StatusByStep[col] = val == DBNull.Value ? null : val?.ToString();
                            }
                            else
                            {
                                // cột không có trong resultset
                                row.StatusByStep[col] = null;
                                _logger.LogDebug("Column {Col} is not present for Line {Line}", col, row.Line);
                            }
                        }
                        catch (Exception ex)
                        {
                            // log cục bộ, tiếp tục đọc cột khác
                            _logger.LogWarning(ex, "Error reading column {Col} for Line {Line}", col, row.Line);
                            row.StatusByStep[col] = null;
                        }
                    }

                    lineDetails.Add(row);
                }

                model.LineDetails = lineDetails;

                if (!model.LineDetails.Any())
                {
                    _logger.LogWarning("No rows returned for Unit={Unit}, DateFrom={From}, DateEnd={To}", model.Unit, model.DateFrom, model.DateEnd);
                    model.Message = "Không có dữ liệu cho khoảng thời gian/Unit đã chọn.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading unit detail data");
                model.Message = $"Lỗi tải dữ liệu chi tiết: {ex.Message}";
                throw;
            }
        }
        private (string circleClass, string statusText) GetDetailStatusInfo(string value)
        {
            return value?.ToLower() switch
            {
                "red" => ("bg-danger Circles_Red", "Lỗi nặng"),
                "green" => ("bg-success", "Không có lỗi"),
                "yellow" => ("bg-warning", "Lỗi nhẹ"),
                _ => ("bg-secondary", "Chưa kiểm tra")
            };
        }

        private List<LineData> ParseLineData(string lineLed)
        {
            var lines = new List<LineData>();

            if (string.IsNullOrEmpty(lineLed))
                return lines;

            var lineList = lineLed.Split(';', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lineList)
            {
                if (line.Length >= 2)
                {
                    var lineCode = line.Substring(0, line.Length - 2);
                    var colorCode = line.Substring(line.Length - 1);
                    var (circleClass, statusText) = GetStatusInfo(colorCode);

                    lines.Add(new LineData
                    {
                        LineCode = lineCode,
                        ColorCode = colorCode,
                        CircleClass = circleClass,
                        StatusText = statusText
                    });
                }
            }

            return lines;
        }

        private (string circleClass, string statusText) GetStatusInfo(string colorCode)
        {
            return colorCode switch
            {
                "R" => ("bg-danger Circles_Red", "Lỗi nặng"),
                "G" => ("bg-success Circles_Green", "Không có lỗi"),
                "Y" => ("bg-warning Circles_Yellow", "Lỗi nhẹ"),
                _ => ("bg-secondary", "Chưa kiểm tra")
            };
        }

        private List<LineHistoryData> GetLineHistoryData(string lineCode, DateTime dateFrom, DateTime dateEnd)
        {
            var historyList = new List<LineHistoryData>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = @"
                    SELECT  
                        t1.*, t3.Operation_Name_VN, t4.FullName,  
                        ROW_NUMBER() OVER(PARTITION BY t1.Report_ID, t1.Line  
                                                ORDER BY t1.Line ASC, t1.LastUpdate DESC) AS rk
                    FROM Form4_BCCLM t1 LEFT JOIN dbo.Operation_Code t3 ON t1.Operation=t3.Operation_Code LEFT JOIN User_List t4 ON t1.UserUpdate=t4.UserName 
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
                    historyList.Add(new LineHistoryData
                    {
                        ID = reader["ID"]  != DBNull.Value ? Convert.ToInt32(reader["ID"]) : 0,
                        Report_ID = reader["Report_ID"]?.ToString(),
                        Operation_Name_VN = reader["Operation_Name_VN"]?.ToString(),
                        Sewer = reader["Sewer"]?.ToString(),
                        Total_Fault_QTY = reader["Total_Fault_QTY"] != DBNull.Value ? Convert.ToInt32(reader["Total_Fault_QTY"]) : 0,
                        QTY = reader["QTY"] != DBNull.Value ? Convert.ToInt32(reader["QTY"]) : 0,
                        Status = reader["Led"]?.ToString(),
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
        private List<LineHistoryData> GetSewerLineHistoryData(string lineCode,string position, DateTime dateFrom, DateTime dateEnd)
        {
            //  _logger.LogInformation($"=== Get GetSewerLineHistory History - Line: '{lineCode}',position: '{position}', From: {dateFrom}, To: {dateEnd:yyyy-MM-dd} ===");
            var historyList = new List<LineHistoryData>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var query = @"
                    SELECT  
                        t1.*, t3.Operation_Name_VN, t4.FullName,  
                        ROW_NUMBER() OVER(PARTITION BY t1.Report_ID, t1.PhysicalLine  
                                                ORDER BY t1.PhysicalLine ASC, t1.LastUpdate DESC) AS rk
                    FROM Form4_BCCLM t1 LEFT JOIN dbo.Operation_Code t3 ON t1.Operation=t3.Operation_Code LEFT JOIN User_List t4 ON t1.UserUpdate=t4.UserName 
                    WHERE t1.PhysicalLine = @LineCode 
                        AND t1.Sewer_Workstation= @Position
                        AND CAST(t1.LastUpdate as DATE) >= CAST(@DateFrom as DATE)
                        AND CAST(t1.LastUpdate as DATE) <= CAST(@DateEnd as DATE)
                    
                    ORDER BY t1.LastUpdate DESC";
                
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@LineCode", lineCode);
                command.Parameters.AddWithValue("@Position", position);
                command.Parameters.AddWithValue("@DateFrom", dateFrom);
                command.Parameters.AddWithValue("@DateEnd", dateEnd);
                // _logger.LogInformation("Executing SQL: {Sql}", GetDebugSql(command));
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    historyList.Add(new LineHistoryData
                    {
                        ID = reader["ID"]  != DBNull.Value ? Convert.ToInt32(reader["ID"]) : 0,
                        Report_ID = reader["Report_ID"]?.ToString(),
                        Operation_Name_VN = reader["Operation_Name_VN"]?.ToString(),
                        Sewer = reader["Sewer"]?.ToString(),
                        Total_Fault_QTY = reader["Total_Fault_QTY"] != DBNull.Value ? Convert.ToInt32(reader["Total_Fault_QTY"]) : 0,
                        QTY = reader["QTY"] != DBNull.Value ? Convert.ToInt32(reader["QTY"]) : 0,
                        Status = reader["Led"]?.ToString(),
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

        private byte[] GenerateExcelData(RP_Form4ViewModel model)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Unit,Line Code,Status,Status Text,Date From,Date To");

            foreach (var unit in model.ReportUnits)
            {
                foreach (var line in unit.Lines)
                {
                    csv.AppendLine($"\"{unit.Unit}\",\"{line.LineCode}\",\"{line.ColorCode}\",\"{line.StatusText}\",\"{model.DateFrom:yyyy-MM-dd}\",\"{model.DateEnd:yyyy-MM-dd}\"");
                }
            }

            return System.Text.Encoding.UTF8.GetPreamble()
                .Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString()))
                .ToArray();
        }
        // private byte[] GenerateExcelData_Unit(RP_Form4_UnitViewModel model)
        // {
        //     var csv = new System.Text.StringBuilder();
        //     csv.AppendLine("Unit,Line Code,Status,Status Text,Date From,Date To");
        //     string unitName = string.IsNullOrEmpty(model.Unit) ? "" : model.Unit;
        //     string dateFrom = model.DateFrom.ToString("yyyy-MM-dd");
        //     string dateTo = model.DateEnd.ToString("yyyy-MM-dd");
        //     foreach (var line in model.LineDetails)
        //     {
        //         var lineCode = line?.Line ?? "";

        //         // Nếu StatusByStep null thì skip
        //         if (line?.StatusByStep == null || !line.StatusByStep.Any())
        //         {
        //             // ghi 1 dòng trắng/không có step nếu bạn muốn:
        //             // sb.AppendLine($"\"{EscapeCsv(unitName)}\",\"{EscapeCsv(lineCode)}\",\"\",\"\",\"{dateFrom}\",\"{dateTo}\"");
        //             continue;
        //         }

        //         foreach (var kv in line.StatusByStep)
        //         {
        //             var step = kv.Key ?? "";
        //             var status = kv.Value ?? "";

        //             csv.AppendLine($"\"{EscapeCsv(unitName)}\",\"{EscapeCsv(lineCode)}\",\"{EscapeCsv(step)}\",\"{EscapeCsv(status)}\",\"{dateFrom}\",\"{dateTo}\"");
        //         }
        //     }

        //     return System.Text.Encoding.UTF8.GetPreamble()
        //         .Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString()))
        //         .ToArray();
        // }
        // private string EscapeCsv(string value)
        // {
        //     if (string.IsNullOrEmpty(value))
        //         return "";

        //     // Nếu có dấu phẩy, ngoặc kép hoặc xuống dòng thì phải bao quanh bằng dấu "
        //     if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        //     {
        //         // Escape dấu " thành ""
        //         value = value.Replace("\"", "\"\"");
        //         return $"\"{value}\"";
        //     }

        //     return value;
        // }

        private object TestConnection()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                _logger.LogInformation("Connection test successful");
                return new { Success = true, Message = "Connection successful" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed");
                return new { Success = false, Error = ex.Message };
            }
        }

        private object TestStoredProcedure(DateTime dateFrom, DateTime dateEnd, string unit)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // Check if stored procedure exists
                var checkQuery = "SELECT COUNT(*) FROM sys.procedures WHERE name = 'RP_BaoCaoChatLuongChuyenMay_Unit'";
                using var checkCmd = new SqlCommand(checkQuery, connection);
                var procExists = (int)checkCmd.ExecuteScalar() > 0;

                if (!procExists)
                {
                    return new { Success = false, Error = "Stored procedure 'RP_BaoCaoChatLuongChuyenMay_Unit' does not exist" };
                }

                // Execute stored procedure
                using var command = new SqlCommand("RP_BaoCaoChatLuongChuyenMay_Unit", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@DateF", dateFrom);
                command.Parameters.AddWithValue("@DateT", dateEnd);
                command.Parameters.AddWithValue("@Unit", unit == "ALL" ? "" : unit);
                command.Parameters.AddWithValue("@FactoryID", "REG2");

                var results = new List<object>();
                using var reader = command.ExecuteReader();
                
                while (reader.Read())
                {
                    var result = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        if (reader.IsDBNull(i))
                        {
                            result[reader.GetName(i)] = "";
                        }
                        else
                        {
                            result[reader.GetName(i)] = reader[i] as object;
                        }
                    }
                    results.Add(result);
                }

                _logger.LogInformation($"Stored procedure executed successfully, returned {results.Count} rows");
                return new { Success = true, RowCount = results.Count, SampleData = results.Take(3) };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stored procedure test failed");
                return new { Success = false, Error = ex.Message };
            }
        }

        public IActionResult DetailForm4(string id) 
        {
            using var conn = new SqlConnection(_connectionString);
            string sql = @"
                SELECT t1.*, t4.FullName 
                FROM Form4_BCCLM t1
                LEFT JOIN User_List t4 ON t1.UserUpdate = t4.UserName
                WHERE t1.ID = @ID
                ORDER BY t1.LastUpdate DESC";

            var detail = conn.QueryFirstOrDefault<Form4_Detail>(sql, new { ID = id });

            if (detail == null)
            {
                return NotFound();
            }
            // Lấy danh sách lỗi
            string sqlFault = @"SELECT Fault_Code AS FaultCode,
                                    Fault_Name_VN AS FaultNameVN,
                                    Fault_Level AS FaultLevel
                                FROM Fault_Code
                                WHERE Form4_Active = 1
                                ORDER BY Fault_Level ASC, Fault_Name_VN ASC";

            var faults = conn.Query<FaultViewModel>(sqlFault).ToList();

            // Tách lỗi từ trường Fault_Detail (giống PHP explode)
            List<SelectedFault> selectedFaults = new();
            if (!string.IsNullOrEmpty(detail.Fault_Detail))
            {
                var arrFault = detail.Fault_Detail.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var f in arrFault)
                {
                    var parts = f.Split('-');
                    if (parts.Length >= 3)
                    {
                        selectedFaults.Add(new SelectedFault
                        {
                            FaultCode = parts[0],
                            FaultQty = int.TryParse(parts[2], out var q) ? q : 0
                        });
                    }
                }
            }

            // Lấy công đoạn
            string Operation_Code = detail.Operation ?? "";
            
            string sqlOperation = @"SELECT Operation_Code AS Operation_Code,
                        Operation_Name_VN AS Operation_Name_VN
                    FROM Operation_Code
                    WHERE Operation_Code = @Operation_Code
                    ";

            var operations = conn.Query<OperationCode>(sqlOperation, new { Operation_Code }).ToList();


            // Gom vào ViewModel
            var vm = new Form4DetailViewModel
            {
                Detail = detail,
                Faults = faults,
                SelectedFaults = selectedFaults,
                Operations = operations
            };

            return PartialView("_tableRP_Form4", vm);
        }

        public IActionResult DeleteReport(int reportId)
        {
            var userName = User.Identity?.Name;
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            using var command = new SqlCommand("Delet_BC_Form4_BCCLM", conn);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Report_ID", reportId);
            command.Parameters.AddWithValue("@UserUpdate", userName);
            
            Console.WriteLine("report ID: " + reportId + "User : " + userName);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var result = reader["Result"]?.ToString();
                if (result == "Success")
                {
                    return Json(new { success = true, message = "Xóa báo cáo thành công!" });
                }
                else
                {
                    var errorMessage = reader["ErrorMessage"]?.ToString() ?? "Không thể xóa báo cáo.";
                    return Json(new { success = false, message = errorMessage });
                }
            }
            else
            {
                return Json(new { success = false, message = "Không tìm thấy báo cáo để xóa." });
            }
        }

        public static string GetDebugSql(SqlCommand cmd)
        {
            string sql = cmd.CommandText;

            foreach (SqlParameter p in cmd.Parameters)
            {
                string value;

                if (p.Value == null || p.Value == DBNull.Value)
                {
                    value = "NULL";
                }
                else if (p.Value is string s)
                {
                    value = $"N'{s.Replace("'", "''")}'";
                }
                else if (p.Value is DateTime dt)
                {
                    value = $"'{dt:yyyy-MM-dd HH:mm:ss}'";
                }
                else
                {
                    value = p.Value.ToString() ?? "NULL";
                }

                sql = sql.Replace(p.ParameterName, value);
            }

            return sql;
        }
    
    }
}