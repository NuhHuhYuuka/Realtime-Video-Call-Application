using System;
using System.IO;

namespace SecurityData.Services
{
    public class FileTransferService
    {
        // Hàm chặt file ra thành mảng byte để gửi đi
        public static byte[] FileToBytes(string filePath)
        {
            return File.ReadAllBytes(filePath);
        }

        // Hàm nhận mảng byte và lưu lại thành file
        public static void BytesToFile(string fileName, byte[] data)
        {
            string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ChatApp_Files");
            if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

            File.WriteAllBytes(Path.Combine(savePath, fileName), data);
        }
    }
}