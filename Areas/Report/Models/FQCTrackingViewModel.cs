using QOS.Models;
namespace QOS.Areas.Report.Models
{
    public class FQCTrackingViewModel 
    {
        public List<string> Customer_List { get; set; } = new List<string>();
        public string Customer { get; set;} ="ALL";
        public string? Industry {get; set;} ="ALL";
        public string? Operation {get; set;} ="ALL";
        public string? Searching { get; set; } ="";
        public DateTime DateFrom { get; set; } = DateTime.Now.AddDays(-1);
        public DateTime DateEnd { get; set; } = DateTime.Now;
        public List<Dictionary<string, object>> ReportData { get; set; } = new List<Dictionary<string, object>>();
    }

     public class FQCTrackingDetailViewModel
    {
        public FQCTracking_Detail Detail { get; set; } = new FQCTracking_Detail();
        public List<FQCTrackingFaultViewModel> Faults { get; set; } = new();
        public List<FQCTrackingSelectedFault> SelectedFaults { get; set; } = new();
        public List<FQCOperationViewModel> Operations { get; set; } = new();
        public List<FQCOperationSelected> SelectedOperations { get; set; } = new();
        
    }

    public class FQCTracking_Detail
    {
        public string? ID_Result { get; set;}
        // public string? Customer { get; set;}
        public DateTime? WorkDate { get; set;}
        // public string? ModuleName { get; set;}
        public string? ID_Report { get; set;}
        // public string? ID_Data {get; set;}
        public string? ResultStatus { get; set;}
        public string? IMG_Result { get; set;}
        public string? Size_Name { get; set;}
        public string? SO {get; set;}
        public string? Style {get; set;}
        public string? Item_No {get; set;}
        public string? PO {get; set;}
        public string? Unit {get; set;}
        public string? Operation {get; set;}
        public string? Update_Date {get; set;}
        public string? shipMode {get; set;}
        public int? QTY { get; set;}
        public int? Check_QTY { get; set;}

        public double? OQL { get; set;}
        public int? Total_Fault_QTY { get; set;}
  
        public string? Fault {get; set;}
        public string? Destination {get; set;}
        public string? Remark {get; set;} ="";
        public string? CreatedBy {get; set;}
        public bool? Re_Audit {get; set;}
        public int? Audit_Time {get; set;}
        public DateTime? LastUpdate {get; set;}
        // public string? Sewer_Workstation {get; set;}
        // public string? Photo_URL {get; set;}
        public string? UserUpdate {get; set;}
        
        
        
    }
    public class FQCTrackingFaultViewModel
    {
        public string FaultCode { get; set; } = "";
        public string FaultNameVN { get; set; } = "";
        public int FaultLevel { get; set; }
    }
    public class FQCTrackingSelectedFault
    {
        public string FaultCode { get; set; } = "";
        public int FaultQty { get; set; }   // nếu trong chuỗi Fault_Detail có số lượng
    }
    public class FQCOperationSelected
    {
        public string OperationCode { get; set; } = "";
        public int OperationQty { get; set; }   // nếu trong chuỗi Fault_Detail có số lượng
    }
    public class FQCOperationViewModel
    {
        public string OperationCode { get; set; } = "";
        public string OperationNameVN { get; set; } = "";
        public string OperationNameEN { get; set; } = "";
    }


}