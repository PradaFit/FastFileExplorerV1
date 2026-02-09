using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Fastest_FileExplorer
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            try
            {
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                HandleException(ex, true);
            }
        }

        private static void CurrentDomain_FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            if (IsIgnoredException(e.Exception))
            {
                return;
            }
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            
            if (e.Exception?.InnerException != null)
            {
                if (IsIgnoredException(e.Exception.InnerException))
                {
                    return;
                }
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleException(e.Exception, false);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                HandleException(ex, e.IsTerminating);
            }
        }

        private static bool IsIgnoredException(Exception ex)
        {
            return ex is UnauthorizedAccessException ||
                   ex is System.IO.DirectoryNotFoundException ||
                   ex is System.IO.FileNotFoundException ||
                   ex is System.IO.PathTooLongException ||
                   ex is System.IO.IOException ||
                   ex is OperationCanceledException ||
                   ex is TaskCanceledException ||
                   ex is ObjectDisposedException ||
                   ex is InvalidOperationException;
        }

        private static void HandleException(Exception ex, bool isFatal)
        {
            if (IsIgnoredException(ex))
            {
                return;
            }

            try
            {
                var message = isFatal
                    ? $"A fatal error occurred: {ex?.Message ?? "Unknown error"}\n\nThe application will now close."
                    : $"An error occurred: {ex?.Message ?? "Unknown error"}";

                MessageBox.Show(
                    message,
                    isFatal ? "Fatal Error" : "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch { }
        }
    }
}
