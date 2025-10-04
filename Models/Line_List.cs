using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace QOS.Models
{
    [Table("Line")]
    public class Line_List
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public required string Factory { get; set; }
        public string? Unit { get; set; }
        public string? Line { get; set; }
        public string? Act { get; set; }
       
    }
}