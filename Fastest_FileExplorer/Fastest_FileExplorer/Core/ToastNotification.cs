using System;
using System.Drawing;
using System.Windows.Forms;

namespace Fastest_FileExplorer.Core
{
    internal static class ToastNotification
    {
        public static void ShowNotification(string title, string message)
        {
            try
            {
                var notifyIcon = new NotifyIcon
                {
                    Visible = true,
                    Icon = SystemIcons.Information,
                    BalloonTipTitle = title ?? "Fastest File Explorer",
                    BalloonTipText = message ?? "",
                    BalloonTipIcon = ToolTipIcon.Info
                };

                notifyIcon.ShowBalloonTip(3000);

                var timer = new Timer { Interval = 4000 };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    notifyIcon.Visible = false;
                    notifyIcon.Dispose();
                    timer.Dispose();
                };
                timer.Start();
            }
            catch { }
        }
    }
}