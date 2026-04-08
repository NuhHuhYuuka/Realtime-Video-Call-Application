using System;

namespace SecurityData.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string Sender { get; set; }   // Tên người gửi
        public string Content { get; set; }  // Nội dung (đã mã hóa)
        public DateTime Timestamp { get; set; }
        public bool IsFile { get; set; }     // Phân biệt tin nhắn thường hay file
    }
}