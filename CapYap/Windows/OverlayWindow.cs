using CapYap.HotKeys.Windows;
using CapYap.Properties;
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
using System.Windows.Shapes;

namespace CapYap.Windows
{
    public class OverlayWindow : Window
    {
        private readonly ILogger<OverlayWindow> _log;

        //private readonly HotKeyManager _hotKeys;
        private readonly int _zoom = 12;

        private readonly string _tempCapturePath;
        private readonly Bitmap _bitmap;
        private readonly Action<string> _uploadCallback;

        private readonly Canvas _overlayCanvas;
        private readonly System.Windows.Shapes.Rectangle _darkOverlay;
        private readonly System.Windows.Shapes.Rectangle _selectionRectangle;
        private readonly Label _positionLabel;
        private ImageSource? _imageSource;

        private readonly Bounds _fullBounds;

        private bool _isMouseDown;
        private System.Windows.Point _mouseStart;
        private System.Windows.Point _mouseEnd;
        private System.Windows.Point _mousePosition;

        private bool _useMagnifier = false;
        private readonly Canvas? _magnifyingGlass;

        private readonly Grid _toolbar;

        private bool _isCtrlDown;
        private Bounds _monitorBounds = new(0, 0, 0, 0);

        private bool _isShiftDown;
        private Bounds _windowBounds = new(0, 0, 0, 0);
        private readonly ICollection<(string title, Bounds bounds)> _windowsOpen;

        public OverlayWindow(ILogger<OverlayWindow> log, Bitmap screenshot, string tempCapturePath, Action<string> uploadCallback, HotKeyManager hotKeys)
        {
            _log = log;

            try
            {
                _tempCapturePath = tempCapturePath;
                _bitmap = screenshot;
                _uploadCallback = uploadCallback;

                _fullBounds = NativeUtils.GetFullVirtualBounds();

                // Configure window
                ConfigureWindow();
                _overlayCanvas = CreateCanvas();
                Content = _overlayCanvas;

                // Add screenshot and darken overlay
                AddScreenshot();
                _darkOverlay = CreateDarkOverlay();
                _overlayCanvas.Children.Add(_darkOverlay);

                // Selection rectangle
                _selectionRectangle = CreateSelectionRectangle();
                _overlayCanvas.Children.Add(_selectionRectangle);

                // Magnifying glass
                _magnifyingGlass = CreateMagnifyingGlass();
                _magnifyingGlass.Visibility = _useMagnifier ? Visibility.Visible : Visibility.Hidden;
                _overlayCanvas.Children.Add(_magnifyingGlass);

                // Position label
                _positionLabel = CreatePositionLabel();
                _overlayCanvas.Children.Add(_positionLabel);

                // Toolbar
                _toolbar = CreateToolbar();
                _overlayCanvas.Children.Add(_toolbar);

                _windowsOpen = NativeUtils.GetOpenWindowsBounds();

                hotKeys.CtrlChanged += OnCtrlChanged;
                hotKeys.ShiftChanged += OnShiftChanged;
                hotKeys.AltChanged += OnAltChanged;
                hotKeys.EscapeChanged += OnEscapeChanged;
            }
            catch (Exception ex)
            {
                _log.LogError("Failed to initialize screen capture overlay: {Exception}", ex);
                throw new Exception($"Failed to initialize screen capture overlay: {ex.Message}", ex);
            }
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

            Left = _fullBounds.Left;
            Top = _fullBounds.Top;
            Width = _fullBounds.Right - _fullBounds.Left;
            Height = _fullBounds.Bottom - _fullBounds.Top;
        }

        private Canvas CreateCanvas()
        {
            var canvas = new Canvas();
            RenderOptions.SetEdgeMode(canvas, EdgeMode.Aliased);
            return canvas;
        }

        private Label CreatePositionLabel()
        {
            var label = new Label();
            label = new Label()
            {
                Content = "0, 0",
                Foreground = System.Windows.Media.Brushes.White,
                Background = System.Windows.Media.Brushes.Black,
                Opacity = 0.7,
                Padding = new Thickness(4),
                FontSize = 12,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Top,
                IsHitTestVisible = false,
                Focusable = false
            };
            return label;
        }

