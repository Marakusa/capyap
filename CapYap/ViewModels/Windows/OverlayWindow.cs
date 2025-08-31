using CapYap.HotKeys;
using CapYap.Utils;
using CapYap.Utils.Models;
using CapYap.Utils.Windows;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CapYap.ViewModels.Windows
{
    public class OverlayWindow : Window
    {
        private readonly ILogger<OverlayWindow> _log;

        //private readonly HotKeyManager _hotKeys;

        private readonly string _tempCapturePath;
        private readonly Bitmap _bitmap;
        private readonly Action<string> _uploadCallback;

        private readonly Canvas _overlayCanvas;
        private readonly System.Windows.Shapes.Rectangle _darkOverlay;
        private readonly System.Windows.Shapes.Rectangle _selectionRectangle;

        private bool _isMouseDown;
        private System.Windows.Point _mouseStart;
        private System.Windows.Point _mouseEnd;
        private System.Windows.Point _mousePosition;

        private bool _isCtrlDown;
        private Bounds _monitorBounds = new(0, 0, 0, 0);

        private bool _isShiftDown;
        private Bounds _windowBounds = new(0, 0, 0, 0);
        private readonly ICollection<(string title, Bounds bounds)> _windowsOpen;

        public OverlayWindow(ILogger<OverlayWindow> log, Bitmap screenshot, string tempCapturePath, Action<string> uploadCallback, HotKeyManager hotKeys)
        {
            _log = log;

            _tempCapturePath = tempCapturePath;
            _bitmap = screenshot;
            _uploadCallback = uploadCallback;

            ConfigureWindow();
            _overlayCanvas = CreateCanvas();
            Content = _overlayCanvas;

            AddScreenshot();
            _darkOverlay = CreateDarkOverlay();
            _overlayCanvas.Children.Add(_darkOverlay);

            _selectionRectangle = CreateSelectionRectangle();
            _overlayCanvas.Children.Add(_selectionRectangle);

            _windowsOpen = NativeUtils.GetOpenWindowsBounds();

            hotKeys.CtrlChanged += OnCtrlChanged;
            hotKeys.ShiftChanged += OnShiftChanged;
            hotKeys.EscapeChanged += OnEscapeChanged;
        }

        #region Window & Canvas Setup

        private void ConfigureWindow()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = false; // Keep GPU acceleration
            Background = System.Windows.Media.Brushes.Black;
            Topmost = true;
            Focusable = true;
            ShowInTaskbar = true;
            Cursor = Cursors.Cross;
            ResizeMode = ResizeMode.NoResize;
            Title = "CapYap Overlay";

            Bounds virtualBounds = NativeUtils.GetFullVirtualBounds();
            Left = virtualBounds.Left;
            Top = virtualBounds.Top;
            Width = virtualBounds.Right - virtualBounds.Left;
            Height = virtualBounds.Bottom - virtualBounds.Top;
        }

        private Canvas CreateCanvas()
        {
            var canvas = new Canvas();
            RenderOptions.SetEdgeMode(canvas, EdgeMode.Aliased);
            return canvas;
        }

        private void AddScreenshot()
        {
            var imageSource = BitmapUtils.BitmapToImageSource(_bitmap);
            imageSource.Freeze();

            var screenshot = new System.Windows.Controls.Image
            {
                Source = imageSource,
                Stretch = Stretch.None,
                Width = Width,
                Height = Height
            };

            RenderOptions.SetBitmapScalingMode(screenshot, BitmapScalingMode.LowQuality);
            _overlayCanvas.Children.Add(screenshot);
        }

        private System.Windows.Shapes.Rectangle CreateDarkOverlay()
        {
            var brush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(120, 0, 0, 0));
            brush.Freeze();

            var overlay = new System.Windows.Shapes.Rectangle
            {
                Width = Width,
                Height = Height,
                Fill = brush,
                CacheMode = new BitmapCache()
            };
            return overlay;
        }

        private System.Windows.Shapes.Rectangle CreateSelectionRectangle()
        {
            return new System.Windows.Shapes.Rectangle
            {
                Width = 0,
                Height = 0,
                Stroke = System.Windows.Media.Brushes.White,
                StrokeDashArray = new DoubleCollection { 4, 4 },
                StrokeDashCap = PenLineCap.Round,
                StrokeDashOffset = 4,
                StrokeThickness = 0
            };
        }

        #endregion

        #region Capture Logic

        private void SaveCapture(Rect captureArea)
        {
            try
            {
                BitmapUtils.Crop(_bitmap, captureArea).Save(_tempCapturePath, ImageFormat.Jpeg);
                _uploadCallback.Invoke(_tempCapturePath);
            }
            catch (Exception ex)
            {
                _log.LogError(ex.Message);
                new Toast.Toast().SetFail(ex.Message);
            }
        }

        #endregion

        #region Overlay Update

        private void UpdateMask(double x, double y, double width, double height)
        {
            var mask = new DrawingBrush
            {
                Drawing = new GeometryDrawing(
                    System.Windows.Media.Brushes.White,
                    null,
                    new GeometryGroup
                    {
                        Children =
                        {
                            new RectangleGeometry(new Rect(0, 0, Width, Height)),
                            new RectangleGeometry(new Rect(x, y, width, height))
                        }
                    }),
                Opacity = 1,
                Stretch = Stretch.None
            };

            _darkOverlay.OpacityMask = mask;
        }

        private void UpdateSelectionRectangle()
        {
            double x = Math.Min(_mouseStart.X, _mouseEnd.X);
            double y = Math.Min(_mouseStart.Y, _mouseEnd.Y);
            double width = Math.Abs(_mouseEnd.X - _mouseStart.X);
            double height = Math.Abs(_mouseEnd.Y - _mouseStart.Y);

            _selectionRectangle.StrokeThickness = 2;
            _selectionRectangle.Width = width;
            _selectionRectangle.Height = height;
            _selectionRectangle.RenderTransform = new TranslateTransform(x, y);

            UpdateMask(x, y, width, height);
        }

        private void UpdateDrawRect()
        {
            if (_isCtrlDown)
            {
                _monitorBounds = NativeUtils.GetCurrentMonitorBounds((int)_mousePosition.X, (int)_mousePosition.Y);

                _mouseStart = new System.Windows.Point(_monitorBounds.Left, _monitorBounds.Top);
                _mouseEnd = new System.Windows.Point(_monitorBounds.Right, _monitorBounds.Bottom);

                UpdateSelectionRectangle();
            }
            else if (_isShiftDown)
            {
                // Default to full monitor bounds
                _windowBounds = NativeUtils.GetCurrentMonitorBounds((int)_mousePosition.X, (int)_mousePosition.Y);

                foreach (var window in _windowsOpen)
                {
                    if (window.bounds.Left < _mousePosition.X && window.bounds.Right > _mousePosition.X &&
                        window.bounds.Bottom > _mousePosition.Y && window.bounds.Top < _mousePosition.Y)
                    {
                        _windowBounds = window.bounds;
                        break;
                    }
                }

                _mouseStart = new System.Windows.Point(_windowBounds.Left, _windowBounds.Top);
                _mouseEnd = new System.Windows.Point(_windowBounds.Right, _windowBounds.Bottom);

                UpdateSelectionRectangle();
            }
            else if (_isMouseDown)
            {
                _mouseEnd = _mousePosition;

                UpdateSelectionRectangle();
            }
            else
            {
                _mouseStart = new();
                _mouseEnd = new();

                UpdateSelectionRectangle();
            }
        }

        #endregion

        #region Event Overrides

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            Topmost = true;
            Focusable = true;
            Activate();
            Focus();
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            Topmost = true;
            Focusable = true;
            Activate();
            Focus();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            Topmost = true;
            Focusable = true;
            Activate();
            Focus();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
        }

        private void OnCtrlChanged(bool down)
        {
            Dispatcher.Invoke(() =>
            {
                _isCtrlDown = down;
                _isShiftDown = false;
                UpdateDrawRect();
            });
        }

        private void OnShiftChanged(bool down)
        {
            Dispatcher.Invoke(() =>
            {
                _isShiftDown = down;
                _isCtrlDown = false;
                UpdateDrawRect();
            });
        }

        private void OnEscapeChanged(bool down)
        {
            if (down)
            {
                Dispatcher.Invoke(() =>
                {
                    Close();
                });
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (_isCtrlDown || _isShiftDown)
                return;

            _isMouseDown = true;
            _mouseStart = _mousePosition;
            _mouseEnd = _mousePosition;

            UpdateDrawRect();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            _mousePosition = e.GetPosition(this);

            UpdateDrawRect();
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            _isCtrlDown = false;
            _isShiftDown = false;
            _isMouseDown = false;

            double x = Math.Min(_mouseStart.X, _mouseEnd.X);
            double y = Math.Min(_mouseStart.Y, _mouseEnd.Y);
            double width = Math.Abs(_mouseEnd.X - _mouseStart.X);
            double height = Math.Abs(_mouseEnd.Y - _mouseStart.Y);

            var captureArea = new Rect((int)x, (int)y, (int)width, (int)height);
            SaveCapture(captureArea);
            Close();
        }

        #endregion
    }
}
