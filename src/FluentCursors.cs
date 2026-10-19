using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace CursorComet
{
    /// <summary>
    /// 系统手型光标比缩放后的箭头小一圈；这里按箭头尺寸复制一份手型光标，
    /// 让悬停时的大小和普通光标一致。
    /// </summary>
    internal static class FluentCursors
    {
        private static Cursor _hand;
        private static bool _loaded;

        public static Cursor Hand
        {
            get
            {
                if (!_loaded)
                {
                    _loaded = true;
                    try
                    {
                        _hand = CreateScaledHand();
                    }
                    catch
                    {
                        _hand = null;
                    }
                }
                return _hand ?? Cursors.Hand;
            }
        }

        private static Cursor CreateScaledHand()
        {
            Cursor hand = Cursors.Hand;
            int target = GetCursorWidth(Cursors.Default);
            if (target <= hand.Size.Width)
            {
                return Cursors.Hand;
            }

            Native.ICONINFO handInfo;
            if (!Native.GetIconInfo(hand.Handle, out handInfo))
            {
                return Cursors.Hand;
            }

            try
            {
                using (Bitmap bitmap = new Bitmap(target, target, PixelFormat.Format32bppArgb))
                {
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.Clear(Color.Transparent);
                        graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        hand.Draw(graphics, new Rectangle(0, 0, target, target));
                    }

                    IntPtr sourceIcon = bitmap.GetHicon();
                    try
                    {
                        Native.ICONINFO iconInfo;
                        if (!Native.GetIconInfo(sourceIcon, out iconInfo))
                        {
                            return Cursors.Hand;
                        }

                        try
                        {
                            iconInfo.xHotspot = (int)Math.Round(
                                handInfo.xHotspot * (double)target / hand.Size.Width);
                            iconInfo.yHotspot = (int)Math.Round(
                                handInfo.yHotspot * (double)target / hand.Size.Width);

                            IntPtr handle = Native.CreateIconIndirect(ref iconInfo);
                            if (handle == IntPtr.Zero)
                            {
                                return Cursors.Hand;
                            }
                            return new Cursor(handle);
                        }
                        finally
                        {
                            Native.DeleteObject(iconInfo.hbmColor);
                            Native.DeleteObject(iconInfo.hbmMask);
                        }
                    }
                    finally
                    {
                        Native.DestroyIcon(sourceIcon);
                    }
                }
            }
            finally
            {
                Native.DeleteObject(handInfo.hbmColor);
                Native.DeleteObject(handInfo.hbmMask);
            }
        }

        private static int GetCursorWidth(Cursor cursor)
        {
            Native.ICONINFO info;
            if (!Native.GetIconInfo(cursor.Handle, out info))
            {
                return cursor.Size.Width;
            }

            try
            {
                Native.BITMAP bitmap;
                if (info.hbmColor != IntPtr.Zero &&
                    Native.GetObject(info.hbmColor, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.BITMAP)), out bitmap) != 0)
                {
                    return bitmap.bmWidth;
                }
                return cursor.Size.Width;
            }
            finally
            {
                Native.DeleteObject(info.hbmColor);
                Native.DeleteObject(info.hbmMask);
            }
        }
    }
}
