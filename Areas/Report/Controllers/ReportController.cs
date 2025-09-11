using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QOS.Areas.Report.Models;
using QOS.Data;
using QOS.Models;
using Dapper;
using QOS.Areas.Function.Filters;


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
        
        [Permission("B_F1")]
        public IActionResult RP_Form1()
        {
            // return View("ManageOperation/F_ManageOperation");
            return RedirectToAction("RP_Form1", "Form1BCCLC", new { area = "Report" });
        }

        [Permission("B_F2")]
        public IActionResult RP_Form2()
        {
            // return View("ManageOperation/F_ManageOperation");
            return RedirectToAction("RP_Form2", "Form2BCCPI", new { area = "Report" });
        }

        [Permission("B_F3")]
        public IActionResult RP_Form3()
        {
            // return View("ManageOperation/F_ManageOperation");
            return RedirectToAction("RP_Form3", "Form3BCDT", new { area = "Report" });
        }

        [Permission("B_F4")]
        public IActionResult RP_Form4()
        {
            // return View("ManageOperation/F_ManageOperation");
            return RedirectToAction("RP_Form4", "Form4BCCLM", new { area = "Report" });
        }
        [Permission("B_F6")]
        public IActionResult RP_Form6()
        {
            // return View("ManageOperation/F_ManageOperation");
            return RedirectToAction("RP_Form6", "Form6BCCC", new { area = "Report" });
        }
    

        public IActionResult Feature2()
        {
            return View();
        }
    }
}
