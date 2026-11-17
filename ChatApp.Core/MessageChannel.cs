using System.Net.Sockets;
using System.Text;

namespace ChatApp.Core;
public sealed class MessageChannel : IDisposable
{
    public const int MaxLineLength = 8 * 1024;

    private static readonly UTF8Encoding Utf8NoBom = new(false);

    private readonly TcpClient client;
    private readonly StreamReader reader;
    private readonly StreamWriter writer;
    private readonly SemaphoreSlim writeLock = new(1, 1);
    private int disposed;

    public MessageChannel(TcpClient client)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        NetworkStream stream = client.GetStream();
        reader = new StreamReader(stream, Utf8NoBom, false, 4096, leaveOpen: true);
        writer = new StreamWriter(stream, Utf8NoBom, 4096, leaveOpen: true) { AutoFlush = false, NewLine = "\n" };
    }
    public string RemoteAddress => client.Client.RemoteEndPoint?.ToString() ?? "?";
    public async Task SendAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        string line = MessageSerializer.Serialize(message);
        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }
    public async Task<ChatMessage?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        if (line is null)
        {
            return null;
        }

        if (line.Length > MaxLineLength)
        {
            throw new FormatException("Mesaj satırı çok uzun.");
        }

        return MessageSerializer.Deserialize(line);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 1)
        {
            return;
        }

        // Önce soket kapatılır; böylece bekleyen okuma/yazma işlemleri hata ile sonlanır.
        // writeLock bilerek Dispose edilmez: Dispose edilen SemaphoreSlim bekleyen görevleri serbest bırakmaz.
        client.Dispose();
        reader.Dispose();
        try
        {
            writer.Dispose();
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
        {
            // Bağlantı kapalı olduğundan tampondaki veri yazılamaz; kapatmaya devam edilir.
        }
    }
}
