using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace QOS.Models
{
    public class UserEditViewModel
    {
        // Dropdown data
        public IEnumerable<SelectListItem> FactoryOptions { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> TeamOptions { get; set; } = new List<SelectListItem>();
        // Thông tin từ User_List
        public int Id { get; set; }
        public string FactoryID { get; set; } = "";
        public string Username { get; set; } = "";
        public string? TeamID { get; set; } = "";
        
        public string Pass { get; set; } = "";
        [DataType(DataType.Password)]
        [Compare("Pass", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = "";
        public string? FullName { get; set; }
        public string? Email { get; set; } = "";
        public bool Act { get; set; }

        public int UserLevel { get; set; }

        public string Fac_per { get; set; } = "";
        public int LoginLevel { get; set; } = 0;

        public string? UserUpdate { get; set; } = "";

        public DateTime LastUpdate { get; set; } = DateTime.Now;

        public string? Unit_Check { get; set; } = "";

        public string? Line_Check { get; set; } = "";

        // Thông tin từ User_Per
        public bool SYS_Admin { get; set; }
        public bool A_F1 { get; set; }
        public bool A_F2 { get; set; }
        public bool? A_F3 { get; set; }
        public bool? A_F4 { get; set; }
        public bool? A_F5 { get; set; }
        public bool? A_F6 { get; set; }
        public bool? A_F7 { get; set; }
        public bool? A_F8 { get; set; }
        public bool? A_F9 { get; set; }
        public bool? A_F10 { get; set; }

        public bool B_F0 { get; set; }
        public bool B_F01 { get; set; }
        public bool B_F1 { get; set; }
        public bool B_F2 { get; set; }
        public bool B_F3 { get; set; }
        public bool B_F4 { get; set; }
        public bool B_F5 { get; set; }
        public bool B_F6 { get; set; }
        public bool? B_F7 { get; set; }
        public bool? B_F8 { get; set; }
        public bool? B_F9 { get; set; }

        public bool C_F1 { get; set; }
        public bool C_F2 { get; set; }
        public bool C_F3 { get; set; }

        public bool S_F1 { get; set; }
        public bool S_F2 { get; set; }

        public bool Q_F0 { get; set; }
        public bool Q_F1 { get; set; }
        public bool Q_F2 { get; set; }
        public bool Q_F3 { get; set; }
        public bool Q_F4 { get; set; }
        public bool Q_F5 { get; set; }
        public bool Q_F6 { get; set; }
        public bool Q_F7 { get; set; }
        public bool Q_F8 { get; set; }
        public bool? Q_F9 { get; set; }
        public bool SYS_LED { get; set; }

 
    }
}