using System;
using System.IO;
using System.Text;

namespace CryptoFileExchange.Models
{
    /// <summary>
    /// Poruka za prenos sifrovanog fajla preko mreze
    /// </summary>
    public class FileTransferMessage
    {
        // Magic bytes za identifikaciju CFTP protokola
        private const string MAGIC = "CFTP";
        private const int VERSION = 1;

        public required string FileName { get; set; }
        public long FileSize { get; set; }
        public required string FileHash { get; set; }
        public required byte[] EncryptedData { get; set; }

        /// <summary>
        /// Serijalizuje poruku u binarni format za slanje preko mreze
        /// </summary>
        public byte[] ToBytes()
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8))
            {
                // MAGIC (4 bytes)
                byte[] magicBytes = Encoding.ASCII.GetBytes(MAGIC);
                writer.Write(magicBytes);

                // VERSION (4 bytes)
                writer.Write(VERSION);

                // FILENAME_LENGTH (4 bytes) + FILENAME (UTF-8 bytes)
                byte[] fileNameBytes = Encoding.UTF8.GetBytes(FileName);
                writer.Write(fileNameBytes.Length);
                writer.Write(fileNameBytes);

                // FILE_SIZE (8 bytes, long)
                writer.Write(FileSize);

                // HASH_LENGTH (4 bytes) + HASH (UTF-8 bytes)
                byte[] hashBytes = Encoding.UTF8.GetBytes(FileHash);
                writer.Write(hashBytes.Length);
                writer.Write(hashBytes);

                // DATA_LENGTH (8 bytes, long) + ENCRYPTED_DATA
                writer.Write((long)EncryptedData.Length);
                writer.Write(EncryptedData);

                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Deserijalizuje poruku iz binarnog formata
        /// </summary>
        public static FileTransferMessage FromBytes(byte[] data)
        {
            using (var memoryStream = new MemoryStream(data))
            using (var reader = new BinaryReader(memoryStream, Encoding.UTF8))
            {
                // Procitaj i proveri MAGIC
                byte[] magicBytes = reader.ReadBytes(4);
                string magic = Encoding.ASCII.GetString(magicBytes);
                if (magic != MAGIC)
                {
                    throw new InvalidDataException($"Invalid magic bytes. Expected '{MAGIC}', got '{magic}'");
                }

                // Procitaj VERSION
                int version = reader.ReadInt32();
                if (version != VERSION)
                {
                    throw new InvalidDataException($"Unsupported version. Expected {VERSION}, got {version}");
                }

                // Procitaj FILENAME
                int fileNameLength = reader.ReadInt32();
                byte[] fileNameBytes = reader.ReadBytes(fileNameLength);
                string fileName = Encoding.UTF8.GetString(fileNameBytes);

                // Procitaj FILE_SIZE
                long fileSize = reader.ReadInt64();

                // Procitaj HASH
                int hashLength = reader.ReadInt32();
                byte[] hashBytes = reader.ReadBytes(hashLength);
                string fileHash = Encoding.UTF8.GetString(hashBytes);

                // Procitaj ENCRYPTED_DATA
                long dataLength = reader.ReadInt64();
                byte[] encryptedData = reader.ReadBytes((int)dataLength);

                return new FileTransferMessage
                {
                    FileName = fileName,
                    FileSize = fileSize,
                    FileHash = fileHash,
                    EncryptedData = encryptedData
                };
            }
        }

        /// <summary>
        /// Validacija poruke
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(FileName)
                && FileSize > 0
                && !string.IsNullOrWhiteSpace(FileHash)
                && EncryptedData != null
                && EncryptedData.Length > 0;
        }

        public override string ToString()
        {
            return $"FileTransferMessage: {FileName} ({FileSize} bytes, Hash: {FileHash.Substring(0, 8)}...)";
        }
    }
}

