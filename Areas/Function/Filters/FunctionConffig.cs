using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;


namespace QOS.Areas.Function.Filters
{
    public static class FunctionConfig {
        public static string FractionText(string value)
        {
            int intValue = (int)Math.Floor(decimal.Parse(value));
            decimal fraction = decimal.Parse(value) - intValue;

            decimal numerator = Math.Round(fraction * 8);
            decimal denominator = 8;
            if (numerator > 0)
            {
                return $"{intValue} {numerator}/{denominator}";
            }
            else
            {
                return intValue.ToString();
            }
        }
        public static string str_replace(string search, string replace, string subject)
        {
            return subject.Replace(search, replace);
        }

        public static bool is_numeric(string value)
        {
            return double.TryParse(value, out _);
        }
        public static string trim(string value)
        {
            return value.Trim();
        }
        public static string to_upper(string value)
        {
            return value.ToUpper();
        }
        public static string to_lower(string value)
        {
            return value.ToLower();
        }
        public static string end(string value)
        {
            return value[^1].ToString();
        }

    }
}