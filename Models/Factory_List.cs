using System.ComponentModel.DataAnnotations.Schema;

namespace QOS.Models
{
    public class Factory_List
    {
        public required string FactoryID { get; set; }
        public required string FactoryName { get; set; }

        public string? Remark { get; set; }
        public string? UserUpdate { get; set; }

        public DateTime LastUpdate { get; set; } = DateTime.Now;
        public DateTime? Effect_Date_From { get; set; } = null;
        public DateTime? Effect_Date_To { get; set; } = null;
        public string? Color { get; set; } = null;
        public string Comp_Add { get; set; } = "ABC";
        public string Comp_Name { get; set; } = "ABC";
        public string? Bank_Inf_TT { get; set; } = null;

    }
}