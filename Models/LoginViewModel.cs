using System.ComponentModel.DataAnnotations;

namespace QOS.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Username!")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập Password!")]
        public string Password { get; set; } = "";
    }
}
