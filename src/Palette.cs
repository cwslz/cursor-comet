using System;
using System.Drawing;

namespace MouseTrail
{
    /// <summary>颜色与 HSV 工具。</summary>
    internal static class Palette
    {
        public static readonly string[] Names = new string[]
        {
            "冰蓝", "紫罗兰", "樱花粉", "金色", "薄荷", "白色"
        };

        public static bool Exists(string name)
        {
            for (int i = 0; i < Names.Length; i++)
            {
                if (Names[i] == name)
                {
                    return true;
                }
            }
            return false;
        }

        public static Color Get(string name)
        {
            if (name == "紫罗兰") return Color.FromArgb(178, 128, 255);
            if (name == "樱花粉") return Color.FromArgb(255, 128, 196);
            if (name == "金色") return Color.FromArgb(255, 198, 92);
            if (name == "薄荷") return Color.FromArgb(96, 255, 186);
            if (name == "白色") return Color.FromArgb(240, 244, 255);
            return Color.FromArgb(72, 214, 255);
        }

        public static Color Blend(Color a, Color b, float t)
        {
            if (t <= 0f) return a;
            if (t >= 1f) return b;
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        public static Color Hsv(double h, double s, double v)
        {
            h = h - Math.Floor(h);
            s = Clamp01(s);
            v = Clamp01(v);

            double i = Math.Floor(h * 6.0);
            double f = h * 6.0 - i;
            double p = v * (1.0 - s);
            double q = v * (1.0 - f * s);
            double t = v * (1.0 - (1.0 - f) * s);

            double r;
            double g;
            double b;
            switch (((int)i) % 6)
            {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                default: r = v; g = p; b = q; break;
            }

            return Color.FromArgb(ToByte(r), ToByte(g), ToByte(b));
        }

        public static void ToHsv(Color color, out double h, out double s, out double v)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            v = max;
            s = max <= 0.0 ? 0.0 : delta / max;

            if (delta <= 0.0)
            {
                h = 0.0;
                return;
            }

            if (max == r) h = ((g - b) / delta) % 6.0;
            else if (max == g) h = (b - r) / delta + 2.0;
            else h = (r - g) / delta + 4.0;

            h /= 6.0;
            if (h < 0.0) h += 1.0;
        }

        private static double Clamp01(double value)
        {
            if (value < 0.0) return 0.0;
            if (value > 1.0) return 1.0;
            return value;
        }

        private static int ToByte(double value)
        {
            int result = (int)Math.Round(value * 255.0);
            if (result < 0) return 0;
            if (result > 255) return 255;
            return result;
        }
    }
}
