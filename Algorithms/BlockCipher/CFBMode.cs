using System;
using System.Security.Cryptography;
using System.Text;
using CryptoFileExchange.Algorithms.Symmetric;

namespace CryptoFileExchange.Algorithms.BlockCipher
{
    /// <summary>
    /// CFB Mode (Cipher Feedback) implementation
    /// </summary>
    internal class CFBMode
    {
        private readonly XXTEAEngine _xxtea;
        private const int BLOCK_SIZE = 16;

        public CFBMode()
        {
            _xxtea = new XXTEAEngine();
        }

        /// <summary>
        /// Convert string to 16-byte key array (padding/truncating)
        /// </summary>
        private byte[] StringToKeyBytes(string keyString)
        {
            if (string.IsNullOrEmpty(keyString))
            {
                return new byte[16];
            }

            byte[] keyBytes = Encoding.UTF8.GetBytes(keyString);

            if (keyBytes.Length == 16)
            {
                return keyBytes;
            }
            else if (keyBytes.Length < 16)
            {
                // Padding with zeros
                byte[] padded = new byte[16];
                Array.Copy(keyBytes, padded, keyBytes.Length);
                return padded;
            }
            else
            {
                // Truncating to 16 bytes
                byte[] truncated = new byte[16];
                Array.Copy(keyBytes, truncated, 16);
                return truncated;
            }
        }

        /// <summary>
        /// Encrypt data using CFB mode
        /// </summary>
        public byte[] Encrypt(byte[] data, string key, byte[]? iv = null)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty");

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty");

            // Convert string key to 16-byte array
            byte[] keyBytes = StringToKeyBytes(key);

            // Use provided IV or generate new one
            byte[] actualIV = iv ?? GenerateIV();
            
            if (actualIV.Length != BLOCK_SIZE)
                throw new ArgumentException($"IV must be exactly {BLOCK_SIZE} bytes");

            // Encrypt using CFB mode
            byte[] encrypted = EncryptCFB(data, keyBytes, actualIV);

            // If IV was provided, don't prepend it to result
            if (iv != null)
            {
                return encrypted;
            }

            // Otherwise, prepend IV to result (original behavior)
            byte[] result = new byte[actualIV.Length + encrypted.Length];
            Array.Copy(actualIV, 0, result, 0, actualIV.Length);
            Array.Copy(encrypted, 0, result, actualIV.Length, encrypted.Length);

            return result;
        }

        /// <summary>
        /// Decrypt data using CFB mode
        /// </summary>
        public byte[] Decrypt(byte[] data, string key, byte[]? iv = null)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty");

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty");

            // Convert string key to 16-byte array
            byte[] keyBytes = StringToKeyBytes(key);

            byte[] encryptedData;
            byte[] actualIV;

            if (iv != null)
            {
                // IV provided explicitly
                actualIV = iv;
                encryptedData = data;
            }
            else
            {
                // IV prepended to data (original behavior)
                if (data.Length < BLOCK_SIZE)
                    throw new ArgumentException("Data too short to contain IV");

                actualIV = new byte[BLOCK_SIZE];
                Array.Copy(data, 0, actualIV, 0, BLOCK_SIZE);

                encryptedData = new byte[data.Length - BLOCK_SIZE];
                Array.Copy(data, BLOCK_SIZE, encryptedData, 0, encryptedData.Length);
            }

            if (actualIV.Length != BLOCK_SIZE)
                throw new ArgumentException($"IV must be exactly {BLOCK_SIZE} bytes");

            return DecryptCFB(encryptedData, keyBytes, actualIV);
        }

        /// <summary>
        /// CFB encryption implementation
        /// </summary>
        private byte[] EncryptCFB(byte[] data, byte[] key, byte[] iv)
        {
            byte[] encrypted = new byte[data.Length];
            byte[] feedback = new byte[BLOCK_SIZE];
            Array.Copy(iv, feedback, BLOCK_SIZE);

            for (int i = 0; i < data.Length; i += BLOCK_SIZE)
            {
                // Encrypt the feedback register
                byte[] encryptedFeedback = _xxtea.Encrypt(feedback, key);

                // XOR with plaintext
                int blockLength = Math.Min(BLOCK_SIZE, data.Length - i);
                for (int j = 0; j < blockLength; j++)
                {
                    encrypted[i + j] = (byte)(data[i + j] ^ encryptedFeedback[j]);
                }

                // Update feedback register with ciphertext
                if (blockLength == BLOCK_SIZE)
                {
                    Array.Copy(encrypted, i, feedback, 0, BLOCK_SIZE);
                }
                else
                {
                    Array.Copy(encrypted, i, feedback, 0, blockLength);
                    for (int j = blockLength; j < BLOCK_SIZE; j++)
                    {
                        feedback[j] = 0;
                    }
                }
            }

            return encrypted;
        }

        /// <summary>
        /// CFB decryption implementation
        /// </summary>
        private byte[] DecryptCFB(byte[] data, byte[] key, byte[] iv)
        {
            byte[] decrypted = new byte[data.Length];
            byte[] feedback = new byte[BLOCK_SIZE];
            Array.Copy(iv, feedback, BLOCK_SIZE);

            for (int i = 0; i < data.Length; i += BLOCK_SIZE)
            {
                // Encrypt the feedback register (same as encrypt)
                byte[] encryptedFeedback = _xxtea.Encrypt(feedback, key);

                // XOR with ciphertext
                int blockLength = Math.Min(BLOCK_SIZE, data.Length - i);
                for (int j = 0; j < blockLength; j++)
                {
                    decrypted[i + j] = (byte)(data[i + j] ^ encryptedFeedback[j]);
                }

                // Update feedback register with ciphertext
                if (blockLength == BLOCK_SIZE)
                {
                    Array.Copy(data, i, feedback, 0, BLOCK_SIZE);
                }
                else
                {
                    Array.Copy(data, i, feedback, 0, blockLength);
                    for (int j = blockLength; j < BLOCK_SIZE; j++)
                    {
                        feedback[j] = 0;
                    }
                }
            }

            return decrypted;
        }

        public byte[] GenerateIV()
        {
            byte[] iv = new byte[BLOCK_SIZE];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }
            return iv;
        }

        public byte[] GenerateIVFromSeed(int seed)
        {
            byte[] iv = new byte[BLOCK_SIZE];
            Random rnd = new Random(seed);
            rnd.NextBytes(iv);
            return iv;
        }

        public int BlockSize => BLOCK_SIZE;
    }
}
