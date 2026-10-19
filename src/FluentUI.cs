using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CursorComet
{
    /// <summary>Microsoft Fluent Design（Windows 11 浅色）配色。</summary>
    internal static class FluentColors
    {
        public static readonly Color Primary = Color.FromArgb(0, 103, 192);
        public static readonly Color OnPrimary = Color.FromArgb(255, 255, 255);
        public static readonly Color PrimaryContainer = Color.FromArgb(229, 241, 251);
        public static readonly Color OnPrimaryContainer = Color.FromArgb(0, 62, 107);
        public static readonly Color SecondaryContainer = Color.FromArgb(243, 243, 243);
        public static readonly Color OnSecondaryContainer = Color.FromArgb(26, 26, 26);
        public static readonly Color Surface = Color.FromArgb(243, 243, 243);
        public static readonly Color SurfaceContainer = Color.FromArgb(255, 255, 255);
        public static readonly Color SurfaceContainerHigh = Color.FromArgb(249, 249, 249);
        public static readonly Color OnSurface = Color.FromArgb(26, 26, 26);
        public static readonly Color OnSurfaceVariant = Color.FromArgb(93, 93, 93);
        public static readonly Color Outline = Color.FromArgb(138, 138, 138);
        public static readonly Color OutlineVariant = Color.FromArgb(229, 229, 229);
        public static readonly Color Error = Color.FromArgb(196, 43, 28);
        public static readonly Color ErrorContainer = Color.FromArgb(253, 231, 233);
        public static readonly Color Accent = Primary;
        public static readonly Color AccentHover = Color.FromArgb(25, 117, 197);
        public static readonly Color AccentPressed = Color.FromArgb(0, 90, 158);
        public static readonly Color Card = Color.FromArgb(255, 255, 255);
        public static readonly Color Stroke = Color.FromArgb(229, 229, 229);
        public static readonly Color SubtleHover = Color.FromArgb(245, 245, 245);
        public static readonly Color SubtlePressed = Color.FromArgb(235, 235, 235);
        public static readonly Color SelectedTint = Color.FromArgb(240, 246, 252);
    }

    internal static class FluentFonts
    {
        public static readonly FontFamily Family = Resolve();

        private static FontFamily Resolve()
        {
            string[] names = { "Microsoft YaHei UI", "Segoe UI Variable Text", "Segoe UI" };
            for (int i = 0; i < names.Length; i++)
            {
                try
                {
                    return new FontFamily(names[i]);
                }
                catch
                {
                }
            }
            return SystemFonts.MessageBoxFont.FontFamily;
        }

        public static Font Create(float size, FontStyle style)
        {
            return new Font(Family, size, style, GraphicsUnit.Point);
        }
    }

    internal static class FluentDraw
    {
        public static GraphicsPath RoundRect(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = Math.Max(0, Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height)));
            if (d <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static Color Mix(Color a, Color b, float t)
        {
            if (t <= 0f) return a;
            if (t >= 1f) return b;
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }
    }

    /// <summary>标准 WinForms 按钮的 Fluent 轻量主题，替代原来的自绘按钮。</summary>
    internal static class FluentButtons
    {
        public static Button Make(string text, Color fill, Color textColor, Color border,
            int height, float fontSize, FontStyle fontStyle, float scale)
        {
            Button button = new Button();
            button.Text = text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = border.A > 0 ? 1 : 0;
            if (border.A > 0)
            {
                button.FlatAppearance.BorderColor = border;
            }
            bool light = fill.GetBrightness() > 0.6f;
            button.FlatAppearance.MouseOverBackColor = light
                ? FluentDraw.Mix(fill, Color.Black, 0.04f)
                : FluentDraw.Mix(fill, Color.White, 0.12f);
            button.FlatAppearance.MouseDownBackColor = FluentDraw.Mix(fill, Color.Black, 0.08f);
            button.BackColor = fill;
            button.ForeColor = textColor;
            button.UseVisualStyleBackColor = false;
            button.Font = FluentFonts.Create(fontSize, fontStyle);
            button.Height = height;
            button.MinimumSize = new Size((int)(88 * scale), height);
            button.Padding = new Padding((int)(12 * scale), 4, (int)(12 * scale), 4);
            button.AutoSize = true;
            button.Cursor = FluentCursors.Hand;
            button.Resize += delegate
            {
                using (GraphicsPath path = FluentDraw.RoundRect(
                    new Rectangle(0, 0, button.Width, button.Height), (int)(4 * scale)))
                {
                    button.Region = new Region(path);
                }
            };
            return button;
        }
    }

    /// <summary>标准 WinForms 开关/滑块的 Fluent 轻量主题。</summary>
    internal static class FluentControls
    {
        public static CheckBox MakeToggle(string text, bool isChecked, Color background, float scale)
        {
            CheckBox box = new CheckBox();
            box.Text = text;
            box.Checked = isChecked;
            box.AutoSize = true;
            box.Font = FluentFonts.Create(10f, FontStyle.Regular);
            box.ForeColor = FluentColors.OnSurface;
            box.BackColor = background;
            box.Cursor = FluentCursors.Hand;
            box.Margin = new Padding(0, (int)(2 * scale), 0, (int)(4 * scale));
            return box;
        }

        public static TrackBar MakeSlider(double min, double max, double value, float scale, Color background)
        {
            TrackBar bar = new TrackBar();
            bar.Minimum = 0;
            bar.Maximum = 1000;
            bar.TickStyle = TickStyle.None;
            bar.AutoSize = false;
            bar.Height = (int)(32 * scale);
            bar.SmallChange = 1;
            bar.LargeChange = 10;
            bar.BackColor = background;
            bar.Value = ValueToPosition(min, max, value);
            return bar;
        }

        public static double SliderValue(TrackBar bar, double min, double max, double step)
        {
            double raw = min + (max - min) * (bar.Value / 1000.0);
            if (step > 0)
            {
                raw = Math.Round(raw / step) * step;
            }
            return Math.Max(min, Math.Min(max, raw));
        }

        private static int ValueToPosition(double min, double max, double value)
        {
            if (max <= min)
            {
                return 0;
            }
            double t = (value - min) / (max - min);
            return (int)Math.Round(Math.Max(0.0, Math.Min(1.0, t)) * 1000);
        }
    }
    internal enum NavIcon
    {
        Sparkle,
        Palette,
        Halo,
        Thickness,
        Timer,
        Tune,
        Globe,
        Info,
        Code
    }

    internal static class FluentIcons
    {
        public static void Draw(Graphics g, Rectangle r, NavIcon icon, Color color)
        {
            SmoothingMode old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(color, 1.7f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                switch (icon)
                {
                    case NavIcon.Sparkle:
                        DrawSparkle(g, r, pen, color);
                        break;
                    case NavIcon.Palette:
                        DrawPalette(g, r, pen, color);
                        break;
                    case NavIcon.Halo:
                        DrawHaloIcon(g, r, pen);
                        break;
                    case NavIcon.Thickness:
                        DrawThickness(g, r, pen);
                        break;
                    case NavIcon.Timer:
                        DrawTimer(g, r, pen);
                        break;
                    case NavIcon.Tune:
                        DrawTune(g, r, pen, color);
                        break;
                    case NavIcon.Globe:
                        DrawGlobe(g, r, pen);
                        break;
                    case NavIcon.Info:
                        DrawInfo(g, r, pen, color);
                        break;
                    case NavIcon.Code:
                        DrawCode(g, r, pen);
                        break;
                }
            }
            g.SmoothingMode = old;
        }

        private static void DrawSparkle(Graphics g, Rectangle r, Pen pen, Color color)
        {
            int cx = r.Left + r.Width / 2;
            int cy = r.Top + r.Height / 2;
            int s = Math.Min(r.Width, r.Height) / 2 - 2;
            g.DrawLine(pen, cx, cy - s, cx, cy + s);
            g.DrawLine(pen, cx - s, cy, cx + s, cy);
            using (SolidBrush b = new SolidBrush(color))
            {
                int d = 4;
                g.FillEllipse(b, cx - d / 2, cy - d / 2, d, d);
            }
        }

        private static void DrawPalette(Graphics g, Rectangle r, Pen pen, Color color)
        {
            Rectangle oval = new Rectangle(r.Left + 3, r.Top + 3, r.Width - 6, r.Height - 6);
            g.DrawEllipse(pen, oval);
            using (SolidBrush b = new SolidBrush(color))
            {
                g.FillEllipse(b, oval.Left + 5, oval.Top + 5, 4, 4);
                g.FillEllipse(b, oval.Right - 9, oval.Top + 7, 3, 3);
                g.FillEllipse(b, oval.Left + 8, oval.Bottom - 9, 3, 3);
            }
        }

        private static void DrawHaloIcon(Graphics g, Rectangle r, Pen pen)
        {
            int cx = r.Left + r.Width / 2;
            int cy = r.Top + r.Height / 2;
            int outer = Math.Min(r.Width, r.Height) / 2 - 2;
            g.DrawEllipse(pen, cx - outer, cy - outer, outer * 2, outer * 2);
            int inner = Math.Max(2, outer / 3);
            g.DrawEllipse(pen, cx - inner, cy - inner, inner * 2, inner * 2);
        }

        private static void DrawThickness(Graphics g, Rectangle r, Pen pen)        {
            int left = r.Left + 3;
            int right = r.Right - 3;
            int cy = r.Top + r.Height / 2;
            using (Pen p1 = new Pen(pen.Color, 1f))
            using (Pen p2 = new Pen(pen.Color, 2.2f))
            using (Pen p3 = new Pen(pen.Color, 3.4f))
            {
                p1.StartCap = LineCap.Round;
                p1.EndCap = LineCap.Round;
                p2.StartCap = LineCap.Round;
                p2.EndCap = LineCap.Round;
                p3.StartCap = LineCap.Round;
                p3.EndCap = LineCap.Round;
                g.DrawLine(p1, left, cy - 6, right, cy - 6);
                g.DrawLine(p2, left, cy, right, cy);
                g.DrawLine(p3, left, cy + 6, right, cy + 6);
            }
        }

        private static void DrawTimer(Graphics g, Rectangle r, Pen pen)
        {
            Rectangle oval = new Rectangle(r.Left + 3, r.Top + 4, r.Width - 6, r.Height - 7);
            g.DrawEllipse(pen, oval);
            int cx = oval.Left + oval.Width / 2;
            int cy = oval.Top + oval.Height / 2;
            g.DrawLine(pen, cx, cy, cx, cy - 6);
            g.DrawLine(pen, cx, cy, cx + 5, cy + 2);
            g.DrawLine(pen, cx - 3, oval.Top - 3, cx + 3, oval.Top - 3);
        }

        private static void DrawTune(Graphics g, Rectangle r, Pen pen, Color color)
        {
            int left = r.Left + 3;
            int right = r.Right - 3;
            int y1 = r.Top + 6;
            int y2 = r.Top + r.Height / 2;
            int y3 = r.Bottom - 6;
            g.DrawLine(pen, left, y1, right, y1);
            g.DrawLine(pen, left, y2, right, y2);
            g.DrawLine(pen, left, y3, right, y3);
            using (SolidBrush b = new SolidBrush(color))
            {
                g.FillEllipse(b, left + 5, y1 - 3, 6, 6);
                g.FillEllipse(b, right - 13, y2 - 3, 6, 6);
                g.FillEllipse(b, left + 11, y3 - 3, 6, 6);
            }
        }

        private static void DrawGlobe(Graphics g, Rectangle r, Pen pen)
        {
            Rectangle oval = new Rectangle(r.Left + 3, r.Top + 3, r.Width - 6, r.Height - 6);
            g.DrawEllipse(pen, oval);
            g.DrawEllipse(pen, oval.Left + oval.Width / 4, oval.Top, oval.Width / 2, oval.Height);
            g.DrawLine(pen, oval.Left, oval.Top + oval.Height / 2,
                oval.Right, oval.Top + oval.Height / 2);
        }

        private static void DrawInfo(Graphics g, Rectangle r, Pen pen, Color color)
        {
            Rectangle oval = new Rectangle(r.Left + 3, r.Top + 3, r.Width - 6, r.Height - 6);
            g.DrawEllipse(pen, oval);
            using (SolidBrush b = new SolidBrush(color))
            {
                int cx = oval.Left + oval.Width / 2;
                g.FillEllipse(b, cx - 1, oval.Top + 5, 3, 3);
            }
            int x = oval.Left + oval.Width / 2;
            g.DrawLine(pen, x, oval.Top + 11, x, oval.Bottom - 5);
        }

        private static void DrawCode(Graphics g, Rectangle r, Pen pen)
        {
            int cy = r.Top + r.Height / 2;
            int left = r.Left + 6;
            int right = r.Right - 6;
            g.DrawLine(pen, left + 4, cy - 5, left, cy);
            g.DrawLine(pen, left, cy, left + 4, cy + 5);
            g.DrawLine(pen, right - 4, cy - 5, right, cy);
            g.DrawLine(pen, right, cy, right - 4, cy + 5);
            g.DrawLine(pen, (left + right) / 2 + 2, cy - 7, (left + right) / 2 - 2, cy + 7);
        }
    }

    internal class NavItem : Control
    {
        private bool _hover;

        public NavIcon Icon = NavIcon.Sparkle;
        public string Label = string.Empty;
        public bool Selected;
        public bool Collapsed;
        public float UiScale = 1f;

        public NavItem()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Height = 40;
            Dock = DockStyle.Fill;
            Margin = new Padding(0, 2, 0, 2);
            Cursor = FluentCursors.Hand;
            Font = FluentFonts.Create(10f, FontStyle.Regular);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(2, 2, Width - 5, Height - 5);
            if (Selected || _hover)
            {
                using (GraphicsPath path = FluentDraw.RoundRect(rect, (int)(4 * UiScale)))
                using (SolidBrush brush = new SolidBrush(Selected
                    ? Color.FromArgb(255, 255, 255)
                    : FluentColors.SubtleHover))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }

            if (Selected)
            {
                Rectangle accent = new Rectangle((int)(2 * UiScale), (Height - (int)(18 * UiScale)) / 2,
                    Math.Max(2, (int)(3 * UiScale)), (int)(18 * UiScale));
                using (GraphicsPath path = FluentDraw.RoundRect(accent, Math.Max(1, (int)(1.5f * UiScale))))
                using (SolidBrush brush = new SolidBrush(FluentColors.Accent))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }

            Color iconColor = Selected ? FluentColors.Accent : FluentColors.OnSurfaceVariant;
            Color textColor = FluentColors.OnSurface;
            int iconSize = (int)(22 * UiScale);
            int iconX = Collapsed ? (Width - iconSize) / 2 : (int)(14 * UiScale);
            Rectangle iconRect = new Rectangle(iconX, (Height - iconSize) / 2, iconSize, iconSize);
            FluentIcons.Draw(e.Graphics, iconRect, Icon, iconColor);

            if (!Collapsed)
            {
                int textX = (int)(46 * UiScale);
                TextRenderer.DrawText(e.Graphics, Label, Font,
                    new Rectangle(textX, 0, Math.Max(0, Width - textX - (int)(8 * UiScale)), Height), textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }
    }

    internal sealed class BufferedFlowPanel : FlowLayoutPanel
    {
        public BufferedFlowPanel()
        {
            DoubleBuffered = true;
        }
    }
}