using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using QOS.Areas.Function.Models;
using QOS.Data;


namespace QOS.Areas.Function.Controllers
{
    [Area("Function")]
    [Authorize]
    public class ManageOperationController : Controller
    {
        private readonly ILogger<ManageOperationController> _logger;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public ManageOperationController(ILogger<ManageOperationController> logger, AppDbContext context, IWebHostEnvironment env, IConfiguration configuration)
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
        //     // return View(new List<ManageOperation>());
        //     var vm = new SearchOperationViewModel
        //     {
        //         Results = new List<ManageOperation>()
        //     };
        //     return View(vm);
        // }
        // GET: Function/ManageOperation
        [HttpGet]
        public IActionResult Index(string? mo)
        {
            var vm = new SearchOperationViewModel
            {
                MO = mo,
                Results = new List<ManageOperation>()
            };

            if (!string.IsNullOrEmpty(mo))
            {
                vm.Results = _context.ManageOperations
                    .Where(x => x.MO.Contains(mo))
                    .ToList();
            }

            return View(vm);
        }
        // POST: Function/ManageOperation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Search(string? mo)
        {
            var vm = new SearchOperationViewModel { MO = mo };
            if (string.IsNullOrEmpty(mo))
            {
                MessageStatus = "Vui lòng nhập MO để tìm kiếm.";
                return RedirectToAction("Index", new { mo });
            }

            // Ví dụ: tìm kiếm trong bảng Operations
            vm.Results = _context.ManageOperations
                .Where(x => x.MO.Contains(mo))
                .ToList();
            if (vm.Results.Count == 0)
            {
                MessageStatus = $"Không tìm thấy công đoạn nào cho MO '{mo}'.";
            }
            else
            {
                MessageStatus = $"Tìm thấy {vm.Results.Count} công đoạn cho MO '{mo}'.";
            }

            return RedirectToAction("Index", new { mo });
        }


        // Các action khác
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upload(IFormFile file)
        {
            var vm = new SearchOperationViewModel();
            if (file == null || file.Length == 0)
            {
                vm.MO = null;
                MessageStatus = "Vui lòng chọn file.";
                Console.WriteLine("file null");
                return RedirectToAction("Index", vm);
            }
            try
            {
                // _logger.LogInformation("File uploaded successfully.");
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
                Directory.CreateDirectory(uploadsFolder); // Tạo thư mục nếu chưa có

                var savedFilePath = Path.Combine(uploadsFolder, file.FileName);
                using (var stream = new FileStream(savedFilePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }
                // ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // bắt buộc với EPPlus 5+
                // ✅ EPPlus 8: set license
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var list = new List<ManageOperation>();

                using (var package = new ExcelPackage(file.OpenReadStream()))
                {
                    var worksheet = package.Workbook.Worksheets[0]; // sheet đầu tiên
                    int rowCount = worksheet.Dimension.Rows;

                    // giả sử file excel có header ở dòng 1: MO | CMD | Operation_Code | Operation_Name_VN | Operation_Name_EN
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var mo = worksheet.Cells[row, 2].Text?.Trim();
                        var opCode = worksheet.Cells[row, 3].Text?.Trim();
                        var opNameEN = worksheet.Cells[row, 4].Text?.Trim();
                        var opNameVN = worksheet.Cells[row, 5].Text?.Trim();
                        var cmd = worksheet.Cells[row, 6].Text?.Trim();

                        if (string.IsNullOrEmpty(mo) || string.IsNullOrEmpty(cmd)) continue;

                        // Xóa dữ liệu cũ nếu MO + CMD trùng
                        var olds = _context.ManageOperations
                            .Where(x => x.MO == mo && x.CMD == cmd);
                        _context.ManageOperations.RemoveRange(olds);

                        var item = new ManageOperation
                        {
                            MO = mo,
                            Operation_Code = opCode,
                            Operation_Name_VN = opNameVN,
                            Operation_Name_EN = opNameEN,
                            Form4_Active = true,
                            UserUpdate = User.Identity?.Name,
                            LastUpdate = DateTime.Now,
                            CMD = cmd
                        };

                        list.Add(item);
                        _context.ManageOperations.Add(item);
                    }
                }

                _context.SaveChanges();
                vm.Results = list;
                MessageStatus = $"Import thành công {list.Count} dòng của đơn {list.FirstOrDefault()?.MO}.";

                // trả về Index hiển thị kết quả
                // return View("Index", vm);
            }
            catch (Exception ex)
            {
                // Xử lý lỗi
                _logger.LogError(ex, "Error occurred while uploading file.");
                MessageStatus = "Đã xảy ra lỗi trong quá trình import.";
                // return RedirectToAction("Index", vm);
            }
            // _logger.LogInformation("MO" + vm.Results.FirstOrDefault()?.MO);
            return RedirectToAction("Index", new { mo = vm.Results.FirstOrDefault()?.MO });
        }

        public IActionResult Download()
        {
            // 1. Kết nối DB
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
            }
            using var conn = new SqlConnection(connString);
            using var cmd = new SqlCommand("exec Json_Get_User_Information '226317','REG'", conn); // gọi procedure
            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);

            // 2. Tạo Excel bằng EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Data");

            // Header
            for (int col = 0; col < dt.Columns.Count; col++)
            {
                ws.Cells[1, col + 1].Value = dt.Columns[col].ColumnName;
            }

            // Data
            for (int row = 0; row < dt.Rows.Count; row++)
            {
                for (int col = 0; col < dt.Columns.Count; col++)
                {
                    ws.Cells[row + 2, col + 1].Value = dt.Rows[row][col];
                }
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            // 3. Xuất file
            var stream = new MemoryStream(package.GetAsByteArray());
            string fileName = $"Report_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }


}