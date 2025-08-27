using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace CapYap.Utils
{
    public static class BitmapUtils
    {
        public static Bitmap AdjustBrightnessContrast(Image image, float brightness, float contrast)
        {
            var bitmap = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(bitmap))
            using (var attributes = new ImageAttributes())
            {
                float[][] matrix = [
                    [contrast, 0, 0, 0, 0],
                    [0, contrast, 0, 0, 0],
                    [0, 0, contrast, 0, 0],
                    [0, 0, 0, 1, 0],
                    [brightness, brightness, brightness, 1, 1]
                ];

                ColorMatrix colorMatrix = new ColorMatrix(matrix);
                attributes.SetColorMatrix(colorMatrix);
                g.DrawImage(image, new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, attributes);

                return bitmap;
            }
        }

        public static ImageSource BitmapToImageSource(Bitmap bmp)
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

        public static Bitmap Crop(Bitmap bitmap, Rect cropArea)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            // Check that the crop area is within the bitmap
            int x = (int)cropArea.X;
            int y = (int)cropArea.Y;
            int width = (int)cropArea.Width;
            int height = (int)cropArea.Height;

            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("Invalid crop area.");
            }

            // Create a new bitmap for the cropped area
            Rectangle rect = new Rectangle(x, y, width, height);
            return bitmap.Clone(rect, bitmap.PixelFormat);
        }
    }
}
