using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecurityData.Models
{
    public class NetworkPacket
    {
        public string Type { get; set; } // TEXT, FILE_META, FILE_CHUNK, FILE_END
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string EncryptedContent { get; set; }
        public string IV { get; set; }
        public string Salt { get; set; }
        public string TransferId { get; set; }
        public string FileName { get; set; }
        public int ChunkIndex { get; set; }
        public int TotalChunks { get; set; }
        public byte[] ChunkData { get; set; }
        public long FileSize { get; set; }
        public string Hash { get; set; }
    }
}