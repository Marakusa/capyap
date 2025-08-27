using CapYap.API.Models;

namespace CapYap.Models
{
    public class GalleryChangedEventArgs : EventArgs
    {
        public Gallery? Gallery { get; }
        public bool Error { get; }
        public string? ErrorMessage { get; }

        public GalleryChangedEventArgs(Gallery? gallery, bool error = false, string? errorMessage = null)
        {
            Gallery = gallery;
            Error = error;
            ErrorMessage = errorMessage;
        }
    }
}
