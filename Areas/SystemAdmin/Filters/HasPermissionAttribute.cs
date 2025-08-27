using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using QOS.Services; // namespace chứa IUserPermissionService

namespace QOS.Areas.SystemAdmin.Filters
{
    public class PermissionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _functionCode;

        public PermissionAttribute(string functionCode)
        {
            _functionCode = functionCode;
        }



        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            var username = user.Identity?.Name;

            if (string.IsNullOrEmpty(username))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            var permissionService = context.HttpContext.RequestServices.GetService<IUserPermissionService>();
            if (permissionService == null || !permissionService.HasPermission(username, _functionCode))
            {
                // Store error message in Items, since TempData is not available here
                context.HttpContext.Items["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này.";
                context.Result = new RedirectToActionResult("AccessDenied", "System", null);
            }
        }
    }
}