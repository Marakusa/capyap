using CapYap.Utils.Models;
using System.Runtime.InteropServices;
using System.Text;

namespace CapYap.Utils
{
    public static class NativeUtils
    {
        public enum ScreenOrientation : int
        {
            DMDO_DEFAULT = 0,
            DMDO_90 = 1,
            DMDO_180 = 2,
            DMDO_270 = 3
        }

        [Flags()]
        public enum DisplayDeviceStateFlags : int
        {
            /// <summary>The device is part of the desktop.</summary>
            AttachedToDesktop = 0x1,
            MultiDriver = 0x2,
            /// <summary>This is the primary display.</summary>
            PrimaryDevice = 0x4,
            /// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
            MirroringDriver = 0x8,
            /// <summary>The device is VGA compatible.</summary>
            VGACompatible = 0x16,
            /// <summary>The device is removable; it cannot be the primary display.</summary>
            Removable = 0x20,
            /// <summary>The device has more display modes than its output devices support.</summary>
            ModesPruned = 0x8000000,
            Remote = 0x4000000,
            Disconnect = 0x2000000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DisplayDevice
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            [MarshalAs(UnmanagedType.U4)]
            public DisplayDeviceStateFlags StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DEVMODE
        {
            private const int CCHDEVICENAME = 0x20;
            private const int CCHFORMNAME = 0x20;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public ScreenOrientation dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        public const int ENUM_CURRENT_SETTINGS = -1;
        const int ENUM_REGISTRY_SETTINGS = -2;

        [DllImport("user32.dll")]
        public static extern int EnumDisplayDevices(string? lpDevice, int iDevNum, ref DisplayDevice lpDisplayDevice, int dwFlags);


        [StructLayout(LayoutKind.Sequential)]
        public struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        public const int CURSOR_SHOWING = 0x00000001;

        [DllImport("user32.dll")]
        public static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        public static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

        public static Bounds GetFullVirtualBounds()
        {
            // Calculate virtual screen bounds
            int screenLeft = int.MaxValue;
            int screenTop = int.MaxValue;
            int screenRight = int.MinValue;
            int screenBottom = int.MinValue;

            int deviceIndex = 0;
            while (true)
            {
                DisplayDevice deviceData = new() { cb = Marshal.SizeOf(typeof(DisplayDevice)) };

                if (EnumDisplayDevices(null, deviceIndex, ref deviceData, 0) == 0)
                    break;

                // Get the position and size of this particular display device
                DEVMODE devMode = new();
                if (EnumDisplaySettings(deviceData.DeviceName, ENUM_CURRENT_SETTINGS, ref devMode))
                {
                    // Update the virtual screen dimensions
                    screenLeft = Math.Min(screenLeft, devMode.dmPositionX);
                    screenTop = Math.Min(screenTop, devMode.dmPositionY);
                    screenRight = Math.Max(screenRight, devMode.dmPositionX + devMode.dmPelsWidth);
                    screenBottom = Math.Max(screenBottom, devMode.dmPositionY + devMode.dmPelsHeight);
                }

                deviceIndex++;
            }

            return new Bounds(screenLeft, screenTop, screenRight, screenBottom);
        }

        public static Bounds GetCurrentMonitorBounds(int mouseX, int mouseY)
        {
            int deviceIndex = 0;
            while (true)
            {
                DisplayDevice deviceData = new() { cb = Marshal.SizeOf(typeof(DisplayDevice)) };

                if (EnumDisplayDevices(null, deviceIndex, ref deviceData, 0) == 0)
                    break; // no more monitors

                DEVMODE devMode = new();
                if (EnumDisplaySettings(deviceData.DeviceName, ENUM_CURRENT_SETTINGS, ref devMode))
                {
                    int left = devMode.dmPositionX;
                    int top = devMode.dmPositionY;
                    int right = left + devMode.dmPelsWidth;
                    int bottom = top + devMode.dmPelsHeight;

                    // Check if the mouse coordinates fall within this monitor
                    if (mouseX >= left && mouseX < right &&
                        mouseY >= top && mouseY < bottom)
                    {
                        return new Bounds(left, top, right, bottom);
                    }
                }

                deviceIndex++;
            }

            // Fallback: if no match, return full virtual screen
            return new Bounds(0, 0, 0, 0);
        }

        // Delegate used by EnumWindows
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        private enum DWMWINDOWATTRIBUTE
        {
            DWMWA_CLOAKED = 14
        }

        private enum DWM_CLOAKED
        {
            DWM_CLOAKED_APP = 1,
            DWM_CLOAKED_SHELL = 2,
            DWM_CLOAKED_INHERITED = 4
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, out int pvAttribute, int cbAttribute);

        private static bool IsWindowCloaked(IntPtr hWnd)
        {
            if (DwmGetWindowAttribute(hWnd, DWMWINDOWATTRIBUTE.DWMWA_CLOAKED, out int cloaked, Marshal.SizeOf<int>()) == 0)
            {
                return cloaked != 0;
            }
            return false;
        }

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        public static List<(string title, Bounds bounds)> GetOpenWindowsBounds()
        {
            IntPtr shellWindow = GetShellWindow();
            var windows = new List<(string title, Bounds bounds)>();

            EnumWindows(delegate (IntPtr hWnd, IntPtr lParam)
            {
                if (hWnd == shellWindow) return true; // skip desktop
                if (!IsWindowVisible(hWnd)) return true;
                if (IsWindowCloaked(hWnd)) return true;

                int length = GetWindowTextLength(hWnd);
                if (length == 0) return true;

                StringBuilder builder = new(length + 1);
                GetWindowText(hWnd, builder, builder.Capacity);
                string title = builder.ToString();

                if (GetClientRect(hWnd, out RECT clientRect))
                {
                    POINT topLeft = new() { x = clientRect.Left, y = clientRect.Top };
                    ClientToScreen(hWnd, ref topLeft);

                    Bounds bounds = new(
                        topLeft.x,
                        topLeft.y,
                        clientRect.Right + topLeft.x,
                        clientRect.Bottom + topLeft.y
                    );
                    windows.Add((title, bounds));
                }

                return true;
            }, IntPtr.Zero);

            return windows;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        public static readonly IntPtr HWND_TOP = new(0);
        public static readonly IntPtr HWND_BOTTOM = new(1);
        public static readonly IntPtr HWND_TOPMOST = new(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new(-2);

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOACTIVATE = 0x0010;
    }
}
