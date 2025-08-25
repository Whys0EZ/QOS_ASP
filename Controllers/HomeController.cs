using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QOS.Models;

namespace QOS.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index(string? main)
        {
            // if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
            //     return RedirectToAction("Login", "Account");

            ViewData["MainMenu"] = string.IsNullOrEmpty(main) ? "" : main;

            Console.WriteLine("IsAuthenticated: " + User.Identity?.IsAuthenticated);
            Console.WriteLine("Claims: " + string.Join(",", User.Claims.Select(c => $"{c.Type}={c.Value}")));

            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
