using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatApp.Core;
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
