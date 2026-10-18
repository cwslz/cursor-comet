using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("Cursor Comet Setup")]
[assembly: AssemblyProduct("Cursor Comet")]
[assembly: AssemblyDescription("Cursor Comet installer")]
[assembly: AssemblyCompany("cwslz")]
[assembly: AssemblyVersion("1.2.0.0")]
[assembly: AssemblyFileVersion("1.2.0.0")]

namespace CursorCometSetup
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string lang = GetArgValue(args, "--lang");
            if (lang != "zh" && lang != "en")
            {
                lang = AppConfig.ReadLanguage();
            }
            if (lang != "zh" && lang != "en")
            {
                lang = L.SystemDefault();
            }
            L.Set(lang);

            if (HasArg(args, "--install"))
            {
                Environment.ExitCode = Installer.Install(
                    GetArgValue(args, "--dir"),
                    HasArg(args, "--desktop"),
                    HasArg(args, "--autostart"),
                    HasArg(args, "--silent"),
                    HasArg(args, "--launch"));
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
        internal static int Install(string dir, bool desktopShortcut, bool autoStart, bool silent, bool launch)
        {
            try
            {
                if (string.IsNullOrEmpty(dir))
                {
                    dir = AppPaths.DefaultDir;
                }
                dir = Path.GetFullPath(Environment.ExpandEnvironmentVariables(dir.Trim()));
                if (!FsUtil.IsSafeInstallDir(dir))
                {
                    throw new InvalidOperationException(L.T("安装路径不合法，请选择一个普通文件夹。",
                        "Invalid install path. Please choose a normal folder."));
                }

                Directory.CreateDirectory(dir);
                AppProcess.Kill();
                Resources.Extract(AppInfo.AppResource, Path.Combine(dir, AppInfo.Exe));
                Resources.Extract(AppInfo.UninstallResource, Path.Combine(dir, AppInfo.UninstallExe));
                Shortcuts.Create(dir, desktopShortcut);
                InstallRegistry.Write(dir);
                AutoStartRegistry.Set(dir, autoStart);
                AppConfig.WriteLanguage(L.Language);

                if (launch)
                {
                    Process.Start(new ProcessStartInfo(Path.Combine(dir, AppInfo.Exe))
                    {
                        WorkingDirectory = dir,
                        UseShellExecute = true
                    });
                }

                if (!silent)
                {
                    MessageBox.Show(
                        L.T("安装完成。\r\n\r\n安装位置：" + dir +
                            "\r\n\r\n可以从开始菜单启动；在“设置 → 应用 → 已安装的应用”里可以卸载。",
                            "Installation complete.\r\n\r\nLocation: " + dir +
                            "\r\n\r\nLaunch it from the Start menu. Uninstall it from Settings → Apps → Installed apps."),
                        AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return 0;
            }
            catch (Exception ex)
            {
                if (!silent)
                {
                    MessageBox.Show(L.T("安装失败：\r\n", "Installation failed:\r\n") + ex.Message,
                        AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return 1;
            }
        }

        internal static void LaunchUninstaller()
        {
            string dir = InstallRegistry.GetInstalledDir();
            if (string.IsNullOrEmpty(dir))
            {
                dir = AppPaths.DefaultDir;
            }

            string exe = Path.Combine(dir, AppInfo.UninstallExe);
            if (!File.Exists(exe))
            {
                MessageBox.Show(L.T("找不到 uninstall.exe，请重新安装。",
                        "uninstall.exe was not found. Please reinstall."),
                    AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Process.Start(new ProcessStartInfo(exe)
            {
                WorkingDirectory = dir,
                UseShellExecute = true
            });
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
        private readonly Label _subtitle;
        private readonly Label _pathLabel;
        private readonly Button _browse;
        private readonly Button _close;
        private readonly Label _note;
        private readonly Label _languageLabel;
        private readonly ComboBox _languageCombo;
        private bool _applyingLanguage;
        private bool _loaded;

        internal SetupForm()
        {
            Text = "Cursor Comet Setup";
            ClientSize = new Size(820, 600);
            MinimumSize = new Size(640, 500);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font(L.IsChinese ? "Microsoft YaHei UI" : "Segoe UI", 9f);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(22, 18, 22, 18);
            root.ColumnCount = 1;
            root.RowCount = 8;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
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
            title.Text = AppInfo.Name;
            title.Font = new Font(Font.FontFamily, 16f, FontStyle.Bold);
            title.AutoSize = true;
            title.Margin = new Padding(0, 0, 0, 2);
            headerText.Controls.Add(title, 0, 0);

            _subtitle = new Label();
            _subtitle.AutoSize = true;
            _subtitle.ForeColor = Color.Gray;
            _subtitle.Margin = new Padding(0, 0, 0, 0);
            headerText.Controls.Add(_subtitle, 0, 1);

            header.Controls.Add(headerText, 1, 0);
            root.Controls.Add(header, 0, 0);

            FlowLayoutPanel languageRow = new FlowLayoutPanel();
            languageRow.AutoSize = true;
            languageRow.WrapContents = false;
            languageRow.Margin = new Padding(0, 14, 0, 0);

            _languageLabel = new Label();
            _languageLabel.AutoSize = true;
            _languageLabel.Margin = new Padding(0, 6, 10, 0);
            languageRow.Controls.Add(_languageLabel);

            _languageCombo = new ComboBox();
            _languageCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _languageCombo.Width = 150;
            _languageCombo.Items.Add("中文");
            _languageCombo.Items.Add("English");
            _languageCombo.Margin = new Padding(0, 2, 0, 0);
            _languageCombo.SelectedIndexChanged += LanguageChanged;
            languageRow.Controls.Add(_languageCombo);
            root.Controls.Add(languageRow, 0, 1);

            TableLayoutPanel pathRow = new TableLayoutPanel();
            pathRow.AutoSize = true;
            pathRow.Dock = DockStyle.Fill;
            pathRow.ColumnCount = 3;
            pathRow.RowCount = 1;
            pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathRow.Margin = new Padding(0, 16, 0, 0);

            _pathLabel = new Label();
            _pathLabel.AutoSize = true;
            _pathLabel.Anchor = AnchorStyles.Left;
            _pathLabel.Margin = new Padding(0, 6, 12, 0);
            pathRow.Controls.Add(_pathLabel, 0, 0);

            _path = new TextBox();
            _path.Dock = DockStyle.Fill;
            _path.Margin = new Padding(0, 4, 10, 0);
            pathRow.Controls.Add(_path, 1, 0);

            _browse = new Button();
            _browse.AutoSize = true;
            _browse.Margin = new Padding(0, 2, 0, 0);
            _browse.Click += BrowseClicked;
            pathRow.Controls.Add(_browse, 2, 0);
            root.Controls.Add(pathRow, 0, 2);

            FlowLayoutPanel checks = new FlowLayoutPanel();
            checks.FlowDirection = FlowDirection.TopDown;
            checks.AutoSize = true;
            checks.WrapContents = false;
            checks.Margin = new Padding(0, 16, 0, 0);

            _desktop = new CheckBox();
            _desktop.AutoSize = true;
            _desktop.Margin = new Padding(0, 3, 0, 3);
            checks.Controls.Add(_desktop);

            _autoStart = new CheckBox();
            _autoStart.AutoSize = true;
            _autoStart.Margin = new Padding(0, 3, 0, 3);
            checks.Controls.Add(_autoStart);

            _launch = new CheckBox();
            _launch.AutoSize = true;
            _launch.Checked = true;
            _launch.Margin = new Padding(0, 3, 0, 3);
            checks.Controls.Add(_launch);
            root.Controls.Add(checks, 0, 3);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Dock = DockStyle.Fill;
            buttons.AutoSize = true;
            buttons.WrapContents = false;
            buttons.Margin = new Padding(0, 18, 0, 0);

            _close = new Button();
            _close.AutoSize = true;
            _close.MinimumSize = new Size(96, 34);
            _close.Margin = new Padding(6, 0, 0, 0);
            _close.Click += delegate { Close(); };
            buttons.Controls.Add(_close);

            _uninstall = new Button();
            _uninstall.AutoSize = true;
            _uninstall.MinimumSize = new Size(96, 34);
            _uninstall.Margin = new Padding(6, 0, 0, 0);
            _uninstall.Enabled = false;
            _uninstall.Click += UninstallClicked;
            buttons.Controls.Add(_uninstall);

            _install = new Button();
            _install.AutoSize = true;
            _install.MinimumSize = new Size(110, 34);
            _install.Margin = new Padding(6, 0, 0, 0);
            _install.Click += InstallClicked;
            buttons.Controls.Add(_install);
            AcceptButton = _install;
            root.Controls.Add(buttons, 0, 5);

            _status = new Label();
            _status.AutoSize = true;
            _status.MaximumSize = new Size(1400, 0);
            _status.ForeColor = Color.Gray;
            _status.Margin = new Padding(0, 14, 0, 0);
            root.Controls.Add(_status, 0, 6);

            _note = new Label();
            _note.AutoSize = true;
            _note.ForeColor = Color.Gray;
            _note.Font = new Font(Font.FontFamily, 8.5f);
            _note.Margin = new Padding(0, 8, 0, 0);
            root.Controls.Add(_note, 0, 7);

            Controls.Add(root);
            Load += delegate
            {
                ApplyLanguage();
            };
        }

        private void LanguageChanged(object sender, EventArgs e)
        {
            if (_applyingLanguage || !_loaded)
            {
                return;
            }

            L.Set(_languageCombo.SelectedIndex == 1 ? "en" : "zh");
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            _applyingLanguage = true;
            _languageCombo.SelectedIndex = L.IsChinese ? 0 : 1;
            _applyingLanguage = false;
            _loaded = true;

            Text = L.T("Cursor Comet 安装程序", "Cursor Comet Setup");
            _subtitle.Text = L.T("Windows 鼠标拖尾特效 · 安装程序", "Windows cursor trail effect · Setup");
            _languageLabel.Text = "语言 / Language";
            _pathLabel.Text = L.T("安装位置", "Install location");
            _browse.Text = L.T("浏览…", "Browse…");
            _desktop.Text = L.T("创建桌面快捷方式", "Create desktop shortcut");
            _autoStart.Text = L.T("开机自动启动", "Start with Windows");
            _launch.Text = L.T("安装完成后启动", "Launch after install");
            _install.Text = L.T("安装", "Install");
            _uninstall.Text = L.T("卸载", "Uninstall");
            _close.Text = L.T("关闭", "Close");
            _note.Text = L.T("安装完成后可在开始菜单找到 Cursor Comet。",
                "Cursor Comet will appear in the Start menu.");
            RefreshState();
        }

        private void RefreshState()
        {
            string installed = InstallRegistry.GetInstalledDir();
            if (!string.IsNullOrEmpty(installed) && File.Exists(Path.Combine(installed, AppInfo.Exe)))
            {
                _path.Text = installed;
                _status.Text = L.T("已安装到该位置，可以覆盖安装或卸载。",
                    "Already installed here. You can reinstall or uninstall.");
                _uninstall.Enabled = true;
            }
            else
            {
                _path.Text = AppPaths.DefaultDir;
                _status.Text = L.T("默认安装到当前用户目录，不需要管理员权限。",
                    "Installs for the current user; no administrator rights required.");
                _uninstall.Enabled = false;
            }
            _path.SelectionStart = 0;
        }

        private void BrowseClicked(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = L.T("选择 Cursor Comet 的安装位置", "Choose the install location");
                dialog.ShowNewFolderButton = true;
                dialog.SelectedPath = _path.Text;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _path.Text = dialog.SelectedPath;
                    _path.SelectionStart = 0;
                }
            }
        }

        private void InstallClicked(object sender, EventArgs e)
        {
            string dir = _path.Text.Trim();
            if (dir.Length == 0)
            {
                MessageBox.Show(L.T("请先选择安装位置。", "Please choose an install location."),
                    AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    _status.Text = L.T("安装完成：", "Installed to: ") + dir;
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
            Installer.LaunchUninstaller();
            Close();
        }
    }
}
