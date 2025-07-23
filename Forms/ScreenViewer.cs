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
        public Action<bool> Screen;
        public Action Closing;

        public bool pushbox = true;

        public ScreenViewer()
        {
            InitializeComponent();
        }
        public void SetScreen(Image img) {
            screenimg.Image = img;
        }

        private void quality_SelectedIndexChanged(object sender, EventArgs e)
        {
            QualityChanged.Invoke(quality.Text);
        }

        private void guna2GradientButton1_Click(object sender, EventArgs e)
        {
            pushbox = !pushbox;
            Screen.Invoke(pushbox);
        }

        private void ScreenViewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            Closing.Invoke();
        }
    }
}
