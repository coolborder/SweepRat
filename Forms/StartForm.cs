using LazyServer;
using Sweep.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sweep
{
    public partial class StartForm : Form
    {
        public StartForm()
        {
            InitializeComponent();
        }

        private async void startbtn_Click(object sender, EventArgs e)
        {
            var server = new LazyServerHost();
            await server.StartAsync(Int32.Parse(port.Text));

            var sweep = new Sweep.Forms.Sweep();
            sweep.server = server;
            this.Hide();
            sweep.Show();
            await Program.FadeIn(sweep);

            System.Media.SoundPlayer player = new System.Media.SoundPlayer(@"./Sounds/Intro.wav");
            player.Play();

            sweep.FormClosing += (object sendere, FormClosingEventArgs ae) =>
            {
                Application.Exit();
            };
        }
    }
}
