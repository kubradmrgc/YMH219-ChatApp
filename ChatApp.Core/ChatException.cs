namespace ChatApp.Core;
public sealed class ChatException : Exception
{
    public ChatException(string message) : base(message)
    {
    }

    public ChatException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
