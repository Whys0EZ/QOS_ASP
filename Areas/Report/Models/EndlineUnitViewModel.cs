using QOS.Models;
namespace QOS.Areas.Report.Models
{
    public class EndlineUnitViewModel
    {
        public List<Unit_List> Unit_List { get; set; } = new List<Unit_List>();
        public List<Line_List> Line_List { get; set; } = new List<Line_List>();
        public List<string> Customer_List {get; set; } = new List<string>();
        public string? Customer { get; set;} ="ALL";
        public string Unit { get; set; } = "ALL";
        public string Line { get; set;} = "ALL";
        public string? Mo { get; set; } ="";
        public string? Color { get; set;} ="";
        public string? DefectedType {get; set;} = "ALL";
        
        public DateTime DateFrom { get; set; } = DateTime.Now.AddDays(-1);
        public DateTime DateEnd { get; set; } = DateTime.Now;
        public List<Dictionary<string, object>> ReportData { get; set; } = new List<Dictionary<string, object>>();
        public List<Dictionary<string, object>> ReportDataSewerImg { get; set; } = new List<Dictionary<string, object>>();
        // public Dictionary<string, DefectStat> DefectStats { get; set; } = new Dictionary<string, DefectStat>();
        public Dictionary<string, DefectCode_EndlineUnit> DefectCodes { get; set; } = new Dictionary<string, DefectCode_EndlineUnit>();
    }
    public class DefectCode_EndlineUnit
    {
        public string? Fault_Code {get; set;}
        public string? Fault_Name_VN {get; set;}
        public string? Fault_Name_EN {get; set;}
    }
}