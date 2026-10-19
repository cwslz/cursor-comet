using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace MouseTrail
{
    /// <summary>用户配置，保存在 %APPDATA%\MouseTrail\config.ini。</summary>
    internal sealed class AppSettings
    {
        public const string DefaultStyle = "comet";
        public const string DefaultColor = "cyan";
        public const double DefaultLife = 0.7;
        public const float DefaultScale = 1f;

        public string Style = DefaultStyle;
        public string ColorName = DefaultColor;
        public float Scale = DefaultScale;
        public double Life = DefaultLife;
        public bool Enabled = true;
        public bool ShowTip = true;
        public string Language = "auto";

        public double GlowStrength = 1.0;
        public bool GlowEnabled = true;
        public double GlowRadius = 42;
        public string GlowColor = "auto";
        public bool HaloEnabled = true;
        public double HaloRadius = 26;
        public double HaloThickness = 2.5;
        public double HaloOpacity = 0.9;
        public string HaloColor = "auto";
        public double RainbowSpeed = 0.85;
        public double RainbowSaturation = 0.92;
        public double RainbowOffset = 0.0;
        public double SparkRate = 1.0;
        public double SparkSize = 1.0;
        public double SparkLife = 1.0;
        public double SparkSpeed = 1.0;
        public double MinimalWidth = 1.0;
        public double MinimalOpacity = 1.0;

        public static string Folder
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MouseTrail");
            }
        }

        public static string FilePath
        {
            get { return Path.Combine(Folder, "config.ini"); }
        }

        public void Reset()
        {
            Style = DefaultStyle;
            ColorName = DefaultColor;
            Scale = DefaultScale;
            Life = DefaultLife;
            GlowStrength = 1.0;
            GlowEnabled = true;
            GlowRadius = 42;
            GlowColor = "auto";
            HaloEnabled = true;
            HaloRadius = 26;
            HaloThickness = 2.5;
            HaloOpacity = 0.9;
            HaloColor = "auto";
            RainbowSpeed = 0.85;
            RainbowSaturation = 0.92;
            RainbowOffset = 0.0;
            SparkRate = 1.0;
            SparkSize = 1.0;
            SparkLife = 1.0;
            SparkSpeed = 1.0;
            MinimalWidth = 1.0;
            MinimalOpacity = 1.0;
        }

        public string ResolveLanguage()
        {
            if (Language == "zh" || Language == "en")
            {
                return Language;
            }
            return L10n.SystemDefault();
        }

        public static AppSettings Load()
        {
            AppSettings settings = new AppSettings();
            bool hasLife = false;
            int legacyLength = -1;

            try
            {
                if (!File.Exists(FilePath))
                {
                    return settings;
                }

                string[] lines = File.ReadAllLines(FilePath, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0 || line[0] == '#' || line[0] == ';')
                    {
                        continue;
                    }

                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                    {
                        continue;
                    }

                    string key = line.Substring(0, eq).Trim().ToLowerInvariant();
                    string val = line.Substring(eq + 1).Trim();

                    if (key == "style" && IsKnownStyle(val))
                    {
                        settings.Style = val;
                    }
                    else if (key == "color")
                    {
                        string color = Palette.Normalize(val);
                        if (color != null)
                        {
                            settings.ColorName = color;
                        }
                    }
                    else if (key == "scale")
                    {
                        settings.Scale = ParseFloat(val, settings.Scale);
                    }
                    else if (key == "life")
                    {
                        settings.Life = ParseDouble(val, settings.Life);
                        hasLife = true;
                    }
                    else if (key == "length")
                    {
                        // 旧版按长度配置，这里换算成大致对应的持续时间。
                        legacyLength = ParseInt(val, 300);
                    }
                    else if (key == "enabled")
                    {
                        settings.Enabled = ParseBool(val, settings.Enabled);
                    }
                    else if (key == "showtip")
                    {
                        settings.ShowTip = ParseBool(val, settings.ShowTip);
                    }
                    else if (key == "lang" && (val == "auto" || val == "zh" || val == "en"))
                    {
                        settings.Language = val;
                    }
                    else if (key == "glowstrength")
                    {
                        settings.GlowStrength = ParseDouble(val, settings.GlowStrength);
                    }
                    else if (key == "glowenabled")
                    {
                        settings.GlowEnabled = ParseBool(val, settings.GlowEnabled);
                    }
                    else if (key == "glowradius")
                    {
                        settings.GlowRadius = ParseDouble(val, settings.GlowRadius);
                    }
                    else if (key == "glowcolor")
                    {
                        settings.GlowColor = ParseEffectColor(val, settings.GlowColor);
                    }
                    else if (key == "halocolor")
                    {
                        settings.HaloColor = ParseEffectColor(val, settings.HaloColor);
                    }
                    else if (key == "haloenabled")
                    {
                        settings.HaloEnabled = ParseBool(val, settings.HaloEnabled);
                    }
                    else if (key == "haloradius")
                    {
                        settings.HaloRadius = ParseDouble(val, settings.HaloRadius);
                    }
                    else if (key == "halothickness")
                    {
                        settings.HaloThickness = ParseDouble(val, settings.HaloThickness);
                    }
                    else if (key == "haloopacity")
                    {
                        settings.HaloOpacity = ParseDouble(val, settings.HaloOpacity);
                    }
                    else if (key == "rainbowspeed")
                    {
                        settings.RainbowSpeed = ParseDouble(val, settings.RainbowSpeed);
                    }
                    else if (key == "rainbowsat")
                    {
                        settings.RainbowSaturation = ParseDouble(val, settings.RainbowSaturation);
                    }
                    else if (key == "rainbowoffset")
                    {
                        settings.RainbowOffset = ParseDouble(val, settings.RainbowOffset);
                    }
                    else if (key == "sparkrate")
                    {
                        settings.SparkRate = ParseDouble(val, settings.SparkRate);
                    }
                    else if (key == "sparksize")
                    {
                        settings.SparkSize = ParseDouble(val, settings.SparkSize);
                    }
                    else if (key == "sparklife")
                    {
                        settings.SparkLife = ParseDouble(val, settings.SparkLife);
                    }
                    else if (key == "sparkspeed")
                    {
                        settings.SparkSpeed = ParseDouble(val, settings.SparkSpeed);
                    }
                    else if (key == "minimalwidth")
                    {
                        settings.MinimalWidth = ParseDouble(val, settings.MinimalWidth);
                    }
                    else if (key == "minimalopacity")
                    {
                        settings.MinimalOpacity = ParseDouble(val, settings.MinimalOpacity);
                    }
                }
            }
            catch (Exception ex)
            {
                TryBackupCorruptConfig();
                Program.Log(ex);
            }

            if (!hasLife && legacyLength > 0)
            {
                settings.Life = LengthToLife(legacyLength);
            }

            if (settings.Life < 0.2)
            {
                settings.Life = 0.2;
            }
            if (settings.Life > 1.5)
            {
                settings.Life = 1.5;
            }
            if (settings.Scale < 0.5f)
            {
                settings.Scale = 0.5f;
            }
            if (settings.Scale > 2f)
            {
                settings.Scale = 2f;
            }
            settings.GlowStrength = ClampDouble(settings.GlowStrength, 0.0, 2.0);
            settings.GlowRadius = ClampDouble(settings.GlowRadius, 8.0, 120.0);
            settings.HaloRadius = ClampDouble(settings.HaloRadius, 8.0, 80.0);
            settings.HaloThickness = ClampDouble(settings.HaloThickness, 1.0, 10.0);
            settings.HaloOpacity = ClampDouble(settings.HaloOpacity, 0.1, 1.0);
            settings.RainbowSpeed = ClampDouble(settings.RainbowSpeed, 0.1, 2.0);
            settings.RainbowSaturation = ClampDouble(settings.RainbowSaturation, 0.2, 1.0);
            settings.RainbowOffset = ClampDouble(settings.RainbowOffset, 0.0, 1.0);
            settings.SparkRate = ClampDouble(settings.SparkRate, 0.2, 2.0);
            settings.SparkSize = ClampDouble(settings.SparkSize, 0.5, 2.0);
            settings.SparkLife = ClampDouble(settings.SparkLife, 0.5, 2.0);
            settings.SparkSpeed = ClampDouble(settings.SparkSpeed, 0.5, 2.0);
            settings.MinimalWidth = ClampDouble(settings.MinimalWidth, 0.5, 2.0);
            settings.MinimalOpacity = ClampDouble(settings.MinimalOpacity, 0.3, 1.0);
            return settings;
        }

        private static double ClampDouble(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static string ParseEffectColor(string value, string fallback)
        {
            if (string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase))
            {
                return "auto";
            }
            string color = Palette.Normalize(value);
            return color ?? fallback;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Folder);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# MouseTrail / 鼠标拖尾 配置：可直接修改，重启后生效");
                sb.AppendLine("# 样式 / Style: comet / rainbow / sparks / minimal");
                sb.AppendLine("style=" + Style);
                sb.AppendLine("# 颜色 / Color: cyan / violet / pink / gold / mint / white");
                sb.AppendLine("color=" + ColorName);
                sb.AppendLine("scale=" + Scale.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine("life=" + Life.ToString("0.##", CultureInfo.InvariantCulture));
                sb.AppendLine("enabled=" + (Enabled ? "1" : "0"));
                sb.AppendLine("showtip=" + (ShowTip ? "1" : "0"));
                sb.AppendLine("# 语言 / Language: auto / zh / en");
                sb.AppendLine("lang=" + Language);
                sb.AppendLine("# 特效参数 / Style parameters");
                sb.AppendLine("glowenabled=" + (GlowEnabled ? "1" : "0"));
                sb.AppendLine("glowstrength=" + GlowStrength.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine("glowradius=" + GlowRadius.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine("glowcolor=" + GlowColor);
                sb.AppendLine("haloenabled=" + (HaloEnabled ? "1" : "0"));
                sb.AppendLine("haloradius=" + HaloRadius.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine("halothickness=" + HaloThickness.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine("haloopacity=" + HaloOpacity.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine("halocolor=" + HaloColor);
                sb.AppendLine("rainbowspeed=" + RainbowSpeed.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine("rainbowsat=" + RainbowSaturation.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine("rainbowoffset=" + RainbowOffset.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine("sparkrate=" + SparkRate.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine("sparksize=" + SparkSize.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine("sparklife=" + SparkLife.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine("sparkspeed=" + SparkSpeed.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine("minimalwidth=" + MinimalWidth.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine("minimalopacity=" + MinimalOpacity.ToString("0.###", CultureInfo.InvariantCulture));

                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, sb.ToString(), new UTF8Encoding(false));
                if (File.Exists(FilePath))
                {
                    File.Replace(tmp, FilePath, FilePath + ".bak");
                }
                else
                {
                    File.Move(tmp, FilePath);
                }
            }
            catch (Exception ex)
            {
                Program.Log(ex);
            }
        }

        private static void TryBackupCorruptConfig()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return;
                }
                string backup = FilePath + ".corrupt-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                File.Copy(FilePath, backup, true);
            }
            catch
            {
            }
        }

        public static bool IsKnownStyle(string value)
        {
            return value == "comet" || value == "rainbow" || value == "sparks" || value == "minimal";
        }

        public static int ParseInt(string value, int fallback)
        {
            int result;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }
            return fallback;
        }

        public static float ParseFloat(string value, float fallback)
        {
            float result;
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }
            return fallback;
        }

        public static double ParseDouble(string value, double fallback)
        {
            double result;
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }
            return fallback;
        }

        public static bool ParseBool(string value, bool fallback)
        {
            if (string.IsNullOrEmpty(value))
            {
                return fallback;
            }
            if (value == "1" ||
                string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "on", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (value == "0" ||
                string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "off", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "no", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return fallback;
        }

        private static double LengthToLife(int length)
        {
            if (length <= 220)
            {
                return 0.45;
            }
            if (length >= 380)
            {
                return 1.0;
            }
            return 0.7;
        }
    }
}
