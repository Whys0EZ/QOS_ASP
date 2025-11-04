using Microsoft.AspNetCore.Mvc;
// using QOS.Areas.API.Helpers;
using QOS.Areas.API.Models;

namespace QOS.Areas.API.Controllers
{
    [Area("API")]
    [Route("api/[controller]")]
    [ApiController]
    public class Get_SQLite_2_Server_BC_DiChuyen_Upload_IMGController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<Get_SQLite_2_Server_BC_DiChuyen_Upload_IMGController> _logger;

        public Get_SQLite_2_Server_BC_DiChuyen_Upload_IMGController(
            IConfiguration config,
            IWebHostEnvironment environment,
            ILogger<Get_SQLite_2_Server_BC_DiChuyen_Upload_IMGController> logger)
        {
            _config = config;
            _environment = environment;
            _logger = logger;
        }

        [HttpPost]
        public IActionResult Get_SQLite_2_Server_BC_DiChuyen_Upload_IMG(
            [FromQuery] string? Code_G,
            [FromForm] string? Form_Data,      // ✅ Thay đổi: nhận trực tiếp từ Form
            [FromForm] string? Folder,
            [FromForm] string? Img_Name,
            [FromForm] string? Image)
        {
            _logger.LogInformation("===== UPLOAD REQUEST =====");
            _logger.LogInformation($"Code_G: {Code_G}");
            _logger.LogInformation($"Form_Data: {Form_Data}");
            _logger.LogInformation($"Folder: {Folder}");
            _logger.LogInformation($"Img_Name: {Img_Name}");
            _logger.LogInformation($"Image length: {Image?.Length ?? 0}");


            if (string.IsNullOrEmpty(Code_G))
                return BadRequest(new { KQ = "NG: Code_G is required" });

            if (string.IsNullOrEmpty(Img_Name) || string.IsNullOrEmpty(Image))
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
                string formID = "Form4_BCCLM";
                string imagePath = Path.Combine(_environment.WebRootPath, "upload", "Photos", "Form4_BCCLM");
                string textCut = "_###_";
                // ✅ Tự động thêm folder theo tháng nếu Img_Name không có path
                string processedImgName = Img_Name;
                if (!Img_Name.Contains("/"))
                {
                    string monthFolder = DateTime.Now.ToString("yyyy-MMM", 
                        new System.Globalization.CultureInfo("en-US"));
                    
                    if (Img_Name.Contains(textCut))
                    {
                        string[] names = Img_Name.Split(textCut, StringSplitOptions.RemoveEmptyEntries);
                        processedImgName = string.Join(textCut, 
                            names.Select(n => $"{monthFolder}/{n.Trim()}"));
                    }
                    else
                    {
                        processedImgName = $"{monthFolder}/{Img_Name}";
                    }
                }
                _logger.LogInformation($"Original Img_Name: {Img_Name}");
                _logger.LogInformation($"Processed Img_Name: {processedImgName}");
                _logger.LogInformation($"Image path: {imagePath}");
                

                // ✅ SỬ DỤNG HELPER DECODE IMAGE
                string result = Functions.DecodeImgListAdd(
                    processedImgName,
                    Image,
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