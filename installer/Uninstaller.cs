using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("Cursor Comet Uninstaller")]
[assembly: AssemblyProduct("Cursor Comet")]
[assembly: AssemblyDescription("Cursor Comet uninstaller")]
[assembly: AssemblyCompany("cwslz")]
[assembly: AssemblyVersion("1.4.2.0")]
[assembly: AssemblyFileVersion("1.4.2.0")]

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

            if (HasArg(args, "--silent"))
            {
                Environment.ExitCode = Uninstaller.Run(true, HasArg(args, "--remove-config"));
                return;
            }

            Application.Run(new UninstallForm());
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

    internal static class Uninstaller
    {
        internal static int Run(bool silent, bool removeConfig)
        {
            string dir = InstallRegistry.GetInstalledDir();
            if (string.IsNullOrEmpty(dir))
            {
                string exe = Application.ExecutablePath;
                if (string.Equals(Path.GetFileName(exe), AppInfo.UninstallExe, StringComparison.OrdinalIgnoreCase))
                {
                    dir = Path.GetDirectoryName(exe);
                }
                else
                {
                    dir = AppPaths.DefaultDir;
                }
            }

            try
            {
                AppProcess.Kill();
                Shortcuts.RemoveAll();
                AutoStartRegistry.Remove();
                InstallRegistry.Remove();
                if (removeConfig)
                {
                    AppConfig.Remove();
                }

                FsUtil.DeleteIfExists(Path.Combine(dir, AppInfo.Exe));
                FsUtil.DeleteIfExists(Path.Combine(dir, AppInfo.UninstallExe));
                FsUtil.TryDeleteEmptyDir(dir);
                FsUtil.ScheduleSelfDelete(dir);
                return 0;
            }
            catch (Exception ex)
            {
                if (!silent)
                {
                    MessageBox.Show(L.T("卸载失败：\r\n", "Uninstall failed:\r\n") + ex.Message,
                        AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return 1;
            }
        }
    }

    internal sealed class UninstallForm : Form
    {
        private readonly Label _subtitle;
        private readonly Label _message;
        private readonly Label _languageLabel;
        private readonly ComboBox _languageCombo;
        private readonly CheckBox _removeConfig;
        private readonly Button _uninstall;
        private readonly Button _cancel;
        private bool _applyingLanguage;
        private bool _loaded;

        internal UninstallForm()
        {
            Text = "Uninstall Cursor Comet";
            ClientSize = new Size(700, 480);
            MinimumSize = new Size(560, 400);
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
            root.RowCount = 7;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
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

            _message = new Label();
            _message.AutoSize = true;
            _message.MaximumSize = new Size(1200, 0);
            _message.Margin = new Padding(0, 18, 0, 0);
            root.Controls.Add(_message, 0, 1);

            FlowLayoutPanel languageRow = new FlowLayoutPanel();
            languageRow.AutoSize = true;
            languageRow.WrapContents = false;
            languageRow.Margin = new Padding(0, 12, 0, 0);

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
            root.Controls.Add(languageRow, 0, 2);

            _removeConfig = new CheckBox();
            _removeConfig.AutoSize = true;
            _removeConfig.Margin = new Padding(0, 14, 0, 0);
            root.Controls.Add(_removeConfig, 0, 3);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Dock = DockStyle.Fill;
            buttons.AutoSize = true;
            buttons.WrapContents = false;
            buttons.Margin = new Padding(0, 18, 0, 0);

            _cancel = new Button();
            _cancel.AutoSize = true;
            _cancel.MinimumSize = new Size(96, 34);
            _cancel.Margin = new Padding(6, 0, 0, 0);
            _cancel.Click += delegate { Close(); };
            buttons.Controls.Add(_cancel);

            _uninstall = new Button();
            _uninstall.AutoSize = true;
            _uninstall.MinimumSize = new Size(120, 34);
            _uninstall.Margin = new Padding(6, 0, 0, 0);
            _uninstall.Click += UninstallClicked;
            buttons.Controls.Add(_uninstall);
            AcceptButton = _uninstall;
            root.Controls.Add(buttons, 0, 5);

            Label note = new Label();
            note.AutoSize = true;
            note.ForeColor = Color.Gray;
            note.Font = new Font(Font.FontFamily, 8.5f);
            note.Margin = new Padding(0, 8, 0, 0);
            note.Text = L.T("卸载完成后，程序文件和快捷方式会被删除。",
                "The app files and shortcuts will be removed.");
            root.Controls.Add(note, 0, 6);

            Controls.Add(root);
            Load += delegate { ApplyLanguage(); };
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

            Text = L.T("卸载 Cursor Comet", "Uninstall Cursor Comet");
            _subtitle.Text = L.T("从这台电脑移除 Cursor Comet", "Remove Cursor Comet from this computer");
            _message.Text = L.T("将卸载 Cursor Comet，并删除程序文件和快捷方式。\r\n配置文件默认保留。",
                "This will uninstall Cursor Comet and remove its files and shortcuts.\r\nYour settings are kept by default.");
            _languageLabel.Text = "语言 / Language";
            _removeConfig.Text = L.T("同时删除配置文件 (%APPDATA%\\MouseTrail)",
                "Also delete settings (%APPDATA%\\MouseTrail)");
            _uninstall.Text = L.T("卸载", "Uninstall");
            _cancel.Text = L.T("取消", "Cancel");
        }

        private void UninstallClicked(object sender, EventArgs e)
        {
            DialogResult answer = MessageBox.Show(
                L.T("确定要卸载 Cursor Comet 吗？", "Uninstall Cursor Comet?"),
                AppInfo.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes)
            {
                return;
            }

            Cursor = Cursors.WaitCursor;
            _uninstall.Enabled = false;
            try
            {
                int result = Uninstaller.Run(false, _removeConfig.Checked);
                if (result == 0)
                {
                    MessageBox.Show(L.T("卸载完成。", "Uninstall complete."),
                        AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Close();
                }
            }
            finally
            {
                Cursor = Cursors.Default;
                _uninstall.Enabled = true;
            }
        }
    }
}
