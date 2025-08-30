using CapYap.Utils.Windows.Native;

namespace CapYap.Utils.Windows
{
    public static class ClipboardUtils
    {
        public static void SetClipboard(string text)
        {
            ClipboardNative.SetClipboardText(text);
        }
    }
}
