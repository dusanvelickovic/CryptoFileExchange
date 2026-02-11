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
    /// Servis za dekripciju fajlova (CFB^-1 -> XXTEA^-1 -> Enigma^-1) + hash verifikacija
    /// </summary>
    public class DecryptionService
    {
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
        public event EventHandler<FileProgressEventArgs>? DecryptionProgress;

        public DecryptionService(string enigmaKey, string xxteaKey, string cfbKey, string cfbIV = "")
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
        /// Dekriptuje fajl i verifikuje hash
        /// </summary>
        public async Task<(byte[] decryptedData, bool hashValid)> DecryptFileAsync(byte[] encryptedData, string expectedHash)
        {
            if (encryptedData == null || encryptedData.Length == 0)
                throw new ArgumentException("Encrypted data cannot be null or empty");

            if (string.IsNullOrWhiteSpace(expectedHash))
                throw new ArgumentException("Expected hash cannot be null or empty");

            Log.Information("Decrypting file ({Bytes} bytes)", encryptedData.Length);

            // Prvo verifikuj hash nad enkriptovanim podacima
            byte[] hashBytes = _tiger.ComputeHash(encryptedData);
            string actualHash = BytesToHexString(hashBytes);

            bool hashValid = actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);

            if (!hashValid)
            {
                Log.Warning("Hash verification failed! Expected: {ExpectedHash}, Actual: {ActualHash}",
                    expectedHash, actualHash);
            }
            else
            {
                Log.Information("Hash verified successfully: {Hash}", actualHash);
            }

            // Dekriptuj kroz obrnutu lanac
            byte[] decryptedData = DecryptChain(encryptedData);

            Log.Information("File decrypted successfully ({Bytes} bytes)", decryptedData.Length);

            return (decryptedData, hashValid);
        }

        /// <summary>
        /// Obrnutu lanac dekripcije: CFB^-1 -> XXTEA^-1 -> Enigma^-1 (with IV)
        /// </summary>
        private byte[] DecryptChain(byte[] data)
        {
            // Korak 1: CFB Decrypt (with IV for compatibility)
            byte[] cfbIVBytes = StringToKeyBytes(_cfbIV, CFB_KEY_SIZE);
            byte[] step1 = _cfb.Decrypt(data, _cfbKey, cfbIVBytes);
            Log.Debug("CFB decryption completed ({Bytes} bytes)", step1.Length);

            // Korak 2: XXTEA Decrypt (uses byte[] key - 16 bytes for compatibility)
            byte[] xxteaKeyBytes = StringToKeyBytes(_xxteaKey, 16);
            byte[] step2 = _xxtea.Decrypt(step1, xxteaKeyBytes);
            Log.Debug("XXTEA decryption completed ({Bytes} bytes)", step2.Length);

            // Korak 3: Enigma Decrypt
            byte[] step3 = _enigma.Decrypt(step2, _enigmaKey);
            Log.Debug("Enigma decryption completed ({Bytes} bytes)", step3.Length);

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
        /// Sacuva dekriptovani fajl na disk
        /// </summary>
        public async Task SaveDecryptedFileAsync(byte[] decryptedData, string outputPath)
        {
            if (decryptedData == null || decryptedData.Length == 0)
                throw new ArgumentException("Decrypted data cannot be null or empty");

            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path cannot be null or empty");

            // Kreiraj direktorijum ako ne postoji
            string? directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(outputPath, decryptedData);

            Log.Information("Decrypted file saved to: {OutputPath} ({Bytes} bytes)", outputPath, decryptedData.Length);
        }

        /// <summary>
        /// Verifikuje hash bez dekripcije (za brzu proveru)
        /// </summary>
        public bool VerifyHash(byte[] encryptedData, string expectedHash)
        {
            if (encryptedData == null || encryptedData.Length == 0)
                return false;

            if (string.IsNullOrWhiteSpace(expectedHash))
                return false;

            byte[] hashBytes = _tiger.ComputeHash(encryptedData);
            string actualHash = BytesToHexString(hashBytes);

            return actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Konvertuje byte[] u hex string (za hash)
        /// </summary>
        private string BytesToHexString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        protected virtual void OnDecryptionProgress(FileProgressEventArgs e)
        {
            DecryptionProgress?.Invoke(this, e);
        }
    }
}
