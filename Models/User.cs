using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;

namespace QOS.Models
{
    [Table("User_List")]
    public class User
    {
        
        public int Id { get; set; }
        [Required, MaxLength(10)]
        public string FactoryID { get; set; } = "";

        [Required, MaxLength(50)]
        public string Username { get; set; } = "";
        public string? TeamID { get; set; } = "";

        [Required]
        public string Pass { get; set; } = "";

        [MaxLength(100)]
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
    }
}
