using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MouseTrail
{
    /// <summary>Material You 风格的主设置窗口：左侧分区导航，右侧卡片内容。</summary>
    internal sealed class MainForm : Form
    {
        private enum Section
        {
            Appearance,
            Color,
            Halo,
            Behavior,
            Language,
            About,
            GitHub
        }

        private const string RepoUrl = "https://github.com/cwslz/cursor-comet";
        private const string AppVersion = "1.8.0";
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int WM_NCHITTEST = 0x84;
        private const int HTCAPTION = 2;
        private const int HTCLIENT = 1;

        private readonly AppSettings _cfg;
        private readonly Action<bool> _changed;
        private readonly ToolTip _toolTip = new ToolTip();
        private readonly List<NavItem> _navItems = new List<NavItem>();
        private readonly List<NavItem> _bottomItems = new List<NavItem>();

        private Section _section = Section.Appearance;
        private TableLayoutPanel _root;
        private TableLayoutPanel _sidebar;
        private Label _appTitle;
        private Label _appSubtitle;
        private TableLayoutPanel _navList;
        private TableLayoutPanel _bottomNav;
        private Label _sectionTitle;
        private Label _sectionSubtitle;
        private FlowLayoutPanel _cards;
        private WindowButton _maxButton;
        private bool _loading;
        private bool _collapsed;
        private float _dpiScale = 1f;

        internal MainForm(AppSettings cfg, Action<bool> changed)
        {
            _cfg = cfg;
            _changed = changed;

            Text = L10n.T("鼠标拖尾 · 设置", "Cursor Comet · Settings");
            BackColor = MaterialColors.Surface;
            ForeColor = MaterialColors.OnSurface;
            Font = FluentFonts.Create(10f, FontStyle.Regular);
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            try
            {
                using (Graphics graphics = CreateGraphics())
                {
                    _dpiScale = graphics.DpiX / 96f;
                }
            }
            catch
            {
                _dpiScale = 1f;
            }
            if (_dpiScale < 1f)
            {
                _dpiScale = 1f;
            }
            ClientSize = new Size((int)(1080 * _dpiScale), (int)(700 * _dpiScale));
            MinimumSize = new Size((int)(760 * _dpiScale), (int)(520 * _dpiScale));
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            int fitWidth = Math.Min(ClientSize.Width, work.Width - (int)(32 * _dpiScale));
            int fitHeight = Math.Min(ClientSize.Height, work.Height - (int)(32 * _dpiScale));
            ClientSize = new Size(Math.Max(MinimumSize.Width, fitWidth),
                Math.Max(MinimumSize.Height, fitHeight));
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = true;
            DoubleBuffered = true;

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
            }

            BuildLayout();

            Load += delegate
            {
                ApplyLanguage();
                SelectSection(Section.Appearance);
            };
            Resize += delegate { UpdateResponsive(); };
        }

        public void ReloadFromSettings()
        {
            L10n.Set(_cfg.ResolveLanguage());
            ApplyLanguage();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateResponsive();
            UpdateMaximizeIcon();
        }

        private WindowButton MakeWindowButton(WindowButtonIcon icon, string tip)
        {
            WindowButton button = new WindowButton();
            button.Icon = icon;
            button.UiScale = _dpiScale;
            button.Width = (int)(46 * _dpiScale);
            button.Height = (int)(32 * _dpiScale);
            button.Margin = new Padding(0, 0, 0, 0);
            _toolTip.SetToolTip(button, tip);
            return button;
        }

        private void AttachWindowDrag(Control control)
        {
            control.MouseDown += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
                }
            };
            control.DoubleClick += delegate { ToggleMaximize(); };
        }

        private void ToggleMaximize()
        {
            if (WindowState == FormWindowState.Maximized)
            {
                WindowState = FormWindowState.Normal;
            }
            else
            {
                UpdateMaximizedBounds();
                WindowState = FormWindowState.Maximized;
            }
        }

        private void UpdateMaximizeIcon()
        {
            if (_maxButton == null)
            {
                return;
            }
            bool maximized = WindowState == FormWindowState.Maximized;
            _maxButton.Icon = maximized ? WindowButtonIcon.Restore : WindowButtonIcon.Maximize;
            _toolTip.SetToolTip(_maxButton, maximized ? L10n.T("还原", "Restore") : L10n.T("最大化", "Maximize"));
            _maxButton.Invalidate();
        }

        private void UpdateMaximizedBounds()
        {
            try
            {
                MaximizedBounds = Screen.FromControl(this).WorkingArea;
            }
            catch
            {
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            FluentBackdrop.Apply(Handle);
            try
            {
                int preference = 2;
                DwmSetWindowAttribute(Handle, 33, ref preference, 4);
            }
            catch
            {
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UpdateMaximizedBounds();
            UpdateMaximizeIcon();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_NCHITTEST && (int)m.Result == HTCLIENT &&
                WindowState == FormWindowState.Normal)
            {
                int raw = m.LParam.ToInt32();
                Point screen = new Point((short)(raw & 0xFFFF), (short)((raw >> 16) & 0xFFFF));
                Point point = PointToClient(screen);
                int border = Math.Max(4, (int)(6 * _dpiScale));
                bool left = point.X < border;
                bool right = point.X > ClientSize.Width - border;
                bool top = point.Y < border;
                bool bottom = point.Y > ClientSize.Height - border;
                if (top && left) m.Result = (IntPtr)13;
                else if (top && right) m.Result = (IntPtr)14;
                else if (bottom && left) m.Result = (IntPtr)16;
                else if (bottom && right) m.Result = (IntPtr)17;
                else if (left) m.Result = (IntPtr)10;
                else if (right) m.Result = (IntPtr)11;
                else if (top) m.Result = (IntPtr)12;
                else if (bottom) m.Result = (IntPtr)15;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000;
                return cp;
            }
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hWnd, int attribute, ref int value, int size);

        private void BuildLayout()
        {
            _root = new TableLayoutPanel();
            _root.Dock = DockStyle.Fill;
            _root.ColumnCount = 2;
            _root.RowCount = 1;
            _root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 236f * _dpiScale));
            _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _root.BackColor = MaterialColors.Surface;
            Controls.Add(_root);

            _sidebar = new TableLayoutPanel();
            _sidebar.Dock = DockStyle.Fill;
            _sidebar.ColumnCount = 1;
            _sidebar.RowCount = 4;
            _sidebar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _sidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _sidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _sidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _sidebar.BackColor = MaterialColors.Surface;
            _sidebar.Padding = new Padding(
                (int)(12 * _dpiScale), (int)(16 * _dpiScale),
                (int)(12 * _dpiScale), (int)(12 * _dpiScale));
            _root.Controls.Add(_sidebar, 0, 0);

            BuildSidebarHeader();
            BuildNavList();
            Panel spacer = new Panel();
            spacer.Dock = DockStyle.Fill;
            spacer.BackColor = MaterialColors.Surface;
            _sidebar.Controls.Add(spacer, 0, 2);
            BuildBottomNav();

            TableLayoutPanel content = new TableLayoutPanel();
            content.Dock = DockStyle.Fill;
            content.ColumnCount = 1;
            content.RowCount = 2;
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            content.BackColor = MaterialColors.Surface;
            _root.Controls.Add(content, 1, 0);

            TableLayoutPanel header = new TableLayoutPanel();
            header.AutoSize = true;
            header.Dock = DockStyle.Top;
            header.ColumnCount = 2;
            header.RowCount = 2;
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            header.Padding = new Padding(
                (int)(32 * _dpiScale), (int)(20 * _dpiScale),
                (int)(6 * _dpiScale), (int)(8 * _dpiScale));
            header.BackColor = MaterialColors.Surface;

            _sectionTitle = new Label();
            _sectionTitle.AutoSize = true;
            _sectionTitle.Font = new Font(Font.FontFamily, 26f, FontStyle.Bold);
            _sectionTitle.ForeColor = MaterialColors.OnSurface;
            _sectionTitle.BackColor = MaterialColors.Surface;
            _sectionTitle.Margin = new Padding(0, (int)(6 * _dpiScale), 0, 2);
            header.Controls.Add(_sectionTitle, 0, 0);

            _sectionSubtitle = new Label();
            _sectionSubtitle.AutoSize = true;
            _sectionSubtitle.Font = new Font(Font.FontFamily, 10f);
            _sectionSubtitle.ForeColor = MaterialColors.OnSurfaceVariant;
            _sectionSubtitle.BackColor = MaterialColors.Surface;
            _sectionSubtitle.Margin = new Padding(0, 0, 0, 0);
            header.Controls.Add(_sectionSubtitle, 0, 1);

            FlowLayoutPanel windowControls = new FlowLayoutPanel();
            windowControls.AutoSize = true;
            windowControls.WrapContents = false;
            windowControls.FlowDirection = FlowDirection.RightToLeft;
            windowControls.BackColor = MaterialColors.Surface;
            windowControls.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            windowControls.Margin = new Padding(0, 0, 0, 0);

            WindowButton closeButton = MakeWindowButton(WindowButtonIcon.Close, L10n.T("关闭", "Close"));
            closeButton.Click += delegate { Close(); };
            _maxButton = MakeWindowButton(WindowButtonIcon.Maximize, L10n.T("最大化", "Maximize"));
            _maxButton.Click += delegate { ToggleMaximize(); };
            WindowButton minButton = MakeWindowButton(WindowButtonIcon.Minimize, L10n.T("最小化", "Minimize"));
            minButton.Click += delegate { WindowState = FormWindowState.Minimized; };

            windowControls.Controls.Add(closeButton);
            windowControls.Controls.Add(_maxButton);
            windowControls.Controls.Add(minButton);
            header.Controls.Add(windowControls, 1, 0);
            header.SetRowSpan(windowControls, 2);

            AttachWindowDrag(header);
            AttachWindowDrag(_sectionTitle);
            AttachWindowDrag(_sectionSubtitle);

            content.Controls.Add(header, 0, 0);

            _cards = new FlowLayoutPanel();
            _cards.Dock = DockStyle.Fill;
            _cards.AutoScroll = true;
            _cards.FlowDirection = FlowDirection.TopDown;
            _cards.WrapContents = false;
            _cards.BackColor = MaterialColors.Surface;
            _cards.Padding = new Padding(
                (int)(32 * _dpiScale), (int)(8 * _dpiScale),
                (int)(32 * _dpiScale), (int)(28 * _dpiScale));
            _cards.Resize += delegate { LayoutCards(); };
            content.Controls.Add(_cards, 0, 1);
        }

        private void BuildSidebarHeader()
        {
            TableLayoutPanel header = new TableLayoutPanel();
            header.AutoSize = true;
            header.Dock = DockStyle.Top;
            header.ColumnCount = 2;
            header.RowCount = 1;
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            header.Margin = new Padding(0, 0, 0, (int)(14 * _dpiScale));
            header.BackColor = MaterialColors.Surface;

            try
            {
                Icon icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (icon != null)
                {
                    PictureBox picture = new PictureBox();
                    picture.Image = icon.ToBitmap();
                    picture.Size = new Size((int)(38 * _dpiScale), (int)(38 * _dpiScale));
                    picture.SizeMode = PictureBoxSizeMode.StretchImage;
                    picture.Margin = new Padding(0, 0, (int)(10 * _dpiScale), 0);
                    header.Controls.Add(picture, 0, 0);
                    AttachWindowDrag(picture);
                }
            }
            catch
            {
            }

            TableLayoutPanel text = new TableLayoutPanel();
            text.AutoSize = true;
            text.Dock = DockStyle.Fill;
            text.ColumnCount = 1;
            text.RowCount = 2;
            text.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            text.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            text.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _appTitle = new Label();
            _appTitle.AutoSize = true;
            _appTitle.Font = new Font(Font.FontFamily, 14f, FontStyle.Bold);
            _appTitle.ForeColor = MaterialColors.OnSurface;
            _appTitle.BackColor = MaterialColors.Surface;
            _appTitle.Margin = new Padding(0, 0, 0, 0);
            text.Controls.Add(_appTitle, 0, 0);

            _appSubtitle = new Label();
            _appSubtitle.AutoSize = true;
            _appSubtitle.Font = new Font(Font.FontFamily, 9f);
            _appSubtitle.ForeColor = MaterialColors.OnSurfaceVariant;
            _appSubtitle.BackColor = MaterialColors.Surface;
            _appSubtitle.Margin = new Padding(0, 0, 0, 0);
            text.Controls.Add(_appSubtitle, 0, 1);

            header.Controls.Add(text, 1, 0);
            _sidebar.Controls.Add(header, 0, 0);

            AttachWindowDrag(header);
            AttachWindowDrag(text);
            AttachWindowDrag(_appTitle);
            AttachWindowDrag(_appSubtitle);
        }

        private void BuildNavList()
        {
            _navList = new TableLayoutPanel();
            _navList.AutoSize = true;
            _navList.Dock = DockStyle.Top;
            _navList.ColumnCount = 1;
            _navList.RowCount = 0;
            _navList.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _navList.BackColor = MaterialColors.Surface;
            _navList.Margin = new Padding(0, 0, 0, 0);
            _sidebar.Controls.Add(_navList, 0, 1);

            AddNav(Section.Appearance, NavIcon.Sparkle, _navList, _navItems);
            AddNav(Section.Color, NavIcon.Palette, _navList, _navItems);
            AddNav(Section.Halo, NavIcon.Halo, _navList, _navItems);
            AddNav(Section.Behavior, NavIcon.Tune, _navList, _navItems);
            AddNav(Section.Language, NavIcon.Globe, _navList, _navItems);
        }

        private void BuildBottomNav()
        {
            _bottomNav = new TableLayoutPanel();
            _bottomNav.AutoSize = true;
            _bottomNav.Dock = DockStyle.Top;
            _bottomNav.ColumnCount = 1;
            _bottomNav.RowCount = 0;
            _bottomNav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _bottomNav.BackColor = MaterialColors.Surface;
            _bottomNav.Margin = new Padding(0, 10, 0, 0);
            _sidebar.Controls.Add(_bottomNav, 0, 3);

            AddNav(Section.About, NavIcon.Info, _bottomNav, _bottomItems);
            AddNav(Section.GitHub, NavIcon.Code, _bottomNav, _bottomItems);
        }

        private void AddNav(Section section, NavIcon icon, TableLayoutPanel host, List<NavItem> list)
        {
            NavItem item = new NavItem();
            item.Icon = icon;
            item.Tag = section;
            item.Label = SectionTitle(section);
            item.UiScale = _dpiScale;
            item.Height = (int)(40 * _dpiScale);
            item.Margin = new Padding(0, (int)(2 * _dpiScale), 0, (int)(2 * _dpiScale));
            item.Click += delegate { SelectSection(section); };
            host.RowCount++;
            host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            host.Controls.Add(item, 0, host.RowCount - 1);
            list.Add(item);
        }

        private void SelectSection(Section section)
        {
            _section = section;

            for (int i = 0; i < _navItems.Count; i++)
            {
                _navItems[i].Selected = (Section)_navItems[i].Tag == section;
                _navItems[i].Invalidate();
            }
            for (int i = 0; i < _bottomItems.Count; i++)
            {
                _bottomItems[i].Selected = (Section)_bottomItems[i].Tag == section;
                _bottomItems[i].Invalidate();
            }

            _sectionTitle.Text = SectionTitle(section);
            _sectionSubtitle.Text = SectionSubtitle(section);
            RebuildCards();
            _cards.AutoScrollPosition = new Point(0, 0);
        }

        private void ClearCards()
        {
            for (int i = _cards.Controls.Count - 1; i >= 0; i--)
            {
                Control control = _cards.Controls[i];
                _cards.Controls.RemoveAt(i);
                control.Dispose();
            }
        }

        private void RebuildCards()
        {
            if (_cards == null)
            {
                return;
            }

            _loading = true;
            ClearCards();

            switch (_section)
            {
                case Section.Appearance:
                    BuildAppearanceCards();
                    break;
                case Section.Color:
                    BuildColorCards();
                    break;
                case Section.Halo:
                    BuildHaloCards();
                    break;
                case Section.Behavior:
                    BuildBehaviorCards();
                    break;
                case Section.Language:
                    BuildLanguageCards();
                    break;
                case Section.About:
                    BuildAboutCards();
                    break;
                case Section.GitHub:
                    BuildGitHubCards();
                    break;
            }

            _loading = false;
            _cards.PerformLayout();
            LayoutCards();
        }

        private void LayoutCards()
        {
            if (_cards == null)
            {
                return;
            }

            int width = _cards.ClientSize.Width - _cards.Padding.Horizontal -
                        SystemInformation.VerticalScrollBarWidth - 4;
            if (width < 240)
            {
                width = Math.Max(200, _cards.ClientSize.Width - _cards.Padding.Horizontal);
            }
            for (int i = 0; i < _cards.Controls.Count; i++)
            {
                Control card = _cards.Controls[i];
                card.MinimumSize = new Size(width, 0);
                card.Width = width;
                card.PerformLayout();
            }
        }

        private void AddCard(string title, string subtitle, Control content, string note = null)
        {
            RoundedPanel card = new RoundedPanel();
            card.Radius = (int)(8 * _dpiScale);
            card.FillColor = MaterialColors.Card;
            card.BorderColor = MaterialColors.Stroke;
            card.BorderWidth = 1f;
            card.Padding = new Padding(
                (int)(18 * _dpiScale), (int)(14 * _dpiScale),
                (int)(18 * _dpiScale), (int)(16 * _dpiScale));
            card.AutoSize = true;
            card.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            card.MinimumSize = new Size((int)(240 * _dpiScale), 0);
            card.Margin = new Padding(0, 0, 0, (int)(8 * _dpiScale));

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.AutoSize = true;
            layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            layout.Dock = DockStyle.Top;
            layout.ColumnCount = 2;
            layout.RowCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.BackColor = MaterialColors.SurfaceContainer;

            Label titleLabel = new Label();
            titleLabel.Text = title;
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font(Font.FontFamily, 13f, FontStyle.Bold);
            titleLabel.ForeColor = MaterialColors.OnSurface;
            titleLabel.BackColor = MaterialColors.Card;
            titleLabel.Margin = new Padding(0, 0, 0, 2);
            layout.Controls.Add(titleLabel, 0, 0);

            if (!string.IsNullOrEmpty(note))
            {
                InfoDot dot = MakeInfoDot(note);
                dot.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                dot.Margin = new Padding(0, 0, 0, 0);
                layout.Controls.Add(dot, 1, 0);
            }

            Label subtitleLabel = new Label();
            subtitleLabel.Text = subtitle;
            subtitleLabel.AutoSize = true;
            subtitleLabel.Font = new Font(Font.FontFamily, 9f);
            subtitleLabel.ForeColor = MaterialColors.OnSurfaceVariant;
            subtitleLabel.BackColor = MaterialColors.Card;
            subtitleLabel.Margin = new Padding(0, 0, 0, 0);
            layout.Controls.Add(subtitleLabel, 0, 1);

            content.Dock = DockStyle.Top;
            content.Margin = new Padding(0, 8, 0, 0);
            if (content.BackColor == Color.Empty || content.BackColor == Color.Transparent)
            {
                content.BackColor = MaterialColors.SurfaceContainer;
            }
            layout.Controls.Add(content, 0, 2);
            layout.SetColumnSpan(content, 2);

            card.Controls.Add(layout);

            _cards.Controls.Add(card);
        }

        private InfoDot MakeInfoDot(string note)
        {
            InfoDot dot = new InfoDot();
            dot.UiScale = _dpiScale;
            dot.Width = (int)(18 * _dpiScale);
            dot.Height = (int)(18 * _dpiScale);
            dot.BackColor = MaterialColors.Card;
            dot.Click += delegate { _toolTip.Show(note, dot, 10000); };
            _toolTip.SetToolTip(dot, note);
            return dot;
        }

        private void BuildAppearanceCards()
        {
            MaterialToggle enable = new MaterialToggle();
            enable.Text = L10n.T("启用鼠标拖尾", "Enable mouse trail");
            enable.Checked = _cfg.Enabled;
            enable.Width = 320;
            enable.Height = (int)(32 * _dpiScale);
            enable.UiScale = _dpiScale;
            enable.CheckedChanged += delegate
            {
                if (_loading)
                {
                    return;
                }
                _cfg.Enabled = enable.Checked;
                _cfg.Save();
                _changed(false);
            };
            AddCard(L10n.T("特效开关", "Effect"), L10n.T("关闭后覆盖层不再绘制拖尾。",
                "When off, the overlay stops drawing the trail."), enable);

            FlowLayoutPanel styles = new FlowLayoutPanel();
            styles.AutoSize = true;
            styles.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            styles.Dock = DockStyle.Top;
            styles.WrapContents = true;
            styles.BackColor = MaterialColors.SurfaceContainer;

            AddStyleCard(styles, "comet", L10n.T("流光", "Comet"),
                L10n.T("带光晕的连续拖尾", "Continuous glowing trail"));
            AddStyleCard(styles, "rainbow", L10n.T("彩虹", "Rainbow"),
                L10n.T("沿轨迹循环变色", "Cycling rainbow colors"));
            AddStyleCard(styles, "sparks", L10n.T("星火", "Sparks"),
                L10n.T("跟随鼠标的星火粒子", "Spark particles around the cursor"));
            AddStyleCard(styles, "minimal", L10n.T("细线", "Minimal"),
                L10n.T("干净的单线轨迹", "Clean thin line"));
            AddCard(L10n.T("样式", "Style"), L10n.T("选择拖尾的渲染方式。",
                "Choose how the trail is rendered."), styles);
            BuildThicknessDurationCard();
            BuildStyleParametersCard();
        }

        private void BuildThicknessDurationCard()
        {
            TableLayoutPanel content = new TableLayoutPanel();
            content.AutoSize = true;
            content.Dock = DockStyle.Top;
            content.ColumnCount = 1;
            content.RowCount = 0;
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            content.BackColor = MaterialColors.SurfaceContainer;

            AddParamRow(content, L10n.T("粗细", "Thickness"), 0.5, 2.0, 0.05, _cfg.Scale,
                delegate(double v) { return v.ToString("0.00") + "×"; },
                delegate(double v) { _cfg.Scale = (float)v; });
            AddParamRow(content, L10n.T("时长", "Duration"), 0.2, 1.5, 0.05, _cfg.Life,
                delegate(double v) { return v.ToString("0.00") + L10n.T(" 秒", " s"); },
                delegate(double v) { _cfg.Life = v; });

            AddCard(L10n.T("粗细与时长", "Thickness and duration"),
                L10n.T("调整拖尾的线宽和停留时间。",
                    "Adjust the trail width and how long it lasts."), content,
                L10n.T("过高的参数可能引起严重卡顿，请自行斟酌。",
                    "Very high settings can cause severe lag; adjust with care."));
        }

        private void BuildStyleParametersCard()
        {
            TableLayoutPanel content = new TableLayoutPanel();
            content.AutoSize = true;
            content.Dock = DockStyle.Top;
            content.ColumnCount = 1;
            content.RowCount = 0;
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            content.BackColor = MaterialColors.SurfaceContainer;

            switch (_cfg.Style)
            {
                case "comet":
                    Label moved = new Label();
                    moved.AutoSize = true;
                    moved.MaximumSize = new Size((int)(720 * _dpiScale), 0);
                    moved.Text = L10n.T("光晕已移至「光圈」选项；流光的光晕会跟随拖尾，拖尾淡出时一起消失。",
                        "The glow has moved to the Halo page; the comet glow follows the trail and fades with it.");
                    moved.Font = new Font(Font.FontFamily, 9f);
                    moved.ForeColor = MaterialColors.OnSurfaceVariant;
                    moved.BackColor = MaterialColors.SurfaceContainer;
                    moved.Margin = new Padding(0, 0, 0, (int)(6 * _dpiScale));
                    AddContentRow(content, moved);

                    MaterialButton openHalo = new MaterialButton();
                    openHalo.Text = L10n.T("前往光圈", "Open Halo");
                    openHalo.FillColor = MaterialColors.PrimaryContainer;
                    openHalo.TextColor = MaterialColors.OnPrimaryContainer;
                    openHalo.Click += delegate { SelectSection(Section.Halo); };
                    AddContentRow(content, openHalo);
                    break;
                case "rainbow":
                    AddRainbowRows(content);
                    break;
                case "sparks":
                    AddParamRow(content, L10n.T("密度", "Rate"), 0.2, 2.0, 0.05, _cfg.SparkRate,
                        delegate(double v) { return v.ToString("0.00") + "×"; },
                        delegate(double v) { _cfg.SparkRate = v; });
                    AddParamRow(content, L10n.T("大小", "Size"), 0.5, 2.0, 0.05, _cfg.SparkSize,
                        delegate(double v) { return v.ToString("0.00") + "×"; },
                        delegate(double v) { _cfg.SparkSize = v; });
                    AddParamRow(content, L10n.T("寿命", "Life"), 0.5, 2.0, 0.05, _cfg.SparkLife,
                        delegate(double v) { return v.ToString("0.00") + "×"; },
                        delegate(double v) { _cfg.SparkLife = v; });
                    AddParamRow(content, L10n.T("速度", "Speed"), 0.5, 2.0, 0.05, _cfg.SparkSpeed,
                        delegate(double v) { return v.ToString("0.00") + "×"; },
                        delegate(double v) { _cfg.SparkSpeed = v; });
                    break;
                case "minimal":
                    AddParamRow(content, L10n.T("线宽", "Width"), 0.5, 2.0, 0.05, _cfg.MinimalWidth,
                        delegate(double v) { return v.ToString("0.00") + "×"; },
                        delegate(double v) { _cfg.MinimalWidth = v; });
                    AddParamRow(content, L10n.T("不透明度", "Opacity"), 0.3, 1.0, 0.05, _cfg.MinimalOpacity,
                        delegate(double v) { return Math.Round(v * 100) + "%"; },
                        delegate(double v) { _cfg.MinimalOpacity = v; });
                    break;
            }

            if (content.Controls.Count == 0)
            {
                return;
            }

            string styleNote = _cfg.Style == "comet"
                ? L10n.T("请注意，流光外侧的尾焰光晕受光晕开关控制，宽度固定；这是已知问题，后续版本会修复并独立。",
                    "Note: the comet's outer trail glow is still controlled by the Halo glow switch and has a fixed width; this is a known issue and will be separated in a later version.")
                : null;
            AddCard(L10n.T("样式参数", "Style parameters"),
                L10n.T("调整当前样式的特效。", "Tune the effects of the current style."), content, styleNote);
        }

        private void AddRainbowRows(TableLayoutPanel host)
        {
            AddParamRow(host, L10n.T("流速", "Speed"), 0.1, 2.0, 0.05, _cfg.RainbowSpeed,
                delegate(double v) { return v.ToString("0.00") + "×"; },
                delegate(double v) { _cfg.RainbowSpeed = v; });
            AddParamRow(host, L10n.T("饱和度", "Saturation"), 0.2, 1.0, 0.05, _cfg.RainbowSaturation,
                delegate(double v) { return Math.Round(v * 100) + "%"; },
                delegate(double v) { _cfg.RainbowSaturation = v; });
            AddParamRow(host, L10n.T("色相偏移", "Hue offset"), 0.0, 1.0, 0.01, _cfg.RainbowOffset,
                delegate(double v) { return v.ToString("0.00"); },
                delegate(double v) { _cfg.RainbowOffset = v; });
        }

        private void AddParamRow(TableLayoutPanel host, string label, double min, double max,
            double step, double value, Func<double, string> format, Action<double> apply)
        {
            TableLayoutPanel row = new TableLayoutPanel();
            row.AutoSize = true;
            row.Dock = DockStyle.Top;
            row.ColumnCount = 3;
            row.RowCount = 1;
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            row.BackColor = MaterialColors.SurfaceContainer;
            row.Margin = new Padding(0, 0, 0, (int)(2 * _dpiScale));

            Label name = new Label();
            name.AutoSize = true;
            name.Text = label;
            name.Font = new Font(Font.FontFamily, 10f);
            name.ForeColor = MaterialColors.OnSurfaceVariant;
            name.BackColor = MaterialColors.Card;
            name.Anchor = AnchorStyles.Left;
            name.Margin = new Padding(0, (int)(10 * _dpiScale), (int)(12 * _dpiScale), 0);
            row.Controls.Add(name, 0, 0);

            MaterialSlider slider = new MaterialSlider();
            slider.Minimum = min;
            slider.Maximum = max;
            slider.Step = step;
            slider.Value = value;
            slider.UiScale = _dpiScale;
            slider.Height = (int)(32 * _dpiScale);
            slider.Dock = DockStyle.Top;
            slider.Margin = new Padding(0, (int)(4 * _dpiScale), (int)(10 * _dpiScale), 0);
            row.Controls.Add(slider, 1, 0);

            Label val = new Label();
            val.AutoSize = true;
            val.Text = format(value);
            val.Font = new Font(Font.FontFamily, 11f, FontStyle.Bold);
            val.ForeColor = MaterialColors.Primary;
            val.BackColor = MaterialColors.Card;
            val.Anchor = AnchorStyles.Right;
            val.TextAlign = ContentAlignment.MiddleRight;
            val.Margin = new Padding(0, (int)(7 * _dpiScale), 0, 0);
            row.Controls.Add(val, 2, 0);

            slider.ValueChanged += delegate
            {
                if (_loading)
                {
                    return;
                }
                apply(slider.Value);
                val.Text = format(slider.Value);
                _changed(false);
            };
            slider.ValueCommitted += delegate { _cfg.Save(); };

            host.RowCount++;
            host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            host.Controls.Add(row, 0, host.RowCount - 1);
        }

        private void AddStyleCard(FlowLayoutPanel host, string key, string title, string subtitle)
        {
            ChoiceCard card = new ChoiceCard();
            card.Title = title;
            card.Subtitle = subtitle;
            card.Selected = string.Equals(_cfg.Style, key, StringComparison.Ordinal);
            card.UiScale = _dpiScale;
            card.Width = (int)(160 * _dpiScale);
            card.Height = (int)(92 * _dpiScale);
            card.Click += delegate
            {
                if (_loading)
                {
                    return;
                }
                _cfg.Style = key;
                _cfg.Save();
                _changed(false);
                BeginInvoke(new MethodInvoker(RebuildCards));
            };
            if (key == "rainbow")
            {
                InfoDot dot = MakeInfoDot(L10n.T("本软件的彩虹是随着鼠标的运动而改变的，并非自行跳动。",
                    "The rainbow follows mouse movement; it does not animate by itself."));
                dot.BackColor = card.Selected ? MaterialColors.SelectedTint : MaterialColors.Card;
                dot.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                dot.Location = new Point(card.Width - dot.Width - (int)(8 * _dpiScale),
                    (int)(8 * _dpiScale));
                dot.Margin = new Padding(0);
                card.Controls.Add(dot);
            }
            host.Controls.Add(card);
        }

        private void BuildRainbowColorCard()
        {
            TableLayoutPanel content = new TableLayoutPanel();
            content.AutoSize = true;
            content.Dock = DockStyle.Top;
            content.ColumnCount = 1;
            content.RowCount = 0;
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            content.BackColor = MaterialColors.SurfaceContainer;

            Label note = new Label();
            note.AutoSize = true;
            note.Text = L10n.T("彩虹模式使用 HSV 循环，颜色色板不参与渲染。请到“外观 → 样式参数 → 彩虹”中调整流速、饱和度和色相偏移。",
                "Rainbow mode uses an HSV cycle; the color palette is not used. Adjust speed, saturation and hue offset in Appearance → Style parameters → Rainbow.");
            note.MaximumSize = new Size((int)(760 * _dpiScale), 0);
            note.ForeColor = MaterialColors.OnSurfaceVariant;
            note.BackColor = MaterialColors.SurfaceContainer;
            note.Font = new Font(Font.FontFamily, 9f);
            note.Margin = new Padding(0, 0, 0, (int)(10 * _dpiScale));
            content.RowCount++;
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.Controls.Add(note, 0, content.RowCount - 1);

            MaterialButton go = new MaterialButton();
            go.Text = L10n.T("前往外观", "Go to Appearance");
            go.FillColor = MaterialColors.Primary;
            go.TextColor = MaterialColors.OnPrimary;
            go.Click += delegate { SelectSection(Section.Appearance); };
            content.RowCount++;
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.Controls.Add(go, 0, content.RowCount - 1);

            AddCard(L10n.T("彩虹模式", "Rainbow mode"),
                L10n.T("颜色不参与彩虹模式。", "Color is not used in rainbow mode."), content);
        }
        private void BuildColorCards()
        {
            if (_cfg.Style == "rainbow")
            {
                BuildRainbowColorCard();
                return;
            }

            FlowLayoutPanel flow = new FlowLayoutPanel();
            flow.AutoSize = false;
            flow.Height = (int)((46 + 10) * _dpiScale);
            flow.Dock = DockStyle.Top;
            flow.WrapContents = true;
            flow.BackColor = MaterialColors.SurfaceContainer;

            for (int i = 0; i < Palette.Definitions.Length; i++)
            {
                ColorDef def = Palette.Definitions[i];
                ColorSwatch swatch = new ColorSwatch();
                swatch.SwatchColor = def.Value;
                swatch.Selected = string.Equals(_cfg.ColorName, def.Key, StringComparison.Ordinal);
                swatch.ColorName = def.Key;
                swatch.UiScale = _dpiScale;
                swatch.Width = (int)(46 * _dpiScale);
                swatch.Height = (int)(46 * _dpiScale);
                _toolTip.SetToolTip(swatch, L10n.T(def.Zh, def.En));
                string key = def.Key;
                swatch.Click += delegate
                {
                    if (_loading)
                    {
                        return;
                    }
                    _cfg.ColorName = key;
                    _cfg.Save();
                    _changed(false);
                    BeginInvoke(new MethodInvoker(RebuildCards));
                };
                flow.Controls.Add(swatch);
            }

            Label current = new Label();
            current.AutoSize = true;
            current.Text = L10n.T("当前颜色：", "Current color: ") + Palette.Display(_cfg.ColorName);
            current.ForeColor = MaterialColors.OnSurfaceVariant;
            current.BackColor = MaterialColors.SurfaceContainer;
            current.Font = new Font(Font.FontFamily, 9f);
            current.Margin = new Padding(0, 8, 0, 0);

            TableLayoutPanel content = new TableLayoutPanel();
            content.AutoSize = true;
            content.Dock = DockStyle.Top;
            content.ColumnCount = 1;
            content.RowCount = 2;
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.BackColor = MaterialColors.SurfaceContainer;
            content.Controls.Add(flow, 0, 0);
            content.Controls.Add(current, 0, 1);

            AddCard(L10n.T("颜色", "Color"), L10n.T("选择拖尾的主色。",
                "Pick the main color of the trail."), content);
        }

        private void BuildHaloCards()
        {
            TableLayoutPanel glowContent = new TableLayoutPanel();
            glowContent.AutoSize = true;
            glowContent.Dock = DockStyle.Top;
            glowContent.ColumnCount = 1;
            glowContent.RowCount = 0;
            glowContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            glowContent.BackColor = MaterialColors.SurfaceContainer;

            MaterialToggle glowToggle = new MaterialToggle();
            glowToggle.Text = L10n.T("显示光晕", "Show glow");
            glowToggle.Checked = _cfg.GlowEnabled;
            glowToggle.Width = (int)(360 * _dpiScale);
            glowToggle.Height = (int)(34 * _dpiScale);
            glowToggle.UiScale = _dpiScale;
            glowToggle.Margin = new Padding(0, 0, 0, (int)(4 * _dpiScale));
            glowToggle.CheckedChanged += delegate
            {
                if (_loading)
                {
                    return;
                }
                _cfg.GlowEnabled = glowToggle.Checked;
                _cfg.Save();
                _changed(false);
            };
            AddContentRow(glowContent, glowToggle);

            AddParamRow(glowContent, L10n.T("半径", "Radius"), 8, 120, 1, _cfg.GlowRadius,
                delegate(double v) { return v.ToString("0") + " px"; },
                delegate(double v) { _cfg.GlowRadius = v; });
            AddParamRow(glowContent, L10n.T("强度", "Strength"), 0.0, 2.0, 0.05, _cfg.GlowStrength,
                delegate(double v) { return v.ToString("0.00") + "×"; },
                delegate(double v) { _cfg.GlowStrength = v; });
            AddHaloColorRow(glowContent, _cfg.GlowColor,
                delegate(string key) { _cfg.GlowColor = key; });

            AddCard(L10n.T("光晕", "Glow"),
                L10n.T("流光：光晕跟随拖尾并在淡出时消失；其他样式：光晕跟随光标。",
                    "Comet: the glow follows the trail and fades with it; other styles: the glow follows the cursor."), glowContent);

            TableLayoutPanel haloContent = new TableLayoutPanel();
            haloContent.AutoSize = true;
            haloContent.Dock = DockStyle.Top;
            haloContent.ColumnCount = 1;
            haloContent.RowCount = 0;
            haloContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            haloContent.BackColor = MaterialColors.SurfaceContainer;

            MaterialToggle haloToggle = new MaterialToggle();
            haloToggle.Text = L10n.T("显示光圈", "Show halo");
            haloToggle.Checked = _cfg.HaloEnabled;
            haloToggle.Width = (int)(360 * _dpiScale);
            haloToggle.Height = (int)(34 * _dpiScale);
            haloToggle.UiScale = _dpiScale;
            haloToggle.Margin = new Padding(0, 0, 0, (int)(4 * _dpiScale));
            haloToggle.CheckedChanged += delegate
            {
                if (_loading)
                {
                    return;
                }
                _cfg.HaloEnabled = haloToggle.Checked;
                _cfg.Save();
                _changed(false);
            };
            AddContentRow(haloContent, haloToggle);

            AddParamRow(haloContent, L10n.T("半径", "Radius"), 8, 80, 1, _cfg.HaloRadius,
                delegate(double v) { return v.ToString("0") + " px"; },
                delegate(double v) { _cfg.HaloRadius = v; });
            AddParamRow(haloContent, L10n.T("粗细", "Thickness"), 1, 10, 0.5, _cfg.HaloThickness,
                delegate(double v) { return v.ToString("0.0") + " px"; },
                delegate(double v) { _cfg.HaloThickness = v; });
            AddParamRow(haloContent, L10n.T("不透明度", "Opacity"), 0.1, 1, 0.05, _cfg.HaloOpacity,
                delegate(double v) { return Math.Round(v * 100) + "%"; },
                delegate(double v) { _cfg.HaloOpacity = v; });
            AddHaloColorRow(haloContent, _cfg.HaloColor,
                delegate(string key) { _cfg.HaloColor = key; });

            AddCard(L10n.T("光圈", "Halo"),
                L10n.T("围绕鼠标的独立圆环，不受拖尾样式影响。",
                    "An independent ring around the cursor, unaffected by the trail style."), haloContent,
                L10n.T("光圈可能带来性能问题。", "The halo ring may cause performance issues."));
        }

        private void AddHaloColorRow(TableLayoutPanel host, string current, Action<string> apply)
        {
            TableLayoutPanel row = new TableLayoutPanel();
            row.AutoSize = true;
            row.Dock = DockStyle.Top;
            row.ColumnCount = 2;
            row.RowCount = 1;
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            row.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            row.BackColor = MaterialColors.SurfaceContainer;
            row.Margin = new Padding(0, (int)(2 * _dpiScale), 0, (int)(2 * _dpiScale));

            Label name = new Label();
            name.AutoSize = true;
            name.Text = L10n.T("颜色", "Color");
            name.Font = new Font(Font.FontFamily, 10f);
            name.ForeColor = MaterialColors.OnSurfaceVariant;
            name.BackColor = MaterialColors.Card;
            name.Anchor = AnchorStyles.Left;
            name.Margin = new Padding(0, (int)(10 * _dpiScale), (int)(12 * _dpiScale), 0);
            row.Controls.Add(name, 0, 0);

            FlowLayoutPanel flow = new FlowLayoutPanel();
            flow.AutoSize = true;
            flow.WrapContents = false;
            flow.BackColor = MaterialColors.SurfaceContainer;
            flow.Margin = new Padding(0, (int)(4 * _dpiScale), 0, 0);

            bool auto = string.Equals(current, "auto", StringComparison.Ordinal);
            MaterialButton autoButton = new MaterialButton();
            autoButton.Text = L10n.T("跟随", "Auto");
            autoButton.Font = new Font(Font.FontFamily, 9f);
            autoButton.FillColor = auto ? MaterialColors.PrimaryContainer : MaterialColors.SurfaceContainerHigh;
            autoButton.TextColor = auto ? MaterialColors.OnPrimaryContainer : MaterialColors.OnSurfaceVariant;
            autoButton.BorderColor = MaterialColors.OutlineVariant;
            autoButton.Height = (int)(30 * _dpiScale);
            autoButton.Margin = new Padding(0, 0, (int)(8 * _dpiScale), (int)(6 * _dpiScale));
            autoButton.Click += delegate
            {
                if (_loading)
                {
                    return;
                }
                apply("auto");
                _cfg.Save();
                _changed(false);
                BeginInvoke(new MethodInvoker(RebuildCards));
            };
            flow.Controls.Add(autoButton);

            for (int i = 0; i < Palette.Definitions.Length; i++)
            {
                ColorDef def = Palette.Definitions[i];
                ColorSwatch swatch = new ColorSwatch();
                swatch.SwatchColor = def.Value;
                swatch.Selected = string.Equals(current, def.Key, StringComparison.Ordinal);
                swatch.ColorName = def.Key;
                swatch.UiScale = _dpiScale;
                swatch.Width = (int)(34 * _dpiScale);
                swatch.Height = (int)(34 * _dpiScale);
                swatch.Margin = new Padding(0, 0, (int)(8 * _dpiScale), (int)(6 * _dpiScale));
                _toolTip.SetToolTip(swatch, L10n.T(def.Zh, def.En));
                string key = def.Key;
                swatch.Click += delegate
                {
                    if (_loading)
                    {
                        return;
                    }
                    apply(key);
                    _cfg.Save();
                    _changed(false);
                    BeginInvoke(new MethodInvoker(RebuildCards));
                };
                flow.Controls.Add(swatch);
            }

            row.Controls.Add(flow, 1, 0);
            host.RowCount++;
            host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            host.Controls.Add(row, 0, host.RowCount - 1);
        }

        private void AddContentRow(TableLayoutPanel host, Control control)
        {
            host.RowCount++;
            host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            host.Controls.Add(control, 0, host.RowCount - 1);
        }

        private void BuildBehaviorCards()
        {
            FlowLayoutPanel toggles = new FlowLayoutPanel();
            toggles.AutoSize = true;
            toggles.Dock = DockStyle.Top;
            toggles.FlowDirection = FlowDirection.TopDown;
            toggles.WrapContents = false;
            toggles.BackColor = MaterialColors.SurfaceContainer;

            MaterialToggle startup = new MaterialToggle();
            startup.Text = L10n.T("开机自动启动", "Start with Windows");
            startup.Checked = AutoStart.IsEnabled();
            startup.Width = 360;
            startup.Height = (int)(34 * _dpiScale);
            startup.UiScale = _dpiScale;
            startup.CheckedChanged += delegate
            {
                if (_loading)
                {
                    return;
                }
                AutoStart.Set(startup.Checked);
                _changed(false);
            };
            toggles.Controls.Add(startup);

            MaterialToggle tips = new MaterialToggle();
            tips.Text = L10n.T("首次启动显示提示", "Show a tip on first launch");
            tips.Checked = _cfg.ShowTip;
            tips.Width = 360;
            tips.Height = (int)(34 * _dpiScale);
            tips.UiScale = _dpiScale;
            tips.Margin = new Padding(0, 6, 0, 0);
            tips.CheckedChanged += delegate
            {
                if (_loading)
                {
                    return;
                }
                _cfg.ShowTip = tips.Checked;
                _cfg.Save();
                _changed(false);
            };
            toggles.Controls.Add(tips);

            AddCard(L10n.T("启动与提示", "Startup and tips"), L10n.T("控制开机启动和首次提示。",
                "Control Windows startup and the first-launch tip."), toggles);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.AutoSize = true;
            actions.Dock = DockStyle.Top;
            actions.WrapContents = true;
            actions.BackColor = MaterialColors.SurfaceContainer;

            MaterialButton reset = new MaterialButton();
            reset.Text = L10n.T("恢复默认设置", "Reset to defaults");
            reset.FillColor = MaterialColors.SecondaryContainer;
            reset.TextColor = MaterialColors.OnSecondaryContainer;
            reset.BorderColor = MaterialColors.Stroke;
            reset.Click += delegate
            {
                using (ConfirmDialog dialog = new ConfirmDialog(
                    L10n.T("恢复默认设置", "Reset to defaults"),
                    L10n.T("这会重置所有外观、颜色、光圈和行为设置，且无法撤销。",
                        "This resets all appearance, color, halo and behavior settings. This cannot be undone."),
                    L10n.T("确认恢复", "Reset"),
                    L10n.T("取消", "Cancel"),
                    _dpiScale))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }
                }
                _cfg.Reset();
                _cfg.Enabled = true;
                _cfg.Save();
                _changed(false);
                BeginInvoke(new MethodInvoker(RebuildCards));
            };
            actions.Controls.Add(reset);

            MaterialButton folder = new MaterialButton();
            folder.Text = L10n.T("打开配置文件夹", "Open config folder");
            folder.FillColor = MaterialColors.SurfaceContainerHigh;
            folder.TextColor = MaterialColors.OnSurface;
            folder.BorderColor = MaterialColors.Stroke;
            folder.Margin = new Padding(8, 0, 0, 0);
            folder.Click += delegate
            {
                try
                {
                    System.IO.Directory.CreateDirectory(AppSettings.Folder);
                    Process.Start("explorer.exe", "\"" + AppSettings.Folder + "\"");
                }
                catch
                {
                }
            };
            actions.Controls.Add(folder);

            AddCard(L10n.T("操作", "Actions"), L10n.T("重置设置或查看配置文件。",
                "Reset settings or open the config folder."), actions);
        }

        private void BuildLanguageCards()
        {
            FlowLayoutPanel flow = new FlowLayoutPanel();
            flow.AutoSize = false;
            flow.Height = (int)((46 + 10) * _dpiScale);
            flow.Dock = DockStyle.Top;
            flow.WrapContents = true;
            flow.BackColor = MaterialColors.SurfaceContainer;

            AddLanguageButton(flow, "auto", L10n.T("自动", "Auto"));
            AddLanguageButton(flow, "zh", "中文");
            AddLanguageButton(flow, "en", "English");

            Label note = new Label();
            note.AutoSize = true;
            note.Text = L10n.T("“自动”会跟随系统语言。", "“Auto” follows the system language.");
            note.ForeColor = MaterialColors.OnSurfaceVariant;
            note.BackColor = MaterialColors.SurfaceContainer;
            note.Font = new Font(Font.FontFamily, 9f);
            note.Margin = new Padding(0, 10, 0, 0);

            TableLayoutPanel content = new TableLayoutPanel();
            content.AutoSize = true;
            content.Dock = DockStyle.Top;
            content.ColumnCount = 1;
            content.RowCount = 2;
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.BackColor = MaterialColors.SurfaceContainer;
            content.Controls.Add(flow, 0, 0);
            content.Controls.Add(note, 0, 1);

            AddCard(L10n.T("界面语言", "Language"), L10n.T("应用界面和托盘菜单使用的语言。",
                "Language used by the app and the tray menu."), content);
        }

        private void AddLanguageButton(FlowLayoutPanel host, string value, string text)
        {
            bool selected = string.Equals(_cfg.Language, value, StringComparison.Ordinal);
            MaterialButton button = new MaterialButton();
            button.Text = text;
            button.FillColor = selected ? MaterialColors.PrimaryContainer : MaterialColors.SurfaceContainerHigh;
            button.TextColor = selected ? MaterialColors.OnPrimaryContainer : MaterialColors.OnSurface;
            button.BorderColor = selected ? MaterialColors.Accent : MaterialColors.Stroke;
            button.Radius = 4;
            button.Margin = new Padding(0, 0, 8, 8);
            button.Click += delegate
            {
                if (_loading)
                {
                    return;
                }
                _cfg.Language = value;
                _cfg.Save();
                L10n.Set(_cfg.ResolveLanguage());
                _changed(true);
                BeginInvoke(new MethodInvoker(ApplyLanguage));
            };
            host.Controls.Add(button);
        }

        private void BuildAboutCards()
        {
            TableLayoutPanel content = new TableLayoutPanel();
            content.AutoSize = true;
            content.Dock = DockStyle.Top;
            content.ColumnCount = 2;
            content.RowCount = 4;
            content.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.BackColor = MaterialColors.SurfaceContainer;

            try
            {
                Icon icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (icon != null)
                {
                    PictureBox picture = new PictureBox();
                    picture.Image = icon.ToBitmap();
                    picture.Size = new Size((int)(64 * _dpiScale), (int)(64 * _dpiScale));
                    picture.SizeMode = PictureBoxSizeMode.StretchImage;
                    picture.Margin = new Padding(0, 0, 16, 0);
                    content.Controls.Add(picture, 0, 0);
                }
            }
            catch
            {
            }

            Label name = new Label();
            name.AutoSize = true;
            name.Text = "Cursor Comet";
            name.Font = new Font(Font.FontFamily, 20f, FontStyle.Bold);
            name.ForeColor = MaterialColors.OnSurface;
            name.BackColor = MaterialColors.Card;
            name.Margin = new Padding(0, 0, 0, 2);
            content.Controls.Add(name, 1, 0);

            Label version = new Label();
            version.AutoSize = true;
            version.Text = "v" + AppVersion + " · GPL-3.0";
            version.Font = new Font(Font.FontFamily, 9.5f);
            version.ForeColor = MaterialColors.OnSurfaceVariant;
            version.BackColor = MaterialColors.Card;
            content.Controls.Add(version, 1, 1);

            Label description = new Label();
            description.AutoSize = true;
            description.MaximumSize = new Size(620, 0);
            description.Text = L10n.T("Windows 鼠标拖尾特效：全屏透明覆盖、四种样式、按时间淡出，支持高 DPI。",
                "Windows cursor trail effect: full-screen transparent overlay, four styles, time-based fading and high-DPI support.");
            description.ForeColor = MaterialColors.OnSurfaceVariant;
            description.BackColor = MaterialColors.Card;
            description.Margin = new Padding(0, 8, 0, 4);
            content.Controls.Add(description, 1, 2);

            Label config = new Label();
            config.AutoSize = true;
            config.MaximumSize = new Size(620, 0);
            config.Text = L10n.T("配置文件：", "Config file: ") + AppSettings.FilePath;
            config.ForeColor = MaterialColors.OnSurfaceVariant;
            config.Font = new Font(Font.FontFamily, 9f);
            config.BackColor = MaterialColors.SurfaceContainer;
            content.Controls.Add(config, 1, 3);

            Label star = new Label();
            star.AutoSize = true;
            star.MaximumSize = new Size((int)(760 * _dpiScale), 0);
            star.Text = L10n.T("如果觉得好用，可以给个 Star 吗 ⭐️✨",
                "If you find it useful, could you give it a Star? ⭐️✨");
            star.Font = new Font(Font.FontFamily, 10f);
            star.ForeColor = MaterialColors.Primary;
            star.BackColor = MaterialColors.SurfaceContainer;
            star.Cursor = Cursors.Hand;
            star.Margin = new Padding(0, (int)(14 * _dpiScale), 0, 0);
            star.Click += delegate { OpenRepo(); };
            _toolTip.SetToolTip(star, L10n.T("打开 GitHub 仓库", "Open the GitHub repository"));
            content.RowCount++;
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.Controls.Add(star, 0, content.RowCount - 1);
            content.SetColumnSpan(star, 2);

            AddCard(L10n.T("关于 Cursor Comet", "About Cursor Comet"),
                L10n.T("版本、许可证和配置位置。", "Version, license and config location."), content);
        }

        private void BuildGitHubCards()
        {
            TableLayoutPanel content = new TableLayoutPanel();
            content.AutoSize = true;
            content.Dock = DockStyle.Top;
            content.ColumnCount = 1;
            content.RowCount = 3;
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.BackColor = MaterialColors.SurfaceContainer;

            Label repo = new Label();
            repo.AutoSize = true;
            repo.Text = "github.com/cwslz/cursor-comet";
            repo.Font = new Font(Font.FontFamily, 14f, FontStyle.Bold);
            repo.ForeColor = MaterialColors.OnSurface;
            repo.BackColor = MaterialColors.Card;
            content.Controls.Add(repo, 0, 0);

            Label description = new Label();
            description.AutoSize = true;
            description.MaximumSize = new Size(620, 0);
            description.Text = L10n.T("源码、问题反馈和版本发布都在 GitHub 上。",
                "Source code, issues and releases are on GitHub.");
            description.ForeColor = MaterialColors.OnSurfaceVariant;
            description.BackColor = MaterialColors.Card;
            description.Margin = new Padding(0, 6, 0, 10);
            content.Controls.Add(description, 0, 1);

            MaterialButton open = new MaterialButton();
            open.Text = L10n.T("在浏览器中打开", "Open in browser");
            open.FillColor = MaterialColors.Primary;
            open.TextColor = MaterialColors.OnPrimary;
            open.Click += delegate
            {
                try
                {
                    Process.Start(RepoUrl);
                }
                catch
                {
                }
            };
            content.Controls.Add(open, 0, 2);

            AddCard("GitHub", L10n.T("项目主页、源码和反馈。", "Project home, source and issues."), content);
        }

        private void ApplyLanguage()
        {
            Text = L10n.T("鼠标拖尾 · 设置", "Cursor Comet · Settings");
            _appTitle.Text = L10n.T("鼠标拖尾", "Cursor Comet");
            _appSubtitle.Text = L10n.T("Cursor Comet", "Cursor Trail");

            for (int i = 0; i < _navItems.Count; i++)
            {
                _navItems[i].Label = SectionTitle((Section)_navItems[i].Tag);
                _navItems[i].Invalidate();
            }
            for (int i = 0; i < _bottomItems.Count; i++)
            {
                _bottomItems[i].Label = SectionTitle((Section)_bottomItems[i].Tag);
                _bottomItems[i].Invalidate();
            }

            _sectionTitle.Text = SectionTitle(_section);
            _sectionSubtitle.Text = SectionSubtitle(_section);
            RebuildCards();
            UpdateResponsive();
        }

        private void OpenRepo()
        {
            try
            {
                Process.Start(RepoUrl);
            }
            catch
            {
            }
        }

        private void UpdateResponsive()
        {
            if (_root == null)
            {
                return;
            }

            bool collapse = ClientSize.Width < 1000 * _dpiScale;
            if (collapse == _collapsed)
            {
                return;
            }
            _collapsed = collapse;

            _root.ColumnStyles[0].Width = collapse ? 76f * _dpiScale : 236f * _dpiScale;
            _sidebar.Padding = collapse
                ? new Padding((int)(8 * _dpiScale), (int)(12 * _dpiScale),
                    (int)(8 * _dpiScale), (int)(8 * _dpiScale))
                : new Padding((int)(12 * _dpiScale), (int)(16 * _dpiScale),
                    (int)(12 * _dpiScale), (int)(12 * _dpiScale));
            _appTitle.Visible = !collapse;
            _appSubtitle.Visible = !collapse;

            for (int i = 0; i < _navItems.Count; i++)
            {
                _navItems[i].Collapsed = collapse;
                _navItems[i].Invalidate();
            }
            for (int i = 0; i < _bottomItems.Count; i++)
            {
                _bottomItems[i].Collapsed = collapse;
                _bottomItems[i].Invalidate();
            }
            LayoutCards();
        }

        private static string SectionTitle(Section section)
        {
            switch (section)
            {
                case Section.Appearance:
                    return L10n.T("外观", "Appearance");
                case Section.Color:
                    return L10n.T("颜色", "Color");
                case Section.Halo:
                    return L10n.T("光圈", "Halo");
                case Section.Behavior:
                    return L10n.T("行为", "Behavior");
                case Section.Language:
                    return L10n.T("语言", "Language");
                case Section.About:
                    return L10n.T("关于", "About");
                case Section.GitHub:
                    return "GitHub";
                default:
                    return string.Empty;
            }
        }

        private string SectionSubtitle(Section section)
        {
            if (section == Section.Color && _cfg.Style == "rainbow")
            {
                return L10n.T("彩虹模式不使用颜色，请到外观调整彩虹参数。",
                    "Rainbow mode does not use color; adjust rainbow settings in Appearance.");
            }

            switch (section)
            {
                case Section.Appearance:
                    return L10n.T("开关、样式、粗细和时长。", "Toggle, style, thickness and duration.");
                case Section.Color:
                    return L10n.T("选择拖尾的主色。", "Pick the main color of the trail.");
                case Section.Halo:
                    return L10n.T("围绕鼠标的光晕和光圈，独立于拖尾样式。",
                        "Cursor glow and halo ring, independent of the trail style.");
                case Section.Behavior:
                    return L10n.T("启动、提示和重置。", "Startup, tips and reset.");
                case Section.Language:
                    return L10n.T("应用界面和托盘菜单使用的语言。", "Language used by the app and tray menu.");
                case Section.About:
                    return L10n.T("版本、许可证和配置位置。", "Version, license and config location.");
                case Section.GitHub:
                    return L10n.T("项目主页、源码和反馈。", "Project home, source and issues.");
                default:
                    return string.Empty;
            }
        }
    }
}
