using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace CursorComet
{
    /// <summary>运行时绘制程序 / 托盘图标，避免额外的资源文件。</summary>
    internal static class IconFactory
    {
        public static Icon CreateAppIcon()
        {
            return Build(32);
        }

        public static Icon CreateTrayIcon()
        {
            return Build(32);
        }

        private static Icon Build(int size)
        {
            using (Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);

                    float s = size / 32f;

                    using (GraphicsPath tail = new GraphicsPath())
                    {
                        tail.AddArc(2f * s, 4f * s, 22f * s, 22f * s, 120f, 110f);
                        using (Pen pen = new Pen(Color.FromArgb(140, 90, 205, 255), 3.4f * s))
                        {
                            pen.StartCap = LineCap.Round;
                            pen.EndCap = LineCap.Round;
                            g.DrawPath(pen, tail);
                        }
                    }

                    using (GraphicsPath head = new GraphicsPath())
                    {
                        head.AddEllipse(10f * s, 10f * s, 16f * s, 16f * s);
                        using (PathGradientBrush brush = new PathGradientBrush(head))
                        {
                            brush.CenterColor = Color.White;
                            brush.SurroundColors = new Color[] { Color.FromArgb(255, 60, 190, 255) };
                            g.FillPath(brush, head);
                        }
                    }

                    using (SolidBrush dot = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
                    {
                        g.FillEllipse(dot, 14.5f * s, 14.5f * s, 7f * s, 7f * s);
                    }
                }

                IntPtr handle = bmp.GetHicon();
                try
                {
                    using (Icon temp = Icon.FromHandle(handle))
                    {
                        return (Icon)temp.Clone();
                    }
                }
                finally
                {
                    Native.DestroyIcon(handle);
                }
            }
        }
    }
}
