using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CryptoFileExchange.Algorithms.Symmetric;

namespace CryptoFileExchange.Algorithms.BlockCipher
{
    internal class CFBMode
    {
        private readonly int _blockSize;
        private const int DEFAULT_BLOCK_SIZE = 16;

        public CFBMode(int blockSize = DEFAULT_BLOCK_SIZE)
        {
            if (blockSize <= 0 || blockSize % 4 != 0)
                throw new ArgumentException("Block size must be positive and multiple of 4");

            _blockSize = blockSize;
        }

        public byte[] Encrypt(byte[] data, string key)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty");

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty");

            byte[] iv = GenerateIV();
            byte[] encrypted = ProcessCFB(data, key, iv, true);

            byte[] result = new byte[iv.Length + encrypted.Length];
            Array.Copy(iv, 0, result, 0, iv.Length);
            Array.Copy(encrypted, 0, result, iv.Length, encrypted.Length);

            return result;
        }

        public byte[] Decrypt(byte[] data, string key)
        {
            if (data == null || data.Length <= _blockSize)
                throw new ArgumentException("Data too short or invalid");

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty");

            byte[] iv = new byte[_blockSize];
            Array.Copy(data, 0, iv, 0, _blockSize);

            byte[] encryptedData = new byte[data.Length - _blockSize];
            Array.Copy(data, _blockSize, encryptedData, 0, encryptedData.Length);

            return ProcessCFB(encryptedData, key, iv, false);
        }

        private byte[] ProcessCFB(byte[] data, string key, byte[] iv, bool encrypt)
        {
            XXTEAEngine cipher = new XXTEAEngine();
            byte[] result = new byte[data.Length];
            byte[] feedbackRegister = new byte[_blockSize];
            Array.Copy(iv, feedbackRegister, _blockSize);

            int position = 0;
            while (position < data.Length)
            {
                int blockLength = Math.Min(_blockSize, data.Length - position);

                byte[] encryptedFeedback = EncryptBlock(feedbackRegister, cipher, key);

                for (int i = 0; i < blockLength; i++)
                {
                    result[position + i] = (byte)(data[position + i] ^ encryptedFeedback[i]);
                }

                if (encrypt)
                {
                    Array.Copy(result, position, feedbackRegister, 0, Math.Min(blockLength, _blockSize));
                    
                    if (blockLength < _blockSize)
                    {
                        Array.Copy(feedbackRegister, blockLength, feedbackRegister, 0, _blockSize - blockLength);
                        Array.Copy(result, position, feedbackRegister, _blockSize - blockLength, blockLength);
                    }
                }
                else
                {
                    Array.Copy(data, position, feedbackRegister, 0, Math.Min(blockLength, _blockSize));
                    
                    if (blockLength < _blockSize)
                    {
                        Array.Copy(feedbackRegister, blockLength, feedbackRegister, 0, _blockSize - blockLength);
                        Array.Copy(data, position, feedbackRegister, _blockSize - blockLength, blockLength);
                    }
                }

                position += blockLength;
            }

            return result;
        }

        private byte[] EncryptBlock(byte[] block, XXTEAEngine cipher, string key)
        {
            try
            {
                byte[] paddedBlock = new byte[block.Length];
                Array.Copy(block, paddedBlock, block.Length);

                if (paddedBlock.Length < 8)
                {
                    byte[] temp = new byte[8];
                    Array.Copy(paddedBlock, temp, paddedBlock.Length);
                    paddedBlock = temp;
                }

                byte[] encrypted = cipher.Encrypt(paddedBlock, key);
                
                if (encrypted.Length > _blockSize)
                {
                    byte[] trimmed = new byte[_blockSize];
                    Array.Copy(encrypted, trimmed, _blockSize);
                    return trimmed;
                }

                return encrypted;
            }
            catch
            {
                byte[] fallback = new byte[_blockSize];
                byte[] keyBytes = Encoding.UTF8.GetBytes(key);
                
                for (int i = 0; i < _blockSize; i++)
                {
                    fallback[i] = (byte)(block[i] ^ keyBytes[i % keyBytes.Length]);
                }
                
                return fallback;
            }
        }

        public byte[] GenerateIV()
        {
            byte[] iv = new byte[_blockSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }
            return iv;
        }

        public byte[] GenerateIVFromSeed(int seed)
        {
            byte[] iv = new byte[_blockSize];
            Random rnd = new Random(seed);
            rnd.NextBytes(iv);
            return iv;
        }

        public int BlockSize => _blockSize;
    }
}
