using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using CryptoFileExchange.Models;
using CryptoFileExchange.Services;
using Serilog;

namespace CryptoFileExchange.UI
{
    /// <summary>
    /// Panel za File Exchange - slanje i primanje sifrova fajlova preko mreze
    /// </summary>
    public partial class FileExchangePanel : UserControl
    {
        private NetworkService? _networkService;
        private EncryptionService? _encryptionService;
        private DecryptionService? _decryptionService;

        private bool _isServerMode = false;
        private const int DEFAULT_PORT = 9999;
        private const string DEFAULT_ENIGMA_KEY = "MyEnigmaSecretKey2024";
        private const string DEFAULT_XXTEA_KEY = "XXTEAKey12345678";
        private const string DEFAULT_CFB_KEY = "CFBModeKey987654";

        public FileExchangePanel()
        {
            InitializeComponent();
            ConfigureListView();
            InitializeServices();
        }

        private void ConfigureListView()
        {
            listViewLog.View = View.Details;
            listViewLog.FullRowSelect = true;
            listViewLog.GridLines = true;

            if (listViewLog.Columns.Count == 0)
            {
                listViewLog.Columns.Add("Events", 770);
            }
        }

        private void InitializeServices()
        {
            _encryptionService = new EncryptionService(DEFAULT_ENIGMA_KEY, DEFAULT_XXTEA_KEY, DEFAULT_CFB_KEY);
            _decryptionService = new DecryptionService(DEFAULT_ENIGMA_KEY, DEFAULT_XXTEA_KEY, DEFAULT_CFB_KEY);
            _networkService = new NetworkService();

            // Subscribe to network events
            _networkService.FileReceived += OnFileReceived;
            _networkService.TransferProgress += OnTransferProgress;
            _networkService.ConnectionStatus += OnConnectionStatus;
            _networkService.NetworkError += OnNetworkError;

            // Subscribe to encryption/decryption progress
            _encryptionService.EncryptionProgress += OnEncryptionProgress;
            _decryptionService.DecryptionProgress += OnDecryptionProgress;

            AddLogEntry("File Exchange initialized", Color.Blue);
        }

        #region Button Events

        private async void btnToggleServerMode_Click(object sender, EventArgs e)
        {
            if (_isServerMode)
            {
                StopServer();
            }
            else
            {
                await StartServerAsync();
            }
        }

