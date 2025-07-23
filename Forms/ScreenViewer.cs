using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace Sweep.Forms
{
    public partial class ScreenViewer : Form
    {
        public Action<string> QualityChanged;
        public Action<bool> ScreenEvent;
        public Action<int> MonitorChanged;
        public Action Closing;

        public bool pushbox = true;

        public ScreenViewer()
        {
            InitializeComponent();
        }
        public void SetScreen(Image img) {
            screenimg.Image = img;
        }

        public void SetMonitors(int idx) {
            for (int i = 0; i > idx; i++) {
                monitors.Items.Add(i);
            }
        }

        private void quality_SelectedIndexChanged(object sender, EventArgs e)
        {
            QualityChanged.Invoke(quality.Text);
        }

        private void guna2GradientButton1_Click(object sender, EventArgs e)
        {
            pushbox = !pushbox;
            ScreenEvent?.Invoke(pushbox);

            guna2GradientButton1.Text = pushbox ? "Stop" : "Play";
            guna2GradientButton1.Image = pushbox ? global::Sweep.Properties.Resources.pause : global::Sweep.Properties.Resources.play;
        }

        private void ScreenViewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            Closing.Invoke();
        }

        private void monitors_SelectedIndexChanged(object sender, EventArgs e)
        {
            MonitorChanged.Invoke(Int32.Parse(monitors.Text));
        }
    }
}
