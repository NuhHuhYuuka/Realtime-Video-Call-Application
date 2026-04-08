using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SecurityData.Services
{
    public class SecurityService
    {
        // Khóa 32 ký tự cho AES-256 (Phải thống nhất giữa các máy)
        private static readonly string SecretKey = "UitiChan_Security_Key_2024_12345";

        public static string Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(SecretKey);
                aes.IV = new byte[16]; // IV tĩnh để đơn giản hóa cho đồ án
                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (var sw = new StreamWriter(cs)) sw.Write(plainText);
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
        }

        public static string Decrypt(string cipherText)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(SecretKey);
                    aes.IV = new byte[16];
                    var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (var ms = new MemoryStream(Convert.FromBase64String(cipherText)))
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (var sr = new StreamReader(cs)) return sr.ReadToEnd();
                        }
                    }
                }
            }
            catch { return "[Lỗi giải mã hoặc tin nhắn không hợp lệ]"; }
        }
    }
}