        private async Task StartServerAsync()
        {
            try
            {
                if (!int.TryParse(txtPort.Text, out int port) || port <= 0 || port > 65535)
                {
                    MessageBox.Show("Please enter a valid port number (1-65535)", "Invalid Port", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                await _networkService!.StartListeningAsync(port);

                _isServerMode = true;
                btnToggleServerMode.Text = "Stop Server";
                btnToggleServerMode.BackColor = Color.Red;
                txtPort.Enabled = false;
                grpClient.Enabled = false;

                AddLogEntry($"Server started on port {port}", Color.Green);
            }
            catch (Exception ex)
            {
                AddLogEntry($"Failed to start server: {ex.Message}", Color.Red, ex);
                MessageBox.Show($"Failed to start server: {ex.Message}", "Server Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopServer()
        {
            try
            {
                _networkService!.StopListening();

                _isServerMode = false;
                btnToggleServerMode.Text = "Start Server";
                btnToggleServerMode.BackColor = SystemColors.Control;
                txtPort.Enabled = true;
                grpClient.Enabled = true;

                AddLogEntry("Server stopped", Color.Orange);
            }
            catch (Exception ex)
            {
                AddLogEntry($"Error stopping server: {ex.Message}", Color.Red, ex);
            }
        }

        private async void btnSendFile_Click(object sender, EventArgs e)
        {
            try
            {
                // Validacija
                if (string.IsNullOrWhiteSpace(txtFilePath.Text) || !File.Exists(txtFilePath.Text))
                {
                    MessageBox.Show("Please select a valid file to send", "No File Selected",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtIPAddress.Text))
                {
                    MessageBox.Show("Please enter recipient IP address", "IP Address Required",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!int.TryParse(txtRecipientPort.Text, out int recipientPort) || recipientPort <= 0 || recipientPort > 65535)
                {
                    MessageBox.Show("Please enter a valid recipient port number (1-65535)", "Invalid Port",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string filePath = txtFilePath.Text;
                string ipAddress = txtIPAddress.Text;

                // Disable buttons during transfer
                btnSendFile.Enabled = false;
                btnBrowseFile.Enabled = false;

                AddLogEntry($"Encrypting file: {Path.GetFileName(filePath)}...", Color.Blue);
                Log.Information("Starting file encryption for: {FilePath}", filePath);

                // Enkriptuj fajl (korak 1)
                var (encryptedData, hash) = await _encryptionService!.EncryptFileAsync(filePath);

                AddLogEntry($"File encrypted successfully. Hash: {hash.Substring(0, 16)}...", Color.Green);
                Log.Information("File encrypted. Hash: {Hash}", hash);

                // === DEBUG: Proveri enkriptovane podatke ===
                Log.Debug("Encrypted data length: {Length} bytes", encryptedData.Length);
                Log.Debug("Hash: {Hash}", hash);

                // Kreiraj Metadata o originalnom fajlu
                FileInfo fileInfo = new FileInfo(filePath);
                var metadata = new FileMetadata
                {
                    OriginalFileName = fileInfo.Name,
                    FileSize = fileInfo.Length,
                    CreationTime = fileInfo.CreationTime,
                    EncryptionAlgorithm = "Enigma -> XXTEA -> CFB",
                    HashAlgorithm = "TigerHash (192-bit)",
                    FileHash = hash
                };

                AddLogEntry($"Metadata created for: {metadata.OriginalFileName}", Color.Blue);
                Log.Information("File metadata: OriginalName={OriginalFileName}, Size={FileSize}, Created={CreationTime}",
                    metadata.OriginalFileName, metadata.FileSize, metadata.CreationTime);

                // Kreiraj FileTransferMessage
                var message = new FileTransferMessage
                {
                    FileName = Path.GetFileName(filePath),
                    FileSize = encryptedData.Length,
                    FileHash = hash,
                    EncryptedData = encryptedData,
                    Metadata = metadata
                };

                // === DEBUG: Proveri message ===
                Log.Debug("FileTransferMessage created:");
                Log.Debug("  FileName: {FileName}", message.FileName);
                Log.Debug("  FileSize: {FileSize}", message.FileSize);
                Log.Debug("  FileHash: {Hash}", message.FileHash);
                Log.Debug("  EncryptedData length: {Length}", message.EncryptedData.Length);
                Log.Debug("  Metadata included: {HasMetadata}", message.Metadata != null);

                AddLogEntry($"Sending file to {ipAddress}:{recipientPort}...", Color.Blue);

                // Posalji fajl preko mreze (korak 2)
                bool success = await _networkService!.SendFileAsync(ipAddress, recipientPort, message);

                if (success)
                {
                    AddLogEntry($"File sent successfully to {ipAddress}:{recipientPort}", Color.Green);
                    MessageBox.Show($"File sent successfully!\nFile: {message.FileName}\nSize: {message.FileSize} bytes\nHash: {hash}",
                        "Transfer Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    AddLogEntry($"Failed to send file to {ipAddress}:{recipientPort}", Color.Red);
                    MessageBox.Show("Failed to send file. Check network connection and recipient settings.",
                        "Transfer Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                AddLogEntry($"Error sending file: {ex.Message}", Color.Red, ex);
                MessageBox.Show($"Error sending file: {ex.Message}", "Send Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSendFile.Enabled = true;
                btnBrowseFile.Enabled = true;
            }
        }

        private void btnBrowseFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Select File to Send";
                dlg.Filter = "All Files (*.*)|*.*";
                dlg.CheckFileExists = true;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtFilePath.Text = dlg.FileName;
                    FileInfo fileInfo = new FileInfo(dlg.FileName);
                    AddLogEntry($"File selected: {fileInfo.Name} ({FormatFileSize(fileInfo.Length)})", Color.Blue);
                }
            }
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            listViewLog.Items.Clear();
            AddLogEntry("Log cleared", Color.Gray);
        }

        #endregion

        #region Network Event Handlers

        private async void OnFileReceived(object? sender, FileReceivedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnFileReceived(sender, e)));
                return;
            }

            try
            {
                AddLogEntry($"File received: {e.Message.FileName} ({e.Message.FileSize} bytes)", Color.Blue);
                Log.Information("File received from {Sender}: {FileName}", e.Sender, e.Message.FileName);

                // === DISPLAY METADATA (if available) ===
                if (e.Message.Metadata != null)
                {
                    AddLogEntry("=== File Metadata ===", Color.Green);
                    AddLogEntry($"  Original Name: {e.Message.Metadata.OriginalFileName}", Color.Green);
                    AddLogEntry($"  Original Size: {FormatFileSize(e.Message.Metadata.FileSize)}", Color.Green);
                    AddLogEntry($"  Created: {e.Message.Metadata.CreationTime:yyyy-MM-dd HH:mm:ss}", Color.Green);
                    AddLogEntry($"  Encryption: {e.Message.Metadata.EncryptionAlgorithm}", Color.Green);
                    AddLogEntry($"  Hash Algorithm: {e.Message.Metadata.HashAlgorithm}", Color.Green);
                    AddLogEntry("====================", Color.Green);

                    Log.Information("Received file metadata: OriginalName={OriginalName}, Size={Size}, Created={Created}, EncAlg={EncAlg}",
                        e.Message.Metadata.OriginalFileName,
                        e.Message.Metadata.FileSize,
                        e.Message.Metadata.CreationTime,
                        e.Message.Metadata.EncryptionAlgorithm);
                }
                else
                {
                    AddLogEntry("No metadata received (older protocol version or sender didn't include it)", Color.Orange);
                }

                // === DEBUG: Proveri podatke ===
                Log.Debug("FileReceived event data:");
                Log.Debug("  FileName: {FileName}", e.Message.FileName);
                Log.Debug("  FileSize: {FileSize}", e.Message.FileSize);
                Log.Debug("  FileHash: {Hash}", e.Message.FileHash);
                Log.Debug("  EncryptedData length: {Length}", e.Message.EncryptedData?.Length ?? 0);
                Log.Debug("  Metadata: {HasMetadata}", e.Message.Metadata != null);

                if (e.Message.EncryptedData == null || e.Message.EncryptedData.Length == 0)
                {
                    AddLogEntry("ERROR: Received empty encrypted data!", Color.Red);
                    Log.Error("EncryptedData is null or empty");
                    MessageBox.Show("Received file has no data!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Dekriptuj automatski (po zahtevu)
                AddLogEntry("Verifying hash and decrypting file...", Color.Blue);

                var (decryptedData, hashValid) = await _decryptionService!.DecryptFileAsync(
                    e.Message.EncryptedData,
                    e.Message.FileHash);

                if (hashValid)
                {
                    AddLogEntry("Hash verification: SUCCESS", Color.Green);
                    
                    // Sacuvaj dekriptovani fajl u Received folder
                    string receivedDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Received");
                    if (!Directory.Exists(receivedDir))
                    {
                        Directory.CreateDirectory(receivedDir);
                    }

                    string outputPath = Path.Combine(receivedDir, e.Message.FileName);
                    await _decryptionService.SaveDecryptedFileAsync(decryptedData, outputPath);

                    AddLogEntry($"File decrypted and saved: {outputPath}", Color.Green);
                    Log.Information("File decrypted successfully: {OutputPath}", outputPath);

                    // Build message box with metadata info
                    string metadataInfo = "";
                    if (e.Message.Metadata != null)
                    {
                        metadataInfo = $"\n\n=== Original File Info ===\n" +
                                      $"Name: {e.Message.Metadata.OriginalFileName}\n" +
                                      $"Size: {FormatFileSize(e.Message.Metadata.FileSize)}\n" +
                                      $"Created: {e.Message.Metadata.CreationTime:yyyy-MM-dd HH:mm:ss}\n" +
                                      $"Encryption: {e.Message.Metadata.EncryptionAlgorithm}\n" +
                                      $"Hash: {e.Message.Metadata.HashAlgorithm}";
                    }

                    MessageBox.Show($"File received and decrypted successfully!\n\n" +
                                   $"File: {e.Message.FileName}\n" +
                                   $"Saved to: {outputPath}\n" +
                                   $"Hash: VALID. {metadataInfo}",
                        "File Received", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    AddLogEntry("Hash verification: FAILED - File may be corrupted!", Color.Red);
                    Log.Warning("Hash verification failed for received file: {FileName}", e.Message.FileName);

                    MessageBox.Show($"File received but hash verification FAILED!\n\nFile: {e.Message.FileName}\nThe file may be corrupted or tampered with.",
                        "Hash Verification Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                AddLogEntry($"Error processing received file: {ex.Message}", Color.Red, ex);
                MessageBox.Show($"Error processing received file: {ex.Message}",
                    "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnTransferProgress(object? sender, TransferProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnTransferProgress(sender, e)));
                return;
            }

            string direction = e.Direction == TransferDirection.Sending ? "Sending" : "Receiving";
            UpdateOrAddProgressEntry($"{direction}: {e.FileName} - {e.ProgressPercentage}% ({FormatFileSize(e.BytesTransferred)}/{FormatFileSize(e.TotalBytes)})",
                Color.Blue);
        }

        private void OnConnectionStatus(object? sender, ConnectionStatusEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnConnectionStatus(sender, e)));
                return;
            }

            Color color = e.IsConnected ? Color.Green : Color.Orange;
            AddLogEntry($"Connection: {e.StatusMessage} ({e.Peer})", color);
        }

        private void OnNetworkError(object? sender, NetworkErrorEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnNetworkError(sender, e)));
                return;
            }

            AddLogEntry($"Network error: {e.ErrorMessage}", Color.Red);
        }

        #endregion

        #region Encryption/Decryption Progress

        private void OnEncryptionProgress(object? sender, FileProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnEncryptionProgress(sender, e)));
                return;
            }

            UpdateOrAddProgressEntry($"Encrypting: {e.FileName} - {e.ProgressPercentage}% ({FormatFileSize(e.BytesProcessed)}/{FormatFileSize(e.TotalBytes)})",
                Color.Blue);
        }

        private void OnDecryptionProgress(object? sender, FileProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnDecryptionProgress(sender, e)));
                return;
            }

            UpdateOrAddProgressEntry($"Decrypting: {e.FileName} - {e.ProgressPercentage}%", Color.Blue);
        }

