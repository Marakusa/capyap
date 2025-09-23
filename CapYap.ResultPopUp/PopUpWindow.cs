using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CapYap.ResultPopUp
{
    internal class PopUpWindow : Window
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        private readonly int _maxWidth = 400;
        private readonly int _maxHeight = 300;

        private System.Timers.Timer _timer;

        public PopUpWindow(Bitmap bitmap)
        {
            int windowWidth = bitmap.Width;
            int windowHeight = bitmap.Height;

            // Contain within max dimensions keeping aspect ratio
            if (windowWidth > _maxWidth)
            {
                float ratio = (float)_maxWidth / windowWidth;
                windowWidth = _maxWidth;
                windowHeight = (int)(windowHeight * ratio);
            }
            if (windowHeight > _maxHeight)
            {
                float ratio = (float)_maxHeight / windowHeight;
                windowHeight = _maxHeight;
                windowWidth = (int)(windowWidth * ratio);
            }

            var workingArea = SystemParameters.WorkArea;

            Width = windowWidth;
            Height = windowHeight;
            Left = workingArea.Right - windowWidth;
            Top = workingArea.Bottom - windowHeight;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.None;
            Cursor = Cursors.Hand;
            AllowsTransparency = true;
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200));
            Topmost = true;
            ShowInTaskbar = false;
            
            MouseDown += OnClickWindow;
            
            Grid grid = new Grid();
            Content = grid;
            System.Windows.Controls.Image image = new();

            IntPtr hBitmap = bitmap.GetHbitmap();
            try
            {
                image.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }

            image.Width = windowWidth;
            image.Height = windowHeight;
            image.Stretch = Stretch.UniformToFill;
            image.Margin = new Thickness(1);
            grid.Children.Add(image);

            _timer = new(5000);
            _timer.Elapsed += (_, _) =>
            {
                if (_timer.Enabled)
                {
                    Application.Current.Dispatcher.Invoke(Close);
                }
            };
            _timer.Start();
        }

        private void OnClickWindow(object sender, MouseButtonEventArgs e)
        {
            Close();
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}
