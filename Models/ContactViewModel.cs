
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;

namespace QOS.Models
{
    public class ContactViewModel
    {
        /// <summary>
        /// ID của feedback (auto-generated từ database)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Họ và tên người gửi
        /// </summary>
        [Required(ErrorMessage = "Vui lòng nhập tên của bạn")]
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string? Username { get; set; }

        /// <summary>
        /// Email người gửi
        /// </summary>
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        /// <summary>
        /// Nội dung phản hồi
        /// </summary>
        [Required(ErrorMessage = "Vui lòng nhập nội dung phản hồi")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Nội dung phải có từ 10 đến 2000 ký tự")]
        [Display(Name = "Nội dung")]
        public string? Content { get; set; }

        /// <summary>
        /// Ngày tạo feedback
        /// </summary>
        [Display(Name = "Ngày gửi")]
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        /// Trạng thái: Pending, Resolved, Closed
        /// </summary>
        [Display(Name = "Trạng thái")]
        public string? Status { get; set; }

        /// <summary>
        /// Câu trả lời từ admin (optional)
        /// </summary>
        [Display(Name = "Phản hồi")]
        public string? Response { get; set; }

        /// <summary>
        /// Ngày admin phản hồi
        /// </summary>
        [Display(Name = "Ngày phản hồi")]
        public DateTime? ResponseDate { get; set; }

        /// <summary>
        /// Người phản hồi (admin username)
        /// </summary>
        [Display(Name = "Người phản hồi")]
        public string? ResponseBy { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ContactViewModel()
        {
            Status = "Pending";
            CreatedDate = DateTime.Now;
        }
    }
}