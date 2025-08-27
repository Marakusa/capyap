using System.Drawing;
using System.IO;
using CapYap.HotKeys;
using CapYap.HotKeys.Models;
using CapYap.Interfaces;
using CapYap.ScreenCapture;
using CapYap.ViewModels.Windows;

namespace CapYap.Services
{
    public class ScreenshotService : IScreenshotService
    {
        private readonly HotKeyManager _hotKeys;
        private readonly string tempScreenshotPath;

        private OverlayWindow? _overlayWindow;

        public ScreenshotService(HotKeyManager hotKeys)
        {
            _hotKeys = hotKeys;
            tempScreenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapYap", "screenshot-temp.jpg");
        }

        public void CaptureAllScreens()
        {
            if (_overlayWindow != null)
            {
                return;
            }

            var screenshotService = new Screenshot();
            Bitmap screenshot = screenshotService.CaptureAllMonitors();

            _overlayWindow = new OverlayWindow(screenshot, tempScreenshotPath);
            _hotKeys.HotKey_CloseCropView += CloseCropView;
            _hotKeys.Rebind(BindingAction.CloseCropView);
            _overlayWindow.Closed += (_, _) =>
            {
                _hotKeys.HotKey_CloseCropView -= CloseCropView;
                _hotKeys.Rebind(BindingAction.CloseCropView);
                _overlayWindow = null;
            };
            _overlayWindow.Show();
        }

        private void CloseCropView(HotKey obj)
        {
            _overlayWindow?.Close();
            _overlayWindow = null;
        }
    }
}
