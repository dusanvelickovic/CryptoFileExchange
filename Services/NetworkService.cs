using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CryptoFileExchange.Models;
using Serilog;

namespace CryptoFileExchange.Services
{
    /// <summary>
    /// Servis za TCP mreznu komunikaciju (Server i Client rezim)
    /// </summary>
    public class NetworkService
    {
        private TcpListener? _listener;
        private CancellationTokenSource? _listenerCancellation;
        private bool _isListening;

        // Konfiguracija
        private const int BUFFER_SIZE = 8192; // 8 KB buffer
        private const int MAX_MESSAGE_SIZE = 1073741824; // 1 GB max

        // Events
        public event EventHandler<FileReceivedEventArgs>? FileReceived;
        public event EventHandler<TransferProgressEventArgs>? TransferProgress;
        public event EventHandler<ConnectionStatusEventArgs>? ConnectionStatus;
        public event EventHandler<NetworkErrorEventArgs>? NetworkError;

        /// <summary>
        /// Pokrece TCP server koji slusa na odredjenom portu
        /// </summary>
        public async Task StartListeningAsync(int port)
        {
            if (_isListening)
            {
                Log.Warning("NetworkService is already listening on port {Port}", port);
                return;
            }

            try
            {
                _listenerCancellation = new CancellationTokenSource();
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                _isListening = true;

                Log.Information("NetworkService started listening on port {Port}", port);
                OnConnectionStatus(new ConnectionStatusEventArgs
                {
                    IsConnected = true,
                    Peer = new NetworkPeer { IPAddress = "0.0.0.0", Port = port },
                    StatusMessage = $"Listening on port {port}",
                    Timestamp = DateTime.Now
                });

                // Async loop za prihvatanje konekcija
                await AcceptClientsLoopAsync(_listenerCancellation.Token);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to start listening on port {Port}", port);
                OnNetworkError(new NetworkErrorEventArgs
                {
                    ErrorMessage = $"Failed to start listening: {ex.Message}",
                    Exception = ex,
                    ErrorTime = DateTime.Now
                });
                throw;
            }
        }

        /// <summary>
        /// Zaustavlja TCP server
        /// </summary>
        public void StopListening()
        {
            if (!_isListening)
            {
                Log.Warning("NetworkService is not listening");
                return;
            }

            try
            {
                _listenerCancellation?.Cancel();
                _listener?.Stop();
                _isListening = false;

                Log.Information("NetworkService stopped listening");
                OnConnectionStatus(new ConnectionStatusEventArgs
                {
                    IsConnected = false,
                    Peer = new NetworkPeer { IPAddress = "0.0.0.0", Port = 0 },
                    StatusMessage = "Server stopped",
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while stopping NetworkService");
                throw;
            }
        }

        /// <summary>
        /// Loop za prihvatanje klijentskih konekcija
        /// </summary>
        private async Task AcceptClientsLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _listener != null)
            {
                try
                {
                    // Cekaj klijentsku konekciju
                    TcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken);

                    // Obradi klijenta u odvojenom Task-u
                    _ = Task.Run(async () => await HandleClientAsync(client), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Normalno - server se zaustavlja
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error accepting client connection");
                    OnNetworkError(new NetworkErrorEventArgs
                    {
                        ErrorMessage = $"Error accepting connection: {ex.Message}",
                        Exception = ex,
                        ErrorTime = DateTime.Now
                    });
                }
            }
        }

        /// <summary>
        /// Obradjuje klijentsku konekciju (prima fajl)
        /// </summary>
        private async Task HandleClientAsync(TcpClient client)
        {
            NetworkPeer? peer = null;

            try
            {
                // Uzmi peer informacije
                var remoteEndPoint = (IPEndPoint?)client.Client.RemoteEndPoint;
                if (remoteEndPoint != null)
                {
                    peer = new NetworkPeer
                    {
                        IPAddress = remoteEndPoint.Address.ToString(),
                        Port = remoteEndPoint.Port
                    };

                    Log.Information("Client connected from {Peer}", peer);
                    OnConnectionStatus(new ConnectionStatusEventArgs
                    {
                        IsConnected = true,
                        Peer = peer,
                        StatusMessage = "Client connected",
                        Timestamp = DateTime.Now
                    });
                }

                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    // Primi poruku
                    FileTransferMessage message = await ReceiveMessageAsync(stream);

                    Log.Information("Received file transfer: {Message}", message);

                    // Okini FileReceived dogadjaj
                    OnFileReceived(new FileReceivedEventArgs
                    {
                        Message = message,
                        Sender = peer ?? new NetworkPeer { IPAddress = "Unknown", Port = 0 },
                        ReceivedTime = DateTime.Now,
                        HashVerified = false // Verifikacija ce se obaviti u UI sloju
                    });

                    // Posalji ACK potvrdu
                    await SendAckAsync(stream, "File received successfully");
                }

                Log.Information("Client disconnected: {Peer}", peer);
                OnConnectionStatus(new ConnectionStatusEventArgs
                {
                    IsConnected = false,
                    Peer = peer ?? new NetworkPeer { IPAddress = "Unknown", Port = 0 },
                    StatusMessage = "Client disconnected",
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error handling client: {Peer}", peer);
                OnNetworkError(new NetworkErrorEventArgs
                {
                    ErrorMessage = $"Error handling client: {ex.Message}",
                    Exception = ex,
                    ErrorTime = DateTime.Now,
                    Peer = peer
                });
            }
        }

        /// <summary>
        /// Prima FileTransferMessage iz mreznog stream-a
        /// </summary>
        private async Task<FileTransferMessage> ReceiveMessageAsync(NetworkStream stream)
        {
            // Prvo procitaj celu poruku u memoriju
            using (var memoryStream = new MemoryStream())
            {
                byte[] buffer = new byte[BUFFER_SIZE];
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await memoryStream.WriteAsync(buffer, 0, bytesRead);

                    // Proveri da li smo procitali sve (ako stream vise ne salje)
                    if (!stream.DataAvailable)
                        break;

                    // Safety check - ne dozvoli preko MAX_MESSAGE_SIZE
                    if (memoryStream.Length > MAX_MESSAGE_SIZE)
                    {
                        throw new InvalidOperationException($"Message size exceeds maximum ({MAX_MESSAGE_SIZE} bytes)");
                    }
                }

                byte[] messageBytes = memoryStream.ToArray();
                Log.Debug("Received {ByteCount} bytes from network", messageBytes.Length);

                // Deserijalizuj poruku
                return FileTransferMessage.FromBytes(messageBytes);
            }
        }

