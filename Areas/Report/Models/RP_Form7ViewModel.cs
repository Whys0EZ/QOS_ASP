namespace QOS.Areas.Report.Models
{
    public class RP_Form7ViewModel
    {
        public string Unit { get; set; } = "2U01";
        public DateTime DateFrom { get; set; } = DateTime.Now.AddDays(-7);
        public DateTime DateEnd { get; set; } = DateTime.Now;
        public string? Message { get; set; }
        public string? SearchMo { get; set; }
        public List<QOS.Models.Unit_List> Unit_List { get; set; } = new();
        public List<ReportUnit> ReportUnits { get; set; } = new();
    }
}