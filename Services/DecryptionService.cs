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
        // Algoritmi
        private readonly EnigmaEngine _enigma;
        private readonly XXTEAEngine _xxtea;
        private readonly CFBMode _cfb;
        private readonly TigerHash _tiger;

        // Kljucevi
        private readonly string _enigmaKey;
        private readonly string _xxteaKey;
        private readonly string _cfbKey;

        // Events za progress
        public event EventHandler<FileProgressEventArgs>? DecryptionProgress;

        public DecryptionService(string enigmaKey, string xxteaKey, string cfbKey)
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
        /// Obrnutu lanac dekripcije: CFB^-1 -> XXTEA^-1 -> Enigma^-1
        /// </summary>
        private byte[] DecryptChain(byte[] data)
        {
            // Korak 1: CFB Decrypt
            byte[] step1 = _cfb.Decrypt(data, _cfbKey);
            Log.Debug("CFB decryption completed ({Bytes} bytes)", step1.Length);

            // Korak 2: XXTEA Decrypt
            byte[] step2 = _xxtea.Decrypt(step1, _xxteaKey);
            Log.Debug("XXTEA decryption completed ({Bytes} bytes)", step2.Length);

            // Korak 3: Enigma Decrypt
            byte[] step3 = _enigma.Decrypt(step2, _enigmaKey);
            Log.Debug("Enigma decryption completed ({Bytes} bytes)", step3.Length);

            return step3;
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
