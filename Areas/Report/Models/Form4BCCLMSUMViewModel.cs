using System;
using System.Data;
using System.Collections.Generic;
namespace QOS.Areas.Report.Models
{
    public class Form4BCCLMSUMViewModel
    {
        public string Unit { get; set; } = "";
        public DateTime DateFrom { get; set; } = DateTime.Now.AddDays(-1);
        public DateTime DateEnd { get; set; } = DateTime.Now;
        public string? Message { get; set; }
        public List<QOS.Models.Unit_List> Unit_List { get; set; } = new();
        public List<string> ColumnHeaders { get; set; } = new();
        public DataTable? DynamicTable { get; set; }

    }
    public class Report_SUM 
    {
        public string? Line {get; set;}
        
    }

}