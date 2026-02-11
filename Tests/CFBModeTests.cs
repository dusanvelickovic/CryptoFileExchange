using System;
using System.Text;
using CryptoFileExchange.Algorithms.BlockCipher;

namespace CryptoFileExchange.Tests
{
    internal class CFBModeTests : TestBase
    {
        protected override string GetTestSuiteName() => "CFBMode";

        public static (int passed, int failed) RunAllTests()
        {
            var instance = new CFBModeTests();
            instance.ResetCounters();

            Console.WriteLine("=== CFBMode Test Suite ===\n");

            instance.TestBasicEncryptDecrypt();
            instance.TestEmptyData();
            instance.TestDifferentKeys();
            instance.TestLargeData();
            instance.TestIVUniqueness();
            // TestDifferentBlockSizes removed - CFBMode now uses fixed 16-byte blocks (DrugaAplikacija compatibility)

            instance.PrintSummary();
            return instance.GetResults();
        }

        private void TestBasicEncryptDecrypt()
        {
            Console.WriteLine("Test 1: Basic Encrypt/Decrypt");
            try
            {
                CFBMode cfb = new CFBMode();
                string testData = "CFB Mode encryption test!";
                string key = "CFBSecretKey";

                byte[] plainBytes = Encoding.UTF8.GetBytes(testData);
                byte[] encrypted = cfb.Encrypt(plainBytes, key);
                byte[] decrypted = cfb.Decrypt(encrypted, key);
                string result = Encoding.UTF8.GetString(decrypted);

                if (result == testData)
                {
                    Pass("Data correctly encrypted and decrypted");
                    Console.WriteLine($"   Original: {testData}");
                    Console.WriteLine($"   Encrypted length: {encrypted.Length} bytes (includes IV)");
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
                CFBMode cfb = new CFBMode();
                byte[] emptyData = new byte[0];
                string key = "TestKey";

                cfb.Encrypt(emptyData, key);
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
                CFBMode cfb = new CFBMode();
                string testData = "Secret CFB message";
                string key1 = "Key1";
                string key2 = "Key2";

                byte[] plainBytes = Encoding.UTF8.GetBytes(testData);
                byte[] encrypted = cfb.Encrypt(plainBytes, key1);
                
                try
                {
                    byte[] decrypted = cfb.Decrypt(encrypted, key2);
                    string result = Encoding.UTF8.GetString(decrypted);

                    if (result != testData)
                    {
                        Pass("Different keys produce different results");
                        Console.WriteLine($"   Original: {testData}");
                        Console.WriteLine($"   With wrong key: (corrupted data)");
                    }
                    else
                    {
                        Fail("Same result with different keys");
                    }
                }
                catch
                {
                    Pass("Wrong key causes decryption failure");
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
                CFBMode cfb = new CFBMode();
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < 1000; i++)
                {
                    sb.Append($"CFB {i}: Stream cipher mode test. ");
                }
                string testData = sb.ToString();
                string key = "LargeCFBKey";

                byte[] plainBytes = Encoding.UTF8.GetBytes(testData);
                byte[] encrypted = cfb.Encrypt(plainBytes, key);
                byte[] decrypted = cfb.Decrypt(encrypted, key);
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

        private void TestIVUniqueness()
        {
            Console.WriteLine("Test 5: IV Uniqueness");
            try
            {
                CFBMode cfb = new CFBMode();
                string testData = "Same data, different IV";
                string key = "SameKey";

                byte[] plainBytes = Encoding.UTF8.GetBytes(testData);
                byte[] encrypted1 = cfb.Encrypt(plainBytes, key);
                byte[] encrypted2 = cfb.Encrypt(plainBytes, key);

                bool ivsDifferent = false;
                int blockSize = cfb.BlockSize;
                
                for (int i = 0; i < blockSize && i < encrypted1.Length && i < encrypted2.Length; i++)
                {
                    if (encrypted1[i] != encrypted2[i])
                    {
                        ivsDifferent = true;
                        break;
                    }
                }

                if (ivsDifferent)
                {
                    Pass("Each encryption uses unique IV");
                    Console.WriteLine($"   Same plaintext produces different ciphertext");
                }
                else
                {
                    Fail("IVs are identical (security risk)");
                }

                byte[] decrypted1 = cfb.Decrypt(encrypted1, key);
                byte[] decrypted2 = cfb.Decrypt(encrypted2, key);
                string result1 = Encoding.UTF8.GetString(decrypted1);
                string result2 = Encoding.UTF8.GetString(decrypted2);

                if (result1 == testData && result2 == testData)
                {
                    Console.WriteLine("   Both decrypt correctly to original");
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

