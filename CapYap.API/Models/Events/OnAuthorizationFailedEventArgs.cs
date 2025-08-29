namespace CapYap.API.Models.Events
{
    public class OnAuthorizationFailedEventArgs : EventArgs
    {
        public string Message { get; }

        public OnAuthorizationFailedEventArgs(string message)
        {
            Message = message;
        }
    }
}
