using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

public class FlagDownloader
{
    private static readonly HttpClient httpClient = new HttpClient();
    public static async Task<string> GetCountryAsync(string ip)
    {
        string countryCode = "AQ"; // Default fallback

        try
        {
            string apiUrl = $"https://ipapi.co/{ip}/country/";
            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                countryCode = (await response.Content.ReadAsStringAsync()).Trim().ToUpper();
            }
        }
        catch
        {}

        if (countryCode == "AQ") {
            return "Agartha"; // real mature of me
        }

        RegionInfo myRI1 = new RegionInfo(countryCode);

        return myRI1.EnglishName;
    }


    public static async Task<Image> GetFlagImageByIpAsync(string ip)
    {
        string countryCode = "AQ"; // Default fallback

        try
        {
            string apiUrl = $"https://ipapi.co/{ip}/country/";
            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                countryCode = (await response.Content.ReadAsStringAsync()).Trim().ToUpper();
            }
        }
        catch { }

        string flagUrl = $"https://flagcdn.com/h240/{countryCode.ToLower()}.png";

        try
        {
            byte[] imageBytes = await httpClient.GetByteArrayAsync(flagUrl);
            return ByteArrayToImage(imageBytes);
        }
        catch
        {
            try
            {
                byte[] fallbackBytes = await httpClient.GetByteArrayAsync("https://flagcdn.com/h240/aq.png");
                return ByteArrayToImage(fallbackBytes);
            }
            catch
            {
                return null;
            }
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
