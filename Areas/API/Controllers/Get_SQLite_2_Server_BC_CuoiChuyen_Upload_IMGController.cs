using Microsoft.AspNetCore.Mvc;
// using QOS.Areas.API.Helpers;
using QOS.Areas.API.Models;

namespace QOS.Areas.API.Controllers
{
    [Area("API")]
    [Route("api/[controller]")]
    [ApiController]
    public class Get_SQLite_2_Server_BC_CuoiChuyen_Upload_IMGController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<Get_SQLite_2_Server_BC_CuoiChuyen_Upload_IMGController> _logger;

        public Get_SQLite_2_Server_BC_CuoiChuyen_Upload_IMGController(
            IConfiguration config,
            IWebHostEnvironment environment,
            ILogger<Get_SQLite_2_Server_BC_CuoiChuyen_Upload_IMGController> logger)
        {
            _config = config;
            _environment = environment;
            _logger = logger;
        }

        [HttpPost]
        public IActionResult Get_SQLite_2_Server_BC_CuoiChuyen_Upload_IMG(
            [FromQuery] string? Code_G,
            [FromForm] PhotoUploadRequest request)
        {
            if (string.IsNullOrEmpty(Code_G))
                return BadRequest(new { KQ = "NG: Code_G is required" });

            if (request == null || string.IsNullOrEmpty(request.Img_Name) || string.IsNullOrEmpty(request.Image))
                return BadRequest(new { KQ = "NG: Img_Name and Image are required" });

            try
            {
                // ✅ SỬ DỤNG HELPER VALIDATE CODE_G
                string facCode = _config.GetValue<string>("AppSettings:FactoryCode") ?? "";
                var (isValid, factoryID, errorMsg) = Functions.ValidateCodeG(Code_G, facCode);

                if (!isValid)
                {
                    _logger.LogWarning($"Authentication failed - Error: {errorMsg}");
                    return Ok(new { KQ = errorMsg });
                }

                // ✅ XỬ LÝ UPLOAD
                string formID = "Form6_BCKCC";
                string imagePath = Path.Combine(_environment.WebRootPath, "upload", "Photos", "Form6_BCKCC");
                string textCut = "_###_";

                // ✅ SỬ DỤNG HELPER DECODE IMAGE
                string result = Functions.DecodeImgListAdd(
                    request.Img_Name,
                    request.Image,
                    imagePath,
                    textCut,
                    formID,
                    _logger);

                return Ok(new { KQ = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UploadPhoto");
                return Ok(new { KQ = "NG: " + ex.Message });
            }
        }
    }

}