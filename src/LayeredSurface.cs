using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MouseTrail
{
    /// <summary>
    /// 一块 32 位预乘 Alpha 的离屏画布，配合 UpdateLayeredWindow 实现逐像素透明。
    /// </summary>
    internal sealed class LayeredSurface : IDisposable
    {
        private readonly int _width;
        private readonly int _height;
        private IntPtr _screenDc;
        private IntPtr _memDc;
        private IntPtr _hBitmap;
        private IntPtr _oldBitmap;
        private IntPtr _bits;
        private Bitmap _bitmap;
        private Graphics _graphics;
        private bool _disposed;

        public LayeredSurface(int width, int height)
        {
            _width = width;
            _height = height;

            try
            {
            _screenDc = Native.GetDC(IntPtr.Zero);
            _memDc = Native.CreateCompatibleDC(_screenDc);

            Native.BITMAPINFO bmi = new Native.BITMAPINFO();
            bmi.bmiHeader.biSize = Marshal.SizeOf(typeof(Native.BITMAPINFOHEADER));
            bmi.bmiHeader.biWidth = width;
            bmi.bmiHeader.biHeight = -height; // 自上而下
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 32;
            bmi.bmiHeader.biCompression = Native.BI_RGB;

            _hBitmap = Native.CreateDIBSection(_memDc, ref bmi, 0, out _bits, IntPtr.Zero, 0);
            if (_hBitmap == IntPtr.Zero)
            {
                throw new InvalidOperationException("创建离屏画布失败。");
            }

            _oldBitmap = Native.SelectObject(_memDc, _hBitmap);
            _bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppPArgb, _bits);
            _graphics = Graphics.FromImage(_bitmap);
            _graphics.SmoothingMode = SmoothingMode.AntiAlias;
            _graphics.PixelOffsetMode = PixelOffsetMode.Half;
            _graphics.CompositingQuality = CompositingQuality.HighSpeed;
            _graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
            _graphics.Clear(Color.Transparent);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public int Width
        {
            get { return _width; }
        }

        public int Height
        {
            get { return _height; }
        }

        public Graphics Graphics
        {
            get { return _graphics; }
        }

        /// <summary>把画布内容提交到窗口，并同时移动窗口到 (x, y)。</summary>
        public void Present(IntPtr hwnd, int x, int y)
        {
            if (_disposed)
            {
                return;
            }

            _graphics.Flush(FlushIntention.Sync);

            Native.POINT dst = new Native.POINT(x, y);
            Native.POINT src = new Native.POINT(0, 0);
            Native.SIZE size = new Native.SIZE(_width, _height);
            Native.BLENDFUNCTION blend = new Native.BLENDFUNCTION();
            blend.BlendOp = Native.AC_SRC_OVER;
            blend.BlendFlags = 0;
            blend.SourceConstantAlpha = 255;
            blend.AlphaFormat = Native.AC_SRC_ALPHA;

            Native.UpdateLayeredWindow(hwnd, _screenDc, ref dst, ref size, _memDc, ref src, 0, ref blend, Native.ULW_ALPHA);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            if (_graphics != null)
            {
                _graphics.Dispose();
                _graphics = null;
            }
            if (_bitmap != null)
            {
                _bitmap.Dispose();
                _bitmap = null;
            }
            if (_memDc != IntPtr.Zero && _oldBitmap != IntPtr.Zero)
            {
                Native.SelectObject(_memDc, _oldBitmap);
                _oldBitmap = IntPtr.Zero;
            }
            if (_hBitmap != IntPtr.Zero)
            {
                Native.DeleteObject(_hBitmap);
                _hBitmap = IntPtr.Zero;
            }
            if (_memDc != IntPtr.Zero)
            {
                Native.DeleteDC(_memDc);
                _memDc = IntPtr.Zero;
            }
            if (_screenDc != IntPtr.Zero)
            {
                Native.ReleaseDC(IntPtr.Zero, _screenDc);
                _screenDc = IntPtr.Zero;
            }
            _bits = IntPtr.Zero;
        }
    }
}
