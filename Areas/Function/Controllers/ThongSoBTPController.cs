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
    public class ThongSoBTPController : Controller
    {
        private readonly ILogger<ThongSoBTPController> _logger;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public ThongSoBTPController(ILogger<ThongSoBTPController> logger, AppDbContext context, IWebHostEnvironment env, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _env = env;
            _configuration = configuration;
        }
        [TempData]
        public string? MessageStatus { get; set; } = "";

        public IActionResult Index()
        {
            return View();
        }
    }
}