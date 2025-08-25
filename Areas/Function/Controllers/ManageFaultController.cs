using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QOS.Data;
using QOS.Models;
using QOS.Services;

namespace QOS.Controllers
{
    [Authorize]
    [Area("Function")]
    public class ManageFaultController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IUserPermissionService _permissionService;

        public ManageFaultController(AppDbContext context, IUserPermissionService permissionService)
        {
            _context = context;
            _permissionService = permissionService;
        }

        // Hiển thị danh sách
        public IActionResult Index(string searchString)
        {
            // var userName = User.Identity?.Name;
            // var hasAccess = _permissionService.HasPermission(userName, "A_F6");
            // if (!hasAccess)
            // {
            //     // return Forbid(); // hoặc RedirectToAction("AccessDenied", "Account");
            //     return RedirectToAction("AccessDenied", "Home");
            // }

            var faults = from f in _context.FaultCodes
                         select f;

            if (!string.IsNullOrEmpty(searchString))
            {
                faults = faults.Where(f => f.Fault_Code.Contains(searchString)
                                        || (f.Fault_Name_VN != null && f.Fault_Name_VN.Contains(searchString)));
            }

            return View(faults.ToList());
        }

        // Thêm lỗi (GET)
        public IActionResult Create()
        {
            // return View(new FaultCode { Fault_Code = string.Empty }); // model rỗng để View không null
            // Lấy Fault_Code lớn nhất hiện có
            var lastCode = _context.FaultCodes
                            .OrderByDescending(f => f.Fault_Code)
                            .Select(f => f.Fault_Code)
                            .FirstOrDefault();

            string newCode = "A01"; // default nếu chưa có dữ liệu

            if (!string.IsNullOrEmpty(lastCode))
            {
                // Giả sử Fault_Code dạng "A09", "A10"...
                string prefix = new string(lastCode.TakeWhile(c => !char.IsDigit(c)).ToArray()); // "A"
                string numberPart = new string(lastCode.SkipWhile(c => !char.IsDigit(c)).ToArray()); // "09"

                if (int.TryParse(numberPart, out int num))
                {
                    newCode = prefix + (num + 1).ToString("D2"); // ví dụ "A10" → "A11"
                }
            }

            var model = new FaultCode
            {
                Fault_Code = newCode,
                UserUpdate = User.Identity?.Name ?? "System"
            };

            return View(model);
        }

        // Thêm lỗi (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(FaultCode model)
        {
            if (ModelState.IsValid)
            {
                model.UserUpdate = User.Identity?.Name ?? "System"; // lấy user hiện tại
                model.LastUpdate = DateTime.Now; // cập nhật thời gian hiện tại
                _context.FaultCodes.Add(model);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // Sửa (GET)
        public IActionResult Edit(string id)
        {
            var fault = _context.FaultCodes.FirstOrDefault(f => f.Fault_Code == id);
            if (fault == null) return NotFound();
            return View(fault);
        }

        // Sửa (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(FaultCode model)
        {
            if (ModelState.IsValid)
            {
                var fault = _context.FaultCodes.FirstOrDefault(f => f.Fault_Code == model.Fault_Code);
                if (fault == null) return NotFound();

                // chỉ update field cho phép
                fault.Factory = model.Factory;
                fault.Fault_Type = model.Fault_Type;
                fault.Fault_Name_VN = model.Fault_Name_VN;
                fault.Fault_Name_EN = model.Fault_Name_EN;
                fault.Form4_Active = model.Form4_Active;
                fault.Form6_Active = model.Form6_Active;

                // cập nhật người sửa và thời gian
                fault.UserUpdate = User.Identity?.Name ?? "System";
                fault.LastUpdate = DateTime.Now;

                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // // Xóa (GET)
        // public IActionResult Delete(string Fault_Code)
        // {
        //     var fault = _context.FaultCodes.FirstOrDefault(f => f.Fault_Code == Fault_Code);
        //     if (fault == null) return NotFound();
        //     return View(fault);
        // }

        // // Xóa (POST)
        // [HttpPost, ActionName("Delete")]
        // [ValidateAntiForgeryToken]
        // public IActionResult DeleteConfirmed(string Fault_Code)
        // {
        //     var fault = _context.FaultCodes.FirstOrDefault(f => f.Fault_Code == Fault_Code);
        //     if (fault == null) return NotFound();
        //     _context.FaultCodes.Remove(fault);
        //     _context.SaveChanges();
        //     return RedirectToAction(nameof(Index));
        // }
        [HttpPost]
        public IActionResult DeleteAjax(string faultCode)
        {
            var fault = _context.FaultCodes.FirstOrDefault(f => f.Fault_Code == faultCode);
            if (fault != null)
            {
                _context.FaultCodes.Remove(fault);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
