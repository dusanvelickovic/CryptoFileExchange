using System;
using System.Text;
using CryptoFileExchange.Algorithms.Symmetric;

namespace CryptoFileExchange.Tests
{
    internal class EnigmaEngineTests : TestBase
    {
        protected override string GetTestSuiteName() => "EnigmaEngine";

        public static (int passed, int failed) RunAllTests()
        {
            var instance = new EnigmaEngineTests();
            instance.ResetCounters();

            Console.WriteLine("=== EnigmaEngine Test Suite ===\n");

            instance.TestBasicEncryptDecrypt();
            instance.TestEmptyData();
            instance.TestDifferentKeys();
            instance.TestLargeData();
            instance.TestBinaryData();
            instance.TestSymmetry();

            instance.PrintSummary();
            return instance.GetResults();
        }

        private void TestBasicEncryptDecrypt()
        {
            Console.WriteLine("Test 1: Basic Encrypt/Decrypt");
            try
            {
                EnigmaEngine enigma = new EnigmaEngine();
                string testData = "Hello, this is a test message!";
                string key = "MySecretKey123";

                byte[] plainBytes = Encoding.UTF8.GetBytes(testData);
                byte[] encrypted = enigma.Encrypt(plainBytes, key);
                byte[] decrypted = enigma.Decrypt(encrypted, key);
                string result = Encoding.UTF8.GetString(decrypted);

                if (result == testData)
                {
                    Pass("Data correctly encrypted and decrypted");
                    Console.WriteLine($"   Original: {testData}");
                    Console.WriteLine($"   Encrypted length: {encrypted.Length} bytes");
                    Console.WriteLine($"   Decrypted: {result}");
                }
                else
                {
                    Fail("Decrypted data doesn't match original");
                    Console.WriteLine($"   Expected: {testData}");
                    Console.WriteLine($"   Got: {result}");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestEmptyData()
        {
            Console.WriteLine("Test 2: Empty Data Handling");
            try
            {
                EnigmaEngine enigma = new EnigmaEngine();
                byte[] emptyData = new byte[0];
                string key = "TestKey";

                enigma.Encrypt(emptyData, key);
                Fail("Should throw exception for empty data");
            }
            catch (ArgumentException)
            {
                Pass("Correctly throws exception for empty data");
            }
            catch (Exception ex)
            {
                Fail($"Wrong exception type: {ex.GetType().Name}");
            }
            Console.WriteLine();
        }

        private void TestDifferentKeys()
        {
            Console.WriteLine("Test 3: Different Keys");
            try
            {
                EnigmaEngine enigma = new EnigmaEngine();
                string testData = "Secret message";
                string key1 = "Key1";
                string key2 = "Key2";

                byte[] plainBytes = Encoding.UTF8.GetBytes(testData);
                byte[] encrypted = enigma.Encrypt(plainBytes, key1);
                byte[] decrypted = enigma.Decrypt(encrypted, key2);
                string result = Encoding.UTF8.GetString(decrypted);

                if (result != testData)
                {
                    Pass("Different keys produce different results");
                    Console.WriteLine($"   Original: {testData}");
                    Console.WriteLine($"   With wrong key: {result}");
                }
                else
                {
                    Fail("Same result with different keys");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestLargeData()
        {
            Console.WriteLine("Test 4: Large Data");
            try
            {
                EnigmaEngine enigma = new EnigmaEngine();
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < 1000; i++)
                {
                    sb.Append($"Line {i}: This is a test of large data encryption. ");
                }
                string testData = sb.ToString();
                string key = "LargeDataKey";

                byte[] plainBytes = Encoding.UTF8.GetBytes(testData);
                byte[] encrypted = enigma.Encrypt(plainBytes, key);
                byte[] decrypted = enigma.Decrypt(encrypted, key);
                string result = Encoding.UTF8.GetString(decrypted);

                if (result == testData)
                {
                    Pass("Large data correctly encrypted and decrypted");
                    Console.WriteLine($"   Data size: {plainBytes.Length} bytes ({plainBytes.Length / 1024.0:F2} KB)");
                }
                else
                {
                    Fail("Large data corruption");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestBinaryData()
        {
            Console.WriteLine("Test 5: Binary Data");
            try
            {
                EnigmaEngine enigma = new EnigmaEngine();
                byte[] binaryData = new byte[256];
                for (int i = 0; i < 256; i++)
                {
                    binaryData[i] = (byte)i;
                }
                string key = "BinaryKey";

                byte[] encrypted = enigma.Encrypt(binaryData, key);
                byte[] decrypted = enigma.Decrypt(encrypted, key);

                bool match = true;
                if (decrypted.Length != binaryData.Length)
                {
                    match = false;
                }
                else
                {
                    for (int i = 0; i < binaryData.Length; i++)
                    {
                        if (decrypted[i] != binaryData[i])
                        {
                            match = false;
                            break;
                        }
                    }
                }

                if (match)
                {
                    Pass("Binary data correctly encrypted and decrypted");
                    Console.WriteLine($"   All 256 byte values preserved");
                }
                else
                {
                    Fail("Binary data corruption");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestSymmetry()
        {
            Console.WriteLine("Test 6: Symmetry (Encrypt = Decrypt in Enigma)");
            try
            {
                EnigmaEngine enigma = new EnigmaEngine();
                string testData = "Symmetry Test";
                string key = "SymmetricKey";

                byte[] plainBytes = Encoding.UTF8.GetBytes(testData);
                byte[] encrypted1 = enigma.Encrypt(plainBytes, key);
                byte[] encrypted2 = enigma.Encrypt(encrypted1, key);

                string result = Encoding.UTF8.GetString(encrypted2);

                if (result == testData)
                {
                    Pass("Enigma is symmetric (E(E(M)) = M)");
                    Console.WriteLine($"   Original: {testData}");
                    Console.WriteLine($"   After double encryption: {result}");
                }
                else
                {
                    Fail("Symmetry not working");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }
    }
}
