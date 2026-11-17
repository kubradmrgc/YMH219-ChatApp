namespace ChatApp.Core;
public static class UserNameRules
{
    public const int MinLength = 3;
    public const int MaxLength = 20;
    public const int MaxMessageLength = 500;
    public const string Everyone = "Herkes";

    private static readonly string[] Reserved = { Everyone, "Sunucu", "Server" };

    public static bool IsValid(string? userName, out string error)
    {
        var name = userName?.Trim() ?? string.Empty;

        if (name.Length < MinLength || name.Length > MaxLength)
        {
            error = $"Kullanıcı adı {MinLength}-{MaxLength} karakter olmalıdır.";
            return false;
        }

        if (!name.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
        {
            error = "Kullanıcı adında yalnızca harf, rakam, '_' ve '-' kullanılabilir.";
            return false;
        }

        if (Reserved.Any(r => string.Equals(r, name, StringComparison.OrdinalIgnoreCase)))
        {
            error = $"\"{name}\" kullanıcı adı ayrılmıştır.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool IsValidMessage(string? text, out string error)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Boş mesaj gönderilemez.";
            return false;
        }

        if (text.Length > MaxMessageLength)
        {
            error = $"Mesaj en fazla {MaxMessageLength} karakter olabilir.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
