using PentestTools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sweep.Forms
{
    public partial class Chat : Form
    {
        public static Chat Instance;
        public Action<string> MessageSent;
        public Action<bool> ChangeSend;
        public string ip;
        public Chat()
        {
            InitializeComponent();
            Instance = this;
        }
        public void add(string thing) {
            input.Text = string.Empty;
            chatb.Multiline = true;
            chatb.AppendText(Environment.NewLine + thing);
        }

        async void send() {
            if (String.IsNullOrEmpty(ip)) {
                ip = await GClass.GetIp();
            }

            MessageSent?.Invoke(input.Text);
            add($"<{DeviceInfo.GetUsername()}@{ip}>: {input.Text}");
        }

        private void Chat_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
        }

        private void guna2GradientButton1_Click(object sender, EventArgs e)
        {
            send();
        }

        private void input_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter && !string.IsNullOrEmpty(input.Text))
            {
                e.Handled = true; // Prevent the ding sound on Enter key press
                send();
            }
        }

        private void Chat_Load(object sender, EventArgs e)
        {
            ChangeSend += (enabled) =>
            {
                add(enabled ? "[ Sending Enabled ]" : "[ Sending Disabled ]");
                guna2GradientButton1.Enabled = enabled;
                input.Enabled = enabled;
            };
        }
    }
}
