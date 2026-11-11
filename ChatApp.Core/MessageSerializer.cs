using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatApp.Core;

/// <summary>
/// Mesajları tek satırlık JSON metnine çevirir. Her mesaj ağ akışında bir satır olarak gönderilir.
/// </summary>
public static class MessageSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return JsonSerializer.Serialize(message, Options);
    }

    /// <summary>Bir satırı mesaja çevirir; geçersiz içerikte <see cref="FormatException"/> fırlatır.</summary>
    public static ChatMessage Deserialize(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            throw new FormatException("Boş mesaj satırı.");
        }

        try
        {
            return JsonSerializer.Deserialize<ChatMessage>(line, Options)
                ?? throw new FormatException("Mesaj çözümlenemedi.");
        }
        catch (JsonException ex)
        {
            throw new FormatException("Geçersiz mesaj biçimi.", ex);
        }
    }
}
