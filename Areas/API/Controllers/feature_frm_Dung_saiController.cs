// feature_frm_Dung_sai_inch
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace QOS.Areas.API.Controllers
{
    [Area("API")]
    [Route("api/[controller]")]
    [ApiController]
    public class feature_frm_Dung_saiController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<feature_frm_Dung_saiController> _logger;

        public feature_frm_Dung_saiController(IConfiguration config, ILogger<feature_frm_Dung_saiController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }
        [HttpGet]
        public IActionResult feature_frm_Dung_sai()
        {
            
            try
            {
                
                var response = new
                {
                    Dung_sai = Dung_sai()
                };
                return Ok(response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCustomerData");
                return BadRequest(new { error = ex.Message });
            }
        }

        private List<object> Dung_sai()
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select * from Dung_sai ", conn))
            {
               
                // cmd.Parameters.AddWithValue("@UserName", UserName);

                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        ID = dr["ID"]?.ToString() ?? "",
                        Thong_so = dr["Thong_so"]?.ToString() ?? "",
                        Don_vi = dr["Don_vi"]?.ToString() ?? "",
                    });
                }
            }

            return list;
        }

    }
}