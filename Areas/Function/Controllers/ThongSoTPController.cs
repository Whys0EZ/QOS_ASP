using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using QOS.Areas.Function.Models;
using QOS.Data;
using System.IO;
using System.Drawing;
using OfficeOpenXml.Drawing;

namespace QOS.Areas.Function.Controllers
{
    [Area("Function")]
    [Authorize]
    public class ThongSoTPController : Controller
    {
        private readonly ILogger<ThongSoTPController> _logger;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public ThongSoTPController(ILogger<ThongSoTPController> logger, AppDbContext context, IWebHostEnvironment env, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _env = env;
            _configuration = configuration;
        }
        [TempData]
        public string? MessageStatus { get; set; } = "";

        // public IActionResult Index()
        // {

        //     return View();
        // }
        [HttpGet,HttpPost]
        public IActionResult Index()
        {
            
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile Upload_EXCEL, string FormType, string FactoryID, string TypeName)
        {
            if (Upload_EXCEL == null || Upload_EXCEL.Length == 0)
            {
                MessageStatus = "Chưa chọn file Excel!";
                return RedirectToAction("Index");
            }
            string FactoryName = _configuration.GetValue<string>("AppSettings:FactoryName") ?? "";
            string UMSS = "NG";
            FactoryID = FactoryName;
            string Search_V = "";
            // 1. Lưu file Excel tạm
            string uploadsFolder = Path.Combine(_env.WebRootPath, "upload/ThongSoThanhPham/EXCEL");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{User.Identity?.Name}_{Upload_EXCEL.FileName}";
            string filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await Upload_EXCEL.CopyToAsync(stream);
            }
            bool success = true;

            try
            {
                // 2. Đọc Excel bằng EPPlus
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var ws = package.Workbook.Worksheets[0]; // sheet đầu tiên

                    string Buyer = ws.Cells[2, 2].Text.Trim();
                    string Remark = ws.Cells[2, 3].Text.Trim();
                    string MO = ws.Cells[3, 2].Text.Trim();
                    Search_V = MO;
                    string StyleName = ws.Cells[4, 2].Text.Trim();
                    string SMPL = ws.Cells[5, 2].Text.Trim();
                    string Unit = ws.Cells[6, 2].Text.Trim();
                    string Sample_Type = ws.Cells[2, 9].Text.Trim();
                    string Season = ws.Cells[3, 9].Text.Trim();
                    string Board = ws.Cells[4, 9].Text.Trim();
                    string Dev_Style_Name = ws.Cells[2, 19].Text.Trim();
                    string Category = ws.Cells[3, 19].Text.Trim();
                    string Development_Size_Range = ws.Cells[4, 19].Text.Trim();
                    string Fit_Intent = ws.Cells[2, 27].Text.Trim();
                    string Grade_Rule_Template = ws.Cells[3, 27].Text.Trim();
                    string Sample_color = ws.Cells[4, 27].Text.Trim();
                    string Date_Insert = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    Console.WriteLine("MO" + MO);

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
                            cmd.Parameters.AddWithValue("@FactoryID", FactoryID ?? "");
                            cmd.Parameters.AddWithValue("@Buyer", Buyer);
                            cmd.Parameters.AddWithValue("@StyleName", StyleName);
                            cmd.Parameters.AddWithValue("@TypeName", TypeName);
                            cmd.Parameters.AddWithValue("@MO", MO);
                            cmd.Parameters.AddWithValue("@SMPL", SMPL);
                            cmd.Parameters.AddWithValue("@Remark", Remark);
                            cmd.Parameters.AddWithValue("@UserUpdate", User.Identity?.Name ?? "system");
                            await cmd.ExecuteNonQueryAsync();
                        }

                        // 4. Insert Title Report
                        // string imgPath = $"/upload/ThongSoThanhPham/IMG/{StyleName}.png"; // TODO: xử lý ảnh nếu có
                        // Folder lưu ảnh
                        string outputFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "upload", "ThongSoThanhPham", "IMG");
                        if (!Directory.Exists(outputFolder))
                        {
                            Directory.CreateDirectory(outputFolder);
                        }

                        string img_name = "";

