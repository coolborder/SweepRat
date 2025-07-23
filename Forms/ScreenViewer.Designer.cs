namespace Sweep.Forms
{
    partial class ScreenViewer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ScreenViewer));
            this.quality = new System.Windows.Forms.ComboBox();
            this.monitors = new System.Windows.Forms.ComboBox();
            this.mouse = new Guna.UI2.WinForms.Guna2CheckBox();
            this.guna2GradientButton1 = new Guna.UI2.WinForms.Guna2GradientButton();
            this.screenimg = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.screenimg)).BeginInit();
            this.SuspendLayout();
            // 
            // quality
            // 
            this.quality.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.quality.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.quality.FormattingEnabled = true;
            this.quality.Items.AddRange(new object[] {
            "100%",
            "90%",
            "80%",
            "70%",
            "60%",
            "50%",
            "40%",
            "30%",
            "20%",
            "10%"});
            this.quality.Location = new System.Drawing.Point(434, 386);
            this.quality.Name = "quality";
            this.quality.Size = new System.Drawing.Size(113, 25);
            this.quality.TabIndex = 2;
            this.quality.Text = "100%";
            this.quality.SelectedIndexChanged += new System.EventHandler(this.quality_SelectedIndexChanged);
            // 
            // monitors
            // 
            this.monitors.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.monitors.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.monitors.FormattingEnabled = true;
            this.monitors.Items.AddRange(new object[] {
            "0"});
            this.monitors.Location = new System.Drawing.Point(315, 386);
            this.monitors.Name = "monitors";
            this.monitors.Size = new System.Drawing.Size(113, 25);
            this.monitors.TabIndex = 3;
            this.monitors.Text = "0";
            this.monitors.SelectedIndexChanged += new System.EventHandler(this.monitors_SelectedIndexChanged);
            // 
            // mouse
            // 
            this.mouse.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mouse.AutoSize = true;
            this.mouse.CheckedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.mouse.CheckedState.BorderRadius = 0;
            this.mouse.CheckedState.BorderThickness = 0;
            this.mouse.CheckedState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.mouse.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.mouse.Location = new System.Drawing.Point(8, 4);
            this.mouse.Name = "mouse";
            this.mouse.Size = new System.Drawing.Size(58, 17);
            this.mouse.TabIndex = 4;
            this.mouse.Text = "Mouse";
            this.mouse.UncheckedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(125)))), ((int)(((byte)(137)))), ((int)(((byte)(149)))));
            this.mouse.UncheckedState.BorderRadius = 0;
            this.mouse.UncheckedState.BorderThickness = 0;
            this.mouse.UncheckedState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(125)))), ((int)(((byte)(137)))), ((int)(((byte)(149)))));
            // 
            // guna2GradientButton1
            // 
            this.guna2GradientButton1.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.guna2GradientButton1.BorderRadius = 3;
            this.guna2GradientButton1.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.guna2GradientButton1.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.guna2GradientButton1.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.guna2GradientButton1.DisabledState.FillColor2 = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.guna2GradientButton1.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.guna2GradientButton1.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.guna2GradientButton1.ForeColor = System.Drawing.Color.White;
            this.guna2GradientButton1.Image = global::Sweep.Properties.Resources.pause;
            this.guna2GradientButton1.ImageAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.guna2GradientButton1.Location = new System.Drawing.Point(117, 386);
            this.guna2GradientButton1.Name = "guna2GradientButton1";
            this.guna2GradientButton1.Size = new System.Drawing.Size(203, 25);
            this.guna2GradientButton1.TabIndex = 1;
            this.guna2GradientButton1.Text = "Stop";
            this.guna2GradientButton1.Click += new System.EventHandler(this.guna2GradientButton1_Click);
            // 
            // screenimg
            // 
            this.screenimg.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.screenimg.Image = global::Sweep.Properties.Resources._019322b3_e9f2_7547_8222_14edf052d8ab;
            this.screenimg.Location = new System.Drawing.Point(1, 23);
            this.screenimg.Name = "screenimg";
            this.screenimg.Size = new System.Drawing.Size(666, 355);
            this.screenimg.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.screenimg.TabIndex = 0;
            this.screenimg.TabStop = false;
            // 
            // ScreenViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(668, 418);
            this.Controls.Add(this.mouse);
            this.Controls.Add(this.monitors);
            this.Controls.Add(this.quality);
            this.Controls.Add(this.guna2GradientButton1);
            this.Controls.Add(this.screenimg);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(684, 456);
            this.Name = "ScreenViewer";
            this.Text = "Screen Viewer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ScreenViewer_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.screenimg)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox screenimg;
        private Guna.UI2.WinForms.Guna2GradientButton guna2GradientButton1;
        private System.Windows.Forms.ComboBox quality;
        private System.Windows.Forms.ComboBox monitors;
        private Guna.UI2.WinForms.Guna2CheckBox mouse;
    }
}