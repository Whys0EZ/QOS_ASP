using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QOS.Areas.Report.Models;
using QOS.Data;
using QOS.Models;
using System.Data;

namespace QOS.Areas.Report.Controllers
{
    [Authorize]
    [Area("Report")]
    public class OQLEndLineController : Controller
    {
        private readonly ILogger<OQLEndLineController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly AppDbContext _context;

        public OQLEndLineController(ILogger<OQLEndLineController> logger, IWebHostEnvironment environment, IConfiguration configuration, AppDbContext context)
        {
            _logger = logger ;
            _env = environment ;
            _configuration = configuration ;
            _context = context ;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";

        }
        public ActionResult Index()
        {
            return RedirectToAction("RP_OQLEndLine");
        }
        
        public ActionResult RP_OQLEndLine(string unit = "1U01", int month = 0, int year = 0)
            {
                if (month == 0) month = DateTime.Now.Month;
            if (year == 0) year = DateTime.Now.Year;

            var model = new OQLEndLineViewModel
            {
                SelectedUnit = unit,
                SelectedMonth = month,
                SelectedYear = year
            };
            // Lấy danh sách Unit
            model.DistinctUnits = GetUnitList();
            model.Zone = GetZone();
            // Lấy dữ liệu từ database
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var data = GetOQLData(unit, month, year);

            // Group theo Line
            var groupedData = data.GroupBy(x => x.Line).OrderBy(g => g.Key);

            foreach (var group in groupedData)
            {
                var lineData = new OQLLineData
                {
                    LineName = group.Key,
                    DailyValues = new List<double?>()
                };

                // Tạo 31 ô cho mỗi ngày
                for (int day = 1; day <= 31; day++)
                {
                    if (day > daysInMonth)
                    {
                        lineData.DailyValues.Add(null);
                    }
                    else
                    {
                        var dayData = group.FirstOrDefault(x => x.Work_Date.Day == day);
                        // Chuyển OQL sang phần trăm (nhân 100)
                        lineData.DailyValues.Add(dayData != null ? dayData.OQL * 100 : (double?)null);
                    }
                }

                model.Lines.Add(lineData);
            }

            // Tính Average cho mỗi ngày
            for (int day = 1; day <= 31; day++)
            {
                if (day > daysInMonth)
                {
                    model.AverageValues.Add(null);
                }
                else
                {
                    var values = model.Lines.Select(x => x.DailyValues[day - 1])
                                    .Where(x => x.HasValue)
                                    .Select(x => x.Value)
                                    .ToList();

                    if (values.Any())
                    {
                        model.AverageValues.Add(values.Average());
                    }
                    else
                    {
                        model.AverageValues.Add(null);
                    }
                }
            }

            return View(model);
        }

        // Action để download Excel
        public IActionResult DownloadReport(string unit, int month, int year)
        {
            var dataTable = GetDataForExport(unit, month, year);

            // Sử dụng EPPlus để export Excel
            // /* Uncomment khi đã cài EPPlus
            using (var package = new OfficeOpenXml.ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("OQL Report");
                worksheet.Cells["A1"].LoadFromDataTable(dataTable, true);
                
                // Format header
                using (var range = worksheet.Cells[1, 1, 1, dataTable.Columns.Count])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 61, 130));
                    range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                }
                
                worksheet.Cells.AutoFitColumns();
                
