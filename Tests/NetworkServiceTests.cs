using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CryptoFileExchange.Models;
using CryptoFileExchange.Services;

namespace CryptoFileExchange.Tests
{
    internal class NetworkServiceTests : TestBase
    {
        protected override string GetTestSuiteName() => "NetworkService";

        private const int TEST_PORT = 9998; // Drugaciji od production porta (9999)
        private const string TEST_IP = "127.0.0.1";

        public static (int passed, int failed) RunAllTests()
        {
            var instance = new NetworkServiceTests();
            instance.ResetCounters();

            Console.WriteLine("=== NetworkService Test Suite ===\n");

            instance.TestStartStopListening();
            instance.TestFileTransferMessageSerialization();
            instance.TestBasicFileTransfer();

            instance.PrintSummary();
            return instance.GetResults();
        }

        private void TestStartStopListening()
        {
            Console.WriteLine("Test 1: Start/Stop Listening");
            try
            {
                var networkService = new NetworkService();

                // Start listening
                var listenTask = Task.Run(async () => await networkService.StartListeningAsync(TEST_PORT));

                // Sacekaj da se server pokrene
                Thread.Sleep(500);

                if (networkService.IsListening)
                {
                    Pass("NetworkService started listening successfully");
                }
                else
                {
                    Fail("NetworkService failed to start listening");
                }

                // Stop listening
                networkService.StopListening();
                Thread.Sleep(500);

                if (!networkService.IsListening)
                {
                    Pass("NetworkService stopped listening successfully");
                }
                else
                {
                    Fail("NetworkService failed to stop listening");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestFileTransferMessageSerialization()
        {
            Console.WriteLine("Test 2: FileTransferMessage Serialization/Deserialization");
            try
            {
                // Kreiraj test poruku
                byte[] testData = new byte[1024];
                for (int i = 0; i < testData.Length; i++)
                {
                    testData[i] = (byte)(i % 256);
                }

                var originalMessage = new FileTransferMessage
                {
                    FileName = "test_file.txt",
                    FileSize = testData.Length,
                    FileHash = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6", // 48 chars (TigerHash)
                    EncryptedData = testData
                };

                // Serialize
                byte[] serialized = originalMessage.ToBytes();
                Console.WriteLine($"   Serialized message size: {serialized.Length} bytes");

                // Deserialize
                var deserializedMessage = FileTransferMessage.FromBytes(serialized);

                // Verify
                if (deserializedMessage.FileName == originalMessage.FileName
                    && deserializedMessage.FileSize == originalMessage.FileSize
                    && deserializedMessage.FileHash == originalMessage.FileHash
                    && deserializedMessage.EncryptedData.Length == originalMessage.EncryptedData.Length)
                {
                    Pass("FileTransferMessage serialization/deserialization successful");
                    Console.WriteLine($"   File: {deserializedMessage.FileName}");
                    Console.WriteLine($"   Size: {deserializedMessage.FileSize} bytes");
                    Console.WriteLine($"   Hash: {deserializedMessage.FileHash.Substring(0, 16)}...");
                }
                else
                {
                    Fail("Deserialized message does not match original");
                }

                // Verify data integrity
                bool dataMatch = true;
                for (int i = 0; i < testData.Length; i++)
                {
                    if (testData[i] != deserializedMessage.EncryptedData[i])
                    {
                        dataMatch = false;
                        break;
                    }
                }

                if (dataMatch)
                {
                    Pass("Encrypted data integrity verified");
                }
                else
                {
                    Fail("Encrypted data corrupted during serialization");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestBasicFileTransfer()
        {
            Console.WriteLine("Test 3: Basic File Transfer (Sender ? Receiver)");
            try
            {
                bool fileReceived = false;
                FileTransferMessage? receivedMessage = null;

                // Kreiraj Receiver (Server)
                var receiver = new NetworkService();
                receiver.FileReceived += (s, e) =>
                {
                    fileReceived = true;
                    receivedMessage = e.Message;
                    Console.WriteLine($"   Receiver: File received - {e.Message.FileName}");
                };

                // Pokreni Server
                var serverTask = Task.Run(async () => await receiver.StartListeningAsync(TEST_PORT));
                Thread.Sleep(1000); // Sacekaj da se server pokrene

                // Kreiraj test poruku za slanje
                byte[] testFileData = System.Text.Encoding.UTF8.GetBytes("This is test file content for network transfer!");
                var messageToSend = new FileTransferMessage
                {
                    FileName = "network_test.txt",
                    FileSize = testFileData.Length,
                    FileHash = "test_hash_1234567890abcdef1234567890abcdef12345678", // 48 chars
                    EncryptedData = testFileData
                };

                // Kreiraj Sender (Client)
                var sender = new NetworkService();

                // Posalji fajl
                var sendTask = Task.Run(async () =>
                {
                    bool success = await sender.SendFileAsync(TEST_IP, TEST_PORT, messageToSend);
                    if (success)
                    {
                        Console.WriteLine("   Sender: File sent successfully");
                    }
                    return success;
                });

                // Sacekaj da se transfer zavrsi
                bool sendSuccess = sendTask.Result;

                // Sacekaj da se podaci obrade
                Thread.Sleep(2000);

                // Zaustavi server
                receiver.StopListening();

                // Verifikacija
                if (sendSuccess && fileReceived && receivedMessage != null)
                {
                    Pass("File transfer completed successfully");
                    
                    if (receivedMessage.FileName == messageToSend.FileName
                        && receivedMessage.FileSize == messageToSend.FileSize
                        && receivedMessage.FileHash == messageToSend.FileHash)
                    {
                        Pass("Received message matches sent message");
                        Console.WriteLine($"   File: {receivedMessage.FileName}");
                        Console.WriteLine($"   Size: {receivedMessage.FileSize} bytes");
                    }
                    else
                    {
                        Fail("Received message metadata mismatch");
                    }

                    // Verify data content
                    string receivedContent = System.Text.Encoding.UTF8.GetString(receivedMessage.EncryptedData);
                    string originalContent = System.Text.Encoding.UTF8.GetString(testFileData);
                    
                    if (receivedContent == originalContent)
                    {
                        Pass("File content verified - no data loss");
                    }
                    else
                    {
                        Fail("File content corrupted during transfer");
                    }
                }
                else
                {
                    Fail($"File transfer failed. SendSuccess: {sendSuccess}, FileReceived: {fileReceived}");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
            Console.WriteLine();
        }
    }
}
