using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace MouseTrail
{
    /// <summary>
    /// 置顶、鼠标穿透的全屏覆盖窗口。拖尾按固定时间淡出，不受窗口大小限制。
    /// </summary>
    internal sealed class TrailForm : Form
    {
        private const int HotkeyId = 0x4D54;

        private readonly AppSettings _cfg;
        private readonly TrailEngine _engine = new TrailEngine();
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly System.Windows.Forms.Timer _timer;
        private readonly Rectangle _screen;

        private LayeredSurface _surface;
        private NotifyIcon _tray;
        private ContextMenuStrip _menu;
        private ToolStripMenuItem _enableItem;
        private ToolStripMenuItem _styleMenu;
        private ToolStripMenuItem _colorMenu;
        private ToolStripMenuItem _sizeMenu;
        private ToolStripMenuItem _lifeMenu;
        private ToolStripMenuItem _languageMenu;
        private ToolStripMenuItem _autoStartItem;

        private bool _hotkeyRegistered;
        private bool _blackoutDone;
        private bool _closing;
        private int _errorCount;
        private int _dpiTick;
        private float _dpiScale = 1f;

        public TrailForm(AppSettings cfg)
        {
            _cfg = cfg;
            _screen = SystemInformation.VirtualScreen;

            Text = L10n.T("鼠标拖尾", "Cursor Comet");
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            Bounds = _screen;
            BackColor = Color.Black;
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.UserPaint, true);
            DoubleBuffered = false;
            Icon = IconFactory.CreateAppIcon();

            RefreshDpi();

            EnsureSurface();

            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 15;
            _timer.Tick += Tick;
            _timer.Start();

            BuildTray();
            BuildMenu();
            SyncMenu();

            _hotkeyRegistered = Native.RegisterHotKey(Handle, HotkeyId,
                Native.MOD_CONTROL | Native.MOD_ALT, (int)Keys.T);
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= Native.WS_EX_LAYERED | Native.WS_EX_TRANSPARENT |
                              Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            RefreshDpi();
            EnsureSurface();
            Blackout();

            if (_cfg.ShowTip)
            {
                _cfg.ShowTip = false;
                _cfg.Save();
                _tray.ShowBalloonTip(5000,
                    L10n.T("鼠标拖尾已启动", "Cursor Comet is running"),
                    L10n.T("Ctrl + Alt + T 开关；右键托盘图标可以切换样式和颜色。",
                        "Ctrl + Alt + T to toggle; right-click the tray icon to change style and color."),
                    ToolTipIcon.Info);
            }
        }

        private void RefreshDpi()
        {
            int dpi = 96;
            try
            {
                uint windowDpi = Native.GetDpiForWindow(Handle);
                if (windowDpi >= 96)
                {
                    dpi = (int)windowDpi;
                }
            }
            catch (EntryPointNotFoundException)
            {
            }

            // 窗口刚创建、还没显示到目标显示器时，部分系统上会返回 96。
            // 此时用当前用户的显示缩放作为兜底。
            if (dpi <= 96)
            {
                try
                {
                    using (Microsoft.Win32.RegistryKey key =
                        Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
                    {
                        object value = key == null ? null : key.GetValue("LogPixels");
                        if (value != null)
                        {
                            int registryDpi = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                            if (registryDpi >= 96)
                            {
                                dpi = registryDpi;
                            }
                        }
                    }
                }
                catch
                {
                }
            }

            _dpiScale = dpi / 96f;
            if (_dpiScale < 0.5f || _dpiScale > 4f)
            {
                _dpiScale = 1f;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _closing = true;
            try
            {
                _timer.Stop();
                _timer.Dispose();
                if (_hotkeyRegistered)
                {
                    Native.UnregisterHotKey(Handle, HotkeyId);
                }
                if (_tray != null)
                {
                    _tray.Visible = false;
                    _tray.Dispose();
                    _tray = null;
                }
                if (_surface != null)
                {
                    _surface.Dispose();
                    _surface = null;
                }
            }
            catch (Exception ex)
            {
                Program.Log(ex);
            }
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_HOTKEY && m.WParam.ToInt32() == HotkeyId)
            {
                SetEnabled(!_cfg.Enabled);
                return;
            }
            base.WndProc(ref m);
        }

        private void Tick(object sender, EventArgs e)
        {
            if (_closing)
            {
                return;
            }

            try
            {
                if (!_cfg.Enabled || !IsHandleCreated)
                {
                    Blackout();
                    return;
                }

                double now = _clock.Elapsed.TotalSeconds;
                Native.POINT cursor;
                if (!Native.GetCursorPos(out cursor))
                {
                    return;
                }

                _dpiTick++;
                if (_dpiTick >= 60)
                {
                    _dpiTick = 0;
                    RefreshDpi();
                }

                bool hasContent = _engine.Update(now, cursor.X, cursor.Y, _cfg, _dpiScale);
                if (!hasContent)
                {
                    Blackout();
                    return;
                }

                Graphics g = _surface.Graphics;
                g.Clear(Color.Transparent);
                TrailRenderer.Render(g, _engine, _cfg, now, _screen.Left, _screen.Top,
                    cursor.X - _screen.Left, cursor.Y - _screen.Top, _dpiScale);
                _surface.Present(Handle, _screen.Left, _screen.Top);
                _blackoutDone = false;
            }
            catch (Exception ex)
            {
                _errorCount++;
                if (_errorCount <= 3)
                {
                    Program.Log(ex);
                }
                if (_errorCount == 3)
                {
                    SetEnabled(false);
                }
            }
        }

        /// <summary>把窗口刷成完全透明，停掉绘制后不残留最后一帧。</summary>
        private void Blackout()
        {
            if (_closing || _blackoutDone || _surface == null || !IsHandleCreated)
            {
                return;
            }

            _blackoutDone = true;
            _surface.Graphics.Clear(Color.Transparent);
            _surface.Present(Handle, _screen.Left, _screen.Top);
        }

        private void EnsureSurface()
        {
            int width = Math.Max(1, _screen.Width);
            int height = Math.Max(1, _screen.Height);
            if (_surface != null && _surface.Width == width && _surface.Height == height)
            {
                return;
            }

            LayeredSurface old = _surface;
            _surface = new LayeredSurface(width, height);
            if (old != null)
            {
                old.Dispose();
            }
            _blackoutDone = false;
        }

        private void BuildTray()
        {
            _tray = new NotifyIcon();
            _tray.Icon = IconFactory.CreateTrayIcon();
            _tray.Text = L10n.T("鼠标拖尾", "Cursor Comet");
            _tray.Visible = true;
            _tray.MouseClick += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    SyncMenu();
                    _menu.Show(Cursor.Position);
                }
            };
            UpdateTrayText();
        }

        private void BuildMenu()
        {
            _menu = new ContextMenuStrip();
            _menu.Font = new Font(L10n.T("Microsoft YaHei UI", "Segoe UI"), 9f,
                FontStyle.Regular, GraphicsUnit.Point);
            _menu.ShowImageMargin = false;
            _menu.ShowCheckMargin = true;

            ToolStripMenuItem header = new ToolStripMenuItem(L10n.T("鼠标拖尾", "Cursor Comet"));
            header.Enabled = false;
            header.Font = new Font(_menu.Font, FontStyle.Bold);
            _menu.Items.Add(header);
            _menu.Items.Add(new ToolStripSeparator());

            _enableItem = new ToolStripMenuItem(L10n.T("启用鼠标拖尾", "Enable cursor trail"));
            _enableItem.Click += delegate { SetEnabled(!_cfg.Enabled); };
            _menu.Items.Add(_enableItem);

            _styleMenu = new ToolStripMenuItem(L10n.T("样式", "Style"));
            AddChoice(_styleMenu, L10n.T("流光", "Comet"), "comet");
            AddChoice(_styleMenu, L10n.T("彩虹", "Rainbow"), "rainbow");
            AddChoice(_styleMenu, L10n.T("星火", "Sparks"), "sparks");
            AddChoice(_styleMenu, L10n.T("细线", "Minimal"), "minimal");
            _menu.Items.Add(_styleMenu);

            _colorMenu = new ToolStripMenuItem(L10n.T("颜色", "Color"));
            for (int i = 0; i < Palette.Definitions.Length; i++)
            {
                ColorDef def = Palette.Definitions[i];
                AddChoice(_colorMenu, L10n.T(def.Zh, def.En), def.Key);
            }
            _menu.Items.Add(_colorMenu);

            _sizeMenu = new ToolStripMenuItem(L10n.T("粗细", "Thickness"));
            AddChoice(_sizeMenu, L10n.T("纤细", "Fine"), "0.7");
            AddChoice(_sizeMenu, L10n.T("标准", "Standard"), "1");
            AddChoice(_sizeMenu, L10n.T("醒目", "Bold"), "1.45");
            _menu.Items.Add(_sizeMenu);

            _lifeMenu = new ToolStripMenuItem(L10n.T("时长", "Duration"));
            AddChoice(_lifeMenu, L10n.T("短", "Short"), "0.45");
            AddChoice(_lifeMenu, L10n.T("中", "Medium"), "0.7");
            AddChoice(_lifeMenu, L10n.T("长", "Long"), "1");
            _menu.Items.Add(_lifeMenu);

            _languageMenu = new ToolStripMenuItem("语言 / Language");
            AddChoice(_languageMenu, L10n.T("自动（跟随系统）", "Auto (system)"), "auto");
            AddChoice(_languageMenu, "中文", "zh");
            AddChoice(_languageMenu, "English", "en");
            _menu.Items.Add(_languageMenu);

            _menu.Items.Add(new ToolStripSeparator());

            _autoStartItem = new ToolStripMenuItem(L10n.T("开机自动启动", "Start with Windows"));
            _autoStartItem.Click += delegate
            {
                AutoStart.Set(!AutoStart.IsEnabled());
                SyncMenu();
            };
            _menu.Items.Add(_autoStartItem);

            ToolStripMenuItem resetItem = new ToolStripMenuItem(L10n.T("恢复默认设置", "Reset to defaults"));
            resetItem.Click += delegate
            {
                _cfg.Reset();
                _cfg.Enabled = true;
                _cfg.Save();
                EnsureSurface();
                _engine.Clear();
                _blackoutDone = false;
                SyncMenu();
                UpdateTrayText();
            };
            _menu.Items.Add(resetItem);

            ToolStripMenuItem aboutItem = new ToolStripMenuItem(L10n.T("关于", "About"));
            aboutItem.Click += delegate { ShowAbout(); };
            _menu.Items.Add(aboutItem);

            _menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exitItem = new ToolStripMenuItem(L10n.T("退出", "Exit"));
            exitItem.Click += delegate { Close(); };
            _menu.Items.Add(exitItem);

            _menu.Opening += delegate(object sender, CancelEventArgs e) { SyncMenu(); };
            _tray.ContextMenuStrip = _menu;
        }

        private void AddChoice(ToolStripMenuItem parent, string text, string value)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.Tag = value;
            ToolStripMenuItem owner = parent;
            item.Click += delegate { OnChoice(owner, value); };
            parent.DropDownItems.Add(item);
        }

        private void OnChoice(ToolStripMenuItem parent, string value)
        {
            if (parent == _languageMenu)
            {
                SetLanguage(value);
                return;
            }

            if (parent == _styleMenu)
            {
                _cfg.Style = value;
            }
            else if (parent == _colorMenu)
            {
                _cfg.ColorName = value;
            }
            else if (parent == _sizeMenu)
            {
                _cfg.Scale = AppSettings.ParseFloat(value, 1f);
            }
            else if (parent == _lifeMenu)
            {
                _cfg.Life = AppSettings.ParseDouble(value, 0.7);
            }

            _cfg.Save();
            EnsureSurface();
            _blackoutDone = false;
            SyncMenu();
        }

        private void SetLanguage(string language)
        {
            _cfg.Language = language;
            _cfg.Save();
            L10n.Set(_cfg.ResolveLanguage());
            Text = L10n.T("鼠标拖尾", "Cursor Comet");

            ContextMenuStrip old = _menu;
            BuildMenu();
            SyncMenu();
            UpdateTrayText();
            if (old != null)
            {
                BeginInvoke(new MethodInvoker(delegate { old.Dispose(); }));
            }
        }

        private void SyncMenu()
        {
            if (_enableItem == null)
            {
                return;
            }

            _enableItem.Checked = _cfg.Enabled;
            SyncChoices(_styleMenu, _cfg.Style);
            SyncChoices(_colorMenu, _cfg.ColorName);
            SyncChoices(_sizeMenu, _cfg.Scale.ToString("0.###", CultureInfo.InvariantCulture));
            SyncChoices(_lifeMenu, _cfg.Life.ToString("0.##", CultureInfo.InvariantCulture));
            SyncChoices(_languageMenu, _cfg.Language);
            _autoStartItem.Checked = AutoStart.IsEnabled();
        }

        private static void SyncChoices(ToolStripMenuItem parent, string value)
        {
            for (int i = 0; i < parent.DropDownItems.Count; i++)
            {
                ToolStripMenuItem item = parent.DropDownItems[i] as ToolStripMenuItem;
                if (item != null)
                {
                    item.Checked = string.Equals(item.Tag as string, value, StringComparison.Ordinal);
                }
            }
        }

        private void SetEnabled(bool enabled)
        {
            _cfg.Enabled = enabled;
            _cfg.Save();

            if (!enabled)
            {
                _engine.Clear();
                Blackout();
            }

            UpdateTrayText();
            SyncMenu();
        }

        private void UpdateTrayText()
        {
            if (_tray != null)
            {
                _tray.Text = _cfg.Enabled
                    ? L10n.T("鼠标拖尾 · 运行中", "Cursor Comet · Running")
                    : L10n.T("鼠标拖尾 · 已暂停", "Cursor Comet · Paused");
            }
        }

        private void ShowAbout()
        {
            string text = L10n.T("鼠标拖尾 v1.4.1", "Cursor Comet v1.4.1") + Environment.NewLine + Environment.NewLine;
            text += L10n.T("Ctrl + Alt + T  开关特效", "Ctrl + Alt + T  Toggle the effect") + Environment.NewLine;
            text += L10n.T("右键（或左键）托盘图标可以切换样式、颜色、粗细和时长。",
                "Click or right-click the tray icon to change style, color, thickness and duration.") +
                Environment.NewLine + Environment.NewLine;
            if (!_hotkeyRegistered)
            {
                text += L10n.T("提示：全局快捷键被其他程序占用了，可以在托盘菜单里操作。",
                    "Note: the global hotkey is used by another program; use the tray menu instead.") +
                    Environment.NewLine + Environment.NewLine;
            }
            text += L10n.T("配置文件：", "Config file: ") + AppSettings.FilePath;
            MessageBox.Show(text, L10n.T("关于鼠标拖尾", "About Cursor Comet"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