        #endregion

        #region Logging Methods

        private void AddLogEntry(string message, Color color, Exception? ex = null)
        {
            var item = new ListViewItem(message)
            {
                ForeColor = color
            };
            listViewLog.Items.Add(item);
            listViewLog.EnsureVisible(listViewLog.Items.Count - 1);

            // Log to Serilog (with exception if provided)
            LogToSerilog(message, color, ex);
        }

        private void UpdateOrAddProgressEntry(string message, Color color)
        {
            // Ako poslednji unos pocinje sa "Encrypting:" ili "Decrypting:" ili "Sending:" ili "Receiving:", update it
            if (listViewLog.Items.Count > 0)
            {
                var lastItem = listViewLog.Items[listViewLog.Items.Count - 1];
                string lastText = lastItem.Text;

                if (lastText.StartsWith("Encrypting:") || lastText.StartsWith("Decrypting:") ||
                    lastText.StartsWith("Sending:") || lastText.StartsWith("Receiving:"))
                {
                    lastItem.Text = message;
                    lastItem.ForeColor = color;
                    return;
                }
            }

            AddLogEntry(message, color);
        }

        private void LogToSerilog(string message, Color color, Exception? ex = null)
        {
            if (color == Color.Red)
            {
                if (ex != null)
                    Log.Error(ex, message);
                else
                    Log.Error(message);
            }
            else if (color == Color.Orange)
            {
                if (ex != null)
                    Log.Warning(ex, message);
                else
                    Log.Warning(message);
            }
            else if (color == Color.Green || color == Color.Blue)
            {
                if (ex != null)
                    Log.Information(ex, message);
                else
                    Log.Information(message);
            }
            else
            {
                if (ex != null)
                    Log.Debug(ex, message);
                else
                    Log.Debug(message);
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

        #endregion
    }
}
