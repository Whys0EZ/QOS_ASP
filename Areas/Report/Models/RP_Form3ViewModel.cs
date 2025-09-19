namespace QOS.Areas.Report.Models
{
    public class RP_Form3ViewModel
    {
        public DateTime DateFrom { get; set; }
        public DateTime DateEnd { get; set; }
        public string? Unit { get; set; }
        public List<QOS.Models.Unit_List>? Unit_List { get; set; }

        // thêm property này để chứa dữ liệu từ Form2_BCCLC
        public List<Form3_BCDT>? History { get; set; }

    }

    public class Form3_BCDT
    {
        public int ID { get; set; }
        public string? Report_ID { get; set; }
        public string? Unit { get; set; }
        public string? Cut_Leader { get; set; }
        public string? MO { get; set; }
        public string? Color { get; set; }
        public string? Roll { get; set; }
        public string? Lot { get; set; }
        public int? QTY { get; set; }
        public int? Layer_QTY { get; set; }
        public string? CutTableRatio { get; set; }
        public int? Cut_QTY { get; set; }
        public string? Cut_Table_Height { get; set; }
        public string? Cut_Table_Long { get; set; }
        public bool? vai_ke { get; set; }
        public bool? noi_ke { get; set; }
        public bool? cang_vai { get; set; }
        public bool? sai_mat { get; set; }
        public bool? hep_kho { get; set; }
        public bool? ban_vai { get; set; }
        public bool? vi_tri { get; set; }
        public bool? vai_nghieng { get; set; }
        public bool? song_vai { get; set; }
        public bool? thang_ke { get; set; }
        public bool? khac_mau { get; set; }
        public bool? quen_bam { get; set; }
        public bool? bam_sau { get; set; }
        public bool? xoc_xech { get; set; }
        public bool? khong_cat { get; set; }
        public bool? khong_gon { get; set; }
        public bool? doi_ke { get; set; }
        public bool? doi_xung { get; set; }
        public bool? so_lop { get; set; }
        public bool? so_btp { get; set; }
        public string? Size_Parameter_Cat { get; set; }
        public string? Size_Parameter_CPI { get; set; }
        public string? Size_Parameter_TP { get; set; }
        
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