using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using QOS.Areas.Function.Models;
using QOS.Data;


namespace QOS.Areas.Function.Controllers
{
    [Area("Function")]
    [Authorize]
    public class TrackingUploadController : Controller
    {
        private readonly ILogger<TrackingUploadController> _logger;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public TrackingUploadController(ILogger<TrackingUploadController> logger, AppDbContext context, IWebHostEnvironment env, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _env = env;
            _configuration = configuration;
        }
        [TempData]
        public string? MessageStatus { get; set; } = "";
        public class TrackingModule
        {
            public string? ModuleName { get; set; }
        }

        public async Task<IActionResult> Index()
        {
            var modules = new List<TrackingModule>();
            using (var conn = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
            using (var cmd = new SqlCommand("SELECT distinct ModuleName FROM TRACKING_Module ORDER BY ModuleName", conn))
            {
                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        modules.Add(new TrackingModule
                        {
                            ModuleName = reader["ModuleName"].ToString(),
                        });
                    }
                }
                ViewBag.Modules = modules;

                return View();
            }
        }
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile Upload_EXCEL, string ModuleName)
        {
            if (Upload_EXCEL == null || Upload_EXCEL.Length == 0)
            {
                TempData["Message"] = "Chưa chọn file Excel!";
                return RedirectToAction("Index");
            }

            string uploadsFolder = Path.Combine(_env.WebRootPath, "upload/TrackingUpload/EXCEL");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{User.Identity?.Name}_{Upload_EXCEL.FileName}";
            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await Upload_EXCEL.CopyToAsync(stream);
            }

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                string lastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string ses_username = User.Identity?.Name ?? "SYSTEM";

                // Lấy cấu hình cột từ procedure
                var columnConfigs = new List<ColumnConfig>();
                using (var conn = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
                using (var cmd = new SqlCommand("TRACKING_GetModuleNameInfor", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ModuleName", ModuleName);

                    await conn.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (!string.IsNullOrEmpty(reader["InforName"].ToString()))
                            {
                                columnConfigs.Add(new ColumnConfig
                                {
                                    DbColumn = reader["InforName"].ToString(),   // VD: Infor_01
                                    DisplayName = reader["Inf_Name"].ToString(), // VD: SO
                                    DataType = reader["Inf_DataType"].ToString(),
                                    Opt = reader["Inf_Opt"].ToString(),
                                    ExcelColumn = Convert.ToInt32(reader["Inf_Column"]),
                                    StartRow = Convert.ToInt32(reader["UpdateForm_StartRow"])
                                });
                            }
                        }
                    }
                }

                // ❌ XÓA DỮ LIỆU CŨ
                using (var conn = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
                using (var cmd = new SqlCommand("DELETE FROM TRACKINIG_UploadData WHERE ModuleName=@ModuleName", conn))
                {
                    cmd.Parameters.AddWithValue("@ModuleName", ModuleName);
                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }

                // ✅ ĐỌC FILE EXCEL & INSERT
                var tableHtml = new StringBuilder();
                tableHtml.Append("<table class='table-fixed'><thead><tr>");
                tableHtml.Append("<td>No</td>");
                foreach (var col in columnConfigs)
                {
                    tableHtml.Append($"<td>{col.DisplayName}</td>");
                }
                tableHtml.Append("<td>Status</td></tr></thead><tbody>");

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var sheet = package.Workbook.Worksheets[0];
                    int row = columnConfigs.First().StartRow;
                    int no = 1;

                    while (!string.IsNullOrWhiteSpace(sheet.Cells[row, 1].Text))
                    {
                        tableHtml.Append($"<tr><td>{no}</td>");

                        var insertCols = new List<string> { "ModuleName" };
                        var insertVals = new List<string> { "@ModuleName" };
                        var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@ModuleName", ModuleName)
                };

                        foreach (var col in columnConfigs)
                        {
                            var cell = sheet.Cells[row, col.ExcelColumn];
                            string? value = cell.Text?.Trim();

                            if (col.DataType == "date" && double.TryParse(cell.Value?.ToString(), out double oaDate))
                            {
                                value = DateTime.FromOADate(oaDate).ToString("yyyy-MM-dd");
                            }

                            tableHtml.Append($"<td>{value}</td>");

                            insertCols.Add(col.DbColumn!);
                            insertVals.Add("@" + col.DbColumn);
                            parameters.Add(new SqlParameter("@" + col.DbColumn, (object?)value ?? DBNull.Value));
                        }

                        insertCols.Add("UserUpdate");
                        insertCols.Add("LastUpdate");
                        insertVals.Add("@UserUpdate");
                        insertVals.Add("@LastUpdate");
                        parameters.Add(new SqlParameter("@UserUpdate", ses_username));
                        parameters.Add(new SqlParameter("@LastUpdate", lastUpdate));

                        string sqlInsert = $"INSERT INTO TRACKINIG_UploadData({string.Join(",", insertCols)}) " +
                                           $"VALUES({string.Join(",", insertVals)})";

                        using (var conn = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
                        using (var cmd = new SqlCommand(sqlInsert, conn))
                        {
                            cmd.Parameters.AddRange(parameters.ToArray());
                            await conn.OpenAsync();
                            await cmd.ExecuteNonQueryAsync();
                        }

                        tableHtml.Append("<td>PASS</td></tr>");
                        row++;
                        no++;
                    }
                }

                tableHtml.Append("</tbody></table>");
                ViewBag.ResultTable = tableHtml.ToString();
                TempData["Message"] = "Upload thành công!";
                return View("Index");
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Lỗi: " + ex.Message;
                _logger.LogError(ex, "Lỗi upload Excel");
                return RedirectToAction("Index");
            }
            finally
            {
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }
        }
        [HttpPost]
        public async Task<IActionResult> Search(string ModuleName, string Search_V)
        {
            if (string.IsNullOrEmpty(ModuleName))
            {
                return Content("<div class='alert alert-danger'>Vui lòng chọn Module</div>", "text/html");
            }

            var columnConfigs = new List<ColumnConfig>();

            try
            {
                using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    // Lấy danh sách cấu hình cột từ procedure
                    using (var cmd = new SqlCommand("TRACKING_GetModuleNameInfor", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ModuleName", ModuleName);

                        await conn.OpenAsync();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            // while (await reader.ReadAsync())
                            // {
                            //     if (!string.IsNullOrEmpty(reader["InforName"].ToString()))
                            //     {
                            //         columnConfigs.Add(new ColumnConfig
                            //         {
                            //             DbColumn = reader["InforName"].ToString(),   // Infor_01
                            //             DisplayName = reader["Inf_Name"].ToString(), // SO, Style No...
                            //             DataType = reader["Inf_DataType"].ToString(),
                            //             Opt = reader["Inf_Opt"].ToString(),
                            //             ExcelColumn = int.TryParse(reader["Inf_Column"]?.ToString(), out int col) ? col : 0,
                            //             StartRow = int.TryParse(reader["UpdateForm_StartRow"]?.ToString(), out int row) ? row : 0
                            //         });
                            //     }
                            // }
                            while (await reader.ReadAsync())
                            {
                                // Lấy dữ liệu gốc từ DB
                                string? inforName = reader["InforName"]?.ToString();
                                string? displayName = reader["Inf_Name"]?.ToString();
                                string? dataType = reader["Inf_DataType"]?.ToString();
                                string? opt = reader["Inf_Opt"]?.ToString();
                                string? colStr = reader["Inf_Column"]?.ToString();
                                string? rowStr = reader["UpdateForm_StartRow"]?.ToString();

                                // Nếu không có cột excel thì bỏ qua
                                if (string.IsNullOrWhiteSpace(inforName) || string.IsNullOrWhiteSpace(colStr))
                                    continue;

                                int excelCol = int.TryParse(colStr, out int col) ? col : 0;
                                int startRow = int.TryParse(rowStr, out int row) ? row : 0;

                                // Nếu excelCol = 0 thì cũng bỏ qua (nghĩa là không map với excel)
                                if (excelCol <= 0)
                                    continue;

                                columnConfigs.Add(new ColumnConfig
                                {
                                    DbColumn = inforName,
                                    DisplayName = displayName,
                                    DataType = dataType,
                                    Opt = opt,
                                    ExcelColumn = excelCol,
                                    StartRow = startRow
                                });
                            }
                        }
                    }

                    if (columnConfigs.Count == 0)
                    {
                        return Content("<div class='alert alert-warning'>Không có cấu hình cho module này</div>", "text/html");
                    }

                    // Build câu SQL SELECT
                    string top = !string.IsNullOrEmpty(Search_V) ? "" : "TOP 100";
                    var selectCols = string.Join(",", columnConfigs.Select(c => c.DbColumn));
                    string sql = $"SELECT {top} * FROM TRACKINIG_UploadData WHERE ModuleName = @ModuleName";

                    if (!string.IsNullOrEmpty(Search_V))
                    {
                        // Giả sử Search theo Infor_01 (SO) hoặc Infor_02 (Style No)
                        sql += " AND (Infor_01 LIKE @Search OR Infor_02 LIKE @Search) ";
                    }
                    sql += " ORDER BY LastUpdate DESC";
                    Console.WriteLine("SQL: " + sql + "| Module :" + ModuleName);
                    // Lấy dữ liệu
                    using (var cmdData = new SqlCommand(sql, conn))
                    {
                        cmdData.Parameters.AddWithValue("@ModuleName", ModuleName);
                        if (!string.IsNullOrEmpty(Search_V))
                        {
                            cmdData.Parameters.AddWithValue("@Search", "%" + Search_V + "%");
                        }

                        using (var reader = await cmdData.ExecuteReaderAsync())
                        {
                            var tableHtml = new System.Text.StringBuilder();
                            var i = 1;
                            // Header
                            tableHtml.Append("<table class='table table-bordered table-sm table-striped'>");
                            tableHtml.Append("<thead class='table-light'><tr>");
                            tableHtml.Append($"<td>No</td>");
                            foreach (var col in columnConfigs)
                            {
                                tableHtml.Append($"<th>{col.DisplayName}</th>");
                            }
                            tableHtml.Append($"<td>UserUpdate</td>");
                            tableHtml.Append($"<td>Delete</td>");
                            tableHtml.Append("</tr></thead><tbody>");

                            // Rows
                            while (await reader.ReadAsync())
                            {

                                tableHtml.Append("<tr>");
                                tableHtml.Append($"<td>{i++}</td>");
                                foreach (var col in columnConfigs)
                                {
                                    string? value = "";
                                    try
                                    {
                                        value = reader[col.DbColumn]?.ToString();
                                    }
                                    catch { value = ""; }
                                    tableHtml.Append($"<td>{value}</td>");

                                }
                                tableHtml.Append($"<td>{reader["UserUpdate"]?.ToString()} / {reader["LastUpdate"]?.ToString()}</td>");
                                tableHtml.Append($"<td><button class='btn btn-danger btn-sm' onclick='deleteRow({reader["ID"]})'>Delete</button></td>");
                                tableHtml.Append("</tr>");
                            }


                            tableHtml.Append("</tbody></table>");

                            return Content(tableHtml.ToString(), "text/html");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Content($"<div class='alert alert-danger'>Lỗi: {ex.Message}</div>", "text/html");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRow(int id)
        {
            try
            {
                using (var conn = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
                using (var cmd = new SqlCommand("DELETE FROM TRACKINIG_UploadData WHERE ID = @ID", conn))
                {
                    cmd.Parameters.AddWithValue("@ID", id);

                    await conn.OpenAsync();
                    int rows = await cmd.ExecuteNonQueryAsync();

                    return Json(new { success = rows > 0, message ="OK" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


    }
    public class ColumnConfig
    {
        public string? DisplayName { get; set; } // Tên hiển thị (Inf_Name)
        public string? DbColumn { get; set; }    // InforName (Infor_01, Infor_02...)
        public int ExcelColumn { get; set; }     // Inf_Column (cột trong Excel)
        public string? DataType { get; set; }    // Inf_DataType
        public string? Opt { get; set; }         // Inf_Opt
        public int StartRow { get; set; }        // UpdateForm_StartRow
    }
}
