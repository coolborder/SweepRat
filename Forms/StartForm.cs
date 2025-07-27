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
        Remembrance remembrance = new Remembrance();
        public StartForm()
        {
            InitializeComponent();
            username.Text = remembrance.GetAttribute<string>("username") ?? "sfhefo";

            if (remembrance.GetAttribute<int>("port").ToString() == "0")
            {
                port.Text = "6969";
            }
            else {
                port.Text = remembrance.GetAttribute<int>("port").ToString() ?? "6969";
            }

            
        }
        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private async void startbtn_Click(object sender, EventArgs e)
        {
            var server = new LazyServerHost();
            remembrance.SetAttribute("username", username.Text);

            await server.StartAsync(int.Parse(port.Text));
            Global.Port = Clamp(int.Parse(port.Text), 1000, 9999);
            Global.Name = username.Text;

            remembrance.SetAttribute("port", Global.Port);

            var sweep = new Sweep.Forms.Sweep(server);
            this.Hide();
            sweep.Show();
            sweep.Opacity = 0;
            await Program.FadeIn(sweep);

            var player = new System.Media.SoundPlayer(@"./Sounds/Intro.wav");
            player.Play();

            sweep.FormClosing += (s, args) => Application.Exit();
        }

        private void port_TextChanged(object sender, EventArgs e)
        {
            string digitsOnly = new string(port.Text.Where(char.IsDigit).ToArray());

            // Cap to 4 digits
            if (digitsOnly.Length > 4)
                digitsOnly = digitsOnly.Substring(0, 4);

            // Update TextBox only if needed
            if (port.Text != digitsOnly)
            {
                int cursor = port.SelectionStart;
                int diff = port.Text.Length - digitsOnly.Length;
                port.Text = digitsOnly;
                port.SelectionStart = Math.Max(0, cursor - diff);
            }
        }

        private void port_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }

            // Prevent typing more than 4 digits
            if (char.IsDigit(e.KeyChar) && port.Text.Length >= 4 && port.SelectionLength == 0)
            {
                e.Handled = true;
            }
        }
    }
}
