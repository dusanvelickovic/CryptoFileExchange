using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CryptoFileExchange.Services;
using Serilog;

namespace CryptoFileExchange.UI
{
    public partial class FileWatcherPanel : UserControl
    {
        private FileSystemWatcherService? _watcherService;
        private DecryptionService? _decryptionService;
        private MetadataService? _metadataService;
        
        private const string DEFAULT_OUTPUT_DIR = "EncryptedFiles";
        
        // Kljucevi za dekripciju (moraju biti isti kao kod enkripcije)
        private const string ENIGMA_KEY = "FileWatcherEnigmaKey2024";
        private const string XXTEA_KEY = "XXTEAAutoEncryptKey123";
        private const string CFB_KEY = "CFBModeAutoEncrypt456";
        private const string CFB_IV = "";

        public FileWatcherPanel()
        {
            InitializeComponent();
            
            // Eksplicitno konfiguriši ListView
            ConfigureListView();
            
            InitializeWatcherService();
            InitializeDecryptionService();
        }

        private void ConfigureListView()
        {
            // Osiguraj da ListView ima kolone
            if (listViewLog.Columns.Count == 0)
            {
                listViewLog.Columns.Add("Events", 770, HorizontalAlignment.Left);
            }

            // Osiguraj da je View postavljen
            listViewLog.View = View.Details;
            listViewLog.FullRowSelect = true;
            listViewLog.GridLines = true;
            listViewLog.HeaderStyle = ColumnHeaderStyle.None;

            Log.Debug("ListView configured: Columns={ColumnCount}, View={View}", 
                listViewLog.Columns.Count, listViewLog.View);
        }

        private void InitializeWatcherService()
        {
            try
            {
                // TEST: Proveri da li ListView radi
                if (listViewLog == null)
                {
                    MessageBox.Show("ListView is NULL!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Kreiraj output direktorijum u istom folderu kao exe
                string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DEFAULT_OUTPUT_DIR);
                _watcherService = new FileSystemWatcherService(outputPath);

                // Subskrajbuj se na dogadjaje
                _watcherService.FileDetected += OnFileDetected;
                _watcherService.FileEncrypted += OnFileEncrypted;
                _watcherService.FileError += OnFileError;
                _watcherService.FileProgress += OnFileProgress;

                txtOutputDirectory.Text = outputPath;
                
                // Dodaj inicijalni log zapis
                AddLogEntry("FileWatcher service initialized successfully", Color.Green);
                
                Log.Information("FileWatcherPanel initialized with output path: {OutputPath}", outputPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Service initialization error: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AddLogEntry($"Failed to initialize FileWatcherService: {ex.Message}", Color.Red, ex);
            }
        }

        private void InitializeDecryptionService()
        {
            try
            {
                _decryptionService = new DecryptionService(ENIGMA_KEY, XXTEA_KEY, CFB_KEY, CFB_IV);
                _metadataService = new MetadataService();
                
                AddLogEntry("Decryption service initialized", Color.Green);
                Log.Information("DecryptionService initialized");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Decryption service initialization error: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AddLogEntry($"Failed to initialize DecryptionService: {ex.Message}", Color.Red, ex);
            }
        }

        private void btnBrowseTarget_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Choose target directory";
                dialog.ShowNewFolderButton = true;

                if (!string.IsNullOrWhiteSpace(txtTargetDirectory.Text) && Directory.Exists(txtTargetDirectory.Text))
                {
                    dialog.SelectedPath = txtTargetDirectory.Text;
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtTargetDirectory.Text = dialog.SelectedPath;
                }
            }
        }

