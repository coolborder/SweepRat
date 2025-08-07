using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace VirtualizationDetection
{
    public enum VmType
    {
        None,
        VMware,
        VirtualBox,
        QEMU,
        AnyRun,
        WindowsSandbox
    }

    public static class VMDetector
    {
        private const uint GENERIC_READ = 0x80000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint OPEN_EXISTING = 3;
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private static readonly Dictionary<VmType, string[]> MacOuis = new Dictionary<VmType, string[]>
        {
            { VmType.VMware,    new[] { "00:05:69", "00:0C:29", "00:50:56" } },
            { VmType.VirtualBox,new[] { "08:00:27" } },
            { VmType.QEMU,      new[] { "52:54:00" } },
        };

        public static VmType Detect()
        {
            var scores = new Dictionary<VmType, int>();

            var sandboxScore = GetWindowsSandboxScore();
            if (sandboxScore >= 2)
                return VmType.WindowsSandbox;

            var anyRunScore = GetAnyRunScore();
            if (anyRunScore >= 3)
                return VmType.AnyRun;

            var vmwareScore = GetVMwareScore();
            if (vmwareScore >= 2)
                return VmType.VMware;

            var vboxScore = GetVirtualBoxScore();
            if (vboxScore >= 2)
                return VmType.VirtualBox;

            var qemuScore = GetQEMUScore();
            if (qemuScore >= 2)
                return VmType.QEMU;

            return VmType.None;
        }

        private static int GetAnyRunScore()
        {
            int score = 0;

            try
            {
                using (var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadOnly);
                    var anyCerts = store.Certificates.Cast<X509Certificate2>().Where(cert =>
                        (cert.Subject.IndexOf("any.run", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         cert.Issuer.IndexOf("any.run", StringComparison.OrdinalIgnoreCase) >= 0) &&
                        cert.Subject.IndexOf("legitimate", StringComparison.OrdinalIgnoreCase) < 0);

                    if (anyCerts.Any())
                        score += 2;
                }

                var procs = Process.GetProcessesByName("srvpost");
                foreach (var p in procs)
                {
                    try
                    {
                        foreach (ProcessModule m in p.Modules)
                        {
                            var name = m.ModuleName.ToLowerInvariant();
                            if (name == "winanr.dll" || name == "winsanr.dll")
                            {
                                score += 2;
                                break;
                            }
                        }
                    }
                    catch { }
                }

                var h = CreateFile(@"\\\\.\\A3E64E55_fl",
                                   GENERIC_READ, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                if (h != INVALID_HANDLE_VALUE)
                {
                    CloseHandle(h);
                    score += 2;
                }

                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(
                        @"SYSTEM\\CurrentControlSet\\Services\\KernelLogger"))
                    {
                        if (key != null)
                        {
                            var displayName = key.GetValue("DisplayName")?.ToString();
                            var imagePath = key.GetValue("ImagePath")?.ToString();

                            if (displayName != null && displayName.IndexOf("any.run", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                imagePath != null && imagePath.IndexOf("any.run", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                score += 1;
                            }
                        }
                    }
                }
                catch { }

                try
                {
                    using (var searcher = new ManagementObjectSearcher(
                        "SELECT * FROM Win32_SystemDriver WHERE Name='KernelLogger'"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            var pathName = obj["PathName"]?.ToString();
                            if (pathName != null && pathName.IndexOf("any.run", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                score += 1;
                            }
                        }
                    }
                }
                catch { }
            }
            catch { }

            return score;
        }

        private static int GetWindowsSandboxScore()
        {
            int score = 0;

            try
            {
                var user = Environment.UserName;
                if (string.Equals(user, "WDAGUtilityAccount", StringComparison.OrdinalIgnoreCase))
                    score += 2;

                var machine = Environment.MachineName;
                if (machine.StartsWith("WDAG", StringComparison.OrdinalIgnoreCase))
                    score += 2;

                try
                {
                    using (var searcher = new ManagementObjectSearcher(
                        "SELECT * FROM Win32_Service WHERE Name='CmService' AND DisplayName LIKE '%Container Manager%'"))
                    {
                        if (searcher.Get().Count > 0)
                            score += 1;
                    }
                }
                catch { }

                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion"))
                    {
                        var productName = key?.GetValue("ProductName")?.ToString();
                        if (productName != null && productName.IndexOf("Windows Sandbox", StringComparison.OrdinalIgnoreCase) >= 0)
                            score += 2;
                    }
                }
                catch { }
            }
            catch { }

            return score;
        }

        private static int GetVMwareScore()
        {
            int score = 0;

            try
            {
                if (HasVMwareMac())
                    score += 1;

                var vmwareProcesses = new[] { "vmtoolsd", "vmwaretray", "vmwareuser" };
                foreach (var procName in vmwareProcesses)
                {
                    if (Process.GetProcessesByName(procName).Length > 0)
                    {
                        score += 1;
                        break;
                    }
                }

                var biosVersion = GetWmiProperty("Win32_BIOS", "Version");
                if (!string.IsNullOrEmpty(biosVersion) && biosVersion.IndexOf("VMware", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 1;

                var manufacturer = GetWmiProperty("Win32_ComputerSystem", "Manufacturer");
                if (!string.IsNullOrEmpty(manufacturer) && manufacturer.IndexOf("VMware", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 1;
            }
            catch { }

            return score;
        }

        private static int GetVirtualBoxScore()
        {
            int score = 0;

            try
            {
                if (HasVirtualBoxMac())
                    score += 1;

                var vboxProcesses = new[] { "VBoxService", "VBoxTray" };
                foreach (var procName in vboxProcesses)
                {
                    if (Process.GetProcessesByName(procName).Length > 0)
                    {
                        score += 1;
                        break;
                    }
                }

                var biosVersion = GetWmiProperty("Win32_BIOS", "Version");
                if (!string.IsNullOrEmpty(biosVersion) && biosVersion.IndexOf("VirtualBox", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 1;

                var manufacturer = GetWmiProperty("Win32_ComputerSystem", "Manufacturer");
                if (!string.IsNullOrEmpty(manufacturer) &&
                    (manufacturer.IndexOf("innotek", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     manufacturer.IndexOf("Oracle", StringComparison.OrdinalIgnoreCase) >= 0))
                    score += 1;
            }
            catch { }

            return score;
        }

        private static int GetQEMUScore()
        {
            int score = 0;

            try
            {
                if (HasQEMUMac())
                    score += 1;

                var manufacturer = GetWmiProperty("Win32_ComputerSystem", "Manufacturer");
                if (!string.IsNullOrEmpty(manufacturer) && manufacturer.IndexOf("QEMU", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 2;

                var model = GetWmiProperty("Win32_ComputerSystem", "Model");
                if (!string.IsNullOrEmpty(model) && model.IndexOf("Standard PC", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 1;
            }
            catch { }

            return score;
        }

        private static bool HasVMwareMac() => HasMacFromOui(MacOuis[VmType.VMware]);
        private static bool HasVirtualBoxMac() => HasMacFromOui(MacOuis[VmType.VirtualBox]);
        private static bool HasQEMUMac() => HasMacFromOui(MacOuis[VmType.QEMU]);

        private static bool HasMacFromOui(string[] ouis)
        {
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus == OperationalStatus.Up)
                    {
                        var mac = nic.GetPhysicalAddress().ToString();
                        if (mac.Length >= 6)
                        {
                            var macPrefix = $"{mac.Substring(0, 2)}:{mac.Substring(2, 2)}:{mac.Substring(4, 2)}";
                            if (ouis.Contains(macPrefix, StringComparer.OrdinalIgnoreCase))
                                return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        private static string GetWmiProperty(string wmiClass, string property)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}"))
                {
                    foreach (var mo in searcher.Get())
                    {
                        var val = mo[property];
                        if (val != null)
                            return val.ToString();
                    }
                }
            }
            catch { }
            return null;
        }
    }
}