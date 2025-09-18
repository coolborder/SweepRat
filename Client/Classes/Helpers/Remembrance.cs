using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Client.Classes.Helpers
{
    public class Remembrance
    {
        // Less obvious registry path
        private readonly string _baseKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\SystemSettingsCache";
        private readonly string _valueName = "CacheData";

        // AES key/IV (should be unique per build for real stealth; here is a static example)
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("A1B2C3D4E5F6G7H8"); // 16 bytes for AES-128
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("1H2G3F4E5D6C7B8A");  // 16 bytes

        private Dictionary<string, string> LoadData()
        {
            try
            {
                using (var regKey = Registry.CurrentUser.CreateSubKey(_baseKey))
                {
                    var enc = regKey?.GetValue(_valueName) as byte[];
                    if (enc == null || enc.Length == 0)
                        return new Dictionary<string, string>();

                    var json = Decrypt(enc);
                    return JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                }
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        private void SaveData(Dictionary<string, string> data)
        {
            try
            {
                var json = JsonConvert.SerializeObject(data);
                var enc = Encrypt(json);
                using (var regKey = Registry.CurrentUser.CreateSubKey(_baseKey))
                {
                    regKey?.SetValue(_valueName, enc, RegistryValueKind.Binary);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception when saving data: {ex}");
            }

        }

        public void SetAttribute(string key, string value)
        {
            var data = LoadData();
            data[key] = value;
            SaveData(data);
        }

        public string GetAttribute(string key)
        {
            var data = LoadData();
            return data.TryGetValue(key, out var value) ? value : null;
        }

        public void RemoveAttribute(string key)
        {
            var data = LoadData();
            if (data.Remove(key))
                SaveData(data);
        }

        private static byte[] Encrypt(string plainText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs, Encoding.UTF8))
                {
                    sw.Write(plainText);
                    sw.Flush();
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }

        private static string Decrypt(byte[] cipherText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                using (var ms = new MemoryStream(cipherText))
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }
}