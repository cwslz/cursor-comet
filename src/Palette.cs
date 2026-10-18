using System;
using System.Drawing;

namespace MouseTrail
{
    internal sealed class ColorDef
    {
        public readonly string Key;
        public readonly string Zh;
        public readonly string En;
        public readonly Color Value;

        public ColorDef(string key, string zh, string en, Color value)
        {
            Key = key;
            Zh = zh;
            En = en;
            Value = value;
        }
    }

    /// <summary>颜色定义与 HSV 工具。配置里存稳定的 key，界面显示按语言切换。</summary>
    internal static class Palette
    {
        private static readonly ColorDef[] _definitions = new ColorDef[]
        {
            new ColorDef("cyan", "冰蓝", "Ice Blue", Color.FromArgb(72, 214, 255)),
            new ColorDef("violet", "紫罗兰", "Violet", Color.FromArgb(178, 128, 255)),
            new ColorDef("pink", "樱花粉", "Sakura Pink", Color.FromArgb(255, 128, 196)),
            new ColorDef("gold", "金色", "Gold", Color.FromArgb(255, 198, 92)),
            new ColorDef("mint", "薄荷", "Mint", Color.FromArgb(96, 255, 186)),
            new ColorDef("white", "白色", "White", Color.FromArgb(240, 244, 255))
        };

        public static ColorDef[] Definitions
        {
            get { return _definitions; }
        }

        public static ColorDef Find(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            for (int i = 0; i < _definitions.Length; i++)
            {
                ColorDef def = _definitions[i];
                if (value == def.Key || value == def.Zh || value == def.En)
                {
                    return def;
                }
            }
            return null;
        }

        public static string Normalize(string value)
        {
            ColorDef def = Find(value);
            return def == null ? null : def.Key;
        }

        public static bool Exists(string value)
        {
            return Find(value) != null;
        }

        public static Color Get(string value)
        {
            ColorDef def = Find(value);
            return def == null ? _definitions[0].Value : def.Value;
        }

        public static string Display(string value)
        {
            ColorDef def = Find(value);
            if (def == null)
            {
                return value;
            }
            return L10n.IsChinese ? def.Zh : def.En;
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
