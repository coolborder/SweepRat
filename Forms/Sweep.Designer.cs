using System.Drawing;
using System.Windows.Forms;

namespace Sweep.Forms
{
    partial class Sweep
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Sweep));
            this.rat = new Guna.UI2.WinForms.Guna2ContextMenuStrip();
            this.listView1 = new BrightIdeasSoftware.ObjectListView();
            this.portnum = new System.Windows.Forms.Label();
            this.clients = new System.Windows.Forms.Label();
            this.seescreen = new System.Windows.Forms.ToolStripMenuItem();
            this.rat.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.listView1)).BeginInit();
            this.SuspendLayout();
            // 
            // rat
            // 
            this.rat.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.rat.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.seescreen});
            this.rat.Name = "rat";
            this.rat.RenderStyle.ArrowColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(143)))), ((int)(((byte)(255)))));
            this.rat.RenderStyle.BorderColor = System.Drawing.Color.Gainsboro;
            this.rat.RenderStyle.ColorTable = null;
            this.rat.RenderStyle.RoundedEdges = true;
            this.rat.RenderStyle.SelectionArrowColor = System.Drawing.Color.White;
            this.rat.RenderStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(88)))), ((int)(((byte)(255)))));
            this.rat.RenderStyle.SelectionForeColor = System.Drawing.Color.White;
            this.rat.RenderStyle.SeparatorColor = System.Drawing.Color.Gainsboro;
            this.rat.RenderStyle.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
            this.rat.Size = new System.Drawing.Size(181, 48);
            // 
            // listView1
            // 
            this.listView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listView1.ContextMenuStrip = this.rat;
            this.listView1.HideSelection = false;
            this.listView1.Location = new System.Drawing.Point(47, 0);
            this.listView1.Name = "listView1";
            this.listView1.RowHeight = 25;
            this.listView1.SelectedColumnTint = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.listView1.ShowGroups = false;
            this.listView1.Size = new System.Drawing.Size(934, 447);
            this.listView1.TabIndex = 1;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            // 
            // portnum
            // 
            this.portnum.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.portnum.AutoSize = true;
            this.portnum.Location = new System.Drawing.Point(74, 452);
            this.portnum.Name = "portnum";
            this.portnum.Size = new System.Drawing.Size(54, 13);
            this.portnum.TabIndex = 2;
            this.portnum.Text = "[ Port %s ]";
            this.portnum.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // clients
            // 
            this.clients.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.clients.AutoSize = true;
            this.clients.Location = new System.Drawing.Point(4, 452);
            this.clients.Name = "clients";
            this.clients.Size = new System.Drawing.Size(66, 13);
            this.clients.TabIndex = 3;
            this.clients.Text = "[ Clients %s ]";
            this.clients.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // seescreen
            // 
            this.seescreen.Image = global::Sweep.Properties.Resources.desktop;
            this.seescreen.Name = "seescreen";
            this.seescreen.Size = new System.Drawing.Size(180, 22);
            this.seescreen.Text = "See Screen";
            this.seescreen.Click += new System.EventHandler(this.seescreen_Click);
            // 
            // Sweep
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(981, 471);
            this.Controls.Add(this.clients);
            this.Controls.Add(this.portnum);
            this.Controls.Add(this.listView1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Sweep";
            this.Opacity = 0D;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Sweep";
            this.Load += new System.EventHandler(this.Sweep_Load);
            this.rat.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.listView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private Guna.UI2.WinForms.Guna2ContextMenuStrip rat;
        private System.Windows.Forms.ToolStripMenuItem seescreen;
        private BrightIdeasSoftware.ObjectListView listView1;
        private Label portnum;
        private Label clients;
    }
}