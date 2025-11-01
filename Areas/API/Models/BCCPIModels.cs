namespace QOS.Areas.API.Models
{

    public class BCCPIModel
    {
        // Primary fields
        
        public string? ID { get; set; }
        public string? Report_ID { get; set; }
        public string? Unit { get; set; }
        public string? AQL { get; set;}
        public string? Cut_Leader { get; set; }
        public string? CPI_Leader { get; set; }
        public string? CPI { get; set; }
        // public string? Lay_Height { get; set; }
        // public string? Table_Long { get; set; }
        // public string? Table_Width { get; set; }
        public string? MO { get; set; }
        public string? Color { get; set; }
        public string? CutTableName { get; set; }
        // public string? CutTableRatio { get; set; }
        public string? Batch { get; set; }
        

        public int QTY { get; set; }
        public int Check_QTY { get; set; }
        public int Fault_AQL_QTY { get; set; }
        public int Fault_QTY { get; set; }
        public bool Passed { get; set; }

        public bool Hole { get; set; }
        public bool Shading { get; set; }
        public bool Yarn { get; set; }
        public bool Slub { get; set; }
        public bool Dirty { get; set; }
        
        // Dimension fields - Set 1
        public string? DS_L_Min { get; set; }
        public string? DS_L_Max { get; set; }
        public string? DS_W_Min { get; set; }
        public string? DS_W_Max { get; set; }
        
        // Dimension fields - Set 2
        // public string? DS_L_Min_2 { get; set; }
        // public string? DS_L_Max_2 { get; set; }
        // public string? DS_W_Min_2 { get; set; }
        // public string? DS_W_Max_2 { get; set; }
        
        public string? Size_Parameter { get; set; }
        
        // Quality fields
        public string? Notch { get; set; }
        public string? Straigh { get; set; }
        // public string? Straigh { get; set; }
        public string? Shape { get; set; }
        public string? Edge { get; set; }
        public string? Stripe { get; set; }
        public bool Match { get; set; }
        public bool Label { get; set; }
        
        public string? Remark { get; set; }
        public bool Re_Audit { get; set; }
        public DateTime? Audit_Time { get; set; }
        public string? User { get; set; }
        public string? User_Edit { get; set; }
        public string? Photo_URL { get; set; }
        public string? Rap { get; set;}
        public DateTime? Date_Edit { get; set;}

        // ===== Fields CHỈ cho GET (Response) =====
        public int No { get; set; }
        public string? Led { get; set; }
        public string? UserUpdate { get; set; }
        public DateTime? LastUpdate { get; set; }
        public string? Cut_Table { get;set;}
        public string? Number_Check { get;set;}
        public string? Re_Check { get;set;}
        public string? cl_List { get; set; }
        public string? cl_Size { get; set; }
        public string? RowColor_Set { get; set; }
        public string? RowColor_V { get; set; }
        public string? RowClick_V { get; set; }

        // ===== Field CHỈ cho POST (Request) =====
        public string? Act { get; set; } // Insert/Update/Delete
    }

    public class BCCPISearchResponse
    {
        public List<BCCPIModel> BCCPI_Search { get; set; } = new();
    }
}