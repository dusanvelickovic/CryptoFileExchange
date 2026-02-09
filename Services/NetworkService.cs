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
        /// LENGTH-PREFIXED: Prvo cita 4 bytes (Int32) sa duzinom, zatim tu duzinu podataka
        /// </summary>
        private async Task<FileTransferMessage> ReceiveMessageAsync(NetworkStream stream)
        {
            // 1. PRVO procitaj duzinu poruke (4 bytes - Int32)
            byte[] lengthBuffer = new byte[4];
            int lengthBytesRead = 0;
            
            while (lengthBytesRead < 4)
            {
                int bytesRead = await stream.ReadAsync(lengthBuffer, lengthBytesRead, 4 - lengthBytesRead);
                if (bytesRead == 0)
                    throw new InvalidOperationException("Connection closed while reading message length");
                lengthBytesRead += bytesRead;
            }

            int expectedLength = BitConverter.ToInt32(lengthBuffer, 0);
            Log.Debug("Expecting to receive {Length} bytes (read from length prefix)", expectedLength);

            // Safety check
            if (expectedLength <= 0 || expectedLength > MAX_MESSAGE_SIZE)
            {
                throw new InvalidOperationException($"Invalid message length: {expectedLength}");
            }

            // 2. Sada procitaj tacno toliko bytes koliko ocekujemo
            byte[] messageBytes = new byte[expectedLength];
            int totalBytesRead = 0;

            while (totalBytesRead < expectedLength)
            {
                int bytesRead = await stream.ReadAsync(messageBytes, totalBytesRead, expectedLength - totalBytesRead);
                
                if (bytesRead == 0)
                {
                    throw new InvalidOperationException($"Connection closed prematurely. Expected {expectedLength} bytes, received {totalBytesRead} bytes");
                }

                totalBytesRead += bytesRead;
                
                // Progress logging every 10%
                if (totalBytesRead % (expectedLength / 10 + 1) == 0)
                {
                    int progress = (totalBytesRead * 100) / expectedLength;
                    Log.Debug("Receiving: {Progress}% ({Current}/{Total} bytes)", progress, totalBytesRead, expectedLength);
                }
            }

            Log.Debug("Received {ByteCount} bytes from network (complete)", totalBytesRead);

            // === DEBUG: Ispisi prvih 100 bytes ===
            if (messageBytes.Length >= 100)
            {
                string preview = BitConverter.ToString(messageBytes, 0, 100).Replace("-", " ");
                Log.Debug("First 100 bytes: {Preview}", preview);
            }

            // 3. Deserijalizuj poruku
            FileTransferMessage deserializedMessage = FileTransferMessage.FromBytes(messageBytes);

            // === DEBUG: Proveri deserijalizovanu poruku ===
            Log.Debug("Deserialized message:");
            Log.Debug("  FileName: {FileName}", deserializedMessage.FileName);
            Log.Debug("  FileSize: {FileSize}", deserializedMessage.FileSize);
            Log.Debug("  FileHash: {Hash}", deserializedMessage.FileHash);
            Log.Debug("  EncryptedData length: {Length}", deserializedMessage.EncryptedData?.Length ?? 0);

            return deserializedMessage;
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
            catch (SocketException ex)
            {
                string errorDetails = ex.SocketErrorCode switch
                {
                    SocketError.ConnectionRefused => "Connection refused - Server is not running on target machine or firewall is blocking the port",
                    SocketError.HostUnreachable => "Host unreachable - Check IP address and network connectivity",
                    SocketError.TimedOut => "Connection timed out - Server did not respond",
                    SocketError.NetworkUnreachable => "Network unreachable - Machines are not on the same network",
                    _ => $"Socket error: {ex.SocketErrorCode}"
                };
                
                Log.Error(ex, "Network error connecting to {Peer}: {ErrorDetails}", peer, errorDetails);
                OnNetworkError(new NetworkErrorEventArgs
                {
                    ErrorMessage = $"Failed to send file: {errorDetails}",
                    Exception = ex,
                    ErrorTime = DateTime.Now,
                    Peer = peer
                });
                return false;
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
        /// LENGTH-PREFIXED: Prvo salje 4 bytes (Int32) sa duzinom poruke, zatim poruku
        /// </summary>
        private async Task SendMessageAsync(NetworkStream stream, FileTransferMessage message)
        {
            byte[] messageBytes = message.ToBytes();
            int messageLength = messageBytes.Length;

            Log.Debug("Sending message length: {Length} bytes", messageLength);

            // 1. PRVO posalji duzinu poruke (4 bytes - Int32)
            byte[] lengthPrefix = BitConverter.GetBytes(messageLength);
            await stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);

            long totalBytes = messageBytes.Length;
            long bytesSent = 0;

            Log.Debug("Sending {TotalBytes} bytes of message data", totalBytes);

            // === DEBUG: Ispisi prvih 100 bytes ===
            if (messageBytes.Length >= 100)
            {
                string preview = BitConverter.ToString(messageBytes, 0, 100).Replace("-", " ");
                Log.Debug("First 100 bytes to send: {Preview}", preview);
            }

            // 2. Zatim salji poruku u chunk-ovima sa progress-om
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
            Log.Debug("Message sent successfully (length-prefixed)");
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
