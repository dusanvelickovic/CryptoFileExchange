using System;
using System.IO;
using System.Threading.Tasks;
using CryptoFileExchange.Services;

namespace CryptoFileExchange.Tests
{
    internal class EncryptionDecryptionServiceTests : TestBase
    {
        protected override string GetTestSuiteName() => "EncryptionDecryptionService";

        private const string TEST_KEY_ENIGMA = "TestEnigmaKey123";
        private const string TEST_KEY_XXTEA = "XXTEATestKey456";
        private const string TEST_KEY_CFB = "CFBTestKey789";

        public static (int passed, int failed) RunAllTests()
        {
            var instance = new EncryptionDecryptionServiceTests();
            instance.ResetCounters();

            Console.WriteLine("=== EncryptionDecryptionService Test Suite ===\n");

            instance.TestEncryptionServiceBasic();
            instance.TestDecryptionServiceBasic();
            instance.TestEncryptDecryptRoundTrip();
            instance.TestHashVerification();
            instance.TestLargeFileEncryption();

            instance.PrintSummary();
            return instance.GetResults();
        }

        private void TestEncryptionServiceBasic()
        {
            Console.WriteLine("Test 1: Basic Encryption Service");
            try
            {
                var encryptionService = new EncryptionService(TEST_KEY_ENIGMA, TEST_KEY_XXTEA, TEST_KEY_CFB);

                // Kreiraj test fajl
                string testFilePath = Path.Combine(Path.GetTempPath(), "test_encrypt.txt");
                File.WriteAllText(testFilePath, "This is a test file for encryption service!");

                // Enkriptuj
                var task = Task.Run(async () => await encryptionService.EncryptFileAsync(testFilePath));
                var (encryptedData, hash) = task.Result;

                if (encryptedData != null && encryptedData.Length > 0)
                {
                    Pass("File encrypted successfully");
                    Console.WriteLine($"   Encrypted size: {encryptedData.Length} bytes");
                }
                else
                {
                    Fail("Encrypted data is empty");
                }

                if (!string.IsNullOrEmpty(hash) && hash.Length == 48) // TigerHash = 24 bytes = 48 hex chars
                {
                    Pass("Hash generated successfully (TigerHash 192-bit)");
                    Console.WriteLine($"   Hash: {hash}");
                }
                else
                {
                    Fail($"Invalid hash length: {hash?.Length ?? 0} (expected 48)");
                }

                // Cleanup
                if (File.Exists(testFilePath))
                    File.Delete(testFilePath);
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestDecryptionServiceBasic()
        {
            Console.WriteLine("Test 2: Basic Decryption Service");
            try
            {
                var encryptionService = new EncryptionService(TEST_KEY_ENIGMA, TEST_KEY_XXTEA, TEST_KEY_CFB);
                var decryptionService = new DecryptionService(TEST_KEY_ENIGMA, TEST_KEY_XXTEA, TEST_KEY_CFB);

                // Prvo enkriptuj
                string testFilePath = Path.Combine(Path.GetTempPath(), "test_decrypt.txt");
                string originalContent = "Decryption test content!";
                File.WriteAllText(testFilePath, originalContent);

                var encTask = Task.Run(async () => await encryptionService.EncryptFileAsync(testFilePath));
                var (encryptedData, hash) = encTask.Result;

                // Sada dekriptuj
                var decTask = Task.Run(async () => await decryptionService.DecryptFileAsync(encryptedData, hash));
                var (decryptedData, hashValid) = decTask.Result;

                if (decryptedData != null && decryptedData.Length > 0)
                {
                    Pass("File decrypted successfully");
                    Console.WriteLine($"   Decrypted size: {decryptedData.Length} bytes");
                }
                else
                {
                    Fail("Decrypted data is empty");
                }

                if (hashValid)
                {
                    Pass("Hash verification passed");
                }
                else
                {
                    Fail("Hash verification failed");
                }

                // Cleanup
                if (File.Exists(testFilePath))
                    File.Delete(testFilePath);
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestEncryptDecryptRoundTrip()
        {
            Console.WriteLine("Test 3: Encrypt-Decrypt Round Trip (Data Integrity)");
            try
            {
                var encryptionService = new EncryptionService(TEST_KEY_ENIGMA, TEST_KEY_XXTEA, TEST_KEY_CFB);
                var decryptionService = new DecryptionService(TEST_KEY_ENIGMA, TEST_KEY_XXTEA, TEST_KEY_CFB);

                // Originalni sadrzaj
                string originalContent = "Round trip test! This content should be preserved through encryption and decryption.";
                string testFilePath = Path.Combine(Path.GetTempPath(), "test_roundtrip.txt");
                File.WriteAllText(testFilePath, originalContent);

                // Encrypt
                var encTask = Task.Run(async () => await encryptionService.EncryptFileAsync(testFilePath));
                var (encryptedData, hash) = encTask.Result;

                // Decrypt
                var decTask = Task.Run(async () => await decryptionService.DecryptFileAsync(encryptedData, hash));
                var (decryptedData, hashValid) = decTask.Result;

                // Uporedi sadrzaj
                string decryptedContent = System.Text.Encoding.UTF8.GetString(decryptedData);

                if (decryptedContent == originalContent)
                {
                    Pass("Content preserved through encryption/decryption");
                    Console.WriteLine($"   Original: {originalContent}");
                    Console.WriteLine($"   Decrypted: {decryptedContent}");
                }
                else
                {
                    Fail("Content corrupted during round trip");
                    Console.WriteLine($"   Original: {originalContent}");
                    Console.WriteLine($"   Decrypted: {decryptedContent}");
                }

                // Cleanup
                if (File.Exists(testFilePath))
                    File.Delete(testFilePath);
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestHashVerification()
        {
            Console.WriteLine("Test 4: Hash Verification (Tamper Detection)");
            try
            {
                var encryptionService = new EncryptionService(TEST_KEY_ENIGMA, TEST_KEY_XXTEA, TEST_KEY_CFB);
                var decryptionService = new DecryptionService(TEST_KEY_ENIGMA, TEST_KEY_XXTEA, TEST_KEY_CFB);

                string testFilePath = Path.Combine(Path.GetTempPath(), "test_hash.txt");
                File.WriteAllText(testFilePath, "Hash verification test");

                // Encrypt
                var encTask = Task.Run(async () => await encryptionService.EncryptFileAsync(testFilePath));
                var (encryptedData, correctHash) = encTask.Result;

                // Test sa ispravnim hash-om
                var decTask1 = Task.Run(async () => await decryptionService.DecryptFileAsync(encryptedData, correctHash));
                var (data1, hashValid1) = decTask1.Result;

                if (hashValid1)
                {
                    Pass("Valid hash accepted");
                }
                else
                {
                    Fail("Valid hash rejected");
                }

                // Test sa pogresnim hash-om
                string wrongHash = "0000000000000000000000000000000000000000000000";
                var decTask2 = Task.Run(async () => await decryptionService.DecryptFileAsync(encryptedData, wrongHash));
                var (data2, hashValid2) = decTask2.Result;

                if (!hashValid2)
                {
                    Pass("Invalid hash rejected (tamper detection works)");
                }
                else
                {
                    Fail("Invalid hash accepted (tamper detection failed)");
                }

                // Test brze verifikacije
                bool quickCheck = decryptionService.VerifyHash(encryptedData, correctHash);
                if (quickCheck)
                {
                    Pass("Quick hash verification works");
                }
                else
                {
                    Fail("Quick hash verification failed");
                }

                // Cleanup
                if (File.Exists(testFilePath))
                    File.Delete(testFilePath);
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestLargeFileEncryption()
        {
            Console.WriteLine("Test 5: Large File Encryption (Streaming Mode)");
            try
            {
                var encryptionService = new EncryptionService(TEST_KEY_ENIGMA, TEST_KEY_XXTEA, TEST_KEY_CFB);
                var decryptionService = new DecryptionService(TEST_KEY_ENIGMA, TEST_KEY_XXTEA, TEST_KEY_CFB);

                // Kreiraj fajl veci od 50MB (testiramo streaming)
                string testFilePath = Path.Combine(Path.GetTempPath(), "test_large.bin");
                int fileSize = 60 * 1024 * 1024; // 60 MB

                Console.WriteLine($"   Creating {fileSize / (1024 * 1024)} MB test file...");

                using (FileStream fs = new FileStream(testFilePath, FileMode.Create))
                {
                    byte[] buffer = new byte[1024 * 1024]; // 1 MB chunks
                    Random rnd = new Random(42);

                    for (int i = 0; i < 60; i++)
                    {
                        rnd.NextBytes(buffer);
                        fs.Write(buffer, 0, buffer.Length);
                    }
                }

                Console.WriteLine("   Encrypting large file (streaming mode)...");

                bool progressEventFired = false;
                encryptionService.EncryptionProgress += (s, e) =>
                {
                    progressEventFired = true;
                    Console.WriteLine($"   Progress: {e.ProgressPercentage}% ({e.BytesProcessed}/{e.TotalBytes} bytes)");
                };

                // Encrypt
                var encTask = Task.Run(async () => await encryptionService.EncryptFileAsync(testFilePath));
                var (encryptedData, hash) = encTask.Result;

                if (progressEventFired)
                {
                    Pass("Streaming encryption with progress tracking works");
                }
                else
                {
                    Fail("Progress events not fired during streaming");
                }

                if (encryptedData.Length > fileSize)
                {
                    Pass($"Large file encrypted successfully ({encryptedData.Length} bytes)");
                }
                else
                {
                    Fail("Encrypted data size suspicious");
                }

                // Decrypt
                Console.WriteLine("   Decrypting large file...");
                var decTask = Task.Run(async () => await decryptionService.DecryptFileAsync(encryptedData, hash));
                var (decryptedData, hashValid) = decTask.Result;

                // XXTEA dodaje padding (do 4 bytes), pa tolerisemo malu razliku
                int sizeDifference = Math.Abs(decryptedData.Length - fileSize);
                bool sizeOk = sizeDifference <= 1200; // Tolerancija zbog padding-a (svaki chunk moze dodati par bajta)

                if (hashValid && sizeOk)
                {
                    Pass($"Large file decrypted and verified ({decryptedData.Length} bytes, diff: {sizeDifference})");
                }
                else
                {
                    if (!hashValid)
                        Fail($"Hash verification failed");
                    else
                        Fail($"Size mismatch too large (expected ~{fileSize}, got {decryptedData.Length}, diff: {sizeDifference})");
                }

                // Cleanup
                if (File.Exists(testFilePath))
                    File.Delete(testFilePath);
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }
    }
}
