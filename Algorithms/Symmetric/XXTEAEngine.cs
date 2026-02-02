using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CryptoFileExchange.Algorithms.Symmetric
{
    internal class XXTEAEngine
    {
        private const uint DELTA = 0x9E3779B9;
        private const int KEY_SIZE = 4;

        public byte[] Encrypt(byte[] data, string key)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty");

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty");

            byte[] paddedData = AddPadding(data);
            uint[] dataBlocks = BytesToUInt32(paddedData);
            uint[] keyBlocks = GenerateKey(key);

            if (dataBlocks.Length < 2)
                throw new ArgumentException("Data too short for XXTEA encryption");

            EncryptBlocks(dataBlocks, keyBlocks);

            return UInt32ToBytes(dataBlocks);
        }

        public byte[] Decrypt(byte[] data, string key)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty");

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty");

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

        private uint[] GenerateKey(string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            uint[] keyBlocks = new uint[KEY_SIZE];

            for (int i = 0; i < KEY_SIZE; i++)
            {
                keyBlocks[i] = 0;
                for (int j = 0; j < 4; j++)
                {
                    int index = (i * 4 + j) % keyBytes.Length;
                    keyBlocks[i] |= (uint)keyBytes[index] << (j * 8);
                }
            }

            return keyBlocks;
        }

        private byte[] AddPadding(byte[] data)
        {
            int paddingLength = (4 - (data.Length % 4)) % 4;
            
            if (paddingLength == 0)
                paddingLength = 4;

            byte[] padded = new byte[data.Length + paddingLength];
            Array.Copy(data, padded, data.Length);
            
            padded[data.Length] = 0x80;
            
            for (int i = data.Length + 1; i < padded.Length; i++)
            {
                padded[i] = 0x00;
            }

            return padded;
        }

        private byte[] RemovePadding(byte[] data)
        {
            if (data == null || data.Length == 0)
                return data;

            int paddingStart = data.Length;
            
            for (int i = data.Length - 1; i >= 0; i--)
            {
                if (data[i] == 0x80)
                {
                    paddingStart = i;
                    break;
                }
                else if (data[i] != 0x00)
                {
                    return data;
                }
            }

            byte[] result = new byte[paddingStart];
            Array.Copy(data, result, paddingStart);
            return result;
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
