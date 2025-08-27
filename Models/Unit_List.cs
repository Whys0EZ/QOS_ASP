using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace QOS.Models
{
    [Table("Unit")]
    public class Unit_List
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public required string Factory { get; set; }
        public string? Zone { get; set; }
        public string? Block { get; set; }
        public string? Unit { get; set; }
        public string? Act { get; set; }
        public DateTime? Effect_Date_From { get; set; }
        public string? ETS_SUB { get; set; }
    }
}