using System;

namespace MouseTrail
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool createdNew;
            using (System.Threading.Mutex mutex = new System.Threading.Mutex(true, "MouseTrail_SingleInstance_7C1E", out createdNew))
            {
                if (!createdNew)
                {
                    System.Windows.Forms.MessageBox.Show(
                        "鼠标拖尾已经在运行了，请查看屏幕右下角的托盘图标。",
                        "鼠标拖尾",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Information);
                    return;
                }

                System.Windows.Forms.Application.EnableVisualStyles();
                System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

                EnableDpiAwareness();
                Native.timeBeginPeriod(1);

                System.Windows.Forms.Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs e)
                {
                    Log(e.Exception);
                };
                AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
                {
                    Log(e.ExceptionObject as Exception);
                };

                try
                {
                    AppSettings settings = AppSettings.Load();
                    AutoStart.RepairIfNeeded();
                    using (TrailForm form = new TrailForm(settings))
                    {
                        System.Windows.Forms.Application.Run(form);
                    }
                }
                catch (Exception ex)
                {
                    Log(ex);
                    System.Windows.Forms.MessageBox.Show(
                        "启动失败：" + ex.Message,
                        "鼠标拖尾",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Error);
                }
                finally
                {
                    Native.timeEndPeriod(1);
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
