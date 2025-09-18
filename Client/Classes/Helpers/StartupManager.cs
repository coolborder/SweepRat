using Microsoft.Win32;
using System;
using System.Linq;

public class StartupManager
{
    public static void AddToStartup(string filename)
    {
        string key = @"Software\Microsoft\Windows NT\CurrentVersion\Svchost";

        /*try
        {
            RegistryKey regKey =
                Registry.CurrentUser.OpenSubKey(key, true);

            if (regKey != null)
            {
                regKey.SetValue("UpdateTask", filename,
                    RegistryValueKind.String);
                regKey.Close();
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"Error adding to startup: {ex.Message}");
        }*/
    }

    public static bool IsAdded()
    {
        return false;
    }
}