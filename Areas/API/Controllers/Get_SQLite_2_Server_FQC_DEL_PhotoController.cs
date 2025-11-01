using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using QOS.Areas.API.Models;


namespace QOS.Areas.API.Controllers
{
    [Area("API")]
    [Route("api/[controller]")]
    [ApiController]
    public class Get_SQLite_2_Server_FQC_DEL_PhotoController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<Get_SQLite_2_Server_FQC_DEL_PhotoController> _logger;

        public Get_SQLite_2_Server_FQC_DEL_PhotoController(
            IConfiguration config,
            IWebHostEnvironment environment,
            ILogger<Get_SQLite_2_Server_FQC_DEL_PhotoController> logger)
        {
            _config = config;
            _environment = environment;
            _logger = logger;
        }

        [HttpPost]
        [HttpDelete] // Hỗ trợ cả POST và DELETE
        public IActionResult Get_SQLite_2_Server_FQC_DEL_Photo(
            [FromQuery] string? Code_G,
            [FromBody] DeletePhotoRequest? request)
        {
            if (string.IsNullOrEmpty(Code_G))
                return BadRequest(new { KQ = "NG: Code_G is required" });

            if (request == null || string.IsNullOrEmpty(request.Img_Name))
                return BadRequest(new { KQ = "NG: Img_Name is required" });

            try
            {
                // ✅ Validate Code_G
                string facCode = _config.GetValue<string>("AppSettings:FactoryCode") ?? "";
                var (isValid, factoryID, errorMsg) = Functions.ValidateCodeG(Code_G, facCode);

                if (!isValid)
                {
                    _logger.LogWarning($"Authentication failed: {errorMsg}");
                    return Ok(new { KQ = errorMsg });
                }

                // ✅ Xóa ảnh
                string imagePath = Path.Combine(
                    _environment.WebRootPath, 
                    "upload", 
                    "Photos", 
                    "FQC");

                string textCut = "_###_";

                string result = Functions.DeleteImgList(
                    request.Img_Name,
                    imagePath,
                    textCut,
                    _logger);

                return Ok(new { KQ = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeletePhoto");
                return Ok(new { KQ = $"NG: {ex.Message}" });
            }
        }
    }

}