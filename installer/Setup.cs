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
[assembly: AssemblyVersion("1.3.0.0")]
[assembly: AssemblyFileVersion("1.3.0.0")]

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
                        UseShellExecute = true
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
        private Label _subtitle;
        private Label _stepLabel;

        private Panel _languagePage;
        private Label _languageIntro;
        private RadioButton _zhRadio;
        private RadioButton _enRadio;
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
        private CheckBox _desktop;
        private CheckBox _autoStart;
        private CheckBox _launch;
        private Label _locationNote;

        private Panel _progressPage;
        private Label _progressTitle;
        private ProgressBar _progress;
        private Label _progressStatus;
        private Label _progressDetail;

        private Button _back;
        private Button _next;
        private Button _cancel;

        private int _page;
        private bool _applyingLanguage;
        private bool _installing;
        private bool _installDone;

        internal SetupForm()
        {
            Text = "Cursor Comet Setup";
            ClientSize = new Size(700, 500);
            MinimumSize = new Size(620, 440);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font(L.IsChinese ? "Microsoft YaHei UI" : "Segoe UI", 9f);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(20, 16, 20, 16);
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            root.Controls.Add(BuildHeader(), 0, 0);

            _content = new Panel();
            _content.Dock = DockStyle.Fill;
            _content.Margin = new Padding(0, 14, 0, 10);
            root.Controls.Add(_content, 0, 1);

            BuildLanguagePage();
            BuildLicensePage();
            BuildLocationPage();
            BuildProgressPage();
            _content.Controls.Add(_languagePage);
            _content.Controls.Add(_licensePage);
            _content.Controls.Add(_locationPage);
            _content.Controls.Add(_progressPage);

            BuildFooter(root);

            Controls.Add(root);

            string installedDir = InstallRegistry.GetInstalledDir();
            _path.Text = !string.IsNullOrEmpty(installedDir) && File.Exists(Path.Combine(installedDir, AppInfo.Exe))
                ? installedDir
                : AppPaths.DefaultDir;

            Load += delegate
            {
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

        private Control BuildHeader()
        {
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
                    picture.Size = new Size(48, 48);
                    picture.SizeMode = PictureBoxSizeMode.StretchImage;
                    picture.Margin = new Padding(0, 0, 14, 0);
                    header.Controls.Add(picture, 0, 0);
                }
            }
            catch
            {
            }

            TableLayoutPanel text = new TableLayoutPanel();
            text.AutoSize = true;
            text.Dock = DockStyle.Fill;
            text.ColumnCount = 1;
            text.RowCount = 3;
            text.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            text.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            text.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            text.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label title = new Label();
            title.Text = AppInfo.Name;
            title.Font = new Font(Font.FontFamily, 16f, FontStyle.Bold);
            title.AutoSize = true;
            title.Margin = new Padding(0, 0, 0, 1);
            text.Controls.Add(title, 0, 0);

            _subtitle = new Label();
            _subtitle.AutoSize = true;
            _subtitle.ForeColor = Color.Gray;
            _subtitle.Margin = new Padding(0, 0, 0, 2);
            text.Controls.Add(_subtitle, 0, 1);

            _stepLabel = new Label();
            _stepLabel.AutoSize = true;
            _stepLabel.ForeColor = Color.Gray;
            _stepLabel.Font = new Font(Font.FontFamily, 8.5f);
            _stepLabel.Margin = new Padding(0, 0, 0, 0);
            text.Controls.Add(_stepLabel, 0, 2);

            header.Controls.Add(text, 1, 0);
            return header;
        }

        private void BuildLanguagePage()
        {
            _languagePage = new Panel();
            _languagePage.Dock = DockStyle.Fill;
            _languagePage.Visible = false;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 4;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _languageIntro = new Label();
            _languageIntro.AutoSize = true;
            _languageIntro.Font = new Font(Font.FontFamily, 10f, FontStyle.Bold);
            _languageIntro.Margin = new Padding(0, 6, 0, 14);
            layout.Controls.Add(_languageIntro, 0, 0);

            FlowLayoutPanel radios = new FlowLayoutPanel();
            radios.AutoSize = true;
            radios.WrapContents = false;
            radios.Margin = new Padding(0, 0, 0, 12);

            _zhRadio = new RadioButton();
            _zhRadio.AutoSize = true;
            _zhRadio.Text = "中文";
            _zhRadio.Font = new Font(Font.FontFamily, 11f);
            _zhRadio.Margin = new Padding(0, 0, 36, 0);
            _zhRadio.CheckedChanged += LanguageChanged;
            radios.Controls.Add(_zhRadio);

            _enRadio = new RadioButton();
            _enRadio.AutoSize = true;
            _enRadio.Text = "English";
            _enRadio.Font = new Font(Font.FontFamily, 11f);
            _enRadio.CheckedChanged += LanguageChanged;
            radios.Controls.Add(_enRadio);

            layout.Controls.Add(radios, 0, 1);

            _languageNote = new Label();
            _languageNote.AutoSize = true;
            _languageNote.ForeColor = Color.Gray;
            _languageNote.MaximumSize = new Size(620, 0);
            layout.Controls.Add(_languageNote, 0, 2);

            _languagePage.Controls.Add(layout);
        }

        private void BuildLicensePage()
        {
            _licensePage = new Panel();
            _licensePage.Dock = DockStyle.Fill;
            _licensePage.Visible = false;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 4;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _licenseTitle = new Label();
            _licenseTitle.AutoSize = true;
            _licenseTitle.Font = new Font(Font.FontFamily, 10f, FontStyle.Bold);
            _licenseTitle.Margin = new Padding(0, 6, 0, 8);
            layout.Controls.Add(_licenseTitle, 0, 0);

            _licenseText = new TextBox();
            _licenseText.Multiline = true;
            _licenseText.ReadOnly = true;
            _licenseText.ScrollBars = ScrollBars.Vertical;
            _licenseText.WordWrap = true;
            _licenseText.Dock = DockStyle.Fill;
            _licenseText.BorderStyle = BorderStyle.FixedSingle;
            _licenseText.BackColor = Color.White;
            _licenseText.Font = new Font(Font.FontFamily, 9f);
            _licenseText.Margin = new Padding(0, 0, 0, 10);
            layout.Controls.Add(_licenseText, 0, 1);

            _agree = new CheckBox();
            _agree.AutoSize = true;
            _agree.Margin = new Padding(0, 4, 0, 4);
            _agree.CheckedChanged += delegate { UpdateNextState(); };
            layout.Controls.Add(_agree, 0, 2);

            Label note = new Label();
            note.AutoSize = true;
            note.ForeColor = Color.Gray;
            note.Font = new Font(Font.FontFamily, 8.5f);
            note.MaximumSize = new Size(620, 0);
            note.Margin = new Padding(0, 2, 0, 0);
            note.Text = L.T("安装程序本身同样以 GPL-3.0 发布。", "The installer itself is also released under GPL-3.0.");
            layout.Controls.Add(note, 0, 3);

            _licensePage.Controls.Add(layout);
        }

        private void BuildLocationPage()
        {
            _locationPage = new Panel();
            _locationPage.Dock = DockStyle.Fill;
            _locationPage.Visible = false;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 5;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _locationTitle = new Label();
            _locationTitle.AutoSize = true;
            _locationTitle.Font = new Font(Font.FontFamily, 10f, FontStyle.Bold);
            _locationTitle.Margin = new Padding(0, 6, 0, 12);
            layout.Controls.Add(_locationTitle, 0, 0);

            TableLayoutPanel pathRow = new TableLayoutPanel();
            pathRow.AutoSize = true;
            pathRow.Dock = DockStyle.Fill;
            pathRow.ColumnCount = 3;
            pathRow.RowCount = 1;
            pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathRow.Margin = new Padding(0, 0, 0, 4);

            _pathLabel = new Label();
            _pathLabel.AutoSize = true;
            _pathLabel.Anchor = AnchorStyles.Left;
            _pathLabel.Margin = new Padding(0, 6, 10, 0);
            pathRow.Controls.Add(_pathLabel, 0, 0);

            _path = new TextBox();
            _path.Dock = DockStyle.Fill;
            _path.Margin = new Padding(0, 4, 8, 0);
            pathRow.Controls.Add(_path, 1, 0);

            _browse = new Button();
            _browse.AutoSize = true;
            _browse.Margin = new Padding(0, 2, 0, 0);
            _browse.Click += BrowseClicked;
            pathRow.Controls.Add(_browse, 2, 0);
            layout.Controls.Add(pathRow, 0, 1);

            FlowLayoutPanel options = new FlowLayoutPanel();
            options.FlowDirection = FlowDirection.TopDown;
            options.AutoSize = true;
            options.WrapContents = false;
            options.Margin = new Padding(0, 6, 0, 8);

            _desktop = new CheckBox();
            _desktop.AutoSize = true;
            _desktop.Margin = new Padding(0, 3, 0, 3);
            options.Controls.Add(_desktop);

            _autoStart = new CheckBox();
            _autoStart.AutoSize = true;
            _autoStart.Margin = new Padding(0, 3, 0, 3);
            options.Controls.Add(_autoStart);

            _launch = new CheckBox();
            _launch.AutoSize = true;
            _launch.Checked = true;
            _launch.Margin = new Padding(0, 3, 0, 3);
            options.Controls.Add(_launch);
            layout.Controls.Add(options, 0, 3);

            _locationNote = new Label();
            _locationNote.AutoSize = true;
            _locationNote.ForeColor = Color.Gray;
            _locationNote.MaximumSize = new Size(620, 0);
            _locationNote.Margin = new Padding(0, 2, 0, 10);
            layout.Controls.Add(_locationNote, 0, 2);

            _locationPage.Controls.Add(layout);
        }

        private void BuildProgressPage()
        {
            _progressPage = new Panel();
            _progressPage.Dock = DockStyle.Fill;
            _progressPage.Visible = false;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 5;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _progressTitle = new Label();
            _progressTitle.AutoSize = true;
            _progressTitle.Font = new Font(Font.FontFamily, 10f, FontStyle.Bold);
            _progressTitle.Margin = new Padding(0, 6, 0, 14);
            layout.Controls.Add(_progressTitle, 0, 0);

            _progress = new ProgressBar();
            _progress.Minimum = 0;
            _progress.Maximum = 100;
            _progress.Value = 0;
            _progress.Height = 18;
            _progress.Dock = DockStyle.Top;
            _progress.Margin = new Padding(0, 0, 0, 12);
            layout.Controls.Add(_progress, 0, 1);

            _progressStatus = new Label();
            _progressStatus.AutoSize = true;
            _progressStatus.Margin = new Padding(0, 0, 0, 6);
            layout.Controls.Add(_progressStatus, 0, 2);

            _progressDetail = new Label();
            _progressDetail.AutoSize = true;
            _progressDetail.ForeColor = Color.Gray;
            _progressDetail.MaximumSize = new Size(620, 0);
            layout.Controls.Add(_progressDetail, 0, 3);

            _progressPage.Controls.Add(layout);
        }

        private void BuildFooter(TableLayoutPanel root)
        {
            FlowLayoutPanel footer = new FlowLayoutPanel();
            footer.FlowDirection = FlowDirection.RightToLeft;
            footer.Dock = DockStyle.Fill;
            footer.AutoSize = true;
            footer.WrapContents = false;
            footer.Margin = new Padding(0, 8, 0, 0);

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
            button.MinimumSize = new Size(96, 32);
            button.Margin = new Padding(6, 0, 0, 0);
            return button;
        }

        private void LanguageChanged(object sender, EventArgs e)
        {
            if (_applyingLanguage)
            {
                return;
            }

            L.Set(_zhRadio.Checked ? "zh" : "en");
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            _applyingLanguage = true;
            _zhRadio.Checked = L.IsChinese;
            _enRadio.Checked = !L.IsChinese;
            _applyingLanguage = false;

            Text = L.T("Cursor Comet 安装程序", "Cursor Comet Setup");
            _subtitle.Text = L.T("Windows 鼠标拖尾特效 · 安装程序", "Windows cursor trail effect · Setup");

            _languageIntro.Text = L.T("请选择安装程序的语言：", "Choose the language for the installer:");
            _languageNote.Text = L.T("安装完成后，应用也会使用该语言。",
                "The app will use this language after installation.");

            _licenseTitle.Text = L.T("许可协议", "License agreement");
            _licenseText.Text = L.T(AgreementZh(), AgreementEn());
            _agree.Text = L.T("我已阅读并同意以上协议", "I have read and accept the agreement");

            _locationTitle.Text = L.T("安装位置", "Install location");
            _pathLabel.Text = L.T("安装到", "Install to");
            _browse.Text = L.T("浏览…", "Browse…");
            _desktop.Text = L.T("创建桌面快捷方式", "Create desktop shortcut");
            _autoStart.Text = L.T("开机自动启动", "Start with Windows");
            _launch.Text = L.T("安装完成后启动", "Launch after install");
            RefreshLocationNote();

            _progressTitle.Text = L.T("正在安装 Cursor Comet", "Installing Cursor Comet");
            if (!_installing && !_installDone)
            {
                _progressStatus.Text = L.T("准备安装…", "Ready to install.");
            }

            _back.Text = L.T("上一步", "Back");
            _cancel.Text = L.T("取消", "Cancel");

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

        private void RefreshLocationNote()
        {
            string installed = InstallRegistry.GetInstalledDir();
            bool installedHere = !string.IsNullOrEmpty(installed) &&
                                 File.Exists(Path.Combine(installed, AppInfo.Exe)) &&
                                 string.Equals(_path.Text.Trim().TrimEnd('\\'),
                                     installed.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
            _locationNote.Text = installedHere
                ? L.T("已安装到该位置，可以覆盖安装。", "Already installed here. You can reinstall.")
                : L.T("默认安装到当前用户目录，不需要管理员权限。",
                    "Installs for the current user; no administrator rights required.");
        }

        private void ShowPage(int page)
        {
            _page = page;
            _languagePage.Visible = page == 0;
            _licensePage.Visible = page == 1;
            _locationPage.Visible = page == 2;
            _progressPage.Visible = page == 3;

            _back.Visible = page == 1 || page == 2;
            _cancel.Visible = page < 3;
            _next.Visible = true;

            if (page == 0)
            {
                _stepLabel.Text = L.T("第 1 步，共 3 步", "Step 1 of 3");
                _next.Text = L.T("下一步", "Next");
                _next.Enabled = true;
            }
            else if (page == 1)
            {
                _stepLabel.Text = L.T("第 2 步，共 3 步", "Step 2 of 3");
                _next.Text = L.T("下一步", "Next");
                _next.Enabled = _agree.Checked;
            }
            else if (page == 2)
            {
                _stepLabel.Text = L.T("第 3 步，共 3 步", "Step 3 of 3");
                _next.Text = L.T("安装", "Install");
                _next.Enabled = true;
                RefreshLocationNote();
            }
            else
            {
                _stepLabel.Text = _installDone
                    ? L.T("安装完成", "Done")
                    : L.T("正在安装", "Installing");
                _progressTitle.Text = _installDone
                    ? L.T("安装完成", "Installation complete")
                    : L.T("正在安装 Cursor Comet", "Installing Cursor Comet");
                _next.Text = L.T("完成", "Finish");
                _next.Enabled = _installDone;
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
            else if (_page == 3 && _installDone)
            {
                Close();
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
                    RefreshLocationNote();
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
            _installDone = false;
            ShowPage(3);
            _progress.Value = 0;
            _progressStatus.Text = L.T("正在准备…", "Preparing…");
            _progressDetail.Text = string.Empty;
            Application.DoEvents();

            int result = Installer.Install(dir, _desktop.Checked, _autoStart.Checked, true, _launch.Checked,
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
                _installDone = true;
                _progress.Value = 100;
                _progressStatus.Text = L.T("安装完成。", "Installation complete.");
                _progressDetail.Text = L.T("安装位置：", "Installed to: ") + dir;
                ShowPage(3);
            }
            else
            {
                MessageBox.Show(L.T("安装失败，请检查安装位置后重试。",
                        "Installation failed. Please check the install location and try again."),
                    AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ShowPage(2);
            }
        }
    }
}
