namespace ChatApp.Core;

/// <summary>
/// Ağ üzerinden gönderilen tek bir protokol mesajı.
/// </summary>
public sealed class ChatMessage
{
    public MessageType Type { get; set; }

    /// <summary>Gönderen kullanıcı adı (sunucu mesajlarında boş).</summary>
    public string? From { get; set; }

    /// <summary>Özel mesajın alıcısı.</summary>
    public string? To { get; set; }

    public string? Text { get; set; }

    /// <summary>Mesajın zamanı; sohbet mesajlarında sunucu tarafından atanır.</summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>UserList mesajındaki kullanıcı adları.</summary>
    public List<string>? Users { get; set; }

    public static ChatMessage Join(string userName) => new() { Type = MessageType.Join, From = userName };

    public static ChatMessage Accept(string userName) => new() { Type = MessageType.Accept, To = userName };

    public static ChatMessage Reject(string reason) => new() { Type = MessageType.Reject, Text = reason };

    public static ChatMessage Chat(string from, string text) => new() { Type = MessageType.Chat, From = from, Text = text };

    public static ChatMessage Private(string from, string to, string text) =>
        new() { Type = MessageType.Private, From = from, To = to, Text = text };

    public static ChatMessage UserList(IEnumerable<string> users) => new() { Type = MessageType.UserList, Users = users.ToList() };

    public static ChatMessage UserJoined(string userName) => new() { Type = MessageType.UserJoined, From = userName };

    public static ChatMessage UserLeft(string userName) => new() { Type = MessageType.UserLeft, From = userName };

    public static ChatMessage System(string text) => new() { Type = MessageType.System, Text = text };

    public static ChatMessage Leave() => new() { Type = MessageType.Leave };
}
