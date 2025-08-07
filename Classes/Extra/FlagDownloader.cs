using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;
using MaxMind.GeoIP2.Responses;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

public class FlagDownloader
{
    private static readonly string geoLiteDbPath = @"./Extra/GeoLite2-Country.mmdb";

    public static async Task<string> GetCountryAsync(string ip)
    {
        string countryCode = "AQ"; // Default fallback

        try
        {
            using (var reader = new DatabaseReader(geoLiteDbPath))
            {
                CountryResponse response = reader.Country(ip);
                countryCode = response?.Country?.IsoCode ?? "AQ";
            }
        }
        catch (GeoIP2Exception)
        {
            Console.WriteLine("IP Not found in DB: " + geoLiteDbPath);
        }
        catch (IOException)
        {
            Console.WriteLine("Database file not found: " + geoLiteDbPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred while reading the database: " + ex.Message);
        }

        if (countryCode == "AQ")
        {
            return "Agartha"; // real mature of me
        }

        RegionInfo region = new RegionInfo(countryCode);
        return region.EnglishName;
    }

    public static async Task<Image> GetFlagByCountryAsync(string countryCode)
    {
        string localFlagPath = $"./Extra/Flags/{countryCode.ToLower()}.png";
        string fallbackFlagPath = "./Extra/Flags/aq.png";

        try
        {
            if (File.Exists(localFlagPath))
            {
                using (FileStream fs = new FileStream(localFlagPath, FileMode.Open, FileAccess.Read))
                {
                    return Image.FromStream(fs);
                }
            }
            else if (File.Exists(fallbackFlagPath))
            {
                using (FileStream fs = new FileStream(fallbackFlagPath, FileMode.Open, FileAccess.Read))
                {
                    return Image.FromStream(fs);
                }
            }
            else
            {
                return null;
            }
        }
        catch
        {
            return null;
        }
    }

    public static async Task<Image> GetFlagImageByIpAsync(string ip)
    {
        string countryCode = "AQ"; // Default fallback

        try
        {
            using (var reader = new DatabaseReader(geoLiteDbPath))
            {
                CountryResponse response = reader.Country(ip);
                countryCode = response?.Country?.IsoCode ?? "AQ";
            }
        }
        catch
        {
            // fallback remains AQ
        }

        string localFlagPath = $"./Extra/Flags/{countryCode.ToLower()}.png";
        string fallbackFlagPath = "./Extra/Flags/aq.png";

        try
        {
            if (File.Exists(localFlagPath))
            {
                using (FileStream fs = new FileStream(localFlagPath, FileMode.Open, FileAccess.Read))
                {
                    return Image.FromStream(fs);
                }
            }
            else if (File.Exists(fallbackFlagPath))
            {
                using (FileStream fs = new FileStream(fallbackFlagPath, FileMode.Open, FileAccess.Read))
                {
                    return Image.FromStream(fs);
                }
            }
            else
            {
                return null;
            }
        }
        catch
        {
            return null;
        }
    }

    private static Image ByteArrayToImage(byte[] bytes)
    {
        using (MemoryStream ms = new MemoryStream(bytes))
        {
            return Image.FromStream(ms);
        }
    }
}
