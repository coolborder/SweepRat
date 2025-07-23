using System;
using System.Windows.Forms;
using BrightIdeasSoftware;
using LazyServer;
using Newtonsoft.Json.Linq;
using Sweep.Models;
using Sweep.Services;    // MessageHandler
using Sweep.UI;          // ListViewConfigurator

namespace Sweep.Forms
{
    public partial class Sweep : Form
    {
        private readonly LazyServerHost _server;
        private readonly MessageHandler _dispatcher;

        // Inject the already-started server here
        public Sweep(LazyServerHost server)
        {
            InitializeComponent();

            if (server == null)
                throw new ArgumentNullException(nameof(server));

            _server = server;

            // 1) Configure the ObjectListView once
            ListViewConfigurator.Configure(listView1, Global.Port);

            // 2) Create your central dispatcher
            //    It will subscribe to server events and call your handlers
            _dispatcher = new MessageHandler(_server, listView1, Global.Port);
        }

        private async void seescreen_Click(object sender, EventArgs e)
        {
            foreach (var obj in listView1.SelectedObjects) {
                ClientInfo item = (ClientInfo)obj;
                if (item == null) { return; };

                ClientConnection conn = _server.GetConnectionById(item.ID);
                if (conn != null)
                {
                    await _server.SendMessageToConnection(conn, new JObject {
                        ["command"] = "ssloop",
                        ["quality"] = "100%"
                    }.ToString());
                }
            }
        }

        private void Sweep_Load(object sender, EventArgs e)
        {
            portnum.Text = portnum.Text.Replace("%s", Global.Port.ToString());
        }
    }
}
