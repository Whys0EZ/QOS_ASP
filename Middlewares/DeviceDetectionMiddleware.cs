using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace QOS.Middlewares
{
    public class DeviceDetectionMiddleware
    {
        private readonly RequestDelegate _next;

        public DeviceDetectionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var userAgent = context.Request.Headers["User-Agent"].ToString().ToLower();

            bool isMobile =
                userAgent.Contains("iphone") ||
                userAgent.Contains("android") ||
                userAgent.Contains("ipad") ||
                userAgent.Contains("mobile");

            context.Items["IsMobile"] = isMobile;

            await _next(context);
        }
    }
}
