using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace QOS.Middlewares
{
    public class ClearSessionMiddleware
    {
        private readonly RequestDelegate _next;

        public ClearSessionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Nếu chưa đăng nhập thì clear session
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                if (context.Session.Keys.Any()) // chỉ clear khi có session
                {
                    context.Session.Clear();
                    Console.WriteLine("⚠️ Session cleared vì User chưa login.");
                }
            }

            await _next(context);
        }
    }

    // Extension để dễ dàng đăng ký middleware
    public static class ClearSessionMiddlewareExtensions
    {
        public static IApplicationBuilder UseClearSessionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ClearSessionMiddleware>();
        }
    }
}
