using System.ComponentModel.DataAnnotations;

namespace QOS.Models
{
    /// <summary>
    /// ViewModel for changing user password.
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        public required string Username { get; set; }

        [Required(ErrorMessage = "Mật khẩu cũ là bắt buộc")]
        [DataType(DataType.Password)]
        public required string OldPassword { get; set; }

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
        [StringLength(100, ErrorMessage = "{0} phải có ít nhất {2} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public required string NewPassword { get; set; }

        [Required(ErrorMessage = "Xác nhận mật khẩu mới là bắt buộc")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public required string ConfirmPassword { get; set; }
    }
}
