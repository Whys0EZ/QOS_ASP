using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using System.Data;
public class Functions()
{

    public static string Cut_Zero(string a)
    {
        if (string.IsNullOrEmpty(a))
            return "";

        // Bỏ các ký tự '0' ở đầu chuỗi
        int i = 0;
        while (i < a.Length && a[i] == '0')
            i++;

        // Nếu toàn 0, trả "0" thay vì rỗng
        return i == a.Length ? "0" : a.Substring(i);
    }
    public static string MD5Hash(string input)
    {
        using var md5 = MD5.Create();
        byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }
    public static string GetStringValue(SqlDataReader dr, string columnName)
    {
        try
        {
            return dr[columnName]?.ToString() ?? "";
        }
        catch
        {
            return "";
        }
    }
    public static int GetIntValue(SqlDataReader dr, string columnName)
    {
        try
        {
            return dr[columnName] == DBNull.Value ? 0 : Convert.ToInt32(dr[columnName]);
        }
        catch
        {
            return 0;
        }
    }

    public static decimal GetDecimalValue(SqlDataReader dr, string columnName)
    {
        try
        {
            return dr[columnName] == DBNull.Value ? 0m : Convert.ToDecimal(dr[columnName]);
        }
        catch
        {
            return 0m;
        }
    }

    public static bool GetBoolValue(SqlDataReader dr, string columnName)
    {
        try
        {
            return dr[columnName] != DBNull.Value && Convert.ToBoolean(dr[columnName]);
        }
        catch
        {
            return false;
        }
    }

