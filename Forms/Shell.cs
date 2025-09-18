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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace Sweep.Forms
{
    public partial class Shell : Form
    {
        public LazyServerHost serverHost;
        public string connid;
        public Shell()
        {
            InitializeComponent();
        }

        private void cmd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) {
                _ = serverHost.SendMessageToClient(connid, new JObject
                {
                    ["command"] = "shell",
                    ["body"] = cmd.Text
                }.ToString());
                cmd.Text = string.Empty;
                e.Handled = true;
            }
        }

        private void Shell_Load(object sender, EventArgs e)
        {
            serverHost.MessageReceived += (s, e) => {
                try
                {
                    var msg = JObject.Parse(e.Message);

                    switch ((string)msg["msg"])
                    {
                        case "shellout":
                            void senda()
                            {
                                textBox1.AppendText(Environment.NewLine + (string)msg["body"]);
                            }
                            if (InvokeRequired)
                            {
                                Invoke(new Action(senda));
                            }
                            else
                            {
                                senda();
                            }
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
    }
}
