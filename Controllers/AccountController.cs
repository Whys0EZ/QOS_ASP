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
using QOS;
using Microsoft.Extensions.Localization;

namespace QOS.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<QOS.SharedResource> _sharedLocalizer;

        public AccountController(ILogger<AccountController> logger, AppDbContext context,IStringLocalizer<QOS.SharedResource> sharedLocalizer) 
        { 
            _logger = logger;
            _context = context;
            _sharedLocalizer = sharedLocalizer;
        }
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
        public IActionResult Auth()
        {
           var vm = new AuthViewModel();

            // Nạp dữ liệu cho phần Register
            vm.Register.FactoryOptions = _context.Factory_List
                .Select(f => new SelectListItem
                {
                    Value = f.FactoryID,
                    Text = f.FactoryID
                })
                .ToList();

            vm.Register.TeamOptions = _context.Team_List
                .Select(t => new SelectListItem
                {
                    Value = t.TeamID,
                    Text = t.TeamName
                })
                .ToList();

            return View(vm); // View Index.cshtml chứa cả Login và Register
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Auth(AuthViewModel vm, string actionType)
        {
            // Nạp dữ liệu cho phần Register
            vm.Register.FactoryOptions = _context.Factory_List
                .Select(f => new SelectListItem
                {
                    Value = f.FactoryID,
                    Text = f.FactoryID
                })
                .ToList();

            vm.Register.TeamOptions = _context.Team_List
                .Select(t => new SelectListItem
                {
                    Value = t.TeamID,
                    Text = t.TeamName
                })
                .ToList();
            // Console.WriteLine("login");
            if (actionType == "login")
            {
                // Xử lý Login
                if (string.IsNullOrWhiteSpace(vm.Login.Username) || string.IsNullOrWhiteSpace(vm.Login.Password))
                {
                   
                    // ViewBag.Error = "Vui lòng nhập Username và Password!";
                    ViewBag.Error = _sharedLocalizer["LoginRequired"];
                    ViewBag.ActiveForm  = "login";
                    return View(vm);
                }
                Console.WriteLine(vm.Login.Password);

                var hash = HashPassword(vm.Login.Password);
                var user = _context.Users
                    .FromSqlRaw("EXEC Exec_Login_by_UserName @p0, @p1", vm.Login.Username, hash)
                    .AsEnumerable()
                    .FirstOrDefault();

                if (user == null)
                {
                    // ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu!";
                    ViewBag.Error = _sharedLocalizer["ErrorRequired"];
                    ViewBag.ActiveForm  = "login";
                    return View(vm);
                }

                // Claims + Cookie
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    claimsPrincipal,
                    new AuthenticationProperties
                    {
                        // IsPersistent = true,
                        // ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
                        
                        IsPersistent = vm.Login.RememberMe,   // 👈 Dùng giá trị người dùng chọn
                        ExpiresUtc = vm.Login.RememberMe
                            ? DateTimeOffset.UtcNow.AddDays(7)   // Nếu có “Ghi nhớ đăng nhập” → giữ 7 ngày
                            : DateTimeOffset.UtcNow.AddHours(12) // Nếu không có thì giữ trong 12h
                    });

                HttpContext.Session.SetString("Username", user.Username);
                return RedirectToAction("Index", "Home", new { main = "Index" });
            }
            else if (actionType == "register")
            {
                // Xử lý Register
                if (string.IsNullOrWhiteSpace(vm.Register.FullName) ||string.IsNullOrWhiteSpace(vm.Register.Username) || string.IsNullOrWhiteSpace(vm.Register.Pass))
                {
                   
                    ViewBag.Error = _sharedLocalizer["RegRequired"];
                    ViewBag.ActiveForm  = "register";
                    return View(vm);
                }

                if (_context.Users.Any(u => u.Username == vm.Register.Username))
                {
                    // ModelState.AddModelError("Register.Username", "Username đã tồn tại");
                    MessageStatus = _sharedLocalizer["UsernameExist"];
                    ModelState.AddModelError("Register.Username", _sharedLocalizer["UsernameExist"]);
                    ViewBag.ActiveForm  = "register";
                    ReloadDropdown(vm.Register);
                    return View(vm);
                }

                var passwordHash = HashPassword(vm.Register.Pass);

                var user = new User
                {
                    FactoryID = vm.Register.FactoryID,
                    TeamID = vm.Register.TeamID,
                    Username = vm.Register.Username,
                    Pass = passwordHash,
                    FullName = vm.Register.FullName,
                    Email = vm.Register.Email,
                    Act = true,
                    UserLevel = 0,
                    Fac_per = "",
                    LoginLevel = 9,
                    UserUpdate = vm.Register.UserUpdate ?? vm.Register.Username,
                    LastUpdate = DateTime.Now,
                    Unit_Check = "@ALL",
                    Line_Check = "@ALL"
                };

                _context.Users.Add(user);
                _context.SaveChanges();

                _context.UserPermissions.Add(new UserPermission
                {
                    FactoryID = vm.Register.FactoryID,
                    UserName = vm.Register.Username,
                    SYS_Admin = false,
                    UserUpdate = vm.Register.UserUpdate ?? vm.Register.Username,
                    LastUpdate = DateTime.Now,
                    B_F1 = true, B_F2 = true, B_F3 = true,
                    B_F4 = true, B_F5 = true, B_F6 = true,
                    B_F7 = true, B_F8 = true
                });
                _context.SaveChanges();

                MessageStatus = _sharedLocalizer["RegSuccess"];
                return RedirectToAction("Auth");
            }

            return View(vm);
        }

        private void ReloadDropdown(UserEditViewModel? reg)
        {
            if (reg == null) reg = new UserEditViewModel();
            reg.FactoryOptions = _context.Factory_List
                .Select(f => new SelectListItem { Value = f.FactoryID, Text = f.FactoryID })
                .ToList();
            reg.TeamOptions = _context.Team_List
                .Select(t => new SelectListItem { Value = t.TeamID, Text = t.TeamName })
                .ToList();
        }

        // ---------------- LOGOUT ----------------
        public async Task<IActionResult> LogoutAsync()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Auth", "Account");
        }

        // public async Task<IActionResult> LogoutAsync()
        // {
        //     HttpContext.Session.Clear();
        //     await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        //     return RedirectToAction("Login", "Account");
        // }
        
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
