namespace QOS.Areas.Report.Models
{
    public class RP_Form2ViewModel
    {
        public DateTime DateFrom { get; set; }
        public DateTime DateEnd { get; set; }
        public string? Unit { get; set; }
        public List<QOS.Models.Unit_List>? Unit_List { get; set; }

        // thêm property này để chứa dữ liệu từ Form2_BCCLC
        public List<Form2_BCCPI>? History { get; set; }

    }
    public class Form2_BCCPI
    {
        public int ID { get; set; }
        public string? Report_ID { get; set; }
        public string? Unit { get; set; }
        public string? AQL { get; set; }
        public string? Cut_Leader { get; set; }
        public string? CPI_Leader { get; set; }
        public string? CPI { get; set; }
        public string? Rap { get; set; }
        public string? CutTableName { get; set; }
        public string? Batch { get; set; }
        public string? MO { get; set; }
        public string? Color { get; set; }
        public int? QTY { get; set; }
        public int? Check_QTY { get; set; }
        public int? Fault_AQL_QTY { get; set; }
        public int? Fault_QTY { get; set; }
        public bool? Passed { get; set; }
        public bool? Hole { get; set; }
        public bool? Shading { get; set; }
        public bool? Yarn { get; set; }
        public bool? Slub { get; set; }
        public bool? Dirty { get; set; }
        public string? Notch { get; set; }
        public string? Straigh { get; set; }
        public string? Shape { get; set; }
        public string? Edge { get; set; }
        public string? Stripe { get; set; }
        public bool? Match { get; set; }
        public bool? Label { get; set; }
        public string? DS_L_Min { get; set; }
        public string? DS_L_Max { get; set; }
        public string? DS_W_Min { get; set; }
        public string? DS_W_Max { get; set; }
        public string? Size_Parameter { get; set; }
        
        public string? Remark { get; set; }
        public bool? Re_Audit { get; set; }
        public DateTime? Re_Audit_Time { get; set; }
        public int? Audit_Time { get; set; }
        public string? UserUpdate { get; set; }
        public DateTime? LastUpdate { get; set; }
        public string? Photo_URL { get; set; }

        // Extra column từ JOIN (User_List)
        public string? FullName { get; set; }
    }
}