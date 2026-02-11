using System;
using System.IO;
using System.Threading.Tasks;
using CryptoFileExchange.Algorithms.Symmetric;
using CryptoFileExchange.Algorithms.BlockCipher;
using CryptoFileExchange.Algorithms.Hash;
using Serilog;

namespace CryptoFileExchange.Services
{
    /// <summary>
    /// Servis za enkripciju fajlova (Enigma -> XXTEA -> CFB) + TigerHash
    /// </summary>
    public class EncryptionService
    {
        private const long STREAMING_THRESHOLD = 50 * 1024 * 1024; // 50 MB
        private const int BUFFER_SIZE = 1024 * 1024; // 1 MB chunks
        private const int CFB_KEY_SIZE = 16; // 16 bytes (128-bit)

        // Algoritmi
        private readonly EnigmaEngine _enigma;
        private readonly XXTEAEngine _xxtea;
        private readonly CFBMode _cfb;
        private readonly TigerHash _tiger;

        // Kljucevi
        private readonly string _enigmaKey;
        private readonly string _xxteaKey;
        private readonly string _cfbKey;
        private readonly string _cfbIV;

        // Events za progress
        public event EventHandler<FileProgressEventArgs>? EncryptionProgress;

        public EncryptionService(string enigmaKey, string xxteaKey, string cfbKey, string cfbIV = "")
        {
            if (string.IsNullOrWhiteSpace(enigmaKey))
                throw new ArgumentException("Enigma key cannot be null or empty");
            if (string.IsNullOrWhiteSpace(xxteaKey))
                throw new ArgumentException("XXTEA key cannot be null or empty");
            if (string.IsNullOrWhiteSpace(cfbKey))
                throw new ArgumentException("CFB key cannot be null or empty");

            _enigmaKey = enigmaKey;
            _xxteaKey = xxteaKey;
            _cfbKey = cfbKey;
            _cfbIV = cfbIV;

            _enigma = new EnigmaEngine();
            _xxtea = new XXTEAEngine();
            _cfb = new CFBMode();
            _tiger = new TigerHash();
        }

        /// <summary>
        /// Enkriptuje fajl i vraca encrypted bytes + hash
        /// </summary>
        public async Task<(byte[] encryptedData, string hash)> EncryptFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            FileInfo fileInfo = new FileInfo(filePath);
            byte[] encryptedData;

            Log.Information("Encrypting file: {FileName} ({FileSize} bytes)", fileInfo.Name, fileInfo.Length);

            // Odaberi metodu (in-memory ili streaming) na osnovu velicine
            if (fileInfo.Length < STREAMING_THRESHOLD)
            {
                // In-memory enkripcija za male fajlove
                byte[] originalData = await File.ReadAllBytesAsync(filePath);
                encryptedData = EncryptChain(originalData);
            }
            else
            {
                // Streaming enkripcija za velike fajlove
                encryptedData = await EncryptFileStreamingAsync(filePath);
            }

            // Izracunaj TigerHash nad enkriptovanim podacima
            byte[] hashBytes = _tiger.ComputeHash(encryptedData);
            string hash = BytesToHexString(hashBytes);

            Log.Information("File encrypted successfully. Hash: {Hash}", hash);

            return (encryptedData, hash);
        }

        /// <summary>
        /// Lanac enkripcije: Enigma -> XXTEA -> CFB (with IV)
        /// </summary>
        private byte[] EncryptChain(byte[] data)
        {
            // Korak 1: Enigma
            byte[] step1 = _enigma.Encrypt(data, _enigmaKey);
            Log.Debug("Enigma encryption completed ({Bytes} bytes)", step1.Length);

            // Korak 2: XXTEA (uses byte[] key - 16 bytes for compatibility)
            byte[] xxteaKeyBytes = StringToKeyBytes(_xxteaKey, 16);
            byte[] step2 = _xxtea.Encrypt(step1, xxteaKeyBytes);
            Log.Debug("XXTEA encryption completed ({Bytes} bytes)", step2.Length);

            // Korak 3: CFB (with IV for compatibility)
            byte[] cfbIVBytes = StringToKeyBytes(_cfbIV, CFB_KEY_SIZE);
            byte[] step3 = _cfb.Encrypt(step2, _cfbKey, cfbIVBytes);
            Log.Debug("CFB encryption completed ({Bytes} bytes)", step3.Length);

            return step3;
        }

        /// <summary>
        /// Convert string to byte array with padding/truncating
        /// </summary>
        private byte[] StringToKeyBytes(string keyString, int targetLength)
        {
            if (string.IsNullOrEmpty(keyString))
            {
                return new byte[targetLength];
            }

            byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(keyString);

            if (keyBytes.Length == targetLength)
            {
                return keyBytes;
            }
            else if (keyBytes.Length < targetLength)
            {
                // Padding sa nulama
                byte[] padded = new byte[targetLength];
                Array.Copy(keyBytes, padded, keyBytes.Length);
                return padded;
            }
            else
            {
                // Truncate
                byte[] truncated = new byte[targetLength];
                Array.Copy(keyBytes, truncated, targetLength);
                return truncated;
            }
        }

        /// <summary>
        /// Streaming enkripcija za velike fajlove (>50MB)
        /// </summary>
        private async Task<byte[]> EncryptFileStreamingAsync(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            long totalBytes = fileInfo.Length;
            long bytesProcessed = 0;

            Log.Information("Starting streaming encryption for large file: {FileName}", fileInfo.Name);

            using (var memoryStream = new MemoryStream())
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[BUFFER_SIZE];
                    int bytesRead;

                    while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        byte[] chunk = new byte[bytesRead];
                        Array.Copy(buffer, 0, chunk, 0, bytesRead);

                        // Enkriptuj chunk kroz lanac
                        byte[] encryptedChunk = EncryptChain(chunk);
                        await memoryStream.WriteAsync(encryptedChunk, 0, encryptedChunk.Length);

                        bytesProcessed += bytesRead;

                        // Progress event
                        int progressPercentage = (int)((bytesProcessed * 100) / totalBytes);
                        OnEncryptionProgress(new FileProgressEventArgs
                        {
                            FileName = fileInfo.Name,
                            FilePath = filePath,
                            BytesProcessed = bytesProcessed,
                            TotalBytes = totalBytes,
                            ProgressPercentage = progressPercentage
                        });

                        Log.Debug("Encrypted chunk: {BytesProcessed}/{TotalBytes} bytes ({ProgressPercentage}%)",
                            bytesProcessed, totalBytes, progressPercentage);
                    }
                }

                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Konvertuje byte[] u hex string (za hash)
        /// </summary>
        private string BytesToHexString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        protected virtual void OnEncryptionProgress(FileProgressEventArgs e)
        {
            EncryptionProgress?.Invoke(this, e);
        }
    }
}

