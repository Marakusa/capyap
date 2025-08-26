using CapYap.API.Models.Appwrite;

namespace CapYap.API.Models.Events
{
    public class AuthorizedUserEventArgs : EventArgs
    {
        public User Data { get; }

        public AuthorizedUserEventArgs(User user)
        {
            Data = user;
        }
    }
}
