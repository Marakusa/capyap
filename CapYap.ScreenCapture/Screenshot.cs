using CapYap.ScreenCapture.Helpers;
using CapYap.ScreenCapture.Models;
using System.Drawing;
using System.Runtime.InteropServices;

namespace CapYap.ScreenCapture
{
    public class Screenshot
    {
        public bool CaptureCursor { get; set; } = false;

        public Bitmap CaptureAllMonitors()
        {
            Bounds bounds = NativeUtils.GetFullVirtualBounds();

            // Create a bitmap of the appropriate size to receive the screen-shot.
            Bitmap bmp = new(bounds.Right, bounds.Bottom);

            // Draw the screen-shot into our bitmap.
            using Graphics graphics = Graphics.FromImage(bmp);
            graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bmp.Size * 2);

            // Capture cursor
            if (CaptureCursor)
            {
                var ci = new NativeUtils.CURSORINFO();
                ci.cbSize = Marshal.SizeOf(typeof(NativeUtils.CURSORINFO));
                if (NativeUtils.GetCursorInfo(out ci) && (ci.flags & NativeUtils.CURSOR_SHOWING) != 0)
                {
                    int cursorX = ci.ptScreenPos.x - bounds.Left;
                    int cursorY = ci.ptScreenPos.y - bounds.Top;
                    NativeUtils.DrawIcon(graphics.GetHdc(), cursorX, cursorY, ci.hCursor);
                    graphics.ReleaseHdc();
                }
            }

            return bmp;
        }
    }
}
