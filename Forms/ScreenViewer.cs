using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Sweep.Forms
{
    public partial class ScreenViewer : Form
    {
        public Action<string> QualityChanged;
        public Action<bool> ScreenEvent;
        public Action<int> MonitorChanged;
        public Action<Point> MouseMoved;
        public Action<string> MouseClicked;
        public Action<int> MouseScrolled;
        public Action<string> KeyPressed; // Now string instead of Keys
        public Action Closing;

        public bool pushbox = true;

        public int width = 1920;   // screen width of remote monitor
        public int height = 1080;  // screen height of remote monitor

        public ScreenViewer()
        {
            InitializeComponent();
            screenimg.MouseMove += Screenimg_MouseMove;
            screenimg.MouseDown += Screenimg_MouseDown;
            screenimg.MouseUp += Screenimg_MouseUp;
            screenimg.MouseWheel += Screenimg_MouseWheel;

            this.KeyPreview = true;
            this.KeyDown += ScreenViewer_KeyDown;
        }

        public void SetScreen(Image img)
        {
            screenimg.Image = img;
        }

        public void SetMonitors(int idx)
        {
            monitors.Items.Clear();
            for (int i = 0; i < idx; i++)
            {
                monitors.Items.Add(i);
            }
        }

        int Clamp(int val, int min, int max)
        {
            return Math.Max(min, Math.Min(max, val));
        }

        private void Screenimg_MouseMove(object sender, MouseEventArgs e)
        {
            if (!mouse.Checked || screenimg.Image == null) return;

            Point remotePoint = GetRemoteScreenPointFromMouse(e.Location);
            MouseMoved?.Invoke(remotePoint);
        }

        private void Screenimg_MouseDown(object sender, MouseEventArgs e)
        {
            if (!mouse.Checked) return;

            string button = e.Button switch
            {
                MouseButtons.Left => "left_down",
                MouseButtons.Right => "right_down",
                MouseButtons.Middle => "middle_down",
                _ => null
            };

            if (button != null)
                MouseClicked?.Invoke(button);
        }

        private void Screenimg_MouseUp(object sender, MouseEventArgs e)
        {
            if (!mouse.Checked) return;

            string button = e.Button switch
            {
                MouseButtons.Left => "left_up",
                MouseButtons.Right => "right_up",
                MouseButtons.Middle => "middle_up",
                _ => null
            };

            if (button != null)
                MouseClicked?.Invoke(button);
        }

        private void Screenimg_MouseWheel(object sender, MouseEventArgs e)
        {
            if (!mouse.Checked) return;

            MouseScrolled?.Invoke(e.Delta);
        }

        private void ScreenViewer_KeyDown(object sender, KeyEventArgs e)
        {
            if (!keyboard.Checked) return;

            string keyString = MapKeyEventToString(e);

            if (!string.IsNullOrEmpty(keyString))
            {
                KeyPressed?.Invoke(keyString);
                e.Handled = true;
            }
        }

        private string MapKeyEventToString(KeyEventArgs e)
        {
            bool shift = e.Shift;
            bool caps = Control.IsKeyLocked(Keys.CapsLock);

            // Handle named keys
            switch (e.KeyCode)
            {
                case Keys.Enter: return "Enter";
                case Keys.Escape: return "Escape";
                case Keys.Tab: return "Tab";
                case Keys.Back: return "Backspace";
                case Keys.Space: return " ";
                case Keys.Left: return "Left";
                case Keys.Right: return "Right";
                case Keys.Up: return "Up";
                case Keys.Down: return "Down";
                case Keys.Delete: return "Delete";
                case Keys.Insert: return "Insert";
                case Keys.Home: return "Home";
                case Keys.End: return "End";
                case Keys.PageUp: return "PageUp";
                case Keys.PageDown: return "PageDown";
                case Keys.PrintScreen: return "PrintScreen";
                case Keys.Pause: return "Pause";
                case Keys.NumLock: return "NumLock";
                case Keys.Scroll: return "ScrollLock";
                case Keys.CapsLock: return "CapsLock";
                case Keys.LWin: return "LeftWindows";
                case Keys.RWin: return "RightWindows";
                case Keys.Apps: return "Menu";

                // Modifier keys
                case Keys.ShiftKey: return "Shift";
                case Keys.ControlKey: return "Ctrl";
                case Keys.Menu: return "Alt";

                // Function keys
                case >= Keys.F1 and <= Keys.F24:
                    return e.KeyCode.ToString();
            }

            // Handle letter and number keys
            if (e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z)
            {
                char c = (char)e.KeyCode;
                return (caps ^ shift) ? c.ToString().ToUpper() : c.ToString().ToLower();
            }

            if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)
            {
                return GetNumberOrSymbol(e.KeyCode, shift);
            }

            // Handle symbols
            string symbol = GetShiftedSymbol(e.KeyCode, shift);
            if (!string.IsNullOrEmpty(symbol))
                return symbol;

            return e.KeyCode.ToString(); // fallback
        }

        private string GetNumberOrSymbol(Keys key, bool shift)
        {
            return key switch
            {
                Keys.D0 => shift ? ")" : "0",
                Keys.D1 => shift ? "!" : "1",
                Keys.D2 => shift ? "@" : "2",
                Keys.D3 => shift ? "#" : "3",
                Keys.D4 => shift ? "$" : "4",
                Keys.D5 => shift ? "%" : "5",
                Keys.D6 => shift ? "^" : "6",
                Keys.D7 => shift ? "&" : "7",
                Keys.D8 => shift ? "*" : "8",
                Keys.D9 => shift ? "(" : "9",
                _ => ""
            };
        }

        private string GetShiftedSymbol(Keys key, bool shift)
        {
            var map = new Dictionary<Keys, (char normal, char shifted)>
            {
                { Keys.OemMinus, ('-', '_') },
                { Keys.Oemplus, ('=', '+') },
                { Keys.OemOpenBrackets, ('[', '{') },
                { Keys.Oem6, (']', '}') },
                { Keys.Oem5, ('\\', '|') },
                { Keys.Oem1, (';', ':') },
                { Keys.Oem7, ('\'', '"') },
                { Keys.Oemcomma, (',', '<') },
                { Keys.OemPeriod, ('.', '>') },
                { Keys.OemQuestion, ('/', '?') },
                { Keys.Oemtilde, ('`', '~') },
            };

            return map.TryGetValue(key, out var pair) ? (shift ? pair.shifted.ToString() : pair.normal.ToString()) : "";
        }

        private Point GetRemoteScreenPointFromMouse(Point mousePoint)
        {
            if (screenimg.Image == null) return Point.Empty;

            Image img = screenimg.Image;
            float imageAspect = (float)img.Width / img.Height;
            float boxAspect = (float)screenimg.Width / screenimg.Height;

            int displayedWidth, displayedHeight;
            int offsetX, offsetY;

            if (imageAspect > boxAspect)
            {
                displayedWidth = screenimg.Width;
                displayedHeight = (int)(screenimg.Width / imageAspect);
                offsetX = 0;
                offsetY = (screenimg.Height - displayedHeight) / 2;
            }
            else
            {
                displayedHeight = screenimg.Height;
                displayedWidth = (int)(screenimg.Height * imageAspect);
                offsetX = (screenimg.Width - displayedWidth) / 2;
                offsetY = 0;
            }

            int x = mousePoint.X - offsetX;
            int y = mousePoint.Y - offsetY;

            x = Clamp(x, 0, displayedWidth);
            y = Clamp(y, 0, displayedHeight);

            float scaleX = (float)img.Width / displayedWidth;
            float scaleY = (float)img.Height / displayedHeight;

            int imagePixelX = (int)(x * scaleX);
            int imagePixelY = (int)(y * scaleY);

            int remoteX = imagePixelX * width / img.Width;
            int remoteY = imagePixelY * height / img.Height;

            remoteX = Clamp(remoteX, 0, width - 1);
            remoteY = Clamp(remoteY, 0, height - 1);

            return new Point(remoteX, remoteY);
        }

        private void quality_SelectedIndexChanged(object sender, EventArgs e)
        {
            QualityChanged?.Invoke(quality.Text);
        }

        private void guna2GradientButton1_Click(object sender, EventArgs e)
        {
            pushbox = !pushbox;
            ScreenEvent?.Invoke(pushbox);

            guna2GradientButton1.Text = pushbox ? "Stop" : "Play";
            guna2GradientButton1.Image = pushbox ? global::Sweep.Properties.Resources.pause : global::Sweep.Properties.Resources.play;
        }

        private void ScreenViewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            Closing?.Invoke();
        }

        private void monitors_SelectedIndexChanged(object sender, EventArgs e)
        {
            MonitorChanged?.Invoke(Int32.Parse(monitors.Text));
        }

        public void SetMonitorResolution(int w, int h)
        {
            width = w;
            height = h;
        }
    }
}
