using CapYap.Interfaces;
using System.IO;
using System.Net.Http;
using System.Windows.Media.Imaging;

namespace CapYap.Services
{
    public class ImageCacheService : IImageCacheService
    {
        private readonly Dictionary<string, BitmapImage> _cache = new();
        private readonly Dictionary<string, BitmapImage> _gifCache = new();

        private readonly HttpClient _client;

        public ImageCacheService(HttpClient client)
        {
            _client = client;
        }

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

                if (new Uri(url, UriKind.Absolute).AbsolutePath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                    return null; // do not cache GIFs

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

        public async Task<BitmapImage?> GetGifImageAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            if (_gifCache.TryGetValue(url, out var cached)) return cached;

            try
            {
                var bytes = await _client.GetByteArrayAsync(url);
                var ms = new MemoryStream(bytes); // keep alive by not disposing

                var gif = new BitmapImage();
                gif.BeginInit();
                gif.StreamSource = ms;
                gif.CacheOption = BitmapCacheOption.OnLoad;
                gif.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                gif.EndInit();
                gif.Freeze();

                _gifCache[url] = gif;
                return gif;
            }
            catch
            {
                return null;
            }
        }
    }
}
