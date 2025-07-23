using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public static class ScreenCapture
{
    // Windows API constants
    private const int CURSOR_SHOWING = 0x00000001;
    private const int DI_NORMAL = 0x0003;

    // Windows API structures
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
        public static implicit operator Point(POINT point) => new Point(point.X, point.Y);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CURSORINFO
    {
        public int cbSize;
        public int flags;
        public IntPtr hCursor;
        public POINT ptScreenPos;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ICONINFO
    {
        public bool fIcon;
        public int xHotspot;
        public int yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    // Windows API imports
    [DllImport("user32.dll")]
    private static extern bool GetCursorInfo(out CURSORINFO pci);

    [DllImport("user32.dll")]
    private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);

    [DllImport("user32.dll")]
    private static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    /// <summary>
    /// Captures a screenshot of the specified monitor with the dynamic cursor
    /// </summary>
    /// <param name="monitorIndex">Index of the monitor to capture (0-based). Use -1 for primary monitor.</param>
    /// <param name="format">Image format for the returned byte array (default: PNG)</param>
    /// <returns>Byte array containing the screenshot with cursor, or null if capture fails</returns>
    public static byte[] CaptureScreenWithCursor(int monitorIndex = -1, ImageFormat format = null)
    {
        if (format == null)
            format = ImageFormat.Png;

        try
        {
            // Get the target screen
            Screen targetScreen;

            if (monitorIndex == -1)
            {
                targetScreen = Screen.PrimaryScreen;
            }
            else
            {
                if (monitorIndex < 0 || monitorIndex >= Screen.AllScreens.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(monitorIndex),
                        $"Monitor index must be between 0 and {Screen.AllScreens.Length - 1}");
                }
                targetScreen = Screen.AllScreens[monitorIndex];
            }

            Rectangle bounds = targetScreen.Bounds;

            // Create bitmap for the screen capture
            using (Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(screenshot))
                {
                    // Capture the screen
                    g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);

                    // Get cursor information
                    CURSORINFO cursorInfo = new CURSORINFO();
                    cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);

                    if (GetCursorInfo(out cursorInfo))
                    {
                        // Check if cursor is visible
                        if (cursorInfo.flags == CURSOR_SHOWING)
                        {
                            // Get cursor position relative to the target screen
                            Point cursorPos = cursorInfo.ptScreenPos;
                            int relativeX = cursorPos.X - bounds.X;
                            int relativeY = cursorPos.Y - bounds.Y;

                            // Only draw cursor if it's within the target screen bounds
                            if (relativeX >= 0 && relativeX < bounds.Width &&
                                relativeY >= 0 && relativeY < bounds.Height)
                            {
                                // Get cursor hotspot information
                                ICONINFO iconInfo;
                                if (GetIconInfo(cursorInfo.hCursor, out iconInfo))
                                {
                                    // Adjust cursor position by hotspot
                                    int drawX = relativeX - iconInfo.xHotspot;
                                    int drawY = relativeY - iconInfo.yHotspot;

                                    // Draw the cursor
                                    IntPtr hdc = g.GetHdc();
                                    DrawIcon(hdc, drawX, drawY, cursorInfo.hCursor);
                                    g.ReleaseHdc(hdc);

                                    // Clean up icon info handles
                                    if (iconInfo.hbmColor != IntPtr.Zero)
                                        DeleteObject(iconInfo.hbmColor);
                                    if (iconInfo.hbmMask != IntPtr.Zero)
                                        DeleteObject(iconInfo.hbmMask);
                                }
                            }
                        }
                    }
                }

                // Convert bitmap to byte array
                using (MemoryStream ms = new MemoryStream())
                {
                    screenshot.Save(ms, format);
                    return ms.ToArray();
                }
            }
        }
        catch (Exception ex)
        {
            // Log the exception if you have logging
            System.Diagnostics.Debug.WriteLine($"Screenshot capture failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets information about all available monitors
    /// </summary>
    /// <returns>Array of screen information</returns>
    public static (int Index, string DeviceName, Rectangle Bounds, bool IsPrimary)[] GetMonitorInfo()
    {
        var monitors = new (int, string, Rectangle, bool)[Screen.AllScreens.Length];

        for (int i = 0; i < Screen.AllScreens.Length; i++)
        {
            var screen = Screen.AllScreens[i];
            monitors[i] = (i, screen.DeviceName, screen.Bounds, screen.Primary);
        }

        return monitors;
    }
}