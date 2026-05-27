using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using QOS.Areas.Report.Models;
using QOS.Data;
using QOS.Models;

namespace QOS.Controllers
{
    [Authorize] // chỉ khi login mới được vào
    [Area("Report")]
    public class Form7BTPController : Controller
    {
        private readonly ILogger<Form7BTPController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly string _factoryName;
        private string factoryName =>
            User.Claims.FirstOrDefault(c => c.Type == "FactoryName")?.Value ?? "";

        public Form7BTPController(
            ILogger<Form7BTPController> logger,
            IWebHostEnvironment env,
            IConfiguration configuration,
            AppDbContext context
        )
        {
            _logger = logger;
            _env = env;
            _configuration = configuration;
            _context = context;
            _factoryName = _configuration.GetValue<string>("AppSettings:FactoryName") ?? "";
        }

        protected string FactoryName
        {
            get
            {
                // ADMIN → dùng factory mặc định (ALL)
                if (User.Identity?.Name == "admin")
                    return _factoryName;

                // USER → dùng factory từ claim
                return factoryName;
            }
        }

        [TempData]
        public string? MessageStatus { get; set; }

        public IActionResult Index()
        {
            // return View();
            return RedirectToAction("RP_Form7", "Form7BTP", new { area = "Report" });
        }

        public IActionResult RP_Form7(
            string? Unit,
            DateTime? dateFrom,
            DateTime? dateEnd,
            string? searchMo
        )
        {
            //  _logger.LogInformation($"Parameters - Unit: '{Unit}', DateFrom: {dateFrom}, DateEnd: {dateEnd}"                + $", SearchMo: '{searchMo}'");
            var model = new RP_Form7ViewModel
            {
                Unit = Unit ?? "ALL",
                DateFrom = dateFrom ?? DateTime.Now.AddDays(-7),
                DateEnd = dateEnd ?? DateTime.Now,
                SearchMo = searchMo,
                Unit_List = GetUnitList(),
            };

            // _logger.LogInformation($"Model initialized - Unit: '{model.Unit}', DateFrom: {model.DateFrom}, DateEnd: {model.DateEnd}"                + $", SearchMo: '{model.SearchMo}'");
            return View(model);
        }

        public IActionResult ExportExcel(
            string? Unit,
            DateTime dateFrom,
            DateTime dateEnd,
            string? searchMo
        )
        {
            // Lấy dữ liệu từ session hoặc database
            var reportUnits = new List<ReportUnit>(); // Thay bằng cách lấy dữ liệu thực tế

            // Tạo file Excel
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Report");

                // Thêm header
                worksheet.Cells[1, 1].Value = "Unit";
                worksheet.Cells[1, 2].Value = "Mo";
                worksheet.Cells[1, 3].Value = "Date";

                // Thêm dữ liệu
                // for (int i = 0; i < reportUnits.Count; i++)
                // {
                //     worksheet.Cells[i + 2, 1].Value = reportUnits[i].Unit;
                //     worksheet.Cells[i + 2, 2].Value = reportUnits[i].Mo;
                //     worksheet.Cells[i + 2, 3].Value = reportUnits[i].Date.ToString("yyyy-MM-dd");
                // }

                // Trả về file Excel
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                string excelName = $"Report-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(
                    stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    excelName
                );
            }
        }

        private List<QOS.Models.Unit_List> GetUnitList()
        {
            List<Unit_List> UnitList;
            try
            {
                if (User.Identity?.Name == "admin")
                {
                    // Lấy danh sách Unit cho Factory "ALL"
                    var units = _context.Set<QOS.Models.Unit_List>().OrderBy(u => u.Unit).ToList();

                    // _logger.LogInformation($"Loaded {units.Count} units from database (admin)");
                    return units;
                }
                else
                {
                    // string FactoryName = User.Claims.FirstOrDefault(c => c.Type == "FactoryName")?.Value ?? "";
                    var units = _context
                        .Set<QOS.Models.Unit_List>()
                        .Where(u => u.Factory == FactoryName)
                        .OrderBy(u => u.Unit)
                        .ToList();
                    return units;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading unit list");
                return new List<QOS.Models.Unit_List>();
            }
        }
    }
}