                        foreach (var drawing in ws.Drawings)
                        {
                            if (drawing is ExcelPicture pic)
                            {
                                // Lấy dữ liệu ảnh dạng byte[]
                                byte[] imageBytes = pic.Image.ImageBytes;    // ảnh dưới dạng byte[]
                                var pictureType = pic.Image.Type;           // ePictureType? (ví dụ Png, Jpeg)

                                // Mapping extension
                                string extension = pictureType switch
                                {
                                    OfficeOpenXml.Drawing.ePictureType.Jpg => ".jpg",
                                    OfficeOpenXml.Drawing.ePictureType.Png => ".png",
                                    OfficeOpenXml.Drawing.ePictureType.Gif => ".gif",
                                    _ => ".png"
                                };

                                // Ghi file
                                string filePath_Img = Path.Combine(outputFolder, $"{StyleName}{extension}");
                                // File.WriteAllBytes(filePath_Img, imageBytes);
                                System.IO.File.WriteAllBytes(filePath_Img, imageBytes);

                                img_name = $"../upload/ThongSoThanhPham/IMG/{StyleName}{extension}";
                                // Ở đây bạn có thể gọi procedure insert DB giống như PHP
                            }
                        }

                        string sqlInsert = @"INSERT INTO Form8_ThongSo_TP_Title_Report
                                            (Style_No, Customer, Sample_Type, Sample_color, Season, Board, Dev_Style_Name, Category, Development_Size_Range, Fit_Intent, Grade_Rule_Template, Img, UserUpdate, LastUpdate)
                                            VALUES (@Style_No,@Customer,@Sample_Type,@Sample_color,@Season,@Board,@Dev_Style_Name,@Category,@Development_Size_Range,@Fit_Intent,@Grade_Rule_Template,@Img,@UserUpdate,@LastUpdate)";
                        using (var cmd2 = new SqlCommand(sqlInsert, conn))
                        {
                            cmd2.Parameters.AddWithValue("@Style_No", StyleName);
                            cmd2.Parameters.AddWithValue("@Customer", Buyer);
                            cmd2.Parameters.AddWithValue("@Sample_Type", Sample_Type);
                            cmd2.Parameters.AddWithValue("@Sample_color", Sample_color);
                            cmd2.Parameters.AddWithValue("@Season", Season);
                            cmd2.Parameters.AddWithValue("@Board", Board);
                            cmd2.Parameters.AddWithValue("@Dev_Style_Name", Dev_Style_Name);
                            cmd2.Parameters.AddWithValue("@Category", Category);
                            cmd2.Parameters.AddWithValue("@Development_Size_Range", Development_Size_Range);
                            cmd2.Parameters.AddWithValue("@Fit_Intent", Fit_Intent);
                            cmd2.Parameters.AddWithValue("@Grade_Rule_Template", Grade_Rule_Template);
                            cmd2.Parameters.AddWithValue("@Img", img_name);
                            cmd2.Parameters.AddWithValue("@UserUpdate", User.Identity?.Name ?? "system");
                            cmd2.Parameters.AddWithValue("@LastUpdate", DateTime.Now);

                            int affected = await cmd2.ExecuteNonQueryAsync();
                            if (affected > 0)
                            {
                                UMSS = "OK";
                            }

                        }
                        if (!string.IsNullOrEmpty(StyleName) && UMSS == "OK")
                        {
                            // 5. Lặp đọc detail rows (từ row 8 trở đi)
                            int row = 8;
                            int STT = 1;
                            int Empty_r_Count = 0;
                            bool continueRow = true;

                            if (FormType == "NIKE" || FormType == "SPL" || FormType == "US")
                            {
                                while (continueRow)
                                {
                                    string POM = ws.Cells[row, 1].Text.Trim();
                                    string Item_Name = ws.Cells[row, 2].Text.Trim();
                                    string Criticality = ws.Cells[row, 3].Text.Trim();
                                    string TolMin = ws.Cells[row, 4].Text.Trim();
                                    string TolMax = ws.Cells[row, 5].Text.Trim();
                                    if (!string.IsNullOrEmpty(Item_Name))
                                    {
                                        Empty_r_Count = 0;
                                        bool continueCol = true;
                                        int Empty_c_Count = 0;
                                        int col = 6;
                                        int SizeNo = 0;
                                        while (continueCol)
                                        {

                                            string Size = ws.Cells[7, col].Text.Trim();
                                            if (!string.IsNullOrEmpty(Size))
                                            {
                                                SizeNo++;
                                                Empty_c_Count = 0;
                                                string Act_Value = ws.Cells[row, col].Text.Trim();
                                                if (Unit == "cm") Act_Value = Act_Value.Replace("/", "|");

                                                using (var cmd3 = new SqlCommand("Frm8_ThongSo_TP_INSERT_ThongSo_Detail_New", conn))
                                                {
                                                    cmd3.CommandType = CommandType.StoredProcedure;
                                                    cmd3.Parameters.AddWithValue("@FactoryID", FactoryID);
                                                    cmd3.Parameters.AddWithValue("@StyleName", StyleName);
                                                    cmd3.Parameters.AddWithValue("@STT", STT);
                                                    cmd3.Parameters.AddWithValue("@Item_Name", Item_Name);
                                                    cmd3.Parameters.AddWithValue("@Size", Size);
                                                    cmd3.Parameters.AddWithValue("@Act_Value", Act_Value);
                                                    cmd3.Parameters.AddWithValue("@Size_No", SizeNo);
                                                    cmd3.Parameters.AddWithValue("@POM", POM);
                                                    cmd3.Parameters.AddWithValue("@Criticality", Criticality);
                                                    await cmd3.ExecuteNonQueryAsync();
                                                }
                                            }
                                            else
                                            {
                                                Empty_c_Count++;
                                            }
                                            if (Empty_c_Count >= 6)
                                            {
                                                continueCol = false;
                                            }
                                            col++;
                                        }
                                        // Update TOL
                                        string tolSize = "Tol.";
                                        string tolValue = "0"; // mặc định

                                        string col3 = ws.Cells[row, 4].Text.Trim(); // cột D
                                        string col4 = ws.Cells[row, 5].Text.Trim(); // cột E

                                        if (!string.IsNullOrEmpty(col3))
                                        {
                                            if (col3 == col4)
                                            {
                                                tolValue = "+|-" + col3;
                                            }
                                            else
                                            {
                                                tolValue = "-" + col3 + "|" + "+" + col4;
                                            }
                                        }
                                        else
                                        {
                                            if (decimal.TryParse(col4, out decimal val4) && val4 > 0)
                                            {
                                                tolValue = "+" + col4;
                                            }
                                            else
                                            {
                                                tolValue = "0";
                                            }
                                        }

                                        // gọi procedure
                                        using (var cmd = new SqlCommand("Frm8_ThongSo_TP_INSERT_ThongSo_Detail_New", conn))
                                        {
                                            cmd.CommandType = CommandType.StoredProcedure;
                                            cmd.Parameters.AddWithValue("@FactoryID", FactoryID ?? "");
                                            cmd.Parameters.AddWithValue("@StyleName", StyleName ?? "");
                                            cmd.Parameters.AddWithValue("@STT", STT);
                                            cmd.Parameters.AddWithValue("@Item_Name", Item_Name ?? "");
                                            cmd.Parameters.AddWithValue("@Size", tolSize ?? "");
                                            cmd.Parameters.AddWithValue("@Act_Value", tolValue ?? "");
                                            cmd.Parameters.AddWithValue("@Size_No", SizeNo);
                                            cmd.Parameters.AddWithValue("@POM", POM ?? "");
                                            cmd.Parameters.AddWithValue("@Criticality", Criticality ?? "");

                                            int result = await cmd.ExecuteNonQueryAsync();
                                            UMSS = result > 0 ? "OK" : "NG";
                                        }

                                        STT++;
                                    }
                                    else
                                    {
                                        Empty_r_Count++;
                                    }
                                    if (Empty_r_Count >= 5)
                                    {
                                        continueRow = false;
                                    }
                                    row++;
                                }
                            }
                            else
                            {

                            }

                            if (FormType == "SPL" || FormType == "US")
                            {
                                using (var query_MO = new SqlCommand("Frm8_ThongSo_TP_Update_MOInfor", conn))
                                {
                                    query_MO.CommandType = CommandType.StoredProcedure;
                                    query_MO.Parameters.AddWithValue("@FactoryID", FactoryID);
                                    query_MO.Parameters.AddWithValue("@MO", MO);
                                    query_MO.Parameters.AddWithValue("@StyleName", StyleName);
                                    query_MO.Parameters.AddWithValue("@Color", SMPL);
                                    Console.WriteLine("MO" + query_MO);
                                    await query_MO.ExecuteNonQueryAsync();
                                }

                            }
                            UMSS = "OK";
                            MessageStatus = "Upload success!";
                        }
                        else
                        {
                            UMSS = "NG";
                            MessageStatus = "Upload False!";
                        }

                    }
                }
                UMSS = "OK";
                MessageStatus = "Upload success!";
                success = true;

            }
            catch (Exception ex)
            {
                MessageStatus = "Upload False! " + ex.Message;
                success = false;
            }
            finally
            {
                // Xoá file tạm
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            Console.WriteLine("Factory :" + FactoryID + " Search: " + Search_V);
            // return RedirectToAction("Search" ,new {FactoryID =FactoryID, Search_V = Search_V});
            return Json(new { success , message = MessageStatus , mo = Search_V});
        }

        public IActionResult Search(string FactoryID, string Search_V, int Page_No = 1, int Rows_Page = 20)
        {
            if (string.IsNullOrEmpty(Search_V))
            {
                return Content("<div>No search value</div>", "text/html");
            }

            var tb_string = new StringBuilder();
            // 1. Kết nối DB
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
            }
            using (var conn = new SqlConnection(connString))

            // using (var conn = new SqlConnection(_configuration))
            {
                conn.Open();
                using (var cmd = new SqlCommand("Frm8_ThongSo_TP_Search_New", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@FactoryID", FactoryID);
                    cmd.Parameters.AddWithValue("@StyleName", Search_V);

                    using (var reader = cmd.ExecuteReader())
                    {
                        int i = 1, tr = 1;
                        string[]? clmList = null;

                        while (reader.Read())
                        {
                            if (reader["STT"].ToString() == "NG")
                            {
                                tb_string.AppendLine("<table class='table-fixed table table-bordered table-striped' border='0' width='100%'>");
                                tb_string.AppendLine("<thead class='table-dark'><tr><td>Empty Data</td></tr></thead>");
                                tb_string.AppendLine("<tbody>");
                            }
                            else
                            {
                                if (tr == 4) tr = 1;

                                if (i == 1)
                                {
                                    clmList = (reader["CL"]?.ToString() ?? "")
                                        .Replace("[", "")
                                        .Replace("]", "")
                                        .Split(',');

                                    tb_string.AppendLine("<table class='table-fixed table table-bordered table-striped' border='0' width='100%'>");
                                    tb_string.AppendLine("<thead class='table-dark'><tr>");
                                    tb_string.AppendLine("<td style='width:30px;'>No</td>");
                                    tb_string.AppendLine("<td style='width:60px;'>POM</td>");
                                    tb_string.AppendLine("<td style='width:60px;'>Buyer</td>");
                                    tb_string.AppendLine("<td style='width:100px;'>StyleName</td>");
                                    tb_string.AppendLine("<td style='width:100px;'>MO</td>");
                                    tb_string.AppendLine("<td>SMPL</td>");
                                    tb_string.AppendLine("<td style='width:60px;'>Criticality</td>");
                                    tb_string.AppendLine("<td>Item Name</td>");

                                    foreach (var c in clmList)
                                        tb_string.AppendLine($"<td style='width:50px;'>{c}</td>");

                                    tb_string.AppendLine("<td style='width:50px;'>Tol.</td>");
                                    tb_string.AppendLine("<td style='width:150px;'>Updated by</td>");
                                    tb_string.AppendLine("</tr></thead><tbody>");
                                }

                                tb_string.AppendLine($"<tr class='tr{tr}'>");
                                tb_string.AppendLine($"<td style='text-align:center;'>{reader["STT"]}</td>");
                                tb_string.AppendLine($"<td>{reader["POM"]}</td>");
                                tb_string.AppendLine($"<td>{reader["Buyer"]}</td>");
                                tb_string.AppendLine($"<td>{reader["StyleName"]}</td>");
                                tb_string.AppendLine($"<td>{reader["MO"]}</td>");
                                tb_string.AppendLine($"<td>{reader["SMPL"]}</td>");
                                tb_string.AppendLine($"<td>{reader["Criticality"]}</td>");
                                tb_string.AppendLine($"<td>{reader["Item_Name"]}</td>");

                                if (clmList != null)
                                {
                                    foreach (var c in clmList)
                                        tb_string.AppendLine($"<td>{reader[c]}</td>");
                                }

                                tb_string.AppendLine($"<td>{reader["TOL"]}</td>");
                                tb_string.AppendLine($"<td>{reader["UserUpdate"]}/{Convert.ToDateTime(reader["LastUpdate"]).ToString("yyyy-MM-dd hh:mm tt")}</td>");
                                tb_string.AppendLine("</tr>");

                                i++;
                                tr++;
                            }
                        }
                    }
                }
            }

            tb_string.AppendLine("</tbody></table>");
            // return View("Index", tb_string.ToString());
            return PartialView("_ResultTable", tb_string.ToString());
        }


    }
}