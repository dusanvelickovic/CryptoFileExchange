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
        private const string DEFAULT_OUTPUT_DIR = "EncryptedFiles";

        public FileWatcherPanel()
        {
            InitializeComponent();
            InitializeWatcherService();
        }

        private void InitializeWatcherService()
        {
            try
            {
                // Kreiraj output direktorijum u istom folderu kao exe
                string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DEFAULT_OUTPUT_DIR);
                _watcherService = new FileSystemWatcherService(outputPath);

                // Subskrajbuj se na dogadjaje
                _watcherService.FileDetected += OnFileDetected;
                _watcherService.FileEncrypted += OnFileEncrypted;
                _watcherService.FileError += OnFileError;

                txtOutputDirectory.Text = outputPath;
                
                // Dodaj inicijalni log zapis
                AddLogEntry("FileWatcher service initialized successfully", Color.Green);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Service initialization error: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                AddLogEntry($"FSW Started: {txtTargetDirectory.Text}", Color.Green);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start FWS: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log.Error(ex, "Failed to start file watcher");
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

                AddLogEntry("FWS Stopped", Color.Orange);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to stop FSW: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log.Error(ex, "Failed to stop file watcher");
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
            AddLogEntry(message, Color.Blue);
            Log.Information("File detected: {FileName}", e.FileName);
        }

        private void OnFileEncrypted(object? sender, FileEncryptedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnFileEncrypted(sender, e)));
                return;
            }

            string sizeInfo = $"{e.OriginalFileSize:N0} ? {e.EncryptedFileSize:N0} bytes";
            string message = $"[{e.EncryptionTime:HH:mm:ss}] Encrypted: {e.OriginalFileName} ? {e.EncryptedFileName} ({sizeInfo})";
            AddLogEntry(message, Color.DarkGreen);
            Log.Information("File encrypted: {OriginalFile} -> {EncryptedFile}", 
                e.OriginalFileName, e.EncryptedFileName);
        }

        private void OnFileError(object? sender, FileErrorEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnFileError(sender, e)));
                return;
            }

            string message = $"[{e.ErrorTime:HH:mm:ss}] ? Error: {e.FileName} - {e.ErrorMessage}";
            AddLogEntry(message, Color.Red);
            Log.Error("File encryption error: {FileName} - {Error}", e.FileName, e.ErrorMessage);

            MessageBox.Show($"File encription error:\n{e.FileName}\n\n{e.ErrorMessage}", 
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void AddLogEntry(string message, Color color)
        {
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
                        AddLogEntry($"Error: {ex.Message}", Color.Red);
                        MessageBox.Show($"Encryption error:\n\n{ex.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Log.Error(ex, "Manual file encryption failed");
                    }
                    finally
                    {
                        // Re-enable button
                        btnEncryptFile.Enabled = true;
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
                }
            }
            base.Dispose(disposing);
        }
    }
}
