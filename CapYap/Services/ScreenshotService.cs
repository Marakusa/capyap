using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using CapYap.Interfaces;
using CapYap.ScreenCapture;
using CapYap.ViewModels.Windows;

namespace CapYap.Services
{
    public class ScreenshotService : IScreenshotService
    {
        private readonly string screenshotPath;

        public ScreenshotService()
        {
            screenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapYap", "screenshot-temp.png");
        }

        public void CaptureAllScreens()
        {
            var screenshotService = new Screenshot();
            Bitmap screenshot = screenshotService.CaptureAllMonitors();
            screenshot.Save(screenshotPath, ImageFormat.Png);

            var overlayWindow = new OverlayWindow(screenshot);
            overlayWindow.Show();
        }
    }
}
