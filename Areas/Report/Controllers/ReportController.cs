using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QOS.Areas.Report.Models;
using QOS.Data;
using QOS.Models;
using Dapper;


namespace QOS.Controllers
{
    [Authorize] // chỉ khi login mới được vào
    [Area("Report")]
    public class ReportController : Controller
    {
        private readonly ILogger<ReportController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public ReportController(ILogger<ReportController> logger, IWebHostEnvironment env, IConfiguration configuration, AppDbContext context)
        {
            _logger = logger;
            _env = env;
            _configuration = configuration;
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult RP_Form1(string? Unit, DateTime? dateFrom, DateTime? dateEnd)
        {
            var model = new RP_Form1ViewModel
            {
                Unit_List = _context.Set<Unit_List>().Where(u => u.Factory == "REG2").OrderBy(u => u.Unit).ToList(),
                Unit = Unit,
                DateFrom = dateFrom ?? DateTime.Now.AddDays(-7),
                DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1)

            };
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string sql;

            if (!string.IsNullOrEmpty(Unit) && Unit != "ALL")
            {
                sql = @"
                SELECT t1.*, t4.FullName 
                FROM Form1_BCCLC t1
                LEFT JOIN User_List t4 ON t1.UserUpdate = t4.UserName
                WHERE t1.Unit = @Unit
                AND t1.LastUpdate BETWEEN @dateF AND @dateT
                ORDER BY t1.LastUpdate DESC";
            }
            else
            {
                sql = @"
                SELECT t1.*, t4.FullName 
                FROM Form1_BCCLC t1
                LEFT JOIN User_List t4 ON t1.UserUpdate = t4.UserName
                WHERE t1.LastUpdate BETWEEN @dateF AND @dateT
                ORDER BY t1.LastUpdate DESC";
                    };
            // Console.WriteLine("SQL: " + sql + " Unit: " + Unit);
            var history = conn.Query<Form1_BCCLC>(sql, new { Unit, dateF = model.DateFrom, dateT = model.DateEnd }).ToList();

            model.History = history; // đưa thẳng vào Model

            return View(model);
        }

        public IActionResult Feature2()
        {
            return View();
        }
    }
}
