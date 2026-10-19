using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MouseTrail
{
    /// <summary>Microsoft Fluent Design（Windows 11 浅色）配色。</summary>
    internal static class MaterialColors
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

    internal static class FluentBackdrop
    {
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        public static void Apply(IntPtr hwnd)
        {
            try
            {
                int corner = 2;
                DwmSetWindowAttribute(hwnd, 33, ref corner, sizeof(int));
                int dark = 0;
                DwmSetWindowAttribute(hwnd, 20, ref dark, sizeof(int));
                int backdrop = 2;
                DwmSetWindowAttribute(hwnd, 38, ref backdrop, sizeof(int));
            }
            catch
            {
            }
        }
    }

    internal static class MaterialDraw
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

    internal static class MaterialIcons
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

    internal class RoundedPanel : Panel
    {
        public int Radius = 8;
        public Color FillColor = MaterialColors.Card;
        public Color BorderColor = MaterialColors.Stroke;
        public float BorderWidth = 1f;

        public RoundedPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = MaterialDraw.RoundRect(rect, Radius))
            {
                if (FillColor.A > 0)
                {
                    using (SolidBrush brush = new SolidBrush(FillColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }
                if (BorderColor.A > 0)
                {
                    using (Pen pen = new Pen(BorderColor, BorderWidth))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }
            base.OnPaint(e);
        }
    }

    internal class MaterialButton : Button
    {
        public Color FillColor = MaterialColors.Primary;
        public Color TextColor = MaterialColors.OnPrimary;
        public Color BorderColor = Color.Transparent;
        public float BorderWidth = 1f;
        public Color HoverColor = Color.Empty;
        public int Radius = 4;

        private bool _hover;
        private bool _pressed;

        public MaterialButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Font = FluentFonts.Create(10f, FontStyle.Regular);
            AutoSize = true;
            MinimumSize = new Size(88, 32);
            Padding = new Padding(12, 4, 12, 4);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            FillBackground(e.Graphics);
            Color fill = FillColor;
            if (_pressed)
            {
                fill = MaterialDraw.Mix(fill, Color.Black, 0.08f);
            }
            else if (_hover)
            {
                if (HoverColor.A > 0)
                {
                    fill = HoverColor;
                }
                else if (fill.GetBrightness() > 0.6f)
                {
                    fill = MaterialDraw.Mix(fill, Color.Black, 0.04f);
                }
                else
                {
                    fill = MaterialDraw.Mix(fill, Color.White, 0.12f);
                }
            }

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = MaterialDraw.RoundRect(rect, Radius))
            {
                if (fill.A > 0)
                {
                    using (SolidBrush brush = new SolidBrush(fill))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }
                if (BorderColor.A > 0)
                {
                    using (Pen pen = new Pen(BorderColor, BorderWidth))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }

            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                                    TextFormatFlags.EndEllipsis;
            Rectangle textRect = new Rectangle(8, 0, Width - 16, Height);
            TextRenderer.DrawText(e.Graphics, Text, Font, textRect, TextColor, flags);
        }

        private void FillBackground(Graphics g)
        {
            Color bg = BackColor;
            if (bg.A == 0 && Parent != null)
            {
                bg = Parent.BackColor;
            }
            using (SolidBrush brush = new SolidBrush(bg))
            {
                g.FillRectangle(brush, ClientRectangle);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            FillBackground(pevent.Graphics);
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
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            _pressed = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }
    }

    internal class MaterialToggle : Control
    {
        private bool _hover;
        private bool _checked;
        public float UiScale = 1f;

        public event EventHandler CheckedChanged;

        public MaterialToggle()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
            BackColor = Color.Transparent;
            Height = 32;
            Width = 280;
            Cursor = Cursors.Hand;
            Font = FluentFonts.Create(10f, FontStyle.Regular);
            TabStop = true;
        }

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked != value)
                {
                    _checked = value;
                    Invalidate();
                    if (CheckedChanged != null)
                    {
                        CheckedChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        protected override void OnClick(EventArgs e)
        {
            Checked = !Checked;
            base.OnClick(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                Checked = !Checked;
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color bg = BackColor;
            if (bg.A == 0 && Parent != null)
            {
                bg = Parent.BackColor;
            }
            using (SolidBrush brush = new SolidBrush(bg))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }

            int trackW = (int)(40 * UiScale);
            int trackH = (int)(20 * UiScale);
            int top = (Height - trackH) / 2;
            Rectangle track = new Rectangle(0, top, trackW, trackH);
            using (GraphicsPath path = MaterialDraw.RoundRect(track, trackH / 2))
            {
                Color fill = Checked
                    ? MaterialColors.Accent
                    : (_hover ? MaterialColors.SubtleHover : Color.White);
                using (SolidBrush brush = new SolidBrush(fill))
                {
                    e.Graphics.FillPath(brush, path);
                }
                if (!Checked)
                {
                    using (Pen border = new Pen(MaterialColors.OnSurfaceVariant, Math.Max(1f, 1.1f * UiScale)))
                    {
                        e.Graphics.DrawPath(border, path);
                    }
                }
            }

            int knobSize = trackH - (int)(6 * UiScale);
            int knobX = Checked
                ? track.Right - knobSize - (int)(3 * UiScale)
                : track.Left + (int)(3 * UiScale);
            using (SolidBrush knob = new SolidBrush(Checked ? Color.White : MaterialColors.OnSurfaceVariant))
            {
                e.Graphics.FillEllipse(knob, knobX, top + (trackH - knobSize) / 2, knobSize, knobSize);
            }

            int textX = trackW + (int)(12 * UiScale);
            Rectangle textRect = new Rectangle(textX, 0, Math.Max(0, Width - textX), Height);
            TextRenderer.DrawText(e.Graphics, Text, Font, textRect, MaterialColors.OnSurface,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            Color bg = BackColor;
            if (bg.A == 0 && Parent != null)
            {
                bg = Parent.BackColor;
            }
            using (SolidBrush brush = new SolidBrush(bg))
            {
                pevent.Graphics.FillRectangle(brush, ClientRectangle);
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
    internal class MaterialSlider : Control
    {
        private double _value;
        private bool _dragging;
        private bool _hover;

        public double Minimum = 0.0;
        public double Maximum = 1.0;
        public double Step = 0.05;
        public float UiScale = 1f;

        public event EventHandler ValueChanged;
        public event EventHandler ValueCommitted;

        public MaterialSlider()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Height = 32;
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        public double Value
        {
            get { return _value; }
            set
            {
                double v = Math.Max(Minimum, Math.Min(Maximum, value));
                if (Step > 0)
                {
                    v = Minimum + Math.Round((v - Minimum) / Step) * Step;
                    v = Math.Max(Minimum, Math.Min(Maximum, v));
                }
                if (Math.Abs(v - _value) > 0.0001)
                {
                    _value = v;
                    Invalidate();
                    if (ValueChanged != null)
                    {
                        ValueChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color bg = BackColor;
            if (bg.A == 0 && Parent != null)
            {
                bg = Parent.BackColor;
            }
            using (SolidBrush brush = new SolidBrush(bg))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }

            int left = (int)(10 * UiScale);
            int right = Width - left;
            if (right <= left) return;
            int cy = Height / 2;
            double range = Maximum - Minimum;
            double t = range <= 0 ? 0 : (_value - Minimum) / range;
            int thumb = left + (int)Math.Round((right - left) * t);

            int trackHeight = Math.Max(4, (int)(4 * UiScale));
            Rectangle track = new Rectangle(left, cy - trackHeight / 2, right - left, trackHeight);
            using (GraphicsPath path = MaterialDraw.RoundRect(track, trackHeight / 2))
            using (SolidBrush brush = new SolidBrush(MaterialColors.OutlineVariant))
            {
                e.Graphics.FillPath(brush, path);
            }

            if (thumb > left)
            {
                Rectangle active = new Rectangle(left, cy - trackHeight / 2, thumb - left, trackHeight);
                using (GraphicsPath path = MaterialDraw.RoundRect(active, trackHeight / 2))
                using (SolidBrush brush = new SolidBrush(MaterialColors.Primary))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }

            int r = (int)((_hover || _dragging ? 11 : 10) * UiScale);
            if (_hover || _dragging)
            {
                int halo = (int)(16 * UiScale);
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(30, MaterialColors.Accent)))
                {
                    e.Graphics.FillEllipse(brush, thumb - halo, cy - halo, halo * 2, halo * 2);
                }
            }
            using (SolidBrush fill = new SolidBrush(Color.White))
            {
                e.Graphics.FillEllipse(fill, thumb - r, cy - r, r * 2, r * 2);
            }
            using (Pen ring = new Pen(MaterialColors.Accent, 2f))
            {
                e.Graphics.DrawEllipse(ring, thumb - r, cy - r, r * 2, r * 2);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            Color bg = BackColor;
            if (bg.A == 0 && Parent != null)
            {
                bg = Parent.BackColor;
            }
            using (SolidBrush brush = new SolidBrush(bg))
            {
                pevent.Graphics.FillRectangle(brush, ClientRectangle);
            }
        }

        private void SetFromMouse(int x)
        {
            int left = (int)(10 * UiScale);
            int right = Width - left;
            if (right <= left) return;
            double t = (double)(x - left) / (right - left);
            t = Math.Max(0.0, Math.Min(1.0, t));
            Value = Minimum + t * (Maximum - Minimum);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _dragging = true;
                Capture = true;
                SetFromMouse(e.X);
                Focus();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_dragging)
            {
                SetFromMouse(e.X);
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_dragging)
            {
                _dragging = false;
                Capture = false;
                if (ValueCommitted != null)
                {
                    ValueCommitted(this, EventArgs.Empty);
                }
            }
            base.OnMouseUp(e);
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

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Left || keyData == Keys.Right || keyData == Keys.Home || keyData == Keys.End)
            {
                return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left)
            {
                Value = _value - Step;
                Commit();
            }
            else if (e.KeyCode == Keys.Right)
            {
                Value = _value + Step;
                Commit();
            }
            else if (e.KeyCode == Keys.Home)
            {
                Value = Minimum;
                Commit();
            }
            else if (e.KeyCode == Keys.End)
            {
                Value = Maximum;
                Commit();
            }
            base.OnKeyDown(e);
        }

        private void Commit()
        {
            if (ValueCommitted != null)
            {
                ValueCommitted(this, EventArgs.Empty);
            }
        }
    }

    internal class ChoiceCard : Control
    {
        private bool _hover;

        public string Title = string.Empty;
        public string Subtitle = string.Empty;
        public bool Selected;
        public int Radius = 8;
        public float UiScale = 1f;

        public ChoiceCard()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(210, 84);
            Margin = new Padding(0, 0, 10, 10);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fill = Selected
                ? MaterialColors.SelectedTint
                : (_hover ? MaterialColors.SubtleHover : MaterialColors.Card);
            using (GraphicsPath path = MaterialDraw.RoundRect(rect, Radius))
            {
                using (SolidBrush brush = new SolidBrush(fill))
                {
                    e.Graphics.FillPath(brush, path);
                }
                if (Selected)
                {
                    using (Pen pen = new Pen(MaterialColors.Accent, 2f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
                else
                {
                    using (Pen pen = new Pen(MaterialColors.Stroke, 1f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }

            Color titleColor = MaterialColors.OnSurface;
            int pad = (int)(16 * UiScale);
            using (Font titleFont = FluentFonts.Create(11f, FontStyle.Bold))
            {
                TextRenderer.DrawText(e.Graphics, Title, titleFont,
                    new Rectangle(pad, (int)(12 * UiScale), Width - pad * 2, (int)(22 * UiScale)), titleColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            using (Font subFont = FluentFonts.Create(9f, FontStyle.Regular))
            {
                TextRenderer.DrawText(e.Graphics, Subtitle, subFont,
                    new Rectangle(pad, (int)(38 * UiScale), Width - pad * 2, Height - (int)(48 * UiScale)),
                    MaterialColors.OnSurfaceVariant,
                    TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak |
                    TextFormatFlags.EndEllipsis);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
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

    internal class ColorSwatch : Control
    {
        private bool _hover;

        public Color SwatchColor = Color.White;
        public bool Selected;
        public string ColorName = string.Empty;
        public float UiScale = 1f;

        public ColorSwatch()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(46, 46);
            Cursor = Cursors.Hand;
            Margin = new Padding(0, 0, 10, 0);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(3, 3, Width - 7, Height - 7);
            if (Selected)
            {
                using (Pen ring = new Pen(MaterialColors.Primary, 2.5f))
                {
                    e.Graphics.DrawEllipse(ring, 1, 1, Width - 4, Height - 4);
                }
            }
            else if (_hover)
            {
                using (Pen ring = new Pen(MaterialColors.Outline, 1.5f))
                {
                    e.Graphics.DrawEllipse(ring, 1, 1, Width - 4, Height - 4);
                }
            }
            using (SolidBrush brush = new SolidBrush(SwatchColor))
            {
                e.Graphics.FillEllipse(brush, rect);
            }
            using (Pen outline = new Pen(Color.FromArgb(60, 0, 0, 0), 1f))
            {
                e.Graphics.DrawEllipse(outline, rect);
            }
            if (Selected)
            {
                using (SolidBrush check = new SolidBrush(Color.White))
                {
                    int cx = Width / 2;
                    int cy = Height / 2;
                    e.Graphics.FillEllipse(check, cx - 4, cy - 4, 8, 8);
                }
                using (Pen checkPen = new Pen(MaterialColors.Primary, 1.6f))
                {
                    int cx = Width / 2;
                    int cy = Height / 2;
                    e.Graphics.DrawLine(checkPen, cx - 4, cy, cx - 1, cy + 3);
                    e.Graphics.DrawLine(checkPen, cx - 1, cy + 3, cx + 5, cy - 4);
                }
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
            Cursor = Cursors.Hand;
            Font = FluentFonts.Create(10f, FontStyle.Regular);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(2, 2, Width - 5, Height - 5);
            if (Selected || _hover)
            {
                using (GraphicsPath path = MaterialDraw.RoundRect(rect, (int)(4 * UiScale)))
                using (SolidBrush brush = new SolidBrush(Selected
                    ? Color.FromArgb(255, 255, 255)
                    : MaterialColors.SubtleHover))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }

            if (Selected)
            {
                Rectangle accent = new Rectangle((int)(2 * UiScale), (Height - (int)(18 * UiScale)) / 2,
                    Math.Max(2, (int)(3 * UiScale)), (int)(18 * UiScale));
                using (GraphicsPath path = MaterialDraw.RoundRect(accent, Math.Max(1, (int)(1.5f * UiScale))))
                using (SolidBrush brush = new SolidBrush(MaterialColors.Accent))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }

            Color iconColor = Selected ? MaterialColors.Accent : MaterialColors.OnSurfaceVariant;
            Color textColor = MaterialColors.OnSurface;
            int iconSize = (int)(22 * UiScale);
            int iconX = Collapsed ? (Width - iconSize) / 2 : (int)(14 * UiScale);
            Rectangle iconRect = new Rectangle(iconX, (Height - iconSize) / 2, iconSize, iconSize);
            MaterialIcons.Draw(e.Graphics, iconRect, Icon, iconColor);

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

    internal enum WindowButtonIcon
    {
        Minimize,
        Maximize,
        Restore,
        Close
    }

    internal sealed class WindowButton : Control
    {
        public WindowButtonIcon Icon = WindowButtonIcon.Minimize;
        public float UiScale = 1f;
        private static readonly string GlyphFamily = ResolveGlyphFamily();

        private bool _hover;

        private static string ResolveGlyphFamily()
        {
            string[] names = { "Segoe Fluent Icons", "Segoe MDL2 Assets" };
            for (int i = 0; i < names.Length; i++)
            {
                try
                {
                    using (FontFamily family = new FontFamily(names[i]))
                    {
                        return names[i];
                    }
                }
                catch
                {
                }
            }
            return null;
        }

        public WindowButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Width = 46;
            Height = 32;
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color parentColor = Parent != null ? Parent.BackColor : MaterialColors.Surface;
            using (SolidBrush back = new SolidBrush(parentColor))
            {
                e.Graphics.FillRectangle(back, ClientRectangle);
            }

            if (_hover)
            {
                Color fill = Icon == WindowButtonIcon.Close
                    ? MaterialColors.Error
                    : MaterialColors.SubtleHover;
                using (SolidBrush brush = new SolidBrush(fill))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }
            }

            Color color = _hover && Icon == WindowButtonIcon.Close
                ? Color.White
                : MaterialColors.OnSurface;
            if (GlyphFamily != null)
            {
                string glyph = Icon == WindowButtonIcon.Minimize ? "\uE921"
                    : Icon == WindowButtonIcon.Maximize ? "\uE922"
                    : Icon == WindowButtonIcon.Restore ? "\uE923" : "\uE8BB";
                using (Font glyphFont = new Font(GlyphFamily, 9.5f, FontStyle.Regular, GraphicsUnit.Point))
                {
                    TextRenderer.DrawText(e.Graphics, glyph, glyphFont, ClientRectangle, color,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.NoPadding);
                }
                return;
            }

            using (Pen pen = new Pen(color, Math.Max(1f, 1.2f * UiScale)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                int cx = Width / 2;
                int cy = Height / 2;
                int s = Math.Max(4, (int)(5 * UiScale));
                if (Icon == WindowButtonIcon.Minimize)
                {
                    e.Graphics.DrawLine(pen, cx - s, cy + (int)(1 * UiScale), cx + s, cy + (int)(1 * UiScale));
                }
                else if (Icon == WindowButtonIcon.Maximize)
                {
                    e.Graphics.DrawRectangle(pen, cx - s, cy - s, s * 2, s * 2);
                }
                else if (Icon == WindowButtonIcon.Restore)
                {
                    e.Graphics.DrawRectangle(pen, cx - s + (int)(2 * UiScale), cy - s, s * 2, s * 2);
                    e.Graphics.DrawRectangle(pen, cx - s, cy - s + (int)(2 * UiScale), s * 2, s * 2);
                }
                else
                {
                    e.Graphics.DrawLine(pen, cx - s, cy - s, cx + s, cy + s);
                    e.Graphics.DrawLine(pen, cx + s, cy - s, cx - s, cy + s);
                }
            }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            Color parentColor = Parent != null ? Parent.BackColor : MaterialColors.Surface;
            using (SolidBrush brush = new SolidBrush(parentColor))
            {
                pevent.Graphics.FillRectangle(brush, ClientRectangle);
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
    internal sealed class InfoDot : Control
    {
        public float UiScale = 1f;

        private bool _hover;

        public InfoDot()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(18, 18);
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color bg = BackColor;
            if (bg.A == 0 && Parent != null)
            {
                bg = Parent.BackColor;
            }
            using (SolidBrush back = new SolidBrush(bg))
            {
                e.Graphics.FillRectangle(back, ClientRectangle);
            }

            int pad = Math.Max(1, (int)(2 * UiScale));
            Point apex = new Point(Width / 2, pad);
            Point left = new Point(pad, Height - pad);
            Point right = new Point(Width - pad, Height - pad);
            Color fill = _hover ? MaterialColors.ErrorContainer : bg;
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddPolygon(new Point[] { apex, right, left });
                using (SolidBrush b = new SolidBrush(fill))
                {
                    e.Graphics.FillPath(b, path);
                }
                using (Pen pen = new Pen(MaterialColors.Error, Math.Max(1.2f, 1.3f * UiScale)))
                {
                    pen.LineJoin = LineJoin.Round;
                    e.Graphics.DrawPath(pen, path);
                }
            }

            int cx = Width / 2;
            using (Pen pen = new Pen(MaterialColors.Error, Math.Max(1.4f, 1.5f * UiScale)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                e.Graphics.DrawLine(pen, cx, (int)(Height * 0.38f), cx, (int)(Height * 0.62f));
            }
            using (SolidBrush dot = new SolidBrush(MaterialColors.Error))
            {
                int size = Math.Max(2, (int)(2.4f * UiScale));
                e.Graphics.FillEllipse(dot, cx - size / 2, (int)(Height * 0.74f) - size / 2, size, size);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            Color bg = BackColor;
            if (bg.A == 0 && Parent != null)
            {
                bg = Parent.BackColor;
            }
            using (SolidBrush b = new SolidBrush(bg))
            {
                pevent.Graphics.FillRectangle(b, ClientRectangle);
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
    }}
