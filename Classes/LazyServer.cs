using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LazyServer
{
    public enum MessageType : byte
    {
        Message = 0x01,
        FileOffer = 0x02,
        FileAccept = 0x03,
        FileReject = 0x04,
        FileData = 0x05,
        FileComplete = 0x06,
        Handshake = 0x07,
        Heartbeat = 0xFE,
        UdpInit = 0x08,
        UdpAck = 0x09,
        UdpFileChunk = 0x0A,
        UdpFileComplete = 0x0B,
        FileCompletedNotification = 0x0C
    }

    public class FileMetadata
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public string Metadata { get; set; }
    }

    public class MessageEventArgs : EventArgs
    {
        public string ClientId { get; set; }
        public string Message { get; set; }
    }

    public class FileRequest
    {
        public byte[] FileBytes { get; set; }
        public FileMetadata FileMeta { get; set; }
        public string TransferId { get; set; } = Guid.NewGuid().ToString();
        public string Metadata { get; set; }
    }

    public class FileOfferEventArgs : EventArgs
    {
        public string ClientId { get; set; }
        public string TransferId { get; set; }
        public string Metadata { get; set; }
        public FileRequest FileRequest { get; set; }
    }

    public class FileOfferWithMetaEventArgs : EventArgs
    {
        public string ClientId { get; set; }
        public FileRequest FileRequest { get; set; }
    }

    public class FileProgressEventArgs : EventArgs
    {
        public string ClientId { get; set; }
        public string TransferId { get; set; }
        public long BytesTransferred { get; set; }
        public long TotalBytes { get; set; }
        public FileRequest FileRequest { get; set; }
    }

    public class FileCompletedEventArgs : EventArgs
    {
        public string ClientId { get; set; }
        public FileRequest FileRequest { get; set; }
        public bool Success { get; set; }
        public string Metadata { get; set; }
    }

    public class UdpFileChunk
    {
        public string TransferId { get; set; }
        public int ChunkIndex { get; set; }
        public int TotalChunks { get; set; }
        public byte[] Data { get; set; }
        public bool IsLast { get; set; }
    }

    public class UdpFileTransfer
    {
        public string TransferId { get; set; }
        public string ClientId { get; set; }
        public IPEndPoint ClientEndPoint { get; set; }
        public Dictionary<int, byte[]> ReceivedChunks { get; set; } = new Dictionary<int, byte[]>();
        public int TotalChunks { get; set; }
        public long TotalBytes { get; set; }
        public long ReceivedBytes { get; set; }
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
        public string Metadata { get; set; }
        public HashSet<int> AckedChunks { get; set; } = new HashSet<int>();
    }

    public class LazyServerHost
    {
        private TcpListener _listener;
        private UdpClient _udpListener;
        private X509Certificate2 _serverCertificate;
        private readonly ConcurrentDictionary<string, ClientConnection> _clients = new ConcurrentDictionary<string, ClientConnection>();
        private readonly ConcurrentDictionary<string, UdpFileTransfer> _udpTransfers = new ConcurrentDictionary<string, UdpFileTransfer>();
        private CancellationTokenSource _cancellationToken;
        private bool _isRunning;
        private int _udpPort;

        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler<FileOfferEventArgs> FileOfferReceived;
        public event EventHandler<FileOfferWithMetaEventArgs> FileOfferWithMetaReceived;
        public event EventHandler<FileProgressEventArgs> FileProgress;
        public event EventHandler<FileCompletedEventArgs> FileCompleted;
        public event EventHandler<string> ClientConnected;
        public event EventHandler<string> ClientDisconnected;
        public event EventHandler<FileOfferWithMetaEventArgs> UDPFileOfferWithMetaReceived;
        private readonly ConcurrentDictionary<string, CompletedTransferInfo> _completedTransfers = new ConcurrentDictionary<string, CompletedTransferInfo>();
        public class CompletedTransferInfo
        {
            public string ClientId { get; set; }
            public string Metadata { get; set; }
            public DateTime CompletedAt { get; set; }
            public int FileSize { get; set; }
            public byte[] FileBytes { get; set; } // ADD THIS TO STORE THE ACTUAL FILE BYTES
        }
        public ClientConnection GetConnectionById(string connectionId)
        {
            _clients.TryGetValue(connectionId, out var connection);
            return connection;
        }

        public bool TryGetConnectionById(string connectionId, out ClientConnection connection)
        {
            return _clients.TryGetValue(connectionId, out connection);
        }

        public IEnumerable<string> GetAllConnectionIds()
        {
            return _clients.Keys.ToList();
        }

        public IEnumerable<ClientConnection> GetAllConnections()
        {
            return _clients.Values.ToList();
        }

        public int GetConnectionCount()
        {
            return _clients.Count;
        }

        public bool IsConnectionActive(string connectionId)
        {
            return _clients.TryGetValue(connectionId, out var connection) && connection.IsConnected;
        }

        public async Task StartAsync(int port = 8888, int udpPort = 8889)
        {
            GenerateTemporaryCertificate();
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            _udpPort = udpPort;
            _udpListener = new UdpClient(udpPort);

            _cancellationToken = new CancellationTokenSource();
            _isRunning = true;

            Console.WriteLine($"LazyServer started on TCP port {port} and UDP port {udpPort}");

            _ = Task.Run(AcceptClientsAsync);
            _ = Task.Run(HandleUdpPacketsAsync);
            _ = Task.Run(CleanupExpiredTransfersAsync);

            await Task.CompletedTask;
        }

        public void Stop()
        {
            _isRunning = false;
            _cancellationToken?.Cancel();
            _listener?.Stop();
            _udpListener?.Close();

            foreach (var client in _clients.Values)
            {
                client.Disconnect();
            }
            _clients.Clear();
            _udpTransfers.Clear();

            _serverCertificate?.Dispose();
            Console.WriteLine("LazyServer stopped");
        }

        private async Task HandleUdpPacketsAsync()
        {
            Console.WriteLine("[SERVER] Starting UDP packet handler");
            while (_isRunning && !_cancellationToken.Token.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine("[SERVER] Waiting for UDP packet...");
                    var result = await _udpListener.ReceiveAsync();
                    Console.WriteLine($"[SERVER] Received UDP packet: {result.Buffer.Length} bytes from {result.RemoteEndPoint}");
                    _ = Task.Run(() => ProcessUdpPacket(result.Buffer, result.RemoteEndPoint));
                }
                catch (ObjectDisposedException)
                {
                    Console.WriteLine("[SERVER] UDP listener disposed");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SERVER] UDP error: {ex.Message}");
                }
            }
            Console.WriteLine("[SERVER] UDP packet handler stopped");
        }



        private async Task ProcessUdpPacket(byte[] data, IPEndPoint remoteEndPoint)
        {
            try
            {
                if (data.Length < 1)
                {
                    Console.WriteLine("[SERVER] UDP packet too short");
                    return;
                }

                var messageType = (MessageType)data[0];
                Console.WriteLine($"[SERVER] Processing UDP packet type: {messageType}");

                switch (messageType)
                {
                    case MessageType.UdpInit:
                        Console.WriteLine("[SERVER] Handling UdpInit");
                        await HandleUdpInit(data, remoteEndPoint);
                        break;

                    case MessageType.UdpFileChunk:
                        Console.WriteLine($"[SERVER] Handling UdpFileChunk ({data.Length} bytes)");
                        await HandleUdpFileChunk(data, remoteEndPoint);
                        break;

                    case MessageType.UdpFileComplete:
                        Console.WriteLine("[SERVER] Handling UdpFileComplete");
                        await HandleUdpFileComplete(data, remoteEndPoint);
                        break;

                    default:
                        Console.WriteLine($"[SERVER] Unknown UDP message type: {messageType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER] Error processing UDP packet: {ex.Message}");
                Console.WriteLine($"[SERVER] Stack trace: {ex.StackTrace}");
            }
        }

        private async Task HandleUdpInit(byte[] data, IPEndPoint remoteEndPoint)
        {
            if (data.Length < 37) return; // 1 byte type + 36 bytes transferId

            var transferId = Encoding.UTF8.GetString(data, 1, 36);
            var clientId = data.Length > 37 ? Encoding.UTF8.GetString(data, 37, data.Length - 37) : null;

            if (!string.IsNullOrEmpty(clientId) && _clients.ContainsKey(clientId))
            {
                var transfer = new UdpFileTransfer
                {
                    TransferId = transferId,
                    ClientId = clientId,
                    ClientEndPoint = remoteEndPoint
                };

                _udpTransfers.TryAdd(transferId, transfer);

                // Send ACK back
                var ackData = new byte[37];
                ackData[0] = (byte)MessageType.UdpAck;
                Encoding.UTF8.GetBytes(transferId).CopyTo(ackData, 1);

                await _udpListener.SendAsync(ackData, ackData.Length, remoteEndPoint);
                Console.WriteLine($"UDP initialized for transfer {transferId} from {clientId}");
            }
        }

        private async Task HandleUdpFileChunk(byte[] data, IPEndPoint remoteEndPoint)
        {
            try
            {
                // Parse: [1 byte type][36 bytes transferId][4 bytes chunkIndex][4 bytes totalChunks][1 byte isLast][remaining data]
                if (data.Length < 46) return;

                var transferId = Encoding.UTF8.GetString(data, 1, 36);
                var chunkIndex = BitConverter.ToInt32(data, 37);
                var totalChunks = BitConverter.ToInt32(data, 41);
                var isLast = data[45] == 1;
                var chunkData = new byte[data.Length - 46];
                Array.Copy(data, 46, chunkData, 0, chunkData.Length);

                Console.WriteLine($"[SERVER] Processing chunk {chunkIndex}/{totalChunks} for transfer {transferId} (isLast: {isLast})");

                if (_udpTransfers.TryGetValue(transferId, out var transfer))
                {
                    // Check if already completed
                    if (transfer.IsCompleted)
                    {
                        Console.WriteLine($"[SERVER] Transfer {transferId} already completed, ignoring chunk {chunkIndex}");
                        return;
                    }

                    transfer.LastActivity = DateTime.UtcNow;
                    transfer.ReceivedChunks[chunkIndex] = chunkData;
                    transfer.TotalChunks = totalChunks;
                    transfer.ReceivedBytes += chunkData.Length;

                    // Send ACK for this chunk
                    var ackData = new byte[41];
                    ackData[0] = (byte)MessageType.UdpAck;
                    Encoding.UTF8.GetBytes(transferId).CopyTo(ackData, 1);
                    BitConverter.GetBytes(chunkIndex).CopyTo(ackData, 37);

                    await _udpListener.SendAsync(ackData, ackData.Length, remoteEndPoint);
                    transfer.AckedChunks.Add(chunkIndex);

                    Console.WriteLine($"[SERVER] Received chunk {chunkIndex}, total chunks received: {transfer.ReceivedChunks.Count}/{totalChunks}");

                    // Fire progress event
                    var fileRequest = new FileRequest
                    {
                        TransferId = transferId,
                        FileBytes = chunkData,
                        Metadata = transfer.Metadata
                    };

                    FileProgress?.Invoke(this, new FileProgressEventArgs
                    {
                        ClientId = transfer.ClientId,
                        TransferId = transferId,
                        BytesTransferred = transfer.ReceivedBytes,
                        TotalBytes = transfer.TotalBytes,
                        FileRequest = fileRequest
                    });

                    // Check if transfer is complete - THIS IS THE KEY FIX
                    if (transfer.ReceivedChunks.Count == totalChunks && !transfer.IsCompleted)
                    {
                        Console.WriteLine($"[SERVER] All chunks received for transfer {transferId}, completing transfer");
                        transfer.IsCompleted = true; // Mark as completed before calling CompleteUdpFileTransfer
                        await CompleteUdpFileTransfer(transferId, transfer);
                    }
                }
                else
                {
                    Console.WriteLine($"[SERVER] Transfer {transferId} not found in active transfers");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling UDP file chunk: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        public class UdpFileTransfer
        {
            public string TransferId { get; set; }
            public string ClientId { get; set; }
            public IPEndPoint ClientEndPoint { get; set; }
            public Dictionary<int, byte[]> ReceivedChunks { get; set; } = new Dictionary<int, byte[]>();
            public int TotalChunks { get; set; }
            public long TotalBytes { get; set; }
            public long ReceivedBytes { get; set; }
            public DateTime LastActivity { get; set; } = DateTime.UtcNow;
            public string Metadata { get; set; }
            public HashSet<int> AckedChunks { get; set; } = new HashSet<int>();
            public bool IsCompleted { get; set; } = false; // Add this flag
        }

        private async Task HandleUdpFileComplete(byte[] data, IPEndPoint remoteEndPoint)
        {
            if (data.Length < 37) return;

            var transferId = Encoding.UTF8.GetString(data, 1, 36);

            Console.WriteLine($"[SERVER] Handling UDP file complete for transfer {transferId} from {remoteEndPoint}");

            if (_udpTransfers.TryGetValue(transferId, out var transfer))
            {
                if (!transfer.IsCompleted)
                {
                    Console.WriteLine($"[SERVER] Found transfer {transferId}, completing it");
                    await CompleteUdpFileTransfer(transferId, transfer);
                }

                _udpTransfers.TryRemove(transferId, out _);
            }
            else if (_completedTransfers.TryGetValue(transferId, out var completedInfo))
            {
                Console.WriteLine($"[SERVER] Transfer {transferId} found in completed cache - firing event");

                FileCompleted?.Invoke(this, new FileCompletedEventArgs
                {
                    ClientId = completedInfo.ClientId,
                    FileRequest = new FileRequest
                    {
                        TransferId = transferId,
                        Metadata = completedInfo.Metadata
                    },
                    Success = true,
                    Metadata = completedInfo.Metadata
                });

                // Remove from cache after firing event
                _completedTransfers.TryRemove(transferId, out _);
            }
            else
            {
                Console.WriteLine($"[SERVER] Transfer {transferId} not found anywhere - firing event anyway");

                FileCompleted?.Invoke(this, new FileCompletedEventArgs
                {
                    ClientId = "unknown",
                    FileRequest = new FileRequest
                    {
                        TransferId = transferId,
                        Metadata = "Transfer completed but details not available"
                    },
                    Success = true,
                    Metadata = "Transfer completed but details not available"
                });
            }
        }

        private async Task CompleteUdpFileTransfer(string transferId, UdpFileTransfer transfer)
        {
            try
            {
                Console.WriteLine($"[SERVER] Completing UDP file transfer: {transferId}");

                // Reconstruct the complete file
                var completeFile = new List<byte>();
                for (int i = 0; i < transfer.TotalChunks; i++)
                {
                    if (transfer.ReceivedChunks.TryGetValue(i, out var chunk))
                    {
                        completeFile.AddRange(chunk);
                    }
                    else
                    {
                        Console.WriteLine($"Missing chunk {i} for transfer {transferId}");

                        // CACHE THE FAILED TRANSFER INFO
                        _completedTransfers.TryAdd(transferId, new CompletedTransferInfo
                        {
                            ClientId = transfer.ClientId,
                            Metadata = transfer.Metadata,
                            CompletedAt = DateTime.UtcNow,
                            FileSize = 0
                        });

                        // Fire failed completion event on SERVER
                        FileCompleted?.Invoke(this, new FileCompletedEventArgs
                        {
                            ClientId = transfer.ClientId,
                            FileRequest = new FileRequest
                            {
                                TransferId = transferId,
                                Metadata = transfer.Metadata
                            },
                            Success = false,
                            Metadata = transfer.Metadata
                        });

                        // Notify client of failure
                        await NotifyClientOfCompletion(transfer.ClientId, transferId, false, transfer.Metadata);

                        return;
                    }
                }

                var fileRequest = new FileRequest
                {
                    TransferId = transferId,
                    FileBytes = completeFile.ToArray(),
                    Metadata = transfer.Metadata
                };

                Console.WriteLine($"[SERVER] About to fire UDPFileOfferWithMetaReceived for {transferId} from client {transfer.ClientId}");

                // Fire the UDPFileOfferWithMetaReceived event - THIS IS THE MAIN EVENT YOU WANT
                UDPFileOfferWithMetaReceived?.Invoke(this, new FileOfferWithMetaEventArgs
                {
                    ClientId = transfer.ClientId,
                    FileRequest = fileRequest
                });

                Console.WriteLine($"[SERVER] UDPFileOfferWithMetaReceived event fired for {transferId}");

                // Also fire the regular event for backward compatibility (optional)
                FileOfferWithMetaReceived?.Invoke(this, new FileOfferWithMetaEventArgs
                {
                    ClientId = transfer.ClientId,
                    FileRequest = fileRequest
                });

                // CACHE THE COMPLETED TRANSFER INFO BEFORE FIRING EVENT
                _completedTransfers.TryAdd(transferId, new CompletedTransferInfo
                {
                    ClientId = transfer.ClientId,
                    Metadata = transfer.Metadata,
                    CompletedAt = DateTime.UtcNow,
                    FileSize = completeFile.Count,
                    FileBytes = completeFile.ToArray() // STORE THE ACTUAL FILE BYTES
                });

                // Fire the completion event on SERVER (this is the key change)
                FileCompleted?.Invoke(this, new FileCompletedEventArgs
                {
                    ClientId = transfer.ClientId,
                    FileRequest = fileRequest,
                    Success = true,
                    Metadata = transfer.Metadata
                });

                // Notify client of successful completion
                await NotifyClientOfCompletion(transfer.ClientId, transferId, true, transfer.Metadata);

                Console.WriteLine($"[SERVER] UDP file transfer completed: {transferId} ({completeFile.Count} bytes)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error completing UDP transfer: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                // CACHE THE FAILED TRANSFER INFO
                _completedTransfers.TryAdd(transferId, new CompletedTransferInfo
                {
                    ClientId = transfer.ClientId,
                    Metadata = transfer.Metadata,
                    CompletedAt = DateTime.UtcNow,
                    FileSize = 0
                });

                // Fire failed completion event on SERVER
                FileCompleted?.Invoke(this, new FileCompletedEventArgs
                {
                    ClientId = transfer.ClientId,
                    FileRequest = new FileRequest
                    {
                        TransferId = transferId,
                        Metadata = transfer.Metadata
                    },
                    Success = false,
                    Metadata = transfer.Metadata
                });

                // Notify client of failure
                await NotifyClientOfCompletion(transfer.ClientId, transferId, false, transfer.Metadata);
            }
        }

        private async Task NotifyClientOfCompletion(string clientId, string transferId, bool success, string metadata)
        {
            if (_clients.TryGetValue(clientId, out var client))
            {
                try
                {
                    var notification = new
                    {
                        TransferId = transferId,
                        Success = success,
                        Metadata = metadata
                    };
                    var notificationJson = JsonConvert.SerializeObject(notification);
                    await SendMessage(client, MessageType.FileCompletedNotification, Encoding.UTF8.GetBytes(notificationJson));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error notifying client {clientId} of completion: {ex.Message}");
                }
            }
        }

        private async Task CleanupExpiredTransfersAsync()
        {
            while (_isRunning)
            {
                try
                {
                    // Clean up active transfers
                    var expiredActive = _udpTransfers.Where(t =>
                        DateTime.UtcNow - t.Value.LastActivity > TimeSpan.FromMinutes(5)).ToList();

                    foreach (var transfer in expiredActive)
                    {
                        _udpTransfers.TryRemove(transfer.Key, out _);
                        Console.WriteLine($"Cleaned up expired active transfer: {transfer.Key}");
                    }

                    // Clean up completed transfer cache (after 10 minutes)
                    var expiredCompleted = _completedTransfers.Where(t =>
                        DateTime.UtcNow - t.Value.CompletedAt > TimeSpan.FromMinutes(10)).ToList();

                    foreach (var transfer in expiredCompleted)
                    {
                        _completedTransfers.TryRemove(transfer.Key, out _);
                        Console.WriteLine($"Cleaned up expired completed transfer: {transfer.Key}");
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1), _cancellationToken.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private void GenerateTemporaryCertificate()
        {
            using (var rsa = RSA.Create(2048))
            {
                var req = new CertificateRequest(
                    "CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                req.CertificateExtensions.Add(new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));

                req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                    new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false)); // ServerAuth

                // Add Subject Alternative Name for localhost
                var sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName("localhost");
                sanBuilder.AddIpAddress(IPAddress.Loopback);
                sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
                req.CertificateExtensions.Add(sanBuilder.Build());

                _serverCertificate = req.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(365));

                // Important: Export and re-import with private key to ensure it's properly accessible
                var pfxData = _serverCertificate.Export(X509ContentType.Pfx, "temp");
                _serverCertificate.Dispose();
                _serverCertificate = new X509Certificate2(pfxData, "temp", X509KeyStorageFlags.Exportable);
            }
            Console.WriteLine("Temporary SSL certificate generated");
        }

        private async Task AcceptClientsAsync()
        {
            while (_isRunning && !_cancellationToken.Token.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(tcpClient));
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient tcpClient)
        {
            var clientId = Guid.NewGuid().ToString();
            ClientConnection client = null;

            try
            {
                var sslStream = new SslStream(tcpClient.GetStream(), false);

                // Use more compatible SSL/TLS protocols and authentication options
                await sslStream.AuthenticateAsServerAsync(
                    _serverCertificate,
                    clientCertificateRequired: false,
                    enabledSslProtocols: SslProtocols.Tls12 | SslProtocols.Tls13,
                    checkCertificateRevocation: false);

                client = new ClientConnection(clientId, tcpClient, sslStream);
                client.UdpPort = _udpPort; // Set UDP port info
                _clients.TryAdd(clientId, client);

                ClientConnected?.Invoke(this, clientId);
                Console.WriteLine($"Client {clientId} connected with SSL");

                await HandleClientMessages(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client {clientId} error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                _clients.TryRemove(clientId, out _);
                client?.Disconnect();
                ClientDisconnected?.Invoke(this, clientId);
                Console.WriteLine($"Client {clientId} disconnected");
            }
        }

        private async Task HandleClientMessages(ClientConnection client)
        {
            var buffer = new byte[4096];

            while (client.IsConnected)
            {
                try
                {
                    var received = await client.SslStream.ReadAsync(buffer, 0, buffer.Length);
                    if (received == 0) break;

                    await ProcessMessage(client, buffer, received);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error handling client {client.Id}: {ex.Message}");
                    break;
                }
            }
        }

        private async Task ProcessMessage(ClientConnection client, byte[] buffer, int length)
        {
            if (length < 5) return;

            var messageType = (MessageType)buffer[0];
            var payloadLength = BitConverter.ToInt32(buffer, 1);

            var payload = new byte[payloadLength];
            Array.Copy(buffer, 5, payload, 0, Math.Min(payloadLength, length - 5));

            var totalReceived = length - 5;
            while (totalReceived < payloadLength)
            {
                var remaining = await client.SslStream.ReadAsync(payload, totalReceived, payloadLength - totalReceived);
                if (remaining == 0) break;
                totalReceived += remaining;
            }

            await HandleMessageType(client, messageType, payload);
        }

        private async Task HandleMessageType(ClientConnection client, MessageType messageType, byte[] payload)
        {
            switch (messageType)
            {
                case MessageType.Message:
                    var message = Encoding.UTF8.GetString(payload);
                    MessageReceived?.Invoke(this, new MessageEventArgs { ClientId = client.Id, Message = message });
                    break;

                case MessageType.FileOffer:
                    var offerJson = Encoding.UTF8.GetString(payload);
                    var offerData = JObject.Parse(offerJson);
                    var transferId = offerData["TransferId"]?.ToString();
                    var metadata = offerData["Metadata"]?.ToString() ?? "";

                    var fileRequest = new FileRequest
                    {
                        TransferId = transferId,
                        Metadata = metadata
                    };

                    if (offerData["FileBytes"] != null)
                    {
                        fileRequest.FileBytes = Convert.FromBase64String(offerData["FileBytes"].ToString());
                        FileOfferWithMetaReceived?.Invoke(this, new FileOfferWithMetaEventArgs
                        {
                            ClientId = client.Id,
                            FileRequest = fileRequest
                        });
                    }
                    else
                    {
                        // Setup UDP transfer tracking
                        if (_udpTransfers.ContainsKey(transferId))
                        {
                            _udpTransfers[transferId].Metadata = metadata;
                            _udpTransfers[transferId].ClientId = client.Id;
                        }

                        FileOfferReceived?.Invoke(this, new FileOfferEventArgs
                        {
                            ClientId = client.Id,
                            TransferId = transferId,
                            Metadata = metadata,
                            FileRequest = fileRequest
                        });
                    }
                    break;

                case MessageType.FileData:
                    await HandleFileData(client, payload);
                    break;
            }
        }

        private async Task HandleFileData(ClientConnection client, byte[] payload)
        {
            var transferId = Encoding.UTF8.GetString(payload, 0, 36);
            var fileData = new byte[payload.Length - 36];
            Array.Copy(payload, 36, fileData, 0, fileData.Length);

            if (!client.ActiveTransfers.ContainsKey(transferId))
            {
                client.ActiveTransfers[transferId] = new FileTransfer { Id = transferId };
            }

            var transfer = client.ActiveTransfers[transferId];
            transfer.BytesTransferred += fileData.Length;

            var fileRequest = new FileRequest
            {
                TransferId = transferId,
                FileBytes = fileData,
                Metadata = transfer.Metadata
            };

            FileProgress?.Invoke(this, new FileProgressEventArgs
            {
                ClientId = client.Id,
                TransferId = transferId,
                BytesTransferred = transfer.BytesTransferred,
                TotalBytes = transfer.TotalSize,
                FileRequest = fileRequest
            });
        }

        public async Task SendMessageToClient(string clientId, string message)
        {
            if (_clients.TryGetValue(clientId, out var client))
            {
                await SendMessage(client, MessageType.Message, Encoding.UTF8.GetBytes(message));
            }
        }

        public async Task SendMessageToConnection(ClientConnection connection, string message)
        {
            if (connection != null && connection.IsConnected)
            {
                await SendMessage(connection, MessageType.Message, Encoding.UTF8.GetBytes(message));
            }
        }

        public async Task BroadcastMessage(string message)
        {
            var data = Encoding.UTF8.GetBytes(message);
            var tasks = new List<Task>();

            foreach (var client in _clients.Values)
            {
                tasks.Add(SendMessage(client, MessageType.Message, data));
            }

            await Task.WhenAll(tasks);
        }

        private async Task SendMessage(ClientConnection client, MessageType type, byte[] payload)
        {
            try
            {
                var message = new byte[5 + payload.Length];
                message[0] = (byte)type;
                BitConverter.GetBytes(payload.Length).CopyTo(message, 1);
                payload.CopyTo(message, 5);

                await client.SslStream.WriteAsync(message, 0, message.Length);
                await client.SslStream.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message to client {client.Id}: {ex.Message}");
            }
        }

        /// <summary>
        /// Send an in‐memory byte[] as a "file" (base64‐encoded offer) and complete right away.
        /// </summary>
        public async Task SendFileBytesToClient(string clientId, byte[] fileBytes, string metadata = "")
        {
            if (!_clients.TryGetValue(clientId, out var client) || !client.IsConnected)
                throw new InvalidOperationException($"Client {clientId} not connected.");

            // 1) send the offer (with the entire payload in FileBytes)
            var transferId = Guid.NewGuid().ToString();
            var offer = new
            {
                TransferId = transferId,
                Metadata = metadata,
                FileBytes = Convert.ToBase64String(fileBytes)
            };
            var offerJson = JsonConvert.SerializeObject(offer);
            await SendMessage(client, MessageType.FileOffer, Encoding.UTF8.GetBytes(offerJson));

            // 2) immediately signal completion
            await SendMessage(client, MessageType.FileComplete, Encoding.UTF8.GetBytes(transferId));
        }

        /// <summary>
        /// Stream a file to client using UDP for better performance
        /// </summary>
        public async Task SendFileToClientUdp(string clientId, byte[] fileBytes, string metadata = "")
        {
            if (!_clients.TryGetValue(clientId, out var client) || !client.IsConnected)
                throw new InvalidOperationException($"Client {clientId} not connected.");

            var transferId = Guid.NewGuid().ToString();

            // 1) Send file offer via TCP
            var offer = new
            {
                TransferId = transferId,
                Metadata = metadata,
                FileSize = fileBytes.Length,
                UseUdp = true,
                UdpPort = _udpPort
            };
            var offerJson = JsonConvert.SerializeObject(offer);
            await SendMessage(client, MessageType.FileOffer, Encoding.UTF8.GetBytes(offerJson));

            // 2) Wait a bit for UDP initialization (in a real implementation, wait for client ACK)
            await Task.Delay(100);

            // 3) Send file via UDP chunks
            await SendFileViaUdp(transferId, fileBytes, client.TcpClient.Client.RemoteEndPoint as IPEndPoint);
        }

        private async Task SendFileViaUdp(string transferId, byte[] fileBytes, IPEndPoint clientEndPoint)
        {
            const int CHUNK_SIZE = 1400; // Safe UDP packet size
            var totalChunks = (int)Math.Ceiling((double)fileBytes.Length / CHUNK_SIZE);

            for (int i = 0; i < totalChunks; i++)
            {
                var chunkStart = i * CHUNK_SIZE;
                var chunkSize = Math.Min(CHUNK_SIZE, fileBytes.Length - chunkStart);
                var chunkData = new byte[chunkSize];
                Array.Copy(fileBytes, chunkStart, chunkData, 0, chunkSize);

                // Create UDP packet: [1 byte type][36 bytes transferId][4 bytes chunkIndex][4 bytes totalChunks][1 byte isLast][data]
                var packet = new byte[46 + chunkSize];
                packet[0] = (byte)MessageType.UdpFileChunk;
                Encoding.UTF8.GetBytes(transferId).CopyTo(packet, 1);
                BitConverter.GetBytes(i).CopyTo(packet, 37);
                BitConverter.GetBytes(totalChunks).CopyTo(packet, 41);
                packet[45] = (byte)(i == totalChunks - 1 ? 1 : 0);
                chunkData.CopyTo(packet, 46);

                await _udpListener.SendAsync(packet, packet.Length, clientEndPoint);

                // Small delay to prevent overwhelming
                if (i % 10 == 0)
                    await Task.Delay(1);
            }

            Console.WriteLine($"Sent {totalChunks} UDP chunks for transfer {transferId}");
        }

        /// <summary>
        /// Stream a file off disk to the client via UDP.
        /// </summary>
        public async Task SendFileToClient(string clientId, string filePath, string metadata = "")
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            var fileBytes = await FileHelper.ReadAllBytesAsync(filePath);
            await SendFileToClientUdp(clientId, fileBytes, metadata);
        }

        public async Task SendUDPFileBytes(string clientId, byte[] fileBytes, string metadata = "")
        {
            if (!_clients.TryGetValue(clientId, out var client) || !client.IsConnected)
                throw new InvalidOperationException($"Client {clientId} not connected.");

            var transferId = Guid.NewGuid().ToString();

            // 1) Send file offer via TCP
            var offer = new
            {
                TransferId = transferId,
                Metadata = metadata,
                FileSize = fileBytes.Length,
                UseUdp = true,
                UdpPort = _udpPort
            };
            var offerJson = JsonConvert.SerializeObject(offer);
            await SendMessage(client, MessageType.FileOffer, Encoding.UTF8.GetBytes(offerJson));

            // 2) Wait a bit for UDP initialization (in a real implementation, wait for client ACK)
            await Task.Delay(100);

            // 3) Send file via UDP chunks
            await SendFileViaUdp(transferId, fileBytes, client.TcpClient.Client.RemoteEndPoint as IPEndPoint);

            // 4) Fire the custom UDPFileOfferWithMetaReceived event
            UDPFileOfferWithMetaReceived?.Invoke(this, new FileOfferWithMetaEventArgs
            {
                ClientId = clientId,
                FileRequest = new FileRequest
                {
                    TransferId = transferId,
                    FileBytes = fileBytes,
                    Metadata = metadata
                }
            });
        }
    }

    public class LazyServerClient
    {
        private TcpClient _tcpClient;
        private SslStream _sslStream;
        private UdpClient _udpClient;
        private bool _isConnected;
        private readonly ConcurrentDictionary<string, FileTransfer> _activeTransfers = new ConcurrentDictionary<string, FileTransfer>();
        private readonly ConcurrentDictionary<string, UdpFileTransfer> _udpTransfers = new ConcurrentDictionary<string, UdpFileTransfer>();
        private string _serverHostname;
        private int _serverUdpPort;
        private CancellationTokenSource _cancellationTokenSource; // Add cancellation token

        public bool IsConnected => _isConnected && _tcpClient?.Connected == true;
        public string ConnectionId { get; private set; }

        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler<FileOfferEventArgs> FileOfferReceived;
        public event EventHandler<FileOfferWithMetaEventArgs> FileOfferWithMetaReceived;
        public event EventHandler<FileProgressEventArgs> FileProgress;
        public event EventHandler<FileCompletedEventArgs> FileCompleted;
        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event EventHandler<FileOfferWithMetaEventArgs> UDPFileOfferWithMetaReceived;

        public async Task SendHeartbeat(byte[] payload = null)
        {
            if (!_isConnected) return;

            payload ??= System.Text.Encoding.UTF8.GetBytes("ping");

            var message = new byte[5 + payload.Length];
            message[0] = (byte)MessageType.Heartbeat;
            BitConverter.GetBytes(payload.Length).CopyTo(message, 1);
            payload.CopyTo(message, 5);

            try
            {
                await _sslStream.WriteAsync(message, 0, message.Length);
                await _sslStream.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Heartbeat failed: {ex.Message}");
                // Trigger disconnect if heartbeat fails
                await DisconnectAsync();
            }
        }

        public async Task ConnectAsync(string hostname = "localhost", int port = 8888, int udpPort = 8889)
        {
            try
            {
                _serverHostname = hostname;
                _serverUdpPort = udpPort;
                _cancellationTokenSource = new CancellationTokenSource(); // Initialize cancellation token

                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(hostname, port);

                // Create SSL stream with custom certificate validation
                _sslStream = new SslStream(_tcpClient.GetStream(), false, ValidateServerCertificate);

                // Authenticate as client with proper hostname and protocol settings
                await _sslStream.AuthenticateAsClientAsync(
                    hostname,                                    // Use the actual hostname
                    null,                                       // No client certificates
                    SslProtocols.Tls12 | SslProtocols.Tls13,   // Support multiple protocols
                    false                                       // Don't check certificate revocation
                );

                // Initialize UDP client with automatic port binding
                _udpClient = new UdpClient(0); // 0 = let OS choose available port

                _isConnected = true;
                ConnectionId = Guid.NewGuid().ToString();

                Connected?.Invoke(this, EventArgs.Empty);
                Console.WriteLine($"Connected to LazyServer at {hostname}:{port} (UDP: {udpPort}) with Connection ID: {ConnectionId}");

                // Start background tasks with cancellation support
                _ = Task.Run(() => ReceiveMessagesAsync(_cancellationTokenSource.Token));
                _ = Task.Run(() => HandleUdpPacketsAsync(_cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await DisconnectAsync(); // Ensure cleanup on connection failure
                throw;
            }
        }

        private async Task HandleUdpPacketsAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"[UDP] Starting UDP packet handler");
            while (_isConnected && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_udpClient == null) break;
                    Console.WriteLine("[UDP] Waiting for UDP packet...");

                    var result = await _udpClient.ReceiveAsync();
                    Console.WriteLine($"[UDP] Received {result.Buffer.Length} bytes from {result.RemoteEndPoint}");
                    _ = Task.Run(() => ProcessUdpPacket(result.Buffer, result.RemoteEndPoint));
                }
                catch (ObjectDisposedException)
                {
                    Console.WriteLine("[UDP] UDP listener disposed");
                    break;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted ||
                                                ex.SocketErrorCode == SocketError.NotSocket)
                {
                    Console.WriteLine($"[UDP] UDP error: {ex.Message}");
                    break;
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Bind"))
                {
                    Console.WriteLine($"[UDP] UDP error: {ex.Message}");
                    Console.WriteLine("UDP client binding issue, reinitializing...");
                    try
                    {
                        _udpClient?.Close();
                        _udpClient = new UdpClient(0);
                    }
                    catch
                    {
                        // If reinitializing fails, exit the loop
                        break;
                    }
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine($"[UDP] Receive error: {ex.Message}");
                    // Add a small delay to prevent tight error loops
                    await Task.Delay(100, cancellationToken);
                }
            }
        }

        private async Task ProcessUdpPacket(byte[] data, IPEndPoint remoteEndPoint)
        {
            try
            {
                if (data.Length < 1) return;

                var messageType = (MessageType)data[0];

                switch (messageType)
                {
                    case MessageType.UdpAck:
                        await HandleUdpAck(data);
                        break;

                    case MessageType.UdpFileChunk:
                        await HandleUdpFileChunk(data);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing UDP packet: {ex.Message}");
            }
        }

        private async Task HandleUdpAck(byte[] data)
        {
            if (data.Length >= 37) // transferId ACK
            {
                var transferId = Encoding.UTF8.GetString(data, 1, 36);
                Console.WriteLine($"Received UDP ACK for transfer: {transferId}");
            }
            else if (data.Length >= 41) // chunk ACK
            {
                var transferId = Encoding.UTF8.GetString(data, 1, 36);
                var chunkIndex = BitConverter.ToInt32(data, 37);
                Console.WriteLine($"Received chunk ACK for transfer {transferId}, chunk {chunkIndex}");
            }
        }

        private async Task HandleUdpFileChunk(byte[] data)
        {
            try
            {
                if (data.Length < 46) return;

                var transferId = Encoding.UTF8.GetString(data, 1, 36);
                var chunkIndex = BitConverter.ToInt32(data, 37);
                var totalChunks = BitConverter.ToInt32(data, 41);
                var isLast = data[45] == 1;
                var chunkData = new byte[data.Length - 46];
                Array.Copy(data, 46, chunkData, 0, chunkData.Length);

                if (!_udpTransfers.TryGetValue(transferId, out var transfer))
                {
                    transfer = new UdpFileTransfer
                    {
                        TransferId = transferId,
                        TotalChunks = totalChunks
                    };
                    _udpTransfers.TryAdd(transferId, transfer);
                }

                transfer.ReceivedChunks[chunkIndex] = chunkData;
                transfer.ReceivedBytes += chunkData.Length;
                transfer.LastActivity = DateTime.UtcNow;

                // Send ACK back to sender
                var serverEndPoint = new IPEndPoint(IPAddress.Parse(_serverHostname == "localhost" ? "127.0.0.1" : _serverHostname), _serverUdpPort);
                var ackData = new byte[41];
                ackData[0] = (byte)MessageType.UdpAck;
                Encoding.UTF8.GetBytes(transferId).CopyTo(ackData, 1);
                BitConverter.GetBytes(chunkIndex).CopyTo(ackData, 37);

                await _udpClient.SendAsync(ackData, ackData.Length, serverEndPoint);

                // Fire progress event
                var fileRequest = new FileRequest
                {
                    TransferId = transferId,
                    FileBytes = chunkData,
                    Metadata = transfer.Metadata
                };

                FileProgress?.Invoke(this, new FileProgressEventArgs
                {
                    TransferId = transferId,
                    BytesTransferred = transfer.ReceivedBytes,
                    TotalBytes = transfer.TotalBytes,
                    FileRequest = fileRequest
                });

                // Check if transfer is complete
                if (transfer.ReceivedChunks.Count == totalChunks && isLast)
                {
                    await CompleteUdpFileTransfer(transferId, transfer);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling UDP file chunk: {ex.Message}");
            }
        }

        private async Task CompleteUdpFileTransfer(string transferId, UdpFileTransfer transfer)
        {
            try
            {
                // Reconstruct the complete file
                var completeFile = new List<byte>();
                for (int i = 0; i < transfer.TotalChunks; i++)
                {
                    if (transfer.ReceivedChunks.TryGetValue(i, out var chunk))
                    {
                        completeFile.AddRange(chunk);
                    }
                    else
                    {
                        Console.WriteLine($"Missing chunk {i} for transfer {transferId}");
                        return;
                    }
                }

                var fileRequest = new FileRequest
                {
                    TransferId = transferId,
                    FileBytes = completeFile.ToArray(),
                    Metadata = transfer.Metadata
                };

                FileCompleted?.Invoke(this, new FileCompletedEventArgs
                {
                    FileRequest = fileRequest,
                    Success = true
                });

                _udpTransfers.TryRemove(transferId, out _);
                Console.WriteLine($"UDP file transfer completed: {transferId} ({completeFile.Count} bytes)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error completing UDP transfer: {ex.Message}");
            }
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // For development/testing with self-signed certificates, accept all certificates
            // In production, you should implement proper certificate validation
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine($"SSL Policy Errors: {sslPolicyErrors}");

            // Accept self-signed certificates for development
            if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors ||
                sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch ||
                (sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
            {
                Console.WriteLine("Accepting self-signed certificate for development");
                return true;
            }

            return false;
        }

        // NEW: Async disconnect method that properly handles cleanup and events
        public async Task DisconnectAsync()
        {
            if (!_isConnected) return; // Already disconnected

            _isConnected = false;

            try
            {
                // Cancel background tasks
                _cancellationTokenSource?.Cancel();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error canceling tasks: {ex.Message}");
            }

            try
            {
                _udpClient?.Close();
                _udpClient?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing UDP client: {ex.Message}");
            }
            finally
            {
                _udpClient = null;
            }

            try
            {
                _sslStream?.Close();
                _tcpClient?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing TCP connection: {ex.Message}");
            }

            try
            {
                _cancellationTokenSource?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing cancellation token: {ex.Message}");
            }

            // Fire the Disconnected event
            try
            {
                Disconnected?.Invoke(this, EventArgs.Empty);
                Console.WriteLine($"Disconnected from LazyServer (Connection ID: {ConnectionId})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error firing Disconnected event: {ex.Message}");
            }
        }

        // Keep the synchronous version for compatibility, but make it call the async version
        public void Disconnect()
        {
            // Run the async disconnect synchronously
            try
            {
                DisconnectAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in synchronous disconnect: {ex.Message}");
            }
        }

        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];

            while (_isConnected && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var received = await _sslStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (received == 0)
                    {
                        // Server closed connection
                        Console.WriteLine("Server closed the connection");
                        await DisconnectAsync();
                        break;
                    }

                    await ProcessReceivedMessage(buffer, received);
                }
                catch (OperationCanceledException)
                {
                    // Normal cancellation, exit gracefully
                    break;
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine($"Error receiving message: {ex.Message}");
                    // Connection lost, trigger disconnect
                    await DisconnectAsync();
                    break;
                }
            }
        }

        private async Task ProcessReceivedMessage(byte[] buffer, int length)
        {
            if (length < 5) return;

            var messageType = (MessageType)buffer[0];
            var payloadLength = BitConverter.ToInt32(buffer, 1);

            var payload = new byte[payloadLength];
            Array.Copy(buffer, 5, payload, 0, Math.Min(payloadLength, length - 5));

            switch (messageType)
            {
                case MessageType.Message:
                    var message = Encoding.UTF8.GetString(payload);
                    MessageReceived?.Invoke(this, new MessageEventArgs { Message = message });
                    break;

                case MessageType.FileOffer:
                    await HandleFileOffer(payload);
                    break;

                case MessageType.FileCompletedNotification: // Handle server completion notification
                    await HandleFileCompletedNotification(payload);
                    break;
            }
        }
        private async Task HandleFileCompletedNotification(byte[] payload)
        {
            try
            {
                var notificationJson = Encoding.UTF8.GetString(payload);
                var notificationData = JObject.Parse(notificationJson);
                var transferId = notificationData["TransferId"]?.ToString();
                var success = notificationData["Success"]?.ToObject<bool>() ?? false;
                var metadata = notificationData["Metadata"]?.ToString() ?? "";

                // Find the transfer data if we have it
                FileRequest fileRequest = null;
                if (_udpTransfers.TryGetValue(transferId, out var transfer))
                {
                    // Reconstruct file if we have the chunks (for client-side reference)
                    if (success && transfer.ReceivedChunks.Count == transfer.TotalChunks)
                    {
                        var completeFile = new List<byte>();
                        for (int i = 0; i < transfer.TotalChunks; i++)
                        {
                            if (transfer.ReceivedChunks.TryGetValue(i, out var chunk))
                            {
                                completeFile.AddRange(chunk);
                            }
                        }
                        fileRequest = new FileRequest
                        {
                            TransferId = transferId,
                            FileBytes = completeFile.ToArray(),
                            Metadata = metadata
                        };
                    }
                    _udpTransfers.TryRemove(transferId, out _);
                }

                // Create a basic file request if we don't have the full data
                fileRequest ??= new FileRequest
                {
                    TransferId = transferId,
                    Metadata = metadata
                };

                // Fire the FileCompleted event on client (triggered by server notification)
                FileCompleted?.Invoke(this, new FileCompletedEventArgs
                {
                    ClientId = ConnectionId,
                    FileRequest = fileRequest,
                    Success = success,
                    Metadata = metadata
                });

                Console.WriteLine($"Received completion notification from server: {transferId} (Success: {success})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling file completed notification: {ex.Message}");
            }
        }
        private async Task HandleFileOffer(byte[] payload)
        {
            var offerJson = Encoding.UTF8.GetString(payload);
            var offerData = JObject.Parse(offerJson);
            var transferId = offerData["TransferId"]?.ToString();
            var metadata = offerData["Metadata"]?.ToString() ?? "";
            var useUdp = offerData["UseUdp"]?.ToObject<bool>() ?? false;

            if (useUdp)
            {
                // Initialize UDP transfer
                var serverEndPoint = new IPEndPoint(IPAddress.Parse(_serverHostname == "localhost" ? "127.0.0.1" : _serverHostname), _serverUdpPort);

                var initData = new byte[37 + ConnectionId.Length];
                initData[0] = (byte)MessageType.UdpInit;
                Encoding.UTF8.GetBytes(transferId).CopyTo(initData, 1);
                Encoding.UTF8.GetBytes(ConnectionId).CopyTo(initData, 37);

                await _udpClient.SendAsync(initData, initData.Length, serverEndPoint);

                var transfer = new UdpFileTransfer
                {
                    TransferId = transferId,
                    Metadata = metadata,
                    TotalBytes = offerData["FileSize"]?.ToObject<long>() ?? 0
                };
                _udpTransfers.TryAdd(transferId, transfer);

                Console.WriteLine($"Initialized UDP transfer: {transferId}");
            }
            else if (offerData["FileBytes"] != null)
            {
                // Handle inline file bytes
                var fileBytes = Convert.FromBase64String(offerData["FileBytes"].ToString());
                var fileRequest = new FileRequest
                {
                    TransferId = transferId,
                    FileBytes = fileBytes,
                    Metadata = metadata
                };

                FileOfferWithMetaReceived?.Invoke(this, new FileOfferWithMetaEventArgs
                {
                    FileRequest = fileRequest
                });
            }
            else
            {
                // Handle regular file offer
                var fileRequest = new FileRequest
                {
                    TransferId = transferId,
                    Metadata = metadata
                };

                FileOfferReceived?.Invoke(this, new FileOfferEventArgs
                {
                    TransferId = transferId,
                    Metadata = metadata,
                    FileRequest = fileRequest
                });
            }
        }

        public async Task SendMessage(string message)
        {
            if (!_isConnected) throw new InvalidOperationException("Not connected to server");

            var payload = Encoding.UTF8.GetBytes(message);
            await SendMessageInternal(MessageType.Message, payload);
        }

        public async Task SendFileBytes(byte[] fileBytes, string metadata = "")
        {
            if (!_isConnected) throw new InvalidOperationException("Not connected to server");

            var transferId = Guid.NewGuid().ToString();

            var offerData = new
            {
                TransferId = transferId,
                Metadata = metadata,
                FileBytes = Convert.ToBase64String(fileBytes)
            };

            var offerJson = JsonConvert.SerializeObject(offerData);
            await SendMessageInternal(MessageType.FileOffer, Encoding.UTF8.GetBytes(offerJson));

            var fileRequest = new FileRequest
            {
                TransferId = transferId,
                FileBytes = fileBytes,
                Metadata = metadata
            };

            FileCompleted?.Invoke(this, new FileCompletedEventArgs
            {
                FileRequest = fileRequest,
                Success = true
            });
        }

        public async Task SendFileBytesWithMeta(byte[] fileBytes, string metadata)
        {
            await SendFileBytes(fileBytes, metadata);
        }

        public async Task SendFile(string filePath, string metadata = "")
        {
            var fileInfo = new FileInfo(filePath);
            await SendFileWithMeta(filePath, metadata);
        }

        public async Task SendFileWithMeta(string filePath, string metadata)
        {
            if (!_isConnected) throw new InvalidOperationException("Not connected to server");
            if (!File.Exists(filePath)) throw new FileNotFoundException($"File not found: {filePath}");

            var fileBytes = await FileHelper.ReadAllBytesAsync(filePath);
            await SendFileViaUdp(fileBytes, metadata);
        }

        private bool IsUdpClientReady()
        {
            try
            {
                return _udpClient != null && _udpClient.Client != null && _udpClient.Client.IsBound;
            }
            catch
            {
                return false;
            }
        }

        public async Task SendFileViaUdp(byte[] fileBytes, string metadata = "")
        {
            if (!_isConnected) throw new InvalidOperationException("Not connected to server");

            if (!IsUdpClientReady())
            {
                Console.WriteLine("UDP client not ready, reinitializing...");
                try
                {
                    _udpClient?.Close();
                    _udpClient = new UdpClient(0);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to initialize UDP client: {ex.Message}");
                }
            }

            var transferId = Guid.NewGuid().ToString();

            // 1) Send offer via TCP
            var offerData = new
            {
                TransferId = transferId,
                Metadata = metadata,
                FileSize = fileBytes.Length,
                UseUdp = true
            };

            var offerJson = JsonConvert.SerializeObject(offerData);
            await SendMessageInternal(MessageType.FileOffer, Encoding.UTF8.GetBytes(offerJson));

            // 2) Initialize UDP connection
            var serverEndPoint = new IPEndPoint(IPAddress.Parse(_serverHostname == "localhost" ? "127.0.0.1" : _serverHostname), _serverUdpPort);

            var initData = new byte[37 + ConnectionId.Length];
            initData[0] = (byte)MessageType.UdpInit;
            Encoding.UTF8.GetBytes(transferId).CopyTo(initData, 1);
            Encoding.UTF8.GetBytes(ConnectionId).CopyTo(initData, 37);

            await _udpClient.SendAsync(initData, initData.Length, serverEndPoint);

            // 3) Wait for server UDP initialization
            await Task.Delay(200);

            // 4) Send file data via UDP
            await SendFileDataViaUdp(transferId, fileBytes, serverEndPoint, metadata);
        }

        private async Task SendFileDataViaUdp(string transferId, byte[] fileBytes, IPEndPoint serverEndPoint, string metadata)
        {
            const int CHUNK_SIZE = 1400;
            var totalChunks = (int)Math.Ceiling((double)fileBytes.Length / CHUNK_SIZE);

            for (int i = 0; i < totalChunks; i++)
            {
                var chunkStart = i * CHUNK_SIZE;
                var chunkSize = Math.Min(CHUNK_SIZE, fileBytes.Length - chunkStart);
                var chunkData = new byte[chunkSize];
                Array.Copy(fileBytes, chunkStart, chunkData, 0, chunkSize);

                // Create UDP packet: [1 byte type][36 bytes transferId][4 bytes chunkIndex][4 bytes totalChunks][1 byte isLast][data]
                var packet = new byte[46 + chunkSize];
                packet[0] = (byte)MessageType.UdpFileChunk;
                Encoding.UTF8.GetBytes(transferId).CopyTo(packet, 1);
                BitConverter.GetBytes(i).CopyTo(packet, 37);
                BitConverter.GetBytes(totalChunks).CopyTo(packet, 41);
                packet[45] = (byte)(i == totalChunks - 1 ? 1 : 0);
                chunkData.CopyTo(packet, 46);

                await _udpClient.SendAsync(packet, packet.Length, serverEndPoint);

                // Fire progress event with correct bytes transferred calculation
                var fileRequest = new FileRequest
                {
                    TransferId = transferId,
                    FileBytes = chunkData,
                    Metadata = metadata
                };

                FileProgress?.Invoke(this, new FileProgressEventArgs
                {
                    TransferId = transferId,
                    BytesTransferred = Math.Min((long)(i + 1) * CHUNK_SIZE, fileBytes.Length),
                    TotalBytes = fileBytes.Length,
                    FileRequest = fileRequest
                });

                // Small delay to prevent overwhelming
                if (i % 10 == 0)
                    await Task.Delay(1);
            }

            // 5) Send completion signal
            var completeData = new byte[37];
            completeData[0] = (byte)MessageType.UdpFileComplete;
            Encoding.UTF8.GetBytes(transferId).CopyTo(completeData, 1);

            await _udpClient.SendAsync(completeData, completeData.Length, serverEndPoint);

            // Wait a moment for the completion signal to be processed
            await Task.Delay(50);

            // Fire the FileOfferWithMetaReceived event (client-side event for sent files)
            var offerRequest = new FileRequest
            {
                TransferId = transferId,
                FileBytes = fileBytes,
                Metadata = metadata
            };
            try
            {
                FileOfferWithMetaReceived?.Invoke(this, new FileOfferWithMetaEventArgs
                {
                    FileRequest = offerRequest
                });
                Console.WriteLine($"FileOfferWithMetaReceived event fired for transfer: {transferId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error firing FileOfferWithMetaReceived event: {ex.Message}");
            }

            // REMOVED: FileCompleted event firing - now only fires on server
            // The server will send us a notification when it completes processing

            Console.WriteLine($"UDP file transfer sent: {transferId} ({totalChunks} chunks, {fileBytes.Length} bytes)");
        }

        public async Task StreamFile(string filePath, string metadata = "")
        {
            await StreamFileWithMeta(filePath, metadata);
        }

        public async Task StreamFileWithMeta(string filePath, string metadata)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException($"File not found: {filePath}");

            var fileBytes = await FileHelper.ReadAllBytesAsync(filePath);
            await SendFileViaUdp(fileBytes, metadata);
        }

        public async Task AcceptFile(string transferId, string savePath)
        {
            var response = new { TransferId = transferId, Accepted = true, SavePath = savePath };
            var responseJson = JsonConvert.SerializeObject(response);
            await SendMessageInternal(MessageType.FileAccept, Encoding.UTF8.GetBytes(responseJson));
        }

        public async Task RejectFile(string transferId)
        {
            var response = new { TransferId = transferId, Accepted = false };
            var responseJson = JsonConvert.SerializeObject(response);
            await SendMessageInternal(MessageType.FileReject, Encoding.UTF8.GetBytes(responseJson));
        }

        private async Task SendMessageInternal(MessageType type, byte[] payload)
        {
            if (!_isConnected)
                throw new InvalidOperationException("Not connected to server");

            try
            {
                var message = new byte[5 + payload.Length];
                message[0] = (byte)type;
                BitConverter.GetBytes(payload.Length).CopyTo(message, 1);
                payload.CopyTo(message, 5);

                await _sslStream.WriteAsync(message, 0, message.Length);
                await _sslStream.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
                // Connection lost, trigger disconnect
                await DisconnectAsync();
                throw;
            }
        }

        // Add a method to check if client is still connected and clean up if not
        public async Task<bool> CheckConnectionAsync()
        {
            if (!_isConnected) return false;

            try
            {
                // Try to send a heartbeat to check connection
                await SendHeartbeat();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task SendUdpFileBytes(byte[] fileBytes, string metadata = "")
        {
            if (!_isConnected) throw new InvalidOperationException("Not connected to server");

            var transferId = Guid.NewGuid().ToString();

            // 1) Send offer via TCP
            var offerData = new
            {
                TransferId = transferId,
                Metadata = metadata,
                FileSize = fileBytes.Length,
                UseUdp = true
            };

            var offerJson = JsonConvert.SerializeObject(offerData);
            await SendMessageInternal(MessageType.FileOffer, Encoding.UTF8.GetBytes(offerJson));

            // 2) Initialize UDP connection
            var serverEndPoint = new IPEndPoint(
                IPAddress.Parse(_serverHostname == "localhost" ? "127.0.0.1" : _serverHostname),
                _serverUdpPort);

            var initData = new byte[37 + ConnectionId.Length];
            initData[0] = (byte)MessageType.UdpInit;
            Encoding.UTF8.GetBytes(transferId).CopyTo(initData, 1);
            Encoding.UTF8.GetBytes(ConnectionId).CopyTo(initData, 37);

            await _udpClient.SendAsync(initData, initData.Length, serverEndPoint);

            // 3) Wait for server UDP initialization
            await Task.Delay(200);

            // 4) Send file data via UDP
            await SendFileDataViaUdp(transferId, fileBytes, serverEndPoint, metadata);

            // 5) DO NOT fire the UDPFileOfferWithMetaReceived event here - the server will fire it when it receives the file
            Console.WriteLine($"UDP file bytes sent to server: {transferId} ({fileBytes.Length} bytes)");
        }
    }

    public class ClientConnection
    {
        public string Id { get; }
        public TcpClient TcpClient { get; }
        public SslStream SslStream { get; }
        public bool IsConnected => TcpClient?.Connected == true;
        public ConcurrentDictionary<string, FileTransfer> ActiveTransfers { get; } = new ConcurrentDictionary<string, FileTransfer>();
        public int UdpPort { get; set; }

        public DateTime ConnectedAt { get; }
        public string RemoteEndPoint { get; }

        public ClientConnection(string id, TcpClient tcpClient, SslStream sslStream)
        {
            Id = id;
            TcpClient = tcpClient;
            SslStream = sslStream;
            ConnectedAt = DateTime.UtcNow;
            RemoteEndPoint = tcpClient.Client.RemoteEndPoint?.ToString() ?? "Unknown";
        }

        public void Disconnect()
        {
            foreach (var transfer in ActiveTransfers.Values)
            {
                transfer.FileStream?.Dispose();
            }
            ActiveTransfers.Clear();

            SslStream?.Close();
            TcpClient?.Close();
        }

        public override string ToString()
        {
            return $"ClientConnection[Id={Id}, Remote={RemoteEndPoint}, Connected={IsConnected}, Since={ConnectedAt:yyyy-MM-dd HH:mm:ss}, UDP={UdpPort}]";
        }
    }

    public class FileTransfer
    {
        public string Id { get; set; }
        public string Metadata { get; set; }
        public long BytesTransferred { get; set; }
        public long TotalSize { get; set; }
        public FileStream FileStream { get; set; }
    }

    public static class FileHelper
    {
        public static async Task<byte[]> ReadAllBytesAsync(string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            {
                var buffer = new byte[fileStream.Length];
                await fileStream.ReadAsync(buffer, 0, buffer.Length);
                return buffer;
            }
        }
    }

    public static class LazyServerExample
    {
        public static async Task RunServerExample()
        {
            var server = new LazyServerHost();

            server.MessageReceived += (s, e) => Console.WriteLine($"Message from {e.ClientId}: {e.Message}");
            server.FileOfferReceived += (s, e) => Console.WriteLine($"File offer from {e.ClientId}: {e.Metadata}");
            server.FileOfferWithMetaReceived += (s, e) => Console.WriteLine($"File with bytes from {e.ClientId}: {e.FileRequest.Metadata} ({e.FileRequest.FileBytes.Length} bytes)");
            server.FileProgress += (s, e) => Console.WriteLine($"File progress {e.ClientId}: {e.BytesTransferred}/{e.TotalBytes} bytes ({(e.BytesTransferred * 100.0 / Math.Max(e.TotalBytes, 1)):F1}%)");
            server.FileCompleted += (s, e) => Console.WriteLine($"File completed from {e.ClientId}: {e.FileRequest.Metadata} (Success: {e.Success})");

            server.ClientConnected += (s, clientId) =>
            {
                Console.WriteLine($"Client connected: {clientId}");
                var connection = server.GetConnectionById(clientId);
                if (connection != null)
                {
                    Console.WriteLine($"Connection details: {connection}");
                }
            };

            server.ClientDisconnected += (s, clientId) => Console.WriteLine($"Client disconnected: {clientId}");

            await server.StartAsync(8888, 8889); // TCP port 8888, UDP port 8889

            Console.WriteLine("Server running. Commands:");
            Console.WriteLine("  list - show connections");
            Console.WriteLine("  send <id> <message> - send message to client");
            Console.WriteLine("  file <id> <path> - send file to client via UDP");
            Console.WriteLine("  quit - stop server");

            string input;
            while ((input = Console.ReadLine()) != "quit")
            {
                if (input == "list")
                {
                    Console.WriteLine($"Active connections ({server.GetConnectionCount()}):");
                    foreach (var connectionId in server.GetAllConnectionIds())
                    {
                        var connection = server.GetConnectionById(connectionId);
                        Console.WriteLine($"  {connection}");
                    }
                }
                else if (input.StartsWith("send "))
                {
                    var parts = input.Split(new char[] { ' ' }, 3);
                    if (parts.Length >= 3)
                    {
                        var targetId = parts[1];
                        var message = parts[2];

                        if (server.TryGetConnectionById(targetId, out var connection))
                        {
                            await server.SendMessageToConnection(connection, message);
                            Console.WriteLine($"Message sent to {targetId}");
                        }
                        else
                        {
                            Console.WriteLine($"Connection {targetId} not found");
                        }
                    }
                }
                else if (input.StartsWith("file "))
                {
                    var parts = input.Split(new char[] { ' ' }, 3);
                    if (parts.Length >= 3)
                    {
                        var targetId = parts[1];
                        var filePath = parts[2];

                        if (server.TryGetConnectionById(targetId, out var connection))
                        {
                            try
                            {
                                await server.SendFileToClient(targetId, filePath, $"{{\"filename\":\"{Path.GetFileName(filePath)}\",\"sentAt\":\"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}\"}}");
                                Console.WriteLine($"File {filePath} sent to {targetId} via UDP");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error sending file: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Connection {targetId} not found");
                        }
                    }
                }
            }

            server.Stop();
        }

        public static async Task RunClientExample()
        {
            var client = new LazyServerClient();

            client.MessageReceived += (s, e) => Console.WriteLine($"Server message: {e.Message}");
            client.Connected += (s, e) => Console.WriteLine($"Connected to server with Connection ID: {client.ConnectionId}");
            client.FileProgress += (s, e) => Console.WriteLine($"File progress: {e.BytesTransferred}/{e.TotalBytes} bytes ({(e.BytesTransferred * 100.0 / Math.Max(e.TotalBytes, 1)):F1}%)");
            client.FileCompleted += (s, e) => Console.WriteLine($"File transfer completed: {e.FileRequest.Metadata} (Success: {e.Success}, Size: {e.FileRequest.FileBytes?.Length ?? 0} bytes)");

            await client.ConnectAsync("localhost", 8888, 8889);

            await client.SendMessage($"Hello from UDP-enabled client with ID: {client.ConnectionId}!");

            if (File.Exists("test.txt"))
            {
                Console.WriteLine("Sending test.txt via UDP...");
                await client.SendFile("test.txt", "{\"type\":\"text\",\"priority\":\"high\",\"transport\":\"udp\"}");
            }

            // Test sending file bytes via UDP
            var fileBytes = Encoding.UTF8.GetBytes("This is file content sent via UDP!");
            await client.SendFileViaUdp(fileBytes, "{\"author\":\"LazyServer\",\"version\":\"2.0\",\"transport\":\"udp\",\"tags\":[\"test\",\"demo\",\"udp\"]}");

            Console.WriteLine($"UDP-enabled client running with Connection ID: {client.ConnectionId}. Press any key to disconnect...");
            Console.ReadKey();

            client.Disconnect();
        }
    }
}