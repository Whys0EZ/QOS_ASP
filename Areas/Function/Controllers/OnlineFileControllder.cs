using System.Data;
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
    public class OnlineFileController : Controller
    {
        private readonly ILogger<OnlineFileController> _logger;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public OnlineFileController(ILogger<OnlineFileController> logger, AppDbContext context, IWebHostEnvironment env, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _env = env;
            _configuration = configuration;
        }
        [TempData]
        public string? MessageStatus { get; set; } = "";

        public IActionResult Index(string? GroupID, DateTime? dateFrom, DateTime? dateEnd, string? Search_V)
        {
            // Nếu user chưa chọn -> set mặc định
            GroupID ??= "2LA3";
            dateFrom ??= DateTime.Now.AddDays(-7);
            dateEnd ??= DateTime.Now.Date.AddDays(1).AddTicks(-1);
            var query = _context.OnlineFiles.AsQueryable();
            Console.WriteLine("GroupID: " + GroupID + " DateFrom: " + dateFrom + " DateEnd: " + dateEnd + " Search : " + Search_V);
            // Lọc theo GroupID
            if (!string.IsNullOrEmpty(GroupID))
            {
                query = query.Where(f => f.GroupID == GroupID);
            }

            // Lọc theo khoảng thời gian
            if (dateFrom.HasValue)
            {
                query = query.Where(f => f.LastUpdate >= dateFrom.Value);
            }
            if (dateEnd.HasValue)
            {
                var endDate = dateEnd.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(f => f.LastUpdate <= endDate);
            }

            // Lọc theo chuỗi tìm kiếm
            if (!string.IsNullOrEmpty(Search_V))
            {
                query = query.Where(f =>
                    (f.DataName != null && f.DataName.Contains(Search_V)) ||
                    (f.DataRemark != null && f.DataRemark.Contains(Search_V)) ||
                    (f.Checker != null && f.Checker.Contains(Search_V)) ||
                    (f.CheckRemark != null && f.CheckRemark.Contains(Search_V))
                );
            }

            var model = new OnlineFileViewModel
            {
                GroupID = GroupID,
                DateFrom = dateFrom.Value,
                DateEnd = dateEnd.Value,
                Search_V = Search_V,

                OnlineFiles = query
                                .OrderByDescending(f => f.LastUpdate)
                                .Take(50)
                                .ToList(),
                OnlineFileGroups = _context.OnlineFileGroups
                                           .OrderBy(g => g.GroupID)
                                           .ToList()
            };

            return View(model);
        }
        [HttpPost]
        public IActionResult SaveOnlineFile(SaveOnlineFileDto dto)
        {
            try
            {
                string? userUpdate = User.Identity?.Name;
                DateTime lastUpdate = DateTime.Now;

                OnlineFile onlineFile;

                if (dto.ID > 0)
                {
                    // Cập nhật
                    var existingFile = _context.OnlineFiles.FirstOrDefault(f => f.ID == dto.ID);
                    if (existingFile == null)
                    {
                        return Json(new { success = false, message = "File not found!" });
                    }
                    onlineFile = existingFile;
                    onlineFile.GroupID = dto.GroupID;
                    onlineFile.DataName = dto.DataName;
                    onlineFile.DataRemark = dto.DataRemark;
                    onlineFile.DataLink = dto.DataLink;
                    onlineFile.UserUpdate = userUpdate;
                    onlineFile.LastUpdate = lastUpdate;

                    _context.OnlineFiles.Update(onlineFile);
                }
                else
                {
                    // Thêm mới
                    onlineFile = new OnlineFile
                    {
                        GroupID = dto.GroupID,
                        DataName = dto.DataName,
                        DataRemark = dto.DataRemark,
                        DataLink = dto.DataLink,
                        UserUpdate = userUpdate,
                        LastUpdate = lastUpdate
                    };

                    _context.OnlineFiles.Add(onlineFile);
                }

                _context.SaveChanges();
                return Json(new { success = true, message = "Save successful!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        public IActionResult GetOnlineFile(string groupId, int id)
        {
            var file = _context.OnlineFiles
                               .FirstOrDefault(f => f.ID == id && f.GroupID == groupId);

            if (file == null)
            {
                return Json(null); // hoặc trả NotFound()
            }

            return Json(new
            {
                id = file.ID,
                groupID = file.GroupID,
                dataName = file.DataName,
                dataRemark = file.DataRemark,
                dataLink = file.DataLink,
                checker = file.Checker,
                checkDate = file.CheckDate?.ToString("yyyy-MM-dd HH:mm"),
                checkResult = file.CheckResult,
                checkRemark = file.CheckRemark,
                userUpdate = file.UserUpdate,
                lastUpdate = file.LastUpdate?.ToString("yyyy-MM-dd HH:mm")
            });
        }

        public IActionResult DeleteOnlineFile(int id)
        {
            try
            {
                var onlineFile = _context.OnlineFiles.FirstOrDefault(f => f.ID == id);
                if (onlineFile == null)
                    return Json(new { success = false, message = "Không tìm thấy dữ liệu." });

                _context.OnlineFiles.Remove(onlineFile);
                _context.SaveChanges();
                return Json(new { success = true, message = "Delete successful!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }


}