using QOS.Models;
namespace QOS.Areas.Report.Models
{
    public class TopForm4QualityViewModel
    {
        public List<Unit_List> Unit_List { get; set; } = new List<Unit_List>();
        
        public string Unit { get; set; } = "ALL";
        public string? Line { get; set; }
        
        public DateTime DateFrom { get; set; } = DateTime.Now.AddDays(-1);
        public DateTime DateEnd { get; set; } = DateTime.Now;
        public List<Dictionary<string, object>> ReportData { get; set; } = new List<Dictionary<string, object>>();
        // public Dictionary<string, DefectStat> DefectStats { get; set; } = new Dictionary<string, DefectStat>();
        public Dictionary<string, DefectCode_TopForm4> DefectCodes { get; set; } = new Dictionary<string, DefectCode_TopForm4>();
    }
    public class DefectCode_TopForm4
    {
        public string? Fault_Code {get; set;}
        public string? Fault_Name_VN {get; set;}
        public string? Fault_Name_EN {get; set;}
    }
}