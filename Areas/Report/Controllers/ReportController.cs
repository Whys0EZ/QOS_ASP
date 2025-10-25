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
            // return View();
            return RedirectToAction("RP_Form1", "Form1BCCLC", new { area = "Report" });
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
        [Permission("B_F6")]
        public IActionResult RP_Summary_Defects_KCC()
        {
            // return View("ManageOperation/F_ManageOperation");
            return RedirectToAction("RP_Summary_Defects_KCC", "SummaryKCC", new { area = "Report" });
        }
        [Permission("B_F6")]
        public IActionResult RP_PhotoDefect()
        {
            // return View("ManageOperation/F_ManageOperation");
            return RedirectToAction("RP_PhotoDefect", "PhotoDefect", new { area = "Report" });
        }
        [Permission("B_F6")]
        public IActionResult RP_OQLEndLine()
        {
            // return View("ManageOperation/F_ManageOperation");
            return RedirectToAction("RP_OQLEndLine", "OQLEndLine", new { area = "Report" });
        }
        
        [Permission("B_F6")]
        public IActionResult RP_Summary_Defects_KCM()
        {
            // return View("ManageOperation/F_ManageOperation");
            return RedirectToAction("RP_Summary_Defects_KCM", "SummaryKCM", new { area = "Report" });
        }
        [Permission("B_F6")]
        public IActionResult RP_Form4_BCCLM_SUM()
        {
            return RedirectToAction("RP_Form4_BCCLM_SUM", "Form4BCCLMSUM", new { area = "Report" });
        }
        [Permission("B_F6")]
        public IActionResult RP_Form7()
        {
            return RedirectToAction("RP_Form7", "Form7BTP", new { area = "Report" });
        }
        [Permission("B_F6")]
        public IActionResult RP_Form8()
        {
            return RedirectToAction("RP_Form8", "Form8TP", new { area = "Report" });
        }
        [Permission("B_F6")]
        public IActionResult SummaryEndline()
        {
            return RedirectToAction("SummaryEndline", "SummaryEndline", new { area = "Report" });
        }
        [Permission("B_F6")]
        public IActionResult FCATracking()
        {
            return RedirectToAction("FCATracking", "FCATracking", new { area = "Report" });
        }
        [Permission("B_F6")]
        public IActionResult FCATrackingACDate()
        {
            return RedirectToAction("FCATrackingACDate", "FCATrackingACDate", new { area = "Report" });
        }
        [Permission("B_F6")]
        public IActionResult RP_Form10()
        {
            return RedirectToAction("RP_Form10", "Form10BCHT", new { area = "Report" });
        }
        [Permission("B_F6")]
        public IActionResult SummaryBCHT()
        {
            return RedirectToAction("SummaryBCHT", "SummaryBCHT", new { area = "Report" });
        }
        [Permission("B_F6")]
        public IActionResult SummaryChecker()
        {
            return RedirectToAction("SummaryChecker", "SummaryChecker", new { area = "Report" });
        }
        [Permission("B_F6")]
        public IActionResult RP_Form10OQL()
        {
            return RedirectToAction("RP_Form10OQL", "Form10OQL", new { area = "Report" });
        }
        [Permission("B_F6")]
        public IActionResult FQCTracking()
        {
            return RedirectToAction("FQCTracking", "FQCTracking", new { area = "Report" });
        }
        [Permission("B_F6")]
        public IActionResult SummaryFQC()
        {
            return RedirectToAction("SummaryFQC", "SummaryFQC", new { area = "Report" });
        }
        [Permission("B_F6")]
        public IActionResult Form4_Quality()
        {
            return RedirectToAction("Form4_Quality", "Form4Quality", new { area = "Report" });
        }
        [Permission("B_F6")]
        public IActionResult TopForm4Quality()
        {
            return RedirectToAction("TopForm4Quality", "TopForm4Quality", new { area = "Report" });
        }
        [Permission("B_F6")]
        public IActionResult TopForm6Quality()
        {
            return RedirectToAction("TopForm6Quality", "TopForm6Quality", new { area = "Report" });
        }
        [Permission("B_F6")]
        public IActionResult EndlineReport()
        {
            return RedirectToAction("EndlineReport", "EndlineReport", new { area = "Report" });
        }
        [Permission("B_F6")]
        public IActionResult EndlineUnit()
        {
            return RedirectToAction("EndlineUnit", "EndlineUnit", new { area = "Report" });
        }
        [Permission("B_F6")]
        public IActionResult EndlineUnitEN()
        {
            return RedirectToAction("EndlineUnitEN", "EndlineUnitEN", new { area = "Report" });
        }
        

    }
}
