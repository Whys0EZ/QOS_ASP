using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QOS.Areas.Function.Models
{
    [Table("TRACKING_Module")]
    public class TrackingSetup
    {
        [Key]
        public string ModuleName { get; set; } = string.Empty;

        public string? Remark { get; set; }
        public int? UpdateForm_StartRow { get; set; }
        public string? UserUpdate { get; set; }
        public DateTime? LastUpdate { get; set; }

        [NotMapped]
        public List<InforSetupDto> InforSetups { get; set; } = new();
        [NotMapped]
        public List<ResultSetupDto> ResultSetups { get; set; } = new();
    }
    public class TrackingSetupSaveDto
    {
        public string ModuleName { get; set; } = "";
        public string? Remark { get; set; }
        public int? UpdateForm_StartRow { get; set; }
        public List<InforSetupDto> InforSetups { get; set; } = new();
        public List<ResultSetupDto> ResultSetups { get; set; } = new();
    }
    public class InforSetupDto
    {
        public string? SqlColumn { get; set; }
        public string? Name { get; set; }
        public string? Index { get; set; }
        public string? DataType { get; set; }
        public string? Opt { get; set; }
        public string? Column { get; set; }
        public string? Remark { get; set; }
    }

    public class ResultSetupDto
    {
        public string? SqlColumn { get; set; }
        public string? Name { get; set; }
        public string? Index { get; set; }
        public string? DataType { get; set; }
        public string? SelectionData { get; set; }
        public string? Remark { get; set; }
    }
    [Table("TRACKING_InforSetup_Column")]
    public class TRACKING_InforSetup_Column
    {
        [Key]
        public required string ModuleName { get; set; }
        public string? Infor_01 { get; set; }
        public string? Infor_02 { get; set; }
        public string? Infor_03 { get; set; }
        public string? Infor_04 { get; set; }
        public string? Infor_05 { get; set; }
        public string? Infor_06 { get; set; }
        public string? Infor_07 { get; set; }
        public string? Infor_08 { get; set; }
        public string? Infor_09 { get; set; }
        public string? Infor_10 { get; set; }
        public string? Infor_11 { get; set; }
        public string? Infor_12 { get; set; }
        public string? Infor_13 { get; set; }
        public string? Infor_14 { get; set; }
        public string? Infor_15 { get; set; }
    }
    [Table("TRACKING_InforSetup_DataType")]
    public class TRACKING_InforSetup_DataType
    {
        [Key]
        public required string ModuleName { get; set; }
        public string? Infor_01 { get; set; }
        public string? Infor_02 { get; set; }
        public string? Infor_03 { get; set; }
        public string? Infor_04 { get; set; }
        public string? Infor_05 { get; set; }
        public string? Infor_06 { get; set; }
        public string? Infor_07 { get; set; }
        public string? Infor_08 { get; set; }
        public string? Infor_09 { get; set; }
        public string? Infor_10 { get; set; }
        public string? Infor_11 { get; set; }
        public string? Infor_12 { get; set; }
        public string? Infor_13 { get; set; }
        public string? Infor_14 { get; set; }
        public string? Infor_15 { get; set; }
    }
    [Table("TRACKING_InforSetup_Index")]
    public class TRACKING_InforSetup_Index
    {
        [Key]
        public required string ModuleName { get; set; }
        public string? Infor_01 { get; set; }
        public string? Infor_02 { get; set; }
        public string? Infor_03 { get; set; }
        public string? Infor_04 { get; set; }
        public string? Infor_05 { get; set; }
        public string? Infor_06 { get; set; }
        public string? Infor_07 { get; set; }
        public string? Infor_08 { get; set; }
        public string? Infor_09 { get; set; }
        public string? Infor_10 { get; set; }
        public string? Infor_11 { get; set; }
        public string? Infor_12 { get; set; }
        public string? Infor_13 { get; set; }
        public string? Infor_14 { get; set; }
        public string? Infor_15 { get; set; }
    }
    [Table("TRACKING_InforSetup_Name")]
    public class TRACKING_InforSetup_Name
    {
        [Key]
        public required string ModuleName { get; set; }
        public string? Infor_01 { get; set; }
        public string? Infor_02 { get; set; }
        public string? Infor_03 { get; set; }
        public string? Infor_04 { get; set; }
        public string? Infor_05 { get; set; }
        public string? Infor_06 { get; set; }
        public string? Infor_07 { get; set; }
        public string? Infor_08 { get; set; }
        public string? Infor_09 { get; set; }
        public string? Infor_10 { get; set; }
        public string? Infor_11 { get; set; }
        public string? Infor_12 { get; set; }
        public string? Infor_13 { get; set; }
        public string? Infor_14 { get; set; }
        public string? Infor_15 { get; set; }
    }
    [Table("TRACKING_InforSetup_Opt")]
    public class TRACKING_InforSetup_Opt
    {
        [Key]
        public required string ModuleName { get; set; }
        public string? Infor_01 { get; set; }
        public string? Infor_02 { get; set; }
        public string? Infor_03 { get; set; }
        public string? Infor_04 { get; set; }
        public string? Infor_05 { get; set; }
        public string? Infor_06 { get; set; }
        public string? Infor_07 { get; set; }
        public string? Infor_08 { get; set; }
        public string? Infor_09 { get; set; }
        public string? Infor_10 { get; set; }
        public string? Infor_11 { get; set; }
        public string? Infor_12 { get; set; }
        public string? Infor_13 { get; set; }
        public string? Infor_14 { get; set; }
        public string? Infor_15 { get; set; }
    }
    [Table("TRACKING_InforSetup_Remark")]
    public class TRACKING_InforSetup_Remark
    {
        [Key]
        public required string ModuleName { get; set; }
        public string? Infor_01 { get; set; }
        public string? Infor_02 { get; set; }
        public string? Infor_03 { get; set; }
        public string? Infor_04 { get; set; }
        public string? Infor_05 { get; set; }
        public string? Infor_06 { get; set; }
        public string? Infor_07 { get; set; }
        public string? Infor_08 { get; set; }
        public string? Infor_09 { get; set; }
        public string? Infor_10 { get; set; }
        public string? Infor_11 { get; set; }
        public string? Infor_12 { get; set; }
        public string? Infor_13 { get; set; }
        public string? Infor_14 { get; set; }
        public string? Infor_15 { get; set; }
    }
    [Table("TRACKINIG_ResultSetup_DataType")]
    public class TRACKING_ResultSetup_DataType
    {
        [Key]
        public required string ModuleName { get; set; }
        public string? Infor_01 { get; set; }
        public string? Infor_02 { get; set; }
        public string? Infor_03 { get; set; }
        public string? Infor_04 { get; set; }
        public string? Infor_05 { get; set; }
    }
    [Table("TRACKINIG_ResultSetup_Index")]
    public class TRACKING_ResultSetup_Index
    {
        [Key]
        public required string ModuleName { get; set; }
        public string? Infor_01 { get; set; }
        public string? Infor_02 { get; set; }
        public string? Infor_03 { get; set; }
        public string? Infor_04 { get; set; }
        public string? Infor_05 { get; set; }
    }

    [Table("TRACKINIG_ResultSetup_Name")]
    public class TRACKING_ResultSetup_Name
    {
        [Key]
        public required string ModuleName { get; set; }
        public string? Infor_01 { get; set; }
        public string? Infor_02 { get; set; }
        public string? Infor_03 { get; set; }
        public string? Infor_04 { get; set; }
        public string? Infor_05 { get; set; }
    }
    [Table("TRACKINIG_ResultSetup_Remark")]
    public class TRACKING_ResultSetup_Remark
    {
        [Key]
        public required string ModuleName { get; set; }
        public string? Infor_01 { get; set; }
        public string? Infor_02 { get; set; }
        public string? Infor_03 { get; set; }
        public string? Infor_04 { get; set; }
        public string? Infor_05 { get; set; }
    }
    [Table("TRACKINIG_ResultSetup_SelectionData")]
    public class TRACKING_ResultSetup_SelectionData
    {
        [Key]
        public required string ModuleName { get; set; }
        public string? Infor_01 { get; set; }
        public string? Infor_02 { get; set; }
        public string? Infor_03 { get; set; }
        public string? Infor_04 { get; set; }
        public string? Infor_05 { get; set; }
    }
}