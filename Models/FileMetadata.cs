using System;

namespace CryptoFileExchange.Models
{
    /// <summary>
    /// Metadata o originalnom fajlu (pre enkripcije)
    /// </summary>
    public class FileMetadata
    {
        public required string OriginalFileName { get; set; }
        public long FileSize { get; set; }
        public DateTime CreationTime { get; set; }
        public string EncryptionAlgorithm { get; set; } = string.Empty;
        public string HashAlgorithm { get; set; } = string.Empty;
        public required string FileHash { get; set; }
    }
}
