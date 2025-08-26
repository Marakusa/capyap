namespace CapYap.API.Models.Events
{
    public class OnAuthorizedEventArgs : EventArgs
    {
        public string UserId { get; }
        public string Secret { get; }

        public OnAuthorizedEventArgs(string userId, string secret)
        {
            UserId = userId;
            Secret = secret;
        }
    }
}
