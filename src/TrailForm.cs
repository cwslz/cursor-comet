using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace CursorComet
{
    /// <summary>
    /// 置顶、鼠标穿透的全屏覆盖窗口。拖尾按固定时间淡出，不受窗口大小限制。
    /// </summary>
    internal sealed class TrailForm : Form
    {
        private const int HotkeyId = 0x4D54;
        private const int WM_DISPLAYCHANGE = 0x007E;

        private readonly AppSettings _cfg;
        private readonly TrailEngine _engine = new TrailEngine();
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly System.Windows.Forms.Timer _timer;
        private Rectangle _screen;

        private LayeredSurface _surface;
        private NotifyIcon _tray;
        private ContextMenuStrip _menu;
        private ToolStripMenuItem _enableItem;
        private MainForm _mainForm;
        private readonly bool _openSettings;

        private bool _hotkeyRegistered;
        private bool _blackoutDone;
        private bool _closing;
        private int _errorCount;
        private int _dpiTick;
        private int _topMostTick;
        private float _dpiScale = 1f;
        private float _lastCursorX;
        private float _lastCursorY;
        private bool _hasLastCursor;
        private bool _haloDirty = true;
        private bool _hadContent;
        private bool _paused;
        private Rectangle _lastDirty;
        private double _colorTime;

        public TrailForm(AppSettings cfg, bool openSettings)
        {
            _cfg = cfg;
            _openSettings = openSettings;
            _screen = SystemInformation.VirtualScreen;

            Text = "CursorCometOverlay";
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
                    L10n.T("Ctrl + Alt + T 开关；右键托盘图标可以打开主界面。",
                        "Ctrl + Alt + T to toggle; right-click the tray icon to open the main window."),
                    ToolTipIcon.Info);
            }

            if (_openSettings)
            {
                BeginInvoke(new MethodInvoker(OpenMain));
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
            if (m.Msg == WM_DISPLAYCHANGE)
            {
                try
                {
                    BeginInvoke(new MethodInvoker(UpdateDisplayBounds));
                }
                catch
                {
                }
            }

            if (m.Msg == Native.WM_HOTKEY && m.WParam.ToInt32() == HotkeyId)
            {
                TogglePause();
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
                if (!IsHandleCreated)
                {
                    Blackout();
                    return;
                }

                if (_paused)
                {
                    Blackout();
                    return;
                }

                _topMostTick++;
                if (_topMostTick >= 30)
                {
                    _topMostTick = 0;
                    // 无边框全屏游戏激活后会排到 topmost 之上；定期把覆盖层重新置顶。
                    Native.SetWindowPos(Handle, new IntPtr(-1), 0, 0, 0, 0, 0x0002 | 0x0001 | 0x0010);
                }

                bool glow = _cfg.GlowEnabled && _cfg.GlowStrength > 0.001;
                bool halo = _cfg.HaloEnabled;
                if (!_cfg.Enabled && !glow && !halo)
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

                bool hasContent = false;
                if (_cfg.Enabled)
                {
                    hasContent = _engine.Update(now, cursor.X, cursor.Y, _cfg, _dpiScale);
                }
                else if (_engine.Samples.Count > 0)
                {
                    _engine.Clear();
                }

                if (!hasContent && !glow && !halo)
                {
                    Blackout();
                    return;
                }

                bool moved = !_hasLastCursor ||
                    cursor.X != _lastCursorX || cursor.Y != _lastCursorY;
                bool contentEnded = _hadContent && !hasContent;
                if (!hasContent && (glow || halo) && !moved && !_haloDirty && !contentEnded)
                {
                    return;
                }

                if (moved)
                {
                    // 每个移动帧都推进彩虹颜色，不必等新的轨迹采样点。
                    _colorTime = now;
                }

                bool drawGhost = moved && _hasLastCursor;
                Rectangle dirty = ComputeDirty(cursor.X, cursor.Y, drawGhost);
                Rectangle screenBounds = SystemInformation.VirtualScreen;
                Rectangle windowRect = Rectangle.Intersect(
                    Rectangle.Union(dirty, _lastDirty), screenBounds);
                if (windowRect.Width <= 0 || windowRect.Height <= 0)
                {
                    Blackout();
                    return;
                }

                if (_screen != windowRect)
                {
                    _screen = windowRect;
                    Bounds = windowRect;
                    EnsureSurface();
                }

                int cursorX = cursor.X - _screen.Left;
                int cursorY = cursor.Y - _screen.Top;
                Rectangle surface = new Rectangle(0, 0, _surface.Width, _surface.Height);
                Graphics g = _surface.Graphics;
                g.SetClip(surface);
                g.CompositingMode = CompositingMode.SourceCopy;
                using (SolidBrush clear = new SolidBrush(Color.Transparent))
                {
                    g.FillRectangle(clear, surface);
                }
                g.CompositingMode = CompositingMode.SourceOver;
                TrailRenderer.Render(g, _engine, _cfg, now, _colorTime, _screen.Left, _screen.Top,
                    cursorX, cursorY,
                    _lastCursorX - _screen.Left, _lastCursorY - _screen.Top, drawGhost, _dpiScale);
                g.ResetClip();
                _surface.Present(Handle, _screen.Left, _screen.Top);
                _lastDirty = windowRect;
                _blackoutDone = false;
                _lastCursorX = cursor.X;
                _lastCursorY = cursor.Y;
                _hasLastCursor = true;
                _haloDirty = false;
                _hadContent = hasContent;
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

        private Rectangle ComputeDirty(int cursorX, int cursorY, bool drawGhost)
        {
            float minX = cursorX;
            float maxX = cursorX;
            float minY = cursorY;
            float maxY = cursorY;
            if (drawGhost)
            {
                minX = Math.Min(minX, _lastCursorX);
                maxX = Math.Max(maxX, _lastCursorX);
                minY = Math.Min(minY, _lastCursorY);
                maxY = Math.Max(maxY, _lastCursorY);
            }

            List<TrailEngine.Sample> samples = _engine.Samples;
            for (int i = 0; i < samples.Count; i++)
            {
                TrailEngine.Sample sample = samples[i];
                float x = sample.X;
                float y = sample.Y;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            List<TrailEngine.Spark> sparks = _engine.Sparks;
            for (int i = 0; i < sparks.Count; i++)
            {
                TrailEngine.Spark spark = sparks[i];
                float x = spark.X;
                float y = spark.Y;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            int margin = (int)((80 + _cfg.GlowRadius + _cfg.HaloThickness) * _dpiScale);
            int left = (int)Math.Floor(minX) - margin;
            int top = (int)Math.Floor(minY) - margin;
            int right = (int)Math.Ceiling(maxX) + margin;
            int bottom = (int)Math.Ceiling(maxY) + margin;
            return Rectangle.FromLTRB(left, top, right, bottom);
        }

        /// <summary>把窗口刷成完全透明，停掉绘制后不残留最后一帧。</summary>
        private void Blackout()
        {
            if (_closing || _blackoutDone || _surface == null || !IsHandleCreated)
            {
                return;
            }

            _blackoutDone = true;
            _lastDirty = Rectangle.Empty;
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
            _lastDirty = Rectangle.Empty;
            _haloDirty = true;
            _hasLastCursor = false;
            _hadContent = false;
        }

        /// <summary>插拔显示器或改分辨率后，重新覆盖整个虚拟屏幕。</summary>
        private void UpdateDisplayBounds()
        {
            if (_closing || !IsHandleCreated)
            {
                return;
            }

            try
            {
                Rectangle screen = SystemInformation.VirtualScreen;
                if (screen.Width <= 0 || screen.Height <= 0)
                {
                    return;
                }
                if (screen == _screen && Bounds == screen)
                {
                    return;
                }

                _screen = screen;
                Bounds = screen;
                EnsureSurface();
                _engine.Clear();
                _blackoutDone = false;
                _lastDirty = Rectangle.Empty;
                _haloDirty = true;
                _hasLastCursor = false;
                _hadContent = false;
                RefreshDpi();
            }
            catch (Exception ex)
            {
                Program.Log(ex);
            }
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
            _tray.DoubleClick += delegate { OpenMain(); };
            UpdateTrayText();
        }

        private void BuildMenu()
        {
            _menu = new ContextMenuStrip();
            _menu.Font = FluentFonts.Create(9.5f, FontStyle.Regular);
            _menu.ShowImageMargin = false;
            _menu.ShowCheckMargin = false;

            ToolStripMenuItem header = new ToolStripMenuItem(L10n.T("鼠标拖尾", "Cursor Comet"));
            header.Enabled = false;
            header.Font = new Font(_menu.Font, FontStyle.Bold);
            _menu.Items.Add(header);

            ToolStripMenuItem openItem = new ToolStripMenuItem(L10n.T("打开主界面", "Open main window"));
            openItem.Font = new Font(_menu.Font, FontStyle.Bold);
            openItem.Click += delegate { OpenMain(); };
            _menu.Items.Add(openItem);

            _enableItem = new ToolStripMenuItem(L10n.T("关闭效果", "Disable effects"));
            _enableItem.CheckOnClick = false;
            _enableItem.Click += delegate { TogglePause(); };
            _menu.Items.Add(_enableItem);

            ToolStripMenuItem exitItem = new ToolStripMenuItem(L10n.T("退出", "Exit"));
            exitItem.Click += delegate { Close(); };
            _menu.Items.Add(exitItem);

            _menu.Opening += delegate(object sender, CancelEventArgs e) { SyncMenu(); };
            _tray.ContextMenuStrip = _menu;
        }
        private void RebuildMenu()
        {
            ContextMenuStrip old = _menu;
            BuildMenu();
            SyncMenu();
            if (old != null)
            {
                BeginInvoke(new MethodInvoker(delegate { old.Dispose(); }));
            }
        }

        private void OpenMain()
        {
            if (_mainForm == null || _mainForm.IsDisposed)
            {
                _mainForm = new MainForm(_cfg, MainSettingsChanged);
                _mainForm.FormClosed += delegate { _mainForm = null; };
            }

            _mainForm.Show();
            if (_mainForm.WindowState == FormWindowState.Minimized)
            {
                _mainForm.WindowState = FormWindowState.Normal;
            }
            _mainForm.Activate();
            _mainForm.BringToFront();
        }

        internal void OpenMainFromSignal()
        {
            OpenMain();
        }

        private void MainSettingsChanged(bool languageChanged)
        {
            EnsureSurface();
            if (!_cfg.Enabled)
            {
                _engine.Clear();
            }
            _blackoutDone = false;
            _haloDirty = true;
            _hasLastCursor = false;
            _hadContent = false;

            if (languageChanged)
            {
                L10n.Set(_cfg.ResolveLanguage());
                Text = "CursorCometOverlay";
                RebuildMenu();
            }
            else
            {
                SyncMenu();
            }
            UpdateTrayText();
        }

        private void RefreshMainForm()
        {
            if (_mainForm != null && !_mainForm.IsDisposed)
            {
                _mainForm.ReloadFromSettings();
            }
        }

        private void SyncMenu()
        {
            if (_enableItem == null)
            {
                return;
            }
            _enableItem.Text = _paused
                ? L10n.T("开启效果", "Enable effects")
                : L10n.T("关闭效果", "Disable effects");
        }

        private void TogglePause()
        {
            _paused = !_paused;
            _blackoutDone = false;
            _haloDirty = true;
            _hasLastCursor = false;
            _hadContent = false;
            if (_paused)
            {
                _engine.Clear();
                Blackout();
            }
            SyncMenu();
            UpdateTrayText();
        }

        private void SetEnabled(bool enabled)
        {
            _cfg.Enabled = enabled;
            _cfg.Save();
            _haloDirty = true;
            _hadContent = false;

            if (!enabled)
            {
                _engine.Clear();
            }

            UpdateTrayText();
            SyncMenu();
            RefreshMainForm();
        }

        private void UpdateTrayText()
        {
            if (_tray != null)
            {
                if (_paused)
                {
                    _tray.Text = L10n.T("鼠标拖尾 · 效果已关闭", "Cursor Comet · Effects off");
                }
                else if (_cfg.Enabled)
                {
                    _tray.Text = L10n.T("鼠标拖尾 · 运行中", "Cursor Comet · Running");
                }
                else if (_cfg.GlowEnabled || _cfg.HaloEnabled)
                {
                    _tray.Text = L10n.T("鼠标拖尾 · 拖尾已关闭", "Cursor Comet · Trail off");
                }
                else
                {
                    _tray.Text = L10n.T("鼠标拖尾 · 已暂停", "Cursor Comet · Paused");
                }
            }
        }

    }
}
