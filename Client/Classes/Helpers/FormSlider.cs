using System;
using System.Drawing;
using System.Windows.Forms;

public static class FormSlider
{
    public static void ShowSliding(Form form, int speed = 10, int interval = 10, int timeout = 0)
    {
        Rectangle screen = Screen.PrimaryScreen.WorkingArea;
        int targetX = screen.Right - form.Width;
        int targetY = screen.Bottom - form.Height;

        // Set initial position off-screen (below the screen)
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(targetX, screen.Bottom);
        form.TopMost = true;
        form.ShowInTaskbar = false;

        form.Load += (s, e) =>
        {
            Timer slideIn = new Timer { Interval = interval };
            slideIn.Tick += (ts, te) =>
            {
                if (form.Top > targetY)
                {
                    form.Top -= speed;
                    if (form.Top < targetY) form.Top = targetY;
                }
                else
                {
                    slideIn.Stop();

                    // Start auto-close if timeout > 0
                    if (timeout > 0)
                    {
                        Timer delay = new Timer { Interval = timeout };
                        delay.Tick += (ds, de) =>
                        {
                            delay.Stop();
                            SlideOut(form, speed, interval);
                        };
                        delay.Start();
                    }
                }
            };
            slideIn.Start();
        };

        // Start message loop with the form
        form.Show();
    }

    public static void SlideOut(Form form, int speed = 10, int interval = 10)
    {
        Timer slideOut = new Timer { Interval = interval };
        int screenBottom = Screen.PrimaryScreen.WorkingArea.Bottom;

        slideOut.Tick += (s, e) =>
        {
            if (form.Top < screenBottom)
            {
                form.Top += speed;
            }
            else
            {
                slideOut.Stop();
                form.Close();
            }
        };

        slideOut.Start();
    }
}
