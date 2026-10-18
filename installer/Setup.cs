using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Cursor Comet Setup")]
[assembly: AssemblyProduct("Cursor Comet")]
[assembly: AssemblyDescription("Cursor Comet installer")]
[assembly: AssemblyCompany("cwslz")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]

namespace CursorCometSetup
{
    internal static class Program
    {
        internal const string AppName = "Cursor Comet";
        internal const string AppVersion = "1.1.0";
        internal const string AppExe = "cursor-comet.exe";
        internal const string UninstallExe = "uninstall.exe";
        internal const string ResourceName = "CursorCometApp.exe";
        internal const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\CursorComet";
        internal const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        internal const string RunValue = "MouseTrail";
        internal const string RepoUrl = "https://github.com/cwslz/cursor-comet";

        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool silent = HasArg(args, "--silent");
            if (HasArg(args, "--uninstall"))
            {
                bool removeConfig = HasArg(args, "--remove-config");
                Environment.ExitCode = Installer.Uninstall(silent, removeConfig);
                return;
            }

            if (HasArg(args, "--install"))
            {
                string dir = GetArgValue(args, "--dir");
                bool desktop = HasArg(args, "--desktop");
                bool autoStart = HasArg(args, "--autostart");
                bool launch = HasArg(args, "--launch");
                Environment.ExitCode = Installer.Install(dir, desktop, autoStart, silent, launch);
                return;
            }

            Application.Run(new SetupForm());
        }

