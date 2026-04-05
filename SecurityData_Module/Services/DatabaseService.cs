using SecurityData.Models;
using SecurityData.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace SecurityData.Services
{
    public class DatabaseService
    {
        private string dbPath = "LocalChat.db";

        public DatabaseService()
        {
            if (!File.Exists(dbPath)) SQLiteConnection.CreateFile(dbPath);
            ExecuteNonQuery("CREATE TABLE IF NOT EXISTS Messages (Id INTEGER PRIMARY KEY AUTOINCREMENT, Sender TEXT, Content TEXT, IsFile INTEGER, Time DATETIME)");
        }

        private void ExecuteNonQuery(string sql)
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                new SQLiteCommand(sql, conn).ExecuteNonQuery();
            }
        }

        public void SaveMessage(string sender, string encryptedContent, bool isFile)
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string sql = "INSERT INTO Messages (Sender, Content, IsFile, Time) VALUES (@s, @c, @f, datetime('now'))";
                var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@s", sender);
                cmd.Parameters.AddWithValue("@c", encryptedContent);
                cmd.Parameters.AddWithValue("@f", isFile ? 1 : 0);
                cmd.ExecuteNonQuery();
            }
        }

        public List<ChatMessage> GetHistory()
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
                            Sender = reader["Sender"].ToString(),
                            Content = reader["Content"].ToString(),
                            IsFile = Convert.ToInt32(reader["IsFile"]) == 1
                        });
                    }
                }
            }
            return list;
        }
    }
}