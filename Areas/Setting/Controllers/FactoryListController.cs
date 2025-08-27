using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QOS.Data;

namespace QOS.Areas.SystemAdmin.Controllers
{
    [Area("Setting")]
    [Authorize]
    public class FactoryListController : Controller
    {
        private readonly ILogger<FactoryListController> _logger;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public FactoryListController(ILogger<FactoryListController> logger, AppDbContext context, IWebHostEnvironment env)
        {
            _logger = logger;
            _context = context;
            _env = env;
        }
        [TempData]
        public string? MessageStatus { get; set; } = "";

         

    }
}