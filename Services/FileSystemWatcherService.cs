using System;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace CryptoFileExchange.Services
{
    internal class FileSystemWatcherService
    {
        private FileSystemWatcher _watcher;
        private readonly string _encryptedOutputDirectory;
        private bool _isRunning;

        // Eventi za notifikaciju UI-ja
        public event EventHandler<FileDetectedEventArgs> FileDetected;
        public event EventHandler<FileEncryptedEventArgs> FileEncrypted;
        public event EventHandler<FileErrorEventArgs> FileError;

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

            // Kreiraj output direktorijum ako ne postoji
            if (! Directory.Exists(_encryptedOutputDirectory))
            {
                Directory.CreateDirectory(_encryptedOutputDirectory);
                Log.Information("Created encrypted output directory: {Directory}", _encryptedOutputDirectory);
            }
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
            try
            {
                string fileName = Path.GetFileName(sourceFilePath);
                string encryptedFileName = $"{Path.GetFileNameWithoutExtension(fileName)}.cfex";
                string encryptedFilePath = Path.Combine(_encryptedOutputDirectory, encryptedFileName);

                Log.Information("Starting automatic encryption: {FileName}", fileName);

                // Citaj fajl
                byte[] fileData = await File.ReadAllBytesAsync(sourceFilePath);
                long fileSize = fileData.Length;

                // TODO: Ovde integrisati tvoje encryption algoritme
                // Za sada simulacija sifrovanja
                byte[] encryptedData = SimulateEncryption(fileData);

                // Sacuvaj sifrovani fajl
                await File.WriteAllBytesAsync(encryptedFilePath, encryptedData);

                Log.Information("File encrypted successfully: {OriginalFile} -> {EncryptedFile}", 
                    fileName, encryptedFileName);

                // Notifikuj UI o uspesnom sifrovanju
                OnFileEncryptedEvent(new FileEncryptedEventArgs
                {
                    OriginalFileName = fileName,
                    OriginalFilePath = sourceFilePath,
                    EncryptedFileName = encryptedFileName,
                    EncryptedFilePath = encryptedFilePath,
                    OriginalFileSize = fileSize,
                    EncryptedFileSize = encryptedData.Length,
                    EncryptionTime = DateTime.Now,
                    Success = true
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to encrypt file: {FilePath}", sourceFilePath);

                // Notifikuj UI o gresci
                OnFileErrorEvent(new FileErrorEventArgs
                {
                    FileName = Path.GetFileName(sourceFilePath),
                    FilePath = sourceFilePath,
                    ErrorMessage = ex.Message,
                    ErrorTime = DateTime.Now
                });
            }
        }

        /// <summary>
        /// Simulira sifrovanje (privremeno dok ne integrises prave algoritme)
        /// </summary>
        private byte[] SimulateEncryption(byte[] data)
        {
            // PLACEHOLDER - Zameni sa pravim EnigmaEngine, XXTEA, CFB, itd.
            // Primer integracije (zahteva dodavanje using direktiva i key parametra):
            // 
            // var enigma = new EnigmaEngine();
            // var xxtea = new XXTEAEngine();
            // var cfb = new CFBMode();
            // return cfb.Encrypt(xxtea.Encrypt(enigma.Encrypt(data, key), key), key);
            
            return data; // Za sada vraca iste podatke
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

    #endregion
}
