using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("Cursor Comet Setup")]
[assembly: AssemblyProduct("Cursor Comet")]
[assembly: AssemblyDescription("Cursor Comet installer")]
[assembly: AssemblyCompany("cwslz")]
[assembly: AssemblyVersion("1.6.0.0")]
[assembly: AssemblyFileVersion("1.6.0.0")]

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
                    HasArg(args, "--launch"),
                    null);
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
        internal static int Install(string dir, bool desktopShortcut, bool autoStart, bool silent, bool launch,
            Action<int, string> progress)
        {
            try
            {
                Report(progress, 3, L.T("正在准备安装…", "Preparing…"));

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

                Report(progress, 10, L.T("正在创建安装目录…", "Creating install folder…"));
                Directory.CreateDirectory(dir);
                AppProcess.Kill();

                Report(progress, 30, L.T("正在复制主程序…", "Copying the app…"));
                Resources.Extract(AppInfo.AppResource, Path.Combine(dir, AppInfo.Exe));

                Report(progress, 50, L.T("正在复制卸载器…", "Copying the uninstaller…"));
                Resources.Extract(AppInfo.UninstallResource, Path.Combine(dir, AppInfo.UninstallExe));

                Report(progress, 65, L.T("正在创建快捷方式…", "Creating shortcuts…"));
                Shortcuts.Create(dir, desktopShortcut);

                Report(progress, 80, L.T("正在写入卸载信息…", "Writing the uninstall entry…"));
                InstallRegistry.Write(dir);

                Report(progress, 90, L.T("正在保存设置…", "Saving settings…"));
                AutoStartRegistry.Set(dir, autoStart);
                AppConfig.WriteLanguage(L.Language);

                Report(progress, 97, L.T("正在完成…", "Finishing…"));
                if (launch)
                {
                    Process.Start(new ProcessStartInfo(Path.Combine(dir, AppInfo.Exe))
                    {
                        WorkingDirectory = dir,
                        UseShellExecute = true,
                        Arguments = "--settings"
                    });
                }

                Report(progress, 100, L.T("安装完成", "Installation complete"));

                if (!silent)
                {
                    MessageBox.Show(
                        L.T("安装完成。\r\n\r\n安装位置：" + dir +
                            "\r\n\r\n可以从开始菜单启动；卸载请使用“设置 → 应用”或安装目录里的 uninstall.exe。",
                            "Installation complete.\r\n\r\nLocation: " + dir +
                            "\r\n\r\nLaunch it from the Start menu. To uninstall, use Settings → Apps or uninstall.exe in the install folder."),
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

        private static void Report(Action<int, string> progress, int percent, string status)
        {
            if (progress != null)
            {
                progress(percent, status);
            }
        }
    }

    internal sealed class SetupForm : Form
    {
        private Panel _content;

        private Panel _languagePage;
        private Label _languagePrompt;
        private ComboBox _languageCombo;
        private Label _languageNote;

        private Panel _licensePage;
        private Label _licenseTitle;
        private TextBox _licenseText;
        private CheckBox _agree;

        private Panel _locationPage;
        private Label _locationTitle;
        private Label _pathLabel;
        private TextBox _path;
        private Button _browse;
        private Label _spaceRequired;
        private Label _spaceAvailable;
        private CheckBox _desktop;
        private CheckBox _autoStart;

        private Panel _progressPage;
        private Label _progressTitle;
        private Label _progressText;
        private ProgressBar _progress;
        private Label _progressStatus;

        private Panel _completionPage;
        private Label _completionTitle;
        private Label _completionText;
        private CheckBox _run;

        private Button _back;
        private Button _next;
        private Button _cancel;

        private int _page;
        private bool _applyingLanguage;
        private bool _installing;
        private string _installedDir;

        internal SetupForm()
        {
            Text = "Cursor Comet Setup";
            AutoScaleMode = AutoScaleMode.None;
            Size = new Size(576, 302);
            MinimumSize = new Size(520, 290);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.Manual;
            Font = new Font(L.IsChinese ? "Microsoft YaHei UI" : "Segoe UI", 8.5f);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(10, 8, 10, 8);
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 1f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _content = new Panel();
            _content.Dock = DockStyle.Fill;
            _content.Margin = new Padding(0, 0, 0, 6);
            root.Controls.Add(_content, 0, 0);

            BuildLanguagePage();
            BuildLicensePage();
            BuildLocationPage();
            BuildProgressPage();
            BuildCompletionPage();
            _content.Controls.Add(_languagePage);
            _content.Controls.Add(_licensePage);
            _content.Controls.Add(_locationPage);
            _content.Controls.Add(_progressPage);
            _content.Controls.Add(_completionPage);

            Panel footerLine = new Panel();
            footerLine.Dock = DockStyle.Top;
            footerLine.Height = 1;
            footerLine.BackColor = Color.FromArgb(200, 200, 200);
            footerLine.Margin = new Padding(0, 0, 0, 4);
            root.Controls.Add(footerLine, 0, 1);

            BuildFooter(root);
            Controls.Add(root);

            string installedDir = InstallRegistry.GetInstalledDir();
            _path.Text = !string.IsNullOrEmpty(installedDir) && File.Exists(Path.Combine(installedDir, AppInfo.Exe))
                ? installedDir
                : AppPaths.DefaultDir;
            _path.TextChanged += delegate { UpdateSpace(); };
            UpdateSpace();

            Load += delegate
            {
                CenterOnScreen();
                ApplyLanguage();
                ShowPage(0);
            };
            FormClosing += delegate(object sender, FormClosingEventArgs e)
            {
                if (_installing)
                {
                    e.Cancel = true;
                }
            };
        }

        private Panel NewPage()
        {
            Panel page = new Panel();
            page.Dock = DockStyle.Fill;
            page.Visible = false;
            return page;
        }

        private static TableLayoutPanel NewLayout(int rows, int percentRow)
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = rows;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int i = 0; i < rows; i++)
            {
                layout.RowStyles.Add(i == percentRow
                    ? new RowStyle(SizeType.Percent, 100f)
                    : new RowStyle(SizeType.AutoSize));
            }
            return layout;
        }

        private Label NewTitle()
        {
            Label title = new Label();
            title.AutoSize = true;
            title.MaximumSize = new Size(540, 0);
            title.Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold);
            title.Margin = new Padding(0, 2, 0, 5);
            return title;
        }

        private void BuildLanguagePage()
        {
            _languagePage = NewPage();
            TableLayoutPanel layout = NewLayout(4, 3);

            TableLayoutPanel top = new TableLayoutPanel();
            top.AutoSize = true;
            top.Dock = DockStyle.Top;
            top.ColumnCount = 2;
            top.RowCount = 1;
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            top.Margin = new Padding(0, 4, 0, 6);

            try
            {
                Icon appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (appIcon != null)
                {
                    Icon = appIcon;
                    PictureBox picture = new PictureBox();
                    picture.Image = appIcon.ToBitmap();
                    picture.Size = new Size(48, 48);
                    picture.SizeMode = PictureBoxSizeMode.StretchImage;
                    picture.Margin = new Padding(0, 0, 10, 0);
                    top.Controls.Add(picture, 0, 0);
                }
            }
            catch
            {
            }

            _languagePrompt = new Label();
            _languagePrompt.AutoSize = true;
            _languagePrompt.Anchor = AnchorStyles.Left;
            _languagePrompt.Font = new Font(Font.FontFamily, 11f, FontStyle.Bold);
            _languagePrompt.Margin = new Padding(0, 10, 0, 0);
            top.Controls.Add(_languagePrompt, 1, 0);
            layout.Controls.Add(top, 0, 0);

            _languageCombo = new ComboBox();
            _languageCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _languageCombo.Width = 300;
            _languageCombo.Margin = new Padding(0, 0, 0, 6);
            _languageCombo.Items.Add("简体中文");
            _languageCombo.Items.Add("English");
            _languageCombo.SelectedIndexChanged += LanguageChanged;
            layout.Controls.Add(_languageCombo, 0, 1);

            _languageNote = new Label();
            _languageNote.AutoSize = true;
            _languageNote.ForeColor = Color.Gray;
            _languageNote.Font = new Font(Font.FontFamily, 8f);
            _languageNote.MaximumSize = new Size(540, 0);
            _languageNote.Margin = new Padding(0, 0, 0, 0);
            layout.Controls.Add(_languageNote, 0, 2);

            _languagePage.Controls.Add(layout);
        }

        private void BuildLicensePage()
        {
            _licensePage = NewPage();
            TableLayoutPanel layout = NewLayout(3, 1);

            _licenseTitle = NewTitle();
            layout.Controls.Add(_licenseTitle, 0, 0);

            _licenseText = new TextBox();
            _licenseText.Multiline = true;
            _licenseText.ReadOnly = true;
            _licenseText.ScrollBars = ScrollBars.Vertical;
            _licenseText.WordWrap = true;
            _licenseText.Dock = DockStyle.Fill;
            _licenseText.BorderStyle = BorderStyle.FixedSingle;
            _licenseText.BackColor = Color.White;
            _licenseText.Font = new Font(Font.FontFamily, 8f);
            _licenseText.Margin = new Padding(0, 0, 0, 4);
            layout.Controls.Add(_licenseText, 0, 1);

            _agree = new CheckBox();
            _agree.AutoSize = true;
            _agree.Margin = new Padding(0, 1, 0, 0);
            _agree.CheckedChanged += delegate { UpdateNextState(); };
            layout.Controls.Add(_agree, 0, 2);

            _licensePage.Controls.Add(layout);
        }

        private void BuildLocationPage()
        {
            _locationPage = NewPage();
            TableLayoutPanel layout = NewLayout(5, 4);

            _locationTitle = NewTitle();
            layout.Controls.Add(_locationTitle, 0, 0);

            TableLayoutPanel pathRow = new TableLayoutPanel();
            pathRow.AutoSize = true;
            pathRow.Dock = DockStyle.Fill;
            pathRow.ColumnCount = 3;
            pathRow.RowCount = 1;
            pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathRow.Margin = new Padding(0, 0, 0, 3);

            _pathLabel = new Label();
            _pathLabel.AutoSize = true;
            _pathLabel.Anchor = AnchorStyles.Left;
            _pathLabel.Margin = new Padding(0, 5, 8, 0);
            pathRow.Controls.Add(_pathLabel, 0, 0);

            _path = new TextBox();
            _path.Dock = DockStyle.Fill;
            _path.Margin = new Padding(0, 3, 6, 0);
            pathRow.Controls.Add(_path, 1, 0);

            _browse = new Button();
            _browse.AutoSize = true;
            _browse.Margin = new Padding(0, 1, 0, 0);
            _browse.Click += BrowseClicked;
            pathRow.Controls.Add(_browse, 2, 0);
            layout.Controls.Add(pathRow, 0, 1);

            FlowLayoutPanel spaceRow = new FlowLayoutPanel();
            spaceRow.AutoSize = true;
            spaceRow.WrapContents = true;
            spaceRow.Margin = new Padding(0, 2, 0, 6);

            _spaceRequired = new Label();
            _spaceRequired.AutoSize = true;
            _spaceRequired.ForeColor = Color.Gray;
            _spaceRequired.Font = new Font(Font.FontFamily, 8f);
            _spaceRequired.Margin = new Padding(0, 0, 16, 0);
            spaceRow.Controls.Add(_spaceRequired);

            _spaceAvailable = new Label();
            _spaceAvailable.AutoSize = true;
            _spaceAvailable.ForeColor = Color.Gray;
            _spaceAvailable.Font = new Font(Font.FontFamily, 8f);
            _spaceAvailable.Margin = new Padding(0, 0, 0, 0);
            spaceRow.Controls.Add(_spaceAvailable);
            layout.Controls.Add(spaceRow, 0, 2);

            FlowLayoutPanel options = new FlowLayoutPanel();
            options.FlowDirection = FlowDirection.LeftToRight;
            options.AutoSize = true;
            options.WrapContents = false;
            options.Margin = new Padding(0, 2, 0, 2);

            _desktop = new CheckBox();
            _desktop.AutoSize = true;
            _desktop.Margin = new Padding(0, 2, 20, 2);
            options.Controls.Add(_desktop);

            _autoStart = new CheckBox();
            _autoStart.AutoSize = true;
            _autoStart.Margin = new Padding(0, 2, 0, 2);
            options.Controls.Add(_autoStart);
            layout.Controls.Add(options, 0, 3);

            _locationPage.Controls.Add(layout);
        }

        private void BuildProgressPage()
        {
            _progressPage = NewPage();
            TableLayoutPanel layout = NewLayout(5, 4);

            _progressTitle = NewTitle();
            layout.Controls.Add(_progressTitle, 0, 0);

            _progressText = new Label();
            _progressText.AutoSize = true;
            _progressText.MaximumSize = new Size(540, 0);
            _progressText.Margin = new Padding(0, 0, 0, 8);
            layout.Controls.Add(_progressText, 0, 1);

            _progress = new ProgressBar();
            _progress.Minimum = 0;
            _progress.Maximum = 100;
            _progress.Value = 0;
            _progress.Height = 14;
            _progress.Dock = DockStyle.Top;
            _progress.Margin = new Padding(0, 0, 0, 6);
            layout.Controls.Add(_progress, 0, 2);

            _progressStatus = new Label();
            _progressStatus.AutoSize = true;
            _progressStatus.Margin = new Padding(0, 0, 0, 0);
            layout.Controls.Add(_progressStatus, 0, 3);

            _progressPage.Controls.Add(layout);
        }

        private void BuildCompletionPage()
        {
            _completionPage = NewPage();
            TableLayoutPanel layout = NewLayout(4, 3);

            _completionTitle = NewTitle();
            layout.Controls.Add(_completionTitle, 0, 0);

            _completionText = new Label();
            _completionText.AutoSize = true;
            _completionText.MaximumSize = new Size(540, 0);
            _completionText.Margin = new Padding(0, 0, 0, 8);
            layout.Controls.Add(_completionText, 0, 1);

            _run = new CheckBox();
            _run.AutoSize = true;
            _run.Checked = true;
            _run.Margin = new Padding(0, 2, 0, 2);
            layout.Controls.Add(_run, 0, 2);

            _completionPage.Controls.Add(layout);
        }

        private void BuildFooter(TableLayoutPanel root)
        {
            FlowLayoutPanel footer = new FlowLayoutPanel();
            footer.FlowDirection = FlowDirection.RightToLeft;
            footer.Dock = DockStyle.Fill;
            footer.AutoSize = true;
            footer.WrapContents = false;
            footer.Margin = new Padding(0, 4, 0, 0);

            _cancel = MakeButton("取消");
            _cancel.Click += delegate
            {
                if (!_installing)
                {
                    Close();
                }
            };
            footer.Controls.Add(_cancel);

            _next = MakeButton("下一步");
            _next.Click += NextClicked;
            footer.Controls.Add(_next);

            _back = MakeButton("上一步");
            _back.Visible = false;
            _back.Click += BackClicked;
            footer.Controls.Add(_back);

            root.Controls.Add(footer, 0, 2);
        }

        private Button MakeButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.AutoSize = true;
            button.MinimumSize = new Size(76, 26);
            button.Margin = new Padding(4, 0, 0, 0);
            return button;
        }

        private void LanguageChanged(object sender, EventArgs e)
        {
            if (_applyingLanguage)
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

            Text = L.T("Cursor Comet 安装向导", "Cursor Comet Setup Wizard");
            _languagePrompt.Text = L.T("请选择安装向导语言", "Please select the setup language");
            _languageNote.Text = L.T("安装程序和应用都会使用该语言。",
                "The installer and the app will use this language.");

            _licenseTitle.Text = L.T("许可协议", "License Agreement");
            _licenseText.Text = L.T(AgreementZh(), AgreementEn());
            _agree.Text = L.T("我接受许可协议中的条款", "I accept the terms in the License Agreement");

            _locationTitle.Text = L.T("选择安装位置", "Choose Install Location");
            _pathLabel.Text = L.T("目标文件夹", "Destination Folder");
            _browse.Text = L.T("浏览…", "Browse…");
            _desktop.Text = L.T("创建桌面快捷方式", "Create desktop shortcut");
            _autoStart.Text = L.T("开机自动启动", "Start with Windows");

            _progressTitle.Text = L.T("正在安装", "Installing");
            _progressText.Text = L.T("正在安装 Cursor Comet，请稍候。",
                "Please wait while Cursor Comet is being installed.");
            if (!_installing)
            {
                _progressStatus.Text = L.T("准备安装…", "Ready to install.");
            }

            _completionTitle.Text = L.T("正在完成 Cursor Comet 安装向导",
                "Completing the Cursor Comet Setup Wizard");
            _completionText.Text = L.T("Cursor Comet 已安装到你的电脑。\r\n点击“完成”关闭向导。",
                "Cursor Comet has been installed on your computer.\r\nClick Finish to close this wizard.");
            _run.Text = L.T("启动 Cursor Comet", "Launch Cursor Comet");

            _back.Text = L.T("上一步", "Back");
            _cancel.Text = L.T("取消", "Cancel");

            UpdateSpace();
            ShowPage(_page);
        }

        private static string AgreementZh()
        {
            return "Cursor Comet 是自由软件，按 GNU General Public License v3.0（GPL-3.0）发布。\r\n\r\n" +
                   "你可以自由使用、修改和分发本软件；分发修改后的版本时，需要继续以 GPL-3.0 提供源代码。\r\n\r\n" +
                   "本软件不提供任何担保。完整协议：\r\n" +
                   "https://www.gnu.org/licenses/gpl-3.0.html\r\n\r\n" +
                   "继续安装表示你已阅读并同意以上条款。";
        }

        private static string AgreementEn()
        {
            return "Cursor Comet is free software licensed under the GNU General Public License v3.0 (GPL-3.0).\r\n\r\n" +
                   "You may use, modify and distribute it freely; if you distribute a modified version, you must provide the source under GPL-3.0 as well.\r\n\r\n" +
                   "This software comes with NO WARRANTY. Full text:\r\n" +
                   "https://www.gnu.org/licenses/gpl-3.0.html\r\n\r\n" +
                   "By continuing, you confirm that you have read and accept these terms.";
        }

        private void UpdateSpace()
        {
            if (_spaceRequired == null)
            {
                return;
            }

            long required = ResourceSize(AppInfo.AppResource) + ResourceSize(AppInfo.UninstallResource);
            _spaceRequired.Text = L.T("所需空间：", "Space required: ") + FormatSize(required);

            string available = "—";
            try
            {
                string full = Path.GetFullPath(_path.Text);
                string root = Path.GetPathRoot(full);
                if (!string.IsNullOrEmpty(root) && Directory.Exists(root))
                {
                    DriveInfo drive = new DriveInfo(root);
                    available = FormatSize(drive.AvailableFreeSpace);
                }
            }
            catch
            {
            }
            _spaceAvailable.Text = L.T("可用空间：", "Space available: ") + available;
        }

        private static long ResourceSize(string name)
        {
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
                {
                    return stream == null ? 0 : stream.Length;
                }
            }
            catch
            {
                return 0;
            }
        }

        private static string FormatSize(long bytes)
        {
            if (bytes >= 1024L * 1024 * 1024)
            {
                return (bytes / 1024.0 / 1024.0 / 1024.0).ToString("0.#") + " GB";
            }
            if (bytes >= 1024L * 1024)
            {
                return (bytes / 1024.0 / 1024.0).ToString("0.#") + " MB";
            }
            return Math.Max(1L, (long)Math.Round(bytes / 1024.0)) + " KB";
        }

        private void CenterOnScreen()
        {
            Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;
            int x = area.Left + (area.Width - Width) / 2;
            int y = area.Top + (area.Height - Height) / 2;
            Location = new Point(x, y);
        }

        private void ShowPage(int page)
        {
            _page = page;
            _languagePage.Visible = page == 0;
            _licensePage.Visible = page == 1;
            _locationPage.Visible = page == 2;
            _progressPage.Visible = page == 3;
            _completionPage.Visible = page == 4;

            _back.Visible = page == 1 || page == 2;
            _cancel.Visible = page < 3;
            _next.Visible = page != 3;

            if (page == 0)
            {
                _next.Text = L.T("确定", "OK");
                _next.Enabled = true;
            }
            else if (page == 1)
            {
                _next.Text = L.T("下一步", "Next");
                _next.Enabled = _agree.Checked;
            }
            else if (page == 2)
            {
                _next.Text = L.T("安装", "Install");
                _next.Enabled = true;
                UpdateSpace();
            }
            else if (page == 4)
            {
                _next.Text = L.T("完成", "Finish");
                _next.Enabled = true;
            }
        }

        private void UpdateNextState()
        {
            if (_page == 1)
            {
                _next.Enabled = _agree.Checked;
            }
        }

        private void BackClicked(object sender, EventArgs e)
        {
            if (_installing)
            {
                return;
            }

            if (_page == 1)
            {
                ShowPage(0);
            }
            else if (_page == 2)
            {
                ShowPage(1);
            }
        }

        private void NextClicked(object sender, EventArgs e)
        {
            if (_installing)
            {
                return;
            }

            if (_page == 0)
            {
                ShowPage(1);
            }
            else if (_page == 1)
            {
                if (_agree.Checked)
                {
                    ShowPage(2);
                }
            }
            else if (_page == 2)
            {
                StartInstall();
            }
            else if (_page == 4)
            {
                Finish();
            }
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
                    UpdateSpace();
                }
            }
        }

        private void StartInstall()
        {
            string dir = _path.Text.Trim();
            if (dir.Length == 0)
            {
                MessageBox.Show(L.T("请先选择安装位置。", "Please choose an install location."),
                    AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _installing = true;
            _installedDir = dir;
            ShowPage(3);
            _progress.Value = 0;
            _progressStatus.Text = L.T("正在准备…", "Preparing…");
            Application.DoEvents();

            int result = Installer.Install(dir, _desktop.Checked, _autoStart.Checked, true, false,
                delegate(int percent, string status)
                {
                    if (percent < _progress.Minimum)
                    {
                        percent = _progress.Minimum;
                    }
                    if (percent > _progress.Maximum)
                    {
                        percent = _progress.Maximum;
                    }
                    _progress.Value = percent;
                    _progressStatus.Text = status;
                    Application.DoEvents();
                    Thread.Sleep(60);
                });

            _installing = false;
            if (result == 0)
            {
                ShowPage(4);
            }
            else
            {
                MessageBox.Show(L.T("安装失败，请检查安装位置后重试。",
                        "Installation failed. Please check the install location and try again."),
                    AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ShowPage(2);
            }
        }

        private void Finish()
        {
            try
            {
                if (_run.Checked && !string.IsNullOrEmpty(_installedDir))
                {
                    Process.Start(new ProcessStartInfo(Path.Combine(_installedDir, AppInfo.Exe))
                    {
                        WorkingDirectory = _installedDir,
                        UseShellExecute = true,
                        Arguments = "--settings"
                    });
                }
            }
            catch
            {
            }
            Close();
        }
    }
}
