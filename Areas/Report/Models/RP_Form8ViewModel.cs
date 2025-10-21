using System;
using System.Data;
using System.Collections.Generic;
namespace QOS.Areas.Report.Models
{
    public class Form8TPViewModel
    {
        public string Unit { get; set; } = "";
        public int Page_No {get; set;} = 1;
        public float Row_Page { get; set;}= 30;
        public DateTime DateFrom { get; set; } = DateTime.Now.AddDays(-1);
        public DateTime DateEnd { get; set; } = DateTime.Now;
        public string? Search { get; set; }
        public List<QOS.Models.Unit_List> Unit_List { get; set; } = new();
        public List<Dictionary<string, object>> ReportData { get; set; } = new List<Dictionary<string, object>>();
        // public List<string> ColumnHeaders { get; set; } = new();
        // public DataTable DynamicTable { get; set; }

    }
   
    public class Form8TPDetailViewModel 
    {
        public string WorkDate { get; set; } = "";
        public string FactoryID  { get; set; } = "";
        public string TypeName  { get; set; } = "";
        public string WorkstageName  { get; set; } = "";
        public string LINE_No  { get; set; } = "";
        public string StyleCode  { get; set; } = "";
        public string MO  { get; set; } = "";
        public string ColorCode  { get; set; } = "";
        public string Item  { get; set; } = "";
        public string Pattern  { get; set; } = "";
        public string BatchCode  { get; set; } = "";
        public string TableCode  { get; set; } = "";
        
        public string? Customer { get; set; }
        public string? Sample_Type { get; set; }
        public string? Sample_color { get; set; }
        public string? Season { get; set; }
        public string? Board { get; set; }
        public string? Dev_Style_Name { get; set; }
        public string? Category { get; set; }
        public string? Development_Size_Range { get; set; }
        public string? Fit_Intent { get; set; }
        public string? Grade_Rule_Template { get; set; }
        public string? Img { get; set; }

        public List<Dictionary<string, object>> MeasurementData { get; set; }

        public List<string> ColumnNames { get; set; }

        public List<SizeColumn> SizeColumns { get; set; }

        public Form8TPDetailViewModel()
        {
            MeasurementData = new List<Dictionary<string, object>>();
            ColumnNames = new List<string>();
            SizeColumns = new List<SizeColumn>();
        }

        /// <summary>
        /// Parse SizeList string into SizeColumn objects
        /// Format: "count_sizeName;count_sizeName"
        /// Example: "3_XS;3_S;3_M"
        /// </summary>
        public void ParseSizeList(string sizeListStr)
        {
            if (string.IsNullOrEmpty(sizeListStr))
                return;

            var sizes = sizeListStr.Split(';');
            foreach (var size in sizes)
            {
                if (!string.IsNullOrEmpty(size))
                {
                    var parts = size.Split('_');
                    if (parts.Length >= 2)
                    {
                        SizeColumns.Add(new SizeColumn
                        {
                            Count = int.TryParse(parts[0], out int count) ? count : 0,
                            Name = parts[1]
                        });
                    }
                }
            }
        }

        public void ParseColumnNames(string clNameStr)
        {
            if (string.IsNullOrEmpty(clNameStr))
                return;

            var columns = clNameStr.Split(';');
            foreach (var col in columns)
            {
                if (!string.IsNullOrEmpty(col))
                {
                    ColumnNames.Add(col);
                }
            }
        }
    }
    public class SizeColumn
    {
        /// <summary>
        /// Size name (e.g., XS, S, M, L, XL)
        /// </summary>
        public string? Name { get; set; }
        
        /// <summary>
        /// Number of measurements for this size
        /// </summary>
        public int? Count { get; set; }
        
        /// <summary>
        /// Column type: S (Before Iron), T (After Iron), H (After Iron H)
        /// </summary>
        public string? Type { get; set; }
        
        /// <summary>
        /// Is this a base column (_1_S or _1_T)
        /// </summary>
        public bool? IsBase { get; set; }
    }
    public class MeasurementRow
    {
        public string? POM { get; set; }
        public string? Item_Name { get; set; }
        public string? Criticality { get; set; }
        public string? TOL { get; set; }
        
        /// <summary>
        /// Dictionary of dynamic measurement values
        /// Key: Column name (e.g., "XS_1_S", "S_2_T")
        /// Value: "baseValue__measurementValue__status" (e.g., "10__9.8__P" or "10__11.2__R")
        /// </summary>
        public Dictionary<string, string> MeasurementValues { get; set; }

        public MeasurementRow()
        {
            MeasurementValues = new Dictionary<string, string>();
        }
    }


        /// <summary>
    /// ViewModel for Form8TP Ticket/Label page
    /// </summary>
    public class Form8TPTicketViewModel
    {
        #region ID Parameters
        
        public string? WorkDate { get; set; }
        public string? FactoryID { get; set; }
        public string? TypeName { get; set; }
        public string? WorkstageName { get; set; }
        public string? LINE_No { get; set; }
        public string? StyleCode { get; set; }
        public string? MO { get; set; }
        public string? ColorCode { get; set; }
        public string? Item { get; set; }
        public string? Pattern { get; set; }
        public string? BatchCode { get; set; }
        public string? TableCode { get; set; }
        public string? SizeList { get; set; }
        
        #endregion

        /// <summary>
        /// List of ticket groups - mỗi group cho 1 size
        /// </summary>
        public List<TicketGroup> TicketGroups { get; set; }

        public Form8TPTicketViewModel()
        {
            TicketGroups = new List<TicketGroup>();
        }
    }

    /// <summary>
    /// Represents a ticket for one size
    /// </summary>
    public class TicketGroup
    {
        /// <summary>
        /// Size name (e.g., "XS", "S_1_S", "M_2_T")
        /// </summary>
        public string? SizeName { get; set; }
        
        /// <summary>
        /// Size code (first part before underscore)
        /// </summary>
        public string? SizeCode { get; set; }
        
        /// <summary>
        /// Style code from query result
        /// </summary>
        public string? StyleCode { get; set; }
        
        /// <summary>
        /// List of measurement items for this size
        /// </summary>
        public List<MeasurementItem> Measurements { get; set; }

        public TicketGroup()
        {
            Measurements = new List<MeasurementItem>();
        }
    }

    /// <summary>
    /// Represents a single measurement item
    /// </summary>
    public class MeasurementItem
    {
        /// <summary>
        /// Item ID (POM number)
        /// </summary>
        public string? ItemID { get; set; }
        
        /// <summary>
        /// Item name (measurement point name)
        /// </summary>
        public string? Item_Name { get; set; }
        
        /// <summary>
        /// Base value (có thể là phân số, hỗn số, hoặc số thập phân)
        /// </summary>
        public string? Base_Value { get; set; }
        
        /// <summary>
        /// Sum value after calculation (Base_Value + ActValue)
        /// </summary>
        public string? Sum_Value { get; set; }
    }
}