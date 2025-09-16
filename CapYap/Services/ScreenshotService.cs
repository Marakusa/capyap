using CapYap.HotKeys;
using CapYap.Interfaces;
using CapYap.Properties;
using CapYap.ResultPopUp;
using CapYap.ScreenCapture;
using CapYap.Toast;
using CapYap.Utils.Windows;
using CapYap.Windows;
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
        private readonly AudioUtils _audioUtils;

        private OverlayWindow? _overlayWindow;

        public static event EventHandler? OverlayWindowOpening;
        public static event EventHandler? OverlayWindowOpened;
        public static event EventHandler? OverlayWindowClosed;

        public ScreenshotService(HotKeyManager hotKeys, IApiService apiService, ILogger<OverlayWindow> overlayLogger, AudioUtils audioUtils)
        {
            _hotKeys = hotKeys;
            _apiService = apiService;
            _overlayLogger = overlayLogger;
            _audioUtils = audioUtils;

            Capture();
        }

        public void Capture()
        {
            if (_overlayWindow != null)
            {
                return;
            }

            _audioUtils.PlayAudioClip(AudioClip.Capture);

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

            OverlayWindowOpening?.Invoke(this, EventArgs.Empty);
            ToastManager.HideAllToasts();
            ResultPopUpWindow.Close();

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
                
                OverlayWindowClosed?.Invoke(this, EventArgs.Empty);

                ToastManager.ShowAllToasts();
            };

            _overlayWindow.Show();
            OverlayWindowOpened?.Invoke(this, EventArgs.Empty);
        }

        private async Task UploadImage(string path)
        {
            Toast.Toast toast = new();
            toast.SetWait("Uploading screen capture...");
            try
            {
                Bitmap image = new(path);

                string url = await _apiService.UploadCaptureAsync(path, AppSettings.Default.CompressionQuality, AppSettings.Default.CompressionLevel);
                ClipboardUtils.SetClipboard(url);
                //toast.SetSuccess("Screen capture uploaded and copied to clipboard");
                toast.Close();
                ResultPopUpWindow.Show(image);
                _audioUtils.PlayAudioClip(AudioClip.Complete);
            }
            catch (Exception ex)
            {
                toast.SetFail(ex.Message);
            }
        }
    }
}
