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
        [Permission("C_F1")]
        public IActionResult EditFactory(string id)
        {
            var factory = _context.Factory_List.FirstOrDefault(fac => fac.FactoryID == id);
            if (factory == null)
            {
                return NotFound();
            }
            var model = new Factory_List
            {
                FactoryID = factory.FactoryID,
                FactoryName = factory.FactoryName,
                Remark = factory.Remark
            };
            return View(model);
        }
        [HttpPost]
        public IActionResult EditFactory(Factory_List model)
        {
            if (ModelState.IsValid)
            {
                var factory = _context.Factory_List.FirstOrDefault(fac => fac.FactoryID == model.FactoryID);
                if (factory == null)
                {
                    return NotFound();
                }
                factory.FactoryName = model.FactoryName;
                factory.Remark = model.Remark;
                factory.UserUpdate = User.Identity?.Name;
                factory.LastUpdate = DateTime.Now;
                _context.SaveChanges();
                MessageStatus = _setingLocalizer["FactoryUpdateSuccess"];
                return RedirectToAction("FactoryList");
            }
            return View(model);
        }
        [HttpPost]
    
        public IActionResult DeleteFactory(string FactoryID)
        {
            // Console.WriteLine("ID DELETE: " + FactoryID);
            var factory = _context.Factory_List.FirstOrDefault(fac => fac.FactoryID == FactoryID);
            if (factory == null)
            {
                // Console.WriteLine("Factory not found for deletion.");
                MessageStatus = _setingLocalizer["FactoryDeleteFail"];
                return RedirectToAction("FactoryList");
            }
            _context.Factory_List.Remove(factory);
            _context.SaveChanges();
            MessageStatus = _setingLocalizer["FactoryDeleteSuccess"];
            return RedirectToAction("FactoryList");
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
        [Permission("C_F2")]
        public IActionResult EditDepartment(string id)
        {
            var department = _context.Team_List.FirstOrDefault(dep => dep.TeamID == id);
            if (department == null)
            {
                return NotFound();
            }
            var model = new Team_List
            {
                TeamID = department.TeamID,
                TeamName = department.TeamName,
                Remark = department.Remark
            };
            return View(model);
        }
        [HttpPost]
        public IActionResult EditDepartment(Team_List model)
        {
            if (ModelState.IsValid)
            {
                var department = _context.Team_List.FirstOrDefault(dep => dep.TeamID == model.TeamID);
                if (department == null)
                {
                    return NotFound();
                }
                department.TeamName = model.TeamName;
                department.Remark = model.Remark;
                department.UserUpdate = User.Identity?.Name;
                department.LastUpdate = DateTime.Now;
                _context.SaveChanges();
                MessageStatus = _setingLocalizer["DepartmentUpdateSuccess"];
                return RedirectToAction("DepartmentList");
            }
            return View(model);
        }
        [HttpPost]
        public IActionResult DeleteDepartment(string TeamID)
        {
            // Console.WriteLine("ID DELETE: " + FactoryID);
            var department = _context.Team_List.FirstOrDefault(dep => dep.TeamID == TeamID);
            if (department == null)
            {
                // Console.WriteLine("Factory not found for deletion.");
                MessageStatus = _setingLocalizer["DepartmentDeleteFail"];
                return RedirectToAction("DepartmentList");
            }
            _context.Team_List.Remove(department);
            _context.SaveChanges();
            MessageStatus = _setingLocalizer["DepartmentDeleteSuccess"];
            return RedirectToAction("DepartmentList");
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
        [Permission("C_F3")]
        public IActionResult EditUnit(string id)
        {
            var unit = _context.Unit_List.FirstOrDefault(u => u.Unit == id);
            if (unit == null)
            {
                return NotFound();
            }
            var model = new Unit_List
            {
                Factory = unit.Factory,
                Zone =  unit.Zone,
                Block = unit.Block,
                Unit = unit.Unit,
                Act = unit.Act,
                ETS_SUB = unit.ETS_SUB
            };
            return View(model);
        }
        [HttpPost]
        public IActionResult EditUnit(Unit_List model)
        {
            if (ModelState.IsValid)
            {
                var unit = _context.Unit_List.FirstOrDefault(u => u.Unit == model.Unit);
                if (unit == null)
                {
                    return NotFound();
                }
                unit.Factory = model.Factory;
                unit.Zone = model.Zone;
                unit.Block = model.Block;
                unit.Act = model.Act;
                unit.ETS_SUB = model.ETS_SUB;
                _context.SaveChanges();
                MessageStatus = _setingLocalizer["UnitUpdateSuccess"];
                return RedirectToAction("UnitList");
            }
            return View(model);
        }
        [HttpPost]
        public IActionResult DeleteUnit(string Unit)
        {
            // Console.WriteLine("ID DELETE: " + FactoryID);
            var unit = _context.Unit_List.FirstOrDefault(u => u.Unit == Unit);
            if (unit == null)
            {
                // Console.WriteLine("Factory not found for deletion.");
                MessageStatus = _setingLocalizer["UnitDeleteFail"];
                return RedirectToAction("UnitList");
            }
            _context.Unit_List.Remove(unit);
            _context.SaveChanges();
            MessageStatus = _setingLocalizer["UnitDeleteSuccess"];
            return RedirectToAction("UnitList");
        }
        public IActionResult AccessDenied()
        {
            return View();
        }

    }
}
