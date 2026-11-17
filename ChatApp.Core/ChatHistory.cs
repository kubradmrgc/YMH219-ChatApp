using System.Text;

namespace ChatApp.Core;
public sealed class ChatHistory
{
    private readonly List<string> lines = new();

    public int Count => lines.Count;

    public IReadOnlyList<string> Lines => lines;

    public void Add(DateTime time, string text)
    {
        lines.Add($"[{time:yyyy-MM-dd HH:mm:ss}] {text}");
    }

    public void Clear() => lines.Clear();
    public void Save(string path, string? userName, string? server)
    {
        using var writer = new StreamWriter(path, append: false, Encoding.UTF8);
        writer.WriteLine("YMH219 Chat Uygulaması - Mesaj Geçmişi");
        writer.WriteLine($"Kullanıcı : {userName ?? "-"}");
        writer.WriteLine($"Sunucu    : {server ?? "-"}");
        writer.WriteLine($"Kayıt     : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        writer.WriteLine($"Satır     : {lines.Count}");
        writer.WriteLine(new string('-', 60));
        foreach (string line in lines)
        {
            writer.WriteLine(line);
        }
    }

    public static string SuggestFileName(string? userName, DateTime now)
    {
        string name = string.IsNullOrWhiteSpace(userName) ? "sohbet" : $"sohbet_{userName}";
        return $"{name}_{now:yyyyMMdd_HHmmss}.txt";
    }
}
