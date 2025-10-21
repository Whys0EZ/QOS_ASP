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
using QOS.Helpers;
using System.Text.RegularExpressions;


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
        private readonly string _connectionString;

        public Form8TPController(ILogger<Form8TPController> logger, IWebHostEnvironment env, IConfiguration configuration, AppDbContext context)
        {
            _logger = logger;
            _env = env;
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Missing connection string: DefaultConnection");
            
            _context = context;
        }
        [TempData]
        public string? MessageStatus { get; set;}
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

        public IActionResult ExportExcel(DateTime? dateFrom, DateTime? dateEnd, string? Unit, string? Searching)
        {
            var model = new Form8TPViewModel {
                
                Unit_List = GetUnitList(),
                Unit = Unit ?? "ALL",
                Search = Searching ?? "",
                DateFrom = dateFrom ?? DateTime.Now,
                DateEnd = dateEnd ?? DateTime.Now.Date.AddDays(1).AddTicks(-1),
                
            };

            // Tạo Excel file (ví dụ với ClosedXML hoặc EPPlus)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Đường dẫn tới file template
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Report", "RP_Form8_ThongSo_TP_SUM.xlsx");
            
            if (!System.IO.File.Exists(templatePath))
            {
                MessageStatus = "Không tìm thấy file mẫu báo cáo.";
                return RedirectToAction("RP_Form8");
            }

            using var package = new ExcelPackage(new FileInfo(templatePath));
            var worksheet = package.Workbook.Worksheets[0];

            // Ghi tiêu đề báo cáo
            worksheet.Cells["A2"].Value = Unit ;
            worksheet.Cells["B2"].Value = (dateFrom ?? DateTime.Now).ToString("dd/MM/yyyy");
            worksheet.Cells["D2"].Value = (dateEnd ?? DateTime.Now).ToString("dd/MM/yyyy");

             // Lấy dữ liệu báo cáo
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("RP_ThongSo_TP_SUM", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            
            cmd.Parameters.AddWithValue("@Date_F", dateFrom);
            cmd.Parameters.AddWithValue("@Date_T", dateEnd);
            cmd.Parameters.AddWithValue("@Search", Searching);
            cmd.Parameters.AddWithValue("@Page_No", 0);
            cmd.Parameters.AddWithValue("@Rows_page", 0);
            cmd.Parameters.AddWithValue("@Unit", Unit);
            
            

            conn.Open();
            int row = 4;
            using (var reader = cmd.ExecuteReader())
            {

                while (reader.Read())
                {
                    worksheet.Cells[row, 1].Value = row - 3; // STT
                    worksheet.Cells[row, 2].Value = reader["WorkDate"] == DBNull.Value ? "" : Convert.ToDateTime(reader["WorkDate"]).ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 3].Value = reader["Line"];
                    worksheet.Cells[row, 4].Value = reader["Supervisor"];
                    worksheet.Cells[row, 5].Value = reader["StyleCode"];
                    worksheet.Cells[row, 6].Value = reader["MO"];
                    worksheet.Cells[row, 7].Value = reader["ColorCode"];
                    worksheet.Cells[row, 8].Value = reader["Item"];
                    worksheet.Cells[row, 9].Value = reader["WorkstageName"];
                    worksheet.Cells[row, 10].Value = reader["PatternCode"];
                    worksheet.Cells[row, 11].Value = reader["BatchCode"];
                    worksheet.Cells[row, 12].Value = reader["TableCode"];
                    worksheet.Cells[row, 13].Value = reader["SizeList"];
                    worksheet.Cells[row, 14].Value = reader["UpdatedBy"];
                    worksheet.Cells[row, 15].Value = reader["Status_Flag"];
                    
                    row++;
                }
            }

            // Cập nhật công thức
            // hiện giá trị ngay cả khi chưa Enable Editing
            package.Workbook.Calculate();
            package.Workbook.CalcMode = ExcelCalcMode.Automatic;

            // Tạo file Excel để tải về
            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Report_EndLine_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }

        public IActionResult RP_Form8_ThongSo_TP_SUM_Detail_New(string? ID)
        {
            Base64Helper.Decode(ID);
            Console.WriteLine("id : "+ Base64Helper.Decode(ID));
            if (string.IsNullOrEmpty(ID))
            {
                return RedirectToAction("RP_Form8");
            }

            // Decrypt và parse ID string
            var idParts = DecryptAndSplit(ID);
            
            var model = new Form8TPDetailViewModel
            {
                WorkDate = idParts[0],
                FactoryID = idParts[1],
                TypeName = idParts[2],
                WorkstageName = idParts[3],
                LINE_No = idParts[4],
                StyleCode = idParts[5],
                MO = idParts[6],
                ColorCode = idParts[7],
                Item = idParts[8],
                Pattern = idParts[9],
                BatchCode = idParts[10],
                TableCode = idParts[11]
            };

            LoadDetailData(model);

            return View(model);
        }
        private void LoadDetailData(Form8TPDetailViewModel model)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Load Title Data
                LoadTitleData(connection, model);

                // Load Detail Data
                LoadMeasurementData(connection, model);
            }
        }

        private void LoadTitleData(SqlConnection connection, Form8TPDetailViewModel model)
        {
            var sqlTitle = @"SELECT TOP(1) * 
                           FROM Form8_ThongSo_TP_Title_Report 
                           WHERE Style_No = @StyleCode";

            using (var cmd = new SqlCommand(sqlTitle, connection))
            {
                cmd.Parameters.AddWithValue("@StyleCode", model.StyleCode);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        model.Customer = reader["Customer"]?.ToString();
                        model.Sample_Type = reader["Sample_Type"]?.ToString();
                        model.Sample_color = reader["Sample_color"]?.ToString();
                        model.Season = reader["Season"]?.ToString();
                        model.Board = reader["Board"]?.ToString();
                        model.Dev_Style_Name = reader["Dev_Style_Name"]?.ToString();
                        model.Category = reader["Category"]?.ToString();
                        model.Development_Size_Range = reader["Development_Size_Range"]?.ToString();
                        model.Fit_Intent = reader["Fit_Intent"]?.ToString();
                        model.Grade_Rule_Template = reader["Grade_Rule_Template"]?.ToString();
                        model.Img = reader["Img"]?.ToString();
                    }
                }
            }
        }

        private void LoadMeasurementData(SqlConnection connection, Form8TPDetailViewModel model)
        {
            var sql = "EXEC RP_ThongSo_TP_SUM_Detail_New @WorkDate, @FactoryID, @TypeName, @LINE_No, @StyleCode, @MO, @ColorCode, @Item, @Pattern, @BatchCode, @TableCode";

            using (var cmd = new SqlCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@WorkDate", model.WorkDate);
                cmd.Parameters.AddWithValue("@FactoryID", model.FactoryID);
                cmd.Parameters.AddWithValue("@TypeName", model.TypeName);
                cmd.Parameters.AddWithValue("@LINE_No", model.LINE_No);
                cmd.Parameters.AddWithValue("@StyleCode", model.StyleCode);
                cmd.Parameters.AddWithValue("@MO", model.MO);
                cmd.Parameters.AddWithValue("@ColorCode", model.ColorCode);
                cmd.Parameters.AddWithValue("@Item", model.Item);
                cmd.Parameters.AddWithValue("@Pattern", model.Pattern);
                cmd.Parameters.AddWithValue("@BatchCode", model.BatchCode);
                cmd.Parameters.AddWithValue("@TableCode", model.TableCode);

                using (var reader = cmd.ExecuteReader())
                {
                    model.MeasurementData = new List<Dictionary<string, object>>();
                    
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object>();
                        
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.GetValue(i);
                        }
                        
                        model.MeasurementData.Add(row);
                    }
                }
            }
        }

        private string[] DecryptAndSplit(string encryptedId)
        {
            // Implement your decryption logic here
            // This should match your dec() function in PHP
            var decrypted = Base64Helper.Decode(encryptedId);
            return decrypted.Split("#_#");
        }

        public IActionResult ExportExcelDetail(string ID)
        {
            Base64Helper.Decode(ID);
            Console.WriteLine("idExportExcelDetail : "+ Base64Helper.Decode(ID));
            // Export to Excel logic
            var model = new Form8TPDetailViewModel();
            // Load data and export
            
            return File(new byte[0], "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MeasurementSheet.xlsx");
        }


        // public IActionResult RP_Form8_ThongSo_TP_SUM_Detail_Full_Size(string ID)
        // {
        //     return View();
        // }

        public IActionResult RP_Form8_ThongSo_TP_SUM_Detail_Full_Size(string ID)
        {
            if (string.IsNullOrEmpty(ID))
            {
                return RedirectToAction("Index");
            }

            // Decrypt và parse ID string
            var idParts = DecryptAndSplit(ID);
            
            var model = new Form8TPTicketViewModel
            {
                WorkDate = idParts[0],
                FactoryID = idParts[1],
                TypeName = idParts[2],
                WorkstageName = idParts[3],
                LINE_No = idParts[4],
                StyleCode = idParts[5],
                MO = idParts[6],
                ColorCode = idParts[7],
                Item = idParts[8],
                Pattern = idParts[9],
                BatchCode = idParts[10],
                TableCode = idParts[11],
                SizeList = idParts.Length > 12 ? idParts[12] : ""
            };

            LoadTicketData(model);

            return View(model);
        }

        private void LoadTicketData(Form8TPTicketViewModel model)
        {
            var modifiedStr = model.SizeList.Replace(".", "_");
            var sizeList = modifiedStr.Split(';', StringSplitOptions.RemoveEmptyEntries);

            model.TicketGroups = new List<TicketGroup>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                foreach (var size in sizeList)
                {
                    var subparts = size.Split('_');
                    var sizeCode = subparts.Length > 0 ? subparts[0] : "";

                    var ticketGroup = new TicketGroup
                    {
                        SizeName = size,
                        SizeCode = sizeCode,
                        Measurements = new List<MeasurementItem>()
                    };

                    var sql = @"EXEC Form8_ThongSo_TP_Data_Ticket 
                              @WorkDate, @FactoryID, @TypeName, @WorkstageName, 
                              @LINE_No, @StyleCode, @MO, @ColorCode, 
                              @Item, @Pattern, @BatchCode, @TableCode, 
                              @SizeCode, @Size";

                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@WorkDate", model.WorkDate);
                        cmd.Parameters.AddWithValue("@FactoryID", model.FactoryID);
                        cmd.Parameters.AddWithValue("@TypeName", model.TypeName);
                        cmd.Parameters.AddWithValue("@WorkstageName", model.WorkstageName);
                        cmd.Parameters.AddWithValue("@LINE_No", model.LINE_No);
                        cmd.Parameters.AddWithValue("@StyleCode", model.StyleCode);
                        cmd.Parameters.AddWithValue("@MO", model.MO);
                        cmd.Parameters.AddWithValue("@ColorCode", model.ColorCode);
                        cmd.Parameters.AddWithValue("@Item", model.Item);
                        cmd.Parameters.AddWithValue("@Pattern", model.Pattern);
                        cmd.Parameters.AddWithValue("@BatchCode", model.BatchCode);
                        cmd.Parameters.AddWithValue("@TableCode", model.TableCode);
                        cmd.Parameters.AddWithValue("@SizeCode", sizeCode);
                        cmd.Parameters.AddWithValue("@Size", size);

                        using (var reader = cmd.ExecuteReader())
                        {
                            // if (reader.Read())
                            // {
                            //     ticketGroup.StyleCode = reader["StyleCode"]?.ToString();
                            // }

                            // do
                            // {
                            //     while (reader.Read())
                            //     {
                            //         var baseValue = reader["Base_Value"]?.ToString() ?? "";
                            //         var actValue = reader["ActValue"]?.ToString() ?? "";
                                    
                            //         string sumValue = CalculateSum(baseValue, actValue);

                            //         ticketGroup.Measurements.Add(new MeasurementItem
                            //         {
                            //             ItemID = reader["ItemID"]?.ToString(),
                            //             Item_Name = reader["Item_Name"]?.ToString(),
                            //             Base_Value = baseValue,
                            //             Sum_Value = sumValue
                            //         });
                            //     }
                            // } while (reader.NextResult());

                                do
                                {
                                    bool firstRow = true;

                                    while (reader.Read())
                                    {
                                        if (firstRow)
                                        {
                                            // Lấy thông tin chung từ dòng đầu tiên (ví dụ StyleCode)
                                            ticketGroup.StyleCode = reader["StyleCode"]?.ToString();
                                            firstRow = false;
                                        }

                                        // Phần xử lý dữ liệu chi tiết
                                        var baseValue = reader["Base_Value"]?.ToString() ?? "";
                                        var actValue = reader["ActValue"]?.ToString() ?? "";
                                        string sumValue = CalculateSum(baseValue, actValue);

                                        ticketGroup.Measurements.Add(new MeasurementItem
                                        {
                                            ItemID = reader["ItemID"]?.ToString(),
                                            Item_Name = reader["Item_Name"]?.ToString(),
                                            Base_Value = baseValue,
                                            Sum_Value = sumValue
                                        });
                                    }
                                } while (reader.NextResult());

                        }
                    }

                    model.TicketGroups.Add(ticketGroup);
                }
            }
        }

        private string CalculateSum(string baseValue, string actValue)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseValue) && string.IsNullOrWhiteSpace(actValue))
                    return "";

                baseValue = baseValue ?? "0";
                actValue = actValue ?? "0";

                // Kiểm tra có phải phân số hay hỗn số không
                if (IsMixedFraction(baseValue) || IsMixedFraction(actValue))
                {
                    var result = PerformOperation(baseValue, actValue);
                    return string.IsNullOrEmpty(result) || result == "0" ? "" : result;
                }
                else
                {
                    // Số thập phân hoặc số nguyên
                    if (double.TryParse(baseValue, out double baseNum) && 
                        double.TryParse(actValue, out double actNum))
                    {
                        var sum = baseNum + actNum;
                        return sum == 0 ? "" : sum.ToString();
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            return "";
        }

        #region Fraction Conversion Methods

        private bool IsMixedFraction(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            
            // Kiểm tra hỗn số: "1 1/2"
            if (Regex.IsMatch(input, @"^\d+ \d+/\d+$")) return true;
            
            // Kiểm tra phân số: "+1/2", "-1/2", "1/2"
            if (Regex.IsMatch(input, @"^[+-]?\d+/\d+$")) return true;
            
            return false;
        }

        private double ConvertToDecimal(string stringValue)
        {
            stringValue = stringValue?.Trim() ?? "0";

            // Hỗn số: "1 1/2"
            var mixedMatch = Regex.Match(stringValue, @"^(\d+) (\d+)/(\d+)$");
            if (mixedMatch.Success)
            {
                var wholePart = int.Parse(mixedMatch.Groups[1].Value);
                var numerator = int.Parse(mixedMatch.Groups[2].Value);
                var denominator = int.Parse(mixedMatch.Groups[3].Value);
                return wholePart + (double)numerator / denominator;
            }

            // Phân số: "1/2"
            var fractionMatch = Regex.Match(stringValue, @"^(\d+)/(\d+)$");
            if (fractionMatch.Success)
            {
                var numerator = int.Parse(fractionMatch.Groups[1].Value);
                var denominator = int.Parse(fractionMatch.Groups[2].Value);
                return (double)numerator / denominator;
            }

            // Số thập phân hoặc số nguyên
            if (double.TryParse(stringValue, out double result))
                return result;

            return 0;
        }

        private string ConvertToFraction(double decimalValue)
        {
            if (decimalValue == 0) return "0";

            var wholePart = (int)Math.Floor(decimalValue);
            var fractionalPart = decimalValue - wholePart;

            if (fractionalPart == 0)
                return wholePart.ToString();

            // Tìm phân số tối giản
            const int precision = 1000000;
            var gcd = GCD((int)Math.Round(fractionalPart * precision), precision);

            var numerator = (int)Math.Round(fractionalPart * precision / gcd);
            var denominator = precision / gcd;

            if (wholePart == 0)
                return $"{numerator}/{denominator}";

            return $"{wholePart} {numerator}/{denominator}";
        }

        private int GCD(int a, int b)
        {
            while (b != 0)
            {
                var remainder = a % b;
                a = b;
                b = remainder;
            }
            return Math.Abs(a);
        }

        private string PerformOperation(string stringValue, string operation)
        {
            var decimalValue = ConvertToDecimal(stringValue);
            operation = operation?.Trim() ?? "";

            // Nếu operation là '+' hoặc '-' không có giá trị
            if (operation == "+" || operation == "-" || operation == "+0" || operation == "-0")
            {
                return ConvertToFraction(decimalValue);
            }

            // Parse operation: "+1/2", "-1/2", "+1", "-1"
            var match = Regex.Match(operation, @"^([+-]?)(\d+)?(?:/(\d+))?$");
            if (!match.Success)
                return ConvertToFraction(decimalValue);

            var sign = match.Groups[1].Value;
            if (string.IsNullOrEmpty(sign)) sign = "+";

            var numerator = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
            var denominator = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 1;

            var fractionDecimal = (double)numerator / denominator;

            double result;
            if (sign == "+")
                result = decimalValue + fractionDecimal;
            else if (sign == "-")
                result = decimalValue - fractionDecimal;
            else
                result = decimalValue;

            return ConvertToFraction(result);
        }

        #endregion

        // private string[] DecryptAndSplit(string encryptedId)
        // {
        //     var decrypted = Decrypt(encryptedId);
        //     return decrypted.Split("#_#");
        // }

        // private string Decrypt(string encrypted)
        // {
        //     // Implement your decryption algorithm here
        //     return encrypted;
        // }

    }
}