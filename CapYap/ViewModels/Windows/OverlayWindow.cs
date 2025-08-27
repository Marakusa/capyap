using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using CapYap.ScreenCapture.Models;
using CapYap.ScreenCapture.Helpers;

namespace CapYap.ViewModels.Windows
{
    public class OverlayWindow : Window
    {
        public OverlayWindow(Bitmap screenshot)
        {
            // Make the window borderless, transparent, topmost
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = System.Windows.Media.Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;

            // Get full virtual screen bounds from your helper
            Bounds virtualBounds = NativeUtils.GetFullVirtualBounds();

            Left = virtualBounds.Left;
            Top = virtualBounds.Top;
            Width = virtualBounds.Right - virtualBounds.Left;
            Height = virtualBounds.Bottom - virtualBounds.Top;

            // Convert Bitmap to WPF ImageSource
            var imageSource = BitmapToImageSource(screenshot);

            // Display screenshot in an Image control
            var imageControl = new System.Windows.Controls.Image
            {
                Source = imageSource,
                Stretch = Stretch.Fill
            };

            Content = imageControl;
        }

        private ImageSource BitmapToImageSource(Bitmap bmp)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // Freeze for cross-thread usage
                return bitmapImage;
            }
        }
    }
}
