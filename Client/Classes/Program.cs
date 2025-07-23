using LazyServer;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        public static JObject config = GClass.config();
        static async Task Main(string[] args)
        {
            GClass.ShowCmd(GClass.IsTrue("debug"));

            var client = new LazyServerClient();
            await GClass.ReconnectLoop(client);

            client.Disconnected += async (s, e) => {
                await GClass.ReconnectLoop(client);
            };


            Console.Read();
        }
    }
}
