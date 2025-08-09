using LazyServer;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace Sweep.Forms
{
    public partial class Chat : Form
    {
        public Action<string, bool> OnMessageSent;
        public string username = "sfhefo";
        public LazyServerHost serverHost;
        public string connid = string.Empty; // Connection ID for the client
        public Chat()
        {
            InitializeComponent();
            OnMessageSent += (message, f) =>
            {
                if (f) {
                    void senda() {
                        body.AppendText(Environment.NewLine + message);
                    }
                    if (InvokeRequired)
                    {
                        Invoke(new Action(senda));
                    }
                    else
                    {
                        senda();
                    }
                }
            };

        }

        void send() {
            OnMessageSent.Invoke(chatmsg.Text, false);
            body.AppendText(Environment.NewLine + $"<{username}>: {chatmsg.Text}");
            chatmsg.Text = string.Empty;
        }
        private void guna2GradientButton1_Click(object sender, EventArgs e)
        {
            send();
        }

        private void chatmsg_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true; // Prevent the beep sound on Enter key press
                send();
            }
        }

        private void Chat_Load(object sender, EventArgs e)
        {

            serverHost.MessageReceived += (s, e) => {
                try
                {
                    var msg = JObject.Parse(e.Message);
                    switch ((string)msg["msg"])
                    {
                        case "chatmsg":
                            if (e.ClientId == connid)
                            {
                                string username = (string)msg["username"];
                                string text = (string)msg["text"];
                                OnMessageSent.Invoke($"<{username}>: {text}", true);
                            }
                            ;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                ;
            };
        }

        private async void nosend_CheckedChanged(object sender, EventArgs e)
        {
            OnMessageSent.Invoke(!nosend.Checked ? "[ Sending Enabled ]" : "[ Sending Disabled ]", true);
            await serverHost.SendMessageToClient(connid, new JObject
            {
                ["command"] = "togglesend",
                ["body"] = !nosend.Checked
            }.ToString());
        }

        private async void Chat_FormClosing(object sender, FormClosingEventArgs e)
        {
            await serverHost.SendMessageToClient(connid, new JObject
            {
                ["command"] = "chatclose"
            }.ToString());
        }
    }
}
