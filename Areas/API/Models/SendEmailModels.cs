namespace QOS.Areas.API.Models
{
    public class SendEmailRequest
    {
        public string? Email { get; set; }
        public string? Solution { get; set; }  // Group
        public string? Infor_01 { get; set; }  // SO
        public string? Infor_02 { get; set; }  // Style
        public string? Infor_04 { get; set; }  // PO
        public string? Status { get; set; }
        public string? Remark { get; set; }
        public string? Pro { get; set; }
        public string? Qty { get; set; }
        public string? Destination { get; set; }
        public string? Item_No { get; set; }
        public string? Update { get; set; }
    }

    public class EmailApiRequest
    {
        public string Email_v { get; set; } = "";
        public string Subject_v { get; set; } = "";
        public string Body_v { get; set; } = "";
    }

    public class EmailApiResponse
    {
        public string error { get; set; } = "";
        public string message { get; set; } = "";
    }
}