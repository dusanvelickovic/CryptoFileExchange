using System;
using System.Text;
using CryptoFileExchange.Algorithms.Symmetric;
using CryptoFileExchange.Algorithms.BlockCipher;

namespace CryptoFileExchange.Tests
{
    /// <summary>
    /// Test kompatibilnosti sa DrugaAplikacija
    /// Ovaj test pokazuje TA?NO šta se dešava u svakom koraku enkripcije/dekripcije
    /// </summary>
    internal class CompatibilityDebugTest : TestBase
    {
        protected override string GetTestSuiteName() => "Compatibility Debug";

        public static (int passed, int failed) RunAllTests()
        {
            var instance = new CompatibilityDebugTest();
            instance.ResetCounters();

            Console.WriteLine("=== COMPATIBILITY DEBUG TEST ===\n");
            Console.WriteLine("Ovaj test pokazuje detalje enkripcije/dekripcije za debug.\n");

            instance.TestEnigmaOnly();
            instance.TestFullChain();
            instance.TestBase64Characters();

            instance.PrintSummary();
            return instance.GetResults();
        }

        private void TestEnigmaOnly()
        {
            Console.WriteLine("Test 1: Enigma Only (izolovano testiranje)");
            try
            {
                string testKey = "MyEnigmaSecretKey2024";
                byte[] testData = Encoding.UTF8.GetBytes("Hello World!");

                Console.WriteLine($"   Original data: {Encoding.UTF8.GetString(testData)}");
                Console.WriteLine($"   Original bytes length: {testData.Length}");

                EnigmaEngine enigma = new EnigmaEngine();

                // Encrypt
                byte[] encrypted = enigma.Encrypt(testData, testKey);
                Console.WriteLine($"   Encrypted bytes length: {encrypted.Length}");
                
                // Prikazi Base64 string koji se enkriptuje
                string base64Original = Convert.ToBase64String(testData);
                Console.WriteLine($"   Base64 representation: {base64Original}");
                Console.WriteLine($"   Base64 length: {base64Original.Length}");

                // Dekriptiraj enkriptirane bajtove nazad u string
                string encryptedAsString = Encoding.UTF8.GetString(encrypted);
                Console.WriteLine($"   Encrypted as string: {encryptedAsString.Substring(0, Math.Min(50, encryptedAsString.Length))}...");

                // Decrypt
                byte[] decrypted = enigma.Decrypt(encrypted, testKey);
                string result = Encoding.UTF8.GetString(decrypted);

                if (result == "Hello World!")
                {
                    Pass("Enigma encrypt/decrypt radi ispravno");
                    Console.WriteLine($"   Decrypted: {result}");
                }
                else
                {
                    Fail($"Enigma decrypt ne radi. Ocekivano: 'Hello World!', Dobijeno: '{result}'");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
            Console.WriteLine();
        }

        private void TestFullChain()
        {
            Console.WriteLine("Test 2: Full Chain (Enigma -> XXTEA -> CFB)");
            try
            {
                string enigmaKey = "MyEnigmaSecretKey2024";
                string xxteaKey = "XXTEAKey12345678";
                string cfbKey = "CFBModeKey987654";
                string cfbIV = "InitVector000000";

                byte[] testData = Encoding.UTF8.GetBytes("Test file content");
                Console.WriteLine($"   Original: {Encoding.UTF8.GetString(testData)}");

                // Encrypt
                EnigmaEngine enigma = new EnigmaEngine();
                byte[] step1 = enigma.Encrypt(testData, enigmaKey);
                Console.WriteLine($"   After Enigma: {step1.Length} bytes");

                XXTEAEngine xxtea = new XXTEAEngine();
                byte[] xxteaKeyBytes = StringToKeyBytes(xxteaKey, 16);
                byte[] step2 = xxtea.Encrypt(step1, xxteaKeyBytes);
                Console.WriteLine($"   After XXTEA: {step2.Length} bytes");

                CFBMode cfb = new CFBMode();
                byte[] cfbIVBytes = StringToKeyBytes(cfbIV, 16);
                byte[] encrypted = cfb.Encrypt(step2, cfbKey, cfbIVBytes);
                Console.WriteLine($"   After CFB: {encrypted.Length} bytes");

                // Decrypt (reverse order)
                byte[] decStep1 = cfb.Decrypt(encrypted, cfbKey, cfbIVBytes);
                Console.WriteLine($"   After CFB Decrypt: {decStep1.Length} bytes");

                byte[] decStep2 = xxtea.Decrypt(decStep1, xxteaKeyBytes);
                Console.WriteLine($"   After XXTEA Decrypt: {decStep2.Length} bytes");

                byte[] decrypted = enigma.Decrypt(decStep2, enigmaKey);
                Console.WriteLine($"   After Enigma Decrypt: {decrypted.Length} bytes");

                string result = Encoding.UTF8.GetString(decrypted);

                if (result == "Test file content")
                {
                    Pass("Full chain encrypt/decrypt radi ispravno");
                    Console.WriteLine($"   Decrypted: {result}");
                }
                else
                {
                    Fail($"Full chain ne radi. Ocekivano: 'Test file content', Dobijeno: '{result}'");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
                Console.WriteLine($"   Details: {ex.ToString()}");
            }
            Console.WriteLine();
        }

        private void TestBase64Characters()
        {
            Console.WriteLine("Test 3: Base64 Character Handling");
            try
            {
                // Test data koji ?e proizvesti sve Base64 karaktere (A-Z, a-z, 0-9, +, /, =)
                byte[] testData = new byte[100];
                for (int i = 0; i < 100; i++)
                {
                    testData[i] = (byte)(i * 2);
                }

                string base64 = Convert.ToBase64String(testData);
                Console.WriteLine($"   Base64 string: {base64.Substring(0, 50)}...");
                Console.WriteLine($"   Contains lowercase: {base64.Any(c => c >= 'a' && c <= 'z')}");
                Console.WriteLine($"   Contains digits: {base64.Any(c => c >= '0' && c <= '9')}");
                Console.WriteLine($"   Contains +: {base64.Contains('+')}");
                Console.WriteLine($"   Contains /: {base64.Contains('/')}");
                Console.WriteLine($"   Contains =: {base64.Contains('=')}");

                EnigmaEngine enigma = new EnigmaEngine();
                string key = "TestKey";

                byte[] encrypted = enigma.Encrypt(testData, key);
                string encryptedString = Encoding.UTF8.GetString(encrypted);

                Console.WriteLine($"   Encrypted string sample: {encryptedString.Substring(0, Math.Min(50, encryptedString.Length))}...");
                
                // Provera: Enigma enkriptuje samo A-Z, ostali karakteri ostaju netaknuti
                int unchangedCount = 0;
                for (int i = 0; i < Math.Min(base64.Length, encryptedString.Length); i++)
                {
                    char original = base64[i];
                    char encrypted_char = encryptedString[i];
                    
                    if (!(original >= 'A' && original <= 'Z'))
                    {
                        if (original == encrypted_char)
                        {
                            unchangedCount++;
                        }
                    }
                }

                Console.WriteLine($"   Non-uppercase chars unchanged: {unchangedCount}");

                byte[] decrypted = enigma.Decrypt(encrypted, key);
                
                bool match = true;
                if (decrypted.Length != testData.Length)
                {
                    match = false;
                }
                else
                {
                    for (int i = 0; i < testData.Length; i++)
                    {
                        if (decrypted[i] != testData[i])
                        {
                            match = false;
                            break;
                        }
                    }
                }

                if (match)
                {
                    Pass("Base64 character handling radi ispravno");
                }
                else
                {
                    Fail("Base64 character handling problem");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
                Console.WriteLine($"   Error: {ex.ToString()}");
            }
            Console.WriteLine();
        }

        private byte[] StringToKeyBytes(string keyString, int targetLength)
        {
            if (string.IsNullOrEmpty(keyString))
            {
                return new byte[targetLength];
            }

            byte[] keyBytes = Encoding.UTF8.GetBytes(keyString);

            if (keyBytes.Length == targetLength)
            {
                return keyBytes;
            }
            else if (keyBytes.Length < targetLength)
            {
                byte[] padded = new byte[targetLength];
                Array.Copy(keyBytes, padded, keyBytes.Length);
                return padded;
            }
            else
            {
                byte[] truncated = new byte[targetLength];
                Array.Copy(keyBytes, truncated, targetLength);
                return truncated;
            }
        }
    }
}
