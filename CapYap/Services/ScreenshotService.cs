using CapYap.HotKeys;
using CapYap.Interfaces;
using CapYap.Properties;
using CapYap.ScreenCapture;
using CapYap.Utils.Windows;
using CapYap.ViewModels.Windows;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.IO;

namespace CapYap.Services
{
    public class ScreenshotService : IScreenshotService
    {
        private readonly HotKeyManager _hotKeys;
        private readonly IApiService _apiService;
        private readonly ILogger<OverlayWindow> _overlayLogger;

        private OverlayWindow? _overlayWindow;

        public ScreenshotService(HotKeyManager hotKeys, IApiService apiService, ILogger<OverlayWindow> overlayLogger)
        {
            _hotKeys = hotKeys;
            _apiService = apiService;
            _overlayLogger = overlayLogger;
        }

        public void CaptureAllScreens()
        {
            if (_overlayWindow != null)
            {
                return;
            }

            var screenshotService = new Screenshot();
            Bitmap screenshot = screenshotService.CaptureAllMonitors();

            string format = "jpg";

            switch (AppSettings.Default.UploadFormat)
            {
                default:
                case 0:
                    format = "jpg";
                    break;
                case 1:
                    format = "png";
                    break;
                case 2:
                    format = "gif";
                    break;
            }

            string tempScreenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapYap", "screenshot-temp." + format);

            _overlayWindow = new OverlayWindow(_overlayLogger, screenshot, tempScreenshotPath, async (path) =>
            {
                if (path != null)
                {
                    await UploadImage(tempScreenshotPath);
                }
            }, _hotKeys);
            _overlayWindow.Closed += (_, _) =>
            {
                _overlayWindow = null;
            };
            _overlayWindow.Show();
        }

        private async Task UploadImage(string path)
        {
            Toast.Toast toast = new();
            toast.SetWait("Uploading screen capture...");
            try
            {
                string url = await _apiService.UploadCaptureAsync(path, AppSettings.Default.CompressionQuality, AppSettings.Default.CompressionLevel);
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
