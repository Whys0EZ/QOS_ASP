using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using QOS.Data;
using QOS.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace QOS.Controllers
{
    public class BackupAccountController : Controller
    {
        private readonly AppDbContext _context;
        public BackupAccountController(AppDbContext context) => _context = context;

        // private static string HashPassword(string password)
        // {
        //     using var md5 = MD5.Create();
        //     var bytes = Encoding.UTF8.GetBytes(password ?? "");
        //     var hash = md5.ComputeHash(bytes);
        //     return Convert.ToBase64String(hash);
        // }
        [TempData]
        public string? MessageStatus { get; set; }
        private static string HashPassword(string password)
        {
            using var md5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(password ?? "");
            var hash = md5.ComputeHash(bytes);

            // chuyển mảng byte sang chuỗi hex (lowercase)
            var sb = new StringBuilder();
            foreach (var b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
       
        [HttpGet]
        public IActionResult Login() => View(new LoginViewModel());

        [HttpPost]
        public async Task<IActionResult> LoginAsync(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ Username và Password!";
                return View(model);
            }
            // // Sử dụng Dbset truy cập thẳng table
            // var user = _context.Users.FirstOrDefault(u => u.Username == model.Username);
            // if (user == null)
            // {
            //     ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu!";
            //     return View(model);
            // }

            var hash = HashPassword(model.Password);

            // if (!string.Equals(user.PasswordHash, hash, StringComparison.Ordinal))
            // {
            //     ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu!";
            //     return View(model);
            // }
            // ✅ Gọi thủ tục trong SQL
            var user = _context.Users
                .FromSqlRaw("EXEC Exec_Login_by_UserName @p0, @p1", model.Username, hash)
                .AsEnumerable()  // chạy query
                .FirstOrDefault();

            if (user == null)
            {
                ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu!";
                return View(model);
            }
            // ✅ Tạo Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                // new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // ✅ Đăng nhập với cookie
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                claimsPrincipal,
                new AuthenticationProperties
                {
                    IsPersistent = true,                    // Cookie tồn tại sau khi đóng browser
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1) // Thời gian hết hạn (1 ngày)
                });

            HttpContext.Session.SetString("Username", user.Username);
            // HttpContext.Session.SetString("Role", user.Role ?? "User");
            Console.WriteLine("Claims count: " + claims.Count);

            return RedirectToAction("Index", "Home", new { main = "Index" });
        }

        [HttpGet]
        // public IActionResult Register() => View(new UserEditViewModel());
        public IActionResult Register()
        {
            var model = new UserEditViewModel();
            // Lấy danh sách Factory
            model.FactoryOptions = _context.Factory_List
                .Select(f => new SelectListItem
                {
                    Value = f.FactoryID,
                    Text = f.FactoryID
                })
                .ToList();

            // Lấy danh sách Team từ DB
            model.TeamOptions = _context.Team_List
                .Select(t => new SelectListItem
                {
                    Value = t.TeamID,
                    Text = t.TeamName
                })
                .ToList();

            return View(model);
        }

        [HttpPost]
        public IActionResult Register(UserEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // load lại dropdown nếu lỗi validate
                model.FactoryOptions = _context.Factory_List
                .Select(f => new SelectListItem
                {
                    Value = f.FactoryID,
                    Text = f.FactoryName
                })
                .ToList();
                model.TeamOptions = _context.Team_List
                    .Select(t => new SelectListItem
                    {
                        Value = t.TeamID,
                        Text = t.TeamName
                    })
                    .ToList();

                return View(model);

            }

            // Kiểm tra trùng username
            if (_context.Users.Any(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Username đã tồn tại");
                return View(model);
            }
            var passwordHash = HashPassword(model.Pass);
            // Insert vào Users
            var user = new User
            {
                FactoryID = model.FactoryID,
                TeamID = model.TeamID,
                Username = model.Username,
                Pass = passwordHash,   // 🚨 nên hash mật khẩu trước khi lưu
                FullName = model.FullName,
                Email = model.Email,
                Act = true,
                UserLevel = 0,
                Fac_per = "",
                LoginLevel = 9,
                UserUpdate = model.UserUpdate ?? model.Username,
                LastUpdate = DateTime.Now,
                Unit_Check = "@ALL",
                Line_Check = "@ALL"
            };

            _context.Users.Add(user);
            _context.SaveChanges(); // cần Save để lấy user.Id

            // Insert vào User_Per (map quyền từ model)
            var per = new UserPermission
            {
                // UserId = user.Id,
                FactoryID = model.FactoryID,
                UserName = model.Username,
                SYS_Admin = false,
                UserUpdate = model.UserUpdate ?? model.Username,
                LastUpdate = DateTime.Now,
                // A_F1 = model.A_F1,
                // A_F2 = model.A_F2,
                // A_F3 = model.A_F3,
                // A_F4 = model.A_F4,
                // A_F5 = model.A_F5,
                // A_F6 = model.A_F6,
                // A_F7 = model.A_F7,
                // A_F8 = model.A_F8,
                // A_F9 = model.A_F9,
                // A_F10 = model.A_F10,
                B_F1 = true,
                B_F2 = true,
                B_F3 = true,
                B_F4 = true,
                B_F5 = true,
                B_F6 = true,
                B_F7 = true,
                B_F8 = true,
                // C_F1 = true,
                // C_F2 = true,
                // C_F3 = true,
                // S_F1 = model.S_F1,
                // S_F2 = model.S_F2,
                // Q_F0 = model.Q_F0,
                // Q_F1 = model.Q_F1,
                // Q_F2 = model.Q_F2,
                // Q_F3 = model.Q_F3,
                // Q_F4 = model.Q_F4,
                // Q_F5 = model.Q_F5,
                // Q_F6 = model.Q_F6,
                // Q_F7 = model.Q_F7,
                // Q_F8 = model.Q_F8,
                // Q_F9 = model.Q_F9,
                // SYS_LED = model.SYS_LED

            };

            _context.UserPermissions.Add(per);
            _context.SaveChanges();
            MessageStatus = "Đăng ký tài khoản thành công. Vui lòng đăng nhập.";

            return RedirectToAction("Login");
        }

        public async Task<IActionResult> LogoutAsync()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
        public IActionResult AccessDenied()
        {
            return View();
        }
        public IActionResult Settings()
        {
            return RedirectToAction("Index", "Home", new { main = "Index" });
        }
        public IActionResult Profile()
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == User.Identity.Name);
            return View(user);
        }

        [HttpGet]
        public IActionResult EditProfile()
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == User.Identity.Name);
            if (user == null)
            {
            return RedirectToAction("Login");
            }

             // Đường dẫn thư mục avatar
            var avatarFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars");
            var avatarFile = Path.Combine(avatarFolder, $"{User.Identity?.Name}.png");

            // Nếu chưa có ảnh thì dùng default-avatar
            var avatarPath = System.IO.File.Exists(avatarFile)
                ? $"/images/avatars/{User.Identity?.Name}.png"
                : "/images/avatars/default-avatar.png";

            var model = new EditProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone_Num,
                Team = user.TeamID,
                AvatarPath = avatarPath
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model, IFormFile? AvatarFile)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ!";
                return View(model);
            }

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity!.Name);
                if (user == null)
                {
                    TempData["Error"] = "Không tìm thấy người dùng!";
                    return View(model);
                }

                // Update thông tin cơ bản
                user.FullName  = model.FullName;
                user.Email     = model.Email;
                user.Phone_Num = model.Phone;
                user.TeamID    = model.Team;

                // Nếu có upload ảnh thì xử lý
                if (AvatarFile != null && AvatarFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var ext = Path.GetExtension(AvatarFile.FileName).ToLower();

                    if (!allowedExtensions.Contains(ext))
                    {
                        TempData["Error"] = "Chỉ hỗ trợ file ảnh (.jpg, .jpeg, .png, .gif)";
                        return View(model);
                    }
                    if (AvatarFile.Length > 2 * 1024 * 1024)
                    {
                        TempData["Error"] = "Ảnh quá lớn (tối đa 2MB)";
                        return View(model);
                    }

                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var fileName = $"{User.Identity?.Name}.png";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    // Xóa file cũ (nếu có)
                    var oldFiles = Directory.GetFiles(uploadsFolder, $"{User.Identity?.Name}.*");
                    foreach (var oldFile in oldFiles)
                        System.IO.File.Delete(oldFile);

                    // Lưu file mới (convert sang PNG)
                    using (var image = await SixLabors.ImageSharp.Image.LoadAsync(AvatarFile.OpenReadStream()))
                    {
                        await image.SaveAsPngAsync(filePath);
                    }

                    // AvatarPath chỉ dùng để render ra View (không lưu DB nếu bạn không cần)
                    model.AvatarPath = "/images/avatars/" + fileName;
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Profile"); // ✅ luôn redirect
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi khi cập nhật: " + ex.Message;
                return View(model); // ở lại form nếu có lỗi
            }
        }
    }
}
