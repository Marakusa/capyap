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
        private readonly ILogger<ScreenshotService> _log;
        private readonly HotKeyManager _hotKeys;
        private readonly IApiService _apiService;
        private readonly ILogger<OverlayWindow> _overlayLogger;
        private readonly AudioUtils _audioUtils;

        private OverlayWindow? _overlayWindow;

        public static event EventHandler? OverlayWindowOpening;
        public static event EventHandler? OverlayWindowOpened;
        public static event EventHandler? OverlayWindowClosed;

        public ScreenshotService(ILogger<ScreenshotService> log, HotKeyManager hotKeys, IApiService apiService, ILogger<OverlayWindow> overlayLogger, AudioUtils audioUtils)
        {
            _log = log;
            _hotKeys = hotKeys;
            _apiService = apiService;
            _overlayLogger = overlayLogger;
            _audioUtils = audioUtils;
        }

        public void Capture()
        {
            if (_overlayWindow != null)
            {
                _log.LogError("Overlay is already open.");
                return;
            }

            _log.LogInformation("Playing capture sound...");

            _audioUtils.PlayAudioClip(AudioClip.Capture);

            _log.LogInformation("Capturing screen...");

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

            _log.LogInformation("Screen captured in format: {Format}", format);

            string tempScreenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapYap", "screenshot-temp." + format);

            _log.LogInformation("Screen capture saved to: {Path}", tempScreenshotPath);

            _log.LogInformation("Opening overlay window...");

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

            _log.LogInformation("Showing overlay window...");

            _overlayWindow.Show();
            OverlayWindowOpened?.Invoke(this, EventArgs.Empty);
        }

        private async Task UploadImage(string path)
        {
            _log.LogInformation("Uploading screen capture...");

            Toast.Toast toast = new();
            toast.SetWait("Uploading screen capture...");
            try
            {
                Bitmap image = new(path);

                string url = await _apiService.UploadCaptureAsync(path, AppSettings.Default.CompressionQuality, AppSettings.Default.CompressionLevel);
                _log.LogInformation("Screen capture uploaded to: {Url}", url);
                ClipboardUtils.SetClipboard(url);
                //toast.SetSuccess("Screen capture uploaded and copied to clipboard");
                toast.Close();
                ResultPopUpWindow.Show(image);
                _audioUtils.PlayAudioClip(AudioClip.Complete);
                _log.LogInformation("Upload completed.");
            }
            catch (Exception ex)
            {
                toast.SetFail(ex.Message);
                _log.LogError("Failed to upload screen capture. {Error}", ex.Message);
            }
        }
    }
}
