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
            this.listView1 = new System.Windows.Forms.ListView();
            this.ss = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ip = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.country = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.flag = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.id = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.user = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.os = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.cpu = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.gpu = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.uac = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.rat = new Guna.UI2.WinForms.Guna2ContextMenuStrip();
            this.seescreen = new System.Windows.Forms.ToolStripMenuItem();
            this.rat.SuspendLayout();
            this.SuspendLayout();
            // 
            // listView1
            // 
            this.listView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.ss,
            this.ip,
            this.country,
            this.flag,
            this.id,
            this.user,
            this.os,
            this.cpu,
            this.gpu,
            this.uac});
            this.listView1.FullRowSelect = true;
            this.listView1.HideSelection = false;
            this.listView1.Location = new System.Drawing.Point(60, 0);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(932, 466);
            this.listView1.TabIndex = 0;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            // 
            // ss
            // 
            this.ss.Text = "Screen";
            this.ss.Width = 46;
            // 
            // ip
            // 
            this.ip.Text = "IP";
            this.ip.Width = 100;
            // 
            // country
            // 
            this.country.Text = "Country";
            this.country.Width = 90;
            // 
            // flag
            // 
            this.flag.Text = "Flag";
            this.flag.Width = 40;
            // 
            // id
            // 
            this.id.Text = "ID";
            this.id.Width = 100;
            // 
            // user
            // 
            this.user.Text = "Username";
            this.user.Width = 80;
            // 
            // os
            // 
            this.os.Text = "Operating System";
            this.os.Width = 115;
            // 
            // cpu
            // 
            this.cpu.Text = "CPU";
            this.cpu.Width = 145;
            // 
            // gpu
            // 
            this.gpu.Text = "GPU";
            this.gpu.Width = 152;
            // 
            // uac
            // 
            this.uac.Text = "UAC?";
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
            this.rat.Size = new System.Drawing.Size(131, 26);
            // 
            // seescreen
            // 
            this.seescreen.Image = global::Sweep.Properties.Resources.desktop;
            this.seescreen.Name = "seescreen";
            this.seescreen.Size = new System.Drawing.Size(130, 22);
            this.seescreen.Text = "See Screen";
            this.seescreen.Click += new System.EventHandler(this.seescreen_Click);
            // 
            // Sweep
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(992, 466);
            this.Controls.Add(this.listView1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Sweep";
            this.Opacity = 0D;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Sweep";
            this.rat.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ColumnHeader ss;
        private System.Windows.Forms.ColumnHeader ip;
        private System.Windows.Forms.ColumnHeader country;
        private System.Windows.Forms.ColumnHeader flag;
        private System.Windows.Forms.ColumnHeader id;
        private System.Windows.Forms.ColumnHeader user;
        private System.Windows.Forms.ColumnHeader os;
        private System.Windows.Forms.ColumnHeader cpu;
        private System.Windows.Forms.ColumnHeader gpu;
        private System.Windows.Forms.ColumnHeader uac;
        private Guna.UI2.WinForms.Guna2ContextMenuStrip rat;
        private System.Windows.Forms.ToolStripMenuItem seescreen;
    }
}