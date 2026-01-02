using QOS.Models;
namespace QOS.Areas.Report.Models
{
	public class OQLEndLineViewModel
	{
		public string SelectedUnit { get; set; } = "1U01";
        public int SelectedMonth { get; set; } = DateTime.Now.Month;
        public int SelectedYear { get; set; } = DateTime.Now.Year;
        public List<OQLLineData> Lines { get; set; } = new List<OQLLineData>();
        public List<double?> AverageValues { get; set; } = new List<double?>();
        public List<Unit_List> DistinctUnits { get; set; } = new List<Unit_List>();
        public List<string> Zone { get; set; } = new List<string>();
        }
    public class OQL_EndLine
    {
        public DateTime Work_Date { get; set; }
        public string? Unit { get; set; }
        public string? Line { get; set; }
        public int Check_QTY { get; set; }
        public int Fault_QTY { get; set; }
        public double OQL { get; set; }
        public string? Led { get; set; }
        public float OQL_Target { get; set; }
    }
    public class OQLLineData
    {
        public string? LineName { get; set; }
        public List<double?> DailyValues { get; set; } = new List<double?>();
    }
}