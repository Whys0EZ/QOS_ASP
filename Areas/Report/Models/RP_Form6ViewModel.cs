namespace QOS.Areas.Report.Models
{
    public class RP_Form6ViewModel
    {
        public string Unit { get; set; } = "ALL";
        public DateTime DateFrom { get; set; } = DateTime.Now;
        public DateTime DateEnd { get; set; } = DateTime.Now;
        public string? Message { get; set; }
        public List<QOS.Models.Unit_List> Unit_List { get; set; } = new();
        public List<Dictionary<string, object>> ReportData { get; set; }
        // Thêm 2 list để build chart
        public List<ChartPoint> DataPointsREG { get; set; } = new();
        public List<ChartPoint> DataPointsUnitTarget { get; set; } = new();
    }
    public class RP_Form6_UnitViewModel
    {
        public string Unit { get; set; } = "";
        public DateTime DateFrom { get; set; } = DateTime.Now.AddDays(-1);
        public DateTime DateEnd { get; set; } = DateTime.Now;
        public string? Message { get; set; }
        public List<QOS.Models.Unit_List> Unit_List { get; set; } = new();
       
    }
    public class ChartPoint
    {
        public string Label { get; set; } = "";
        public double Y { get; set; }
    }

    public class LineHistoryDataForm6
    {
        public int? ID {get; set;} =0;
        public string? Report_ID { get; set; } = "";
        public string? MO { get; set;}="";
        public string? Color { get; set;}="";
        public string? AQL { get; set; } = "";
        public int? Total_Fault_QTY { get; set; } = 0;
        public int? QTY { get; set; } = 0;
        public int? Check_QTY { get; set; } = 0;
        public string? Status { get; set; } = "";
        public double OQL  {get; set;}= 0;
        public string? Audit_Time { get; set; } = "";
        public string? UserUpdate { get; set; } = "";
        public string? FullName { get; set; } = "";
        public DateTime LastUpdate { get; set; } = DateTime.Now;
        public int? RowNum { get; set; } = 0;
    }

    public class Form6DetailViewModel
    {
        public Form6_Detail Detail { get; set; } = new Form6_Detail();
        public List<Form6FaultViewModel> Faults { get; set; } = new();
        public List<Form6SelectedFault> SelectedFaults { get; set; } = new();
        
    }

    public class Form6_Detail
    {
        public int ID { get; set;}
        public string? Report_ID { get; set;}
        public string? Unit { get; set;}
        public string? Line { get; set;}
        public string? Sewer { get; set;}
        public string? Sup {get; set;}
        // public string? Ast_Sup { get; set;}
        public string? MO { get; set;}
        public string? Color { get; set;}
        public string? Size {get; set;}
        public int? QTY { get; set;}
        public int? Check_QTY { get; set;}
        public string? AQL { get; set;}
        public double? OQL { get; set;}
        public int? Total_Fault_QTY { get; set;}
        public string? Fault_AQL_QTY {get; set;}
        public string? Fault_Detail {get; set;}
        public string? Remark {get; set;}
        public string? Led {get; set;}
        public bool? Re_Audit {get; set;}
        public int? Audit_Time {get; set;}
        public DateTime? LastUpdate {get; set;}
        // public string? Sewer_Workstation {get; set;}
        public string? Photo_URL {get; set;}
        public string? UserUpdate {get; set;}
        
        
    }
    public class Form6FaultViewModel
    {
        public string FaultCode { get; set; } = "";
        public string FaultNameVN { get; set; } = "";
        public int FaultLevel { get; set; }
    }
    public class Form6SelectedFault
    {
        public string FaultCode { get; set; } = "";
        public int FaultQty { get; set; }   // nếu trong chuỗi Fault_Detail có số lượng
    }
}