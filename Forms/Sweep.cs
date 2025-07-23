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

namespace Sweep.Forms
{
    public partial class Sweep : Form
    {
        public LazyServerHost server;
        public Sweep()
        {
            InitializeComponent();
            listView1.OwnerDraw = true;
            listView1.FullRowSelect = true;
            listView1.View = View.Details;

            listView1.DrawColumnHeader += (s, e) =>
            {
                e.DrawDefault = true;
            };

            listView1.DrawItem += (s, e) =>
            {
                // No need to draw here unless you want selection background
                e.DrawBackground();
            };

            listView1.DrawSubItem += (s, e) =>
            {
                e.DrawDefault = true;

                // Only draw separator once per row (on last subitem)
                if (e.ColumnIndex == listView1.Columns.Count - 1 && e.ItemIndex < listView1.Items.Count - 1)
                {
                    Rectangle rowBounds = e.Bounds;
                    rowBounds.X = listView1.Bounds.Left;
                    rowBounds.Width = listView1.Width;

                    e.Graphics.DrawLine(Pens.Gray, rowBounds.Left, rowBounds.Bottom - 1, rowBounds.Right, rowBounds.Bottom - 1);
                }
            };
        }

        private void seescreen_Click(object sender, EventArgs e)
        {

        }
    }
}
