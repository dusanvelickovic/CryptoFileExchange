using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using CryptoFileExchange.Models;

namespace CryptoFileExchange.Services
{
    internal class MetadataService
    {
        private const string HEADER_MAGIC = "CFEX"; // CryptoFileEXchange
        private const int HEADER_VERSION = 1;
        private const int SIZE_LENGTH = 4; // int32 za dužinu JSON-a

        /// <summary>
        /// Serijalizuje FileMetadata objekat u JSON string
        /// </summary>
        public string SerializeToJson(FileMetadata metadata)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            return JsonConvert.SerializeObject(metadata, Formatting.Indented);
        }

        /// <summary>
        /// Deserijalizuje JSON string u FileMetadata objekat
        /// </summary>
        public FileMetadata DeserializeFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON cannot be null or empty", nameof(json));

            return JsonConvert.DeserializeObject<FileMetadata>(json);
        }

        /// <summary>
        /// Kreira kompletan fajl sa header-om i šifrovanim podacima
        /// Struktura: [MAGIC(4)] [VERSION(4)] [HEADER_SIZE(4)] [JSON_HEADER] [ENCRYPTED_DATA]
        /// </summary>
        public byte[] AddHeaderToFile(FileMetadata metadata, byte[] encryptedData)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            if (encryptedData == null || encryptedData.Length == 0)
                throw new ArgumentException("Encrypted data cannot be null or empty", nameof(encryptedData));

            // Serijalizuj metadata u JSON
            string jsonMetadata = SerializeToJson(metadata);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonMetadata);

            // Kreiraj header
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                // Magic bytes (4 bytes) - identifikator formata
                writer.Write(Encoding.ASCII.GetBytes(HEADER_MAGIC));

                // Version (4 bytes) - verzija formata
                writer.Write(HEADER_VERSION);

                // Header size (4 bytes) - dužina JSON metadata
                writer.Write(jsonBytes.Length);

                // JSON metadata
                writer.Write(jsonBytes);

                // Encrypted data
                writer.Write(encryptedData);

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Čita header iz fajla i vraća metadata i šifrovane podatke
        /// </summary>
        public (FileMetadata metadata, byte[] encryptedData) ReadHeaderFromFile(byte[] fileData)
        {
            if (fileData == null || fileData.Length == 0)
                throw new ArgumentException("File data cannot be null or empty", nameof(fileData));

            using (MemoryStream ms = new MemoryStream(fileData))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                // Proveri magic bytes
                byte[] magicBytes = reader.ReadBytes(4);
                string magic = Encoding.ASCII.GetString(magicBytes);

                if (magic != HEADER_MAGIC)
                    throw new InvalidDataException($"Invalid file format. Expected '{HEADER_MAGIC}', got '{magic}'");

                // Čitaj verziju
                int version = reader.ReadInt32();
                if (version != HEADER_VERSION)
                    throw new InvalidDataException($"Unsupported version {version}. Expected {HEADER_VERSION}");

                // Čitaj dužinu header-a
                int headerSize = reader.ReadInt32();
                if (headerSize <= 0 || headerSize > fileData.Length)
                    throw new InvalidDataException($"Invalid header size: {headerSize}");

                // Čitaj JSON metadata
                byte[] jsonBytes = reader.ReadBytes(headerSize);
                string jsonMetadata = Encoding.UTF8.GetString(jsonBytes);

                // Deserijalizuj metadata
                FileMetadata metadata = DeserializeFromJson(jsonMetadata);

                // Čitaj preostale podatke (šifrovani sadržaj)
                int remainingBytes = (int)(ms.Length - ms.Position);
                byte[] encryptedData = reader.ReadBytes(remainingBytes);

                return (metadata, encryptedData);
            }
        }

        /// <summary>
        /// Kreira FileMetadata objekat sa zadatim parametrima
        /// </summary>
        public FileMetadata CreateMetadata(
            string originalFileName,
            long fileSize,
            string encryptionAlgorithm,
            string hashAlgorithm,
            string fileHash)
        {
            return new FileMetadata
            {
                OriginalFileName = originalFileName,
                FileSize = fileSize,
                CreationDate = DateTime.Now,
                EncryptionAlgorithm = encryptionAlgorithm,
                HashAlgorithm = hashAlgorithm,
                FileHash = fileHash
            };
        }

        /// <summary>
        /// Učitava fajl sa diska i čita header
        /// </summary>
        public (FileMetadata metadata, byte[] encryptedData) ReadHeaderFromFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            byte[] fileData = File.ReadAllBytes(filePath);
            return ReadHeaderFromFile(fileData);
        }

        /// <summary>
        /// Cuva fajl sa header-om na disk
        /// </summary>
        public void SaveFileWithHeader(string filePath, FileMetadata metadata, byte[] encryptedData)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            byte[] fileWithHeader = AddHeaderToFile(metadata, encryptedData);
            File.WriteAllBytes(filePath, fileWithHeader);
        }

        /// <summary>
        /// Validira da li fajl ima ispravan CFEX header
        /// </summary>
        public bool ValidateFileFormat(byte[] fileData)
        {
            if (fileData == null || fileData.Length < 12) // Minimum: MAGIC(4) + VERSION(4) + SIZE(4)
                return false;

            try
            {
                byte[] magicBytes = new byte[4];
                Array.Copy(fileData, 0, magicBytes, 0, 4);
                string magic = Encoding.ASCII.GetString(magicBytes);

                return magic == HEADER_MAGIC;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Prikazuje metadata u human-readable formatu
        /// </summary>
        public string FormatMetadataForDisplay(FileMetadata metadata)
        {
            if (metadata == null)
                return "No metadata available";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== File Metadata ===");
            sb.AppendLine($"Original File Name: {metadata.OriginalFileName}");
            sb.AppendLine($"File Size: {FormatFileSize(metadata.FileSize)}");
            sb.AppendLine($"Creation Date: {metadata.CreationDate:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Encryption Algorithm: {metadata.EncryptionAlgorithm}");
            sb.AppendLine($"Hash Algorithm: {metadata.HashAlgorithm}");
            sb.AppendLine($"File Hash: {metadata.FileHash}");

            return sb.ToString();
        }

        /// <summary>
        /// Formatira veličinu fajla u čitljiv oblik
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}
