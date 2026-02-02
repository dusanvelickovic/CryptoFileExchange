using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoFileExchange.Algorithms.Hash
{
    internal class TigerHash
    {
        private const int BLOCK_SIZE = 64;
        private const int HASH_SIZE = 24;

        private static readonly ulong[] T1 = InitializeT1();
        private static readonly ulong[] T2 = InitializeT2();
        private static readonly ulong[] T3 = InitializeT3();
        private static readonly ulong[] T4 = InitializeT4();

        public byte[] ComputeHash(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            byte[] paddedData = PadMessage(data);

            ulong a = 0x0123456789ABCDEFUL;
            ulong b = 0xFEDCBA9876543210UL;
            ulong c = 0xF096A5B4C3B2E187UL;

            for (int i = 0; i < paddedData.Length; i += BLOCK_SIZE)
            {
                ulong[] block = new ulong[8];
                for (int j = 0; j < 8; j++)
                {
                    block[j] = BitConverter.ToUInt64(paddedData, i + j * 8);
                }

                ulong aa = a;
                ulong bb = b;
                ulong cc = c;

                Pass(ref a, ref b, ref c, block, 5);
                KeySchedule(block);
                Pass(ref c, ref a, ref b, block, 7);
                KeySchedule(block);
                Pass(ref b, ref c, ref a, block, 9);

                a ^= aa;
                b = unchecked(b - bb);
                c = unchecked(c + cc);
            }

            byte[] hash = new byte[HASH_SIZE];
            Array.Copy(BitConverter.GetBytes(a), 0, hash, 0, 8);
            Array.Copy(BitConverter.GetBytes(b), 0, hash, 8, 8);
            Array.Copy(BitConverter.GetBytes(c), 0, hash, 16, 8);

            return hash;
        }

        private void Pass(ref ulong a, ref ulong b, ref ulong c, ulong[] x, int mul)
        {
            unchecked
            {
                for (int i = 0; i < 8; i++)
                {
                    c ^= x[i];
                    a = unchecked(a - (T1[(byte)c] ^ T2[(byte)(c >> 16)] ^ T3[(byte)(c >> 32)] ^ T4[(byte)(c >> 48)]));
                    b = unchecked(b + (T4[(byte)(c >> 8)] ^ T3[(byte)(c >> 24)] ^ T2[(byte)(c >> 40)] ^ T1[(byte)(c >> 56)]));
                    b = unchecked(b * (ulong)mul);

                    ulong temp = a;
                    a = b;
                    b = c;
                    c = temp;
                }
            }
        }

        private void KeySchedule(ulong[] x)
        {
            unchecked
            {
                x[0] = unchecked(x[0] - (x[7] ^ 0xA5A5A5A5A5A5A5A5UL));
                x[1] ^= x[0];
                x[2] = unchecked(x[2] + x[1]);
                x[3] = unchecked(x[3] - (x[2] ^ ((~x[1]) << 19)));
                x[4] ^= x[3];
                x[5] = unchecked(x[5] + x[4]);
                x[6] = unchecked(x[6] - (x[5] ^ ((~x[4]) >> 23)));
                x[7] ^= x[6];
                x[0] = unchecked(x[0] + x[7]);
                x[1] = unchecked(x[1] - (x[0] ^ ((~x[7]) << 19)));
                x[2] ^= x[1];
                x[3] = unchecked(x[3] + x[2]);
                x[4] = unchecked(x[4] - (x[3] ^ ((~x[2]) >> 23)));
                x[5] ^= x[4];
                x[6] = unchecked(x[6] + x[5]);
                x[7] = unchecked(x[7] - (x[6] ^ 0x0123456789ABCDEFUL));
            }
        }

        private byte[] PadMessage(byte[] data)
        {
            ulong bitLength = (ulong)data.Length * 8;
            int paddingLength = BLOCK_SIZE - ((data.Length + 9) % BLOCK_SIZE);
            if (paddingLength == BLOCK_SIZE)
                paddingLength = 0;

            byte[] padded = new byte[data.Length + 1 + paddingLength + 8];
            Array.Copy(data, padded, data.Length);
            padded[data.Length] = 0x01;

            byte[] lengthBytes = BitConverter.GetBytes(bitLength);
            Array.Copy(lengthBytes, 0, padded, padded.Length - 8, 8);

            return padded;
        }

        private static ulong[] InitializeT1()
        {
            ulong[] table = new ulong[256];
            Random rnd = new Random(0x01234567);
            for (int i = 0; i < 256; i++)
            {
                table[i] = ((ulong)rnd.Next() << 32) | (ulong)rnd.Next();
            }
            return table;
        }

        private static ulong[] InitializeT2()
        {
            ulong[] table = new ulong[256];
            Random rnd = new Random(unchecked((int)0x89ABCDEF));
            for (int i = 0; i < 256; i++)
            {
                table[i] = ((ulong)rnd.Next() << 32) | (ulong)rnd.Next();
            }
            return table;
        }

        private static ulong[] InitializeT3()
        {
            ulong[] table = new ulong[256];
            Random rnd = new Random(unchecked((int)0xFEDCBA98));
            for (int i = 0; i < 256; i++)
            {
                table[i] = ((ulong)rnd.Next() << 32) | (ulong)rnd.Next();
            }
            return table;
        }

        private static ulong[] InitializeT4()
        {
            ulong[] table = new ulong[256];
            Random rnd = new Random(0x76543210);
            for (int i = 0; i < 256; i++)
            {
                table[i] = ((ulong)rnd.Next() << 32) | (ulong)rnd.Next();
            }
            return table;
        }

        public static string HashToHexString(byte[] hash)
        {
            StringBuilder sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
            {
                sb.AppendFormat("{0:x2}", b);
            }
            return sb.ToString();
        }
    }
}
