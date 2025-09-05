using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QOS.Areas.Report.Models;
using QOS.Data;
using QOS.Models;


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

        public IActionResult RP_Form1()
        {
            var model = new RP_Form1ViewModel
            {
                Unit_List = _context.Set<Unit_List>().ToList(),
                Unit = null,
                DateFrom = DateTime.Now.AddDays(-7),
                DateEnd = DateTime.Now.Date.AddDays(1).AddTicks(-1)

            };
            
            return View(model);
        }

        public IActionResult Feature2()
        {
            return View();
        }
    }
}
