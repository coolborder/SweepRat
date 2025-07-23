using System.Drawing;

namespace Sweep.Models
{
    public class ClientInfo
    {
        public Image Screen { get; set; }
        public string IP { get; set; }
        public string Country { get; set; }
        public Image Flag { get; set; }
        public string ID { get; set; }
        public string Username { get; set; }
        public string OperatingSystem { get; set; }
        public string CPU { get; set; }
        public string GPU { get; set; }
        public string UAC { get; set; }
        public string HWID { get; set; }
    }
}
