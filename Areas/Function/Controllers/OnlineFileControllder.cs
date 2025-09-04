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
            var query = _context.OnlineFiles.AsQueryable();

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
                query = query.Where(f => f.LastUpdate <= dateEnd.Value);
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
        public IActionResult SaveOnlineFile()
        {
            try
            {
                var form = Request.Form;
                string? idStr = form["ID"];
                long id = 0;
                if (!string.IsNullOrEmpty(idStr))
                {
                    long.TryParse(idStr, out id);
                }

                string? groupId = form["GroupID"];
                string? dataName = form["DataName"];
                string? dataRemark = form["DataRemark"];
                string? dataLink = form["DataLink"];
                string? checker = "";
                DateTime? checkDate = null;
                string? checkResult = "";
                string? checkRemark = "";
                string? userUpdate = User.Identity?.Name;
                DateTime lastUpdate = DateTime.Now;

                OnlineFile onlineFile;
                if (id > 0)
                {
                    // Cập nhật bản ghi hiện có
                    onlineFile = _context.OnlineFiles.FirstOrDefault(f => f.ID == id);
                    if (onlineFile == null)
                    {
                        return NotFound();
                    }
                    onlineFile.GroupID = groupId;
                    onlineFile.DataName = dataName;
                    onlineFile.DataRemark = dataRemark;
                    onlineFile.DataLink = dataLink;
                    onlineFile.Checker = checker;
                    onlineFile.CheckDate = checkDate;
                    onlineFile.CheckResult = checkResult;
                    onlineFile.CheckRemark = checkRemark;
                    onlineFile.UserUpdate = userUpdate;
                    onlineFile.LastUpdate = lastUpdate;

                    _context.OnlineFiles.Update(onlineFile);
                }
                else
                {
                    // Tạo bản ghi mới
                    onlineFile = new OnlineFile
                    {
                        GroupID = groupId,
                        DataName = dataName,
                        DataRemark = dataRemark,
                        DataLink = dataLink,
                        Checker = checker,
                        CheckDate = checkDate,
                        CheckResult = checkResult,
                        CheckRemark = checkRemark,
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