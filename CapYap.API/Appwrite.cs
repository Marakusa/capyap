using Appwrite;
using Appwrite.Services;

namespace CapYap.API
{
    internal class Appwrite
    {
        public readonly Account account;
        public readonly Client client;

        public Appwrite()
        {
            client = new Client()
                .SetEndpoint("https://aw.marakusa.me/v1")
                .SetProject("67e6390300179aa2b086");

            account = new Account(client);
        }
    }
}
