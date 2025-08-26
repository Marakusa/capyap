namespace CapYap.API.Models.Exceptions
{
    public class AppwriteException : Exception
    {
        public override string Message { get; }
        public int? Code { get; }
        public string? Type { get; }
        public string? Version { get; }

        public AppwriteException(string? message, int? code = null, string? type = null, string? version = null)
        {
            Message = message ?? "Appwrite responded with an error";
            Code = code;
            Type = type;
            Version = version;
        }
    }
}
