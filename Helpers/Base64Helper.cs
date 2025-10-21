using System;
using System.Text;

namespace QOS.Helpers
{
    public static class Base64Helper
    {
        /// <summary>
        /// Mã hóa chuỗi sang Base64
        /// </summary>
        public static string Encode(string? plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainBytes);
        }

        /// <summary>
        /// Giải mã chuỗi Base64 về dạng gốc
        /// </summary>
        public static string Decode(string? base64Text)
        {
            if (string.IsNullOrEmpty(base64Text))
                return string.Empty;

            try
            {
                var base64Bytes = Convert.FromBase64String(base64Text);
                return Encoding.UTF8.GetString(base64Bytes);
            }
            catch (FormatException)
            {
                // Nếu chuỗi không phải Base64 hợp lệ
                return base64Text;
            }
        }
    }
}
