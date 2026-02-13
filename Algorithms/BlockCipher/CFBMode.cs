using System;
using System.Security.Cryptography;
using System.Text;
using CryptoFileExchange.Algorithms.Symmetric;

namespace CryptoFileExchange.Algorithms.BlockCipher
{
    /// <summary>
    /// CFB Mode (Cipher Feedback) implementacija
    /// CFB mod pretvara blok sifru u stream šifru
    /// </summary>
    internal class CFBMode
    {
        private readonly XXTEAEngine _xxtea;
        private const int BLOCK_SIZE = 16; // Velicina bloka: 16 bajtova (128 bita)

        public CFBMode()
        {
            _xxtea = new XXTEAEngine();
        }

        /// <summary>
        /// Konverzija stringa u 16-bajtni kljuc (padding ili skracivanje)
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
                // Padding nulama do 16 bajtova
                byte[] padded = new byte[16];
                Array.Copy(keyBytes, padded, keyBytes.Length);
                return padded;
            }
            else
            {
                // Skracivanje na 16 bajtova
                byte[] truncated = new byte[16];
                Array.Copy(keyBytes, truncated, 16);
                return truncated;
            }
        }

        /// <summary>
        /// Enkripcija podataka u CFB modu
        /// </summary>
        public byte[] Encrypt(byte[] data, string key, byte[]? iv = null)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty");

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty");

            // Konverzija string kljuca u 16-bajtni niz
            byte[] keyBytes = StringToKeyBytes(key);

            // Generisanje ili koriscenje postojećeg IV (Initialization Vector)
            // IV mora biti jedinstven za svaku enkripciju
            byte[] actualIV = iv ?? GenerateIV();
            
            if (actualIV.Length != BLOCK_SIZE)
                throw new ArgumentException($"IV must be exactly {BLOCK_SIZE} bytes");

            // Enkripcija koristeci CFB mod
            byte[] encrypted = EncryptCFB(data, keyBytes, actualIV);

            // Ako je IV prosledjen, ne dodaje se u rezultat
            if (iv != null)
            {
                return encrypted;
            }

            // Inace, dodavanje random IV na pocetak rezultata
            byte[] result = new byte[actualIV.Length + encrypted.Length];
            Array.Copy(actualIV, 0, result, 0, actualIV.Length);
            Array.Copy(encrypted, 0, result, actualIV.Length, encrypted.Length);

            return result;
        }

        /// <summary>
        /// Dekripcija podataka u CFB modu
        /// </summary>
        public byte[] Decrypt(byte[] data, string key, byte[]? iv = null)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty");

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty");

            // Konverzija string kljuca u 16-bajtni niz
            byte[] keyBytes = StringToKeyBytes(key);

            byte[] encryptedData;
            byte[] actualIV;

            if (iv != null)
            {
                // IV prosleđen eksplicitno
                actualIV = iv;
                encryptedData = data;
            }
            else
            {
                // Ekstrahovanje IV iz prvih 16 bajtova podataka
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
        /// CFB enkripcija: lanac zavisnosti gde svaki blok zavisi od prethodnog ciphertexta
        /// </summary>
        private byte[] EncryptCFB(byte[] data, byte[] key, byte[] iv)
        {
            byte[] encrypted = new byte[data.Length];
            byte[] feedback = new byte[BLOCK_SIZE];
            Array.Copy(iv, feedback, BLOCK_SIZE); // Inicijalizacija sa IV

            for (int i = 0; i < data.Length; i += BLOCK_SIZE)
            {
                // Enkripcija feedback registra (ne plaintext-a!)
                // Prva iteracija: IV se enkriptuje XXTEA-om
                // Naredne iteracije: prethodni ciphertext blok se enkriptuje
                byte[] encryptedFeedback = _xxtea.Encrypt(feedback, key);

                // XOR operacija: rezultat XOR sa blokom plaintexta
                // Ključna operacija u CFB modu, reverzibilna: (A XOR B) XOR B = A
                int blockLength = Math.Min(BLOCK_SIZE, data.Length - i);
                for (int j = 0; j < blockLength; j++)
                {
                    encrypted[i + j] = (byte)(data[i + j] ^ encryptedFeedback[j]);
                }

                // Azuriranje feedback registra sa ciphertextom (lanac zavisnosti)
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
        /// CFB dekripcija: ista logika kao enkripcija (feedback se ENKRIPTUJE, ne dekriptuje!)
        /// </summary>
        private byte[] DecryptCFB(byte[] data, byte[] key, byte[] iv)
        {
            byte[] decrypted = new byte[data.Length];
            byte[] feedback = new byte[BLOCK_SIZE];
            Array.Copy(iv, feedback, BLOCK_SIZE); // Inicijalizacija sa IV

            for (int i = 0; i < data.Length; i += BLOCK_SIZE)
            {
                // Enkripcija feedback registra (isti proces kao kod enkripcije!)
                // Prva iteracija: IV se enkriptuje (NE dekriptuje!)
                // Naredne iteracije: prethodni ciphertext blok se enkriptuje
                byte[] encryptedFeedback = _xxtea.Encrypt(feedback, key);

                // XOR operacija: rezultat XOR sa blokom ciphertexta
                // Zbog osobine XOR: (A XOR B) XOR B = A, dobijamo originalni plaintext
                int blockLength = Math.Min(BLOCK_SIZE, data.Length - i);
                for (int j = 0; j < blockLength; j++)
                {
                    decrypted[i + j] = (byte)(data[i + j] ^ encryptedFeedback[j]);
                }

                // Azuriranje feedback registra sa ciphertextom (lanac zavisnosti)
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

        // Generisanje random IV (Initialization Vector) od 16 bajtova
        public byte[] GenerateIV()
        {
            byte[] iv = new byte[BLOCK_SIZE];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }
            return iv;
        }

        // Generisanje deterministickog IV iz seed-a (za testiranje)
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
