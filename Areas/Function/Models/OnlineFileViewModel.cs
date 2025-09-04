using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QOS.Areas.Function.Models
{

    public class OnlineFileViewModel
    {
        public List<OnlineFile>? OnlineFiles { get; set; }
        public List<OnlineFileGroup>? OnlineFileGroups { get; set; }
    }

    [Table("OnlineFiles")]
    public class OnlineFile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }
        public string? GroupID { get; set; }
        public string? DataName { get; set; }
        public string? DataRemark { get; set; }
        public string? DataLink { get; set; }
        public string? Checker { get; set; }
        public DateTime? CheckDate { get; set; }
        public string? CheckResult { get; set; }
        public string? CheckRemark { get; set; }
        public string? UserUpdate { get; set; }
        public DateTime? LastUpdate { get; set; }
    }

    [Table("OnlineFileGroup")]
    public class OnlineFileGroup
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string? GroupID { get; set; }
        public string? Remark { get; set; }
    }
}