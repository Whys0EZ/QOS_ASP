using System.Text;
using System.Text.Json;
using QOS.Areas.API.Models;

namespace QOS.Areas.API.Helpers
{
    public static class EmailHelper
    {
        /// <summary>
        /// Send email via external API
        /// </summary>
        public static async Task<string> SendEmailApiAsync(
            string emailTo,
            string subject,
            string body,
            ILogger? logger = null)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                var requestData = new EmailApiRequest
                {
                    Email_v = emailTo,
                    Subject_v = subject,
                    Body_v = EncodeBase64(body)
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Add authorization header
                httpClient.DefaultRequestHeaders.Add("Authorization", "Authorization");

                // Call external API
                var response = await httpClient.PostAsync(
                    "https://care.crystal-regent.com.vn/webservice/SendEmail_API.php",
                    content);

                var responseContent = await response.Content.ReadAsStringAsync();
                logger?.LogInformation($"Email API raw response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    if (responseContent.TrimStart().StartsWith("{"))
                    {
                        var responseData = JsonSerializer.Deserialize<EmailApiResponse>(responseContent);
                        if (responseData?.error == "0")
                        {
                            logger?.LogInformation($"Email sent successfully to {emailTo}");
                            return responseContent;
                        }
                        else
                        {
                            logger?.LogWarning($"Email API returned error: {responseContent}");
                            return responseContent;
                        }
                    }
                    else
                    {
                        logger?.LogInformation($"Email API non-JSON response: {responseContent}");
                        return responseContent;
                    }
                }
                else
                {
                    logger?.LogError($"Email API returned status code: {response.StatusCode}");
                    return $"Error: HTTP {response.StatusCode}";
                }
            }
            catch (TaskCanceledException ex)
            {
                logger?.LogError(ex, "Email API timeout");
                return "Error: Request timeout";
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error sending email via API");
                return $"Error: Cannot connect to API - {ex.Message}";
            }
        }

        /// <summary>
        /// Build tracking email HTML body
        /// </summary>
        public static string BuildTrackingEmailBody(
            string approveName,
            string group,
            string so,
            string style,
            string po,
            string pro,
            string qty,
            string destination,
            string updateDate,
            string itemNo,
            string status,
            string remark)
        {
            var sb = new StringBuilder();
            
            sb.Append("<html>");
            
            sb.Append("<body>");

            sb.Append($"Dear <b>{approveName}</b>, you have new request need to acknowledge !<br/>");
            
            sb.Append("<table border='0' cellpadding='2' cellspacing='2'>");
            sb.Append("<tr><th colspan='2' align='left' style='background-color: #C4D563;border: 1px solid black;padding: 5px;'>Thông tin chung/ General Information</th></tr>");
            
            sb.Append($"<tr><td style='border: 1px solid black;padding: 5px;'>Job Group</td><td style='border: 1px solid black;padding: 5px;width:450px'>{group}</td></tr>");
            sb.Append($"<tr><td style='border: 1px solid black;padding: 5px;'>SO.</td><td style='border: 1px solid black;padding: 5px;'>{so}</td></tr>");
            sb.Append($"<tr><td style='border: 1px solid black;padding: 5px;'>Style</td><td style='border: 1px solid black;padding: 5px;'>{style}</td></tr>");
            sb.Append($"<tr><td style='border: 1px solid black;padding: 5px;'>PO</td><td style='border: 1px solid black;padding: 5px;'>{po}</td></tr>");
            sb.Append($"<tr><td style='border: 1px solid black;padding: 5px;'>Customer + Unit</td><td style='border: 1px solid black;padding: 5px;'>{pro}</td></tr>");
            sb.Append($"<tr><td style='border: 1px solid black;padding: 5px;'>Qty</td><td style='border: 1px solid black;padding: 5px;'>{qty}</td></tr>");
            sb.Append($"<tr><td style='border: 1px solid black;padding: 5px;'>Destination</td><td style='border: 1px solid black;padding: 5px;'>{destination}</td></tr>");
            sb.Append($"<tr><td style='border: 1px solid black;padding: 5px;'>Update New Del(AC) Date</td><td style='border: 1px solid black;padding: 5px;'>{updateDate}</td></tr>");
            sb.Append($"<tr><td style='border: 1px solid black;padding: 5px;'>Item No</td><td style='border: 1px solid black;padding: 5px;'>{itemNo}</td></tr>");
            sb.Append($"<tr><td style='border: 1px solid black;padding: 5px;'>Status</td><td style='border: 1px solid black;padding: 5px;'>{status}</td></tr>");
            sb.Append($"<tr><td style='border: 1px solid black;padding: 5px;'>Lý do/ Reason</td><td style='border: 1px solid black;padding: 5px;'>{remark}</td></tr>");
            
            sb.Append("</table><br/>");
            sb.Append("</body></html>");
            
            // return sb.ToString().TrimStart(' ', '\n', '\r', '\t', '>');
            return sb.ToString();
        }

        /// <summary>
        /// Encode string to Base64 (giống hàm enc() trong PHP)
        /// </summary>
        private static string EncodeBase64(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            byte[] bytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Decode Base64 string
        /// </summary>
        public static string DecodeBase64(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            try
            {
                byte[] bytes = Convert.FromBase64String(input);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return input;
            }
        }
    }
}