using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using QOS.Data;
using QOS.Middlewares;
using QOS.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// đang ki service UserPermission
builder.Services.AddScoped<IUserPermissionService, UserPermissionService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // giữ nguyên tên property
    });
    
// builder.Services.AddSession();

// Thêm Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(1);
});

// Thêm Authentication cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie( options =>
    {
        options.LoginPath = "/Account/Auth";  // Nếu chưa login → chuyển về trang login
        options.LogoutPath = "/Account/Logout"; // Đường dẫn logout
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(1);   // Cookie sống 1 ngày
        options.SlidingExpiration = true; // reset lại thời gian khi user thao tác
    });

// ✅ Đặt license cho EPPlus 1 lần toàn app
// ExcelPackage.License = new LicenseContext(LicenseType.NonCommercial);
// ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
builder.WebHost.UseUrls("http://0.0.0.0:8080");

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();  // phải trước Authorization
app.UseAuthorization();   // sau Authentication

app.UseSession(); 
// app.UseClearSessionMiddleware(); // Middleware xóa session khi chưa đăng nhập
app.MapAreaControllerRoute(
    name: "function",
    areaName: "Function",
    pattern: "Function/{controller=Function}/{action=Index}/{id?}"
);
app.MapAreaControllerRoute(
    name: "report",
    areaName: "Report",
    pattern: "Report/{controller=Report}/{action=Index}/{id?}"
);
app.MapAreaControllerRoute(
    name: "setting",
    areaName: "Setting",
    pattern: "Setting/{controller=Setting}/{action=Index}/{id?}"
);
app.MapAreaControllerRoute(
    name: "system",
    areaName: "SystemAdmin",
    pattern: "SystemAdmin/{controller=SystemAdmin}/{action=Index}/{id?}"
);
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
