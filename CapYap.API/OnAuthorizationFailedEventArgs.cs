namespace CapYap.API
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
