using Microsoft.AspNetCore.Mvc;
using QOS.Areas.API.Helpers;
using QOS.Areas.API.Models;

namespace QOS.Areas.API.Controllers
{
    [Area("API")]
    [Route("api/[controller]")]
    [ApiController]
    public class SendTrackingEmailController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SendTrackingEmailController> _logger;

        public SendTrackingEmailController(IConfiguration config, ILogger<SendTrackingEmailController> logger)
        {
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Send tracking email
        /// </summary>
        [HttpPost("SendTrackingEmail")]
        public async Task<IActionResult> SendTrackingEmail([FromBody] SendEmailRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email))
                return BadRequest(new { error = "Email is required" });

            try
            {
                // Get approve name from config or default
                string approveName = _config.GetValue<string>("Email:ApproveName") ?? "Team";

                // Build email body
                string emailBody = EmailHelper.BuildTrackingEmailBody(
                    approveName: approveName,
                    group: request.Solution ?? "",
                    so: request.Infor_01 ?? "",
                    style: request.Infor_02 ?? "",
                    po: request.Infor_04 ?? "",
                    pro: request.Pro ?? "",
                    qty: request.Qty ?? "",
                    destination: request.Destination ?? "",
                    updateDate: request.Update ?? "",
                    itemNo: request.Item_No ?? "",
                    status: request.Status ?? "",
                    remark: request.Remark ?? ""
                );

                // Send email via API
                string result = await EmailHelper.SendEmailApiAsync(
                    emailTo: request.Email,
                    subject: "FCA SEND MAIL TRACKING",
                    body: emailBody,
                    logger: _logger
                );

                return Ok(new { result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending tracking email");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Send custom email
        /// </summary>
        [HttpPost("SendCustomEmail")]
        public async Task<IActionResult> SendCustomEmail([FromBody] CustomEmailRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email))
                return BadRequest(new { error = "Email is required" });

            if (string.IsNullOrEmpty(request.Subject))
                return BadRequest(new { error = "Subject is required" });

            if (string.IsNullOrEmpty(request.Body))
                return BadRequest(new { error = "Body is required" });

            try
            {
                string result = await EmailHelper.SendEmailApiAsync(
                    emailTo: request.Email,
                    subject: request.Subject,
                    body: request.Body,
                    logger: _logger
                );

                return Ok(new { result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending custom email");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    // ===== ADDITIONAL MODEL =====
    public class CustomEmailRequest
    {
        public string Email { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Body { get; set; } = "";
    }
}