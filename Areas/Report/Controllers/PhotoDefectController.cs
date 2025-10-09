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
using OfficeOpenXml.Style;
using System.Drawing;



namespace QOS.Areas.Report.Controllers
{
    [Authorize] // chỉ khi login mới được vào
    [Area("Report")]
    public class PhotoDefectController : Controller
    {
        private readonly ILogger<PhotoDefectController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly AppDbContext _context;

        public PhotoDefectController(ILogger<PhotoDefectController> logger, IWebHostEnvironment env, IConfiguration configuration, AppDbContext context)
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
            return RedirectToAction("RP_PhotoDefect", "PhotoDefect");
        }
        [TempData]
        public string? MessageStatus { get; set;}
        [HttpGet]
        public IActionResult RP_PhotoDefect(string? report_Type, string? Unit, string? Sewer, string? Mo, DateTime? dateFrom, DateTime? dateEnd)
        {
            _logger.LogInformation($"Parameters - Unit: '{Unit}', DateFrom: {dateFrom}, DateEnd: {dateEnd}, Sewer: '{Sewer}', Mo: '{Mo}',  Report_Type: '{report_Type}'");
            var model = new RP_PhotoDefectViewModel
            {
                Unit_List = _context.Unit_List.ToList(),
                Report_Type = report_Type ?? "Form1_BCCLC",
                Unit = Unit ?? "ALL",
                Mo = Mo ?? "",
                Sewer = Sewer ?? "",
                DateFrom = dateFrom ?? DateTime.Now,
                DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1),
                ReportData = new List<Dictionary<string, object>>(),
                Unit_FQC_Unit = GetUnitList()

            };

            LoadReportData(model);
            return View(model);
        }
        private List<Unit_FQC> GetUnitList()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var units = connection.Query<Unit_FQC>(" SELECT DISTINCT Unit FROM FQC_UQ_Result_SUM_MO order by Unit ").ToList();


                _logger.LogInformation($"Loaded {units.Count} units from database");
                return units;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading unit list");
                return new List<Unit_FQC>();
            }
        }
        private void LoadReportData(RP_PhotoDefectViewModel model)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@Unit", model.Unit);
                parameters.Add("@Sewer", string.IsNullOrWhiteSpace(model.Sewer) ? "" : model.Sewer.Trim());
                parameters.Add("@Mo", string.IsNullOrWhiteSpace(model.Mo) ? "" : model.Mo.Trim());
                parameters.Add("@DateFrom", model.DateFrom);
                parameters.Add("@DateEnd", model.DateEnd);

                string storedProcedure = model.Report_Type switch
                {
                    "Form1_BCCLC" => "sp_RP_PhotoDefect_Form1_BCCLC",
                    "Form2_BCCPI" => "sp_RP_PhotoDefect_Form2_BCCPI",
                    "Form3_BCDT" => "sp_RP_PhotoDefect_Form3_BCDT",
                    "Form4_BCCLM" => "sp_RP_PhotoDefect_Form4_BCCLM",
                    "Form6_BCKCC" => "sp_RP_PhotoDefect_Form6_BCKCC",
                    "FQC_UQ_Result" => "sp_RP_PhotoDefect_FQC_UQ_Result",
                    _ => throw new ArgumentException("Invalid Report_Type")
                };

                

                _logger.LogInformation($"Executing stored procedure: {storedProcedure} with parameters: {JsonSerializer.Serialize(parameters.ParameterNames.ToDictionary(name => name, name => parameters.Get<object>(name)))}");

                var result = connection.Query(storedProcedure, parameters, commandType: CommandType.StoredProcedure);

                model.ReportData = result.Select(row => (IDictionary<string, object>)row)
                                        .Select(dict => dict.ToDictionary(kv => kv.Key, kv => kv.Value ?? ""))
                                        .ToList();

                _logger.LogInformation($"Retrieved {model.ReportData.Count} records for report type {model.Report_Type}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading report data");
                MessageStatus = $"Error loading report data: {ex.Message}";
            }
        }

    }
}