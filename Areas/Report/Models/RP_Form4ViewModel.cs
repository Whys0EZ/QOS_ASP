namespace QOS.Areas.Report.Models
{
    public class RP_Form4ViewModel
    {
        public string Unit { get; set; } = "ALL";
        public DateTime DateFrom { get; set; } = DateTime.Now.AddDays(-7);
        public DateTime DateEnd { get; set; } = DateTime.Now;
        public string? Message { get; set; }
        public List<QOS.Models.Unit_List> Unit_List { get; set; } = new();
        public List<ReportUnit> ReportUnits { get; set; } = new();
    }
    public class RP_Form4_UnitViewModel
    {
        public string Unit { get; set; } = "";
        public DateTime DateFrom { get; set; } = DateTime.Now.AddDays(-7);
        public DateTime DateEnd { get; set; } = DateTime.Now;
        public string? Message { get; set; }
        public List<QOS.Models.Unit_List> Unit_List { get; set; } = new();
        public List<string> ColumnHeaders { get; set; } = new();
        public List<LineDetailRow> LineDetails { get; set; } = new();
    }

    public class ReportUnit
    {
        public string Unit { get; set; } = "";
        public string LineLed { get; set; } = "";
        public List<LineData> Lines { get; set; } = new();
    }

    public class LineData
    {
        public string LineCode { get; set; } = "";
        public string ColorCode { get; set; } = "";
        public string CircleClass { get; set; } = "";
        public string StatusText { get; set; } = "";
    }
    public class LineDetailData
    {
        public string LineCode { get; set; } = "";
        public string RowClass { get; set; } = "";
        public Dictionary<string, ColumnValue> ColumnValues { get; set; } = new();
    }
    public class ColumnValue
    {
        public string Value { get; set; } = "";
        public string CircleClass { get; set; } = "";
        public string StatusText { get; set; } = "";
        public string DisplayColumn { get; set; } = "";
    }
    public class LineDetailRow
    {
        public string Line { get; set; } = "";
        public Dictionary<string, string?> StatusByStep { get; set; } = new Dictionary<string, string?>();
    }

    public class LineHistoryData
    {
        public string Report_ID { get; set; } = "";
        public string Operation_Name_VN { get; set; } = "";
        public string Sewer { get; set; } = "";
        public int Total_Fault_QTY { get; set; } = 0;
        public int QTY { get; set; } = 0;
        public string Status { get; set; } = "";
        public string Audit_Time { get; set; } = "";
        public string UserUpdate { get; set; } = "";
        public string FullName { get; set; } = "";
        public DateTime LastUpdate { get; set; } = DateTime.Now;
        public int RowNum { get; set; } = 0;
    }
}