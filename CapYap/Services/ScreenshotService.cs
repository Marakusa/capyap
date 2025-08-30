using CapYap.HotKeys;
using CapYap.HotKeys.Models;
using CapYap.Interfaces;
using CapYap.ScreenCapture;
using CapYap.Utils.Windows;
using CapYap.ViewModels.Windows;
using System.Drawing;
using System.IO;

namespace CapYap.Services
{
    public class ScreenshotService : IScreenshotService
    {
        private readonly HotKeyManager _hotKeys;
        private readonly IApiService _apiService;

        private readonly string tempScreenshotPath;

        private OverlayWindow? _overlayWindow;

        public ScreenshotService(HotKeyManager hotKeys, IApiService apiService)
        {
            _hotKeys = hotKeys;
            _apiService = apiService;

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

            _overlayWindow = new OverlayWindow(screenshot, tempScreenshotPath, async (path) =>
            {
                if (path != null)
                {
                    await UploadImage(tempScreenshotPath);
                }
            });
            _hotKeys.HotKey_CloseCropView += CloseCropView;
            _hotKeys.Rebind(BindingAction.CloseCropView, System.Windows.Input.Key.Escape, KeyModifier.None);
            _overlayWindow.Closed += (_, _) =>
            {
                _hotKeys.HotKey_CloseCropView -= CloseCropView;
                _hotKeys.Rebind(BindingAction.CloseCropView, System.Windows.Input.Key.Escape, KeyModifier.None);
                _overlayWindow = null;
            };
            _overlayWindow.Show();
        }

        private void CloseCropView(HotKey obj)
        {
            _overlayWindow?.Close();
            _overlayWindow = null;
        }

        private async Task UploadImage(string path)
        {
            Toast.Toast toast = new();
            toast.SetWait("Uploading screen capture...");
            try
            {
                string url = await _apiService.UploadCaptureAsync(path);
                ClipboardUtils.SetClipboard(url);
                toast.SetSuccess("Screen capture uploaded and copied to clipboard");
            }
            catch (Exception ex)
            {
                toast.SetFail(ex.Message);
            }
        }
    }
}
