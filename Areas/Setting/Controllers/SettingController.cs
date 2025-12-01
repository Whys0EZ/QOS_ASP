using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QOS.Data;
using QOS.Models;
using QOS.Areas.Function.Filters;
using QOS.Areas.Setting;
using QOS;
using Microsoft.Extensions.Localization;

namespace QOS.Controllers
{
    [Authorize] // chỉ khi login mới được vào
    [Area("Setting")]
    public class SettingController : Controller
    {
        private readonly ILogger<SettingController> _logger;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IStringLocalizer<QOS.SharedResource> _sharedLocalizer;
        private readonly IStringLocalizer<QOS.Areas.Setting.SharedResource> _setingLocalizer;

        public SettingController(ILogger<SettingController> logger, AppDbContext context, IWebHostEnvironment env,
        IStringLocalizer<QOS.SharedResource> sharedLocalizer,
        IStringLocalizer<QOS.Areas.Setting.SharedResource> setingLocalizer)
        {
            _logger = logger;
            _context = context;
            _env = env;
            _sharedLocalizer = sharedLocalizer;
            _setingLocalizer = setingLocalizer;
        }
        [TempData]
        public string? MessageStatus { get; set; }
        public IActionResult Index()
        {
            var factorylist = _context.Factory_List.OrderBy(fac => fac.FactoryID).ToList();
            return RedirectToAction("FactoryList", factorylist);
            // return View();
        }
        // Factory List
        [Permission("C_F1")]
        public IActionResult FactoryList()
        {
            var factorylist = _context.Factory_List.OrderBy(fac => fac.FactoryID).ToList();
            return View(factorylist);
        }

        [Permission("C_F1")]
        public IActionResult CreateFactory()
        {
            return View();
        }
        [HttpPost]
        public IActionResult CreateFactory(Factory_List model)
        {
            if (ModelState.IsValid)
            {
                model.FactoryID = model.FactoryID;
                model.FactoryName = model.FactoryName;
                model.Remark = model.Remark;
                model.UserUpdate = User.Identity?.Name;
                model.LastUpdate = DateTime.Now;
                _context.Factory_List.Add(model);
                _context.SaveChanges();
                MessageStatus = _setingLocalizer["FactorySuccess"];
                return RedirectToAction("FactoryList");
            }
            return View(model);
        }

        // DepartmentList
        [Permission("C_F2")]
        public IActionResult DepartmentList()
        {
            var department = _context.Team_List.OrderBy(dep => dep.TeamID).ToList();
            return View(department);
        }
        [Permission("C_F2")]
        public IActionResult CreateDepartment()
        {
            return View();
        }
        [HttpPost]
        public IActionResult CreateDepartment(Team_List model)
        {
            if (ModelState.IsValid)
            {
                model.TeamID = model.TeamID;
                model.TeamName = model.TeamName;
                model.Remark = model.Remark;
                model.UserUpdate = User.Identity?.Name;
                model.LastUpdate = DateTime.Now;
                _context.Team_List.Add(model);
                _context.SaveChanges();
                MessageStatus = _setingLocalizer["DepartmentSuccess"];
                return RedirectToAction("DepartmentList");
            }
            return View(model);
        }

        // Unit List
        [Permission("C_F3")]
        public IActionResult UnitList()
        {
            var unitlist = _context.Unit_List.OrderBy(unit => unit.Unit).ToList();
            return View(unitlist);
        }
        [Permission("C_F3")]
        public IActionResult CreateUnit()
        {
            return View();
        }
        
        [HttpPost]
        public IActionResult CreateUnit(Unit_List model)
        {
            if (ModelState.IsValid)
            {
                model.Factory = model.Factory;
                model.Zone = model.Zone;
                model.Block = model.Block;
                model.Unit = model.Unit;
                model.Act = model.Act;
                model.Effect_Date_From = DateTime.Now;
                model.ETS_SUB = model.ETS_SUB;
                _context.Unit_List.Add(model);
                _context.SaveChanges();
                MessageStatus = _setingLocalizer["UnitSuccess"];
                return RedirectToAction("UnitList");
            }
            return View(model);
        }
        public IActionResult AccessDenied()
        {
            return View();
        }

    }
}
