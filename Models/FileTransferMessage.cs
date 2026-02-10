using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace CryptoFileExchange.Models
{
    /// <summary>
    /// Poruka za prenos sifrovanog fajla preko mreze (CFTP Protocol)
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
        /// Metadata o originalnom fajlu (pre enkripcije)
        /// </summary>
        public FileMetadata? Metadata { get; set; }

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

                // METADATA (optional) - JSON serialized
                if (Metadata != null)
                {
                    string metadataJson = JsonSerializer.Serialize(Metadata);
                    byte[] metadataBytes = Encoding.UTF8.GetBytes(metadataJson);
                    writer.Write(metadataBytes.Length);  // METADATA_LENGTH (4 bytes)
                    writer.Write(metadataBytes);         // METADATA_JSON
                }
                else
                {
                    writer.Write(0);  // No metadata (length = 0)
                }

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
                byte[] fileNameBytes = new byte[fileNameLength];
                int fnRead = 0;
                while (fnRead < fileNameLength)
                {
                    int bytesRead = reader.Read(fileNameBytes, fnRead, fileNameLength - fnRead);
                    if (bytesRead == 0) throw new InvalidDataException("Stream ended while reading filename");
                    fnRead += bytesRead;
                }
                string fileName = Encoding.UTF8.GetString(fileNameBytes);

                // Procitaj FILE_SIZE
                long fileSize = reader.ReadInt64();

                // Procitaj HASH
                int hashLength = reader.ReadInt32();
                byte[] hashBytes = new byte[hashLength];
                int hashRead = 0;
                while (hashRead < hashLength)
                {
                    int bytesRead = reader.Read(hashBytes, hashRead, hashLength - hashRead);
                    if (bytesRead == 0) throw new InvalidDataException("Stream ended while reading hash");
                    hashRead += bytesRead;
                }
                string fileHash = Encoding.UTF8.GetString(hashBytes);

                // Procitaj ENCRYPTED_DATA
                long dataLength = reader.ReadInt64();
                
                // Safety check
                if (dataLength <= 0)
                {
                    throw new InvalidDataException($"Invalid data length: {dataLength}");
                }
                
                if (dataLength > int.MaxValue)
                {
                    throw new InvalidDataException($"Data length too large: {dataLength}");
                }
                
                // Read encrypted data in loop to ensure all bytes are read
                byte[] encryptedData = new byte[dataLength];
                int totalRead = 0;
                
                while (totalRead < dataLength)
                {
                    int bytesRead = reader.Read(encryptedData, totalRead, (int)(dataLength - totalRead));
                    
                    if (bytesRead == 0)
                    {
                        throw new InvalidDataException($"Stream ended prematurely. Expected {dataLength} bytes, but only read {totalRead} bytes");
                    }
                    
                    totalRead += bytesRead;
                }
                
                // Verify we read all bytes
                if (totalRead != dataLength)
                {
                    throw new InvalidDataException($"Expected to read {dataLength} bytes, but only read {totalRead} bytes");
                }
                
                // XXTEA requirement: length must be divisible by 4
                if (encryptedData.Length % 4 != 0)
                {
                    throw new InvalidDataException($"EncryptedData length ({encryptedData.Length}) must be divisible by 4 for XXTEA decryption");
                }

                // METADATA (optional) - JSON deserialization
                FileMetadata? metadata = null;
                
                if (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    int metadataLength = reader.ReadInt32();
                    
                    if (metadataLength > 0)
                    {
                        byte[] metadataBytes = new byte[metadataLength];
                        int metadataRead = 0;
                        
                        while (metadataRead < metadataLength)
                        {
                            int bytesRead = reader.Read(metadataBytes, metadataRead, metadataLength - metadataRead);
                            if (bytesRead == 0) throw new InvalidDataException("Stream ended while reading metadata");
                            metadataRead += bytesRead;
                        }
                        
                        string metadataJson = Encoding.UTF8.GetString(metadataBytes);
                        metadata = JsonSerializer.Deserialize<FileMetadata>(metadataJson);
                    }
                }

                return new FileTransferMessage
                {
                    FileName = fileName,
                    FileSize = fileSize,
                    FileHash = fileHash,
                    EncryptedData = encryptedData,
                    Metadata = metadata
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

