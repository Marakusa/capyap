using CapYap.HotKeys.Windows;
using CapYap.Interfaces;
using CapYap.ResultPopUp;
using CapYap.ScreenCapture.Windows;
using CapYap.Settings;
using CapYap.Toast;
using CapYap.Utils.Windows;
using CapYap.Windows;
using Serilog;
using System.Drawing;
using System.IO;

namespace CapYap.Services
{
    public class ScreenshotService : IScreenshotService
    {
        private readonly ILogger _log;
        private readonly HotKeyManager _hotKeys;
        private readonly IApiService _apiService;
        private readonly AudioUtils _audioUtils;

        private OverlayWindow? _overlayWindow;

        public static event EventHandler? OverlayWindowOpening;
        public static event EventHandler? OverlayWindowOpened;
        public static event EventHandler? OverlayWindowClosed;

        public ScreenshotService(ILogger log, HotKeyManager hotKeys, IApiService apiService, AudioUtils audioUtils)
        {
            _log = log;
            _hotKeys = hotKeys;
            _apiService = apiService;
            _audioUtils = audioUtils;
        }

        public void Capture()
        {
            try
            {
                if (_overlayWindow != null && _overlayWindow.IsVisible)
                {
                    _log.Error("Overlay is already open.");
                    return;
                }

                _audioUtils.PlayAudioClip(AudioClip.Capture);

                var screenshotService = new Screenshot(_log);
                Bitmap screenshot = screenshotService.CaptureAllMonitors();

                string format = "jpg";

                format = UserSettingsManager.Current.UploadSettings.UploadFormat switch
                {
                    1 => "png",
                    2 => "gif",
                    _ => "jpg",
                };

                string tempScreenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapYap", "screenshot-temp." + format);

                OverlayWindowOpening?.Invoke(this, EventArgs.Empty);
                ToastManager.HideAllToasts();
                ResultPopUpWindow.Close();

                if (_overlayWindow == null)
                {
                    _overlayWindow = new OverlayWindow(_log, screenshot, tempScreenshotPath, async (path) =>
                    {
                        if (path != null)
                        {
                            await UploadImage(tempScreenshotPath);
                        }
                    }, _hotKeys);

                    _overlayWindow.IsVisibleChanged += (_, args) =>
                    {
                        if (_overlayWindow?.IsVisible == false)
                        {
                            OverlayWindowClosed?.Invoke(this, EventArgs.Empty);
                            ToastManager.ShowAllToasts();
                        }
                    };

                    _overlayWindow.Closed += (_, _) =>
                    {
                        _overlayWindow = null;
                    };
                }
                else
                {
                    _overlayWindow.UpdateWindow(screenshot);
                }

            _overlayWindow.Show();
                OverlayWindowOpened?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _log.Error("Failed to capture the screen and to open the overlay: {Ex}", ex);
                new Toast.Toast().SetFail(ex.ToString());
            }
        }

        private async Task UploadImage(string path)
        {
            _log.Information("Uploading screen capture...");

            Toast.Toast toast = new();
            toast.SetWait("Uploading screen capture...");
            try
            {
                Bitmap image = new(path);

                string url = await _apiService.UploadCaptureAsync(path, UserSettingsManager.Current.UploadSettings.CompressionQuality, UserSettingsManager.Current.UploadSettings.CompressionLevel);
                _log.Information("Screen capture uploaded to: {Url}", url);
                ClipboardUtils.SetClipboard(url);
                //toast.SetSuccess("Screen capture uploaded and copied to clipboard");
                toast.Close();
                ResultPopUpWindow.Show(image);
                _audioUtils.PlayAudioClip(AudioClip.Complete);
                _log.Information("Upload completed.");
            }
            catch (Exception ex)
            {
                toast.SetFail(ex.Message);
                _log.Error("Failed to upload screen capture. {Error}", ex.Message);
            }
        }
    }
}
