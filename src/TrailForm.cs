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

            Text = "鼠标拖尾";
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
                _tray.ShowBalloonTip(5000, "鼠标拖尾已启动",
                    "Ctrl + Alt + T 开关；右键托盘图标可以切换样式和颜色。", ToolTipIcon.Info);
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
            _tray.Text = "鼠标拖尾";
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
            _menu.Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
            _menu.ShowImageMargin = false;
            _menu.ShowCheckMargin = true;

            ToolStripMenuItem header = new ToolStripMenuItem("鼠标拖尾");
            header.Enabled = false;
            header.Font = new Font(_menu.Font, FontStyle.Bold);
            _menu.Items.Add(header);
            _menu.Items.Add(new ToolStripSeparator());

            _enableItem = new ToolStripMenuItem("启用鼠标拖尾");
            _enableItem.Click += delegate { SetEnabled(!_cfg.Enabled); };
            _menu.Items.Add(_enableItem);

            _styleMenu = new ToolStripMenuItem("样式");
            AddChoice(_styleMenu, "流光", "comet");
            AddChoice(_styleMenu, "彩虹", "rainbow");
            AddChoice(_styleMenu, "星火", "sparks");
            AddChoice(_styleMenu, "细线", "minimal");
            _menu.Items.Add(_styleMenu);

            _colorMenu = new ToolStripMenuItem("颜色");
            for (int i = 0; i < Palette.Names.Length; i++)
            {
                AddChoice(_colorMenu, Palette.Names[i], Palette.Names[i]);
            }
            _menu.Items.Add(_colorMenu);

            _sizeMenu = new ToolStripMenuItem("粗细");
            AddChoice(_sizeMenu, "纤细", "0.7");
            AddChoice(_sizeMenu, "标准", "1");
            AddChoice(_sizeMenu, "醒目", "1.45");
            _menu.Items.Add(_sizeMenu);

            _lifeMenu = new ToolStripMenuItem("时长");
            AddChoice(_lifeMenu, "短", "0.45");
            AddChoice(_lifeMenu, "中", "0.7");
            AddChoice(_lifeMenu, "长", "1");
            _menu.Items.Add(_lifeMenu);

            _menu.Items.Add(new ToolStripSeparator());

            _autoStartItem = new ToolStripMenuItem("开机自动启动");
            _autoStartItem.Click += delegate
            {
                AutoStart.Set(!AutoStart.IsEnabled());
                SyncMenu();
            };
            _menu.Items.Add(_autoStartItem);

            ToolStripMenuItem resetItem = new ToolStripMenuItem("恢复默认设置");
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

            ToolStripMenuItem aboutItem = new ToolStripMenuItem("关于");
            aboutItem.Click += delegate { ShowAbout(); };
            _menu.Items.Add(aboutItem);

            _menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exitItem = new ToolStripMenuItem("退出");
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
                _tray.Text = _cfg.Enabled ? "鼠标拖尾 · 运行中" : "鼠标拖尾 · 已暂停";
            }
        }

        private void ShowAbout()
        {
            string text = "鼠标拖尾 v1.1.0" + Environment.NewLine + Environment.NewLine;
            text += "Ctrl + Alt + T  开关特效" + Environment.NewLine;
            text += "右键（或左键）托盘图标可以切换样式、颜色、粗细和时长。" + Environment.NewLine + Environment.NewLine;
            if (!_hotkeyRegistered)
            {
                text += "提示：全局快捷键被其他程序占用了，可以在托盘菜单里操作。" + Environment.NewLine + Environment.NewLine;
            }
            text += "配置文件：" + AppSettings.FilePath;
            MessageBox.Show(text, "关于鼠标拖尾", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
