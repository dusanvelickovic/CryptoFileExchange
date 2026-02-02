using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoFileExchange.Models
{
    internal class FileMetadata
    {
        public string OriginalFileName { get; set; }
        public long FileSize { get; set; }
        public DateTime CreationDate { get; set; }
        public string EncryptionAlgorithm { get; set; }
        public string HashAlgorithm { get; set; }
        public string FileHash { get; set; }
    }
}
