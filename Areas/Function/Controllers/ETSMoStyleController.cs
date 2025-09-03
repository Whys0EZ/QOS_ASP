using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using QOS.Areas.Function.Models;
using QOS.Data;
using System.IO;
using System.Drawing;
using OfficeOpenXml.Drawing;

namespace QOS.Areas.Function.Controllers
{
    [Area("Function")]
    [Authorize]
    public class ETSMoStyleController : Controller
    {
        private readonly ILogger<ETSMoStyleController> _logger;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public ETSMoStyleController(ILogger<ETSMoStyleController> logger, AppDbContext context, IWebHostEnvironment env, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _env = env;
            _configuration = configuration;
        }
        [TempData]
        public string? MessageStatus { get; set; } = "";

        public IActionResult Index()
        {

            return View();
        }
        [HttpPost]
        public IActionResult Search(string Search_V)
        {
            var tb_string = new StringBuilder();
            if (string.IsNullOrEmpty(Search_V))
            {
                tb_string.AppendLine("No Search Value!!!!!!!!");
                return PartialView("_ResultTable", tb_string.ToString());
                // MessageStatus = "Chưa nhâp thông tin tìm kiếm";
                // return View("Index");
            }


            // 1. Kết nối DB
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
            }
            using (var conn = new SqlConnection(connString))

            // using (var conn = new SqlConnection(_configuration))
            {
                conn.Open();
                using (var cmd = new SqlCommand("RP_Search_ETS_MO_Information", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    // cmd.Parameters.AddWithValue("@FactoryID", "FactoryID");
                    cmd.Parameters.AddWithValue("@Search", Search_V);

                    using (var reader = cmd.ExecuteReader())
                    {
                        int i = 1, tr = 1;


                        while (reader.Read())
                        {

                            if (tr == 4) tr = 1;

                            if (i == 1)
                            {
                                tb_string.AppendLine("<table class='table-fixed table table-bordered table-striped' border='0' width='100%'>");
                                tb_string.AppendLine("<thead class='table-dark'>");
                                tb_string.AppendLine("<tr >");
                                tb_string.AppendLine("<td style='width:30px;' >No</td>");
                                tb_string.AppendLine("<td style='width:60px;' >MO</td>");
                                tb_string.AppendLine("<td style='width:100px;' >StyleName</td>");
                                tb_string.AppendLine("<td style='width:100px;' >ColorCode</td>");
                                tb_string.AppendLine("<td style='width:100px;' >Size</td>");
                                tb_string.AppendLine("</tr>");
                                tb_string.AppendLine("</thead>");
                                tb_string.AppendLine("<tbody>");

                            }
                            else { }

                            tb_string.AppendLine($"<tr class='tr{tr}'>");
                            tb_string.AppendLine($"<td style='text-align:center;'>{i}</td>");
                            tb_string.AppendLine($"<td>{reader["MO"]}</td>");
                            tb_string.AppendLine($"<td>{reader["StyleCode"]}</td>");
                            tb_string.AppendLine($"<td>{reader["ColorCode"]}</td>");
                            tb_string.AppendLine($"<td>{reader["Size"]}</td>");




                            i++;
                            tr++;
                        }
                        if (i == 1)
                        { 
                            tb_string.AppendLine("<table class='table-fixed table table-bordered table-striped' border='0' width='100%'>");
                            tb_string.AppendLine("<thead ><tr><td>Empty Data</td></tr></thead>");
                        }
                        tb_string.AppendLine("</tbody></table>");
                    }
                }
            }
             // return View("Index", tb_string.ToString());
            return PartialView("_ResultTable", tb_string.ToString());
        } 
    }
}
