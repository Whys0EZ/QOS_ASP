
using QOS.Models;
namespace QOS.Areas.Report.Models
{
    public class RP_PhotoDefectViewModel
    {
        public List<Unit_List> Unit_List { get; set; } = new List<Unit_List>();
        public string Report_Type {get; set;} = "Form1_BCCLC";
        public string Unit { get; set; } = "ALL";
        public string Mo { get; set;} = "";
      
        public string? Sewer { get; set; } ="";
        
        public DateTime DateFrom { get; set; } = DateTime.Now.AddDays(-1);
        public DateTime DateEnd { get; set; } = DateTime.Now;
        public List<Dictionary<string, object>> ReportData { get; set; } = new List<Dictionary<string, object>>();
        
    }
}