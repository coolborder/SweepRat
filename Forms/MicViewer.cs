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
    public partial class MicViewer : Form
    {
        private bool pushbox = true;
        public Action<bool> Mic;
        public Action<int> DeviceChanged;
        public Action MicClosing;
        public MicViewer()
        {
            InitializeComponent();
        }
        public void SetMonitors(int idx)
        {
            monitors.Items.Clear();
            for (int i = 0; i < idx; i++)
            {
                monitors.Items.Add(i);
            }
        }
        private void monitors_SelectedIndexChanged(object sender, EventArgs e)
        {
            DeviceChanged.Invoke(Int32.Parse(monitors.Text));
        }

        private void guna2GradientButton1_Click(object sender, EventArgs e)
        {
            pushbox = !pushbox;
            guna2GradientButton1.Text = pushbox ? "Stop" : "Play";
            guna2GradientButton1.Image = pushbox ? global::Sweep.Properties.Resources.pause : global::Sweep.Properties.Resources.play;

            Mic.Invoke(pushbox);
        }

        private void MicViewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            MicClosing?.Invoke();
        }
    }
}
