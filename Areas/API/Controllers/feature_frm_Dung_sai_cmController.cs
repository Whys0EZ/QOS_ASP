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
    public class feature_frm_Dung_sai_cmController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<feature_frm_Dung_sai_cmController> _logger;

        public feature_frm_Dung_sai_cmController(IConfiguration config, ILogger<feature_frm_Dung_sai_cmController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }
        [HttpGet]
        public IActionResult feature_frm_Dung_sai_cm()
        {
            
            try
            {
                
                var response = new
                {
                    Dung_sai_cm = Dung_sai_cm()
                };
                return Ok(response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCustomerData");
                return BadRequest(new { error = ex.Message });
            }
        }

        private List<object> Dung_sai_cm()
        {
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new("Select * from Dung_sai where Don_vi = 'cm' ", conn))
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