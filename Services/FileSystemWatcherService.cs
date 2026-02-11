using System;
using System.IO;
using System.Threading.Tasks;
using CryptoFileExchange.Algorithms.Hash;
using CryptoFileExchange.Models;
using Serilog;

namespace CryptoFileExchange.Services
{
    internal class FileSystemWatcherService
    {
        private FileSystemWatcher _watcher;
        private readonly string _encryptedOutputDirectory;
        private bool _isRunning;

        // Servisi za enkripciju i metadata
        private readonly EncryptionService _encryptionService;
        private readonly MetadataService _metadataService;
        private readonly TigerHash _tigerHash;

        // Kljucevi za enkripciju
        private const string ENIGMA_KEY = "FileWatcherEnigmaKey2024";
        private const string XXTEA_KEY = "XXTEAAutoEncryptKey123";
        private const string CFB_KEY = "CFBModeAutoEncrypt456";
        private const string CFB_IV = "";

        // Prag za odluku kada koristiti streaming (50 MB)
        private const long STREAMING_THRESHOLD = 50 * 1024 * 1024;
        // Velicina buffer-a za streaming (1 MB)
        private const int BUFFER_SIZE = 1024 * 1024;

        // Eventi za notifikacije UI-ja
        public event EventHandler<FileDetectedEventArgs> FileDetected;
        public event EventHandler<FileEncryptedEventArgs> FileEncrypted;
        public event EventHandler<FileErrorEventArgs> FileError;
        public event EventHandler<FileProgressEventArgs> FileProgress;

        /// <summary>
        /// Konstruktor FileSystemWatcherService
        /// </summary>
        /// <param name="encryptedOutputDirectory">Direktorijum gde se cuvaju sifrovani fajlovi (X)</param>
        public FileSystemWatcherService(string encryptedOutputDirectory)
        {
            if (string.IsNullOrWhiteSpace(encryptedOutputDirectory))
                throw new ArgumentException("Encrypted output directory cannot be null or empty", nameof(encryptedOutputDirectory));

            _encryptedOutputDirectory = encryptedOutputDirectory;
            _isRunning = false;

            // Inicijalizuj servise za enkripciju
            _encryptionService = new EncryptionService(ENIGMA_KEY, XXTEA_KEY, CFB_KEY, CFB_IV);
            _metadataService = new MetadataService();
            _tigerHash = new TigerHash();

            // Subscribe na encryption progress
            _encryptionService.EncryptionProgress += OnEncryptionProgress;

            // Kreiraj output direktorijum ako ne postoji
            if (! Directory.Exists(_encryptedOutputDirectory))
            {
                Directory.CreateDirectory(_encryptedOutputDirectory);
                Log.Information("Created encrypted output directory: {Directory}", _encryptedOutputDirectory);
            }

            Log.Information("FileSystemWatcherService initialized with encryption algorithms: Enigma -> XXTEA -> CFB");
        }

        /// <summary>
        /// Pokrece pracenje direktorijuma
        /// </summary>
        /// <param name="targetDirectory">Target direktorijum za pracenje</param>
        public void Start(string targetDirectory)
        {
            if (string.IsNullOrWhiteSpace(targetDirectory))
                throw new ArgumentException("Target directory cannot be null or empty", nameof(targetDirectory));

            if (!Directory.Exists(targetDirectory))
                throw new DirectoryNotFoundException($"Target directory does not exist: {targetDirectory}");

            if (_isRunning)
            {
                Log.Warning("FileSystemWatcher is already running. Stop it first before starting again.");
                return;
            }

            try
            {
                // Kreiraj FileSystemWatcher
                _watcher = new FileSystemWatcher
                {
                    Path = targetDirectory,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                    Filter = "*.*", // Prati sve fajlove
                    EnableRaisingEvents = false
                };

                // Subskrajbuj se na dogadjaje
                _watcher.Created += OnFileCreated;
                _watcher.Error += OnWatcherError;

                // Pokreni pracenje
                _watcher.EnableRaisingEvents = true;
                _isRunning = true;

                Log.Information("FileSystemWatcher started. Monitoring directory: {Directory}", targetDirectory);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to start FileSystemWatcher for directory: {Directory}", targetDirectory);
                throw;
            }
        }

        /// <summary>
        /// Zaustavlja pracenje direktorijuma
        /// </summary>
        public void Stop()
        {
            if (!_isRunning || _watcher == null)
            {
                Log.Warning("FileSystemWatcher is not running");
                return;
            }

            try
            {
                // Odsubskrajbuj se od dogadjaja
                _watcher.Created -= OnFileCreated;
                _watcher.Error -= OnWatcherError;

                // Zaustavi pracenje
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
                _isRunning = false;

                Log.Information("FileSystemWatcher stopped");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while stopping FileSystemWatcher");
                throw;
            }
        }

