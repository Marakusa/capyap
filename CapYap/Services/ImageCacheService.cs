using CapYap.Interfaces;
using System.Windows.Media.Imaging;

namespace CapYap.Services
{
    public class ImageCacheService : IImageCacheService
    {
        private readonly Dictionary<string, BitmapImage> _cache = new();

        public BitmapImage? GetImage(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            if (_cache.TryGetValue(url, out var cached))
                return cached;

            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(url, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.CreateOptions = BitmapCreateOptions.None;
                bmp.EndInit();

                if (bmp.CanFreeze)
                    bmp.Freeze();

                _cache[url] = bmp;
                return bmp;
            }
            catch (Exception ex)
            {
                new Toast.Toast().SetFail(ex.Message);
                return null;
            }
        }
    }
}
