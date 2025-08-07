using LazyServer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sweep.Forms
{
    public partial class VBScriptExecute : Form
    {
        public string connid;
        public LazyServerHost serverHost;

        private Remembrance remembrance = new Remembrance();
        public VBScriptExecute()
        {
            InitializeComponent();
            if (!String.IsNullOrEmpty(remembrance.GetAttribute<string>("vbscript"))) {
                yo.Text = remembrance.GetAttribute<string>("vbscript");
            }
        }

        private async void execute_Click(object sender, EventArgs e)
        {
            await serverHost.SendMessageToClient(connid, new JObject {
                ["command"] = "vbexec",
                ["body"] = yo.Text
            }.ToString());
        }

        private void yo_TextChanged(object sender, FastColoredTextBoxNS.TextChangedEventArgs e)
        {
            remembrance.SetAttribute("vbscript", yo.Text);
        }
    }
}
