using System.ComponentModel.DataAnnotations;

namespace QOS.Models
{
    public class RegisterViewModel
    {
        [Required]
        public string Username { get; set; } = "";
        [Required]
        public string Password { get; set; } = "";
        [Required]
        [Compare("Password", ErrorMessage = "Mật khẩu nhập lại không khớp")]
        public string ConfirmPassword { get; set; } = "";
        public string? FullName { get; set; }
    }
}