        /// <summary>
        /// Event handler za Created dogadjaj
        /// </summary>
        private async void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            Log.Information("New file detected: {FileName} at {FilePath}", Path.GetFileName(e.FullPath), e.FullPath);

            // Notifikuj UI o detekciji fajla
            OnFileDetectedEvent(new FileDetectedEventArgs
            {
                FileName = Path.GetFileName(e.FullPath),
                FilePath = e.FullPath,
                DetectedTime = DateTime.Now
            });

            // Sačekaj malo da fajl bude kompletno zapisan
            await Task.Delay(500);

            // Proveri da li fajl jos uvek postoji (moze biti obrisan)
            if (! File.Exists(e.FullPath))
            {
                Log.Warning("File no longer exists: {FilePath}", e.FullPath);
                return;
            }

            // Automatski sifruj fajl
            await EncryptFileAsync(e.FullPath);
        }

        /// <summary>
        /// Automatski sifruje detektovani fajl
        /// </summary>
        private async Task EncryptFileAsync(string sourceFilePath)
        {
            string fileName = Path.GetFileName(sourceFilePath);
            string encryptedFileName = $"{Path.GetFileNameWithoutExtension(fileName)}.cfex";
            string encryptedFilePath = Path.Combine(_encryptedOutputDirectory, encryptedFileName);

            try
            {
                Log.Information("Starting automatic encryption: {FileName}", fileName);

                // Proveri velicinu fajla
                FileInfo fileInfo = new FileInfo(sourceFilePath);
                long fileSize = fileInfo.Length;

                // === KORAK 1: ENKRIPCIJA ===
                Log.Information("Encrypting file: {FileName} ({FileSize} bytes)", fileName, fileSize);

                // Koristi EncryptionService za pravu enkripciju
                var (encryptedData, hash) = await _encryptionService.EncryptFileAsync(sourceFilePath);

                Log.Information("Encryption completed. Hash: {Hash}", hash);

                // === KORAK 2: SACUVAJ ENKRIPTOVANI FAJL SA METADATA HEADER-OM ===
                // Kreiraj metadata objekat
                var metadata = new FileMetadata
                {
                    OriginalFileName = fileName,
                    FileSize = fileSize,
                    CreationTime = DateTime.Now,
                    EncryptionAlgorithm = "Enigma -> XXTEA -> CFB",
                    HashAlgorithm = "TigerHash (192-bit)",
                    FileHash = hash
                };

                // Dodaj header i sacuvaj kompletan fajl
                byte[] fileWithHeader = _metadataService.AddHeaderToFile(metadata, encryptedData);
                await File.WriteAllBytesAsync(encryptedFilePath, fileWithHeader);

                Log.Information("Encrypted file with metadata header saved: {EncryptedFilePath}", encryptedFilePath);

                // === KORAK 3: OPCIONO - SACUVAJ I STANDALONE JSON ===
                string jsonMetadataPath = encryptedFilePath + ".json";
                string jsonMetadata = _metadataService.SerializeToJson(metadata);
                await File.WriteAllTextAsync(jsonMetadataPath, jsonMetadata);

                Log.Information("Metadata JSON created: {JsonPath}", jsonMetadataPath);

                // === KORAK 4: NOTIFIKUJ UI ===
                OnFileEncryptedEvent(new FileEncryptedEventArgs
                {
                    OriginalFileName = fileName,
                    OriginalFilePath = sourceFilePath,
                    EncryptedFileName = encryptedFileName,
                    EncryptedFilePath = encryptedFilePath,
                    OriginalFileSize = fileSize,
                    EncryptedFileSize = fileWithHeader.Length, // Ukljucuje header
                    EncryptionTime = DateTime.Now,
                    Success = true
                });

                Log.Information("Encryption successful: {OriginalFile} -> {EncryptedFile} ({OriginalSize} -> {EncryptedSize} bytes)",
                    fileName, encryptedFileName, fileSize, fileWithHeader.Length);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to encrypt file: {FilePath}", sourceFilePath);

                // Notifikuj UI o gresci
                OnFileErrorEvent(new FileErrorEventArgs
                {
                    FileName = fileName,
                    FilePath = sourceFilePath,
                    ErrorMessage = ex.Message,
                    ErrorTime = DateTime.Now
                });
            }
        }

