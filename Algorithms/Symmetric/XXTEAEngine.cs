using System;

namespace CryptoFileExchange.Algorithms.Symmetric
{
    internal class XXTEAEngine
    {
        private const uint DELTA = 0x9E3779B9;
        private const int KEY_SIZE = 4;

        public byte[] Encrypt(byte[] data, byte[] key)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty");

            if (key == null || key.Length != 16)
                throw new ArgumentException("Key must be exactly 16 bytes (128 bits)");

            byte[] paddedData = AddPadding(data);
            uint[] dataBlocks = BytesToUInt32(paddedData);
            uint[] keyBlocks = GenerateKey(key);

            if (dataBlocks.Length < 2)
                throw new ArgumentException("Data too short for XXTEA encryption");

            EncryptBlocks(dataBlocks, keyBlocks);

            return UInt32ToBytes(dataBlocks);
        }

        public byte[] Decrypt(byte[] data, byte[] key)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty");

            if (key == null || key.Length != 16)
                throw new ArgumentException("Key must be exactly 16 bytes (128 bits)");

            if (data.Length % 4 != 0)
                throw new ArgumentException("Invalid encrypted data length");

            uint[] dataBlocks = BytesToUInt32(data);
            uint[] keyBlocks = GenerateKey(key);

            if (dataBlocks.Length < 2)
                throw new ArgumentException("Data too short for XXTEA decryption");

            DecryptBlocks(dataBlocks, keyBlocks);

            byte[] result = UInt32ToBytes(dataBlocks);
            return RemovePadding(result);
        }

        private void EncryptBlocks(uint[] v, uint[] k)
        {
            int n = v.Length;
            uint z = v[n - 1];
            uint y;
            uint sum = 0;
            int q = 6 + 52 / n;

            while (q-- > 0)
            {
                sum += DELTA;
                uint e = (sum >> 2) & 3;

                for (int p = 0; p < n; p++)
                {
                    y = v[(p + 1) % n];
                    z = v[p] += MX(z, y, sum, k[p & 3 ^ e], p, e);
                }
            }
        }

        private void DecryptBlocks(uint[] v, uint[] k)
        {
            int n = v.Length;
            uint z;
            uint y = v[0];
            int q = 6 + 52 / n;
            uint sum = (uint)(q * DELTA);

            while (sum != 0)
            {
                uint e = (sum >> 2) & 3;

                for (int p = n - 1; p >= 0; p--)
                {
                    z = v[p > 0 ? p - 1 : n - 1];
                    y = v[p] -= MX(z, y, sum, k[p & 3 ^ e], p, e);
                }

                sum -= DELTA;
            }
        }

        private uint MX(uint z, uint y, uint sum, uint key, int p, uint e)
        {
            return (((z >> 5) ^ (y << 2)) + ((y >> 3) ^ (z << 4))) 
                   ^ ((sum ^ y) + (key ^ z));
        }

        private uint[] GenerateKey(byte[] key)
        {
            // Key is already validated to be 16 bytes in Encrypt/Decrypt
            uint[] keyBlocks = new uint[KEY_SIZE];

            for (int i = 0; i < KEY_SIZE; i++)
            {
                keyBlocks[i] = BitConverter.ToUInt32(key, i * 4);
            }

            return keyBlocks;
        }

        private byte[] AddPadding(byte[] data)
        {
            // Simple zero-padding (no marker byte)
            int paddingLength = (4 - (data.Length % 4)) % 4;
            
            byte[] padded = new byte[data.Length + paddingLength];
            Array.Copy(data, padded, data.Length);
            // Rest is already zeros (new byte[] initializes to 0)

            return padded;
        }

        private byte[] RemovePadding(byte[] data)
        {
            // Padding (0-3 zero bytes) is not removed after decryption
            // It's handled by Enigma's Base64 decoding which trims trailing nulls
            return data;
        }

        private uint[] BytesToUInt32(byte[] data)
        {
            if (data.Length % 4 != 0)
                throw new ArgumentException("Data length must be multiple of 4");

            uint[] result = new uint[data.Length / 4];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = BitConverter.ToUInt32(data, i * 4);
            }

            return result;
        }

        private byte[] UInt32ToBytes(uint[] data)
        {
            byte[] result = new byte[data.Length * 4];

            for (int i = 0; i < data.Length; i++)
            {
                byte[] bytes = BitConverter.GetBytes(data[i]);
                Array.Copy(bytes, 0, result, i * 4, 4);
            }

            return result;
        }
    }
}
