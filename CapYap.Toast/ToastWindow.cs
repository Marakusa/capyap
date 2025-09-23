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

        private readonly Border _textContainer;
        private readonly TextBlock _statusTextBlock;
        private readonly Image _loader;
        private readonly Border _timeoutBar;

        private DispatcherTimer? _closeTimer;

        private bool _isDone = false;
        private bool _canClose = false;

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

            _loader = new Image
            {
                Width = WindowHeight * 0.6,
                Height = WindowHeight * 0.6,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Source = new BitmapImage(new Uri("pack://application:,,,/Assets/loader.png"))
            };

            // Center transform for rotation
            RotateTransform rotate = new(0, 0.5, 0.5);
            _loader.RenderTransform = rotate;
            _loader.RenderTransformOrigin = new Point(0.5, 0.5);

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
            loaderContainer.Children.Add(_loader);
            grid.Children.Add(loaderContainer);

            _textContainer = new Border
            {
                Width = WindowWidth - WindowHeight,
                Height = WindowHeight,
                Margin = new Thickness(WindowHeight, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            _statusTextBlock = new TextBlock()
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
            _textContainer.Child = _statusTextBlock;
            grid.Children.Add(_textContainer);

            // Timeout progress bar
            _timeoutBar = new Border
            {
                Height = 3,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Background = Brushes.White,
                Width = 0
            };
            grid.Children.Add(_timeoutBar);

            Content = grid;
        }

        public void SetStatus(bool loading, string message, bool error = false)
        {
            _isDone = !loading;

            if (loading)
            {
                _loader.Visibility = Visibility.Visible;
                _textContainer.Width = WindowWidth - WindowHeight;
                _textContainer.Height = WindowHeight;
                _textContainer.HorizontalAlignment = HorizontalAlignment.Left;
                _textContainer.Margin = new Thickness(WindowHeight, 0, 0, 0);
                _textContainer.Padding = new Thickness(0, 0, 0, 0);
            }
            else
            {
                _loader.Visibility = Visibility.Hidden;
                _textContainer.Width = WindowWidth - 50;
                _textContainer.Height = WindowHeight - 20;
                _textContainer.HorizontalAlignment = HorizontalAlignment.Center;
                _textContainer.Margin = new Thickness(0, 0, 0, 0);
                _textContainer.Padding = new Thickness(10, 0, 10, 0);

                Cursor = Cursors.Hand;
            }

            _statusTextBlock.Text = message;
            _statusTextBlock.Foreground = error ? Brushes.IndianRed : Brushes.White;
            _timeoutBar.Background = error ? Brushes.IndianRed : Brushes.White;
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
            if (!_canClose)
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

            _timeoutBar.Width = WindowWidth;

            DoubleAnimation widthAnimation = new()
            {
                From = WindowWidth,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(closeTimeout)
            };
            _timeoutBar.BeginAnimation(WidthProperty, widthAnimation);

            _closeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(closeTimeout)
            };
            _closeTimer.Tick += CloseTimer_Tick;
            _closeTimer.Start();
        }

        private void CloseTimer_Tick(object? sender, EventArgs e)
        {
            _closeTimer?.Stop();
            CloseWindow();
        }

        private void OnClickWindow(object sender, MouseButtonEventArgs e)
        {
            if (!_isDone)
            {
                return;
            }

            CloseWindow();
        }

        internal void CloseWindow()
        {
            _canClose = true;

            _loader.RenderTransform.BeginAnimation(RotateTransform.AngleProperty, null);

            if (_closeTimer != null)
            {
                _closeTimer.Tick -= CloseTimer_Tick;
                _closeTimer = null;
            }

            Close();
        }
    }
}
