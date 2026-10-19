using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
            Thickness,
            Duration,
            Behavior,
            Language,
            About,
            GitHub
        }

        private const string RepoUrl = "https://github.com/cwslz/cursor-comet";
        private const string AppVersion = "1.6.1";

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
            Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 9.5f);
            AutoScaleMode = AutoScaleMode.None;
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
            TopMost = true;
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
        }

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
            _sidebar.BackColor = MaterialColors.SurfaceContainerHigh;
            _sidebar.Padding = new Padding(
                (int)(12 * _dpiScale), (int)(16 * _dpiScale),
                (int)(12 * _dpiScale), (int)(12 * _dpiScale));
            _root.Controls.Add(_sidebar, 0, 0);

            BuildSidebarHeader();
            BuildNavList();
            Panel spacer = new Panel();
            spacer.Dock = DockStyle.Fill;
            spacer.BackColor = MaterialColors.SurfaceContainerHigh;
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
            header.ColumnCount = 1;
            header.RowCount = 2;
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            header.Padding = new Padding(
                (int)(32 * _dpiScale), (int)(26 * _dpiScale),
                (int)(32 * _dpiScale), (int)(8 * _dpiScale));
            header.BackColor = MaterialColors.Surface;

            _sectionTitle = new Label();
            _sectionTitle.AutoSize = true;
            _sectionTitle.Font = new Font(Font.FontFamily, 22f, FontStyle.Bold);
            _sectionTitle.ForeColor = MaterialColors.OnSurface;
            _sectionTitle.BackColor = MaterialColors.Surface;
            _sectionTitle.Margin = new Padding(0, 0, 0, 2);
            header.Controls.Add(_sectionTitle, 0, 0);

            _sectionSubtitle = new Label();
            _sectionSubtitle.AutoSize = true;
            _sectionSubtitle.Font = new Font(Font.FontFamily, 9.5f);
            _sectionSubtitle.ForeColor = MaterialColors.OnSurfaceVariant;
            _sectionSubtitle.BackColor = MaterialColors.Surface;
            _sectionSubtitle.Margin = new Padding(0, 0, 0, 0);
            header.Controls.Add(_sectionSubtitle, 0, 1);
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
            header.BackColor = MaterialColors.SurfaceContainerHigh;

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
            _appTitle.Font = new Font(Font.FontFamily, 12f, FontStyle.Bold);
            _appTitle.ForeColor = MaterialColors.OnSurface;
            _appTitle.BackColor = MaterialColors.SurfaceContainerHigh;
            _appTitle.Margin = new Padding(0, 0, 0, 0);
            text.Controls.Add(_appTitle, 0, 0);

            _appSubtitle = new Label();
            _appSubtitle.AutoSize = true;
            _appSubtitle.Font = new Font(Font.FontFamily, 8.5f);
            _appSubtitle.ForeColor = MaterialColors.OnSurfaceVariant;
            _appSubtitle.BackColor = MaterialColors.SurfaceContainerHigh;
            _appSubtitle.Margin = new Padding(0, 0, 0, 0);
            text.Controls.Add(_appSubtitle, 0, 1);

            header.Controls.Add(text, 1, 0);
            _sidebar.Controls.Add(header, 0, 0);
        }

        private void BuildNavList()
        {
            _navList = new TableLayoutPanel();
            _navList.AutoSize = true;
            _navList.Dock = DockStyle.Top;
            _navList.ColumnCount = 1;
            _navList.RowCount = 0;
            _navList.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _navList.BackColor = MaterialColors.SurfaceContainerHigh;
            _navList.Margin = new Padding(0, 0, 0, 0);
            _sidebar.Controls.Add(_navList, 0, 1);

            AddNav(Section.Appearance, NavIcon.Sparkle, _navList, _navItems);
            AddNav(Section.Color, NavIcon.Palette, _navList, _navItems);
            AddNav(Section.Thickness, NavIcon.Thickness, _navList, _navItems);
            AddNav(Section.Duration, NavIcon.Timer, _navList, _navItems);
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
            _bottomNav.BackColor = MaterialColors.SurfaceContainerHigh;
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
            item.Height = (int)(44 * _dpiScale);
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
                case Section.Thickness:
                    BuildThicknessCards();
                    break;
                case Section.Duration:
                    BuildDurationCards();
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

        private void AddCard(string title, string subtitle, Control content)
        {
            RoundedPanel card = new RoundedPanel();
            card.Radius = (int)(20 * _dpiScale);
            card.FillColor = MaterialColors.SurfaceContainer;
            card.Padding = new Padding(
                (int)(20 * _dpiScale), (int)(16 * _dpiScale),
                (int)(20 * _dpiScale), (int)(18 * _dpiScale));
            card.AutoSize = true;
            card.MinimumSize = new Size((int)(240 * _dpiScale), 0);
            card.Margin = new Padding(0, 0, 0, (int)(12 * _dpiScale));

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.AutoSize = true;
            layout.Dock = DockStyle.Top;
            layout.ColumnCount = 1;
            layout.RowCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.BackColor = MaterialColors.SurfaceContainer;

            Label titleLabel = new Label();
            titleLabel.Text = title;
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font(Font.FontFamily, 11.5f, FontStyle.Bold);
            titleLabel.ForeColor = MaterialColors.OnSurface;
            titleLabel.BackColor = MaterialColors.SurfaceContainer;
            titleLabel.Margin = new Padding(0, 0, 0, 2);
            layout.Controls.Add(titleLabel, 0, 0);

            Label subtitleLabel = new Label();
            subtitleLabel.Text = subtitle;
            subtitleLabel.AutoSize = true;
            subtitleLabel.Font = new Font(Font.FontFamily, 8.5f);
            subtitleLabel.ForeColor = MaterialColors.OnSurfaceVariant;
            subtitleLabel.BackColor = MaterialColors.SurfaceContainer;
            subtitleLabel.Margin = new Padding(0, 0, 0, 0);
            layout.Controls.Add(subtitleLabel, 0, 1);

            content.Dock = DockStyle.Top;
            content.Margin = new Padding(0, 10, 0, 0);
            if (content.BackColor == Color.Empty || content.BackColor == Color.Transparent)
            {
                content.BackColor = MaterialColors.SurfaceContainer;
            }
            layout.Controls.Add(content, 0, 2);

            card.Controls.Add(layout);

            _cards.Controls.Add(card);
        }

        private void BuildAppearanceCards()
        {
            MaterialToggle enable = new MaterialToggle();
            enable.Text = L10n.T("启用鼠标拖尾", "Enable mouse trail");
            enable.Checked = _cfg.Enabled;
            enable.Width = 320;
            enable.Height = (int)(34 * _dpiScale);
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
            BuildStyleParametersCard();
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
                    AddParamRow(content, L10n.T("光晕", "Glow"), 0.0, 1.5, 0.05, _cfg.CometGlow,
                        delegate(double v) { return v.ToString("0.00") + "×"; },
                        delegate(double v) { _cfg.CometGlow = v; });
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

            AddCard(L10n.T("样式参数", "Style parameters"),
                L10n.T("调整当前样式的特效。", "Tune the effects of the current style."), content);
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
            name.Font = new Font(Font.FontFamily, 9f);
            name.ForeColor = MaterialColors.OnSurfaceVariant;
            name.BackColor = MaterialColors.SurfaceContainer;
            name.Anchor = AnchorStyles.Left;
            name.Margin = new Padding(0, (int)(10 * _dpiScale), (int)(12 * _dpiScale), 0);
            row.Controls.Add(name, 0, 0);

            MaterialSlider slider = new MaterialSlider();
            slider.Minimum = min;
            slider.Maximum = max;
            slider.Step = step;
            slider.Value = value;
            slider.UiScale = _dpiScale;
            slider.Height = (int)(30 * _dpiScale);
            slider.Dock = DockStyle.Top;
            slider.Margin = new Padding(0, (int)(4 * _dpiScale), (int)(10 * _dpiScale), 0);
            row.Controls.Add(slider, 1, 0);

            Label val = new Label();
            val.AutoSize = true;
            val.Text = format(value);
            val.Font = new Font(Font.FontFamily, 10.5f, FontStyle.Bold);
            val.ForeColor = MaterialColors.Primary;
            val.BackColor = MaterialColors.SurfaceContainer;
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
            card.Width = (int)(210 * _dpiScale);
            card.Height = (int)(84 * _dpiScale);
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
            note.Text = L10n.T("彩虹模式使用 HSV 循环，下面的颜色色板不参与渲染。",
                "Rainbow mode uses an HSV cycle; the color palette below is not used.");
            note.MaximumSize = new Size((int)(720 * _dpiScale), 0);
            note.ForeColor = MaterialColors.OnSurfaceVariant;
            note.BackColor = MaterialColors.SurfaceContainer;
            note.Font = new Font(Font.FontFamily, 8.5f);
            note.Margin = new Padding(0, 0, 0, (int)(8 * _dpiScale));
            content.RowCount++;
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.Controls.Add(note, 0, content.RowCount - 1);

            AddRainbowRows(content);
            AddCard(L10n.T("彩虹参数", "Rainbow settings"),
                L10n.T("调整彩虹的流速、饱和度和色相偏移。",
                    "Adjust the rainbow speed, saturation and hue offset."), content);
        }

        private void BuildColorCards()
        {
            if (_cfg.Style == "rainbow")
            {
                BuildRainbowColorCard();
                return;
            }

            FlowLayoutPanel flow = new FlowLayoutPanel();            flow.AutoSize = true;
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
            current.Font = new Font(Font.FontFamily, 8.5f);
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

        private void BuildThicknessCards()
        {
            TableLayoutPanel content = new TableLayoutPanel();
            content.AutoSize = true;
            content.Dock = DockStyle.Top;
            content.ColumnCount = 2;
            content.RowCount = 1;
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, (int)(90 * _dpiScale)));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.BackColor = MaterialColors.SurfaceContainer;

            MaterialSlider slider = new MaterialSlider();
            slider.Minimum = 0.5;
            slider.Maximum = 2.0;
            slider.Step = 0.05;
            slider.Value = _cfg.Scale;
            slider.Dock = DockStyle.Top;
            slider.Height = (int)(32 * _dpiScale);
            slider.UiScale = _dpiScale;
            slider.Margin = new Padding(0, 8, 12, 0);

            Label value = new Label();
            value.AutoSize = true;
            value.Text = _cfg.Scale.ToString("0.00") + "×";
            value.Font = new Font(Font.FontFamily, 12f, FontStyle.Bold);
            value.ForeColor = MaterialColors.Primary;
            value.BackColor = MaterialColors.SurfaceContainer;
            value.Anchor = AnchorStyles.Right;
            value.Margin = new Padding(0, 12, 0, 0);

            slider.ValueChanged += delegate
            {
                if (_loading)
                {
                    return;
                }
                _cfg.Scale = (float)slider.Value;
                value.Text = slider.Value.ToString("0.00") + "×";
                _changed(false);
            };
            slider.ValueCommitted += delegate { _cfg.Save(); };

            content.Controls.Add(slider, 0, 0);
            content.Controls.Add(value, 1, 0);

            AddCard(L10n.T("粗细", "Thickness"), L10n.T("调整拖尾的线宽，0.5× 到 2.0×。",
                "Adjust the trail width from 0.5× to 2.0×."), content);
        }

        private void BuildDurationCards()
        {
            TableLayoutPanel content = new TableLayoutPanel();
            content.AutoSize = true;
            content.Dock = DockStyle.Top;
            content.ColumnCount = 2;
            content.RowCount = 1;
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, (int)(100 * _dpiScale)));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.BackColor = MaterialColors.SurfaceContainer;

            MaterialSlider slider = new MaterialSlider();
            slider.Minimum = 0.2;
            slider.Maximum = 1.5;
            slider.Step = 0.05;
            slider.Value = _cfg.Life;
            slider.Dock = DockStyle.Top;
            slider.Height = (int)(32 * _dpiScale);
            slider.UiScale = _dpiScale;
            slider.Margin = new Padding(0, 8, 12, 0);

            Label value = new Label();
            value.AutoSize = true;
            value.Text = _cfg.Life.ToString("0.00") + L10n.T(" 秒", " s");
            value.Font = new Font(Font.FontFamily, 12f, FontStyle.Bold);
            value.ForeColor = MaterialColors.Primary;
            value.BackColor = MaterialColors.SurfaceContainer;
            value.Anchor = AnchorStyles.Right;
            value.Margin = new Padding(0, 12, 0, 0);

            slider.ValueChanged += delegate
            {
                if (_loading)
                {
                    return;
                }
                _cfg.Life = slider.Value;
                value.Text = slider.Value.ToString("0.00") + L10n.T(" 秒", " s");
                _changed(false);
            };
            slider.ValueCommitted += delegate { _cfg.Save(); };

            content.Controls.Add(slider, 0, 0);
            content.Controls.Add(value, 1, 0);

            AddCard(L10n.T("时长", "Duration"), L10n.T("调整拖尾停留的时间，0.2 到 1.5 秒。",
                "Adjust how long the trail stays, from 0.2 to 1.5 s."), content);
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
            reset.Click += delegate
            {
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
            folder.BorderColor = MaterialColors.OutlineVariant;
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
            flow.AutoSize = true;
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
            note.Font = new Font(Font.FontFamily, 8.5f);
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
            button.BorderColor = selected ? MaterialColors.Primary : Color.Transparent;
            button.Radius = 18;
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
            name.Font = new Font(Font.FontFamily, 18f, FontStyle.Bold);
            name.ForeColor = MaterialColors.OnSurface;
            name.BackColor = MaterialColors.SurfaceContainer;
            name.Margin = new Padding(0, 0, 0, 2);
            content.Controls.Add(name, 1, 0);

            Label version = new Label();
            version.AutoSize = true;
            version.Text = "v" + AppVersion + " · GPL-3.0";
            version.Font = new Font(Font.FontFamily, 9f);
            version.ForeColor = MaterialColors.OnSurfaceVariant;
            version.BackColor = MaterialColors.SurfaceContainer;
            content.Controls.Add(version, 1, 1);

            Label description = new Label();
            description.AutoSize = true;
            description.MaximumSize = new Size(620, 0);
            description.Text = L10n.T("Windows 鼠标拖尾特效：全屏透明覆盖、四种样式、按时间淡出，支持高 DPI。",
                "Windows cursor trail effect: full-screen transparent overlay, four styles, time-based fading and high-DPI support.");
            description.ForeColor = MaterialColors.OnSurfaceVariant;
            description.BackColor = MaterialColors.SurfaceContainer;
            description.Margin = new Padding(0, 8, 0, 4);
            content.Controls.Add(description, 1, 2);

            Label config = new Label();
            config.AutoSize = true;
            config.MaximumSize = new Size(620, 0);
            config.Text = L10n.T("配置文件：", "Config file: ") + AppSettings.FilePath;
            config.ForeColor = MaterialColors.OnSurfaceVariant;
            config.Font = new Font(Font.FontFamily, 8.5f);
            config.BackColor = MaterialColors.SurfaceContainer;
            content.Controls.Add(config, 1, 3);

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
            repo.Font = new Font(Font.FontFamily, 12f, FontStyle.Bold);
            repo.ForeColor = MaterialColors.OnSurface;
            repo.BackColor = MaterialColors.SurfaceContainer;
            content.Controls.Add(repo, 0, 0);

            Label description = new Label();
            description.AutoSize = true;
            description.MaximumSize = new Size(620, 0);
            description.Text = L10n.T("源码、问题反馈和版本发布都在 GitHub 上。",
                "Source code, issues and releases are on GitHub.");
            description.ForeColor = MaterialColors.OnSurfaceVariant;
            description.BackColor = MaterialColors.SurfaceContainer;
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
                case Section.Thickness:
                    return L10n.T("粗细", "Thickness");
                case Section.Duration:
                    return L10n.T("时长", "Duration");
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
                return L10n.T("彩虹模式使用 HSV 循环，参数在下面调整。",
                    "Rainbow mode uses an HSV cycle; adjust its settings below.");
            }

            switch (section)
            {
                case Section.Appearance:
                    return L10n.T("开关、样式和整体观感。", "Toggle, style and overall look.");
                case Section.Color:
                    return L10n.T("选择拖尾的主色。", "Pick the main color of the trail.");
                case Section.Thickness:
                    return L10n.T("调整拖尾的线宽。", "Adjust the trail thickness.");
                case Section.Duration:
                    return L10n.T("调整拖尾停留的时间。", "Adjust how long the trail lasts.");
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