        private void btnToggleWatcher_Click(object sender, EventArgs e)
        {
            if (_watcherService == null)
            {
                MessageBox.Show("Watcher servis is not initialized!", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_watcherService.IsRunning)
            {
                StopWatcher();
            }
            else
            {
                StartWatcher();
            }
        }

        private void StartWatcher()
        {
            if (string.IsNullOrWhiteSpace(txtTargetDirectory.Text))
            {
                MessageBox.Show("You have to choose directory first!", "Warning", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!Directory.Exists(txtTargetDirectory.Text))
            {
                MessageBox.Show("Chosen directory does not exist!", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                _watcherService?.Start(txtTargetDirectory.Text);
                
                // Update ui
                btnToggleWatcher.Text = "Stop FWS";
                btnToggleWatcher.BackColor = Color.IndianRed;
                lblStatus.Text = $"FSW - Active: {txtTargetDirectory.Text}";
                lblStatus.ForeColor = Color.Green;
                txtTargetDirectory.Enabled = false;
                btnBrowseTarget.Enabled = false;
                btnEncryptFile.Enabled = false; // Disable manual encryption
                btnDecryptFile.Enabled = false; // Disable manual decryption

                AddLogEntry($"FSW Started: {txtTargetDirectory.Text}", Color.Green);
            }
            catch (Exception ex)
            {
                AddLogEntry($"Failed to start FWS: {ex.Message}", Color.Red, ex);
                MessageBox.Show($"Failed to start FWS: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopWatcher()
        {
            try
            {
                _watcherService?.Stop();
                
                // Update UI
                btnToggleWatcher.Text = "Start FSW";
                btnToggleWatcher.BackColor = Color.MediumSeaGreen;
                lblStatus.Text = "FWS - Inactive";
                lblStatus.ForeColor = Color.Gray;
                txtTargetDirectory.Enabled = true;
                btnBrowseTarget.Enabled = true;
                btnEncryptFile.Enabled = true; // Enable manual encryption
                btnDecryptFile.Enabled = true; // Enable manual decryption

                AddLogEntry("FWS Stopped", Color.Orange);
            }
            catch (Exception ex)
            {
                AddLogEntry($"Failed to stop FSW: {ex.Message}", Color.Red, ex);
                MessageBox.Show($"Failed to stop FSW: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnFileDetected(object? sender, FileDetectedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnFileDetected(sender, e)));
                return;
            }

            string message = $"[{e.DetectedTime:HH:mm:ss}] Detected: {e.FileName}";
            AddLogEntry(message, Color.Blue); // Automatski loguje u Serilog
        }

        private void OnFileEncrypted(object? sender, FileEncryptedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnFileEncrypted(sender, e)));
                return;
            }

            string sizeInfo = $"{e.OriginalFileSize:N0} -> {e.EncryptedFileSize:N0} bytes";
            string message = $"[{e.EncryptionTime:HH:mm:ss}] Encrypted: {e.OriginalFileName} -> {e.EncryptedFileName} ({sizeInfo})";
            AddLogEntry(message, Color.DarkGreen); // Automatski loguje u Serilog
        }

        private void OnFileError(object? sender, FileErrorEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnFileError(sender, e)));
                return;
            }

            string message = $"[{e.ErrorTime:HH:mm:ss}] -> Error: {e.FileName} - {e.ErrorMessage}";
            AddLogEntry(message, Color.Red); // Automatski loguje u Serilog kao Error

            MessageBox.Show($"File encription error:\n{e.FileName}\n\n{e.ErrorMessage}", 
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void OnFileProgress(object? sender, FileProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnFileProgress(sender, e)));
                return;
            }

            // Formatuj velicinu
            string processed = FormatFileSize(e.BytesProcessed);
            string total = FormatFileSize(e.TotalBytes);
            
            string message = $"[{DateTime.Now:HH:mm:ss}] Progress: {e.FileName} - {e.ProgressPercentage}% ({processed}/{total})";
            
            // Azuriraj log entry (trazi postojeci entry za ovaj fajl i azuriraj ga)
            UpdateOrAddProgressEntry(e.FileName, message);
        }

