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
        // P/Invoke constants & structs for CreateFile
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
            // Use scoring system to reduce false positives
            var scores = new Dictionary<VmType, int>();

            // Windows Sandbox detection
            var sandboxScore = GetWindowsSandboxScore();
            if (sandboxScore >= 2) // Require multiple indicators
                return VmType.WindowsSandbox;

            // AnyRun detection
            var anyRunScore = GetAnyRunScore();
            if (anyRunScore >= 3) // Require multiple strong indicators
                return VmType.AnyRun;

            // VMware detection
            var vmwareScore = GetVMwareScore();
            if (vmwareScore >= 2)
                return VmType.VMware;

            // VirtualBox detection  
            var vboxScore = GetVirtualBoxScore();
            if (vboxScore >= 2)
                return VmType.VirtualBox;

            // QEMU detection
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
                // 1) Certificate check - but be more specific
                using (var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadOnly);
                    var anyCerts = store.Certificates.Cast<X509Certificate2>().Where(cert =>
                        (cert.Subject.IndexOf("any.run", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         cert.Issuer.IndexOf("any.run", StringComparison.OrdinalIgnoreCase) >= 0) &&
                        cert.Subject.IndexOf("legitimate", StringComparison.OrdinalIgnoreCase) < 0 // Avoid catching legitimate certs
                    );

                    if (anyCerts.Any())
                        score += 2; // Strong indicator
                }

                // 2) Process + DLL check - more targeted
                try
                {
                    var procs = Process.GetProcessesByName("srvpost");
                    foreach (var p in procs)
                    {
                        try
                        {
                            foreach (ProcessModule m in p.Modules)
                            {
                                var name = m.ModuleName.ToLowerInvariant();
                                if (name == "winanr.dll" || name == "winsanr.dll") // Exact match
                                {
                                    score += 2;
                                    break;
                                }
                            }
                        }
                        catch { /* Process may have exited */ }
                    }
                }
                catch { /* Process access denied */ }

                // 3) Device symbolic check
                var h = CreateFile(@"\\.\A3E64E55_fl",
                                   GENERIC_READ, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                if (h != INVALID_HANDLE_VALUE)
                {
                    CloseHandle(h);
                    score += 2; // Strong indicator
                }

                // 4) Registry service check - more specific
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(
                        @"SYSTEM\CurrentControlSet\Services\KernelLogger"))
                    {
                        if (key != null)
                        {
                            var displayName = key.GetValue("DisplayName")?.ToString();
                            var imagePath = key.GetValue("ImagePath")?.ToString();

                            // Check if it's actually the AnyRun service, not just any KernelLogger
                            if (displayName != null && displayName.IndexOf("any.run", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                imagePath != null && imagePath.IndexOf("any.run", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                score += 1;
                            }
                        }
                    }
                }
                catch { /* Registry access denied */ }

                // 5) Remove the directory existence check - too many false positives
                // Many legitimate programs create directories with similar names

                // 6) WMI service check - be more specific
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
                catch { /* WMI access issues */ }
            }
            catch
            {
                // Swallow exceptions but don't assume it's a VM
            }

            return score;
        }

        private static int GetWindowsSandboxScore()
        {
            int score = 0;

            try
            {
                // 1) Check for WDAG user - exact match
                var user = Environment.UserName;
                if (string.Equals(user, "WDAGUtilityAccount", StringComparison.OrdinalIgnoreCase))
                    score += 2;

                // 2) Check machine name - exact prefix
                var machine = Environment.MachineName;
                if (machine.StartsWith("WDAG", StringComparison.OrdinalIgnoreCase))
                    score += 2;

                // 3) Check for sandbox-specific services
                try
                {
                    using (var searcher = new ManagementObjectSearcher(
                        "SELECT * FROM Win32_Service WHERE Name='CmService' AND DisplayName LIKE '%Container Manager%'"))
                    {
                        if (searcher.Get().Count > 0)
                            score += 1;
                    }
                }
                catch { /* WMI issues */ }

                // 4) Check for Windows Sandbox-specific registry entries
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                    {
                        var productName = key?.GetValue("ProductName")?.ToString();
                        if (productName != null && productName.IndexOf("Windows Sandbox", StringComparison.OrdinalIgnoreCase) >= 0)
                            score += 2;
                    }
                }
                catch { /* Registry access denied */ }
            }
            catch
            {
                // Ignore exceptions
            }

            return score;
        }

        private static int GetVMwareScore()
        {
            int score = 0;

            try
            {
                // Check MAC addresses
                if (HasVMwareMac())
                    score += 1;

                // Check for VMware-specific processes
                var vmwareProcesses = new[] { "vmtoolsd", "vmwaretray", "vmwareuser" };
                foreach (var procName in vmwareProcesses)
                {
                    if (Process.GetProcessesByName(procName).Length > 0)
                    {
                        score += 1;
                        break; // Don't stack points for multiple processes
                    }
                }

                // Check registry for VMware
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\VMware, Inc.\VMware Tools"))
                    {
                        if (key != null)
                            score += 2;
                    }
                }
                catch { }

                // Check WMI for VMware strings
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
                // Check MAC addresses
                if (HasVirtualBoxMac())
                    score += 1;

                // Check for VirtualBox processes
                var vboxProcesses = new[] { "VBoxService", "VBoxTray" };
                foreach (var procName in vboxProcesses)
                {
                    if (Process.GetProcessesByName(procName).Length > 0)
                    {
                        score += 1;
                        break;
                    }
                }

                // Check WMI for VirtualBox
                var biosVersion = GetWmiProperty("Win32_BIOS", "Version");
                if (!string.IsNullOrEmpty(biosVersion) && biosVersion.IndexOf("VirtualBox", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 1;

                var manufacturer = GetWmiProperty("Win32_ComputerSystem", "Manufacturer");
                if (!string.IsNullOrEmpty(manufacturer) && (manufacturer.IndexOf("innotek", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    manufacturer.IndexOf("Oracle", StringComparison.OrdinalIgnoreCase) >= 0))
                    score += 1;

                // Check for VirtualBox guest additions
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Oracle\VirtualBox Guest Additions"))
                    {
                        if (key != null)
                            score += 2;
                    }
                }
                catch { }
            }
            catch { }

            return score;
        }

        private static int GetQEMUScore()
        {
            int score = 0;

            try
            {
                // Check MAC addresses
                if (HasQEMUMac())
                    score += 1;

                // Check WMI for QEMU
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

        private static bool HasVMwareMac()
        {
            return HasMacFromOui(MacOuis[VmType.VMware]);
        }

        private static bool HasVirtualBoxMac()
        {
            return HasMacFromOui(MacOuis[VmType.VirtualBox]);
        }

        private static bool HasQEMUMac()
        {
            return HasMacFromOui(MacOuis[VmType.QEMU]);
        }

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

        private static string GetCommandLine(int processId)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return obj["CommandLine"]?.ToString() ?? "";
                    }
                }
            }
            catch { }
            return "";
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
            catch
            {
                // Ignore
            }
            return null;
        }
    }
}