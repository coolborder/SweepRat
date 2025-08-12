using Guna.UI2.WinForms;
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

namespace Sweep.Forms
{
    public partial class MsgBox : Form
    {
        public static string clientid;
        public LazyServerHost serverHost;

        public string icon = "error";

        public MsgBox()
        {
            InitializeComponent();

            foreach (Control c in this.Controls)
            {
                if (c is Guna2RadioButton) {
                    var radioButton = (Guna2RadioButton)c;
                    radioButton.CheckedChanged += (s, e) => {
                        if (radioButton.Checked)
                        {
                            icon = radioButton.Tag.ToString();
                        }
                    };
                }
            }
        }

        private async void submit_Click(object sender, EventArgs e)
        {
            MessageBox.Show(icon);

            var txttitle = title.Text;
            var txtmsg = txt.Text;

            var button = buttons.Text;

            if (string.IsNullOrEmpty(txttitle) || string.IsNullOrEmpty(txtmsg) || string.IsNullOrEmpty(button))
            {
                MessageBox.Show("Please fill in both fields.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            await serverHost.SendMessageToClient(clientid, new JObject {
                ["command"] = "msgbox",
                ["title"] = txttitle,
                ["message"] = txtmsg,
                ["button"] = button
            }.ToString());
        }
    }
}
