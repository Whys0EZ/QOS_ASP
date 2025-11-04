namespace QOS.Areas.API.Models
{

    public class BCDTModel
    {
        // Fields cho cả GET và POST
        public string? ID { get; set; }
        public string? Report_ID { get; set; }
        public string? Unit { get; set; }
        public string? Cut_Leader { get; set; }
        public string? MO { get; set; }
        public string? Color { get; set; }
        public string? Roll { get; set; }
        public string? Lot { get; set; }
        public int? QTY { get; set; }
        public int? Layer_QTY { get; set; }
        public int? Cut_QTY { get; set; }
        public string? CutTableRatio { get; set; }
        public string? Cut_Table_Height { get; set; }
        public string? Cut_Table_Long { get; set; }
        
        // Các lỗi fabric
        public int? vai_ke { get; set; }
        public int? noi_vai { get; set; }
        public int? cang_vai { get; set; }
        public int? sai_mat { get; set; }
        public int? hep_kho { get; set; }
        public int? ban_vai { get; set; }
        public int? vi_tri { get; set; }
        public int? vai_nghieng { get; set; }
        public int? song_vai { get; set; }
        public int? thang_ke { get; set; }
        public int? khac_mau { get; set; }
        public int? quen_bam { get; set; }
        public int? bam_sau { get; set; }
        public int? xoc_xech { get; set; }
        public int? khong_cat { get; set; }
        public int? khong_gon { get; set; }
        public int? doi_ke { get; set; }
        public int? doi_xung { get; set; }
        public int? so_lop { get; set; }
        public int? so_btp { get; set; }
        
        public string? Size_Parameter_Cat { get; set; }
        public string? Size_Parameter_CPI { get; set; }
        public string? Size_Parameter_TP { get; set; }
        public string? Remark { get; set; }
        public bool Re_Audit { get; set; }
        public int Audit_Time { get; set; }
        public string? UserUpdate { get; set; }
        public string? Photo_URL { get; set; }

        // ===== Fields CHỈ cho GET (Response) =====
        public int No { get; set; }
        public string? Led { get; set; }
        public string? LastUpdate { get; set; }
        public string? cl_List { get; set; }
        public string? cl_Size { get; set; }
        public string? RowColor_Set { get; set; }
        public string? RowColor_V { get; set; }
        public string? RowClick_V { get; set; }

        // ===== Field CHỈ cho POST (Request) =====
        public string? Act { get; set; } // Insert/Update/Delete
    }


    public class BCDTSearchResponse
    {
        public List<BCDTModel> BCDT_Search { get; set; } = new();
    }
}