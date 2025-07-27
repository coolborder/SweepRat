namespace Sweep.Forms
{
    partial class Builder
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Builder));
            this.guna2TabControl1 = new Guna.UI2.WinForms.Guna2TabControl();
            this.conn = new System.Windows.Forms.TabPage();
            this.opt = new System.Windows.Forms.TabPage();
            this.build = new System.Windows.Forms.TabPage();
            this.builddialog = new System.Windows.Forms.SaveFileDialog();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.guna2GradientButton1 = new Guna.UI2.WinForms.Guna2GradientButton();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.guna2TabControl1.SuspendLayout();
            this.build.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.SuspendLayout();
            // 
            // guna2TabControl1
            // 
            this.guna2TabControl1.Controls.Add(this.conn);
            this.guna2TabControl1.Controls.Add(this.opt);
            this.guna2TabControl1.Controls.Add(this.build);
            this.guna2TabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.guna2TabControl1.ItemSize = new System.Drawing.Size(180, 40);
            this.guna2TabControl1.Location = new System.Drawing.Point(0, 0);
            this.guna2TabControl1.Name = "guna2TabControl1";
            this.guna2TabControl1.SelectedIndex = 0;
            this.guna2TabControl1.Size = new System.Drawing.Size(545, 369);
            this.guna2TabControl1.TabButtonHoverState.BorderColor = System.Drawing.Color.Empty;
            this.guna2TabControl1.TabButtonHoverState.FillColor = System.Drawing.Color.WhiteSmoke;
            this.guna2TabControl1.TabButtonHoverState.Font = new System.Drawing.Font("Segoe UI Semibold", 10F);
            this.guna2TabControl1.TabButtonHoverState.ForeColor = System.Drawing.Color.Black;
            this.guna2TabControl1.TabButtonHoverState.InnerColor = System.Drawing.Color.Magenta;
            this.guna2TabControl1.TabButtonIdleState.BorderColor = System.Drawing.Color.Empty;
            this.guna2TabControl1.TabButtonIdleState.FillColor = System.Drawing.Color.White;
            this.guna2TabControl1.TabButtonIdleState.Font = new System.Drawing.Font("Segoe UI Semibold", 10F);
            this.guna2TabControl1.TabButtonIdleState.ForeColor = System.Drawing.Color.Black;
            this.guna2TabControl1.TabButtonIdleState.InnerColor = System.Drawing.Color.White;
            this.guna2TabControl1.TabButtonSelectedState.BorderColor = System.Drawing.Color.Empty;
            this.guna2TabControl1.TabButtonSelectedState.FillColor = System.Drawing.Color.White;
            this.guna2TabControl1.TabButtonSelectedState.Font = new System.Drawing.Font("Segoe UI Semibold", 10F);
            this.guna2TabControl1.TabButtonSelectedState.ForeColor = System.Drawing.Color.Black;
            this.guna2TabControl1.TabButtonSelectedState.InnerColor = System.Drawing.Color.Magenta;
            this.guna2TabControl1.TabButtonSize = new System.Drawing.Size(180, 40);
            this.guna2TabControl1.TabIndex = 0;
            this.guna2TabControl1.TabMenuBackColor = System.Drawing.Color.White;
            this.guna2TabControl1.TabMenuOrientation = Guna.UI2.WinForms.TabMenuOrientation.HorizontalTop;
            // 
            // conn
            // 
            this.conn.Location = new System.Drawing.Point(4, 44);
            this.conn.Name = "conn";
            this.conn.Padding = new System.Windows.Forms.Padding(3);
            this.conn.Size = new System.Drawing.Size(537, 321);
            this.conn.TabIndex = 0;
            this.conn.Text = "Server";
            this.conn.UseVisualStyleBackColor = true;
            // 
            // opt
            // 
            this.opt.Location = new System.Drawing.Point(4, 44);
            this.opt.Name = "opt";
            this.opt.Padding = new System.Windows.Forms.Padding(3);
            this.opt.Size = new System.Drawing.Size(537, 321);
            this.opt.TabIndex = 1;
            this.opt.Text = "Options";
            this.opt.UseVisualStyleBackColor = true;
            // 
            // build
            // 
            this.build.Controls.Add(this.label2);
            this.build.Controls.Add(this.pictureBox2);
            this.build.Controls.Add(this.label1);
            this.build.Controls.Add(this.pictureBox1);
            this.build.Controls.Add(this.guna2GradientButton1);
            this.build.Location = new System.Drawing.Point(4, 44);
            this.build.Name = "build";
            this.build.Padding = new System.Windows.Forms.Padding(3);
            this.build.Size = new System.Drawing.Size(537, 321);
            this.build.TabIndex = 2;
            this.build.Text = "Build";
            this.build.UseVisualStyleBackColor = true;
            // 
            // builddialog
            // 
            this.builddialog.DefaultExt = "exe";
            this.builddialog.FileName = "SweepClient";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::Sweep.Properties.Resources.exclamation_mark;
            this.pictureBox1.Location = new System.Drawing.Point(8, 6);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(37, 50);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // guna2GradientButton1
            // 
            this.guna2GradientButton1.BorderRadius = 3;
            this.guna2GradientButton1.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.guna2GradientButton1.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.guna2GradientButton1.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.guna2GradientButton1.DisabledState.FillColor2 = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.guna2GradientButton1.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.guna2GradientButton1.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.guna2GradientButton1.ForeColor = System.Drawing.Color.White;
            this.guna2GradientButton1.Image = global::Sweep.Properties.Resources.play;
            this.guna2GradientButton1.ImageAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.guna2GradientButton1.Location = new System.Drawing.Point(8, 271);
            this.guna2GradientButton1.Name = "guna2GradientButton1";
            this.guna2GradientButton1.Size = new System.Drawing.Size(521, 42);
            this.guna2GradientButton1.TabIndex = 0;
            this.guna2GradientButton1.Text = "BUILD!";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(50, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(228, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Size of stub may vary depending on selections.";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(50, 74);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(384, 26);
            this.label2.TabIndex = 4;
            this.label2.Text = "If you choose to build or use the stub, you are fully responsible for how it is u" +
    "sed.\r\nAny misuse is your own legal and ethical responsibility.";
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = global::Sweep.Properties.Resources.exclamation_mark;
            this.pictureBox2.Location = new System.Drawing.Point(8, 62);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(37, 50);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox2.TabIndex = 3;
            this.pictureBox2.TabStop = false;
            // 
            // Builder
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(545, 369);
            this.Controls.Add(this.guna2TabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Builder";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Builder";
            this.guna2TabControl1.ResumeLayout(false);
            this.build.ResumeLayout(false);
            this.build.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Guna.UI2.WinForms.Guna2TabControl guna2TabControl1;
        private System.Windows.Forms.TabPage conn;
        private System.Windows.Forms.TabPage opt;
        private System.Windows.Forms.TabPage build;
        private System.Windows.Forms.PictureBox pictureBox1;
        private Guna.UI2.WinForms.Guna2GradientButton guna2GradientButton1;
        private System.Windows.Forms.SaveFileDialog builddialog;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.PictureBox pictureBox2;
    }
}