using System.Drawing.Imaging;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls;
using CapYap.Utils;
using CapYap.Utils.Models;

namespace CapYap.ViewModels.Windows
{
    public class OverlayWindow : Window
    {
        private readonly string _tempCapturePath;
        private readonly Bitmap _bitmap;
        private readonly Action<string> _uploadCallback;

        private readonly Canvas _overlayCanvas;
        private System.Windows.Shapes.Rectangle drawRectangle;
        private System.Windows.Shapes.Path _darkOverlay;
        private RectangleGeometry _selectionRectGeometry;

        public OverlayWindow(Bitmap screenshot, string tempCapturePath, Action<string> uploadCallback)
        {
            _tempCapturePath = tempCapturePath;
            _bitmap = screenshot;
            _uploadCallback = uploadCallback;

            // Make the window borderless, transparent, topmost
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = System.Windows.Media.Brushes.Transparent;
            //Topmost = true;
            ShowInTaskbar = false;
            Cursor = Cursors.Cross;
            ResizeMode = ResizeMode.NoResize;

            // Create a Canvas to draw rectangles
            _overlayCanvas = new Canvas();
            Content = _overlayCanvas;

            // Get full virtual screen bounds from your helper
            Bounds virtualBounds = NativeUtils.GetFullVirtualBounds();

            Left = virtualBounds.Left;
            Top = virtualBounds.Top;
            Width = virtualBounds.Right - virtualBounds.Left;
            Height = virtualBounds.Bottom - virtualBounds.Top;

            // Convert Bitmap to WPF ImageSource
            var imageSource = BitmapUtils.BitmapToImageSource(_bitmap);

            // Display screenshot in an Image control
            var imageControl = new System.Windows.Controls.Image
            {
                Source = imageSource,
                Stretch = Stretch.Fill
            };

            _overlayCanvas.Children.Add(imageControl);

            // Darkened overlay
            _selectionRectGeometry = new RectangleGeometry(new Rect(0, 0, 0, 0));
            _darkOverlay = new System.Windows.Shapes.Path
            {
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(120, 0, 0, 0)), // semi-transparent black
                Data = new RectangleGeometry(new Rect(0, 0, Width, Height))
            };
            _overlayCanvas.Children.Add(_darkOverlay);

            // Draw the default rectangle
            drawRectangle = new System.Windows.Shapes.Rectangle
            {
                Width = 0,
                Height = 0,
                Stroke = System.Windows.Media.Brushes.White,
                StrokeDashArray = new DoubleCollection { 4, 4 },
                StrokeDashCap = PenLineCap.Round,
                StrokeDashOffset = 4,
                StrokeThickness = 0
            };
            _overlayCanvas.Children.Add(drawRectangle);
        }

        private void SaveCapture(Rect captureArea)
        {
            BitmapUtils.Crop(_bitmap, captureArea).Save(_tempCapturePath, ImageFormat.Jpeg);
            _uploadCallback.Invoke(_tempCapturePath);
        }

        #region Mouse events
        private bool _mouseDown = false;
        private System.Windows.Point _mouseStartPoint = new();
        private System.Windows.Point _mouseEndPoint = new();

        private void DrawRectangle()
        {
            double x = Math.Min(_mouseStartPoint.X, _mouseEndPoint.X);
            double y = Math.Min(_mouseStartPoint.Y, _mouseEndPoint.Y);
            double width = Math.Abs(_mouseEndPoint.X - _mouseStartPoint.X);
            double height = Math.Abs(_mouseEndPoint.Y - _mouseStartPoint.Y);

            drawRectangle.StrokeThickness = 2;

            drawRectangle.Width = width;
            drawRectangle.Height = height;

            Canvas.SetLeft(drawRectangle, x);
            Canvas.SetTop(drawRectangle, y);

            // Update dark overlay
            var fullWindow = new RectangleGeometry(new Rect(0, 0, Width, Height));
            _selectionRectGeometry.Rect = new Rect(x, y, width, height);
            var combined = new CombinedGeometry(GeometryCombineMode.Exclude, fullWindow, _selectionRectGeometry);
            _darkOverlay.Data = combined;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            _mouseDown = true;

            _mouseStartPoint = e.GetPosition(this);
            _mouseEndPoint = _mouseStartPoint;

            DrawRectangle();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (!_mouseDown)
            {
                return;
            }

            _mouseEndPoint = e.GetPosition(this);

            DrawRectangle();
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            _mouseDown = false;

            double x = Math.Min(_mouseStartPoint.X, _mouseEndPoint.X);
            double y = Math.Min(_mouseStartPoint.Y, _mouseEndPoint.Y);
            double width = Math.Abs(_mouseEndPoint.X - _mouseStartPoint.X);
            double height = Math.Abs(_mouseEndPoint.Y - _mouseStartPoint.Y);

            Rect captureArea = new Rect((int)x, (int)y, (int)width, (int)height);
            SaveCapture(captureArea);

            Close();
        }
        #endregion
    }
}
