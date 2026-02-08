using System;

namespace CryptoFileExchange.Models
{
    /// <summary>
    /// Informacije o mreznom peer-u (IP adresa i port)
    /// </summary>
    public class NetworkPeer
    {
        public required string IPAddress { get; set; }
        public int Port { get; set; }

        public override string ToString()
        {
            return $"{IPAddress}:{Port}";
        }
    }

    /// <summary>
    /// Event arguments za prijem fajla
    /// </summary>
    public class FileReceivedEventArgs : EventArgs
    {
        public required FileTransferMessage Message { get; set; }
        public required NetworkPeer Sender { get; set; }
        public DateTime ReceivedTime { get; set; }
        public bool HashVerified { get; set; }
    }

    /// <summary>
    /// Event arguments za progres prenosa
    /// </summary>
    public class TransferProgressEventArgs : EventArgs
    {
        public required string FileName { get; set; }
        public long BytesTransferred { get; set; }
        public long TotalBytes { get; set; }
        public int ProgressPercentage { get; set; }
        public TransferDirection Direction { get; set; }
    }

    /// <summary>
    /// Smer prenosa (slanje ili primanje)
    /// </summary>
    public enum TransferDirection
    {
        Sending,
        Receiving
    }

    /// <summary>
    /// Event arguments za status konekcije
    /// </summary>
    public class ConnectionStatusEventArgs : EventArgs
    {
        public bool IsConnected { get; set; }
        public required NetworkPeer Peer { get; set; }
        public required string StatusMessage { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Event arguments za greske u mreznoj komunikaciji
    /// </summary>
    public class NetworkErrorEventArgs : EventArgs
    {
        public required string ErrorMessage { get; set; }
        public Exception? Exception { get; set; }
        public DateTime ErrorTime { get; set; }
        public NetworkPeer? Peer { get; set; }
    }
}