        private void UpdateOrAddProgressEntry(string fileName, string message)
        {
            // Trazi postojeci progress entry za ovaj fajl
            ListViewItem? existingItem = null;
            foreach (ListViewItem item in listViewLog.Items)
            {
                if (item.Text.Contains($"Progress: {fileName}"))
                {
                    existingItem = item;
                    break;
                }
            }

            if (existingItem != null)
            {
                // Azuriraj postojeci entry
                existingItem.Text = message;
            }
            else
            {
                // Dodaj novi entry
                AddLogEntry(message, Color.DarkOrange);
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private void AddLogEntry(string message, Color color, Exception? ex = null)
        {
            try
            {
                if (listViewLog == null)
                {
                    Log.Error("listViewLog is NULL!");
                    return;
                }

                if (InvokeRequired)
                {
                    Invoke(new Action(() => AddLogEntry(message, color, ex)));
                    return;
                }

                // Automatically log to Serilog based on color (with exception if provided)
                LogToSerilog(message, color, ex);

                var item = new ListViewItem(message)
                {
                    ForeColor = color
                };
                
                listViewLog.Items.Insert(0, item);

                // Limit logs
                if (listViewLog.Items.Count > 100)
                {
                    listViewLog.Items.RemoveAt(listViewLog.Items.Count - 1);
                }
            }
            catch (Exception logEx)
            {
                Log.Error(logEx, "Failed to add log entry: {Message}", message);
                MessageBox.Show($"Failed to add log entry: {logEx.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LogToSerilog(string message, Color color, Exception? ex = null)
        {
            // Map color to Serilog level
            if (color == Color.Red || color.Name == "Red")
            {
                if (ex != null)
                    Log.Error(ex, message);
                else
                    Log.Error(message);
            }
            else if (color == Color.Orange || color == Color.DarkOrange || color.Name == "Orange")
            {
                if (ex != null)
                    Log.Warning(ex, message);
                else
                    Log.Warning(message);
            }
            else if (color == Color.Gray || color.Name == "Gray")
            {
                if (ex != null)
                    Log.Debug(ex, message);
                else
                    Log.Debug(message);
            }
            else // Green, Blue, DarkGreen, Purple, etc.
            {
                if (ex != null)
                    Log.Information(ex, message);
                else
                    Log.Information(message);
            }
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            listViewLog.Items.Clear();
            AddLogEntry("Logs cleared", Color.Gray);
        }

        private async void btnEncryptFile_Click(object sender, EventArgs e)
        {
            if (_watcherService == null)
            {
                MessageBox.Show("Watcher service is not initialized!", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Choose file for encryption";
                dialog.Filter = "All files (*.*)|*.*";
                dialog.CheckFileExists = true;
                dialog.Multiselect = false;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        AddLogEntry($"Manual encryption started: {Path.GetFileName(dialog.FileName)}", Color.Blue);
                        
                        // Disable button during encryption
                        btnEncryptFile.Enabled = false;
                        
                        bool success = await _watcherService.EncryptFileManuallyAsync(dialog.FileName);
                        
                        if (success)
                        {
                            AddLogEntry($"Manual encryption completed: {Path.GetFileName(dialog.FileName)}", Color.DarkGreen);
                            MessageBox.Show($"File successfully encrypted!\n\nOriginal: {Path.GetFileName(dialog.FileName)}\nOutput directory: {_watcherService.EncryptedOutputDirectory}",
                                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            AddLogEntry($"Manual encryption failed: {Path.GetFileName(dialog.FileName)}", Color.Red);
                        }
                    }
                    catch (Exception ex)
                    {
                        AddLogEntry($"Error: {ex.Message}", Color.Red, ex);
                        MessageBox.Show($"Encryption error:\n\n{ex.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        // Re-enable button
                        btnEncryptFile.Enabled = true;
                    }
                }
            }
        }

        private async void btnDecryptFile_Click(object sender, EventArgs e)
        {
            if (_decryptionService == null || _metadataService == null)
            {
                MessageBox.Show("Decryption service is not initialized!", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Choose encrypted file for decryption";
                dialog.Filter = "Encrypted Files (*.cfex)|*.cfex|All Files (*.*)|*.*";
                dialog.CheckFileExists = true;
                dialog.Multiselect = false;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string encryptedFilePath = dialog.FileName;
                        string fileName = Path.GetFileName(encryptedFilePath);
                        
                        AddLogEntry($"Manual decryption started: {fileName}", Color.Blue);
                        Log.Information("Starting manual decryption: {FilePath}", encryptedFilePath);

                        btnDecryptFile.Enabled = false;

                        // Ucitaj enkriptovani fajl
                        byte[] fileWithHeader = await File.ReadAllBytesAsync(encryptedFilePath);
                        
                        AddLogEntry($"File loaded: {fileWithHeader.Length} bytes", Color.Blue);

                        // Ekstrakcija metadata i enkriptovanih podataka
                        var (metadata, encryptedData) = _metadataService.ReadHeaderFromFile(fileWithHeader);

                        if (metadata != null)
                        {
                            AddLogEntry("=== Metadata Found ===", Color.Green);
                            AddLogEntry($"  Original Name: {metadata.OriginalFileName}", Color.Green);
                            AddLogEntry($"  Original Size: {FormatFileSize(metadata.FileSize)}", Color.Green);
                            AddLogEntry($"  Created: {metadata.CreationTime:yyyy-MM-dd HH:mm:ss}", Color.Green);
                            AddLogEntry($"  Encryption: {metadata.EncryptionAlgorithm}", Color.Green);
                            AddLogEntry($"  Hash: {metadata.FileHash}", Color.Green);
                            AddLogEntry("======================", Color.Green);

                            Log.Information("Metadata extracted: OriginalName={OriginalName}, Size={Size}, Hash={Hash}",
                                metadata.OriginalFileName, metadata.FileSize, metadata.FileHash);
                        }
                        else
                        {
                            AddLogEntry("No metadata found in file", Color.Orange);
                            Log.Warning("No metadata header found in file: {FilePath}", encryptedFilePath);
                        }

                        // Dekriptovanje
                        AddLogEntry("Decrypting file...", Color.Blue);
                        
                        string expectedHash = metadata?.FileHash ?? "";
                        var (decryptedData, hashValid) = await _decryptionService.DecryptFileAsync(encryptedData, expectedHash);

                        if (!string.IsNullOrEmpty(expectedHash))
                        {
                            if (hashValid)
                            {
                                AddLogEntry("Hash verification: SUCCESS", Color.Green);
                                Log.Information("Hash verification passed");
                            }
                            else
                            {
                                AddLogEntry("Hash verification: FAILED (file may be corrupted!)", Color.Red);
                                Log.Warning("Hash verification failed for file: {FilePath}", encryptedFilePath);
                                
                                var result = MessageBox.Show(
                                    "Hash verification FAILED!\n\nThe file may be corrupted or tampered with.\n\nDo you want to save it anyway?",
                                    "Hash Verification Failed",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Warning);
                                
                                if (result == DialogResult.No)
                                {
                                    AddLogEntry("Decryption cancelled by user", Color.Orange);
                                    return;
                                }
                            }
                        }

                        // Sasuvaj dekriptovani fajl u istom folderu kao original
                        string outputDirectory = Path.GetDirectoryName(encryptedFilePath) ?? AppDomain.CurrentDomain.BaseDirectory;
                        string originalFileName = metadata?.OriginalFileName ?? Path.GetFileNameWithoutExtension(fileName);
                        string outputPath = Path.Combine(outputDirectory, originalFileName);

                        // Ako fajl vec postoji, dodaj sufiks
                        if (File.Exists(outputPath))
                        {
                            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
                            string extension = Path.GetExtension(originalFileName);
                            int counter = 1;
                            
                            do
                            {
                                outputPath = Path.Combine(outputDirectory, $"{fileNameWithoutExt}_decrypted_{counter}{extension}");
                                counter++;
                            }
                            while (File.Exists(outputPath));
                        }

                        await _decryptionService.SaveDecryptedFileAsync(decryptedData, outputPath);

                        AddLogEntry($"File decrypted and saved: {Path.GetFileName(outputPath)}", Color.DarkGreen);
                        Log.Information("Decrypted file saved to: {OutputPath}", outputPath);

                        string hashInfo = hashValid ? "Hash: VALID" : "Hash: INVALID";
                        MessageBox.Show(
                            $"File successfully decrypted!\n\n" +
                            $"Original: {fileName}\n" +
                            $"Decrypted: {Path.GetFileName(outputPath)}\n" +
                            $"Saved to: {outputDirectory}\n" +
                            $"Size: {FormatFileSize(decryptedData.Length)}\n" +
                            $"{hashInfo}",
                            "Decryption Complete",
                            MessageBoxButtons.OK,
                            hashValid ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                    }
                    catch (Exception ex)
                    {
                        AddLogEntry($"Decryption error: {ex.Message}", Color.Red, ex);
                        Log.Error(ex, "Manual decryption failed");
                        MessageBox.Show($"Decryption error:\n\n{ex.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        btnDecryptFile.Enabled = true;
                    }
                }
            }
        }

        /// <summary>
        /// Test metoda za AddLogEntry - pozovi je da testiraš log
        /// </summary>
        public void TestAddLogEntry()
        {
            AddLogEntry("TEST: Info message", Color.Blue);
            AddLogEntry("TEST: Success message", Color.Green);
            AddLogEntry("TEST: Warning message", Color.Orange);
            AddLogEntry("TEST: Error message", Color.Red);
            AddLogEntry($"TEST: Current time is {DateTime.Now:HH:mm:ss}", Color.Purple);
        }

        private void btnOpenOutput_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(txtOutputDirectory.Text))
            {
                System.Diagnostics.Process.Start("explorer.exe", txtOutputDirectory.Text);
            }
            else
            {
                MessageBox.Show("Output directory does not exist!", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_watcherService != null)
                {
                    if (_watcherService.IsRunning)
                    {
                        _watcherService.Stop();
                    }
                    
                    _watcherService.FileDetected -= OnFileDetected;
                    _watcherService.FileEncrypted -= OnFileEncrypted;
                    _watcherService.FileError -= OnFileError;
                    _watcherService.FileProgress -= OnFileProgress;
                }
            }
            base.Dispose(disposing);
        }
    }
}
