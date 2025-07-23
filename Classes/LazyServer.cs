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
using System.Xml.Linq;

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
        public string Metadata { get; set; } // Custom JSON string or any data
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
        public string Metadata { get; set; } // Added missing property
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
                var req = new CertificateRequest("CN=LazyServer", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));
                req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));

                _serverCertificate = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1));
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
                var sslStream = new SslStream(tcpClient.GetStream());
                await sslStream.AuthenticateAsServerAsync(_serverCertificate, false, SslProtocols.Tls12, false);

                client = new ClientConnection(clientId, tcpClient, sslStream);
                _clients.TryAdd(clientId, client);

                ClientConnected?.Invoke(this, clientId);
                Console.WriteLine($"Client {clientId} connected with SSL");

                await HandleClientMessages(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client {clientId} error: {ex.Message}");
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
            if (length < 5) return; // Minimum: type(1) + length(4)

            var messageType = (MessageType)buffer[0];
            var payloadLength = BitConverter.ToInt32(buffer, 1);

            var payload = new byte[payloadLength];
            Array.Copy(buffer, 5, payload, 0, Math.Min(payloadLength, length - 5));

            // If we didn't receive the full payload, read the rest
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

                    // Check if this is a file offer with bytes included
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
            // File data handling for streaming transfers
            var transferId = Encoding.UTF8.GetString(payload, 0, 36); // GUID length
            var fileData = new byte[payload.Length - 36];
            Array.Copy(payload, 36, fileData, 0, fileData.Length);

            // Get or create file transfer tracking
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
    }

    public class LazyServerClient
    {
        private TcpClient _tcpClient;
        private SslStream _sslStream;
        private string _serverId = Guid.NewGuid().ToString();
        private bool _isConnected;
        private readonly ConcurrentDictionary<string, FileTransfer> _activeTransfers = new ConcurrentDictionary<string, FileTransfer>();

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

                _sslStream = new SslStream(_tcpClient.GetStream(), false, (sender, certificate, chain, sslPolicyErrors) => true);
                await _sslStream.AuthenticateAsClientAsync("LazyServer");

                _isConnected = true;
                Connected?.Invoke(this, EventArgs.Empty);
                Console.WriteLine($"Connected to LazyServer at {hostname}:{port}");

                _ = Task.Run(ReceiveMessagesAsync);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection failed: {ex.Message}");
                throw;
            }
        }

        public void Disconnect()
        {
            _isConnected = false;
            _sslStream?.Close();
            _tcpClient?.Close();
            Disconnected?.Invoke(this, EventArgs.Empty);
            Console.WriteLine("Disconnected from LazyServer");
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
                    // Handle file offer
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

            // Send file offer with bytes included
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

            // Send file offer
            var offerData = new
            {
                TransferId = transferId,
                Metadata = metadata
            };

            var offerJson = JsonConvert.SerializeObject(offerData);
            await SendMessageInternal(MessageType.FileOffer, Encoding.UTF8.GetBytes(offerJson));

            // For simplicity, assume file is accepted and start transfer
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

                    var dataPacket = new byte[36 + read]; // GUID + data
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

    internal class ClientConnection
    {
        public string Id { get; }
        public TcpClient TcpClient { get; }
        public SslStream SslStream { get; }
        public bool IsConnected => TcpClient?.Connected == true;
        public ConcurrentDictionary<string, FileTransfer> ActiveTransfers { get; } = new ConcurrentDictionary<string, FileTransfer>();

        public ClientConnection(string id, TcpClient tcpClient, SslStream sslStream)
        {
            Id = id;
            TcpClient = tcpClient;
            SslStream = sslStream;
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
    }

    internal class FileTransfer
    {
        public string Id { get; set; }
        public string Metadata { get; set; }
        public long BytesTransferred { get; set; }
        public long TotalSize { get; set; }
        public FileStream FileStream { get; set; }
    }

    // Example usage class
    public static class LazyServerExample
    {
        public static async Task RunServerExample()
        {
            var server = new LazyServerHost();

            server.MessageReceived += (s, e) => Console.WriteLine($"Message from {e.ClientId}: {e.Message}");
            server.FileOfferReceived += (s, e) => Console.WriteLine($"File offer from {e.ClientId}: {e.Metadata}");
            server.FileOfferWithMetaReceived += (s, e) => Console.WriteLine($"File with bytes from {e.ClientId}: {e.FileRequest.Metadata} ({e.FileRequest.FileBytes.Length} bytes)");
            server.ClientConnected += (s, clientId) => Console.WriteLine($"Client connected: {clientId}");
            server.ClientDisconnected += (s, clientId) => Console.WriteLine($"Client disconnected: {clientId}");

            await server.StartAsync(8888);

            // Keep server running
            Console.WriteLine("Server running. Press any key to stop...");
            Console.ReadKey();

            server.Stop();
        }

        public static async Task RunClientExample()
        {
            var client = new LazyServerClient();

            client.MessageReceived += (s, e) => Console.WriteLine($"Server message: {e.Message}");
            client.Connected += (s, e) => Console.WriteLine("Connected to server");
            client.FileCompleted += (s, e) => Console.WriteLine($"File transfer completed: {e.FileRequest.Metadata}");

            await client.ConnectAsync("localhost", 8888);

            await client.SendMessage("Hello from client!");

            // Send file from disk
            await client.SendFile("test.txt", "{\"type\":\"text\",\"priority\":\"high\"}");

            // Send file from bytes
            var fileBytes = Encoding.UTF8.GetBytes("This is file content from bytes!");
            await client.SendFileBytes(fileBytes, "{\"author\":\"LazyServer\",\"version\":\"1.0\",\"tags\":[\"test\",\"demo\"]}");

            // Keep client running
            Console.WriteLine("Client running. Press any key to disconnect...");
            Console.ReadKey();

            client.Disconnect();
        }
    }
}