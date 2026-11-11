namespace ChatApp.Core;

/// <summary>
/// Kullanıcıya gösterilebilecek bağlantı veya sohbet hatası.
/// </summary>
public sealed class ChatException : Exception
{
    public ChatException(string message) : base(message)
    {
    }

    public ChatException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
