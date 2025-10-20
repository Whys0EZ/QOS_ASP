using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QOS.Areas.Report.Models;
using QOS.Data;
using QOS.Models;
using System.Data;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QOS.Areas.Function.Filters;


using Microsoft.EntityFrameworkCore;
using Dapper;
using System.Text.Json;
using System.Drawing;

namespace QOS.Controllers
{
    [Authorize] // chỉ khi login mới được vào
    [Area("Report")]
    public class Form8TPController : Controller
    {
        private readonly ILogger<Form8TPController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public Form8TPController(ILogger<Form8TPController> logger, IWebHostEnvironment env, IConfiguration configuration, AppDbContext context)
        {
            _logger = logger;
            _env = env;
            _configuration = configuration;
            _context = context;
        }
        public IActionResult Index()
        {
            // return View();
            return RedirectToAction("RP_Form8", "Form8TP", new { area = "Report" });
        }
        public IActionResult RP_Form8(DateTime? dateFrom, DateTime? dateEnd, string? Unit, string? Searching, int? Page_No, int? Row_Page)
        {
            var model = new Form8TPViewModel {
                
                Unit_List = GetUnitList(),
                Unit = Unit ?? "ALL",
                Page_No = Page_No ?? 1,
                Row_Page = Row_Page ?? 30,
                Search = Searching ?? "",
                DateFrom = dateFrom ?? DateTime.Now,
                DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1),
                ReportData = new List<Dictionary<string, object>>()
            };
            LoadReportData(model);
            return View(model);
        }
        private List<QOS.Models.Unit_List> GetUnitList()
        {
            try
            {
                var units = _context.Set<QOS.Models.Unit_List>()
                    .Where(u => u.Factory == "REG2")
                    .OrderBy(u => u.Unit)
                    .ToList();

                _logger.LogInformation($"Loaded {units.Count} units from database");
                return units;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading unit list");
                return new List<QOS.Models.Unit_List>();
            }
        }
        private void LoadReportData(Form8TPViewModel model)
        {
            try
            {
                _logger.LogInformation("=== LoadReportData Start ===");
                
                // Prepare parameters for stored procedure
                var dateFrom = model.DateFrom.ToString("yyyy-MM-dd");
                var dateEnd = model.DateEnd.ToString("yyyy-MM-dd");
                var unit = string.IsNullOrEmpty(model.Unit) ? "ALL" : model.Unit;
                
                var Page_No = model.Page_No;
                var Rows_page = model.Row_Page;
               
                var search = model.Search;

                _logger.LogInformation($"SP Parameters: DateFrom={dateFrom}, DateTo={dateEnd}, unit={unit}, Page_No={Page_No}, Rows_page={Rows_page}, search={search} ");

                // Execute stored procedure
                var sql = @"EXEC RP_ThongSo_TP_SUM_OQL @Date_F, @Date_T,@Search,@Page_No,@Rows_page, @Unit";
                
                var parameters = new[]
                {
                    new Microsoft.Data.SqlClient.SqlParameter("@Date_F", dateFrom),
                    new Microsoft.Data.SqlClient.SqlParameter("@Date_T", dateEnd),
                    new Microsoft.Data.SqlClient.SqlParameter("@Search", search),
                    new Microsoft.Data.SqlClient.SqlParameter("@Page_No", Page_No),
                    new Microsoft.Data.SqlClient.SqlParameter("@Rows_page", Rows_page),
                    
                    new Microsoft.Data.SqlClient.SqlParameter("@Unit", unit),
                   
                };

                // Get raw data from stored procedure
                var connection = _context.Database.GetDbConnection();
                var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Parameters.AddRange(parameters);
                
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    connection.Open();
                }

                var reader = command.ExecuteReader();
                var reportDataList = new List<Dictionary<string, object>>();
               

                while (reader.Read())
                {
                    // Read data based on actual column names from SP
                    var rowData = new Dictionary<string, object>();
                    
                    // Map columns: Fault_Code, Fault_QTY, Fault_Level, Fault_Name_EN, Fault_Name_VN
                    var ID_L = reader["ID_L"] != DBNull.Value ? Convert.ToInt32(reader["ID_L"]) : 0;
                    var WorkDate = reader["WorkDate"] == DBNull.Value ? "" : Convert.ToDateTime(reader["WorkDate"]).ToString("yyyy-MM-dd");
                    
                    var FactoryID = reader["FactoryID"]?.ToString() ?? "";
                    var TypeName = reader["TypeName"]?.ToString() ?? "";
                    var CustomerName = reader["CustomerName"]?.ToString() ?? "";
                    var WorkstageName = reader["WorkstageName"]?.ToString() ?? "";
                    var Line = reader["Line"]?.ToString() ?? "";
                    var Supervisor = reader["Supervisor"]?.ToString() ?? "";
                    var StyleCode = reader["StyleCode"]?.ToString() ?? "";
                    var MO = reader["MO"]?.ToString() ?? "";
                    var ColorCode = reader["ColorCode"]?.ToString() ?? "";
                    var Item = reader["Item"]?.ToString() ?? "";
                    var PatternCode = reader["PatternCode"]?.ToString() ?? "";
                    var BatchCode = reader["BatchCode"]?.ToString() ?? "";
                    var TableCode = reader["TableCode"]?.ToString() ?? "";
                    var SizeList = reader["SizeList"]?.ToString() ?? "";
                    var UpdatedBy = reader["UpdatedBy"]?.ToString() ?? "";
                    var Status_Flag = reader["Status_Flag"]?.ToString() ?? "";
                    var Fault = reader["Fault"] != DBNull.Value ? Convert.ToInt32(reader["Fault"]) : 0;
                    var Qty = reader["Qty"] != DBNull.Value ? Convert.ToInt32(reader["Qty"]) : 0;
                    var Total_P = reader["Total_P"] != DBNull.Value ? Convert.ToInt32(reader["Total_P"]) : 0;
                    var Total_Rows = reader["Total_Rows"] != DBNull.Value ? Convert.ToInt32(reader["Total_Rows"]) : 0;

                    rowData["ID_L"] = ID_L;
                    rowData["WorkDate"] = WorkDate;
                    rowData["FactoryID"] = FactoryID;
                    rowData["TypeName"] = TypeName;
                    rowData["CustomerName"] = CustomerName;
                    rowData["WorkstageName"] = WorkstageName;
                    rowData["Line"] = Line;
                    rowData["Supervisor"] = Supervisor;
                    rowData["StyleCode"] = StyleCode;
                    rowData["MO"] = MO;
                    rowData["ColorCode"] = ColorCode;
                    rowData["Item"] = Item;
                    rowData["PatternCode"] = PatternCode;
                    rowData["BatchCode"] = BatchCode;
                    rowData["TableCode"] = TableCode;
                    rowData["SizeList"] = SizeList;
                    rowData["UpdatedBy"] = UpdatedBy;
                    rowData["Status_Flag"] = Status_Flag;
                    rowData["Fault"] = Fault;
                    rowData["Qty"] = Qty;
                    rowData["Total_P"] = Total_P;
                    rowData["Total_Rows"] = Total_Rows;
                    

                    reportDataList.Add(rowData);

                    
                }
                reader.Close();

            
                model.ReportData = reportDataList;
                //  _logger.LogInformation($"Statistics calculated - Total: {model.ReportData}, Defect Types: {model.ReportData.Count}");
               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing stored procedure");
                model.ReportData = new List<Dictionary<string, object>>();
               
                throw;
            }
        }

        public IActionResult ExportExcel()
        {
            return View();
        }

    }
}