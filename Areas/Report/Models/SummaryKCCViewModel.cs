using QOS.Models;
namespace QOS.Areas.Report.Models
{
    public class SummaryKCCViewModel
    {
        public List<Unit_List> Unit_List { get; set; } = new List<Unit_List>();
        public string TopDefected { get; set; } = "6";
        public string TypeCode { get; set; } = "ALL";
        public string Unit { get; set; } = "ALL";
        public string? Line { get; set; }
        public string? Mo { get; set; }
        public string? StyleCode { get; set; }
        public DateTime DateFrom { get; set; } = DateTime.Now.AddDays(-1);
        public DateTime DateEnd { get; set; } = DateTime.Now;
        public List<Dictionary<string, object>> ReportData { get; set; } = new List<Dictionary<string, object>>();
        public Dictionary<string, DefectStat> DefectStats { get; set; } = new Dictionary<string, DefectStat>();
        public int TotalDefects { get; set; }
    }

    public class DefectStat
    {
        public string Name { get; set; } = "";
        public string Name_EN { get; set; } = "";
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }
}