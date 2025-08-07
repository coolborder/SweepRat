namespace Sweep.Forms
{
    partial class VBScriptExecute
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VBScriptExecute));
            this.yo = new FastColoredTextBoxNS.FastColoredTextBox();
            this.execute = new Guna.UI2.WinForms.Guna2GradientButton();
            ((System.ComponentModel.ISupportInitialize)(this.yo)).BeginInit();
            this.SuspendLayout();
            // 
            // yo
            // 
            this.yo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.yo.AutoCompleteBracketsList = new char[] {
        '(',
        ')',
        '{',
        '}',
        '[',
        ']',
        '\"',
        '\"',
        '\'',
        '\''};
            this.yo.AutoIndentCharsPatterns = "\r\n^\\s*[\\w\\.\\(\\)]+\\s*(?<range>=)\\s*(?<range>.+)\r\n";
            this.yo.AutoScrollMinSize = new System.Drawing.Size(499, 140);
            this.yo.BackBrush = null;
            this.yo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.yo.CharHeight = 14;
            this.yo.CharWidth = 8;
            this.yo.CommentPrefix = "\'";
            this.yo.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.yo.DisabledColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.yo.IsReplaceMode = false;
            this.yo.Language = FastColoredTextBoxNS.Language.VB;
            this.yo.LeftBracket = '(';
            this.yo.Location = new System.Drawing.Point(0, 0);
            this.yo.Name = "yo";
            this.yo.Paddings = new System.Windows.Forms.Padding(0);
            this.yo.RightBracket = ')';
            this.yo.SelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))));
            this.yo.ServiceColors = ((FastColoredTextBoxNS.ServiceColors)(resources.GetObject("yo.ServiceColors")));
            this.yo.Size = new System.Drawing.Size(677, 359);
            this.yo.TabIndex = 0;
            this.yo.Text = resources.GetString("yo.Text");
            this.yo.Zoom = 100;
            this.yo.TextChanged += new System.EventHandler<FastColoredTextBoxNS.TextChangedEventArgs>(this.yo_TextChanged);
            // 
            // execute
            // 
            this.execute.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.execute.BorderRadius = 3;
            this.execute.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.execute.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.execute.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.execute.DisabledState.FillColor2 = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.execute.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.execute.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.execute.ForeColor = System.Drawing.Color.White;
            this.execute.Location = new System.Drawing.Point(3, 363);
            this.execute.Name = "execute";
            this.execute.Size = new System.Drawing.Size(671, 45);
            this.execute.TabIndex = 1;
            this.execute.Text = "Execute";
            this.execute.Click += new System.EventHandler(this.execute_Click);
            // 
            // VBScriptExecute
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(677, 411);
            this.Controls.Add(this.execute);
            this.Controls.Add(this.yo);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "VBScriptExecute";
            this.Text = "VBS Executor";
            ((System.ComponentModel.ISupportInitialize)(this.yo)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private FastColoredTextBoxNS.FastColoredTextBox yo;
        private Guna.UI2.WinForms.Guna2GradientButton execute;
    }
}