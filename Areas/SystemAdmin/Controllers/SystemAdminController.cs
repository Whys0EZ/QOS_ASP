using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QOS.Models;
using QOS.Data;
using System.Security.Cryptography;
using System.Text;
using QOS.Services;


namespace QOS.Controllers
{
    [Authorize] // chỉ khi login mới được vào
    // [Authorize(Roles = "Admin")]
    [Area("SystemAdmin")]
    public class SystemAdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IUserPermissionService _permissionService;

        public SystemAdminController(AppDbContext context, IUserPermissionService permissionService)
        {
            _context = context;
            _permissionService = permissionService;
        }

        [TempData]
        public string? MessageStatus { get; set; }

        // Danh sách user
        public IActionResult Index()
        {
            // var hasAccess = _permissionService.HasPermission(User.Identity.Name, "Sys_Admin");
            // if (!hasAccess)
            // {
            //     return Forbid(); // hoặc RedirectToAction("AccessDenied", "Account");
            // }
            var users = _context.Users
                .OrderByDescending(u => u.LastUpdate)
                .ToList();
            return View(users);
        }

        // Thêm user
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(User model)
        {
            if (ModelState.IsValid)
            {
                model.Pass = HashPassword(model.Pass); // hash mật khẩu
                model.LastUpdate = DateTime.Now;
                model.Act = true; // mặc định active
                model.UserLevel = 9;
                model.Fac_per = "";
                model.LoginLevel = 0;
                model.UserUpdate = User.Identity?.Name;
                _context.Users.Add(model);
                _context.SaveChanges();

                // 2. Tạo quyền mặc định cho user
                var permission = new UserPermission
                {
                    FactoryID = model.FactoryID, // hoặc lấy từ form
                    UserName = model.Username,
                    SYS_Admin = false,
                    UserUpdate = User.Identity?.Name,
                    LastUpdate = DateTime.Now,

                    // Gán quyền mặc định
                    A_F1 = true,
                    A_F2 = false,
                    B_F1 = false,
                    // ...
                };

                _context.UserPermissions.Add(permission);
                _context.SaveChanges();
                MessageStatus = "User created successfully.";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // Sửa user
        public IActionResult EditUser(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();

            var per = _context.UserPermissions.FirstOrDefault(p => p.UserName == user.Username && p.FactoryID == user.FactoryID);
            if (per == null) return NotFound();

            var model = new UserEditViewModel
            {
                Id = user.Id,
                FactoryID = user.FactoryID,
                Username = user.Username,
                TeamID = user.TeamID,
                Pass = user.Pass,
                FullName = user.FullName,
                Email = user.Email,
                Act = user.Act,
                UserLevel = user.UserLevel,
                Fac_per = user.Fac_per,
                LoginLevel = user.LoginLevel,
                UserUpdate = user.UserUpdate,
                LastUpdate = user.LastUpdate,
                Unit_Check = user.Unit_Check,
                Line_Check = user.Line_Check,
                // Quyền
                SYS_Admin = per.SYS_Admin,
                A_F1 = per.A_F1,
                A_F2 = per.A_F2,
                A_F3 = per.A_F3,
                A_F4 = per.A_F4,
                A_F5 = per.A_F5,
                A_F6 = per.A_F6,
                A_F7 = per.A_F7,
                A_F8 = per.A_F8,
                A_F9 = per.A_F9,
                A_F10 = per.A_F10,

                B_F0 = per.B_F0,
                B_F01 = per.B_F01,
                B_F1 = per.B_F1,
                B_F2 = per.B_F2,
                B_F3 = per.B_F3,
                B_F4 = per.B_F4,
                B_F5 = per.B_F5,
                B_F6 = per.B_F6,
                B_F7 = per.B_F7,
                B_F8 = per.B_F8,

                C_F1 = per.C_F1,
                C_F2 = per.C_F2,
                C_F3 = per.C_F3,

                S_F1 = per.S_F1,
                S_F2 = per.S_F2,

                Q_F0 = per.Q_F0,
                Q_F1 = per.Q_F1,
                Q_F2 = per.Q_F2,
                Q_F3 = per.Q_F3,
                Q_F4 = per.Q_F4,
                Q_F5 = per.Q_F5,
                Q_F6 = per.Q_F6,
                Q_F7 = per.Q_F7,
                Q_F8 = per.Q_F8,
                Q_F9 = per.Q_F9,

                SYS_LED = per.SYS_LED


            };
            return View(model);
        }

        [HttpPost]
        public IActionResult EditUser(int id, UserEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            // Cập nhật thông tin cá nhân
            user.FactoryID = model.FactoryID;
            user.TeamID = model.TeamID;
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.Act = true; // mặc định active
            user.UserLevel = 9;
            user.Fac_per = "";
            user.LoginLevel = 0;
            user.LoginLevel = model.LoginLevel;
            user.UserUpdate = User.Identity?.Name;
            user.LastUpdate = DateTime.Now;
            user.Unit_Check = model.Unit_Check;
            user.Line_Check = model.Line_Check;

             // Update bảng User_Per
            var per = _context.UserPermissions.FirstOrDefault(p => p.UserName == model.Username && p.FactoryID == model.FactoryID);
            if (per == null) return NotFound();
            per.SYS_Admin = model.SYS_Admin;
            per.A_F1 = model.A_F1;
            per.A_F2 = model.A_F2;
            per.A_F3 = model.A_F3;
            per.A_F4 = model.A_F4;
            per.A_F5 = model.A_F5;
            per.A_F6 = model.A_F6;
            per.A_F7 = model.A_F7;
            per.A_F8 = model.A_F8;
            per.A_F9 = model.A_F9;
            per.A_F10 = model.A_F10;

            per.B_F0 = model.B_F0;
            per.B_F01 = model.B_F01;
            per.B_F1 = model.B_F1;
            per.B_F2 = model.B_F2;
            per.B_F3 = model.B_F3;
            per.B_F4 = model.B_F4;
            per.B_F5 = model.B_F5;
            per.B_F6 = model.B_F6;
            per.B_F7 = model.B_F7;
            per.B_F8 = model.B_F8;

            per.C_F1 = model.C_F1;
            per.C_F2 = model.C_F2;
            per.C_F3 = model.C_F3;

            per.S_F1 = model.S_F1;
            per.S_F2 = model.S_F2;

            per.Q_F0 = model.Q_F0;
            per.Q_F1 = model.Q_F1;
            per.Q_F2 = model.Q_F2;
            per.Q_F3 = model.Q_F3;
            per.Q_F4 = model.Q_F4;
            per.Q_F5 = model.Q_F5;
            per.Q_F6 = model.Q_F6;
            per.Q_F7 = model.Q_F7;
            per.Q_F8 = model.Q_F8;
            per.Q_F9 = model.Q_F9;

            per.SYS_LED = model.SYS_LED;

            _context.SaveChanges();

            MessageStatus = "Cập nhật thông tin người dùng thành công.";
            return RedirectToAction("Index");
        }

        // Xóa user
        public IActionResult Delete(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();
            _context.Users.Remove(user);
            _context.SaveChanges();

            var user_permission = _context.UserPermissions.Where(up => up.UserName == user.Username).ToList();
            _context.UserPermissions.RemoveRange(user_permission);
            _context.SaveChanges();

            MessageStatus = "User deleted successfully.";
            return RedirectToAction("Index");
        }

        private static string HashPassword(string password)
        {
            using var md5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(password ?? "");
            var hash = md5.ComputeHash(bytes);

            // chuyển mảng byte sang chuỗi hex (lowercase)
            var sb = new StringBuilder();
            foreach (var b in hash)
            {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
        }


        [HttpGet]
        public IActionResult ChangePassword()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var user = _context.Users.Find(int.Parse(userId));
            if (user == null)
            {
                return NotFound();
            }

            var model = new ChangePasswordViewModel
            {
                Username = User.Identity?.Name, // lấy tên đăng nhập từ Claims
                OldPassword = string.Empty,    // vì required nên phải gán
                NewPassword = string.Empty,
                ConfirmPassword = string.Empty
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = _context.Users.Find(int.Parse(userId)); // giả sử Id là int
            if (user == null) return NotFound();

            var oldPassHash = HashPassword(model.OldPassword);

            if (user.Pass != oldPassHash)
            {
                ModelState.AddModelError(nameof(model.OldPassword), "Mật khẩu cũ không đúng.");
                return View(model);
            }

            // Hash mật khẩu mới và lưu
            user.Pass = HashPassword(model.NewPassword);
            _context.SaveChanges();

            TempData["MessageStatus"] = "Đổi mật khẩu thành công.";
            return RedirectToAction("Index", "System", new { area = "System" });
        }
        

        public IActionResult AccessDenied()
        {
            return View();
        }
    }

    
}
