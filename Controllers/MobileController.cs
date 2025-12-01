using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QOS.Areas.Report.Models;
using QOS.Data;
using QOS.Models;
using Dapper;
using OfficeOpenXml;
using System.Data;
using System.Text.Json;
using QOS.Areas.Function.Filters;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Diagnostics;

namespace QOS.Controllers
{
    [Authorize]
    public class MobileController : Controller
    {
        public IActionResult Home()
        {
            return View("~/Views/Mobile/Home.cshtml");
        }
        public IActionResult CatQuality() => View();
        public IActionResult CPI() => View();
        public IActionResult FirstCut() => View();
        public IActionResult MoveReport() => View();
        public IActionResult EndLineCheck() => View();
        public IActionResult FinalDefect() => View();
        public IActionResult ErrorImages() => View();
        public IActionResult EndLineOQL() => View();
        public IActionResult BTPParams() => View();
        public IActionResult FQC() => View();
        public IActionResult TopInline() => View();
    }
}
