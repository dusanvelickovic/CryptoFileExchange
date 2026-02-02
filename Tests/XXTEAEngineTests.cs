using System;
using System.Text;
using CryptoFileExchange.Algorithms.Symmetric;

namespace CryptoFileExchange.Tests
{
    internal class XXTEAEngineTests
    {
        private static int _passedTests = 0;
        private static int _failedTests = 0;

        public static (int passed, int failed) RunAllTests()
        {
            _passedTests = 0;
            _failedTests = 0;

            Console.WriteLine("=== XXTEAEngine Test Suite ===\n");

            TestBasicEncryptDecrypt();
            TestEmptyData();
            TestDifferentKeys();
            TestLargeData();
            TestBinaryData();
            TestBlockAlignment();

            PrintSummary();
            return (_passedTests, _failedTests);
        }

        private static void PrintSummary()
        {
            Console.WriteLine("\n=== XXTEAEngine Summary ===");
            Console.WriteLine($"Total Tests: {_passedTests + _failedTests}");
            Console.WriteLine($"Passed: {_passedTests}");
            Console.WriteLine($"Failed: {_failedTests}");
            
            if (_failedTests == 0)
            {
                Console.WriteLine("All tests PASSED!");
            }
            else
            {
                Console.WriteLine($" {_failedTests} test(s) FAILED!");
            }
        }

        private static void Pass(string message)
        {
            _passedTests++;
            Console.WriteLine($"PASSED - {message}");
        }

        private static void Fail(string message)
        {
            _failedTests++;
            Console.WriteLine($"FAILED - {message}");
        }

        private static void TestBasicEncryptDecrypt()
        {
            Console.WriteLine("Test 1: Basic Encrypt/Decrypt");
            try
            {
                XXTEAEngine xxtea = new XXTEAEngine();
                string testData = "XXTEA block cipher test message!";
                string key = "SecretKey123";

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

        private static void TestEmptyData()
        {
            Console.WriteLine("Test 2: Empty Data Handling");
            try
            {
                XXTEAEngine xxtea = new XXTEAEngine();
                byte[] emptyData = new byte[0];
                string key = "TestKey";

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

        private static void TestDifferentKeys()
        {
            Console.WriteLine("Test 3: Different Keys");
            try
            {
                XXTEAEngine xxtea = new XXTEAEngine();
                string testData = "Secret XXTEA message";
                string key1 = "CorrectKey";
                string key2 = "WrongKey";

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

        private static void TestLargeData()
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
                string key = "LargeDataTestKey";

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

        private static void TestBinaryData()
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
                string key = "BinaryTestKey";

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

        private static void TestBlockAlignment()
        {
            Console.WriteLine("Test 6: Block Alignment & Padding");
            try
            {
                XXTEAEngine xxtea = new XXTEAEngine();
                string key = "PaddingTestKey";
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
