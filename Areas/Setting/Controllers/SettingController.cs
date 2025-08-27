using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QOS.Data;
using QOS.Models;

namespace QOS.Controllers
{
    [Authorize] // chỉ khi login mới được vào
    [Area("Setting")]
    public class SettingController : Controller
    {
        private readonly ILogger<SettingController> _logger;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public SettingController(ILogger<SettingController> logger, AppDbContext context, IWebHostEnvironment env)
        {
            _logger = logger;
            _context = context;
            _env = env;
        }
        [TempData]
        public string? MessageStatus { get; set; }
        public IActionResult Index()
        {
            var factorylist = _context.Factory_List.OrderBy(fac => fac.FactoryID).ToList();
            return RedirectToAction("FactoryList", factorylist);
        }
        // Factory List
        public IActionResult FactoryList()
        {
            var factorylist = _context.Factory_List.OrderBy(fac => fac.FactoryID).ToList();
            return View(factorylist);
        }
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
                MessageStatus = "Thêm nhà máy thành công";
                return RedirectToAction("FactoryList");
            }
            return View(model);
        }

        // DepartmentList
        public IActionResult DepartmentList()
        {
            var department = _context.Team_List.OrderBy(dep => dep.TeamID).ToList();
            return View(department);
        }
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
                MessageStatus = "Thêm phòng ban thành công";
                return RedirectToAction("DepartmentList");
            }
            return View(model);
        }

        // Unit List
        public IActionResult UnitList()
        {
            var unitlist = _context.Unit_List.OrderBy(unit => unit.Unit).ToList();
            return View(unitlist);
        }

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
                MessageStatus = "Thêm xưởng thành công";
                return RedirectToAction("UnitList");
            }
            return View(model);
        }
        

    }
}
