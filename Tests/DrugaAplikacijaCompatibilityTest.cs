using System;
using System.Text;
using CryptoFileExchange.Algorithms.Symmetric;
using CryptoFileExchange.Algorithms.BlockCipher;

namespace CryptoFileExchange.Tests
{
    /// <summary>
    /// Test koji pokazuje TA?NO šta se dešava u svakom koraku enkripcije/dekripcije
    /// </summary>
    internal class DrugaAplikacijaCompatibilityTest : TestBase
    {
        protected override string GetTestSuiteName() => "DrugaAplikacija Compatibility";

        // ISTI klju?evi kao u DrugaAplikacija (nakon bugfix-a u Form1.cs linija 225)
        private const string ENIGMA_KEY = "MyEnigmaSecretKey2024";
        private const string XXTEA_KEY = "XXTEAKey12345678";
        private const string CFB_KEY = "CFBModeKey987654";
        private const string CFB_IV = "";  // Prazan = 16 zero bajtova

        public static (int passed, int failed) RunAllTests()
        {
            var instance = new DrugaAplikacijaCompatibilityTest();
            instance.ResetCounters();

            Console.WriteLine("=== DRUGAAPLIKACIJA COMPATIBILITY TEST ===");
            Console.WriteLine("Ovaj test pokazuje TA?NO šta se dešava u svakom koraku.\n");

            instance.TestStepByStepEncryption();
            instance.TestBase64AfterEnigma();
            instance.TestXXTEAOutput();
            instance.TestCFBOutput();
            instance.TestFullChainWithSmallData();

            instance.PrintSummary();
            return instance.GetResults();
        }