    public static DateTime? GetDateTimeValue(SqlDataReader dr, string columnName)
    {
        try
        {
            return dr[columnName] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr[columnName]);
        }
        catch
        {
            return null;
        }
    }
    // ===== PARSE CODE_G =====
    public static (string codeGs, string myDeviceName, string myMAC, string loginID, string dateF) ParseCodeG(string codeG)
    {
        string[] txt = codeG.Split("_____");

        return (
            codeGs: txt.Length > 0 ? txt[0] : "",
            myDeviceName: txt.Length > 1 ? txt[1] : "",
            myMAC: txt.Length > 2 ? txt[2] : "",
            loginID: txt.Length > 3 ? txt[3] : "",
            dateF: txt.Length > 4 ? txt[4] : ""
        );
    }

    // ===== VALIDATE CODE_G =====
    public static (bool isValid, string factoryID, string errorMsg) ValidateCodeG(
        string codeG, 
        string facCode)
    {
        try
        {
            if (string.IsNullOrEmpty(codeG))
            {
                return (false, "", "NG: Code_G is required");
            }

            string[] txt = codeG.Split("_____");
            string codeGs = txt.Length > 0 ? txt[0] : "";

            if (string.IsNullOrEmpty(codeGs) || codeGs.Length < 64)
            {
                return (false, "", "NG: Invalid Code_G format");
            }

            string tmp1 = codeGs.Substring(0, 32);
            string tmp2 = codeGs.Substring(0, codeGs.Length - 32);
            string tmp3 = tmp2.Length >= 32 ? tmp2.Substring(tmp2.Length - 32) : "";
            string tmp4 = tmp2.Length > 32 ? tmp2.Substring(32) : "";
            string tmp5 = tmp4.Length > 32 ? tmp4.Substring(0, tmp4.Length - 32) : "";
            string factoryID = tmp5;

            if (tmp1 != MD5Hash(facCode) || tmp3 != MD5Hash(factoryID))
            {
                return (false, factoryID, $"NG2{factoryID}_{facCode}");
            }

            return (true, factoryID, "");
        }
        catch (Exception ex)
        {
            return (false, "", $"NG: {ex.Message}");
        }
    }

    // ===== DECODE IMAGE LIST AND ADD (GIỐNG PHP) =====
    public static string DecodeImgListAdd(
        string? nameList,
        string valueList,
        string imagePath,
        string textCut,
        string formID,
        ILogger? logger = null)
    {
        if (string.IsNullOrEmpty(nameList))
        {
            return "No image name provided";
        }
        string[] nameArray = nameList.Split(textCut, StringSplitOptions.RemoveEmptyEntries);
        string[] valueArray = valueList.Split(textCut, StringSplitOptions.RemoveEmptyEntries);

        List<string> results = new();

        for (int i = 0; i < nameArray.Length; i++)
        {
            string imgName = nameArray[i].Trim();
            if (!string.IsNullOrEmpty(imgName) && i < valueArray.Length)
            {
                string result = DecodeImgAdd(imgName, valueArray[i], imagePath, formID, logger);
                results.Add(result);
            }
        }

        // Trả về kết quả ngược (giống PHP)
        results.Reverse();
        return string.Join("<br/>", results);
    }

    // ===== DECODE IMAGE AND ADD (GIỐNG PHP) =====
    public static string DecodeImgAdd(
        string imgName,
        string base64Value,
        string imagePath,
        string formID,
        ILogger? logger = null)
    {
        try
        {
            // logger?.LogInformation($"[DecodeImgAdd] Processing: {imgName}");
            // logger?.LogInformation($"[DecodeImgAdd] Base path: {imagePath}");

            // ✅ Tạo thư mục gốc nếu chưa có
            if (!Directory.Exists(imagePath))
            {
                Directory.CreateDirectory(imagePath);
                // logger?.LogInformation($"[DecodeImgAdd] Created base directory: {imagePath}");
            }

            // ✅ Xây dựng đường dẫn đầy đủ
            string fullImagePath = Path.Combine(imagePath, imgName.Replace("/", Path.DirectorySeparatorChar.ToString()));
            
            // logger?.LogInformation($"[DecodeImgAdd] Full path: {fullImagePath}");

            // ✅ Tạo TẤT CẢ các thư mục cha (2025-Nov, 2025-Nov/2ISD, etc.)
            string? directoryPath = Path.GetDirectoryName(fullImagePath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                // logger?.LogInformation($"[DecodeImgAdd] Created directory structure: {directoryPath}");
            }

            // ✅ Xóa file cũ nếu tồn tại
            if (File.Exists(fullImagePath))
            {
                File.Delete(fullImagePath);
                logger?.LogInformation($"[DecodeImgAdd] Deleted old file: {fullImagePath}");
            }

            // ✅ Decode base64
            string base64Data = base64Value;
            if (base64Value.Contains(","))
            {
                base64Data = base64Value.Split(',')[1];
            }

            byte[] imageBytes = Convert.FromBase64String(base64Data);

            // ✅ Lưu file
            File.WriteAllBytes(fullImagePath, imageBytes);

            logger?.LogInformation($"[DecodeImgAdd] ✅ SUCCESS: Saved {fullImagePath} ({imageBytes.Length} bytes)");

            return $"{imgName} --> OK";
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, $"[DecodeImgAdd] ❌ ERROR processing {imgName}");
            return $"{imgName} --> NG: {ex.Message}";
        }
    }

    // ===== DETECT IMAGE FORMAT =====
    public static string DetectImageFormat(byte[] imageBytes)
    {
        if (imageBytes.Length < 4)
            return ".jpg";

        // PNG: 89 50 4E 47
        if (imageBytes[0] == 0x89 && imageBytes[1] == 0x50 &&
            imageBytes[2] == 0x4E && imageBytes[3] == 0x47)
            return ".png";

        // JPEG: FF D8 FF
        if (imageBytes[0] == 0xFF && imageBytes[1] == 0xD8 && imageBytes[2] == 0xFF)
            return ".jpg";

        // GIF: 47 49 46
        if (imageBytes[0] == 0x47 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46)
            return ".gif";

        // BMP: 42 4D
        if (imageBytes[0] == 0x42 && imageBytes[1] == 0x4D)
            return ".bmp";

        return ".jpg";
    }

    // ===== SANITIZE FILE NAME =====
    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return "";

        // Loại bỏ ký tự không hợp lệ
        string invalid = new string(Path.GetInvalidFileNameChars()) + 
                        new string(Path.GetInvalidPathChars());
        
        foreach (char c in invalid)
        {
            fileName = fileName.Replace(c.ToString(), "");
        }

        // Giới hạn độ dài
        if (fileName.Length > 100)
        {
            fileName = fileName.Substring(0, 100);
        }

        return fileName;
    }



    // ===== GET YEAR-MONTH FOLDER =====
    public static string GetYearMonthFolder()
    {
        return DateTime.Now.ToString("yyyy-MMM", new System.Globalization.CultureInfo("en-US"));
    }

    // ===== CREATE PHOTO PATH =====
    public static string CreatePhotoPath(string webRootPath, string formFolder, string yearMonth, string unit)
    {
        string relativePath = Path.Combine(yearMonth, unit);
        string fullPath = Path.Combine(webRootPath, "upload", "Photos", formFolder, relativePath);

        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        return fullPath;
    }


    // ===== DELETE IMAGE LIST =====
    public static string DeleteImgList(
        string nameList,
        string imagePath,
        string textCut,
        ILogger? logger = null)
    {
        string[] nameArray = nameList.Split(textCut, StringSplitOptions.RemoveEmptyEntries);
        List<string> results = new();

        foreach (string imgName in nameArray)
        {
            string trimmedName = imgName.Trim();
            if (!string.IsNullOrEmpty(trimmedName))
            {
                string result = DeleteImg(trimmedName, imagePath, logger);
                results.Add(result);
            }
        }

        // Trả về kết quả ngược (giống PHP)
        results.Reverse();
        return string.Join("<br/>", results);
    }

    // ===== DELETE SINGLE IMAGE =====
    public static string DeleteImg(
        string imgName,
        string imagePath,
        ILogger? logger = null)
    {
        try
        {
            // Xử lý đường dẫn: tách thành các phần thư mục + file
            // Ví dụ: "2025-Mar/2U09/209S07_V2503513001_280626_20250324_073632_1.jpg"
            string[] nameParts = imgName.Split('/', StringSplitOptions.RemoveEmptyEntries);

            string fullImagePath = imagePath;

            // Xây dựng đường dẫn đầy đủ
            foreach (string part in nameParts)
            {
                fullImagePath = Path.Combine(fullImagePath, part);
            }

            // Xóa file nếu tồn tại
            if (File.Exists(fullImagePath))
            {
                File.Delete(fullImagePath);
                logger?.LogInformation($"Photo deleted: {imgName}");
                return $"{imgName} --> OK";
            }
            else
            {
                logger?.LogWarning($"Photo not found: {imgName}");
                return $"{imgName} --> OK (file not found)";
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, $"Error deleting photo: {imgName}");
            return $"{imgName} --> NG: {ex.Message}";
        }
    }
}
