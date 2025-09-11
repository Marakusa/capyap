using System.Windows.Media.Imaging;

namespace CapYap.Interfaces
{
    public interface IImageCacheService
    {
        public BitmapImage? GetImage(string url);

        public Task<BitmapImage?> GetGifImageAsync(string url);
    }
}
