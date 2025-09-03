using Microsoft.AspNetCore.Http;
using QOS.Areas.Function.Models;
using System.Collections.Generic;

namespace QOS.Helpers
{
    public static class TrackingHelper
    {
        public static List<InforSetupDto> BuildInforSetups(
            TRACKING_InforSetup_Column? col,
            TRACKING_InforSetup_Name? name,
            TRACKING_InforSetup_Index? index,
            TRACKING_InforSetup_DataType? type,
            TRACKING_InforSetup_Opt? opt,
            TRACKING_InforSetup_Remark? remark)
        {
            var list = new List<InforSetupDto>();
            for (int i = 1; i <= 15; i++)
            {
                list.Add(new InforSetupDto
                {
                    // No = i,
                    Column = col?.GetType().GetProperty($"Infor_{i:D2}")?.GetValue(col)?.ToString(),
                    Name = name?.GetType().GetProperty($"Infor_{i:D2}")?.GetValue(name)?.ToString(),
                    Index = index?.GetType().GetProperty($"Infor_{i:D2}")?.GetValue(index)?.ToString(),
                    DataType = type?.GetType().GetProperty($"Infor_{i:D2}")?.GetValue(type)?.ToString(),
                    Opt = opt?.GetType().GetProperty($"Infor_{i:D2}")?.GetValue(opt)?.ToString(),
                    Remark = remark?.GetType().GetProperty($"Infor_{i:D2}")?.GetValue(remark)?.ToString(),
                });
            }
            return list;
        }

        public static List<ResultSetupDto> BuildResultSetups(
            TRACKING_ResultSetup_Name? name,
            TRACKING_ResultSetup_Index? index,
            TRACKING_ResultSetup_DataType? type,
            TRACKING_ResultSetup_SelectionData? selectiondata,
            TRACKING_ResultSetup_Remark? remark)
        {
            var list1 = new List<ResultSetupDto>();
            for (int i = 1; i <= 5; i++)
            {
                list1.Add(new ResultSetupDto
                {
                    // No = i,
                    Name = name?.GetType().GetProperty($"Infor_{i:D2}")?.GetValue(name)?.ToString(),
                    Index = index?.GetType().GetProperty($"Infor_{i:D2}")?.GetValue(index)?.ToString(),
                    DataType = type?.GetType().GetProperty($"Infor_{i:D2}")?.GetValue(type)?.ToString(),
                    SelectionData = selectiondata?.GetType().GetProperty($"Infor_{i:D2}")?.GetValue(selectiondata)?.ToString(),
                    Remark = remark?.GetType().GetProperty($"Infor_{i:D2}")?.GetValue(remark)?.ToString(),
                });
            }
            return list1;
        }
    }
}