        private static bool HasArg(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static string GetArgValue(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }
            return null;
        }
    }

    internal static class Installer
    {
        internal static string DefaultDir
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Programs\CursorComet");
            }
        }

        internal static string GetInstalledDir()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(Program.UninstallKey, false))
                {
                    if (key != null)
                    {
                        string dir = key.GetValue("InstallLocation") as string;
                        if (!string.IsNullOrEmpty(dir))
                        {
                            return Environment.ExpandEnvironmentVariables(dir);
                        }
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        internal static bool IsInstalled()
        {
            string dir = GetInstalledDir();
            return !string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, Program.AppExe));
        }

        internal static int Install(string dir, bool desktopShortcut, bool autoStart, bool silent, bool launch)
        {
            try
            {
                if (string.IsNullOrEmpty(dir))
                {
                    dir = DefaultDir;
                }
                dir = Path.GetFullPath(Environment.ExpandEnvironmentVariables(dir.Trim()));
                if (!IsSafeInstallDir(dir))
                {
                    throw new InvalidOperationException("安装路径不合法，请选择一个普通文件夹。");
                }

                Directory.CreateDirectory(dir);
                KillRunningApp();
                ExtractApp(dir);
                CreateShortcuts(dir, desktopShortcut);
                WriteUninstallEntry(dir);
                SetAutoStart(dir, autoStart);
                CopyUninstaller(dir);

                if (launch)
                {
                    Process.Start(new ProcessStartInfo(Path.Combine(dir, Program.AppExe))
                    {
                        WorkingDirectory = dir,
                        UseShellExecute = true
                    });
                }

                if (!silent)
                {
                    MessageBox.Show(
                        "安装完成。\r\n\r\n安装位置：" + dir +
                        "\r\n\r\n可以从开始菜单启动；在“设置 → 应用 → 已安装的应用”里可以卸载。",
                        Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return 0;
            }
            catch (Exception ex)
            {
                if (!silent)
                {
                    MessageBox.Show("安装失败：\r\n" + ex.Message, Program.AppName,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return 1;
            }
        }

        internal static int Uninstall(bool silent, bool removeConfig)
        {
            string dir = GetInstalledDir();
            if (string.IsNullOrEmpty(dir))
            {
                dir = DefaultDir;
            }

            if (!silent)
            {
                DialogResult answer = MessageBox.Show(
                    "确定要卸载 " + Program.AppName + " 吗？\r\n\r\n安装位置：" + dir,
                    Program.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                {
                    return 0;
                }
            }

            try
            {
                KillRunningApp();
                RemoveShortcuts();
                RemoveAutoStart();
                RemoveUninstallEntry();
                if (removeConfig)
                {
                    RemoveConfig();
                }

                string appPath = Path.Combine(dir, Program.AppExe);
                string uninstallPath = Path.Combine(dir, Program.UninstallExe);
                DeleteIfExists(appPath);
                DeleteIfExists(uninstallPath);
                TryDeleteEmptyDir(dir);
                ScheduleSelfDelete(dir);

                if (!silent)
                {
                    MessageBox.Show("卸载完成。", Program.AppName,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return 0;
            }
            catch (Exception ex)
            {
                if (!silent)
                {
                    MessageBox.Show("卸载失败：\r\n" + ex.Message, Program.AppName,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return 1;
            }
        }

        private static void ExtractApp(string dir)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Program.ResourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("安装包损坏：找不到主程序资源。");
                }

                string target = Path.Combine(dir, Program.AppExe);
                using (FileStream file = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.CopyTo(file);
                }
            }
        }

        private static void CopyUninstaller(string dir)
        {
            string self = Application.ExecutablePath;
            string target = Path.Combine(dir, Program.UninstallExe);
            if (!string.Equals(Path.GetFullPath(self), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(self, target, true);
            }
        }

        private static void CreateShortcuts(string dir, bool desktopShortcut)
        {
            string target = Path.Combine(dir, Program.AppExe);
            string startMenu = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                Program.AppName + ".lnk");
            CreateShortcut(startMenu, target, dir, "Cursor Comet - 鼠标拖尾特效");

            if (desktopShortcut)
            {
                string desktop = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    Program.AppName + ".lnk");
                CreateShortcut(desktop, target, dir, "Cursor Comet - 鼠标拖尾特效");
            }
        }

        private static void CreateShortcut(string lnkPath, string targetPath, string workingDir, string description)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                throw new InvalidOperationException("无法创建快捷方式：WScript.Shell 不可用。");
            }

            object shell = Activator.CreateInstance(shellType);
            object shortcut = null;
            try
            {
                shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod,
                    null, shell, new object[] { lnkPath });
                Type shortcutType = shortcut.GetType();
                shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null,
                    shortcut, new object[] { targetPath });
                shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null,
                    shortcut, new object[] { workingDir });
                shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty, null,
                    shortcut, new object[] { targetPath + ",0" });
                shortcutType.InvokeMember("Description", BindingFlags.SetProperty, null,
                    shortcut, new object[] { description });
                shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
            }
            finally
            {
                if (shortcut != null)
                {
                    Marshal.ReleaseComObject(shortcut);
                }
                if (shell != null)
                {
                    Marshal.ReleaseComObject(shell);
                }
            }
        }

        private static void WriteUninstallEntry(string dir)
        {
            string exe = Path.Combine(dir, Program.AppExe);
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(Program.UninstallKey))
            {
                key.SetValue("DisplayName", Program.AppName);
                key.SetValue("DisplayVersion", Program.AppVersion);
                key.SetValue("Publisher", "cwslz");
                key.SetValue("DisplayIcon", exe + ",0");
                key.SetValue("InstallLocation", dir);
                key.SetValue("UninstallString",
                    "\"" + Path.Combine(dir, Program.UninstallExe) + "\" --uninstall");
                key.SetValue("QuietUninstallString",
                    "\"" + Path.Combine(dir, Program.UninstallExe) + "\" --uninstall --silent");
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                key.SetValue("URLInfoAbout", Program.RepoUrl);
                key.SetValue("HelpLink", Program.RepoUrl);
                try
                {
                    key.SetValue("EstimatedSize", (int)(new FileInfo(exe).Length / 1024), RegistryValueKind.DWord);
                }
                catch
                {
                }
            }
        }

        private static void SetAutoStart(string dir, bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(Program.RunKey, true))
            {
                if (key == null)
                {
                    return;
                }

                if (enabled)
                {
                    key.SetValue(Program.RunValue, "\"" + Path.Combine(dir, Program.AppExe) + "\"",
                        RegistryValueKind.String);
                }
                else
                {
                    key.DeleteValue(Program.RunValue, false);
                }
            }
        }

        private static void RemoveShortcuts()
        {
            DeleteIfExists(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                Program.AppName + ".lnk"));
            DeleteIfExists(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Program.AppName + ".lnk"));
        }

        private static void RemoveAutoStart()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(Program.RunKey, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(Program.RunValue, false);
                    }
                }
            }
            catch
            {
            }
        }

        private static void RemoveUninstallEntry()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(Program.UninstallKey, false);
            }
            catch
            {
            }
        }

        private static void RemoveConfig()
        {
            try
            {
                string configDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MouseTrail");
                if (Directory.Exists(configDir))
                {
                    Directory.Delete(configDir, true);
                }
            }
            catch
            {
            }
        }

        private static void KillRunningApp()
        {
            string[] names = new string[] { "cursor-comet", "鼠标拖尾" };
            for (int i = 0; i < names.Length; i++)
            {
                Process[] processes = Process.GetProcessesByName(names[i]);
                for (int j = 0; j < processes.Length; j++)
                {
                    try
                    {
                        processes[j].Kill();
                        processes[j].WaitForExit(3000);
                    }
                    catch
                    {
                    }
                    finally
                    {
                        processes[j].Dispose();
                    }
                }
            }
        }

        private static bool IsSafeInstallDir(string dir)
        {
            if (dir.Length < 5)
            {
                return false;
            }

            string root = Path.GetPathRoot(dir);
            if (string.IsNullOrEmpty(root) ||
                string.Equals(dir.TrimEnd('\\'), root.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (!string.IsNullOrEmpty(windows) &&
                dir.StartsWith(windows, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private static void TryDeleteEmptyDir(string dir)
        {
            try
            {
                if (Directory.Exists(dir) && Directory.GetFileSystemEntries(dir).Length == 0)
                {
                    Directory.Delete(dir);
                }
            }
            catch
            {
            }
        }

        private static void ScheduleSelfDelete(string dir)
        {
            try
            {
                string exe = Application.ExecutablePath;
                string selfDir = Path.GetDirectoryName(exe);
                if (string.IsNullOrEmpty(selfDir) ||
                    !string.Equals(Path.GetFullPath(selfDir).TrimEnd('\\'),
                        Path.GetFullPath(dir).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                string command = "/c ping 127.0.0.1 -n 3 >nul & del /f /q \"" + exe +
                                 "\" & rd /q \"" + dir + "\"";
                Process.Start(new ProcessStartInfo("cmd.exe", command)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
            catch
            {
            }
        }

        private static void DeleteIfExists(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }
    }

    internal sealed class SetupForm : Form
    {
        private readonly TextBox _path;
        private readonly CheckBox _desktop;
        private readonly CheckBox _autoStart;
        private readonly CheckBox _launch;
        private readonly Button _install;
        private readonly Button _uninstall;
        private readonly Label _status;

        internal SetupForm()
        {
            Text = "Cursor Comet 安装程序";
            ClientSize = new Size(820, 560);
            MinimumSize = new Size(640, 460);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Microsoft YaHei UI", 9f);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(22, 18, 22, 18);
            root.ColumnCount = 1;
            root.RowCount = 7;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            TableLayoutPanel header = new TableLayoutPanel();
            header.AutoSize = true;
            header.Dock = DockStyle.Fill;
            header.ColumnCount = 2;
            header.RowCount = 1;
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            try
            {
                Icon appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (appIcon != null)
                {
                    Icon = appIcon;
                    PictureBox picture = new PictureBox();
                    picture.Image = appIcon.ToBitmap();
                    picture.Size = new Size(56, 56);
                    picture.SizeMode = PictureBoxSizeMode.StretchImage;
                    picture.Margin = new Padding(0, 0, 16, 0);
                    header.Controls.Add(picture, 0, 0);
                }
            }
            catch
            {
            }

            TableLayoutPanel headerText = new TableLayoutPanel();
            headerText.AutoSize = true;
            headerText.Dock = DockStyle.Fill;
            headerText.ColumnCount = 1;
            headerText.RowCount = 2;
            headerText.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            headerText.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            headerText.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label title = new Label();
            title.Text = "Cursor Comet";
            title.Font = new Font(Font.FontFamily, 16f, FontStyle.Bold);
            title.AutoSize = true;
            title.Margin = new Padding(0, 0, 0, 2);
            headerText.Controls.Add(title, 0, 0);

            Label subtitle = new Label();
            subtitle.Text = "Windows 鼠标拖尾特效 · 安装程序";
            subtitle.ForeColor = Color.Gray;
            subtitle.AutoSize = true;
            subtitle.Margin = new Padding(0, 0, 0, 0);
            headerText.Controls.Add(subtitle, 0, 1);

            header.Controls.Add(headerText, 1, 0);
            root.Controls.Add(header, 0, 0);

            TableLayoutPanel pathRow = new TableLayoutPanel();
            pathRow.AutoSize = true;
            pathRow.Dock = DockStyle.Fill;
            pathRow.ColumnCount = 3;
            pathRow.RowCount = 1;
            pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathRow.Margin = new Padding(0, 18, 0, 0);

            Label pathLabel = new Label();
            pathLabel.Text = "安装位置";
            pathLabel.AutoSize = true;
            pathLabel.Anchor = AnchorStyles.Left;
            pathLabel.Margin = new Padding(0, 6, 12, 0);
            pathRow.Controls.Add(pathLabel, 0, 0);

            _path = new TextBox();
            _path.Dock = DockStyle.Fill;
            _path.Margin = new Padding(0, 4, 10, 0);
            pathRow.Controls.Add(_path, 1, 0);

            Button browse = new Button();
            browse.Text = "浏览…";
            browse.AutoSize = true;
            browse.Margin = new Padding(0, 2, 0, 0);
            browse.Click += BrowseClicked;
            pathRow.Controls.Add(browse, 2, 0);

            root.Controls.Add(pathRow, 0, 1);

            FlowLayoutPanel checks = new FlowLayoutPanel();
            checks.FlowDirection = FlowDirection.TopDown;
            checks.AutoSize = true;
            checks.WrapContents = false;
            checks.Margin = new Padding(0, 16, 0, 0);

            _desktop = new CheckBox();
            _desktop.Text = "创建桌面快捷方式";
            _desktop.AutoSize = true;
            _desktop.Margin = new Padding(0, 3, 0, 3);
            checks.Controls.Add(_desktop);

            _autoStart = new CheckBox();
            _autoStart.Text = "开机自动启动";
            _autoStart.AutoSize = true;
            _autoStart.Margin = new Padding(0, 3, 0, 3);
            checks.Controls.Add(_autoStart);

            _launch = new CheckBox();
            _launch.Text = "安装完成后启动";
            _launch.AutoSize = true;
            _launch.Checked = true;
            _launch.Margin = new Padding(0, 3, 0, 3);
            checks.Controls.Add(_launch);

            root.Controls.Add(checks, 0, 2);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Dock = DockStyle.Fill;
            buttons.AutoSize = true;
            buttons.WrapContents = false;
            buttons.Margin = new Padding(0, 18, 0, 0);

            Button close = new Button();
            close.Text = "关闭";
            close.AutoSize = true;
            close.MinimumSize = new Size(96, 34);
            close.Margin = new Padding(6, 0, 0, 0);
            close.Click += delegate { Close(); };
            buttons.Controls.Add(close);

            _uninstall = new Button();
            _uninstall.Text = "卸载";
            _uninstall.AutoSize = true;
            _uninstall.MinimumSize = new Size(96, 34);
            _uninstall.Margin = new Padding(6, 0, 0, 0);
            _uninstall.Click += UninstallClicked;
            _uninstall.Enabled = false;
            buttons.Controls.Add(_uninstall);

            _install = new Button();
            _install.Text = "安装";
            _install.AutoSize = true;
            _install.MinimumSize = new Size(110, 34);
            _install.Margin = new Padding(6, 0, 0, 0);
            _install.Click += InstallClicked;
            buttons.Controls.Add(_install);
            AcceptButton = _install;

            root.Controls.Add(buttons, 0, 4);

            _status = new Label();
            _status.AutoSize = true;
            _status.MaximumSize = new Size(1200, 0);
            _status.ForeColor = Color.Gray;
            _status.Margin = new Padding(0, 14, 0, 0);
            root.Controls.Add(_status, 0, 5);

            Label note = new Label();
            note.Text = "安装完成后可在开始菜单找到 Cursor Comet。";
            note.ForeColor = Color.Gray;
            note.Font = new Font(Font.FontFamily, 8.5f);
            note.AutoSize = true;
            note.Margin = new Padding(0, 8, 0, 0);
            root.Controls.Add(note, 0, 6);

            Controls.Add(root);
            Load += delegate { RefreshState(); };
        }

        private void RefreshState()
        {
            string installed = Installer.GetInstalledDir();
            if (!string.IsNullOrEmpty(installed) && Directory.Exists(installed))
            {
                _path.Text = installed; _path.SelectionStart = 0;
                _status.Text = "已安装到该位置，可以覆盖安装或卸载。";
                _uninstall.Enabled = true;
            }
            else
            {
                _path.Text = Installer.DefaultDir; _path.SelectionStart = 0;
                _status.Text = "默认安装到当前用户目录，不需要管理员权限。";
                _uninstall.Enabled = false;
            }
        }

        private void BrowseClicked(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "选择 Cursor Comet 的安装位置";
                dialog.ShowNewFolderButton = true;
                dialog.SelectedPath = _path.Text;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _path.Text = dialog.SelectedPath; _path.SelectionStart = 0;
                }
            }
        }

        private void InstallClicked(object sender, EventArgs e)
        {
            string dir = _path.Text.Trim();
            if (dir.Length == 0)
            {
                MessageBox.Show("请先选择安装位置。", Program.AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Cursor = Cursors.WaitCursor;
            _install.Enabled = false;
            _uninstall.Enabled = false;
            try
            {
                int result = Installer.Install(dir, _desktop.Checked, _autoStart.Checked, false, _launch.Checked);
                if (result == 0)
                {
                    _status.Text = "安装完成：" + dir;
                    _uninstall.Enabled = true;
                }
            }
            finally
            {
                Cursor = Cursors.Default;
                _install.Enabled = true;
            }
        }

        private void UninstallClicked(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            _install.Enabled = false;
            _uninstall.Enabled = false;
            try
            {
                int result = Installer.Uninstall(false, false);
                if (result == 0)
                {
                    _status.Text = "已卸载。";
                    _uninstall.Enabled = false;
                }
            }
            finally
            {
                Cursor = Cursors.Default;
                _install.Enabled = true;
            }
        }
    }
}
