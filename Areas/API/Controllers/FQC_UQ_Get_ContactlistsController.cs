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
    public class FQC_UQ_Get_ContactlistsController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<FQC_UQ_Get_ContactlistsController> _logger;

        public FQC_UQ_Get_ContactlistsController(IConfiguration config, ILogger<FQC_UQ_Get_ContactlistsController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }
        [HttpGet]
        [HttpGet("[action]")]
        public IActionResult FQC_UQ_Get_Contactlists(string? Code_G, string? SO)
        {
            if (string.IsNullOrEmpty(Code_G))
                return BadRequest(new { error = "Code_G is required" });

            try
            {
                  
                var response = new
                {
                    Data_Contact_Info = Data_Contact_Info()
                };
            

                return Ok(response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Data_Contact_Info");
                return BadRequest(new { error = ex.Message });
            }
        }
        private List<object> Data_Contact_Info()
        {
            string sql = $@"Select STT,GroupID,GroupName,ContactList,Remark from TRACKING_GroupContactList ";
            List<object> list = new();

            using (SqlConnection conn = new(_connectionString))
            using (SqlCommand cmd = new(sql, conn))
            {
                
                // cmd.Parameters.AddWithValue("@FactoryID", FactoryID);

                conn.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
                
                while (dr.Read())
                {
                    list.Add(new
                    {
                        
                        STT = dr["STT"]?.ToString() ?? "",
                        GroupID = dr["GroupID"]?.ToString() ?? "",
                        GroupName = dr["GroupName"]?.ToString() ?? "",
                        ContactList = dr["ContactList"]?.ToString() ?? "",
                        Remark = dr["Remark"]?.ToString() ?? "",
                        
                    });
                }
            }

            return list;
        }
    }
}

// http://192.168.145.36:8080/api/FQC_UQ_Get_Contactlists?Code_G=NIKE-Document/ Accessories&SO=2323

