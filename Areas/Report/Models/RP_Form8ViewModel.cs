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
}