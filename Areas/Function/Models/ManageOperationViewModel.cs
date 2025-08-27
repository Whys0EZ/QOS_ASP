using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QOS.Areas.Function.Models
{
    [Table("Operation_Code")]
    public class ManageOperation
    {
        // Các thuộc tính của model
        [Key]
        public int Id { get; set; }

        [MaxLength(100)]
        public required string MO { get; set; }

        [MaxLength(100)]
        public string? Operation_Code { get; set; }
        [MaxLength(300)]
        public string? Operation_Name_VN { get; set; }

        [MaxLength(300)]
        public string? Operation_Name_EN { get; set; }

        public bool? Form4_Active { get; set; }
        [MaxLength(50)]
        public string? UserUpdate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LastUpdate { get; set; }

        [MaxLength(10)]
        public string? CMD { get; set; }

    }
}