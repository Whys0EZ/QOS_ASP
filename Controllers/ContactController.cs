using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using QOS.Areas.Report.Models;
using QOS.Data;
using QOS.Models;
using QOS.Areas.Function.Filters;

namespace QOS.Controllers
{
    public class ContactController : Controller
    {
        private readonly string _connectionString;

        public ContactController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Missing connection string: DefaultConnection");
        }

        // GET: Contact
        [HttpGet]
        public IActionResult Index()
        {
            var model = new ContactViewModel();
            return View(model);
        }

        // POST: Contact/Submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Submit(ContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin!";
                return View("Index", model);
            }

            try
            {
                SaveFeedback(model);
                TempData["SuccessMessage"] = "Cảm ơn bạn đã gửi phản hồi! Chúng tôi sẽ liên hệ lại sớm.";
                
                // Reset form
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return View("Index", model);
            }
        }

        private void SaveFeedback(ContactViewModel model)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var sql = @"INSERT INTO Contact_Feedback 
                           (Username, Email, Content, CreatedDate, Status) 
                           VALUES 
                           (@Username, @Email, @Content, @CreatedDate, @Status)";

                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@Username", model.Username);
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@Content", model.Content);
                    cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@Status", "Pending"); // New, Pending, Resolved

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Admin: View all feedbacks
        [Permission("SYS_Admin")]
        [HttpGet]
        public IActionResult Admin(string status = "All", string search = "")
        {
            var model = new FeedbackAdminViewModel
            {
                Feedbacks = GetAllFeedbacks(status, search),
                CurrentStatus = status,
                SearchKeyword = search,
                Statistics = GetStatistics()
            };

            return View(model);
        }

        // Admin: Get feedback detail
        [Permission("SYS_Admin")]
        [HttpGet]
        public IActionResult Detail(int id)
        {
            var feedback = GetFeedbackById(id);
            if (feedback == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy feedback này!";
                return RedirectToAction("Admin");
            }

            return View(feedback);
        }

        // Admin: Update status
        [HttpPost]
        public IActionResult UpdateStatus(int id, string status, string response)
        {
            try
            {
                UpdateFeedbackStatus(id, status, response);
                TempData["SuccessMessage"] = "Cập nhật trạng thái thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction("Detail", new { id });
        }

        // Admin: Delete feedback
        [HttpPost]
        public IActionResult Delete(int id)
        {
            try
            {
                DeleteFeedback(id);
                TempData["SuccessMessage"] = "Đã xóa feedback thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction("Admin");
        }

        #region Private Methods

        private List<ContactViewModel> GetAllFeedbacks(string status = "All", string search = "")
        {
            var feedbacks = new List<ContactViewModel>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var sql = @"SELECT Id, Username, Email, Content, CreatedDate, Status, 
                                  Response, ResponseDate, ResponseBy 
                           FROM Contact_Feedback 
                           WHERE (@Status = 'All' OR Status = @Status)
                           AND (@Search = '' OR Username LIKE '%' + @Search + '%' 
                                OR Email LIKE '%' + @Search + '%' 
                                OR Content LIKE '%' + @Search + '%')
                           ORDER BY CreatedDate DESC";

                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.Parameters.AddWithValue("@Search", search ?? "");

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            feedbacks.Add(new ContactViewModel
                            {
                                Id = reader["Id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Id"]),
                                Username = reader["Username"]?.ToString() ?? "",
                                Email = reader["Email"]?.ToString() ?? "",
                                Content = reader["Content"]?.ToString() ?? "",
                                CreatedDate = reader["CreatedDate"] == DBNull.Value
                                            ? DateTime.MinValue
                                            : Convert.ToDateTime(reader["CreatedDate"]),
                                Status = reader["Status"]?.ToString() ?? "",
                                Response = reader["Response"]?.ToString(),
                                ResponseDate = reader["ResponseDate"] == DBNull.Value
                                            ? null
                                            : Convert.ToDateTime(reader["ResponseDate"]),
                                ResponseBy = reader["ResponseBy"]?.ToString()
                            });
                        }
                    }
                }
            }

            return feedbacks;
        }

        private ContactViewModel GetFeedbackById(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var sql = @"SELECT Id, Username, Email, Content, CreatedDate, Status, 
                                  Response, ResponseDate, ResponseBy 
                           FROM Contact_Feedback 
                           WHERE Id = @Id";

                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@Id", id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new ContactViewModel
                            {
                                Id = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Email = reader.GetString(2),
                                Content = reader.GetString(3),
                                CreatedDate = reader.GetDateTime(4),
                                Status = reader.GetString(5),
                                Response = reader.IsDBNull(6) ? null : reader.GetString(6),
                                ResponseDate = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                                ResponseBy = reader.IsDBNull(8) ? null : reader.GetString(8)
                            };
                        }
                    }
                }
            }

            return null;
        }

        private void UpdateFeedbackStatus(int id, string status, string response)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var sql = @"UPDATE Contact_Feedback 
                           SET Status = @Status, 
                               Response = @Response,
                               ResponseDate = @ResponseDate,
                               ResponseBy = @ResponseBy
                           WHERE Id = @Id";

                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.Parameters.AddWithValue("@Response", string.IsNullOrEmpty(response) ? DBNull.Value : response);
                    cmd.Parameters.AddWithValue("@ResponseDate", string.IsNullOrEmpty(response) ? DBNull.Value : DateTime.Now);
                    cmd.Parameters.AddWithValue("@ResponseBy", string.IsNullOrEmpty(response) ? DBNull.Value : User.Identity.Name ?? "Admin");

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void DeleteFeedback(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var sql = "DELETE FROM Contact_Feedback WHERE Id = @Id";

                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private FeedbackStatistics GetStatistics()
        {
            var stats = new FeedbackStatistics();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var sql = @"SELECT 
                               COUNT(*) as Total,
                               SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END) as Pending,
                               SUM(CASE WHEN Status = 'Resolved' THEN 1 ELSE 0 END) as Resolved,
                               SUM(CASE WHEN Status = 'Closed' THEN 1 ELSE 0 END) as Closed,
                               SUM(CASE WHEN CreatedDate >= DATEADD(day, -7, GETDATE()) THEN 1 ELSE 0 END) as ThisWeek,
                               SUM(CASE WHEN CreatedDate >= DATEADD(day, -30, GETDATE()) THEN 1 ELSE 0 END) as ThisMonth
                           FROM Contact_Feedback";

                using (var cmd = new SqlCommand(sql, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        stats.Total     = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        stats.Pending   = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                        stats.Resolved  = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                        stats.Closed    = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                        stats.ThisWeek  = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                        stats.ThisMonth = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);
                    }
                }
            }

            return stats;
        }

        #endregion

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}