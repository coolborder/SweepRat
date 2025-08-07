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
    public partial class Chat : Form
    {
        public Action<string, bool> OnMessageSent;
        public string username = "sfhefo";
        public Chat()
        {
            InitializeComponent();
            OnMessageSent += (message, f) =>
            {
                if (f) {
                    body.AppendText(Environment.NewLine + message);
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
    }
}
