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
    public class FQC_Get_Contactlist_InforController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<FQC_Get_Contactlist_InforController> _logger;

        public FQC_Get_Contactlist_InforController(IConfiguration config, ILogger<FQC_Get_Contactlist_InforController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }
        [HttpGet]
        [HttpGet("[action]")]
        public IActionResult FQC_Get_Contactlist_Infor(string? Code_G, string? SO)
        {
            if (string.IsNullOrEmpty(Code_G))
                return BadRequest(new { error = "Code_G is required" });

            try
            {
                  
                var response = new
                {
                    ETS_Data_Contact_Info = ETS_Data_Contact_Info(Code_G)
                };
            

                return Ok(response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ETS_Data_Contact_Info");
                return BadRequest(new { error = ex.Message });
            }
        }
        private List<object> ETS_Data_Contact_Info(string Code_G)
        {
            string sql = $@"Select distinct ContactList,Remark from TRACKING_GroupContactList where GroupName = N'{Code_G}' ";
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
                        
                        ContactList = dr["ContactList"]?.ToString() ?? "",
                        Remark = dr["Remark"]?.ToString() ?? "",

                        
                    });
                }
            }

            return list;
        }
    }
}

// http://192.168.145.36:8080/api/FQC_Get_Contactlist_Infor?Code_G=NIKE-Document/ Accessories&SO=2323

