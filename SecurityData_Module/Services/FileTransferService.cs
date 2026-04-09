using SecurityData.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace SecurityData.Services
{
    public class FileTransferService
    {
        public static byte[] FileToBytes(string filePath)
        {
            return File.ReadAllBytes(filePath);
        }

        public static void BytesToFile(string fileName, byte[] data)
        {
            string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ChatApp_Files");
            if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

            File.WriteAllBytes(Path.Combine(savePath, fileName), data);
        }

        public static IEnumerable<FileChunk> SplitFile(string filePath, int chunkSize = 64 * 1024)
        {
            var fileInfo = new FileInfo(filePath);
            int totalChunks = (int)Math.Ceiling((double)fileInfo.Length / chunkSize);
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[chunkSize];
                for (int chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
                {
                    int bytesRead = fs.Read(buffer, 0, chunkSize);
                    var chunk = new FileChunk
                    {
                        FileName = fileInfo.Name,
                        ChunkIndex = chunkIndex,
                        TotalChunks = totalChunks,
                        Data = new byte[bytesRead],
                        IsLastChunk = chunkIndex == totalChunks - 1
                    };
                    Array.Copy(buffer, chunk.Data, bytesRead);
                    yield return chunk;
                }
            }
        }

        public static string ComputeSha256(string filePath)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    return BitConverter.ToString(sha256.ComputeHash(fs)).Replace("-", string.Empty);
                }
            }
        }
    }
}