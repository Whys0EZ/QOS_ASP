using Microsoft.AspNetCore.Mvc.Rendering;

namespace QOS.Helpers
{
    public static class HtmlHelperExtensions
    {
        public static string IsActive(this IHtmlHelper htmlHelper, string[] controllers, string[]? actions = null)
        {
            var routeData = htmlHelper.ViewContext.RouteData.Values;
            var currentCtrl = routeData["controller"]?.ToString();
            var currentAct = routeData["action"]?.ToString();

            bool matchController = controllers.Any(c =>
                string.Equals(c, currentCtrl, StringComparison.OrdinalIgnoreCase));

            bool matchAction = actions == null || actions.Length == 0 || actions.Any(a =>
                string.Equals(a, currentAct, StringComparison.OrdinalIgnoreCase));

            return matchController && matchAction ? "active" : "";
        }

    }
}
