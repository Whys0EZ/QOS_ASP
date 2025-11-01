using System.Security.Cryptography;
using System.Text;

namespace QOS.Areas.API.Helpers
{
    public static class EncryptionHelper
    {
        // ✅ Giống PHP MCrypt class
        private const string DefaultIV = "fedcba9876543210";
        private const string DefaultKey = "0123456789abcdef";

        /// <summary>
        /// Encrypt string (giống PHP MCrypt::encrypt)
        /// </summary>
        public static string Encrypt(string plainText, string? key = null, string? iv = null)
        {
            try
            {
                key ??= DefaultKey;
                iv ??= DefaultIV;

                byte[] keyBytes = Encoding.UTF8.GetBytes(key);
                byte[] ivBytes = Encoding.UTF8.GetBytes(iv);
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

                using Aes aes = Aes.Create();
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using MemoryStream ms = new();
                using CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write);
                
                cs.Write(plainBytes, 0, plainBytes.Length);
                cs.FlushFinalBlock();

                byte[] encrypted = ms.ToArray();
                
                // ✅ Convert to hex string (giống bin2hex trong PHP)
                return BitConverter.ToString(encrypted).Replace("-", "").ToLower();
            }
            catch (Exception ex)
            {
                throw new Exception($"Encryption failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Decrypt encrypted hex string (giống PHP MCrypt::decrypt)
        /// </summary>
        public static string Decrypt(string encryptedHex, string? key = null, string? iv = null)
        {
            try
            {
                key ??= DefaultKey;
                iv ??= DefaultIV;

                // ✅ Convert hex string to bytes (giống hex2bin trong PHP)
                byte[] encryptedBytes = HexToBytes(encryptedHex.Trim());
                byte[] keyBytes = Encoding.UTF8.GetBytes(key);
                byte[] ivBytes = Encoding.UTF8.GetBytes(iv);

                using Aes aes = Aes.Create();
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using MemoryStream ms = new(encryptedBytes);
                using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
                using StreamReader sr = new(cs);
                
                string decrypted = sr.ReadToEnd();
                
                // ✅ Trim giống PHP
                return decrypted.Trim();
            }
            catch (Exception ex)
            {
                throw new Exception($"Decryption failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Convert hex string to byte array (giống hex2bin trong PHP)
        /// </summary>
        private static byte[] HexToBytes(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string must have even length");

            byte[] bytes = new byte[hex.Length / 2];
            
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }

        /// <summary>
        /// Convert byte array to hex string (giống bin2hex trong PHP)
        /// </summary>
        public static string BytesToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }
}