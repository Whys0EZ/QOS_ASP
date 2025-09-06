namespace QOS.Areas.Report.Models
{
    public class RP_Form1ViewModel
    {
        public DateTime DateFrom { get; set; }
        public DateTime DateEnd { get; set; }
        public string? Unit { get; set; }
        public List<QOS.Models.Unit_List>? Unit_List { get; set; }

        // thêm property này để chứa dữ liệu từ Form1_BCCLC
        public List<Form1_BCCLC>? History { get; set; }

    }
    public class Form1_BCCLC
    {
        public int ID { get; set; }
        public string? Report_ID { get; set; }
        public string? Unit { get; set; }
        public string? Cut_Leader { get; set; }
        public string? CutTableName { get; set; }
        public string? Lay_Height { get; set; }
        public string? Table_Long { get; set; }
        public string? Table_Width { get; set; }
        public string? CutTableRatio { get; set; }
        public string? Cut_Lot { get; set; }
        public string? MO { get; set; }
        public string? Color { get; set; }
        public string? Batch { get; set; }
        public int? Cut_QTY { get; set; }
        public bool? Shading { get; set; }
        public bool? Wave { get; set; }
        public bool? Narrow_Width { get; set; }
        public bool? Spreading { get; set; }
        public string? DS_L_Min { get; set; }
        public string? DS_L_Max { get; set; }
        public string? DS_W_Min { get; set; }
        public string? DS_W_Max { get; set; }
        public string? Size_Parameter { get; set; }
        public string? Notch { get; set; }
        public string? Unclean { get; set; }
        public string? Straigh { get; set; }
        public string? Shape { get; set; }
        public string? Edge { get; set; }
        public string? Stripe { get; set; }
        public string? Remark { get; set; }
        public bool? Re_Audit { get; set; }
        public DateTime? Re_Audit_Time { get; set; }
        public int? Audit_Time { get; set; }
        public string? UserUpdate { get; set; }
        public DateTime? LastUpdate { get; set; }
        public string? Photo_URL { get; set; }
        public string? Rap { get; set; }
        public string? User_Edit { get; set; }
        public DateTime? Date_Edit { get; set; }

        // Extra column từ JOIN (User_List)
        public string? FullName { get; set; }
    }
}