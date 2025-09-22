using System.ComponentModel.DataAnnotations;

namespace QOS.Models
{
    public class AuthViewModel
    {
        public LoginViewModel Login { get; set; } = new LoginViewModel();
        public UserEditViewModel Register { get; set; } = new UserEditViewModel();
    }
}