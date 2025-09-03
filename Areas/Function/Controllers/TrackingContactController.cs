using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using QOS.Areas.Function.Models;
using QOS.Data;


namespace QOS.Areas.Function.Controllers
{
    [Area("Function")]
    [Authorize]
    public class TrackingContactController : Controller
    {
        private readonly ILogger<TrackingContactController> _logger;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public TrackingContactController(ILogger<TrackingContactController> logger, AppDbContext context, IWebHostEnvironment env, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _env = env;
            _configuration = configuration;
        }
        [TempData]
        public string? MessageStatus { get; set; } = "";
        public IActionResult Index()
        {
            var contaclist = _context.GroupContactList.OrderBy(c => c.STT).ToList();
            return View(contaclist);
        }

        // public IActionResult Index()
        // {
        // var tb_string = new StringBuilder();
        // // if (string.IsNullOrEmpty(Search_V))
        // // {
        // //     tb_string.AppendLine("No Search Value!!!!!!!!");
        // //     return PartialView("_ResultTable", tb_string.ToString());
        // //     // MessageStatus = "Chưa nhâp thông tin tìm kiếm";
        // //     // return View("Index");
        // // }
        // // 1. Kết nối DB
        // string? connString = _configuration.GetConnectionString("DefaultConnection");
        // if (string.IsNullOrEmpty(connString))
        // {
        //     throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        // }
        // using (var conn = new SqlConnection(connString))

        // // using (var conn = new SqlConnection(_configuration))
        // {
        //     conn.Open();
        //     using (var cmd = new SqlCommand("SELECT * FROM TRACKING_GroupContactList WHERE GroupID LIKE @Search OR GroupName LIKE @Search ORDER BY STT", conn))
        //     {
        //         // cmd.CommandType = CommandType.StoredProcedure;
        //         // cmd.Parameters.AddWithValue("@FactoryID", "FactoryID");
        //         cmd.Parameters.AddWithValue("@Search", "%" + Search_V + "%");

        //         using (var reader = cmd.ExecuteReader())
        //         {
        //             int i = 1, tr = 1;

        //             tb_string.AppendLine("<table class='table-fixed table table-bordered table-striped' border='0' width='100%'>");
        //             tb_string.AppendLine("<thead class='table-dark'>");
        //             tb_string.AppendLine("<tr >");
        //             tb_string.AppendLine("<td style='width:30px;' >No</td>");
        //             tb_string.AppendLine("<td style='width:60px;' >ModuleName</td>");
        //             tb_string.AppendLine("<td style='width:100px;' >GroupID</td>");
        //             tb_string.AppendLine("<td style='width:100px;' >GroupName</td>");
        //             tb_string.AppendLine("<td >ContactList</td>");
        //             tb_string.AppendLine("<td >UserUpdate</td>");
        //             tb_string.AppendLine("<td >LastUpdate</td>");
        //             tb_string.AppendLine("<td >Remark</td>");
        //             tb_string.AppendLine("</tr>");
        //             tb_string.AppendLine("</thead>");
        //             while (reader.Read())
        //             {

        //                 if (tr == 4) tr = 1;

        //                 tb_string.AppendLine("<tbody>");

        //                 tb_string.AppendLine($"<tr class='tr{tr}'>");
        //                 tb_string.AppendLine($"<td style='text-align:center;'>{i}</td>");
        //                 tb_string.AppendLine($"<td>{reader["ModuleName"]}</td>");
        //                 tb_string.AppendLine($"<td>{reader["GroupID"]}</td>");
        //                 tb_string.AppendLine($"<td>{reader["GroupName"]}</td>");
        //                 tb_string.AppendLine($"<td>{reader["ContactList"]}</td>");
        //                 tb_string.AppendLine($"<td>{reader["UserUpdate"]}</td>");
        //                 tb_string.AppendLine($"<td>{reader["LastUpdate"]}</td>");
        //                 tb_string.AppendLine($"<td>{reader["Remark"]}</td>");



        //                 i++;
        //                 tr++;
        //             }

        //             tb_string.AppendLine("</tbody></table>");
        //         }
        //     }
        // }
        // return View("Index", tb_string.ToString());
        // return PartialView("_ResultTable", tb_string.ToString());
        // }



        [HttpPost]
        public IActionResult Save(GroupContactList model, string? GroupID_old)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                  .Select(e => e.ErrorMessage)
                                  .ToList();
                Console.WriteLine("Error: " + string.Join("; ", errors));
                return Json(new { success = false, message = "Invalid data" + string.Join("; ", errors)  });
            }

            if (!string.IsNullOrEmpty(GroupID_old)) // Update
                {
                    var entity = _context.GroupContactList.FirstOrDefault(x => x.GroupID == GroupID_old);
                    if (entity == null)
                        return Json(new { success = false, message = "Group not found!" });

                    // Có thể không cho sửa GroupID, chỉ update các field khác
                    entity.STT = model.STT;
                    entity.ModuleName = model.ModuleName;
                    entity.GroupName = model.GroupName;
                    entity.ContactList = model.ContactList;
                    entity.Remark = model.Remark;
                    entity.UserUpdate = User.Identity?.Name;
                    entity.LastUpdate = DateTime.Now;

                    _context.SaveChanges();
                    return Json(new { success = true, message = "Updated successfully!" });
                }
                else // Insert
                {
                    model.UserUpdate = User.Identity?.Name;
                    model.LastUpdate = DateTime.Now;

                    _context.GroupContactList.Add(model);
                    _context.SaveChanges();
                    return Json(new { success = true, message = "Inserted successfully!" });
                }
        }

        [HttpPost]
        public IActionResult Delete(string GroupID_old)
        {
            var entity = _context.GroupContactList.FirstOrDefault(x => x.GroupID == GroupID_old);
            if (entity == null) return NotFound();

            _context.GroupContactList.Remove(entity);
            _context.SaveChanges();

            return Json(new { success = true, message = $"{GroupID_old} --> Deleted Successfully!" });
        }
    }
}