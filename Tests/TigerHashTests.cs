using System;
using System.Text;
using CryptoFileExchange.Algorithms.Hash;

namespace CryptoFileExchange.Tests
{
    internal class TigerHashTests : TestBase
    {
        protected override string GetTestSuiteName() => "TigerHash";

        public static (int passed, int failed) RunAllTests()
        {
            var instance = new TigerHashTests();
            instance.ResetCounters();

            Console.WriteLine("=== TigerHash Test Suite ===\n");

            instance.TestBasicHashing();
            instance.TestEmptyData();
            instance.TestConsistency();
            instance.TestDifferentData();
            instance.TestLargeData();
            instance.TestBinaryData();

            instance.PrintSummary();
            return instance.GetResults();
        }

        private void TestBasicHashing()
        {
            Console.WriteLine("Test 1: Basic Hashing");
            try
            {
                TigerHash tiger = new TigerHash();
                string testData = "Tiger hash algorithm test";
                
                byte[] dataBytes = Encoding.UTF8.GetBytes(testData);
                byte[] hash = tiger.ComputeHash(dataBytes);

                if (hash != null && hash.Length == 24)
                {
                    Pass("Hash computed successfully");
                    Console.WriteLine($"   Input: {testData}");
                    Console.WriteLine($"   Hash length: {hash.Length} bytes (192 bits)");
                    Console.WriteLine($"   Hash (hex): {TigerHash.HashToHexString(hash)}");
                }
                else
                {
                    Fail("Invalid hash length");
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
                TigerHash tiger = new TigerHash();
                byte[] emptyData = new byte[0];

                byte[] hash = tiger.ComputeHash(emptyData);

                if (hash != null && hash.Length == 24)
                {
                    Pass("Empty data produces valid hash");
                    Console.WriteLine($"   Hash (hex): {TigerHash.HashToHexString(hash)}");
                }
                else
                {
                    Fail("Invalid hash for empty data");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestConsistency()
        {
            Console.WriteLine("Test 3: Consistency (Same Input = Same Hash)");
            try
            {
                TigerHash tiger = new TigerHash();
                string testData = "Consistency test data";
                byte[] dataBytes = Encoding.UTF8.GetBytes(testData);

                byte[] hash1 = tiger.ComputeHash(dataBytes);
                byte[] hash2 = tiger.ComputeHash(dataBytes);

                bool identical = true;
                if (hash1.Length != hash2.Length)
                {
                    identical = false;
                }
                else
                {
                    for (int i = 0; i < hash1.Length; i++)
                    {
                        if (hash1[i] != hash2[i])
                        {
                            identical = false;
                            break;
                        }
                    }
                }

                if (identical)
                {
                    Pass("Same input produces identical hash");
                    Console.WriteLine($"   Input: {testData}");
                    Console.WriteLine($"   Hash: {TigerHash.HashToHexString(hash1)}");
                }
                else
                {
                    Fail("Same input produces different hashes");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestDifferentData()
        {
            Console.WriteLine("Test 4: Different Data = Different Hash");
            try
            {
                TigerHash tiger = new TigerHash();
                string data1 = "First test string";
                string data2 = "Second test string";

                byte[] hash1 = tiger.ComputeHash(Encoding.UTF8.GetBytes(data1));
                byte[] hash2 = tiger.ComputeHash(Encoding.UTF8.GetBytes(data2));

                bool different = false;
                for (int i = 0; i < hash1.Length; i++)
                {
                    if (hash1[i] != hash2[i])
                    {
                        different = true;
                        break;
                    }
                }

                if (different)
                {
                    Pass("Different inputs produce different hashes");
                    Console.WriteLine($"   Input 1: {data1}");
                    Console.WriteLine($"   Hash 1:  {TigerHash.HashToHexString(hash1)}");
                    Console.WriteLine($"   Input 2: {data2}");
                    Console.WriteLine($"   Hash 2:  {TigerHash.HashToHexString(hash2)}");
                }
                else
                {
                    Fail("Different inputs produce same hash (collision!)");
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
            Console.WriteLine("Test 5: Large Data Hashing");
            try
            {
                TigerHash tiger = new TigerHash();
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < 10000; i++)
                {
                    sb.Append($"Block {i} ");
                }
                string testData = sb.ToString();

                byte[] dataBytes = Encoding.UTF8.GetBytes(testData);
                byte[] hash = tiger.ComputeHash(dataBytes);

                if (hash != null && hash.Length == 24)
                {
                    Pass("Large data hashed successfully");
                    Console.WriteLine($"   Data size: {dataBytes.Length} bytes ({dataBytes.Length / 1024.0:F2} KB)");
                    Console.WriteLine($"   Hash (hex): {TigerHash.HashToHexString(hash)}");
                }
                else
                {
                    Fail("Large data hashing failed");
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
            Console.WriteLine("Test 6: Binary Data Hashing");
            try
            {
                TigerHash tiger = new TigerHash();
                byte[] binaryData = new byte[512];
                
                for (int i = 0; i < binaryData.Length; i++)
                {
                    binaryData[i] = (byte)(i % 256);
                }

                byte[] hash = tiger.ComputeHash(binaryData);

                if (hash != null && hash.Length == 24)
                {
                    Pass("Binary data hashed successfully");
                    Console.WriteLine($"   Binary data: {binaryData.Length} bytes (all 256 byte values)");
                    Console.WriteLine($"   Hash (hex): {TigerHash.HashToHexString(hash)}");
                }
                else
                {
                    Fail("Binary data hashing failed");
                }

                byte[] hash2 = tiger.ComputeHash(binaryData);
                bool consistent = true;
                for (int i = 0; i < hash.Length; i++)
                {
                    if (hash[i] != hash2[i])
                    {
                        consistent = false;
                        break;
                    }
                }

                if (consistent)
                {
                    Console.WriteLine("   Binary data produces consistent hash");
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
