using System;
using System.Data;
using System.Collections.Generic;
namespace QOS.Areas.Report.Models
{
    public class Form4QualityViewModel
    {
        public string Unit { get; set; } = "";
        public string Line {get; set;} = "";
        
        public DateTime DateFrom { get; set; } = DateTime.Now.AddDays(-1);
        public DateTime DateEnd { get; set; } = DateTime.Now;
       
        public List<QOS.Models.Unit_List> Unit_List { get; set; } = new();
        public List<Dictionary<string, object>> ReportData { get; set; } = new List<Dictionary<string, object>>();
        public Dictionary<string, DefectCode> DefectCodes { get; set; } = new Dictionary<string, DefectCode>();
    }
    public class DefectCode
    {
        public string? Fault_Code {get; set;}
        public string? Fault_Name {get; set;}
    }
}
