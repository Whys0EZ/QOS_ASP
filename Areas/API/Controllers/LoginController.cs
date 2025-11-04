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
    public class LoginController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<LoginController> _logger;

        public LoginController(IConfiguration config, ILogger<LoginController> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
        }

        [HttpGet]
        public IActionResult Login(string? Code_G, string? DB_Ver)
        {
            if (string.IsNullOrEmpty(Code_G))
                return BadRequest("Code_G is required");

            string[] txt = Code_G.Split('_');
            if (txt.Length < 4)
                return BadRequest("Invalid Code_G format");

            string code = txt[0];
            string password_G = txt[1].ToUpper();
            string myDeviceName = txt[2];
            string factoryID = txt[3];
            string myMAC = txt.Length > 4 ? txt[4] : "";

            // Giải mã Code_G
            string tmp1 = code.Length >= 32 ? code.Substring(0, 32) : "";
            string tmp2 = code.Length > 64 ? code.Substring(0, code.Length - 32) : "";
            string tmp3 = tmp2.Length >= 32 ? tmp2.Substring(tmp2.Length - 32) : "";
            string tmp4 = tmp2.Length > 64 ? tmp2.Substring(32) : "";
            string tmp5 = tmp4.Length > 32 ? tmp4.Substring(0, tmp4.Length - 32) : "";
            string mLoginID = tmp5;

            string facCode = _config.GetValue<string>("AppSettings:FactoryCode") ?? "";

            // _logger.LogInformation("=== Login GET Request === " + password_G);

            if (tmp1 != MD5Hash(facCode) || tmp3 != MD5Hash(mLoginID))
            {
                return Unauthorized(new { message = "Invalid factory or user hash" });
            }

            using (SqlConnection conn = new(_connectionString))
            {
                conn.Open();

                using (SqlCommand cmd = new("Json_Get_User_Information", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserName", mLoginID);
                    cmd.Parameters.AddWithValue("@FactoryID", factoryID);

                    using SqlDataReader dr = cmd.ExecuteReader();

                    if (!dr.Read())
                        return NotFound(new { message = "User not found" });

                    // ✅ LƯU TẤT CẢ DỮ LIỆU TRƯỚC KHI ĐÓNG DataReader
                    string status = "";
                    string act = dr["Act"].ToString() ?? "";
                    string pass = dr["Pass"].ToString() ?? "";
                    
                    // Lưu các giá trị cần dùng sau này
                    string userFactoryID = dr["FactoryID"].ToString() ?? "";
                    string userName = dr["UserName"].ToString() ?? "";
                    
                    var userInfo = new
                    {
                        LoginID = dr["UserName"],
                        FactoryID = dr["FactoryID"],
                        Fac_Name_Line1 = dr["Fac_Name_Line1"],
                        Fac_Name_Line2 = dr["Fac_Name_Line2"],
                        TeamID = dr["TeamID"],
                        FullName = dr["FullName"],
                        Phone_Num = dr["Phone_Num"],
                        Email = dr["Email"],
                        Status = status, // Sẽ update sau
                        Password = dr["Pass"],
                        Unit_Check = dr["Unit_Check"],
                        Line_Check = dr["Line_Check"],
                        SMS_F = dr["SMS_F"],
                        SMS_EN = dr["SMS_EN"],
                        SMS_LC = dr["SMS_LC"],
                        SyncTime = dr["SyncTime"],
                        KeepDataTime = dr["KeepDataTime"],
                        Q_F1 = dr["Q_F1"],
                        Q_F2 = dr["Q_F2"],
                        Q_F3 = dr["Q_F3"],
                        Q_F4 = dr["Q_F4"],
                        Q_F5 = dr["Q_F5"],
                        Q_F6 = dr["Q_F6"],
                        Q_F7 = dr["Q_F7"],
                        Q_F8 = dr["Q_F8"],
                        Ver_No = dr["Ver_No"],
                        Ver_Name = dr["Ver_Name"],
                        DB_Ver = dr["DB_Ver"]
                    };

                    // _logger.LogInformation("=== act GET Request === " + act);

                    // ✅ Đóng DataReader SAU KHI đã lưu hết dữ liệu
                    dr.Close();

                    // Xử lý login status
                    if (act != "True")
                    {
                        status = "Not_Act";
                    }
                    else if (password_G == pass.ToUpper())
                    {
                        status = "Login_OK";
                        
                        // ✅ Bây giờ dùng biến đã lưu thay vì dr["FactoryID"]
                        using (SqlCommand log = new("Insert_Login_Log", conn))
                        {
                            log.CommandType = CommandType.StoredProcedure;
                            log.Parameters.AddWithValue("@FactoryID", userFactoryID);
                            log.Parameters.AddWithValue("@Emp_No", userName);
                            log.Parameters.AddWithValue("@Login_Status", status);
                            log.Parameters.AddWithValue("@DeviceName", myDeviceName);
                            log.Parameters.AddWithValue("@MAC_add", myMAC);
                            log.Parameters.AddWithValue("@DB_Ver", DB_Ver);
                            log.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        status = "Login Failed";
                    }

                    // ✅ Tạo lại object với status đã update
                    return Ok(new[]
                    {new {
                            userInfo.LoginID,
                            userInfo.FactoryID,
                            userInfo.Fac_Name_Line1,
                            userInfo.Fac_Name_Line2,
                            userInfo.TeamID,
                            userInfo.FullName,
                            userInfo.Phone_Num,
                            userInfo.Email,
                            Status = status, // Status đã được cập nhật
                            userInfo.Password,
                            userInfo.Unit_Check,
                            userInfo.Line_Check,
                            userInfo.SMS_F,
                            userInfo.SMS_EN,
                            userInfo.SMS_LC,
                            userInfo.SyncTime,
                            userInfo.KeepDataTime,
                            userInfo.Q_F1,
                            userInfo.Q_F2,
                            userInfo.Q_F3,
                            userInfo.Q_F4,
                            userInfo.Q_F5,
                            userInfo.Q_F6,
                            userInfo.Q_F7,
                            userInfo.Q_F8,
                            userInfo.Ver_No,
                            userInfo.Ver_Name,
                            userInfo.DB_Ver
                        }
                    });
                }
            }
        }

        private static string MD5Hash(string input)
        {
            using var md5 = MD5.Create();
            byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }
}
