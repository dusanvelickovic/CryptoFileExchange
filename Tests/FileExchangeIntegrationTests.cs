using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CryptoFileExchange.Models;
using CryptoFileExchange.Services;

namespace CryptoFileExchange.Tests
{
    internal class FileExchangeIntegrationTests : TestBase
    {
        protected override string GetTestSuiteName() => "FileExchange Integration";

        private const int TEST_PORT = 9998;
        private const string TEST_IP = "127.0.0.1";
        private const string ENIGMA_KEY = "TestEnigmaKey123";
        private const string XXTEA_KEY = "XXTEATestKey456";
        private const string CFB_KEY = "CFBTestKey789";

        public static (int passed, int failed) RunAllTests()
        {
            var instance = new FileExchangeIntegrationTests();
            instance.ResetCounters();

            Console.WriteLine("=== FileExchange Integration Test Suite ===\n");

            instance.TestCompleteFileExchangeWorkflow();
            instance.TestHashVerificationOnReceiver();
            instance.TestLargeFileTransfer();

            instance.PrintSummary();
            return instance.GetResults();
        }

        /// <summary>
        /// Test 1: Kompletan E2E workflow - Encrypt, Send, Receive, Decrypt
        /// </summary>
        private void TestCompleteFileExchangeWorkflow()
        {
            Console.WriteLine("Test 1: Complete File Exchange Workflow (E2E)");
            try
            {
                // === SETUP ===
                string testFilePath = Path.Combine(Path.GetTempPath(), "integration_test.txt");
                string testContent = "This is an integration test for P2P file exchange!";
                File.WriteAllText(testFilePath, testContent);

                Console.WriteLine($"   Created test file: {testFilePath}");

                // === SENDER SIDE ===
                var encryptionService = new EncryptionService(ENIGMA_KEY, XXTEA_KEY, CFB_KEY);
                var senderNetwork = new NetworkService();

                Console.WriteLine("   [SENDER] Encrypting file...");
                var encTask = Task.Run(async () => await encryptionService.EncryptFileAsync(testFilePath));
                var (encryptedData, hash) = encTask.Result;

                if (encryptedData != null && encryptedData.Length > 0)
                {
                    Pass($"Encryption successful ({encryptedData.Length} bytes)");
                    Console.WriteLine($"   Hash: {hash.Substring(0, 16)}...");
                }
                else
                {
                    Fail("Encryption failed - no data");
                    return;
                }

                // === RECEIVER SIDE ===
                bool fileReceived = false;
                FileTransferMessage? receivedMessage = null;

                var receiverNetwork = new NetworkService();
                var decryptionService = new DecryptionService(ENIGMA_KEY, XXTEA_KEY, CFB_KEY);

                receiverNetwork.FileReceived += (s, e) =>
                {
                    fileReceived = true;
                    receivedMessage = e.Message;
                    Console.WriteLine($"   [RECEIVER] File received: {e.Message.FileName}");
                };

                Console.WriteLine("   [RECEIVER] Starting server...");
                var serverTask = Task.Run(async () => await receiverNetwork.StartListeningAsync(TEST_PORT));
                Thread.Sleep(1000); // Wait for server to start

                // === TRANSFER ===
                var message = new FileTransferMessage
                {
                    FileName = Path.GetFileName(testFilePath),
                    FileSize = encryptedData.Length,
                    FileHash = hash,
                    EncryptedData = encryptedData
                };

                Console.WriteLine("   [SENDER] Sending file...");
                var sendTask = Task.Run(async () => await senderNetwork.SendFileAsync(TEST_IP, TEST_PORT, message));
                bool sendSuccess = sendTask.Result;

                Thread.Sleep(2000); // Wait for transfer

                receiverNetwork.StopListening();

                if (!sendSuccess)
                {
                    Fail("File send failed");
                    return;
                }

                Pass("File transfer completed");

                // === RECEIVER DECRYPTION ===
                if (fileReceived && receivedMessage != null)
                {
                    Pass("File received on receiver side");

                    Console.WriteLine("   [RECEIVER] Decrypting file...");
                    var decTask = Task.Run(async () => 
                        await decryptionService.DecryptFileAsync(receivedMessage.EncryptedData, receivedMessage.FileHash));
                    var (decryptedData, hashValid) = decTask.Result;

                    if (hashValid)
                    {
                        Pass("Hash verification SUCCESS");
                    }
                    else
                    {
                        Fail("Hash verification FAILED");
                    }

                    // Verify content
                    string decryptedContent = System.Text.Encoding.UTF8.GetString(decryptedData);
                    if (decryptedContent == testContent)
                    {
                        Pass("Decrypted content matches original - E2E SUCCESS!");
                        Console.WriteLine($"   Original:  {testContent}");
                        Console.WriteLine($"   Decrypted: {decryptedContent}");
                    }
                    else
                    {
                        Fail("Content mismatch after decryption");
                        Console.WriteLine($"   Original:  {testContent}");
                        Console.WriteLine($"   Decrypted: {decryptedContent}");
                    }
                }
                else
                {
                    Fail("File not received");
                }

                // Cleanup
                if (File.Exists(testFilePath))
                    File.Delete(testFilePath);
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
                Console.WriteLine($"   Stack: {ex.StackTrace}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Test 2: Hash Verification na receiver strani (tamper detection)
        /// </summary>
        private void TestHashVerificationOnReceiver()
        {
            Console.WriteLine("Test 2: Hash Verification on Receiver (Tamper Detection)");
            try
            {
                // === SETUP ===
                string testFilePath = Path.Combine(Path.GetTempPath(), "hash_test.txt");
                File.WriteAllText(testFilePath, "Hash verification test");

                // === ENCRYPT ===
                var encryptionService = new EncryptionService(ENIGMA_KEY, XXTEA_KEY, CFB_KEY);
                var encTask = Task.Run(async () => await encryptionService.EncryptFileAsync(testFilePath));
                var (encryptedData, correctHash) = encTask.Result;

                Console.WriteLine($"   Correct hash: {correctHash.Substring(0, 16)}...");

                // === RECEIVER WITH CORRECT HASH ===
                var decryptionService = new DecryptionService(ENIGMA_KEY, XXTEA_KEY, CFB_KEY);
                var decTask1 = Task.Run(async () => await decryptionService.DecryptFileAsync(encryptedData, correctHash));
                var (data1, valid1) = decTask1.Result;

                if (valid1)
                {
                    Pass("Correct hash accepted");
                }
                else
                {
                    Fail("Correct hash rejected (BUG!)");
                }

                // === RECEIVER WITH TAMPERED DATA ===
                byte[] tamperedData = new byte[encryptedData.Length];
                Array.Copy(encryptedData, tamperedData, encryptedData.Length);
                tamperedData[10] ^= 0xFF; // Flip bits in byte 10

                var decTask2 = Task.Run(async () => await decryptionService.DecryptFileAsync(tamperedData, correctHash));
                var (data2, valid2) = decTask2.Result;

                if (!valid2)
                {
                    Pass("Tampered data detected (hash mismatch)");
                }
                else
                {
                    Fail("Tampered data NOT detected (SECURITY BUG!)");
                }

                // === RECEIVER WITH WRONG HASH ===
                string wrongHash = "0000000000000000000000000000000000000000000000";
                var decTask3 = Task.Run(async () => await decryptionService.DecryptFileAsync(encryptedData, wrongHash));
                var (data3, valid3) = decTask3.Result;

                if (!valid3)
                {
                    Pass("Wrong hash rejected");
                }
                else
                {
                    Fail("Wrong hash accepted (BUG!)");
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

        /// <summary>
        /// Test 3: Transfer velikog fajla (>50MB - streaming mode)
        /// </summary>
        private void TestLargeFileTransfer()
        {
            Console.WriteLine("Test 3: Large File Transfer (Streaming Mode)");
            try
            {
                // Kreiraj 60MB test fajl
                string testFilePath = Path.Combine(Path.GetTempPath(), "large_integration_test.bin");
                int fileSize = 60 * 1024 * 1024; // 60 MB

                Console.WriteLine($"   Creating {fileSize / (1024 * 1024)} MB test file...");

                using (FileStream fs = new FileStream(testFilePath, FileMode.Create))
                {
                    byte[] buffer = new byte[1024 * 1024];
                    Random rnd = new Random(42);

                    for (int i = 0; i < 60; i++)
                    {
                        rnd.NextBytes(buffer);
                        fs.Write(buffer, 0, buffer.Length);
                    }
                }

                Pass("Large test file created");

                // === ENCRYPT (streaming mode will activate) ===
                var encryptionService = new EncryptionService(ENIGMA_KEY, XXTEA_KEY, CFB_KEY);

                bool progressFired = false;
                encryptionService.EncryptionProgress += (s, e) =>
                {
                    progressFired = true;
                    if (e.ProgressPercentage % 20 == 0) // Log every 20%
                    {
                        Console.WriteLine($"   Encryption progress: {e.ProgressPercentage}%");
                    }
                };

                Console.WriteLine("   Encrypting large file (streaming mode)...");
                var encTask = Task.Run(async () => await encryptionService.EncryptFileAsync(testFilePath));
                var (encryptedData, hash) = encTask.Result;

                if (progressFired)
                {
                    Pass("Streaming mode activated (progress events fired)");
                }
                else
                {
                    Fail("Progress events NOT fired (streaming mode issue?)");
                }

                if (encryptedData.Length > fileSize)
                {
                    Pass($"Large file encrypted ({encryptedData.Length / (1024 * 1024)} MB)");
                }
                else
                {
                    Fail("Encrypted data size suspicious");
                }

                // === DECRYPT ===
                Console.WriteLine("   Decrypting large file...");
                var decryptionService = new DecryptionService(ENIGMA_KEY, XXTEA_KEY, CFB_KEY);
                var decTask = Task.Run(async () => await decryptionService.DecryptFileAsync(encryptedData, hash));
                var (decryptedData, hashValid) = decTask.Result;

                if (hashValid)
                {
                    Pass("Large file hash verified");
                }
                else
                {
                    Fail("Hash verification failed for large file");
                }

                // Size check (allow padding tolerance)
                int sizeDiff = Math.Abs(decryptedData.Length - fileSize);
                if (sizeDiff <= 1200) // Tolerance for padding
                {
                    Pass($"Large file decrypted (size diff: {sizeDiff} bytes)");
                }
                else
                {
                    Fail($"Size mismatch too large: {sizeDiff} bytes");
                }

                // Cleanup
                if (File.Exists(testFilePath))
                    File.Delete(testFilePath);

                Console.WriteLine("   Note: Full network transfer test skipped (file too large for quick test)");
                Console.WriteLine("   Use manual test with 2 instances for large file network transfer");
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }
    }
}
