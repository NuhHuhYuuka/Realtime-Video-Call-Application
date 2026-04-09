using System;

namespace SecurityData.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Content { get; set; }        // ciphertext hoặc plaintext sau giải mã
        public DateTime Timestamp { get; set; }
        public bool IsFile { get; set; }

        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string IV { get; set; }
        public string Salt { get; set; }
        public string TransferId { get; set; }
    }
}