        private void AddScreenshot()
        {
            _imageSource = BitmapUtils.BitmapToImageSource(_bitmap);
            _imageSource.Freeze();

            var screenshot = new System.Windows.Controls.Image
            {
                Source = _imageSource,
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

        private Canvas CreateMagnifyingGlass()
        {
            double size = 120;

            // Container for the whole magnifier
            var magnifier = new Canvas
            {
                Width = size,
                Height = size,
                IsHitTestVisible = false
            };

            // Zoomed image
            var zoomedImage = new System.Windows.Controls.Image
            {
                Source = _imageSource,
                RenderTransform = new ScaleTransform(_zoom, _zoom),
                Stretch = Stretch.None
            };

            RenderOptions.SetBitmapScalingMode(zoomedImage, BitmapScalingMode.NearestNeighbor);

            // Clip the zoomed image with ellipse
            var clip = new EllipseGeometry(new Rect(0, 0, size, size));
            magnifier.Clip = clip;
            magnifier.Children.Add(zoomedImage);

            // White border ellipse
            var border = new Ellipse
            {
                Width = size,
                Height = size,
                Stroke = System.Windows.Media.Brushes.White,
                StrokeThickness = 2
            };
            magnifier.Children.Add(border);

            // Crosshair lines
            var lineH = new Line
            {
                X1 = 0,
                Y1 = size / 2,
                X2 = size,
                Y2 = size / 2,
                Stroke = System.Windows.Media.Brushes.Red,
                StrokeThickness = 1
            };

            var lineV = new Line
            {
                X1 = size / 2,
                Y1 = 0,
                X2 = size / 2,
                Y2 = size,
                Stroke = System.Windows.Media.Brushes.Red,
                StrokeThickness = 1
            };

            magnifier.Children.Add(lineH);
            magnifier.Children.Add(lineV);

            // Keep a reference so we can move/offset later
            magnifier.Tag = zoomedImage;

            return magnifier;
        }

        private Button CreateToolbarButton(string glyph, Action? action = null)
        {
            var button = new Button
            {
                Content = new TextBlock
                {
                    Text = glyph,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons"),
                    FontSize = 16,
                    Foreground = System.Windows.Media.Brushes.White,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                },
                Padding = new Thickness(0),
                Margin = new Thickness(4),
                Width = 32,
                Height = 32,
            };
            button.Click += (_, _) => action?.Invoke();
            return button;
        }
        private Grid CreateToolbar()
        {
            Grid toolbar = new()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8),
            };

            // Background container with rounded corners and border
            Border background = new()
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 40, 40)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(67, 67, 67)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(4),
            };