        /// <summary>
        /// Rucno sifruje izabrani fajl (koristi se kada je FSW iskljucen)
        /// </summary>
        /// <param name="sourceFilePath">Putanja do fajla koji treba sifrovati</param>
        /// <returns>True ako je sifrovanje uspelo, False ako nije</returns>
        public async Task<bool> EncryptFileManuallyAsync(string sourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath))
                throw new ArgumentException("Source file path cannot be null or empty", nameof(sourceFilePath));

            if (!File.Exists(sourceFilePath))
                throw new FileNotFoundException($"Source file does not exist: {sourceFilePath}");

            try
            {
                Log.Information("Manual encryption started: {FilePath}", sourceFilePath);

                // Notifikuj UI o detekciji fajla (rucno izabran)
                OnFileDetectedEvent(new FileDetectedEventArgs
                {
                    FileName = Path.GetFileName(sourceFilePath),
                    FilePath = sourceFilePath,
                    DetectedTime = DateTime.Now
                });

                // Sifruj fajl (koristi istu metodu kao i automatsko sifrovanje)
                await EncryptFileAsync(sourceFilePath);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Manual encryption failed: {FilePath}", sourceFilePath);
                return false;
            }
        }

        /// <summary>
        /// Event handler za greske u FileSystemWatcheru
        /// </summary>
        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            Exception ex = e.GetException();
            Log.Error(ex, "FileSystemWatcher error occurred");

            // Pokusaj restart
            try
            {
                if (_watcher != null)
                {
                    string path = _watcher.Path;
                    Stop();
                    Log.Information("Attempting to restart FileSystemWatcher...");
                    Task.Delay(1000).Wait();
                    Start(path);
                }
            }
            catch (Exception restartEx)
            {
                Log.Error(restartEx, "Failed to restart FileSystemWatcher");
            }
        }

        /// <summary>
        /// Handler za encryption progress iz EncryptionService
        /// </summary>
        private void OnEncryptionProgress(object? sender, FileProgressEventArgs e)
        {
            // Prosledi progress event UI-ju
            OnFileProgressEvent(e);
        }

        /// <summary>
        /// Podize FileDetected dogadjaj
        /// </summary>
        protected virtual void OnFileDetectedEvent(FileDetectedEventArgs e)
        {
            FileDetected?.Invoke(this, e);
        }

        /// <summary>
        /// Podize FileEncrypted dogadjaj
        /// </summary>
        protected virtual void OnFileEncryptedEvent(FileEncryptedEventArgs e)
        {
            FileEncrypted?.Invoke(this, e);
        }

        /// <summary>
        /// Podize FileError dogadjaj
        /// </summary>
        protected virtual void OnFileErrorEvent(FileErrorEventArgs e)
        {
            FileError?.Invoke(this, e);
        }

        /// <summary>
        /// Podize FileProgress dogadjaj
        /// </summary>
        protected virtual void OnFileProgressEvent(FileProgressEventArgs e)
        {
            FileProgress?.Invoke(this, e);
        }

        /// <summary>
        /// Provera da li je servis pokrenut
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Trenutni target direktorijum koji se prati
        /// </summary>
        public string MonitoredDirectory => _watcher?.Path;

        /// <summary>
        /// Direktorijum gde se cuvaju sifrovani fajlovi
        /// </summary>
        public string EncryptedOutputDirectory => _encryptedOutputDirectory;
    }

    #region Event Arguments

    /// <summary>
    /// Argumenti dogadjaja za detekciju fajla
    /// </summary>
    public class FileDetectedEventArgs : EventArgs
    {
        public required string FileName { get; set; }
        public required string FilePath { get; set; }
        public DateTime DetectedTime { get; set; }
    }

    /// <summary>
    /// Argumenti dogadjaja za sifrovani fajl
    /// </summary>
    public class FileEncryptedEventArgs : EventArgs
    {
        public required string OriginalFileName { get; set; }
        public required string OriginalFilePath { get; set; }
        public required string EncryptedFileName { get; set; }
        public required string EncryptedFilePath { get; set; }
        public long OriginalFileSize { get; set; }
        public long EncryptedFileSize { get; set; }
        public DateTime EncryptionTime { get; set; }
        public bool Success { get; set; }
    }

    /// <summary>
    /// Argumenti dogadjaja za greske
    /// </summary>
    public class FileErrorEventArgs : EventArgs
    {
        public required string FileName { get; set; }
        public required string FilePath { get; set; }
        public required string ErrorMessage { get; set; }
        public DateTime ErrorTime { get; set; }
    }

    /// <summary>
    /// Argumenti dogadjaja za progres sifrovanja (koristi se za velike fajlove)
    /// </summary>
    public class FileProgressEventArgs : EventArgs
    {
        public required string FileName { get; set; }
        public required string FilePath { get; set; }
        public long BytesProcessed { get; set; }
        public long TotalBytes { get; set; }
        public int ProgressPercentage { get; set; }
    }

    #endregion
}
