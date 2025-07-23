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
        Handshake = 0x07
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
    }

    public class LazyServerHost
    {
        private TcpListener _listener;
        private X509Certificate2 _serverCertificate;
        private readonly ConcurrentDictionary<string, ClientConnection> _clients = new ConcurrentDictionary<string, ClientConnection>();
        private CancellationTokenSource _cancellationToken;
        private bool _isRunning;

        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler<FileOfferEventArgs> FileOfferReceived;
        public event EventHandler<FileOfferWithMetaEventArgs> FileOfferWithMetaReceived;
        public event EventHandler<FileProgressEventArgs> FileProgress;
        public event EventHandler<FileCompletedEventArgs> FileCompleted;
        public event EventHandler<string> ClientConnected;
        public event EventHandler<string> ClientDisconnected;

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

        public async Task StartAsync(int port = 8888)
        {
            GenerateTemporaryCertificate();
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _cancellationToken = new CancellationTokenSource();
            _isRunning = true;

            Console.WriteLine($"LazyServer started on port {port}");

            _ = Task.Run(AcceptClientsAsync);
            await Task.CompletedTask;
        }

        public void Stop()
        {
            _isRunning = false;
            _cancellationToken?.Cancel();
            _listener?.Stop();

            foreach (var client in _clients.Values)
            {
                client.Disconnect();
            }
            _clients.Clear();

            _serverCertificate?.Dispose();
            Console.WriteLine("LazyServer stopped");
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
        /// Send an in‐memory byte[] as a “file” (base64‐encoded offer) and complete right away.
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
        /// Stream a file off disk to the client chunk by chunk.
        /// </summary>
        public async Task SendFileToClient(string clientId, string filePath, string metadata = "")
        {
            if (!_clients.TryGetValue(clientId, out var client) || !client.IsConnected)
                throw new InvalidOperationException($"Client {clientId} not connected.");
            if (!File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            var transferId = Guid.NewGuid().ToString();
            var fi = new FileInfo(filePath);

            // 1) send offer (no FileBytes here; client will request or trust the server to stream)
            var offer = new
            {
                TransferId = transferId,
                Metadata = metadata,
                FileName = fi.Name,
                FileSize = fi.Length
            };
            var offerJson = JsonConvert.SerializeObject(offer);
            await SendMessage(client, MessageType.FileOffer, Encoding.UTF8.GetBytes(offerJson));

            // 2) stream the file data
            const int BUF = 8192;
            var buf = new byte[BUF];
            using (var fs = File.OpenRead(filePath))
            {
                long sent = 0;
                int read;
                while ((read = await fs.ReadAsync(buf, 0, BUF)) > 0)
                {
                    // packet = [ 36‐byte UTF8(transferId) | file bytes... ]
                    var packet = new byte[36 + read];
                    Encoding.UTF8.GetBytes(transferId).CopyTo(packet, 0);
                    Array.Copy(buf, 0, packet, 36, read);
                    await SendMessage(client, MessageType.FileData, packet);
                    sent += read;
                }
            }

            // 3) finally, tell the client we’re done
            await SendMessage(client, MessageType.FileComplete, Encoding.UTF8.GetBytes(transferId));
        }

    }

    public class LazyServerClient
    {
        private TcpClient _tcpClient;
        private SslStream _sslStream;
        private bool _isConnected;
        private readonly ConcurrentDictionary<string, FileTransfer> _activeTransfers = new ConcurrentDictionary<string, FileTransfer>();

        public string ConnectionId { get; private set; }

        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler<FileOfferEventArgs> FileOfferReceived;
        public event EventHandler<FileOfferWithMetaEventArgs> FileOfferWithMetaReceived;
        public event EventHandler<FileProgressEventArgs> FileProgress;
        public event EventHandler<FileCompletedEventArgs> FileCompleted;
        public event EventHandler Connected;
        public event EventHandler Disconnected;

        public async Task ConnectAsync(string hostname = "localhost", int port = 8888)
        {
            try
            {
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

                _isConnected = true;
                ConnectionId = Guid.NewGuid().ToString();

                Connected?.Invoke(this, EventArgs.Empty);
                Console.WriteLine($"Connected to LazyServer at {hostname}:{port} with Connection ID: {ConnectionId}");

                _ = Task.Run(ReceiveMessagesAsync);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
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

        public void Disconnect()
        {
            _isConnected = false;
            _sslStream?.Close();
            _tcpClient?.Close();
            Disconnected?.Invoke(this, EventArgs.Empty);
            Console.WriteLine($"Disconnected from LazyServer (Connection ID: {ConnectionId})");
        }

        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[4096];

            while (_isConnected)
            {
                try
                {
                    var received = await _sslStream.ReadAsync(buffer, 0, buffer.Length);
                    if (received == 0) break;

                    await ProcessReceivedMessage(buffer, received);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving message: {ex.Message}");
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
                    break;
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

            var transferId = Guid.NewGuid().ToString();

            var offerData = new
            {
                TransferId = transferId,
                Metadata = metadata
            };

            var offerJson = JsonConvert.SerializeObject(offerData);
            await SendMessageInternal(MessageType.FileOffer, Encoding.UTF8.GetBytes(offerJson));

            await StreamFileInternal(filePath, transferId, metadata);
        }

        public async Task StreamFile(string filePath, string metadata = "")
        {
            await StreamFileWithMeta(filePath, metadata);
        }

        public async Task StreamFileWithMeta(string filePath, string metadata)
        {
            var transferId = Guid.NewGuid().ToString();
            await StreamFileInternal(filePath, transferId, metadata);
        }

        private async Task StreamFileInternal(string filePath, string transferId, string metadata)
        {
            const int bufferSize = 8192;
            var buffer = new byte[bufferSize];
            long totalSent = 0;
            var fileInfo = new FileInfo(filePath);

            using (var fileStream = File.OpenRead(filePath))
            {
                while (totalSent < fileInfo.Length)
                {
                    var read = await fileStream.ReadAsync(buffer, 0, bufferSize);
                    if (read == 0) break;

                    var dataPacket = new byte[36 + read];
                    Encoding.UTF8.GetBytes(transferId).CopyTo(dataPacket, 0);
                    Array.Copy(buffer, 0, dataPacket, 36, read);

                    await SendMessageInternal(MessageType.FileData, dataPacket);

                    totalSent += read;

                    var fileRequest = new FileRequest
                    {
                        TransferId = transferId,
                        FileBytes = new ArraySegment<byte>(buffer, 0, read).ToArray(),
                        Metadata = metadata
                    };

                    FileProgress?.Invoke(this, new FileProgressEventArgs
                    {
                        TransferId = transferId,
                        BytesTransferred = totalSent,
                        TotalBytes = fileInfo.Length,
                        FileRequest = fileRequest
                    });
                }
            }

            var completedRequest = new FileRequest
            {
                TransferId = transferId,
                Metadata = metadata
            };

            FileCompleted?.Invoke(this, new FileCompletedEventArgs
            {
                FileRequest = completedRequest,
                Success = true
            });

            Console.WriteLine($"File transfer completed: {Path.GetFileName(filePath)}");
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
            var message = new byte[5 + payload.Length];
            message[0] = (byte)type;
            BitConverter.GetBytes(payload.Length).CopyTo(message, 1);
            payload.CopyTo(message, 5);

            await _sslStream.WriteAsync(message, 0, message.Length);
            await _sslStream.FlushAsync();
        }
    }

    public class ClientConnection
    {
        public string Id { get; }
        public TcpClient TcpClient { get; }
        public SslStream SslStream { get; }
        public bool IsConnected => TcpClient?.Connected == true;
        public ConcurrentDictionary<string, FileTransfer> ActiveTransfers { get; } = new ConcurrentDictionary<string, FileTransfer>();

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
            return $"ClientConnection[Id={Id}, Remote={RemoteEndPoint}, Connected={IsConnected}, Since={ConnectedAt:yyyy-MM-dd HH:mm:ss}]";
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

    public static class LazyServerExample
    {
        public static async Task RunServerExample()
        {
            var server = new LazyServerHost();

            server.MessageReceived += (s, e) => Console.WriteLine($"Message from {e.ClientId}: {e.Message}");
            server.FileOfferReceived += (s, e) => Console.WriteLine($"File offer from {e.ClientId}: {e.Metadata}");
            server.FileOfferWithMetaReceived += (s, e) => Console.WriteLine($"File with bytes from {e.ClientId}: {e.FileRequest.Metadata} ({e.FileRequest.FileBytes.Length} bytes)");

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

            await server.StartAsync(8888);

            Console.WriteLine("Server running. Type 'list' to see connections, 'send <id> <message>' to send to specific client, or 'quit' to stop...");

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
            }

            server.Stop();
        }

        public static async Task RunClientExample()
        {
            var client = new LazyServerClient();

            client.MessageReceived += (s, e) => Console.WriteLine($"Server message: {e.Message}");
            client.Connected += (s, e) => Console.WriteLine($"Connected to server with Connection ID: {client.ConnectionId}");
            client.FileCompleted += (s, e) => Console.WriteLine($"File transfer completed: {e.FileRequest.Metadata}");

            await client.ConnectAsync("localhost", 8888);

            await client.SendMessage($"Hello from client with ID: {client.ConnectionId}!");

            if (File.Exists("test.txt"))
            {
                await client.SendFile("test.txt", "{\"type\":\"text\",\"priority\":\"high\"}");
            }

            var fileBytes = Encoding.UTF8.GetBytes("This is file content from bytes!");
            await client.SendFileBytes(fileBytes, "{\"author\":\"LazyServer\",\"version\":\"1.0\",\"tags\":[\"test\",\"demo\"]}");

            Console.WriteLine($"Client running with Connection ID: {client.ConnectionId}. Press any key to disconnect...");
            Console.ReadKey();

            client.Disconnect();
        }
    }
}