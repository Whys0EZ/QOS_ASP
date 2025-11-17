using System.ComponentModel.DataAnnotations;

namespace QOS.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Please enter Username!")]
        // [Required(
        //     ErrorMessageResourceType = typeof(SharedResource),
        //     ErrorMessageResourceName = "UsernameRequired"
        // )]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Please enter Password!")]
        // [Required(
        //     ErrorMessageResourceType = typeof(SharedResource),
        //     ErrorMessageResourceName = "PasswordRequired"
        // )]
        public string Password { get; set; } = "";
        public bool RememberMe { get; set; }  // ← thêm dòng này
    }
}