        /// <summary>
        /// Salje fajl na odredjenu IP adresu i port (Client rezim)
        /// </summary>
        public async Task<bool> SendFileAsync(string ipAddress, int port, FileTransferMessage message)
        {
            NetworkPeer peer = new NetworkPeer { IPAddress = ipAddress, Port = port };

            try
            {
                Log.Information("Connecting to {Peer} to send file: {FileName}", peer, message.FileName);

                using (TcpClient client = new TcpClient())
                {
                    // Povezi se na server
                    await client.ConnectAsync(ipAddress, port);

                    Log.Information("Connected to {Peer}", peer);
                    OnConnectionStatus(new ConnectionStatusEventArgs
                    {
                        IsConnected = true,
                        Peer = peer,
                        StatusMessage = "Connected to server",
                        Timestamp = DateTime.Now
                    });

                    using (NetworkStream stream = client.GetStream())
                    {
                        // Posalji poruku
                        await SendMessageAsync(stream, message);

                        Log.Information("File sent to {Peer}: {FileName}", peer, message.FileName);

                        // Primi ACK odgovor
                        string response = await ReceiveAckAsync(stream);
                        Log.Information("Received ACK from {Peer}: {Response}", peer, response);
                    }

                    OnConnectionStatus(new ConnectionStatusEventArgs
                    {
                        IsConnected = false,
                        Peer = peer,
                        StatusMessage = "Disconnected from server",
                        Timestamp = DateTime.Now
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to send file to {Peer}", peer);
                OnNetworkError(new NetworkErrorEventArgs
                {
                    ErrorMessage = $"Failed to send file: {ex.Message}",
                    Exception = ex,
                    ErrorTime = DateTime.Now,
                    Peer = peer
                });
                return false;
            }
        }

        /// <summary>
        /// Salje FileTransferMessage preko mreznog stream-a
        /// </summary>
        private async Task SendMessageAsync(NetworkStream stream, FileTransferMessage message)
        {
            byte[] messageBytes = message.ToBytes();
            long totalBytes = messageBytes.Length;
            long bytesSent = 0;

            Log.Debug("Sending {TotalBytes} bytes", totalBytes);

            // Salji u chunk-ovima sa progress-om
            int offset = 0;
            while (offset < messageBytes.Length)
            {
                int chunkSize = Math.Min(BUFFER_SIZE, messageBytes.Length - offset);
                await stream.WriteAsync(messageBytes, offset, chunkSize);
                offset += chunkSize;
                bytesSent += chunkSize;

                // Progress event
                int progressPercentage = (int)((bytesSent * 100) / totalBytes);
                OnTransferProgress(new TransferProgressEventArgs
                {
                    FileName = message.FileName,
                    BytesTransferred = bytesSent,
                    TotalBytes = totalBytes,
                    ProgressPercentage = progressPercentage,
                    Direction = TransferDirection.Sending
                });
            }

            await stream.FlushAsync();
            Log.Debug("Message sent successfully");
        }

        /// <summary>
        /// Salje ACK odgovor
        /// </summary>
        private async Task SendAckAsync(NetworkStream stream, string message)
        {
            byte[] ackBytes = System.Text.Encoding.UTF8.GetBytes($"ACK:{message}");
            await stream.WriteAsync(ackBytes, 0, ackBytes.Length);
            await stream.FlushAsync();
        }

        /// <summary>
        /// Prima ACK odgovor
        /// </summary>
        private async Task<string> ReceiveAckAsync(NetworkStream stream)
        {
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            return System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }

        // Event trigger metode
        protected virtual void OnFileReceived(FileReceivedEventArgs e) => FileReceived?.Invoke(this, e);
        protected virtual void OnTransferProgress(TransferProgressEventArgs e) => TransferProgress?.Invoke(this, e);
        protected virtual void OnConnectionStatus(ConnectionStatusEventArgs e) => ConnectionStatus?.Invoke(this, e);
        protected virtual void OnNetworkError(NetworkErrorEventArgs e) => NetworkError?.Invoke(this, e);

        public bool IsListening => _isListening;
    }
}
