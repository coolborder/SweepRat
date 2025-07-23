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
            await server.StartAsync(int.Parse(port.Text));
            Global.Port = int.Parse(port.Text);

            var sweep = new Sweep.Forms.Sweep(server);
            this.Hide();
            sweep.Show();
            await Program.FadeIn(sweep);

            var player = new System.Media.SoundPlayer(@"./Sounds/Intro.wav");
            player.Play();

            sweep.FormClosing += (s, args) => Application.Exit();
        }

    }
}
