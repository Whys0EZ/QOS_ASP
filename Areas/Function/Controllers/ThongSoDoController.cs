using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using QOS.Areas.Function.Models;
using QOS.Data;
using QOS.Areas.Function.Filters;
using QOS.Helpers;
using QOS.Models;

namespace QOS.Areas.Function.Controllers
{
    [Area("Function")]
    [Authorize]
    public class ThongSoDoController : Controller
    {
        private readonly ILogger<ThongSoDoController> _logger;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public ThongSoDoController(ILogger<ThongSoDoController> logger, AppDbContext context, IWebHostEnvironment env, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _env = env;
            _configuration = configuration;
        }
        [TempData]
        public string? MessageStatus { get; set; } = "";

        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ViewData(string StyleName)
        {
            var data = await GetData(StyleName);

            ViewBag.Data = data;
            ViewBag.StyleName = StyleName;

            return View("Index");
        }
        private async Task<List<ThongSoDoViewModel>> GetData(string StyleName)
        {
            List<ThongSoDoViewModel> list = new();

            string? connString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connString))
            {
                await conn.OpenAsync();

                string sql = @"
                    SELECT *
                    FROM Form8_ThongSo_TP_ItemList_Detail
                    WHERE StyleName LIKE '%' + @StyleName + '%'
                    ORDER BY Size, STT
                ";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@StyleName", StyleName ?? "");

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new ThongSoDoViewModel
                            {
                                FactoryID = reader["FactoryID"]?.ToString(),
                                StyleName = reader["StyleName"]?.ToString(),
                                STT = reader["STT"]?.ToString(),
                                Item_Name = reader["Item_Name"]?.ToString(),
                                Size = reader["Size"]?.ToString(),
                                Target_Value = reader["Target_Value"]?.ToString(),
                                TOL = reader["TOL"]?.ToString(),
                                Ver = reader["Ver"]?.ToString()
                            });
                        }
                    }
                }
            }

            return list;
        }
        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file, string StyleName)
        {
            if (file == null || file.Length == 0)
            {
                MessageStatus = "Vui lòng chọn một tệp Excel để tải lên.";
                return RedirectToAction("Index");
            }

            // 1. Lưu file Excel tạm
            string uploadsFolder = Path.Combine(_env.WebRootPath, "upload/ThongSoDo/EXCEL");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{User.Identity?.Name}_{file.FileName}";
            string filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            bool isSuccess = false;

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension.Rows;
                    var colCount = worksheet.Dimension.Columns;
                    var Fac = "REG1";
                    var arSO = worksheet.Cells[1, 4].Text.Split(": ");
                    var chuoi = arSO[1].Trim();
                    var arSO2 = chuoi.Split("-");
                    var arrVer = worksheet.Cells[11,5].Text.Split(": ");
                    var ver = arrVer[1].Trim();
                    var Style = (worksheet.Cells[7, 4].Text + "_" + ver).Trim();
                    var SO = (arSO2[^1] + ver).Trim();

                    // 1. Kết nối DB
                    string? connString = _configuration.GetConnectionString("DefaultConnection");
                    if (string.IsNullOrEmpty(connString))
                    {
                        throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
                    }
                    using (var conn = new SqlConnection(connString))
                    {
                        await conn.OpenAsync();

                        // 3. Gọi store insert SUM
                        using (var cmd = new SqlCommand("Frm8_ThongSo_TP_INSERT_ThongSo_SUM", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@FactoryID", Fac ?? "REG1");
                            cmd.Parameters.AddWithValue("@Buyer", "ATY");
                            cmd.Parameters.AddWithValue("@StyleName", Style);
                            cmd.Parameters.AddWithValue("@TypeName", "ÁO");
                            cmd.Parameters.AddWithValue("@MO", SO);
                            cmd.Parameters.AddWithValue("@SMPL", StyleName);
                            cmd.Parameters.AddWithValue("@Remark", "Remark");
                            cmd.Parameters.AddWithValue("@UserUpdate", User.Identity?.Name ?? "system");
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    var result ="";
                    for (int row = 18; row <= 24; row++)
                    {
                        int STT =0 ;
                        
                        var Size = worksheet.Cells[row, 1].Text.Trim();
                        for (int r =6; r <= 12; r++)
                        {
                            if(r != 10)
                            {
                                STT++;
                                var Item = worksheet.Cells[r, 3].Text.Trim();
                                var Target_Value = FunctionConfig.str_replace("-", " ", worksheet.Cells[r, 5].Text.Trim());
                                if(FunctionConfig.is_numeric(Target_Value))
                                {
                                    result = FunctionConfig.FractionText(Target_Value);
                                } else
                                {
                                    result = Target_Value;
                                }
                                int Total = 0;
                                using (var conn2 = new SqlConnection(connString))
                                {
                                    await conn2.OpenAsync();
                                    using (var cmd2 = new SqlCommand(" INSERT INTO Form8_ThongSo_TP_ItemList_Detail(FactoryID, StyleName, STT, Item_Name, Size, Target_Value, TOL, Ver) VALUES (@FactoryID, @StyleName, @STT, @Item_Name, @Size, @Target_Value, @TOL, @Ver)", conn2))
                                    {
                                        cmd2.Parameters.AddWithValue("@FactoryID", Fac ?? "REG1");
                                        cmd2.Parameters.AddWithValue("@StyleName", Style);
                                        cmd2.Parameters.AddWithValue("@STT", STT);
                                        cmd2.Parameters.AddWithValue("@Item_Name", Item);
                                        cmd2.Parameters.AddWithValue("@Size", Size);
                                        cmd2.Parameters.AddWithValue("@Target_Value", result);
                                        cmd2.Parameters.AddWithValue("@TOL", Total);
                                        cmd2.Parameters.AddWithValue("@Ver", ver);
                                        int execsql = await cmd2.ExecuteNonQueryAsync();
                                        if(execsql > 0)
                                        {
                                            isSuccess = true;
                                        } else
                                        {
                                            isSuccess = false;
                                        }
                                    }
                                }
                            }
                        }
                      
                        for (int r = 6; r <= 13; r++) {
                            if (r != 7 && r != 11) { // Bỏ qua lấy dữ liệu từ dòng 10
                                if (worksheet.Cells[r, 8].Text != "") {
                                    STT = STT + 1;
                                    string Item = FunctionConfig.str_replace("-", " ", worksheet.Cells[r, 8].Text.Trim());
                                    string Target_Value = FunctionConfig.str_replace("-", " ", worksheet.Cells[r, 10].Text.Trim());
                                    if (FunctionConfig.is_numeric(Target_Value)) {

                                        result = FunctionConfig.FractionText(Target_Value);
                                    } else {
                                        // Giá trị không hợp lệ, xử lý hoặc báo lỗi tùy vào yêu cầu của bạn
                                        result = Target_Value;
                                    }
                                    int Total = 0;
                                    using (var conn3 = new SqlConnection(connString))
                                    {
                                        await conn3.OpenAsync();
                                        using (var cmd3 = new SqlCommand(" INSERT INTO Form8_ThongSo_TP_ItemList_Detail(FactoryID, StyleName, STT, Item_Name, Size, Target_Value, TOL, Ver) VALUES (@FactoryID, @StyleName, @STT, @Item_Name, @Size, @Target_Value, @TOL, @Ver)", conn3 ))
                                        {
                                            cmd3.Parameters.AddWithValue("@FactoryID", Fac ?? "REG1");
                                            cmd3.Parameters.AddWithValue("@StyleName", Style);
                                            cmd3.Parameters.AddWithValue("@STT", STT);
                                            cmd3.Parameters.AddWithValue("@Item_Name", Item);
                                            cmd3.Parameters.AddWithValue("@Size", Size);
                                            cmd3.Parameters.AddWithValue("@Target_Value", result);
                                            cmd3.Parameters.AddWithValue("@TOL", Total);
                                            cmd3.Parameters.AddWithValue("@Ver", ver);
                                            int execsql = await cmd3.ExecuteNonQueryAsync();
                                            if(execsql > 0)
                                            {
                                                isSuccess = true;
                                            } else
                                            {
                                                isSuccess = false;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        int rowNumber = 18;
                        int lastColumn = worksheet.Dimension.End.Column;
                        for (int col = 3; col <= 15; col++) {
                            STT = STT + 1;
                            string Item = FunctionConfig.str_replace("-", " ", (worksheet.Cells[16, col].Text + "" + worksheet.Cells[17, col].Text).Trim());
                            string Target_Value = FunctionConfig.str_replace("-", " ", (worksheet.Cells[rowNumber, col].Text).Trim());
                            if (FunctionConfig.is_numeric(Target_Value)) {
                                result = FunctionConfig.FractionText(Target_Value);
                            } else {
                                // Giá trị không hợp lệ, xử lý hoặc báo lỗi tùy vào yêu cầu của bạn
                                result = FunctionConfig.str_replace("-", " ", (worksheet.Cells[rowNumber, col].Text).Trim());
                            }
                            int Total = int.Parse(FunctionConfig.str_replace("'", "", (worksheet.Cells[25, col].Text).Trim()));
                            using (var conn4 = new SqlConnection(connString))
                            {   
                                await conn4.OpenAsync();
                                using (var cmd4 = new SqlCommand(" INSERT INTO Form8_ThongSo_TP_ItemList_Detail(FactoryID, StyleName, STT, Item_Name, Size, Target_Value, TOL, Ver) VALUES (@FactoryID, @StyleName, @STT, @Item_Name, @Size, @Target_Value, @TOL, @Ver)", conn4 ))
                                {
                                    cmd4.Parameters.AddWithValue("@FactoryID", Fac ?? "REG1");
                                    cmd4.Parameters.AddWithValue("@StyleName", Style);
                                    cmd4.Parameters.AddWithValue("@STT", STT);
                                    cmd4.Parameters.AddWithValue("@Item_Name", Item);
                                    cmd4.Parameters.AddWithValue("@Size", Size);
                                    cmd4.Parameters.AddWithValue("@Target_Value", result);
                                    cmd4.Parameters.AddWithValue("@TOL", Total);
                                    cmd4.Parameters.AddWithValue("@Ver", ver);
                                    int execsql = await cmd4.ExecuteNonQueryAsync();
                                    if(execsql > 0)
                                    {
                                        isSuccess = true;
                                    } else
                                    {
                                        isSuccess = false;
                                    }
                                }
                            }
                        }
                
                    }
                    string query_MO = "   Delete ETS_Data_MO_Infor where MO = '" + SO + "' and StyleCode = N'" + Style + "' EXEC Frm8_ThongSo_TP_Update_MOInfor N'" + Fac + "',N'" + SO + "',N'" + Style + "',N'" + StyleName + "'";
                    using (var conn5 = new SqlConnection(connString))
                    {
                        await conn5.OpenAsync();
                        using (var cmd5 = new SqlCommand(query_MO, conn5))
                        {
                            int execsql = await cmd5.ExecuteNonQueryAsync();
                            if(execsql > 0)
                            {
                                isSuccess = true;
                            } else
                            {
                                isSuccess = false;
                            }
                        }

                    
                    }
                }

                MessageStatus = "Tải lên và nhập dữ liệu thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi nhập dữ liệu từ tệp Excel.");
                MessageStatus = "Đã xảy ra lỗi khi nhập dữ liệu. Vui lòng kiểm tra lại tệp Excel.";
            }
            finally
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            var data = await GetData(StyleName);

            ViewBag.Data = data;
            ViewBag.StyleName = StyleName;
            ViewBag.IsSuccess = isSuccess;

            return View("Index");
        }
    }
}