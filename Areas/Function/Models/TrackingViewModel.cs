using QOS.Areas.Function.Models;

public class TrackingViewModel
{
    public TrackingSetup Module { get; set; } = new TrackingSetup();

    // Infor Setup
    public string[]? Infor_Column { get; set; }
    public string[]? Infor_DataType { get; set; }
    public string[]? Infor_Index { get; set; }
    public string[]? Infor_Name { get; set; }
    public string[]? Infor_Opt { get; set; }
    public string[]? Infor_Remark { get; set; }

    // Result Setup
    public string[]? Result_DataType { get; set; }
    public string[]? Result_Index { get; set; }
    public string[]? Result_Name { get; set; }
    public string[]? Result_Remark { get; set; }
    public string[]? Result_SelectionData { get; set; }
}
