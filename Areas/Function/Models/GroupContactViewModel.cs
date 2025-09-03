using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QOS.Areas.Function.Models
{
    [Table("TRACKING_GroupContactList")]
    public class GroupContactList
    {
        public int? STT { get; set; }
        public string? ModuleName { get; set; }
        [Key]
        public required string GroupID { get; set; }
        public string? GroupName { get; set; }
        public string? ContactList { get; set; }
        public string? Remark { get; set; }
        public string? UserUpdate { get; set; }
        public DateTime? LastUpdate { get; set; }
    }
}