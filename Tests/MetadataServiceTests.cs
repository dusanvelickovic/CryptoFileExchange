using System;
using System.IO;
using System.Text;
using CryptoFileExchange.Models;
using CryptoFileExchange.Services;

namespace CryptoFileExchange.Tests
{
    internal class MetadataServiceTests : TestBase
    {
        protected override string GetTestSuiteName() => "MetadataService";

        public static (int passed, int failed) RunAllTests()
        {
            var instance = new MetadataServiceTests();
            instance.ResetCounters();

            Console.WriteLine("=== MetadataService Test Suite ===\n");

            instance.TestJsonSerialization();
            instance.TestJsonDeserialization();
            instance.TestAddHeaderToFile();
            instance.TestReadHeaderFromFile();
            instance.TestCreateMetadata();
            instance.TestValidateFileFormat();

            instance.PrintSummary();
            return instance.GetResults();
        }

        private void TestJsonSerialization()
        {
            Console.WriteLine("Test 1: JSON Serialization");
            try
            {
                MetadataService service = new MetadataService();
                FileMetadata metadata = new FileMetadata
                {
                    OriginalFileName = "test.txt",
                    FileSize = 1024,
                    CreationTime = DateTime.Now,
                    EncryptionAlgorithm = "XXTEA",
                    HashAlgorithm = "TigerHash",
                    FileHash = "abc123def456"
                };

                string json = service.SerializeToJson(metadata);

                if (!string.IsNullOrEmpty(json) && 
                    json.Contains("test.txt") && 
                    json.Contains("XXTEA"))
                {
                    Pass("Metadata serialized to JSON successfully");
                    Console.WriteLine($"   JSON length: {json.Length} characters");
                }
                else
                {
                    Fail("JSON serialization produced invalid output");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestJsonDeserialization()
        {
            Console.WriteLine("Test 2: JSON Deserialization");
            try
            {
                MetadataService service = new MetadataService();
                string json = @"{
                    ""OriginalFileName"": ""document.pdf"",
                    ""FileSize"": 2048,
                    ""CreationDate"": ""2024-01-01T12:00:00"",
                    ""EncryptionAlgorithm"": ""Enigma"",
                    ""HashAlgorithm"": ""TigerHash"",
                    ""FileHash"": ""xyz789abc123""
                }";

                FileMetadata metadata = service.DeserializeFromJson(json);

                if (metadata != null && 
                    metadata.OriginalFileName == "document.pdf" &&
                    metadata.FileSize == 2048 &&
                    metadata.EncryptionAlgorithm == "Enigma")
                {
                    Pass("JSON deserialized to FileMetadata successfully");
                    Console.WriteLine($"   File: {metadata.OriginalFileName}");
                    Console.WriteLine($"   Size: {metadata.FileSize} bytes");
                }
                else
                {
                    Fail("JSON deserialization produced invalid object");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestAddHeaderToFile()
        {
            Console.WriteLine("Test 3: Add Header To File");
            try
            {
                MetadataService service = new MetadataService();
                FileMetadata metadata = service.CreateMetadata(
                    "encrypted.dat",
                    512,
                    "CFB-XXTEA",
                    "TigerHash",
                    "hash123456"
                );

                byte[] encryptedData = Encoding.UTF8.GetBytes("This is encrypted content");
                byte[] fileWithHeader = service.AddHeaderToFile(metadata, encryptedData);

                // Proveri da header pocinje sa "CFEX"
                string magic = Encoding.ASCII.GetString(fileWithHeader, 0, 4);

                if (magic == "CFEX" && fileWithHeader.Length > encryptedData.Length)
                {
                    Pass("Header added to file successfully");
                    Console.WriteLine($"   Original data: {encryptedData.Length} bytes");
                    Console.WriteLine($"   With header: {fileWithHeader.Length} bytes");
                    Console.WriteLine($"   Header size: {fileWithHeader.Length - encryptedData.Length} bytes");
                }
                else
                {
                    Fail("Header not added correctly");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestReadHeaderFromFile()
        {
            Console.WriteLine("Test 4: Read Header From File");
            try
            {
                MetadataService service = new MetadataService();
                
                // Kreiraj test fajl sa header-om
                FileMetadata originalMetadata = service.CreateMetadata(
                    "original.txt",
                    256,
                    "Enigma",
                    "TigerHash",
                    "hash_value_123"
                );

                byte[] originalData = Encoding.UTF8.GetBytes("Secret message content");
                byte[] fileWithHeader = service.AddHeaderToFile(originalMetadata, originalData);

                // Citaj nazad
                var (readMetadata, readData) = service.ReadHeaderFromFile(fileWithHeader);

                bool metadataMatch = 
                    readMetadata.OriginalFileName == originalMetadata.OriginalFileName &&
                    readMetadata.FileSize == originalMetadata.FileSize &&
                    readMetadata.EncryptionAlgorithm == originalMetadata.EncryptionAlgorithm;

                bool dataMatch = Encoding.UTF8.GetString(readData) == Encoding.UTF8.GetString(originalData);

                if (metadataMatch && dataMatch)
                {
                    Pass("Header read from file successfully");
                    Console.WriteLine($"   File: {readMetadata.OriginalFileName}");
                    Console.WriteLine($"   Algorithm: {readMetadata.EncryptionAlgorithm}");
                    Console.WriteLine($"   Data: {Encoding.UTF8.GetString(readData)}");
                }
                else
                {
                    Fail("Header or data mismatch after reading");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestCreateMetadata()
        {
            Console.WriteLine("Test 5: Create Metadata");
            try
            {
                MetadataService service = new MetadataService();
                
                FileMetadata metadata = service.CreateMetadata(
                    "myfile.bin",
                    4096,
                    "XXTEA-CFB",
                    "TigerHash-192",
                    "1a2b3c4d5e6f"
                );

                if (metadata != null &&
                    metadata.OriginalFileName == "myfile.bin" &&
                    metadata.FileSize == 4096 &&
                    metadata.CreationTime <= DateTime.Now)
                {
                    Pass("Metadata created successfully");
                    Console.WriteLine($"   File: {metadata.OriginalFileName}");
                    Console.WriteLine($"   Size: {metadata.FileSize} bytes");
                    Console.WriteLine($"   Created: {metadata.CreationTime:yyyy-MM-dd HH:mm:ss}");
                }
                else
                {
                    Fail("Metadata creation failed");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestValidateFileFormat()
        {
            Console.WriteLine("Test 6: Validate File Format");
            try
            {
                MetadataService service = new MetadataService();
                
                // Valid CFEX file
                FileMetadata metadata = service.CreateMetadata("test.dat", 100, "Enigma", "Tiger", "hash");
                byte[] validFile = service.AddHeaderToFile(metadata, new byte[] { 1, 2, 3 });

                // Invalid file
                byte[] invalidFile = Encoding.UTF8.GetBytes("Just some random data");

                bool validResult = service.ValidateFileFormat(validFile);
                bool invalidResult = service.ValidateFileFormat(invalidFile);

                if (validResult && !invalidResult)
                {
                    Pass("File format validation works correctly");
                    Console.WriteLine("   Valid CFEX file: Recognized");
                    Console.WriteLine("   Invalid file: Rejected");
                }
                else
                {
                    Fail("File format validation failed");
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