                var stream = new System.IO.MemoryStream(package.GetAsByteArray());
                return File(stream, 
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"OQL_Report_{unit}_{month}_{year}.xlsx");
            }
            // */

            /*return File(new byte[0], 
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"OQL_Report_{unit}_{month}_{year}.xlsx");*/
        }

        // API để lấy dữ liệu theo ngày cụ thể
        [HttpGet]
        public IActionResult GetDayDetails(string unit, string line, DateTime date)
        {
            var data = GetDayDetail(unit, line, date);

            if (data == null)
                return NotFound();

            return Json(new
            {
                workDate = data.Work_Date.ToString("yyyy-MM-dd"),
                unit = data.Unit,
                line = data.Line,
                checkQty = data.Check_QTY,
                faultQty = data.Fault_QTY,
                oql = data.OQL,
                oqlPercent = (data.OQL * 100).ToString("F1") + "%",
                led = data.Led,
                oqlTarget = data.OQL_Target,
                oqlTargetPercent = (data.OQL_Target * 100).ToString("F1") + "%"
            });
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
        private List<string> GetUnitbyZone(string zone)
		{
			try
			{
				var units  = _context.Set<Unit_List>()
					.Where(u => u.Factory == "REG2" && u.Zone == zone)
					.OrderBy(u => u.Unit)
                    .Select(u => u.Unit)
					.ToList();

				_logger.LogInformation($"Loaded {units .Count} units from database");
				return units ;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading unit list");
				return new List<string>();
			}
		}
        private List<string> GetZone()
        {
            try {
                var zones = new List<string>();
                string connStr = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection conn = new SqlConnection(connStr) )
                {
                    conn.Open();
                    string sql = @" SELECT DISTINCT Zone
                                        FROM Unit
                                        WHERE Act='Y' 
                                        order by  Zone ASC";
                    
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Kiểm tra NULL
                            if (!reader.IsDBNull(0))
                                zones.Add(reader.GetString(0));
                        }
                    }
                }
                return zones;
            } catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting lines for block table");
                return new List<string>();
                
            }
        }

        // Lấy dữ liệu OQL theo Unit, tháng, năm
        public List<OQL_EndLine> GetOQLData(string unit, int month, int year)
        {

            var data = new List<OQL_EndLine>();
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            List<string> unitsToQuery = new List<string>();
            if (unit.StartsWith("Z"))
            {
                var zone = unit.Substring(1); //bỏ chữ Z
                unitsToQuery = GetUnitbyZone(zone);
            }
            else {
                unitsToQuery.Add(unit);
            }

            if (!unitsToQuery.Any())
            return data;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                // Tạo IN clause cho Units
                var unitParams = string.Join(",", unitsToQuery.Select((u, i) => $"@Unit{i}"));

                // var query = @"
                //     SELECT [Work_Date], [Unit], [Line], [Check_QTY], 
                //         [Fault_QTY], [OQL], [Led], [OQL_Target]
                //     FROM [QOS].[dbo].[OQL_EndLine]
                //     WHERE [Unit] = @Unit 
                //     AND [Work_Date] >= @StartDate 
                //     AND [Work_Date] <= @EndDate
                //     ORDER BY [Line], [Work_Date]";
                var query = $@"
                SELECT [Work_Date], [Unit], [Line], [Check_QTY], 
                       [Fault_QTY], [OQL], [Led], [OQL_Target]
                FROM [QOS].[dbo].[OQL_EndLine]
                WHERE [Unit] IN ({unitParams})
                  AND [Work_Date] >= @StartDate 
                  AND [Work_Date] <= @EndDate
                ORDER BY [Unit], [Line], [Work_Date]";

                using (var command = new SqlCommand(query, connection))
                {
                    // command.Parameters.AddWithValue("@Unit", unit);
                    // Add parameters cho Units
                    for (int i = 0; i < unitsToQuery.Count; i++)
                    {
                        command.Parameters.AddWithValue($"@Unit{i}", unitsToQuery[i]);
                    }
                    command.Parameters.AddWithValue("@StartDate", startDate);
                    command.Parameters.AddWithValue("@EndDate", endDate);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            data.Add(new OQL_EndLine
                            {
                                Work_Date = reader.GetDateTime(0),
                                Unit = reader.GetString(1),
                                Line = reader.GetString(2),
                                Check_QTY = reader.GetInt32(3),
                                Fault_QTY = reader.GetInt32(4),
                                OQL = reader.GetDouble(5),
                                Led = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                OQL_Target = reader.GetFloat(7)
                            });
                        }
                    }
                }
            }

            return data;
        }
        // Lấy chi tiết một ngày cụ thể
        public OQL_EndLine? GetDayDetail(string unit, string line, DateTime date)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT TOP 1 [Work_Date], [Unit], [Line], [Check_QTY], 
                        [Fault_QTY], [OQL], [Led], [OQL_Target]
                    FROM [QOS].[dbo].[OQL_EndLine]
                    WHERE [Unit] = @Unit 
                    AND [Line] = @Line 
                    AND [Work_Date] = @Date";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Unit", unit);
                    command.Parameters.AddWithValue("@Line", line);
                    command.Parameters.AddWithValue("@Date", date.Date);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new OQL_EndLine
                            {
                                Work_Date = reader.GetDateTime(0),
                                Unit = reader.GetString(1),
                                Line = reader.GetString(2),
                                Check_QTY = reader.GetInt32(3),
                                Fault_QTY = reader.GetInt32(4),
                                OQL = reader.GetDouble(5),
                                Led = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                OQL_Target = reader.GetFloat(7)
                            };
                        }
                    }
                }
            }

            return null;
        }

        // Export dữ liệu cho Excel
        public DataTable GetDataForExport(string unit, int month, int year)
        {
            var dataTable = new DataTable();
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            List<string> unitsToQuery = new List<string>();
            if (unit.StartsWith("Z"))
            {
                var zone = unit.Substring(1); //bỏ chữ Z
                unitsToQuery = GetUnitbyZone(zone);
            }
            else {
                unitsToQuery.Add(unit);
            }

            if (!unitsToQuery.Any())
            return dataTable;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                // Tạo IN clause cho Units
                var unitParams = string.Join(",", unitsToQuery.Select((u, i) => $"@Unit{i}"));

                // var query = @"
                //     SELECT [Work_Date], [Unit], [Line], [Check_QTY], 
                //         [Fault_QTY], [OQL], [Led], [OQL_Target]
                //     FROM [QOS].[dbo].[OQL_EndLine]
                //     WHERE [Unit] = @Unit 
                //     AND [Work_Date] >= @StartDate 
                //     AND [Work_Date] <= @EndDate
                //     ORDER BY [Line], [Work_Date]";
                var query = $@"
                SELECT [Work_Date], [Unit], [Line], [Check_QTY], 
                       [Fault_QTY], [OQL], [Led], [OQL_Target]
                FROM [QOS].[dbo].[OQL_EndLine]
                WHERE [Unit] IN ({unitParams})
                  AND [Work_Date] >= @StartDate 
                  AND [Work_Date] <= @EndDate
                ORDER BY [Unit], [Line], [Work_Date]";

                using (var command = new SqlCommand(query, connection))
                using (var adapter = new SqlDataAdapter(command))
                {
                    // command.Parameters.AddWithValue("@Unit", unit);
                    // Add parameters cho Units
                    for (int i = 0; i < unitsToQuery.Count; i++)
                    {
                        command.Parameters.AddWithValue($"@Unit{i}", unitsToQuery[i]);
                    }
                    command.Parameters.AddWithValue("@StartDate", startDate);
                    command.Parameters.AddWithValue("@EndDate", endDate);

                    adapter.Fill(dataTable);
                }
            }

            return dataTable;
        }

	}
}