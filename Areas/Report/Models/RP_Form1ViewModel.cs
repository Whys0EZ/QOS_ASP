namespace QOS.Areas.Report.Models
{
    public class RP_Form1ViewModel
    {
        public DateTime DateFrom { get; set; }
        public DateTime DateEnd { get; set; }
        public string? Unit { get; set; }
        public List<QOS.Models.Unit_List>? Unit_List { get; set; }
    }
}