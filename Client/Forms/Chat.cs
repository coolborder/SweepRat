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
        public Chat()
        {
            InitializeComponent();
            Instance = this;
        }

        public void add(string thing) {
            chatb.Multiline = true;
            chatb.AppendText(Environment.NewLine + thing);
        }

        private void Chat_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
        }
    }
}
