using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;

namespace QOS.TagHelpers
{
    [HtmlTargetElement("a", Attributes = "asp-controller, asp-action, asp-active")]
    public class ActiveTagHelper : TagHelper
    {
        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext? ViewContext { get; set; }

        public string? AspController { get; set; }
        public string? AspAction { get; set; }
        public bool AspActive { get; set; } = true;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var currentCtrl = ViewContext?.RouteData.Values["controller"]?.ToString();
            var currentAct = ViewContext?.RouteData.Values["action"]?.ToString();

            if (AspActive &&
    string.Equals(currentCtrl, AspController, StringComparison.OrdinalIgnoreCase) &&
    (string.IsNullOrEmpty(AspAction) || string.Equals(currentAct, AspAction, StringComparison.OrdinalIgnoreCase)))
            {
                var existingClass = output.Attributes.ContainsName("class")
                    ? output.Attributes["class"].Value.ToString()
                    : "";

                output.Attributes.SetAttribute("class", $"{existingClass} active".Trim());
            }
        }
    }
}
