using LazyServer;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

public class GClass
{
    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    const int SW_HIDE = 0;
    const int SW_SHOW = 5;
    public static JObject config()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Client.config.json";

        string result;
        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        using (StreamReader reader = new StreamReader(stream))
        {
            result = reader.ReadToEnd();
        }
        return JObject.Parse(result);
    }

    public static bool IsTrue(string param)
    {
        return (bool)config()[param];
    }

    public static void ShowCmd(bool show) {
        var handle = GetConsoleWindow();
        ShowWindow(handle, show ? SW_SHOW : SW_HIDE);
    }

    public static async Task ReconnectLoop(LazyServerClient client)
    {
        while (true)
        {
            try
            {
                await client.ConnectAsync((string)config()["server"], (int)config()["port"]);
                break;
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
        var externalIpString = (await new HttpClient().GetStringAsync("http://icanhazip.com"))
            .Replace("\\r\\n", "").Replace("\\n", "").Trim();
        if (!IPAddress.TryParse(externalIpString, out var ipAddress)) return null;
        return externalIpString;
    }
}
