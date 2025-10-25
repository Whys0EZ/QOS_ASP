using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QOS.Models
{
    [Table("User_Per")]
    public class UserPermission
    {
        // public int ID { get; set; }
        // [Required, MaxLength(10)]
        public required string FactoryID { get; set; }
        [Required, MaxLength(20)]
        public required string UserName { get; set; }
        public bool SYS_Admin { get; set; } = false;
        [MaxLength(20)]
        public string? UserUpdate { get; set; }
        public DateTime LastUpdate { get; set; }

        public bool A_F1 { get; set; } = false;
        public bool A_F2 { get; set; } = false;
        public bool? A_F3 { get; set; } = false;
        public bool? A_F4 { get; set; } = false;
        public bool? A_F5 { get; set; } = false;
        public bool? A_F6 { get; set; } = false;
        public bool? A_F7 { get; set; } = false;
        public bool? A_F8 { get; set; } = false;
        public bool? A_F9 { get; set; } = false;
        public bool? A_F10 { get; set; } = false;

        public bool B_F0 { get; set; } = false;
        public bool B_F01 { get; set; } = false;
        public bool B_F1 { get; set; } = false;
        public bool B_F2 { get; set; } = false;
        public bool B_F3 { get; set; } = false;
        public bool B_F4 { get; set; } = false;
        public bool B_F5 { get; set; } = false;
        public bool B_F6 { get; set; } = false;
        public bool? B_F7 { get; set; } = false;
        public bool? B_F8 { get; set; } = false;
        public bool? B_F9 { get; set; } = false;

        public bool C_F1 { get; set; } = false;
        public bool C_F2 { get; set; } = false;
        public bool C_F3 { get; set; } = false;

        public bool S_F1 { get; set; } = false;
        public bool S_F2 { get; set; } = false;

        public bool Q_F0 { get; set; } = false;
        public bool Q_F1 { get; set; } = false;
        public bool Q_F2 { get; set; } = false;
        public bool Q_F3 { get; set; } = false;
        public bool Q_F4 { get; set; } = false;
        public bool Q_F5 { get; set; } = false;
        public bool Q_F6 { get; set; } = false;
        public bool Q_F7 { get; set; } = false;
        public bool Q_F8 { get; set; } = false;
        public bool? Q_F9 { get; set; } = false;
        public bool SYS_LED { get; set; } = false;
    }
}