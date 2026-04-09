using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SecurityData.Services
{
    public class SecurityService
    {
        // Khóa bí mật cho AES-256 - nên dùng passphrase thay vì hardcode key này
        private static readonly string SecretKey = "UitiChan_Security_Key_2024_12345";

        // Mã hóa AES với IV ngẫu nhiên và salt cho mỗi tin nhắn
        public static string Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256; // AES-256
                aes.BlockSize = 128;

                // Sinh salt và IV ngẫu nhiên
                byte[] salt = new byte[16];
                byte[] iv = new byte[16];
                using (var rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(salt);
                    rng.GetBytes(iv);
                }

                // Derive key từ passphrase và salt
                var key = new Rfc2898DeriveBytes(SecretKey, salt, 100000, HashAlgorithmName.SHA256);

                aes.Key = key.GetBytes(32); // 256-bit key
                aes.IV = iv;

                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (var sw = new StreamWriter(cs)) sw.Write(plainText);
                    }

                    // Trả về base64 của Salt, IV và Ciphertext
                    byte[] cipherText = ms.ToArray();
                    byte[] result = new byte[salt.Length + iv.Length + cipherText.Length];

                    Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
                    Buffer.BlockCopy(iv, 0, result, salt.Length, iv.Length);
                    Buffer.BlockCopy(cipherText, 0, result, salt.Length + iv.Length, cipherText.Length);

                    return Convert.ToBase64String(result);
                }
            }
        }

        // Giải mã AES với IV và Salt được gửi kèm
        public static string Decrypt(string cipherText)
        {
            try
            {
                byte[] fullCipher = Convert.FromBase64String(cipherText);

                // Lấy Salt và IV từ ciphertext
                byte[] salt = new byte[16];
                byte[] iv = new byte[16];
                Buffer.BlockCopy(fullCipher, 0, salt, 0, salt.Length);
                Buffer.BlockCopy(fullCipher, salt.Length, iv, 0, iv.Length);

                // Lấy phần ciphertext còn lại
                byte[] cipherBytes = new byte[fullCipher.Length - salt.Length - iv.Length];
                Buffer.BlockCopy(fullCipher, salt.Length + iv.Length, cipherBytes, 0, cipherBytes.Length);

                // Derive key từ passphrase và salt
                var key = new Rfc2898DeriveBytes(SecretKey, salt, 100000, HashAlgorithmName.SHA256);
                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = 256; // AES-256
                    aes.Key = key.GetBytes(32); // 256-bit key
                    aes.IV = iv;

                    var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (var ms = new MemoryStream(cipherBytes))
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (var sr = new StreamReader(cs)) return sr.ReadToEnd();
                        }
                    }
                }
            }
            catch
            {
                return "[Lỗi giải mã hoặc tin nhắn không hợp lệ]";
            }
        }
    }
}