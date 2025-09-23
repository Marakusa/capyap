using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CapYap.Toast
{
    internal class ToastWindow : Window
    {
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int dwAttribute,
            ref int pvAttribute,
            int cbAttribute);

        private int WindowWidth { get; } = 400;
        private int WindowHeight { get; } = 100;

        private readonly Border textContainer;
        private readonly TextBlock statusTextBlock;
        private readonly Image loader;
        private readonly Border timeoutBar;

        private DispatcherTimer? closeTimer;

        private bool isDone = false;
        private bool canClose = false;

        public ToastWindow()
        {
            var workingArea = SystemParameters.WorkArea;

            Width = WindowWidth;
            Height = WindowHeight;
            Left = workingArea.Right - WindowWidth - 10;
            Top = workingArea.Bottom - WindowHeight - 10;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = new SolidColorBrush(Color.FromRgb(5, 7, 10));
            Topmost = true;
            ShowInTaskbar = false;

            // Close on click
            MouseLeftButtonDown += OnClickWindow;

            // Main container
            Grid grid = new();

            loader = new Image
            {
                Width = WindowHeight * 0.6,
                Height = WindowHeight * 0.6,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Source = new BitmapImage(new Uri("pack://application:,,,/Assets/loader.png"))
            };

            // Center transform for rotation
            RotateTransform rotate = new(0, 0.5, 0.5);
            loader.RenderTransform = rotate;
            loader.RenderTransformOrigin = new Point(0.5, 0.5);

            // Infinite spin animation
            DoubleAnimation spinAnimation = new()
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(1),
                RepeatBehavior = RepeatBehavior.Forever
            };
            rotate.BeginAnimation(RotateTransform.AngleProperty, spinAnimation);

            // Loader container
            Grid loaderContainer = new()
            {
                Width = WindowHeight,
                Height = WindowHeight,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            loaderContainer.Children.Add(loader);
            grid.Children.Add(loaderContainer);

            textContainer = new Border
            {
                Width = WindowWidth - WindowHeight,
                Height = WindowHeight,
                Margin = new Thickness(WindowHeight, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            statusTextBlock = new TextBlock()
            {
                Text = "Uploading screen capture...",
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                MaxHeight = WindowHeight
            };
            textContainer.Child = statusTextBlock;
            grid.Children.Add(textContainer);

            // Timeout progress bar
            timeoutBar = new Border
            {
                Height = 3,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Background = Brushes.White,
                Width = 0
            };
            grid.Children.Add(timeoutBar);

            Content = grid;
        }

        public void SetStatus(bool loading, string message, bool error = false)
        {
            isDone = !loading;

            if (loading)
            {
                loader.Visibility = Visibility.Visible;
                textContainer.Width = WindowWidth - WindowHeight;
                textContainer.Height = WindowHeight;
                textContainer.HorizontalAlignment = HorizontalAlignment.Left;
                textContainer.Margin = new Thickness(WindowHeight, 0, 0, 0);
                textContainer.Padding = new Thickness(0, 0, 0, 0);
            }
            else
            {
                loader.Visibility = Visibility.Hidden;
                textContainer.Width = WindowWidth - 50;
                textContainer.Height = WindowHeight - 20;
                textContainer.HorizontalAlignment = HorizontalAlignment.Center;
                textContainer.Margin = new Thickness(0, 0, 0, 0);
                textContainer.Padding = new Thickness(10, 0, 10, 0);

                Cursor = Cursors.Hand;
            }

            statusTextBlock.Text = message;
            statusTextBlock.Foreground = error ? Brushes.IndianRed : Brushes.White;
            timeoutBar.Background = error ? Brushes.IndianRed : Brushes.White;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Get window handle and apply rounded corners
            var hwnd = new WindowInteropHelper(this).Handle;
            int preference = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!canClose)
            {
                e.Cancel = true;
                return;
            }

            base.OnClosing(e);
        }

        internal void SetToastOrder(int order = 0)
        {
            var workingArea = SystemParameters.WorkArea;
            Top = workingArea.Bottom - WindowHeight - 10 - (order * (WindowHeight + 10));
        }

        internal void CloseIn(int closeTimeout)
        {
            Cursor = Cursors.Hand;

            if (closeTimeout <= 0)
            {
                CloseWindow();
                return;
            }

            timeoutBar.Width = WindowWidth;

            DoubleAnimation widthAnimation = new()
            {
                From = WindowWidth,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(closeTimeout)
            };
            timeoutBar.BeginAnimation(WidthProperty, widthAnimation);

            closeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(closeTimeout)
            };
            closeTimer.Tick += CloseTimer_Tick;
            closeTimer.Start();
        }

        private void CloseTimer_Tick(object? sender, EventArgs e)
        {
            closeTimer?.Stop();
            CloseWindow();
        }

        private void OnClickWindow(object sender, MouseButtonEventArgs e)
        {
            if (!isDone)
            {
                return;
            }

            CloseWindow();
        }

        internal void CloseWindow()
        {
            canClose = true;

            loader.RenderTransform.BeginAnimation(RotateTransform.AngleProperty, null);

            if (closeTimer != null)
            {
                closeTimer.Tick -= CloseTimer_Tick;
                closeTimer = null;
            }

            Close();
        }
    }
}
