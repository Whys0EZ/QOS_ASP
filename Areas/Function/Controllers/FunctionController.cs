using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QOS.Services;
using QOS.Areas.Function.Filters;

namespace QOS.Controllers
{
    [Authorize] // chỉ khi login mới được vào
    [Area("Function")]
    public class FunctionController : Controller
    {
        private readonly IUserPermissionService _permissionService;

        public FunctionController(IUserPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        public IActionResult Index()
        {
            // return View();
            return RedirectToAction("Index", "OnlineFile");
        }
        // [Route("")]
        [Permission("A_F1")]
        public IActionResult F_ManageOperation()
        {
            // return View("ManageOperation/F_ManageOperation");
            return RedirectToAction("Index", "ManageOperation");
        }

        public IActionResult F_ManageFault()
        {
            // return View();
            var userName = User.Identity?.Name;
            var hasAccess = _permissionService.HasPermission(userName, "A_F2");
            if (!hasAccess)
            {
                // return Forbid(); // hoặc RedirectToAction("AccessDenied", "Account");
                return RedirectToAction("AccessDenied", "Function");
            }
            return RedirectToAction("Index", "ManageFault");
        }

        [Permission("A_F3")]
        public IActionResult F_ThongSoBTP()
        {
            return RedirectToAction("Index", "ThongSoBTP");
        }
        [Permission("A_F4")]
        public IActionResult F_ThongSoTP()
        {
            return RedirectToAction("Index", "ThongSoTP");
        }
        [Permission("A_F4")]
        public IActionResult F_ThongSoDo()
        {
            return RedirectToAction("Index", "ThongSoDo");
        }
        [Permission("A_F5")]
        public IActionResult F_MOStyleSize()
        {
            return RedirectToAction("Index", "ETSMoStyle");
        }
        
        public IActionResult F_OnlineFiles()
        {
            return RedirectToAction("Index", "OnlineFile");
        }
        [Permission("A_F6")]
        public IActionResult F_TrackingContact()
        {
            return RedirectToAction("Index", "TrackingContact");
        }
        [Permission("A_F6")]
        public IActionResult F_TrackingSetup()
        {
            return RedirectToAction("Index", "TrackingSetup");
        }
        [Permission("A_F6")]
        public IActionResult F_TrackingUpload()
        {
            return RedirectToAction("Index", "TrackingUpload");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }


    }
}