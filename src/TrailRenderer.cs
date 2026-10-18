using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MouseTrail
{
    /// <summary>把轨迹点绘制成流光 / 彩虹 / 星火 / 细线。</summary>
    internal static class TrailRenderer
    {
        private const float StampSpacing = 1.25f;
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
            int originX, int originY, float headX, float headY, float dpiScale)
        {
            Color baseColor = Palette.Get(cfg.ColorName);
            List<TrailEngine.Sample> samples = engine.Samples;

            float headFade = 0f;
            if (samples.Count > 0)
            {
                headFade = Fade(now, samples[samples.Count - 1].T, cfg.Life);
            }

            Color headColor = cfg.Style == "rainbow"
                ? Palette.Hsv(now * 0.12, 0.85, 1.0)
                : baseColor;
            if (headFade > 0.01f)
            {
                DrawHeadGlow(g, headX, headY, headColor, cfg.Scale * dpiScale, headFade);
            }

            if (cfg.Style == "sparks")
            {
                DrawSparks(g, engine, originX, originY);
                return;
            }

            if (samples.Count == 0)
            {
                return;
            }

            bool rainbow = cfg.Style == "rainbow";
            bool minimal = cfg.Style == "minimal";
            float maxR = (minimal ? 1.35f : MaxRadius) * cfg.Scale * dpiScale;
            float minSpacing = (minimal ? 1.0f : StampSpacing) * dpiScale;

            List<TrailPoint> group = new List<TrailPoint>(samples.Count);
            for (int i = 0; i < samples.Count; i++)
            {
                if (samples[i].Break && group.Count > 0)
                {
                    DrawGroup(g, group, cfg, now, baseColor, maxR, minSpacing, rainbow, minimal);
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
                DrawGroup(g, group, cfg, now, baseColor, maxR, minSpacing, rainbow, minimal);
            }
        }

        private static void DrawGroup(Graphics g, List<TrailPoint> group, AppSettings cfg, double now,
            Color baseColor, float maxR, float minSpacing, bool rainbow, bool minimal)
        {
            if (group.Count == 0)
            {
                return;
            }

            if (group.Count == 1)
            {
                TrailPoint single = group[0];
                float f = Fade(now, single.T, cfg.Life);
                if (f <= 0.02f)
                {
                    return;
                }

                float r = maxR * (float)Math.Pow(f, 0.7);
                int a = (int)(225f * (float)Math.Pow(f, 1.2));
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
                int onlyAlpha = (int)(228f * (float)Math.Pow(onlyFade, 1.15));
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
                    float a = 228f * (float)Math.Pow(f, 1.15);
                    if (r < 0.4f || a < 1.2f)
                    {
                        continue;
                    }

                    Color color;
                    if (rainbow)
                    {
                        color = Palette.Hsv((a1.T * 0.85) % 1.0, 0.92, 1.0);
                    }
                    else
                    {
                        color = Palette.Blend(baseColor, Color.White, 0.15f * f * f);
                    }

                    if (!minimal)
                    {
                        int haloAlpha = (int)(a * 0.065f);
                        if (haloAlpha > 0)
                        {
                            pen.Width = r * 4.8f;
                            pen.Color = Color.FromArgb(haloAlpha, color);
                            g.DrawLine(pen, a0.X, a0.Y, a1.X, a1.Y);
                        }
                    }

                    pen.Width = r * 2f;
                    pen.Color = Color.FromArgb((int)a, color);
                    g.DrawLine(pen, a0.X, a0.Y, a1.X, a1.Y);

                    if (!minimal && f > 0.30f)
                    {
                        pen.Width = r * 0.6f;
                        pen.Color = Color.FromArgb((int)(a * 0.32f), 255, 255, 255);
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

                    Color color = Palette.Hsv(s.Hue, 0.90, 1.0);

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

        private static void DrawHeadGlow(Graphics g, float x, float y, Color color, float scale, float fade)
        {
            float radius = 26f * scale;
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(x - radius, y - radius, radius * 2f, radius * 2f);
                using (PathGradientBrush brush = new PathGradientBrush(path))
                {
                    brush.CenterPoint = new PointF(x, y);
                    brush.CenterColor = Color.FromArgb((int)(125f * fade), color);
                    brush.SurroundColors = new Color[] { Color.FromArgb(0, color) };
                    g.FillPath(brush, path);
                }
            }

            float core = 1.9f * scale;
            using (SolidBrush b = new SolidBrush(Color.FromArgb((int)(130f * fade), 255, 255, 255)))
            {
                g.FillEllipse(b, x - core, y - core, core * 2f, core * 2f);
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
