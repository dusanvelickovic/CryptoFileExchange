using System;

namespace CryptoFileExchange.Algorithms.Symmetric
{
    internal class XXTEAEngine
    {
        // Delta konstanta bazirana na zlatnom preseku (golden ratio)
        private const uint DELTA = 0x9E3779B9;
        // Broj blokova kljuca (4 bloka po 32 bita = 128 bita ukupno)
        private const int KEY_SIZE = 4;

        public byte[] Encrypt(byte[] data, byte[] key)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty");

            if (key == null || key.Length != 16)
                throw new ArgumentException("Key must be exactly 16 bytes (128 bits)");

            // Priprema podataka: dodavanje paddinga da duzina bude deljiva sa 4
            byte[] paddedData = AddPadding(data);
            
            // Konverzija u niz 32-bitnih unsigned integera
            uint[] dataBlocks = BytesToUInt32(paddedData);
            
            // Podela kljuca od 16 bajtova u 4 bloka po 32 bita
            uint[] keyBlocks = GenerateKey(key);

            if (dataBlocks.Length < 2)
                throw new ArgumentException("Data too short for XXTEA encryption");

            // Enkripcija blokova
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

            // Konverzija bajtova u 32-bitne blokove
            uint[] dataBlocks = BytesToUInt32(data);
            
            // Podela kljuca od 16 bajtova u 4 bloka po 32 bita
            uint[] keyBlocks = GenerateKey(key);

            if (dataBlocks.Length < 2)
                throw new ArgumentException("Data too short for XXTEA decryption");

            // Dekripcija blokova (ista logika kao enkripcija ali u obrnutom redosledu)
            DecryptBlocks(dataBlocks, keyBlocks);

            byte[] result = UInt32ToBytes(dataBlocks);
            
            // Uklanjanje paddinga nakon dekripcije
            return RemovePadding(result);
        }

        // Enkripcija blokova: svaki blok zavisi od prethodnog i sledećeg
        private void EncryptBlocks(uint[] v, uint[] k)
        {
            int n = v.Length;
            uint z = v[n - 1]; // Poslednji blok (za zavisnost)
            uint y;
            uint sum = 0; // Akumulator za delta vrednost
            
            // Broj rundi: q = 6 + 52/n (vise rundi za manje blokove)
            int q = 6 + 52 / n;

            // Izvrsavanje rundi enkripcije
            while (q-- > 0)
            {
                // Povecavanje sume za delta u svakoj rundi
                sum += DELTA;
                uint e = (sum >> 2) & 3; // Deo sume za selekciju kljuca

                // Procesiranje svakog bloka
                for (int p = 0; p < n; p++)
                {
                    y = v[(p + 1) % n]; // Sledeci blok (zavisnost)
                    
                    // MX funkcija za permutacije i substitucije, rezultat se dodaje trenutnom bloku
                    z = v[p] += MX(z, y, sum, k[p & 3 ^ e], p, e);
                }
            }
        }

        // Dekripcija blokova: ista logika kao enkripcija ali u obrnutom redosledu
        private void DecryptBlocks(uint[] v, uint[] k)
        {
            int n = v.Length;
            uint z;
            uint y = v[0]; // Prvi blok (za zavisnost)
            
            // Broj rundi: isti kao kod enkripcije
            int q = 6 + 52 / n;
            
            // Suma se racuna kao q * DELTA i smanjuje se
            uint sum = (uint)(q * DELTA);

            // Izvrsavanje rundi dekripcije unazad
            while (sum != 0)
            {
                uint e = (sum >> 2) & 3; // Deo sume za selekciju kljuca

                // Prolazak kroz blokove unazad
                for (int p = n - 1; p >= 0; p--)
                {
                    z = v[p > 0 ? p - 1 : n - 1]; // Prethodni blok (zavisnost)
                    
                    // MX funkcija za permutacije i substitucije, rezultat se oduzima od trenutnog bloka
                    y = v[p] -= MX(z, y, sum, k[p & 3 ^ e], p, e);
                }

                // Smanjivanje sume za delta
                sum -= DELTA;
            }
        }

        // MX funkcija: kombinacija shift operacija, XOR operacija i mesanja sa sumom i kljucem
        // Formula: ((z>>5 XOR y<<2) + (y>>3 XOR z<<4)) XOR ((sum XOR y) + (key XOR z))
        private uint MX(uint z, uint y, uint sum, uint key, int p, uint e)
        {
            // Shift operacije (pomeranje bitova) i XOR između susednih blokova
            // Mesanje sa sumom i ključem za dodatnu difuziju
            return (((z >> 5) ^ (y << 2)) + ((y >> 3) ^ (z << 4))) 
                   ^ ((sum ^ y) + (key ^ z));
        }

        // Podela ključa od 16 bajtova u 4 bloka po 32 bita
        private uint[] GenerateKey(byte[] key)
        {
            // Kljuc je vec validiran da bude 16 bajtova u Encrypt/Decrypt metodama
            uint[] keyBlocks = new uint[KEY_SIZE];

            // Konverzija svakog 4-bajtnog segmenta u 32-bitni unsigned integer
            for (int i = 0; i < KEY_SIZE; i++)
            {
                keyBlocks[i] = BitConverter.ToUInt32(key, i * 4);
            }

            return keyBlocks;
        }

        // Dodavanje paddinga: duzina mora biti deljiva sa 4 (za 32-bitne blokove)
        private byte[] AddPadding(byte[] data)
        {
            // Racunanje potrebnog paddinga (0-3 bajta)
            int paddingLength = (4 - (data.Length % 4)) % 4;
            
            // Kreiranje novog niza sa paddingom
            byte[] padded = new byte[data.Length + paddingLength];
            Array.Copy(data, padded, data.Length);
            // Preostali bajtovi su vec nule (new byte[] inicijalizuje na 0)

            return padded;
        }

        // Uklanjanje paddinga nakon dekripcije
        private byte[] RemovePadding(byte[] data)
        {
            // Padding (0-3 nula bajta) se ne uklanja nakon dekripcije
            // To se resava Base64 dekodiranjem u Enigma koje uklanja trailing nule
            return data;
        }

        // Konverzija bajtova u niz 32-bitnih unsigned integera
        private uint[] BytesToUInt32(byte[] data)
        {
            if (data.Length % 4 != 0)
                throw new ArgumentException("Data length must be multiple of 4");

            uint[] result = new uint[data.Length / 4];

            // Svaki 4-bajtni segment se konvertuje u jedan 32-bitni uint
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = BitConverter.ToUInt32(data, i * 4);
            }

            return result;
        }

        // Konverzija niza 32-bitnih unsigned integera nazad u bajtove
        private byte[] UInt32ToBytes(uint[] data)
        {
            byte[] result = new byte[data.Length * 4];

            // Svaki 32-bitni uint se konvertuje u 4 bajta
            for (int i = 0; i < data.Length; i++)
            {
                byte[] bytes = BitConverter.GetBytes(data[i]);
                Array.Copy(bytes, 0, result, i * 4, 4);
            }

            return result;
        }
    }
}
