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

namespace QOS.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly string _connectionString;

    public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Missing connection string: DefaultConnection");
    }

    public IActionResult Index(string? main)
        {
            // if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
            //     return RedirectToAction("Login", "Account");

            ViewData["MainMenu"] = string.IsNullOrEmpty(main) ? "" : main;

            // Console.WriteLine("IsAuthenticated: " + User.Identity?.IsAuthenticated);
            // Console.WriteLine("Claims: " + string.Join(",", User.Claims.Select(c => $"{c.Type}={c.Value}")));
            // Check detect moblie
            bool isMobile = HttpContext.Items["IsMobile"] != null &&
                            (bool)HttpContext.Items["IsMobile"];

            if (isMobile)
            {
                return RedirectToAction("Home", "Mobile");
            }else {

                return View();
            }
        }
    [HttpGet]
    public IActionResult Contact()
    {
        var model = new ContactViewModel();
        return View(model);
    }
    [HttpPost]
    public IActionResult Contact(ContactViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin!";
            return View("Index", model);
        }

        try
        {
            SaveFeedback(model);
            TempData["SuccessMessage"] = "Cảm ơn bạn đã gửi phản hồi! Chúng tôi sẽ liên hệ lại sớm.";
            
            // Reset form
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            return View("Index", model);
        }
    }
    private void SaveFeedback(ContactViewModel model)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();

            var sql = @"INSERT INTO Contact_Feedback 
                        (Username, Email, Content, CreatedDate, Status) 
                        VALUES 
                        (@Username, @Email, @Content, @CreatedDate, @Status)";

            using (var cmd = new SqlCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@Username", model.Username);
                cmd.Parameters.AddWithValue("@Email", model.Email);
                cmd.Parameters.AddWithValue("@Content", model.Content);
                cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@Status", "Pending"); // New, Pending, Resolved

                cmd.ExecuteNonQuery();
            }
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
