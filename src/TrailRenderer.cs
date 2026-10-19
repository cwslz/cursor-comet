using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MouseTrail
{
    /// <summary>把轨迹点绘制成流光 / 彩虹 / 星火 / 细线。</summary>
    internal static class TrailRenderer
    {
        private const float StampSpacing = 2.0f;
        private const float MaxRadius = 4.8f;
        private const int MaxCometStamps = 1400;
        private const int MaxMinimalStamps = 1600;

        private struct TrailPoint
        {
            public float X;
            public float Y;
            public double T;
        }

        public static void Render(Graphics g, TrailEngine engine, AppSettings cfg, double now,
            double colorTime, int originX, int originY, float headX, float headY,
            float ghostX, float ghostY, bool drawGhost, float dpiScale)
        {
            Color baseColor = Palette.Get(cfg.ColorName);
            List<TrailEngine.Sample> samples = engine.Samples;

            float headFade = 0f;
            if (samples.Count > 0)
            {
                headFade = Fade(now, samples[samples.Count - 1].T, cfg.Life);
            }

            Color headColor = cfg.Style == "rainbow"
                ? Palette.Hsv((colorTime * cfg.RainbowSpeed + cfg.RainbowOffset) % 1.0,
                    cfg.RainbowSaturation, 1.0)
                : baseColor;
            Color glowColor = ResolveEffectColor(cfg.GlowColor, headColor);
            Color haloColor = ResolveEffectColor(cfg.HaloColor, headColor);

            if (cfg.GlowEnabled && cfg.GlowStrength > 0.001)
            {
                if (cfg.Style == "comet")
                {
                    if (!cfg.Enabled)
                    {
                        // 拖尾关闭时没有拖尾可跟随，光晕退回到光标周围，保持可见。
                        DrawCursorGlow(g, cfg, glowColor, headX, headY, dpiScale);
                    }
                    else if (headFade > 0.01f)
                    {
                        // 拖尾开启时，流光的光晕跟随拖尾头部，并随拖尾一起淡出。
                        DrawTrailGlow(g, cfg, glowColor, headX, headY, dpiScale, headFade);
                    }
                }
                else
                {
                    DrawCursorGlow(g, cfg, glowColor, headX, headY, dpiScale);
                }
            }

            if (cfg.Style == "sparks")
            {
                DrawSparks(g, engine, originX, originY);
                DrawHaloRing(g, cfg, haloColor, headX, headY, ghostX, ghostY, drawGhost, dpiScale);
                return;
            }

            if (samples.Count == 0)
            {
                DrawHaloRing(g, cfg, haloColor, headX, headY, ghostX, ghostY, drawGhost, dpiScale);
                return;
            }

            bool rainbow = cfg.Style == "rainbow";
            bool minimal = cfg.Style == "minimal";
            float maxR = (minimal ? 1.35f * (float)cfg.MinimalWidth : MaxRadius) * cfg.Scale * dpiScale;
            float minSpacing = (minimal ? 1.6f : StampSpacing) * dpiScale;

            List<TrailPoint> group = new List<TrailPoint>(samples.Count);
            for (int i = 0; i < samples.Count; i++)
            {
                if (samples[i].Break && group.Count > 0)
                {
                    DrawGroup(g, group, cfg, now, baseColor, maxR, minSpacing, rainbow, minimal, glowColor, dpiScale);
                    group.Clear();
                }
                group.Add(new TrailPoint
                {
                    X = samples[i].X - originX,
                    Y = samples[i].Y - originY,
                    T = samples[i].T
                });
            }
            if (group.Count > 0)
            {
                DrawGroup(g, group, cfg, now, baseColor, maxR, minSpacing, rainbow, minimal, glowColor, dpiScale);
            }

            DrawHaloRing(g, cfg, haloColor, headX, headY, ghostX, ghostY, drawGhost, dpiScale);
        }

        private static Color ResolveEffectColor(string name, Color autoColor)
        {
            if (string.IsNullOrEmpty(name) || string.Equals(name, "auto", StringComparison.Ordinal))
            {
                return autoColor;
            }
            return Palette.Get(name);
        }

        private static void DrawHaloRing(Graphics g, AppSettings cfg, Color color,
            float x, float y, float ghostX, float ghostY, bool drawGhost, float dpiScale)
        {
            if (!cfg.HaloEnabled)
            {
                return;
            }

            float radius = (float)cfg.HaloRadius * dpiScale;
            float width = Math.Max(1f, (float)cfg.HaloThickness * dpiScale);
            int alpha = (int)(255 * (float)cfg.HaloOpacity);
            if (radius <= 0f || alpha <= 0)
            {
                return;
            }

            SmoothingMode old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (drawGhost)
            {
                int ghostAlpha = (int)(alpha * 0.18f);
                if (ghostAlpha > 4)
                {
                    using (Pen ghostPen = new Pen(Color.FromArgb(ghostAlpha, color), width))
                    {
                        ghostPen.Alignment = PenAlignment.Center;
                        g.DrawEllipse(ghostPen, ghostX - radius, ghostY - radius, radius * 2f, radius * 2f);
                    }
                }
            }
            using (Pen pen = new Pen(Color.FromArgb(alpha, color), width))
            {
                pen.Alignment = PenAlignment.Center;
                g.DrawEllipse(pen, x - radius, y - radius, radius * 2f, radius * 2f);
            }
            g.SmoothingMode = old;
        }

        private static void DrawTrailGlow(Graphics g, AppSettings cfg, Color color,
            float x, float y, float dpiScale, float fade)
        {
            if (!cfg.GlowEnabled || fade <= 0.01f)
            {
                return;
            }

            float radius = (float)cfg.GlowRadius * dpiScale;
            int alpha = (int)(110f * (float)cfg.GlowStrength * fade);
            if (radius < 1f || alpha < 1)
            {
                return;
            }
            if (alpha > 235)
            {
                alpha = 235;
            }

            SmoothingMode old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(x - radius, y - radius, radius * 2f, radius * 2f);
                using (PathGradientBrush brush = new PathGradientBrush(path))
                {
                    brush.CenterPoint = new PointF(x, y);
                    brush.CenterColor = Color.FromArgb(alpha, color);
                    brush.SurroundColors = new Color[] { Color.FromArgb(0, color) };
                    g.FillPath(brush, path);
                }
            }
            g.SmoothingMode = old;
        }

        private static void DrawCursorGlow(Graphics g, AppSettings cfg, Color color,
            float x, float y, float dpiScale)
        {
            float radius = (float)cfg.GlowRadius * dpiScale;
            int alpha = (int)(110f * (float)cfg.GlowStrength);
            if (radius < 1f || alpha < 1)
            {
                return;
            }
            if (alpha > 235)
            {
                alpha = 235;
            }

            SmoothingMode old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(x - radius, y - radius, radius * 2f, radius * 2f);
                using (PathGradientBrush brush = new PathGradientBrush(path))
                {
                    brush.CenterPoint = new PointF(x, y);
                    brush.CenterColor = Color.FromArgb(alpha, color);
                    brush.SurroundColors = new Color[] { Color.FromArgb(0, color) };
                    g.FillPath(brush, path);
                }
            }
            g.SmoothingMode = old;
        }

        private static void DrawGroup(Graphics g, List<TrailPoint> group, AppSettings cfg, double now,
            Color baseColor, float maxR, float minSpacing, bool rainbow, bool minimal, Color glowColor,
            float dpiScale)
        {
            if (group.Count == 0)
            {
                return;
            }

            float glow = cfg.Style == "comet" && cfg.GlowEnabled ? (float)cfg.GlowStrength : 0f;
            float coreGlow = cfg.GlowEnabled ? (float)cfg.GlowStrength : 0f;
            float opacity = minimal ? (float)cfg.MinimalOpacity : 1f;

            if (group.Count == 1)
            {
                TrailPoint single = group[0];
                float f = Fade(now, single.T, cfg.Life);
                if (f <= 0.02f)
                {
                    return;
                }

                float r = maxR * (float)Math.Pow(f, 0.7);
                int a = (int)(225f * (float)Math.Pow(f, 1.2) * opacity);
                if (r < 0.5f || a < 2)
                {
                    return;
                }

                using (SolidBrush b = new SolidBrush(Color.FromArgb(a, baseColor)))
                {
                    g.FillEllipse(b, single.X - r, single.Y - r, r * 2f, r * 2f);
                }
                return;
            }

            List<TrailPoint> smooth = new List<TrailPoint>(group.Count * 4);
            Chaikin(group, smooth, 2);

            float total = PathLength(smooth);
            int maxSegments = minimal ? MaxMinimalStamps : MaxCometStamps;
            float spacing = Math.Max(minSpacing, total / maxSegments);
            List<TrailPoint> dense = new List<TrailPoint>((int)(total / spacing) + 4);
            Resample(smooth, dense, spacing);

            if (dense.Count == 0)
            {
                return;
            }

            if (dense.Count == 1)
            {
                TrailPoint only = dense[0];
                float onlyFade = Fade(now, only.T, cfg.Life);
                float onlyRadius = maxR * (float)Math.Pow(onlyFade, 0.62);
                int onlyAlpha = (int)(228f * (float)Math.Pow(onlyFade, 1.15) * opacity);
                if (onlyRadius >= 0.4f && onlyAlpha >= 2)
                {
                    using (SolidBrush dot = new SolidBrush(Color.FromArgb(onlyAlpha, baseColor)))
                    {
                        g.FillEllipse(dot, only.X - onlyRadius, only.Y - onlyRadius,
                            onlyRadius * 2f, onlyRadius * 2f);
                    }
                }
                return;
            }

            // 用相邻点之间的圆头线段拼出连续轨迹。线宽只跟采样点的年龄有关，
            // 不再依赖采样间距，所以移动再快也不会变粗。
            using (Pen pen = new Pen(Color.Black, 1f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                // 外侧光晕半透明且很宽，逐段绘制非常耗时；按年龄分成 6 段，
                // 每段用一条路径一次画完，视觉上仍是连续的柔光。
                if (!minimal && glow > 0.001f)
                {
                    GraphicsPath[] haloPaths = new GraphicsPath[6];
                    for (int b = 0; b < haloPaths.Length; b++)
                    {
                        haloPaths[b] = new GraphicsPath();
                    }
                    for (int i = 1; i < dense.Count; i += 2)
                    {
                        TrailPoint a0 = dense[i - 1];
                        TrailPoint a1 = dense[i];
                        float f = Fade(now, a1.T, cfg.Life);
                        if (f <= 0.015f)
                        {
                            continue;
                        }
                        float a = 228f * (float)Math.Pow(f, 1.15) * opacity;
                        if (a < 1.2f)
                        {
                            continue;
                        }
                        int band = (int)(f * haloPaths.Length);
                        if (band >= haloPaths.Length)
                        {
                            band = haloPaths.Length - 1;
                        }
                        haloPaths[band].AddLine(a0.X, a0.Y, a1.X, a1.Y);
                    }
                    for (int b = 0; b < haloPaths.Length; b++)
                    {
                        if (haloPaths[b].PointCount < 2)
                        {
                            continue;
                        }
                        float f = (b + 0.5f) / haloPaths.Length;
                        int haloAlpha = (int)(228f * (float)Math.Pow(f, 1.15) * opacity * 0.065f * glow);
                        if (haloAlpha <= 0)
                        {
                            continue;
                        }
                        pen.Width = 4.8f * MaxRadius * dpiScale;
                        pen.Color = Color.FromArgb(haloAlpha, glowColor);
                        g.DrawPath(pen, haloPaths[b]);
                    }
                    for (int b = 0; b < haloPaths.Length; b++)
                    {
                        haloPaths[b].Dispose();
                    }
                }

                for (int i = 1; i < dense.Count; i++)
                {
                    TrailPoint a0 = dense[i - 1];
                    TrailPoint a1 = dense[i];
                    float f = Fade(now, a1.T, cfg.Life);
                    if (f <= 0.015f)
                    {
                        continue;
                    }

                    float r = maxR * (float)Math.Pow(f, 0.62);
                    float a = 228f * (float)Math.Pow(f, 1.15) * opacity;
                    if (r < 0.4f || a < 1.2f)
                    {
                        continue;
                    }

                    Color color;
                    if (rainbow)
                    {
                        color = Palette.Hsv((a1.T * cfg.RainbowSpeed + cfg.RainbowOffset) % 1.0,
                            cfg.RainbowSaturation, 1.0);
                    }
                    else
                    {
                        color = Palette.Blend(baseColor, Color.White, 0.15f * f * f);
                    }

                    pen.Width = r * 2f;
                    pen.Color = Color.FromArgb((int)a, color);
                    g.DrawLine(pen, a0.X, a0.Y, a1.X, a1.Y);

                    if (!minimal && f > 0.30f)
                    {
                        pen.Width = r * 0.6f;
                        pen.Color = Color.FromArgb((int)(a * 0.32f * coreGlow), 255, 255, 255);
                        g.DrawLine(pen, a0.X, a0.Y, a1.X, a1.Y);
                    }
                }
            }
        }

        private static void DrawSparks(Graphics g, TrailEngine engine, int originX, int originY)
        {
            List<TrailEngine.Spark> sparks = engine.Sparks;
            if (sparks.Count == 0)
            {
                return;
            }

            using (SolidBrush brush = new SolidBrush(Color.Black))
            {
                for (int i = 0; i < sparks.Count; i++)
                {
                    TrailEngine.Spark s = sparks[i];
                    float life = (float)(s.Life / s.MaxLife);
                    if (life <= 0f)
                    {
                        continue;
                    }

                    float x = s.X - originX;
                    float y = s.Y - originY;
                    float r = s.Size * (0.35f + 0.65f * life);
                    int a = (int)(235f * life * life);
                    if (a < 2)
                    {
                        continue;
                    }

                    Color color = Palette.Hsv(s.Hue, s.Sat, s.Val);

                    float glowR = r * 2.6f;
                    brush.Color = Color.FromArgb(a / 3, color);
                    g.FillEllipse(brush, x - glowR, y - glowR, glowR * 2f, glowR * 2f);

                    brush.Color = Color.FromArgb(a, color);
                    g.FillEllipse(brush, x - r, y - r, r * 2f, r * 2f);

                    float coreR = Math.Max(0.8f, r * 0.38f);
                    brush.Color = Color.FromArgb((int)(a * 0.95f), 255, 255, 255);
                    g.FillEllipse(brush, x - coreR, y - coreR, coreR * 2f, coreR * 2f);
                }
            }
        }

        /// <summary>
        /// Chaikin 切角平滑：曲线始终落在控制点围成的凸包里，不会像 Catmull-Rom 那样过冲打结。
        /// </summary>
        private static void Chaikin(List<TrailPoint> input, List<TrailPoint> output, int passes)
        {
            output.Clear();
            if (input.Count < 3)
            {
                output.AddRange(input);
                return;
            }

            List<TrailPoint> a = new List<TrailPoint>(input);
            List<TrailPoint> b = new List<TrailPoint>(input.Count * 2);

            for (int pass = 0; pass < passes; pass++)
            {
                b.Clear();
                b.Add(a[0]);
                for (int i = 0; i < a.Count - 1; i++)
                {
                    TrailPoint p = a[i];
                    TrailPoint q = a[i + 1];
                    b.Add(Lerp(p, q, 0.25f));
                    b.Add(Lerp(p, q, 0.75f));
                }
                b.Add(a[a.Count - 1]);

                List<TrailPoint> tmp = a;
                a = b;
                b = tmp;
            }

            output.AddRange(a);
        }

        /// <summary>按固定弧长重采样，同时插值每个点的采样时间。</summary>
        private static void Resample(List<TrailPoint> input, List<TrailPoint> output, float spacing)
        {
            output.Clear();
            if (input.Count == 0)
            {
                return;
            }

            output.Add(input[0]);
            float carry = 0f;

            for (int i = 1; i < input.Count; i++)
            {
                TrailPoint a = input[i - 1];
                TrailPoint b = input[i];
                float seg = Distance(a, b);
                if (seg < 0.0001f)
                {
                    continue;
                }

                float pos = 0f;
                while (carry + (seg - pos) >= spacing)
                {
                    float need = spacing - carry;
                    pos += need;
                    float u = pos / seg;
                    output.Add(Lerp(a, b, u));
                    carry = 0f;
                }
                carry += seg - pos;
            }

            TrailPoint last = input[input.Count - 1];
            if (output.Count == 0 || Distance(output[output.Count - 1], last) > spacing * 0.35f)
            {
                output.Add(last);
            }
        }

        private static float PathLength(List<TrailPoint> points)
        {
            float total = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                total += Distance(points[i - 1], points[i]);
            }
            return total;
        }

        private static TrailPoint Lerp(TrailPoint a, TrailPoint b, float t)
        {
            TrailPoint result;
            result.X = a.X + (b.X - a.X) * t;
            result.Y = a.Y + (b.Y - a.Y) * t;
            result.T = a.T + (b.T - a.T) * t;
            return result;
        }

        private static float Distance(TrailPoint a, TrailPoint b)
        {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private static float Fade(double now, double sampleTime, double life)
        {
            if (life <= 0.001)
            {
                return 0f;
            }

            float f = (float)(1.0 - (now - sampleTime) / life);
            if (f < 0f)
            {
                return 0f;
            }
            if (f > 1f)
            {
                return 1f;
            }
            return f;
        }
    }
}
