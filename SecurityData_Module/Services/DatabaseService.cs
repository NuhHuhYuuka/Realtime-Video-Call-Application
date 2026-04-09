using SecurityData.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace SecurityData.Services
{
    public class DatabaseService
    {
        // Đường dẫn tuyệt đối đến file SQLite trong thư mục ứng dụng
        private string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChatApp.db");

        // Khởi tạo cơ sở dữ liệu nếu chưa tồn tại
        public DatabaseService()
        {
            if (!File.Exists(dbPath)) SQLiteConnection.CreateFile(dbPath);
            ExecuteNonQuery("CREATE TABLE IF NOT EXISTS Messages (Id INTEGER PRIMARY KEY AUTOINCREMENT, Sender TEXT, Receiver TEXT, Content TEXT, IsFile INTEGER, FileName TEXT, FilePath TEXT, IV TEXT, Salt TEXT, TransferId TEXT, Time DATETIME)");
        }

        // Thực thi câu lệnh SQL không trả về giá trị
        private void ExecuteNonQuery(string sql)
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                new SQLiteCommand(sql, conn).ExecuteNonQuery();
            }
        }

        // Lưu tin nhắn vào cơ sở dữ liệu
        public void SaveMessage(ChatMessage msg)
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string sql = "INSERT INTO Messages (Sender, Receiver, Content, IsFile, FileName, FilePath, IV, Salt, TransferId, Time) VALUES (@s, @r, @c, @f, @fn, @fp, @iv, @slt, @tf, datetime('now'))";
                var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@s", msg.Sender);
                cmd.Parameters.AddWithValue("@r", msg.Receiver);
                cmd.Parameters.AddWithValue("@c", msg.Content);
                cmd.Parameters.AddWithValue("@f", msg.IsFile ? 1 : 0);
                cmd.Parameters.AddWithValue("@fn", msg.FileName);
                cmd.Parameters.AddWithValue("@fp", msg.FilePath);
                cmd.Parameters.AddWithValue("@iv", msg.IV);
                cmd.Parameters.AddWithValue("@slt", msg.Salt);
                cmd.Parameters.AddWithValue("@tf", msg.TransferId);
                cmd.ExecuteNonQuery();
            }
        }

        // Lấy lịch sử tin nhắn theo người nhận/gửi (hoặc tất cả nếu không có tiêu chí)
        public List<ChatMessage> GetHistory(string peer)
        {
            var list = new List<ChatMessage>();
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                var cmd = new SQLiteCommand("SELECT * FROM Messages WHERE Sender = @peer OR Receiver = @peer ORDER BY Time ASC", conn);
                cmd.Parameters.AddWithValue("@peer", peer);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new ChatMessage
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Sender = reader["Sender"].ToString(),
                            Receiver = reader["Receiver"].ToString(),
                            Content = reader["Content"].ToString(),
                            IsFile = Convert.ToInt32(reader["IsFile"]) == 1,
                            FileName = reader["FileName"].ToString(),
                            FilePath = reader["FilePath"].ToString(),
                            IV = reader["IV"].ToString(),
                            Salt = reader["Salt"].ToString(),
                            TransferId = reader["TransferId"].ToString(),
                            Timestamp = Convert.ToDateTime(reader["Time"])
                        });
                    }
                }
            }
            return list;
        }

        // Lấy tất cả lịch sử tin nhắn
        public List<ChatMessage> GetAllHistory()
        {
            var list = new List<ChatMessage>();
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                var cmd = new SQLiteCommand("SELECT * FROM Messages ORDER BY Time ASC", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new ChatMessage
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Sender = reader["Sender"].ToString(),
                            Receiver = reader["Receiver"].ToString(),
                            Content = reader["Content"].ToString(),
                            IsFile = Convert.ToInt32(reader["IsFile"]) == 1,
                            FileName = reader["FileName"].ToString(),
                            FilePath = reader["FilePath"].ToString(),
                            IV = reader["IV"].ToString(),
                            Salt = reader["Salt"].ToString(),
                            TransferId = reader["TransferId"].ToString(),
                            Timestamp = Convert.ToDateTime(reader["Time"])
                        });
                    }
                }
            }
            return list;
        }

        // Cập nhật tin nhắn
        public void UpdateMessage(int id, string newEncryptedContent, string newIv, string newSalt)
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string sql = "UPDATE Messages SET Content = @c, IV = @iv, Salt = @slt WHERE Id = @id";
                var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@c", newEncryptedContent);
                cmd.Parameters.AddWithValue("@iv", newIv);
                cmd.Parameters.AddWithValue("@slt", newSalt);
                cmd.ExecuteNonQuery();
            }
        }

        // Xóa tin nhắn theo ID
        public void DeleteMessage(int id)
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string sql = "DELETE FROM Messages WHERE Id = @id";
                var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        // Xóa tất cả tin nhắn giữa người dùng
        public void DeleteConversation(string peer)
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string sql = "DELETE FROM Messages WHERE Sender = @peer OR Receiver = @peer";
                var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@peer", peer);
                cmd.ExecuteNonQuery();
            }
        }
    }
}