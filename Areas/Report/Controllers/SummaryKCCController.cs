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
    public class SummaryKCCController : Controller
    {
        private readonly ILogger<SummaryKCCController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly AppDbContext _context;

        public SummaryKCCController(ILogger<SummaryKCCController> logger, IWebHostEnvironment env, IConfiguration configuration, AppDbContext context)
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
            return RedirectToAction("RP_Summary_Defects_KCC", "SummaryKCC");
        }
        [TempData]
        public string? MessageStatus { get; set;}
        [HttpGet]
        public IActionResult RP_Summary_Defects_KCC(string? Unit, DateTime? dateFrom, DateTime? dateEnd)
        {
            _logger.LogInformation("=== RP_Summary_Defects_KCC GET Request ===");
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

                // LoadReportData(model); // đã viết
                _logger.LogInformation($"Model created - ReportData available: {model.ReportData.Count}");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RP_Summary_Defects_KCC GET");
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
    }
}