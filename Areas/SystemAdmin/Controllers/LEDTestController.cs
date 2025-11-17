using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QOS.Models;
using QOS.Data;
using System.Security.Cryptography;
using System.Text;
using QOS.Services;
using QOS.Areas.Function.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Dapper;
using Microsoft.Data.SqlClient;

namespace QOS.Controllers
{
    [Area("SystemAdmin")]
    [Authorize]
    [Route("/LEDTest")] // ← override lại route gốc
    public class LEDTestController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<LEDTestController> _logger;

        public LEDTestController(IConfiguration configuration, ILogger<LEDTestController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Missing connection string: DefaultConnection");
            _logger = logger;
        }
        // [Permission("B_F1")]
        [HttpGet]
        public IActionResult Index(string unit = "", string com = "")
        {
            // Check user permission
            var username = User.Identity.Name;
            if (!CheckUserPermission(username))
            {
                return RedirectToAction("Index", "Home", new { main = "Index" });
            }

            var model = new LEDTestViewModel
            {
                Unit = unit,
                COM = com,
                Arduino_Port = 1,
                ON_OFF = "OFF"
            };

            // Load Units
            model.Units = GetUnits();

            // Load COMs if unit selected
            if (!string.IsNullOrEmpty(unit))
            {
                model.COMs = GetCOMs(unit);
            }

            // Load Line-Port mapping if unit and com selected
            if (!string.IsNullOrEmpty(unit) && !string.IsNullOrEmpty(com))
            {
                model.LinePortMapping = GetLinePortMapping(unit, com);
                model.Lines = GetLines(unit);
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Index(LEDTestViewModel model)
        {
            // Check user permission
            var username = User.Identity.Name;
            if (!CheckUserPermission(username))
            {
                return RedirectToAction("Index", "Home");
            }

            string message = "";
            string opacity_green = "";
            string opacity_red = "";
            string opacity_off = "";
            string opacity_update = "";

            try
            {
                switch (model.Act?.ToLower())
                {
                    case "green":
                        opacity_green = "opacity: 1";
                        opacity_red = "opacity: 0.1";
                        opacity_off = "opacity: 0.1";
                        opacity_update = "opacity: 0.1";
                        ExecuteLEDCommand(model.Unit, model.COM, model.Arduino_Port, "G");
                        message = $"Already turn {model.COM} / Port-{model.Arduino_Port} to GREEN!";
                        model.SetLineDisplay = "";
                        break;

                    case "red":
                        opacity_green = "opacity: 0.1";
                        opacity_red = "opacity: 1";
                        opacity_off = "opacity: 0.1";
                        opacity_update = "opacity: 0.1";
                        ExecuteLEDCommand(model.Unit, model.COM, model.Arduino_Port, "R");
                        message = $"Already turn {model.COM} / Port-{model.Arduino_Port} to RED!";
                        model.SetLineDisplay = "";
                        break;

                    case "off":
                        opacity_green = "opacity: 0.1";
                        opacity_red = "opacity: 0.1";
                        opacity_off = "opacity: 1";
                        opacity_update = "opacity: 0.1";
                        ExecuteLEDCommand(model.Unit, model.COM, model.Arduino_Port, "OFF");
                        message = $"Already turn OFF {model.COM} / Port-{model.Arduino_Port}";
                        model.ON_OFF = "ON";
                        model.SetLineDisplay = "";
                        break;

                    case "on":
                        opacity_green = "opacity: 0.1";
                        opacity_red = "opacity: 0.1";
                        opacity_off = "opacity: 1";
                        opacity_update = "opacity: 0.1";
                        ExecuteLEDCommand(model.Unit, model.COM, model.Arduino_Port, "ON");
                        message = $"Already turn ON {model.COM} / Port-{model.Arduino_Port}";
                        model.ON_OFF = "OFF";
                        model.SetLineDisplay = "";
                        break;

                    case "update":
                        opacity_green = "opacity: 0.1";
                        opacity_red = "opacity: 0.1";
                        opacity_off = "opacity: 0.1";
                        opacity_update = "opacity: 1";
                        ExecuteLEDCommand(model.Unit, model.COM, model.Arduino_Port, "U");
                        message = $"Already send update to {model.COM} / Arduino port {model.Arduino_Port}";
                        model.SetLineDisplay = "";
                        break;

                    case "set_line_port":
                        if (!string.IsNullOrEmpty(model.Line))
                        {
                            opacity_green = "opacity: 1";
                            opacity_red = "opacity: 1";
                            opacity_off = "opacity: 1";
                            opacity_update = "opacity: 1";
                            SetLinePort(model.Unit, model.Line, model.Arduino_Port, model.COM);
                            message = $"Updated {model.Unit} / {model.Line} ---> Ard-port: {model.Arduino_Port}";
                            model.Line = "";
                        }
                        break;

                    case "reload":
                        // Just reload the page with current selections
                        break;
                }

                model.Message = message;
                model.OpacityGreen = opacity_green;
                model.OpacityRed = opacity_red;
                model.OpacityOff = opacity_off;
                model.OpacityUpdate = opacity_update;
            }
            catch (Exception ex)
            {
                model.Message = "Error: " + ex.Message;
            }

            // Reload dropdown data
            model.Units = GetUnits();
            if (!string.IsNullOrEmpty(model.Unit))
            {
                model.COMs = GetCOMs(model.Unit);
            }
            if (!string.IsNullOrEmpty(model.Unit) && !string.IsNullOrEmpty(model.COM))
            {
                model.LinePortMapping = GetLinePortMapping(model.Unit, model.COM);
                model.Lines = GetLines(model.Unit);
            }

            return View(model);
        }

        private bool CheckUserPermission(string username)
        {
            // _logger.LogInformation($"Checking user permission for {username}");

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"SELECT SYS_LED FROM User_Per A 
                    LEFT JOIN User_List B ON A.UserName=B.UserName AND A.FactoryID=B.FactoryID 
                    WHERE A.UserName = @Username", conn);
                cmd.Parameters.AddWithValue("@Username", username);
                
                var result = cmd.ExecuteScalar();
                // _logger.LogInformation($"User permission for {username}: {result}");
                return result != null && result.ToString() == "True";
            }
        }

        private List<SelectListItem> GetUnits()
        {
            var units = new List<SelectListItem> { new SelectListItem { Value = "", Text = "--" } };
            
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT Unit FROM COM_Led GROUP BY Unit", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        units.Add(new SelectListItem
                        {
                            Value = reader["Unit"].ToString(),
                            Text = reader["Unit"].ToString()
                        });
                    }
                }
            }
            
            return units;
        }

        private List<SelectListItem> GetCOMs(string unit)
        {
            var coms = new List<SelectListItem> { new SelectListItem { Value = "", Text = "--" } };
            
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT COM, Line FROM COM_Led WHERE Unit = @Unit", conn);
                cmd.Parameters.AddWithValue("@Unit", unit);
                
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        coms.Add(new SelectListItem
                        {
                            Value = reader["COM"].ToString(),
                            Text = reader["COM"].ToString()
                        });
                    }
                }
            }
            
            return coms;
        }

        private string GetLinePortMapping(string unit, string com)
        {
            var result = "";
            
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("EXEC Get_Line_ArduinoPort_From_COM_Unit @Unit, @COM", conn);
                cmd.Parameters.AddWithValue("@Unit", unit);
                cmd.Parameters.AddWithValue("@COM", com);
                
                using (var reader = cmd.ExecuteReader())
                {
                    var lines = "<tr class='tr1'>";
                    var ports = "<tr class='tr2'>";
                    int count = 0;
                    
                    while (reader.Read())
                    {
                        if (count == 0)
                        {
                            result = "<table border='0' width='100%'>";
                        }
                        
                        lines += $"<td>{reader["Line"]}</td>";
                        ports += $"<td>{reader["Arduino_port"]}</td>";
                        count++;
                    }
                    
                    if (count > 0)
                    {
                        lines += "</tr>";
                        ports += "</tr>";
                        result += lines + ports + "</table>";
                    }
                }
            }
            
            return result;
        }

        private List<SelectListItem> GetLines(string unit)
        {
            var lines = new List<SelectListItem> { new SelectListItem { Value = "", Text = "--" } };
            
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT Line FROM Line WHERE Unit = @Unit", conn);
                cmd.Parameters.AddWithValue("@Unit", unit);
                
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lines.Add(new SelectListItem
                        {
                            Value = reader["Line"].ToString(),
                            Text = reader["Line"].ToString()
                        });
                    }
                }
            }
            
            return lines;
        }

        private void ExecuteLEDCommand(string unit, string com, int port, string command)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("EXEC Insert_LED_TEST @Unit, @COM, @Port, @Param1, @Param2, @Command", conn);
                cmd.Parameters.AddWithValue("@Unit", unit);
                cmd.Parameters.AddWithValue("@COM", com);
                cmd.Parameters.AddWithValue("@Port", port);
                cmd.Parameters.AddWithValue("@Param1", 1);
                cmd.Parameters.AddWithValue("@Param2", 3000);
                cmd.Parameters.AddWithValue("@Command", command);
                cmd.ExecuteNonQuery();
            }
        }

        private void SetLinePort(string unit, string line, int port, string com)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("UPDATE Line SET Arduino_port = @Port WHERE Unit = @Unit AND Line = @Line", conn);
                cmd.Parameters.AddWithValue("@Unit", unit);
                cmd.Parameters.AddWithValue("@Line", line);
                cmd.Parameters.AddWithValue("@Port", port);
                cmd.ExecuteNonQuery();

                // Update COM_Led
                var cmd2 = new SqlCommand("EXEC Update_COM_Led @COM, @Line", conn);
                cmd2.Parameters.AddWithValue("@COM", com);
                cmd2.Parameters.AddWithValue("@Line", line);
                cmd2.ExecuteNonQuery();
            }
        }
    }

    // ViewModel
    public class LEDTestViewModel
    {
        public string? Act { get; set; }
        public string? Unit { get; set; }
        public string? COM { get; set; }
        public string? Line { get; set; }
        public int Arduino_Port { get; set; }
        public string? ON_OFF { get; set; }
        public string? Message { get; set; }
        public string? LinePortMapping { get; set; }
        public string? SetLineDisplay { get; set; } = "display:none;";
        
        public string? OpacityGreen { get; set; } = "";
        public string? OpacityRed { get; set; } = "";
        public string? OpacityOff { get; set; } = "";
        public string? OpacityUpdate { get; set; } = "";
        
        public List<SelectListItem> Units { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> COMs { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Lines { get; set; } = new List<SelectListItem>();
    }
}