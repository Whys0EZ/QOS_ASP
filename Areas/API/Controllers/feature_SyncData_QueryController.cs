using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using QOS.Areas.API.Helpers;

namespace QOS.Areas.API.Controllers
{
    [Area("API")]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SyncDataController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SyncDataController> _logger;

        public SyncDataController(
            IConfiguration config,
            IWebHostEnvironment environment,
            ILogger<SyncDataController> logger)
        {
            _config = config;
            _environment = environment;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }

        [HttpGet]
        [HttpPost]
        public IActionResult SaveQuery(
            [FromQuery] string? Code_G,
            [FromQuery] string? QueryData,
            [FromQuery] string? ENC,
            [FromQuery] int? DEC_LENGTH)
        {
            if (string.IsNullOrEmpty(Code_G))
                return BadRequest(new { feature = new[] { new { kq = "NG: Code_G is required" } } });

            if (string.IsNullOrEmpty(QueryData))
                return BadRequest(new { feature = new[] { new { kq = "NG: QueryData is required" } } });

            try
            {
                // ✅ Decrypt nếu cần (giống PHP)
                string processedQueryData = QueryData;
                
                if (ENC?.ToUpper() == "YES" && DEC_LENGTH.HasValue && DEC_LENGTH.Value > 0)
                {
                    try
                    {
                        // ✅ Decrypt với key và IV từ config hoặc dùng default
                        string key = _config.GetValue<string>("Encryption:Key") ?? "0123456789abcdef";
                        string iv = _config.GetValue<string>("Encryption:IV") ?? "fedcba9876543210";
                        
                        string decrypted = EncryptionHelper.Decrypt(QueryData, key, iv);
                        
                        // ✅ Substring với length (giống PHP)
                        if (decrypted.Length > DEC_LENGTH.Value)
                        {
                            processedQueryData = decrypted.Substring(0, DEC_LENGTH.Value);
                        }
                        else
                        {
                            processedQueryData = decrypted;
                        }

                        _logger.LogInformation($"Decrypted query - Original length: {QueryData.Length}, Decrypted length: {decrypted.Length}, Final length: {processedQueryData.Length}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error decrypting QueryData");
                        return Ok(new { feature = new[] { new { kq = "NG: Decryption failed - " + ex.Message } } });
                    }
                }

                // ✅ Validate Code_G
                string facCode = _config.GetValue<string>("AppSettings:FactoryCode") ?? "";
                var (isValid, factoryID, errorMsg) = Functions.ValidateCodeG(Code_G, facCode);

                if (!isValid)
                {
                    _logger.LogWarning($"Authentication failed: {errorMsg}");
                    return Ok(new { feature = new[] { new { kq = errorMsg } } });
                }

                // ✅ Parse Code_G để lấy FormID và LoginID
                var (codeGs, myDeviceName, myMAC, loginID, formID) = Functions.ParseCodeG(Code_G);

                // ✅ Insert vào database
                string result = InsertSyncDataQuery(formID, processedQueryData, loginID);

                return Ok(new { feature = new[] { new { kq = result } } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveQuery");
                return Ok(new { feature = new[] { new { kq = "NG: " + ex.Message } } });
            }
        }

        private string InsertSyncDataQuery(string formID, string queryData, string loginID)
        {
            try
            {
                using SqlConnection conn = new(_connectionString);
                
                // ✅ Xác định IsProcessed dựa trên độ dài query (giống PHP)
                int? isProcessed = queryData.Length > 160 ? null : (int?)1;

                string query = @"
                    INSERT INTO SyncData_Query 
                    VALUES (@FormID, @QueryData, @LoginID, @SyncTime, @IsProcessed)";

                using (SqlCommand cmd = new(query, conn))
                {
                    cmd.Parameters.Add("@FormID", SqlDbType.NVarChar).Value = formID;
                    cmd.Parameters.Add("@QueryData", SqlDbType.NVarChar).Value = queryData;
                    cmd.Parameters.Add("@LoginID", SqlDbType.NVarChar).Value = loginID;
                    cmd.Parameters.Add("@SyncTime", SqlDbType.DateTime).Value = DateTime.Now;
                    
                    if (isProcessed.HasValue)
                    {
                        cmd.Parameters.Add("@IsProcessed", SqlDbType.Int).Value = isProcessed.Value;
                    }
                    else
                    {
                        cmd.Parameters.Add("@IsProcessed", SqlDbType.Int).Value = DBNull.Value;
                    }

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        _logger.LogInformation($"SyncData_Query inserted - FormID: {formID}, Length: {queryData.Length}, IsProcessed: {isProcessed}");
                        
                        // ✅ Log file nếu query ngắn (giống PHP)
                        if (queryData.Length <= 200)
                        {
                            LogToFile($"Query saved - FormID: {formID}, LoginID: {loginID}, Length: {queryData.Length}");
                        }
                        
                        return "OK";
                    }
                    else
                    {
                        return "NG: No rows inserted";
                    }
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error in InsertSyncDataQuery");
                return "NG: Database error";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in InsertSyncDataQuery");
                return "NG: " + ex.Message;
            }
        }

        private void LogToFile(string message)
        {
            try
            {
                string logPath = Path.Combine(_environment.ContentRootPath, "log_sql.log");
                string logMessage = $"[ {DateTime.Now:yyyy-MM-dd HH:mm:ss} ] {message}{Environment.NewLine}";
                
                System.IO.File.AppendAllText(logPath, logMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing to log file");
            }
        }
    }
}


// GET /api/SyncData/SaveQuery?Code_G=abc123_____Device_____MAC_____user123_____FormA&QueryData=3a7f2c8b9e4d1a6f...&ENC=YES&DEC_LENGTH=100