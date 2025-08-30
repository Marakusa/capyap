using System.Runtime.InteropServices;

namespace CapYap.Utils.Windows.Native
{
    internal static class ClipboardNative
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalAlloc(uint uFlags, IntPtr dwBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        private const uint CF_UNICODETEXT = 13;
        private const uint GMEM_MOVEABLE = 0x0002;

        public static void SetClipboardText(string text, int retries = 50, int delayMs = 10)
        {
            for (int i = 0; i < retries; i++)
            {
                if (OpenClipboard(IntPtr.Zero))
                {
                    try
                    {
                        EmptyClipboard();

                        // Allocate global memory for text
                        var bytes = (text.Length + 1) * 2;
                        IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE, (IntPtr)bytes);

                        IntPtr target = GlobalLock(hGlobal);
                        Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                        Marshal.WriteInt16(target, text.Length * 2, 0); // null-terminator
                        GlobalUnlock(hGlobal);

                        SetClipboardData(CF_UNICODETEXT, hGlobal);
                    }
                    finally
                    {
                        CloseClipboard();
                    }
                    return;
                }

                Thread.Sleep(delayMs); // wait, retry
            }

            throw new Exception("Failed to open clipboard after multiple attempts.");
        }
    }
}
