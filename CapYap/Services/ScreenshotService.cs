using System.Drawing;
using System.IO;
using CapYap.API;
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
        private readonly CapYapApi _capYapApi;

        private readonly string tempScreenshotPath;

        private OverlayWindow? _overlayWindow;

        public ScreenshotService(HotKeyManager hotKeys, CapYapApi capYapApi)
        {
            _hotKeys = hotKeys;
            _capYapApi = capYapApi;

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
                await _capYapApi.UploadCaptureAsync(path);
                toast.SetSuccess("Screen capture uploaded.");
            }
            catch (Exception ex)
            {
                toast.SetFail(ex.Message);
            }
        }
    }
}
