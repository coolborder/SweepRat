using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class Remembrance
{
    private static readonly string AppDataPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SweepRat");

    private static readonly string FilePath = Path.Combine(AppDataPath, "settings.json");

    private Dictionary<string, object> _attributes;

    public Remembrance()
    {
        Load();
    }

    public void SetAttribute(string key, object value)
    {
        _attributes[key] = value;
        Save();
    }

    public T? GetAttribute<T>(string key)
    {
        if (_attributes.TryGetValue(key, out var value))
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
            }
            catch
            {
                return default;
            }
        }
        return default;
    }

    private void Load()
    {
        if (File.Exists(FilePath))
        {
            var json = File.ReadAllText(FilePath);
            _attributes = JsonConvert.DeserializeObject<Dictionary<string, object>>(json)
                          ?? new Dictionary<string, object>();
        }
        else
        {
            _attributes = new Dictionary<string, object>();
        }
    }

    private void Save()
    {
        if (!Directory.Exists(AppDataPath))
        {
            Directory.CreateDirectory(AppDataPath);
        }

        var json = JsonConvert.SerializeObject(_attributes, Formatting.Indented);
        File.WriteAllText(FilePath, json);
    }
}
