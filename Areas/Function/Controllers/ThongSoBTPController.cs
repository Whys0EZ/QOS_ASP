using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using QOS.Areas.Function.Models;
using QOS.Data;
using QOS.Models;

namespace QOS.Areas.Function.Controllers
{
    [Area("Function")]
    [Authorize]
    public class ThongSoBTPController : Controller
    {
        private readonly ILogger<ThongSoBTPController> _logger;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public ThongSoBTPController(
            ILogger<ThongSoBTPController> logger,
            AppDbContext context,
            IWebHostEnvironment env,
            IConfiguration configuration
        )
        {
            _logger = logger;
            _context = context;
            _env = env;
            _configuration = configuration;
        }

        [TempData]
        public string? MessageStatus { get; set; } = "";

        public async Task<IActionResult> Index()
        {
            List<QOS.Models.Factory_List> factorys = GetFactoryList();
            ViewBag.Factorys = factorys;
            ViewBag.TypeNames = await GetTypeNameForm7List();
            return View();
        }

        private List<QOS.Models.Factory_List> GetFactoryList()
        {
            try
            {
                if (User.Identity?.Name == "admin")
                {
                    // Lấy danh sách Unit cho Factory "ALL"
                    var factorys = _context
                        .Set<QOS.Models.Factory_List>()
                        .OrderBy(u => u.FactoryID)
                        .ToList();

                    // _logger.LogInformation($"Loaded {units.Count} units from database (admin)");
                    return factorys;
                }
                else
                {
                    var factories = _context
                        .Set<QOS.Models.Factory_List>()
                        .OrderBy(u => u.FactoryID)
                        .ToList();
                    return factories;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading factory list");
                return new List<QOS.Models.Factory_List>();
            }
        }

        private async Task<List<string>> GetTypeNameForm7List()
        {
            List<string> typeNames = new List<string>();
            try
            {
                using (
                    var connection = new SqlConnection(
                        _configuration.GetConnectionString("DefaultConnection")
                    )
                )
                {
                    connection.Open();
                    var command = new SqlCommand(
                        "SELECT TypeName FROM Form7_ThongSo_BTP_ItemList Group by TypeName",
                        connection
                    );
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        typeNames.Add(reader["TypeName"] as string ?? "");
                    }

                    return typeNames;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading TypeNameForm7 list");
                return new List<string>();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetThongSoBTPList(
            string Act,
            string ID,
            string NewName,
            string factoryID,
            string typeName,
            string search_Mo
        )
        {
            List<ThongSoBTPViewModel> thongSoBTPs = new();
            try
            {
                string? connString = _configuration.GetConnectionString("DefaultConnection");
                if (Act == "Update_Item")
                {
                    // string? connString = _configuration.GetConnectionString("DefaultConnection");
                    using (SqlConnection conn = new SqlConnection(connString))
                    {
                        await conn.OpenAsync();
                        using (
                            SqlCommand cmd = new SqlCommand(
                                "UPDATE Form7_ThongSo_BTP_ItemList SET UserUpdate = @UserUpdate, LastUpdate = @LastUpdate, VN_Name = @NewName WHERE ID = @ID",
                                conn
                            )
                        )
                        {
                            cmd.CommandType = CommandType.Text;
                            cmd.Parameters.AddWithValue("@ID", ID);
                            cmd.Parameters.AddWithValue("@NewName", NewName);
                            cmd.Parameters.AddWithValue("@UserUpdate", User.Identity.Name);
                            cmd.Parameters.AddWithValue("@LastUpdate", DateTime.Now);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    MessageStatus = "Item updated successfully.";
                }
                else if (Act == "Delete_Item")
                {
                    // string? connString = _configuration.GetConnectionString("DefaultConnection");
                    using (SqlConnection conn = new SqlConnection(connString))
                    {
                        await conn.OpenAsync();
                        using (
                            SqlCommand cmd = new SqlCommand(
                                "DELETE FROM Form7_ThongSo_BTP_ItemList WHERE ID = @ID",
                                conn
                            )
                        )
                        {
                            cmd.CommandType = CommandType.Text;
                            cmd.Parameters.AddWithValue("@ID", ID);
                            var result = await cmd.ExecuteNonQueryAsync();
                            if (result == 0)
                            {
                                MessageStatus = "Item not found or already deleted.";
                                return RedirectToAction("Index");
                            }
                            else
                            {
                                // using (
                                //     SqlCommand LogCmd = new SqlCommand(
                                //         " update System_Push set DB_Ver= REPLACE(convert(varchar,getdate(),9,' ',''), ThongSo_BTP_ItemList=1 where FactoyID=@FactoryID ",
                                //         conn
                                //     )
                                // )
                                // {
                                //     LogCmd.CommandType = CommandType.Text;
                                //     LogCmd.Parameters.AddWithValue("@FactoryID", factoryID);
                                //     await LogCmd.ExecuteNonQueryAsync();
                                // }
                                MessageStatus = "Item deleted successfully.";
                            }
                        }
                    }
                }
                else if (Act == "Add_Item")
                {
                    // string? connString = _configuration.GetConnectionString("DefaultConnection");
                    using (SqlConnection conn = new SqlConnection(connString))
                    {
                        await conn.OpenAsync();
                        using (SqlCommand cmd = new SqlCommand("CreateNewItem_ThongSo_BTP", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@FactoryID", factoryID);
                            cmd.Parameters.AddWithValue("@UserName", User.Identity.Name);
                            cmd.Parameters.AddWithValue("@TypeName", typeName);
                            cmd.Parameters.AddWithValue("@MO", search_Mo);
                            cmd.Parameters.AddWithValue("@ItemName", NewName);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    MessageStatus = "Item added successfully.";
                }

                // string? connString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("RP_ThongSo_BTP_ItemList", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@FactoryID", factoryID);
                        cmd.Parameters.AddWithValue("@TypeName", typeName);
                        cmd.Parameters.AddWithValue("@MO", search_Mo);
                        var reader = await cmd.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            thongSoBTPs.Add(
                                new ThongSoBTPViewModel
                                {
                                    ID = reader["ID"]?.ToString(),
                                    STT = reader["STT"]?.ToString(),
                                    VN_Name = reader["VN_Name"]?.ToString(),
                                    MO = reader["MO"]?.ToString(),
                                    UserUpdate = reader["UserUpdate"]?.ToString(),
                                    FullName = reader["FullName"]?.ToString(),
                                    LastUpdate = reader["LastUpdate"]?.ToString(),
                                }
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ThongSoBTP list");
            }
            ViewBag.Search_Mo = search_Mo;
            ViewBag.FactoryID = factoryID;
            ViewBag.TypeName = typeName;

            ViewBag.Factorys = GetFactoryList();
            ViewBag.TypeNames = await GetTypeNameForm7List();

            return View("Index", thongSoBTPs);
        }
    }
}
