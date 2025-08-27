namespace CapYap.Models
{
    public class GalleryChangedEventArgs : EventArgs
    {
        public List<string>? Gallery { get; }
        public bool Error { get; }
        public string? ErrorMessage { get; }

        public GalleryChangedEventArgs(List<string>? gallery, bool error = false, string? errorMessage = null)
        {
            Gallery = gallery;
            Error = error;
            ErrorMessage = errorMessage;
        }
    }
}
