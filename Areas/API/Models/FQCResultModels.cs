namespace QOS.Areas.API.Models
{
    public class FQCResultModel
    {
        public string? Act { get; set; }
        
        // Basic Info
        public string? ModuleName { get; set; }
        public string? Customer { get; set; }
        public string? ID_Report { get; set; }
        public string? ID_Data { get; set; }
        public string? No_Carton { get; set; }
        
        // Order Info
        public string? SO { get; set; }
        public string? SizeName { get; set; }
        public string? mStyle { get; set; }  // Style từ POST
        public string? Item { get; set; }
        public string? PO { get; set; }
        public decimal Qty { get; set; }
        public string? AQL { get; set; }
        public string? Destination { get; set; }
        public string? Production { get; set; }
        public string? Operation { get; set; }
        
        // Audit Info
        public string? Remark { get; set; }
        public string? Update { get; set; }
        public string? shipMode { get; set; }
        public string? Remedies { get; set; }
        public string? Fault { get; set; }
        public int Audit_time { get; set; }
        public int Re_Audit { get; set; }
        public decimal Total_Fault_QTY { get; set; }
        
        // Additional Info
        public string? Industry { get; set; }
        public string? OQL { get; set; }
        public decimal Check_Qty { get; set; }
        
        // User Info
        public string? UserUpdate { get; set; }
        public string? CreatedBy { get; set; }
        public string? ResultStatus { get; set; }
    }
}