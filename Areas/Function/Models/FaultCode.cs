using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QOS.Models
{
    [Table("Fault_Code")]
    public class FaultCode
    {
        [Key]
        [StringLength(50)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] 
        public required string Fault_Code { get; set; }
        [StringLength(50)]
        public string? Fault_Type { get; set; }
        [StringLength(1)]
        public string? Fault_Level { get; set; }
        [StringLength(100)]
        public string? Fault_Name_VN { get; set; }
        [StringLength(50)]
        public string? Fault_Name_EN { get; set; }
        // BIT NULL
        public bool? Form4_Active { get; set; }
        public bool? Form6_Active { get; set; }
        [StringLength(50)]
        public string? UserUpdate { get; set; }


        public DateTime? LastUpdate { get; set; }
        [StringLength(100)]
        public string? Factory { get; set; }
    }
}