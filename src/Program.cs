using System;
using System.Threading;
using System.Windows.Forms;

namespace CursorComet
{
    internal static class Program
    {
        private const string MutexName = "CursorComet_SingleInstance_7C1E";
        private const string OpenEventName = "CursorComet_OpenSettings";

        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool openSettings = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--settings", StringComparison.OrdinalIgnoreCase))
                {
                    openSettings = true;
                }
            }

            AppSettings settings = AppSettings.Load();
            L10n.Set(settings.ResolveLanguage());

            bool createdNew;
            using (Mutex mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    try
                    {
                        using (EventWaitHandle ev = EventWaitHandle.OpenExisting(OpenEventName))
                        {
                            ev.Set();
                        }
                    }
                    catch
                    {
                        MessageBox.Show(
                            L10n.T("鼠标拖尾已经在运行了，请查看屏幕右下角的托盘图标。",
                                "Cursor Comet is already running. Check the tray icon near the clock."),
                            L10n.T("鼠标拖尾", "Cursor Comet"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    return;
                }

                EventWaitHandle openEvent = new EventWaitHandle(
                    false, EventResetMode.AutoReset, OpenEventName);

                EnableDpiAwareness();

                Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
                {
                    Log(e.Exception);
                };
                AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
                {
                    Log(e.ExceptionObject as Exception);
                };

                try
                {
                    AutoStart.RepairIfNeeded();
                    using (TrailForm form = new TrailForm(settings, openSettings))
                    {
                        Thread waiter = new Thread(delegate()
                        {
                            while (true)
                            {
                                if (openEvent.WaitOne())
                                {
                                    try
                                    {
                                        form.BeginInvoke(new MethodInvoker(form.OpenMainFromSignal));
                                    }
                                    catch
                                    {
                                    }
                                }
                            }
                        });
                        waiter.IsBackground = true;
                        waiter.Name = "CursorComet.OpenSettings";
                        waiter.Start();

                        Application.Run(form);
                    }
                }
                catch (Exception ex)
                {
                    Log(ex);
                    MessageBox.Show(
                        L10n.T("启动失败：", "Failed to start: ") + ex.Message,
                        L10n.T("鼠标拖尾", "Cursor Comet"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                finally
                {
                    try
                    {
                        mutex.ReleaseMutex();
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static void EnableDpiAwareness()
        {
            try
            {
                if (Native.SetProcessDpiAwarenessContext(new IntPtr(-4)))
                {
                    return;
                }
            }
            catch
            {
            }

            try
            {
                Native.SetProcessDPIAware();
            }
            catch
            {
            }
        }

        public static void Log(Exception ex)
        {
            if (ex == null)
            {
                return;
            }

            try
            {
                System.IO.Directory.CreateDirectory(AppSettings.Folder);
                string path = System.IO.Path.Combine(AppSettings.Folder, "error.log");
                System.IO.File.AppendAllText(path,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + ex + Environment.NewLine + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
