using System;
using System.Text;
using CryptoFileExchange.Algorithms.Symmetric;

namespace CryptoFileExchange.Tests
{
    internal class XXTEAEngineTests : TestBase
    {
        protected override string GetTestSuiteName() => "XXTEAEngine";

        /// <summary>
        /// Helper method: Convert string to 16-byte key array (padding/truncating)
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
                // Padding with zeros
                byte[] padded = new byte[16];
                Array.Copy(keyBytes, padded, keyBytes.Length);
                return padded;
            }
            else
            {
                // Truncating to 16 bytes
                byte[] truncated = new byte[16];
                Array.Copy(keyBytes, truncated, 16);
                return truncated;
            }
        }

        public static (int passed, int failed) RunAllTests()
        {
            var instance = new XXTEAEngineTests();
            instance.ResetCounters();

            Console.WriteLine("=== XXTEAEngine Test Suite ===\n");

            instance.TestBasicEncryptDecrypt();
            instance.TestEmptyData();
            instance.TestDifferentKeys();
            instance.TestLargeData();
            instance.TestBinaryData();
            instance.TestBlockAlignment();

            instance.PrintSummary();
            return instance.GetResults();
        }

        private void TestBasicEncryptDecrypt()
        {
            Console.WriteLine("Test 1: Basic Encrypt/Decrypt");
            try
            {
                XXTEAEngine xxtea = new XXTEAEngine();
                string testData = "XXTEA block cipher test message!";
                byte[] key = StringToKeyBytes("SecretKey123");

                byte[] plainBytes = Encoding.UTF8.GetBytes(testData);
                byte[] encrypted = xxtea.Encrypt(plainBytes, key);
                byte[] decrypted = xxtea.Decrypt(encrypted, key);
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
                XXTEAEngine xxtea = new XXTEAEngine();
                byte[] emptyData = new byte[0];
                byte[] key = StringToKeyBytes("TestKey");

                xxtea.Encrypt(emptyData, key);
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
                XXTEAEngine xxtea = new XXTEAEngine();
                string testData = "Secret XXTEA message";
                byte[] key1 = StringToKeyBytes("CorrectKey");
                byte[] key2 = StringToKeyBytes("WrongKey");

                byte[] plainBytes = Encoding.UTF8.GetBytes(testData);
                byte[] encrypted = xxtea.Encrypt(plainBytes, key1);
                
                try
                {
                    byte[] decrypted = xxtea.Decrypt(encrypted, key2);
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
                    Pass("Wrong key causes decryption failure/corruption");
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
                XXTEAEngine xxtea = new XXTEAEngine();
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < 500; i++)
                {
                    sb.Append($"Block {i}: XXTEA is a block cipher algorithm. ");
                }
                string testData = sb.ToString();
                byte[] key = StringToKeyBytes("LargeDataTestKey");

                byte[] plainBytes = Encoding.UTF8.GetBytes(testData);
                byte[] encrypted = xxtea.Encrypt(plainBytes, key);
                byte[] decrypted = xxtea.Decrypt(encrypted, key);
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
                XXTEAEngine xxtea = new XXTEAEngine();
                byte[] binaryData = new byte[128];
                for (int i = 0; i < 128; i++)
                {
                    binaryData[i] = (byte)(i * 2);
                }
                byte[] key = StringToKeyBytes("BinaryTestKey");

                byte[] encrypted = xxtea.Encrypt(binaryData, key);
                byte[] decrypted = xxtea.Decrypt(encrypted, key);

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
                    Console.WriteLine($"   {binaryData.Length} bytes preserved");
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

        private void TestBlockAlignment()
        {
            Console.WriteLine("Test 6: Block Alignment & Padding");
            try
            {
                XXTEAEngine xxtea = new XXTEAEngine();
                byte[] key = StringToKeyBytes("PaddingTestKey");
                bool allPassed = true;

                for (int length = 5; length <= 25; length++)
                {
                    byte[] data = new byte[length];
                    for (int i = 0; i < length; i++)
                    {
                        data[i] = (byte)(i + length);
                    }

                    byte[] encrypted = xxtea.Encrypt(data, key);
                    byte[] decrypted = xxtea.Decrypt(encrypted, key);

                    if (decrypted.Length != data.Length)
                    {
                        allPassed = false;
                        break;
                    }

                    for (int i = 0; i < data.Length; i++)
                    {
                        if (decrypted[i] != data[i])
                        {
                            allPassed = false;
                            break;
                        }
                    }
                }

                if (allPassed)
                {
                    Pass("All data lengths (5-25 bytes) correctly handled");
                    Console.WriteLine("   Padding mechanism working correctly");
                }
                else
                {
                    Fail("Padding issue detected");
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
