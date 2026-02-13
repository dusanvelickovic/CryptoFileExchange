using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoFileExchange.Algorithms.Hash
{
    internal class TigerHash
    {
        private const int BLOCK_SIZE = 64; // Velicina bloka: 64 bajta (512 bita)
        private const int HASH_SIZE = 24; // Velicina hesa: 24 bajta (192 bita)

        // Cetiri S-box tabele za nelinearne transformacije (256 elemenata svaka)
        private static readonly ulong[] T1 = InitializeT1();
        private static readonly ulong[] T2 = InitializeT2();
        private static readonly ulong[] T3 = InitializeT3();
        private static readonly ulong[] T4 = InitializeT4();

        public byte[] ComputeHash(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // Dopunjavanje podataka do velicine deljive sa 64 bajta
            byte[] paddedData = PadMessage(data);

            // Inicijalizacija tri 64-bitne promenljive sa predefinisanim vrednostima
            ulong a = 0x0123456789ABCDEFUL;
            ulong b = 0xFEDCBA9876543210UL;
            ulong c = 0xF096A5B4C3B2E187UL;

            // Procesiranje svakog bloka od 64 bajta
            for (int i = 0; i < paddedData.Length; i += BLOCK_SIZE)
            {
                // Konverzija bloka od 64 bajta u 8 blokova po 64 bita
                ulong[] block = new ulong[8];
                for (int j = 0; j < 8; j++)
                {
                    block[j] = BitConverter.ToUInt64(paddedData, i + j * 8);
                }

                // Cuvanje pocetnih vrednosti za kasniju kombinaciju
                ulong aa = a;
                ulong bb = b;
                ulong cc = c;

                // Prvi prolaz: multiplikator 5
                Pass(ref a, ref b, ref c, block, 5);
                KeySchedule(block); // Transformacija bloka između prolaza
                
                // Drugi prolaz: multiplikator 7
                Pass(ref c, ref a, ref b, block, 7);
                KeySchedule(block); // Transformacija bloka između prolaza
                
                // Treci prolaz: multiplikator 9
                Pass(ref b, ref c, ref a, block, 9);

                // Kombinovanje rezultata sa pocetnim vrednostima
                a ^= aa;
                b = unchecked(b - bb);
                c = unchecked(c + cc);
            }

            // Generisanje konacnog hesa: tri 64-bitne vrednosti se konvertuju u bajtove (24 bajta)
            byte[] hash = new byte[HASH_SIZE];
            Array.Copy(BitConverter.GetBytes(a), 0, hash, 0, 8);
            Array.Copy(BitConverter.GetBytes(b), 0, hash, 8, 8);
            Array.Copy(BitConverter.GetBytes(c), 0, hash, 16, 8);

            return hash;
        }

        // Funkcija Pass: prolazak kroz svih 8 blokova podataka sa razlicitim multiplikatorom
        private void Pass(ref ulong a, ref ulong b, ref ulong c, ulong[] x, int mul)
        {
            unchecked
            {
                for (int i = 0; i < 8; i++)
                {
                    // XOR promenljive c sa trenutnim blokom
                    c ^= x[i];
                    
                    // Koriscenje 4 S-box tabele (T1, T2, T3, T4) za nelinearne transformacije
                    // Svaki bajt promenljive c se koristi kao indeks u odgovarajuću S-box tabelu
                    a = unchecked(a - (T1[(byte)c] ^ T2[(byte)(c >> 16)] ^ T3[(byte)(c >> 32)] ^ T4[(byte)(c >> 48)]));
                    b = unchecked(b + (T4[(byte)(c >> 8)] ^ T3[(byte)(c >> 24)] ^ T2[(byte)(c >> 40)] ^ T1[(byte)(c >> 56)]));
                    
                    // Mnozenje sa multiplikatorom (5, 7, ili 9)
                    b = unchecked(b * (ulong)mul);

                    // Rotiranje promenljivih a, b, c
                    ulong temp = a;
                    a = b;
                    b = c;
                    c = temp;
                }
            }
        }

        // KeySchedule funkcija: transformacija bloka između prolaza
        // Mesanje podataka kroz XOR, shift, sabiranje i oduzimanje operacije
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

        // Padding poruke: dopunjavanje podataka do velicine deljive sa 64 bajta
        private byte[] PadMessage(byte[] data)
        {
            // Racunanje duzine poruke u bitovima
            ulong bitLength = (ulong)data.Length * 8;
            
            // Racunanje potrebne duzine paddinga
            int paddingLength = BLOCK_SIZE - ((data.Length + 9) % BLOCK_SIZE);
            if (paddingLength == BLOCK_SIZE)
                paddingLength = 0;

            // Kreiranje novog niza: originalni podaci + 0x01 bajt + padding nule + 8 bajtova dužine
            byte[] padded = new byte[data.Length + 1 + paddingLength + 8];
            Array.Copy(data, padded, data.Length);
            padded[data.Length] = 0x01; // Separator bajt

            // Dodavanje duzine poruke na kraj
            byte[] lengthBytes = BitConverter.GetBytes(bitLength);
            Array.Copy(lengthBytes, 0, padded, padded.Length - 8, 8);

            return padded;
        }

        // Inicijalizacija S-box tabele T1 sa deterministickim pseudoslucajnim vrednostima
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

        // Inicijalizacija S-box tabele T2 sa deterministickim pseudoslucajnim vrednostima
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

        // Inicijalizacija S-box tabele T3 sa deterministickim pseudoslucajnim vrednostima
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

        // Inicijalizacija S-box tabele T4 sa determinističkim pseudoslucajnim vrednostima
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
