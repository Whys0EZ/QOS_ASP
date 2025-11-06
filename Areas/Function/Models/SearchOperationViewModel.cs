using QOS.Areas.Function.Models;

namespace QOS.Areas.Function.Models
{
    public class SearchOperationViewModel
    {
        public string? MO { get; set; } ="";
        public List<ManageOperation> Results { get; set; } = new();
    }
}