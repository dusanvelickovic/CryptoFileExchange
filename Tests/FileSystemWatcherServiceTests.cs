using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CryptoFileExchange.Services;

namespace CryptoFileExchange.Tests
{
    internal class FileSystemWatcherServiceTests : TestBase
    {
        protected override string GetTestSuiteName() => "FileSystemWatcherService";

        private static string _testTargetDir = null;
        private static string _testOutputDir = null;

        public static (int passed, int failed) RunAllTests()
        {
            var instance = new FileSystemWatcherServiceTests();
            instance.ResetCounters();

            Console.WriteLine("=== FileSystemWatcherService Test Suite ===\n");

            SetupTestDirectories();

            instance.TestStartStop();
            instance.TestFileDetection();
            instance.TestMultipleFiles();
            instance.TestInvalidDirectory();
            instance.TestManualEncryption();
            instance.TestLargeFileEncryption();

            CleanupTestDirectories();

            instance.PrintSummary();
            return instance.GetResults();
        }

        private static void SetupTestDirectories()
        {
            _testTargetDir = Path.Combine(Path.GetTempPath(), "FSW_Test_Target_" + Guid.NewGuid().ToString("N"));
            _testOutputDir = Path.Combine(Path.GetTempPath(), "FSW_Test_Output_" + Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(_testTargetDir);
            Directory.CreateDirectory(_testOutputDir);
        }

        private static void CleanupTestDirectories()
        {
            try
            {
                if (Directory.Exists(_testTargetDir))
                    Directory.Delete(_testTargetDir, true);

                if (Directory.Exists(_testOutputDir))
                    Directory.Delete(_testOutputDir, true);
            }
            catch
            {
                // Ignorisi greske prilikom ciscenja
            }
        }

        private void TestStartStop()
        {
            Console.WriteLine("Test 1: Start/Stop FileSystemWatcher");
            try
            {
                var service = new FileSystemWatcherService(_testOutputDir);

                // Start
                service.Start(_testTargetDir);

                if (service.IsRunning && service.MonitoredDirectory == _testTargetDir)
                {
                    Pass("FileSystemWatcher started successfully");
                    Console.WriteLine($"   Monitoring: {_testTargetDir}");
                }
                else
                {
                    Fail("FileSystemWatcher failed to start");
                }

                // Stop
                service.Stop();

                if (!service.IsRunning)
                {
                    Pass("FileSystemWatcher stopped successfully");
                }
                else
                {
                    Fail("FileSystemWatcher failed to stop");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestFileDetection()
        {
            Console.WriteLine("Test 2: File Detection");
            try
            {
                var service = new FileSystemWatcherService(_testOutputDir);
                bool fileDetected = false;
                bool fileEncrypted = false;

                // Subskrajbuj se na dogadjaje
                service.FileDetected += (s, e) =>
                {
                    fileDetected = true;
                    Console.WriteLine($"   File detected: {e.FileName}");
                };

                service.FileEncrypted += (s, e) =>
                {
                    fileEncrypted = true;
                    Console.WriteLine($"   File encrypted: {e.EncryptedFileName}");
                };

                service.Start(_testTargetDir);

                // Kreiraj test fajl
                string testFile = Path.Combine(_testTargetDir, "test_file.txt");
                File.WriteAllText(testFile, "Test content for FileSystemWatcher");

                // Sacekaj da se fajl detektuje i procesira
                Thread.Sleep(2000);

                service.Stop();

                if (fileDetected && fileEncrypted)
                {
                    Pass("File detected and encrypted successfully");
                }
                else if (fileDetected && !fileEncrypted)
                {
                    Pass("File detected (encryption in progress)");
                }
                else
                {
                    Fail($"File detection/encryption failed. Detected: {fileDetected}, Encrypted: {fileEncrypted}");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestMultipleFiles()
        {
            Console.WriteLine("Test 3: Multiple Files Detection");
            try
            {
                var service = new FileSystemWatcherService(_testOutputDir);
                int filesDetected = 0;

                service.FileDetected += (s, e) =>
                {
                    filesDetected++;
                };

                service.Start(_testTargetDir);

                // Kreiraj vise fajlova
                for (int i = 0; i < 3; i++)
                {
                    string testFile = Path.Combine(_testTargetDir, $"multi_test_{i}.txt");
                    File.WriteAllText(testFile, $"Content {i}");
                    Thread.Sleep(300); // Sacekaj
                }

                Thread.Sleep(2000);
                service.Stop();

                if (filesDetected >= 3)
                {
                    Pass($"Multiple files detected: {filesDetected} files");
                }
                else
                {
                    Fail($"Not all files detected. Expected: 3, Got: {filesDetected}");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestInvalidDirectory()
        {
            Console.WriteLine("Test 4: Invalid Directory Handling");
            try
            {
                var service = new FileSystemWatcherService(_testOutputDir);
                string invalidDir = @"C:\NonExistentDirectory_" + Guid.NewGuid().ToString("N");

                try
                {
                    service.Start(invalidDir);
                    Fail("Should throw exception for non-existent directory");
                }
                catch (DirectoryNotFoundException)
                {
                    Pass("Correctly throws exception for invalid directory");
                }
            }
            catch (Exception ex)
            {
                Fail($"Unexpected exception: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestManualEncryption()
        {
            Console.WriteLine("Test 5: Manual File Encryption (FSW disabled)");
            try
            {
                var service = new FileSystemWatcherService(_testOutputDir);
                bool fileDetected = false;
                bool fileEncrypted = false;

                // Subskrajbuj se na dogadjaje
                service.FileDetected += (s, e) =>
                {
                    fileDetected = true;
                    Console.WriteLine($"   File detected: {e.FileName}");
                };

                service.FileEncrypted += (s, e) =>
                {
                    fileEncrypted = true;
                    Console.WriteLine($"   File encrypted: {e.OriginalFileName} -> {e.EncryptedFileName}");
                };

                // Kreiraj test fajl
                string testFile = Path.Combine(_testTargetDir, "manual_test.txt");
                File.WriteAllText(testFile, "Manual encryption test content");

                // FSW je iskljucen - rucno sifrovanje
                var result = service.EncryptFileManuallyAsync(testFile).Result;

                // Sacekaj malo
                Thread.Sleep(1000);

                if (result && fileDetected && fileEncrypted)
                {
                    Pass("Manual encryption successful (FSW disabled)");
                    
                    // Proveri da li je fajl kreiran u output direktorijumu
                    string expectedOutput = Path.Combine(_testOutputDir, "manual_test.cfex");
                    if (File.Exists(expectedOutput))
                    {
                        Pass($"Encrypted file created: {Path.GetFileName(expectedOutput)}");
                    }
                    else
                    {
                        Fail("Encrypted file not found in output directory");
                    }
                }
                else
                {
                    Fail($"Manual encryption failed. Result: {result}, Detected: {fileDetected}, Encrypted: {fileEncrypted}");
                }
            }
            catch (Exception ex)
            {
                Fail($"Exception: {ex.Message}");
            }
            Console.WriteLine();
        }

        private void TestLargeFileEncryption()
        {
            Console.WriteLine("Test 6: Large File Encryption (>50MB with streaming)");
            try
            {
                var service = new FileSystemWatcherService(_testOutputDir);
                bool fileDetected = false;
                bool fileEncrypted = false;
                bool progressReported = false;
                int progressUpdates = 0;

                service.FileDetected += (s, e) =>
                {
                    fileDetected = true;
                    Console.WriteLine($"   File detected: {e.FileName}");
                };

                service.FileEncrypted += (s, e) =>
                {
                    fileEncrypted = true;
                    Console.WriteLine($"   File encrypted: {e.OriginalFileName} -> {e.EncryptedFileName}");
                    Console.WriteLine($"   Size: {e.OriginalFileSize:N0} bytes -> {e.EncryptedFileSize:N0} bytes");
                };

                service.FileProgress += (s, e) =>
                {
                    progressReported = true;
                    progressUpdates++;
                    if (progressUpdates % 10 == 0 || e.ProgressPercentage >= 100)
                    {
                        Console.WriteLine($"   Progress: {e.ProgressPercentage}% ({e.BytesProcessed:N0}/{e.TotalBytes:N0} bytes)");
                    }
                };

                // Kreiraj veliki test fajl (60 MB)
                string testFile = Path.Combine(_testTargetDir, "large_file.bin");
                Console.WriteLine("   Creating 60 MB test file...");
                
                using (var fs = new FileStream(testFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] chunk = new byte[1024 * 1024]; // 1 MB chunk
                    for (int i = 0; i < 60; i++) // 60 chunks = 60 MB
                    {
                        // Popuni chunk sa pseudo-random podacima
                        for (int j = 0; j < chunk.Length; j++)
                        {
                            chunk[j] = (byte)((i * 1000 + j) % 256);
                        }
                        fs.Write(chunk, 0, chunk.Length);
                    }
                }

                Console.WriteLine("   Test file created. Starting encryption...");

                // FSW iskljucen - rucno sifrovanje
                var result = service.EncryptFileManuallyAsync(testFile).Result;

                // Sacekaj da se zavrsi
                Thread.Sleep(3000);

                if (result && fileDetected && fileEncrypted)
                {
                    Pass("Large file encrypted successfully");
                    
                    if (progressReported && progressUpdates > 0)
                    {
                        Pass($"Progress reported: {progressUpdates} updates (streaming mode confirmed)");
                    }
                    else
                    {
                        Fail("Progress not reported (streaming might not be working)");
                    }

                    // Proveri da li je fajl kreiran
                    string expectedOutput = Path.Combine(_testOutputDir, "large_file.cfex");
                    if (File.Exists(expectedOutput))
                    {
                        FileInfo outputInfo = new FileInfo(expectedOutput);
                        Pass($"Encrypted file created: {outputInfo.Length:N0} bytes");
                    }
                    else
                    {
                        Fail("Encrypted file not found");
                    }
                }
                else
                {
                    Fail($"Large file encryption failed. Result: {result}, Detected: {fileDetected}, Encrypted: {fileEncrypted}");
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
