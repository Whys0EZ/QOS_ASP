using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QOS.Controllers
{
    [Authorize] // chỉ khi login mới được vào
    [Area("Setting")]
    public class SettingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
