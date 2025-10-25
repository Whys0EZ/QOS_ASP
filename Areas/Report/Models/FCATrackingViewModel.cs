using QOS.Models;
namespace QOS.Areas.Report.Models
{
    public class FCATrackingViewModel 
    {
        public List<string> Customer_List { get; set; } = new List<string>();
        public string Customer { get; set;} ="ALL";
        public string? Searching { get; set; } ="";
        public DateTime DateFrom { get; set; } = DateTime.Now.AddDays(-1);
        public DateTime DateEnd { get; set; } = DateTime.Now;
        public List<Dictionary<string, object>> ReportData { get; set; } = new List<Dictionary<string, object>>();
    }
}