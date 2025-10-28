using Microsoft.AspNetCore.Mvc;

namespace QOS.Areas.Api.Controllers
{
    [Area("Api")]
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet("Ping")]
        public IActionResult Ping()
        {
            return Ok(new
            {
                status = true,
                message = "API is working!",
                serverTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        [HttpPost("Echo")]
        public IActionResult Echo([FromBody] dynamic body)
        {
            return Ok(new
            {
                status = true,
                received = body
            });
        }
    }
}
