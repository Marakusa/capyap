using System.Drawing;
using System.Runtime.InteropServices;
using CapYap.Utils;
using CapYap.Utils.Models;
using Serilog;

namespace CapYap.ScreenCapture.Windows
{
    public class Screenshot
    {
        private readonly ILogger _log;

        public Screenshot(ILogger log)
        {
            _log = log;
        }

        public bool CaptureCursor { get; set; } = false;

        public Bitmap CaptureAllMonitors()
        {
            Bounds bounds = NativeUtils.GetFullVirtualBounds();

            int left = bounds.Left;
            int top = bounds.Top;
            int right = bounds.Right;
            int bottom = bounds.Bottom;

            // This can happen when in example the primary monitor is on the right side and left will be on the negative for the second monitor
            if (left < 0)
            {
                int differenceLeft = Math.Abs(left);
                left += differenceLeft;
                right += differenceLeft;
            }
            if (top < 0)
            {
                int differenceTop = Math.Abs(top);
                top += differenceTop;
                bottom += differenceTop;
            }
            if (left > 0)
            {
                left = 0;
                right -= left;
            }
            if (top > 0)
            {
                top = 0;
                bottom -= top;
            }

            // Create a bitmap of the appropriate size to receive the screen-shot.
            Bitmap bmp = new(right, bottom);

            // Draw the screen-shot into our bitmap.
            using Graphics graphics = Graphics.FromImage(bmp);
            _log.Information("Bounds: {Left}, {Top}, {Right}, {Bottom}", left, top, right, bottom);
            _log.Information("Bmp Size: {Size} (x 2)", bmp.Size);
            graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bmp.Size);

            // Capture cursor
            if (CaptureCursor)
            {
                var ci = new NativeUtils.CURSORINFO();
                ci.cbSize = Marshal.SizeOf(typeof(NativeUtils.CURSORINFO));
                if (NativeUtils.GetCursorInfo(out ci) && (ci.flags & NativeUtils.CURSOR_SHOWING) != 0)
                {
                    int cursorX = ci.ptScreenPos.x - left;
                    int cursorY = ci.ptScreenPos.y - top;
                    NativeUtils.DrawIcon(graphics.GetHdc(), cursorX, cursorY, ci.hCursor);
                    graphics.ReleaseHdc();
                }
            }

            return bmp;
        }
    }
}
