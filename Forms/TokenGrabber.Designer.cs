namespace Sweep.Forms
{
    partial class TokenGrabber
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TokenGrabber));
            this.token = new System.Windows.Forms.RichTextBox();
            this.grab = new Guna.UI2.WinForms.Guna2GradientButton();
            this.SuspendLayout();
            // 
            // token
            // 
            this.token.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.token.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.token.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.token.Location = new System.Drawing.Point(12, 12);
            this.token.Name = "token";
            this.token.Size = new System.Drawing.Size(485, 269);
            this.token.TabIndex = 0;
            this.token.Text = "Token will show here...\nPress \'Grab\' button to start...";
            this.token.TextChanged += new System.EventHandler(this.richTextBox1_TextChanged);
            // 
            // grab
            // 
            this.grab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.grab.BorderRadius = 3;
            this.grab.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.grab.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.grab.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.grab.DisabledState.FillColor2 = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.grab.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.grab.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.grab.ForeColor = System.Drawing.Color.White;
            this.grab.Location = new System.Drawing.Point(328, 285);
            this.grab.Name = "grab";
            this.grab.Size = new System.Drawing.Size(169, 35);
            this.grab.TabIndex = 1;
            this.grab.Text = "GRAB!";
            this.grab.Click += new System.EventHandler(this.grab_Click);
            // 
            // TokenGrabber
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(509, 325);
            this.Controls.Add(this.grab);
            this.Controls.Add(this.token);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TokenGrabber";
            this.Text = "Token Grabber";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox token;
        private Guna.UI2.WinForms.Guna2GradientButton grab;
    }
}