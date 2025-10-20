using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QOS.Data;
using QOS.Models;
using System.Data;
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

        public Form7BTPController(ILogger<Form7BTPController> logger, IWebHostEnvironment env, IConfiguration configuration, AppDbContext context)
        {
            _logger = logger;
            _env = env;
            _configuration = configuration;
            _context = context;
        }
        public IActionResult Index()
        {
            // return View();
            return RedirectToAction("RP_Form7", "Form7BTP", new { area = "Report" });
        }
        public IActionResult RP_Form7()
        {
            return View();
        }
    }
}