            // Button layout (4 buttons horizontally)
            StackPanel buttonPanel = new()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2)
            };

            // Draw button
            //buttonPanel.Children.Add(CreateToolbarButton("\uE932"));

            // Rect select button
            buttonPanel.Children.Add(CreateToolbarButton("\uEF20", () => {
                _isCtrlDown = false;
                _isShiftDown = false;
                UpdateDrawRect();
            }));

            // Monitor select button
            buttonPanel.Children.Add(CreateToolbarButton("\uF7ED", () => {
                _isCtrlDown = true;
                _isShiftDown = false;
                UpdateDrawRect();
            }));

            // Window select button
            buttonPanel.Children.Add(CreateToolbarButton("\uE7C4", () => {
                _isCtrlDown = false;
                _isShiftDown = true;
                UpdateDrawRect();
            }));

            // Put buttons inside the border
            background.Child = buttonPanel;

            // Add to grid
            toolbar.Children.Add(background);

            return toolbar;
        }

        #endregion

        #region Capture Logic

        private void SaveCapture(Rect captureArea)
        {
            try
            {
                ImageFormat format = ImageFormat.Jpeg;

                switch (AppSettings.Default.UploadFormat)
                {
                    default:
                    case 0:
                        format = ImageFormat.Jpeg;
                        break;
                    case 1:
                        format = ImageFormat.Png;
                        break;
                    case 2:
                        format = ImageFormat.Gif;
                        break;
                }

                BitmapUtils.Crop(_bitmap, captureArea).Save(_tempCapturePath, format);
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

            _selectionRectangle.StrokeThickness = 1;
            _selectionRectangle.Width = width;
            _selectionRectangle.Height = height;
            _selectionRectangle.RenderTransform = new TranslateTransform(x, y);

            UpdateMask(x, y, width, height);
        }

        private void UpdateToolbar(Bounds monitorBounds)
        {
            int width = monitorBounds.Right - monitorBounds.Left;
            _toolbar.Margin = new Thickness(
                monitorBounds.Left + (width / 2) - (_toolbar.ActualWidth / 2), 
                monitorBounds.Top + 12, 
                0, 
                0);
        }

        private void UpdateDrawRect()
        {
            _monitorBounds = NativeUtils.GetCurrentMonitorBounds((int)_mousePosition.X, (int)_mousePosition.Y);

            UpdateToolbar(
                new Bounds(
                    _monitorBounds.Left - _fullBounds.Left,
                    _monitorBounds.Top - _fullBounds.Top,
                    _monitorBounds.Right - _fullBounds.Left,
                    _monitorBounds.Bottom - _fullBounds.Top));

            if (!_isMouseDown && _isCtrlDown)
            {
                _mouseStart = new System.Windows.Point(_monitorBounds.Left - _fullBounds.Left, _monitorBounds.Top - _fullBounds.Top);
                _mouseEnd = new System.Windows.Point(_monitorBounds.Right - _fullBounds.Left, _monitorBounds.Bottom - _fullBounds.Top);

                UpdateSelectionRectangle();
            }
            else if (!_isMouseDown && _isShiftDown)
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

                _mouseStart = new System.Windows.Point(_windowBounds.Left - _fullBounds.Left, _windowBounds.Top - _fullBounds.Top);
                _mouseEnd = new System.Windows.Point(_windowBounds.Right - _fullBounds.Left, _windowBounds.Bottom - _fullBounds.Top);

                UpdateSelectionRectangle();
            }
            else if (_isMouseDown)
            {
                _mouseEnd = new System.Windows.Point(_mousePosition.X - _fullBounds.Left, _mousePosition.Y - _fullBounds.Top);

                UpdateSelectionRectangle();
            }
            else
            {
                _mouseStart = new();
                _mouseEnd = new();

                UpdateSelectionRectangle();
            }
        }

        private void UpdateMagnifier()
        {
            if (_useMagnifier && _magnifyingGlass?.Tag is System.Windows.Controls.Image zoomedImage)
            {
                // Move the magnifying glass
                Canvas.SetLeft(_magnifyingGlass, _mousePosition.X - _fullBounds.Left - _magnifyingGlass.Width / 2);
                Canvas.SetTop(_magnifyingGlass, _mousePosition.Y - _fullBounds.Top - _magnifyingGlass.Height / 2);

                // Adjust image offset so the zoom focuses around mouse
                Canvas.SetLeft(zoomedImage, (-_mousePosition.X + _fullBounds.Left) * _zoom + _magnifyingGlass.Width / 2);
                Canvas.SetTop(zoomedImage, (-_mousePosition.Y + _fullBounds.Top) * _zoom + _magnifyingGlass.Height / 2);
            }

            if (_magnifyingGlass != null)
            {
                _magnifyingGlass.Visibility = _useMagnifier ? Visibility.Visible : Visibility.Hidden;
            }
        }

        #endregion

        #region Event Overrides

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
#if DEBUG
            Topmost = false;
#else
            Topmost = true;
#endif
            Focusable = true;
            Activate();
            Focus();
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
#if DEBUG
            Topmost = false;
#else
            Topmost = true;
#endif
            Focusable = true;
            Activate();
            Focus();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
#if DEBUG
            Topmost = false;
#else
            Topmost = true;
#endif
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

        private void OnAltChanged(bool down)
        {
            Dispatcher.Invoke(() =>
            {
                _useMagnifier = down;
                UpdateMagnifier();
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
            _mouseStart = new System.Windows.Point(_mousePosition.X - _fullBounds.Left, _mousePosition.Y - _fullBounds.Top);
            _mouseEnd = _mousePosition;

            UpdateDrawRect();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            _mousePosition = e.GetPosition(this);

            // Set offset
            _mousePosition.X += _fullBounds.Left;
            _mousePosition.Y += _fullBounds.Top;

            _positionLabel.Content = $"{(int)_mousePosition.X}, {(int)_mousePosition.Y}";
            _positionLabel.RenderTransform = new TranslateTransform(_mousePosition.X - _fullBounds.Left + 10, _mousePosition.Y - _fullBounds.Top + 10);

            UpdateDrawRect();

            // Update zoomed image
            UpdateMagnifier();
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

            if (width < 1 || height < 1)
            {
                // No selection, close overlay
                Close();
                return;
            }

            var captureArea = new Rect((int)x, (int)y, (int)width, (int)height);
            SaveCapture(captureArea);
            Close();
        }

        #endregion
    }
}