        private void TestStepByStepEncryption()
        {
            Console.WriteLine("Test 1: Step-by-Step Encryption");
            try
            {
                // Originalni podaci
                byte[] originalData = Encoding.UTF8.GetBytes("TEST");
                Console.WriteLine($"   Original data: TEST");
                Console.WriteLine($"   Original bytes: {BitConverter.ToString(originalData)}");
                Console.WriteLine($"   Original length: {originalData.Length} bytes\n");

                // KORAK 1: Enigma
                EnigmaEngine enigma = new EnigmaEngine();
                byte[] afterEnigma = enigma.Encrypt(originalData, ENIGMA_KEY);
                Console.WriteLine($"   After Enigma:");
                Console.WriteLine($"   Length: {afterEnigma.Length} bytes");
                string enigmaAsString = Encoding.UTF8.GetString(afterEnigma);
                Console.WriteLine($"   As string: {enigmaAsString.Substring(0, Math.Min(50, enigmaAsString.Length))}...");
                
                // Provera: Da li je validan Base64?
                string base64Original = Convert.ToBase64String(originalData);
                Console.WriteLine($"   Base64 of original: {base64Original}");
                Console.WriteLine($"   Enigma output contains non-A-Z chars: {enigmaAsString.Any(c => !(c >= 'A' && c <= 'Z'))}\n");

                // KORAK 2: XXTEA
                XXTEAEngine xxtea = new XXTEAEngine();
                byte[] xxteaKeyBytes = StringToKeyBytes(XXTEA_KEY, 16);
                Console.WriteLine($"   XXTEA key bytes: {BitConverter.ToString(xxteaKeyBytes)}");
                
                byte[] afterXXTEA = xxtea.Encrypt(afterEnigma, xxteaKeyBytes);
                Console.WriteLine($"   After XXTEA:");
                Console.WriteLine($"   Length: {afterXXTEA.Length} bytes");
                Console.WriteLine($"   First 16 bytes: {BitConverter.ToString(afterXXTEA, 0, Math.Min(16, afterXXTEA.Length))}\n");

                // KORAK 3: CFB
                CFBMode cfb = new CFBMode();
                byte[] cfbIVBytes = StringToKeyBytes(CFB_IV, 16);
                Console.WriteLine($"   CFB IV bytes: {BitConverter.ToString(cfbIVBytes)}");
                
                byte[] afterCFB = cfb.Encrypt(afterXXTEA, CFB_KEY, cfbIVBytes);
                Console.WriteLine($"   After CFB:");
                Console.WriteLine($"   Length: {afterCFB.Length} bytes");
                Console.WriteLine($"   First 16 bytes: {BitConverter.ToString(afterCFB, 0, Math.Min(16, afterCFB.Length))}\n");

                // DEKRIPTOVANJE U OBRNUTOM REDOSLEDU
                Console.WriteLine("   === DECRYPTION (Reverse order) ===\n");

                // KORAK 1: CFB Decrypt
                byte[] afterCFBDecrypt = cfb.Decrypt(afterCFB, CFB_KEY, cfbIVBytes);
                Console.WriteLine($"   After CFB Decrypt:");
                Console.WriteLine($"   Length: {afterCFBDecrypt.Length} bytes");
                bool cfbMatch = CompareBytes(afterCFBDecrypt, afterXXTEA);
                Console.WriteLine($"   Matches XXTEA output: {cfbMatch}");
                if (!cfbMatch)
                {
                    Console.WriteLine($"   Expected: {BitConverter.ToString(afterXXTEA, 0, Math.Min(16, afterXXTEA.Length))}");
                    Console.WriteLine($"   Got:      {BitConverter.ToString(afterCFBDecrypt, 0, Math.Min(16, afterCFBDecrypt.Length))}");
                }
                Console.WriteLine();

                // KORAK 2: XXTEA Decrypt
                byte[] afterXXTEADecrypt = xxtea.Decrypt(afterCFBDecrypt, xxteaKeyBytes);
                Console.WriteLine($"   After XXTEA Decrypt:");
                Console.WriteLine($"   Length: {afterXXTEADecrypt.Length} bytes");
                bool xxteaMatch = CompareBytes(afterXXTEADecrypt, afterEnigma);
                Console.WriteLine($"   Matches Enigma output: {xxteaMatch}");
                if (!xxteaMatch)
                {
                    Console.WriteLine($"   Expected length: {afterEnigma.Length}");
                    Console.WriteLine($"   Got length: {afterXXTEADecrypt.Length}");
                }
                Console.WriteLine();

                // KORAK 3: Enigma Decrypt
                byte[] afterEnigmaDecrypt = enigma.Decrypt(afterXXTEADecrypt, ENIGMA_KEY);
                Console.WriteLine($"   After Enigma Decrypt:");
                Console.WriteLine($"   Length: {afterEnigmaDecrypt.Length} bytes");
                string result = Encoding.UTF8.GetString(afterEnigmaDecrypt);
                Console.WriteLine($"   As string: {result}");
                Console.WriteLine($"   Matches original: {result == "TEST"}\n");

                if (result == "TEST")
                {
                    Pass("Full chain encrypt/decrypt works!");
                }
                else
                {
                    Fail($"Full chain failed. Expected: 'TEST', Got: '{result}'");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
                Console.WriteLine($"   Stack trace:\n{ex.StackTrace}");
            }
            Console.WriteLine();
        }

        private void TestBase64AfterEnigma()
        {
            Console.WriteLine("Test 2: Base64 Validation After Enigma");
            try
            {
                byte[] testData = Encoding.UTF8.GetBytes("Hello World");
                EnigmaEngine enigma = new EnigmaEngine();
                
                byte[] encrypted = enigma.Encrypt(testData, ENIGMA_KEY);
                string encryptedString = Encoding.UTF8.GetString(encrypted);
                
                Console.WriteLine($"   Original: Hello World");
                Console.WriteLine($"   Encrypted string length: {encryptedString.Length}");
                Console.WriteLine($"   Encrypted sample: {encryptedString.Substring(0, Math.Min(30, encryptedString.Length))}");
                
                // Dekriptuj
                byte[] decrypted = enigma.Decrypt(encrypted, ENIGMA_KEY);
                string result = Encoding.UTF8.GetString(decrypted);
                
                if (result == "Hello World")
                {
                    Pass("Enigma standalone works correctly");
                }
                else
                {
                    Fail($"Enigma standalone failed. Got: '{result}'");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception in Enigma: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestXXTEAOutput()
        {
            Console.WriteLine("Test 3: XXTEA Encryption");
            try
            {
                XXTEAEngine xxtea = new XXTEAEngine();
                byte[] testData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
                byte[] key = StringToKeyBytes(XXTEA_KEY, 16);
                
                Console.WriteLine($"   Input:  {BitConverter.ToString(testData)}");
                Console.WriteLine($"   Key:    {BitConverter.ToString(key)}");
                
                byte[] encrypted = xxtea.Encrypt(testData, key);
                Console.WriteLine($"   Output: {BitConverter.ToString(encrypted)}");
                Console.WriteLine($"   Output length: {encrypted.Length} (expected: {testData.Length + 4} with padding)");
                
                byte[] decrypted = xxtea.Decrypt(encrypted, key);
                bool match = CompareBytes(testData, decrypted);
                
                if (match)
                {
                    Pass("XXTEA encrypt/decrypt works");
                }
                else
                {
                    Fail("XXTEA decrypt doesn't match original");
                    Console.WriteLine($"   Expected: {BitConverter.ToString(testData)}");
                    Console.WriteLine($"   Got:      {BitConverter.ToString(decrypted)}");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception in XXTEA: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestCFBOutput()
        {
            Console.WriteLine("Test 4: CFB Mode Encryption");
            try
            {
                CFBMode cfb = new CFBMode();
                byte[] testData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
                byte[] iv = StringToKeyBytes(CFB_IV, 16);
                
                Console.WriteLine($"   Input:  {BitConverter.ToString(testData)}");
                Console.WriteLine($"   IV:     {BitConverter.ToString(iv)}");
                
                byte[] encrypted = cfb.Encrypt(testData, CFB_KEY, iv);
                Console.WriteLine($"   Output: {BitConverter.ToString(encrypted)}");
                Console.WriteLine($"   Output length: {encrypted.Length}");
                
                byte[] decrypted = cfb.Decrypt(encrypted, CFB_KEY, iv);
                bool match = CompareBytes(testData, decrypted);
                
                if (match)
                {
                    Pass("CFB encrypt/decrypt works");
                }
                else
                {
                    Fail("CFB decrypt doesn't match original");
                    Console.WriteLine($"   Expected: {BitConverter.ToString(testData)}");
                    Console.WriteLine($"   Got:      {BitConverter.ToString(decrypted)}");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception in CFB: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestFullChainWithSmallData()
        {
            Console.WriteLine("Test 5: Full Chain with Small Data");
            try
            {
                byte[] originalData = Encoding.UTF8.GetBytes("A");
                Console.WriteLine($"   Original: 'A'");
                Console.WriteLine($"   Original bytes: {BitConverter.ToString(originalData)}");

                // Enkriptuj
                EnigmaEngine enigma = new EnigmaEngine();
                XXTEAEngine xxtea = new XXTEAEngine();
                CFBMode cfb = new CFBMode();

                byte[] step1 = enigma.Encrypt(originalData, ENIGMA_KEY);
                Console.WriteLine($"   After Enigma: {step1.Length} bytes");

                byte[] xxteaKey = StringToKeyBytes(XXTEA_KEY, 16);
                byte[] step2 = xxtea.Encrypt(step1, xxteaKey);
                Console.WriteLine($"   After XXTEA: {step2.Length} bytes");

                byte[] cfbIV = StringToKeyBytes(CFB_IV, 16);
                byte[] encrypted = cfb.Encrypt(step2, CFB_KEY, cfbIV);
                Console.WriteLine($"   After CFB: {encrypted.Length} bytes");

                // Dekriptuj
                byte[] decStep1 = cfb.Decrypt(encrypted, CFB_KEY, cfbIV);
                byte[] decStep2 = xxtea.Decrypt(decStep1, xxteaKey);
                byte[] decrypted = enigma.Decrypt(decStep2, ENIGMA_KEY);

                string result = Encoding.UTF8.GetString(decrypted);
                Console.WriteLine($"   Decrypted: '{result}'");

                if (result == "A")
                {
                    Pass("Full chain with small data works!");
                }
                else
                {
                    Fail($"Expected: 'A', Got: '{result}'");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
                Console.WriteLine($"   Details: {ex.ToString()}");
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

        private bool CompareBytes(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
    }
}
