using System.ComponentModel.DataAnnotations;
namespace QOS.Models
{
    public class Team_List
    {
        [Key]
        public required string TeamID { get; set; }
        public string? Remark { get; set; }
        public required string TeamName { get; set; }
        public string? UserUpdate { get; set; }
        public DateTime? LastUpdate { get; set; }
    }
}