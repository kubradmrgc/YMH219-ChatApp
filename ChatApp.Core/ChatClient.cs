using System.Net.Sockets;

namespace ChatApp.Core;
public sealed class ChatClient : IDisposable
{
    public static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);

    private MessageChannel? channel;
    private CancellationTokenSource? cts;
    private volatile bool leaving;
    public event Action<ChatMessage>? MessageReceived;
    public event Action<string>? Disconnected;

    public bool IsConnected => channel is not null;

    public string? UserName { get; private set; }
    public async Task ConnectAsync(string host, int port, string userName, CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            throw new ChatException("Zaten bağlısınız.");
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ChatException("Sunucu adresi boş olamaz.");
        }

        userName = userName.Trim();
        if (!UserNameRules.IsValid(userName, out string error))
        {
            throw new ChatException(error);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ConnectTimeout);

        var tcp = new TcpClient();
        MessageChannel? newChannel = null;
        try
        {
            await tcp.ConnectAsync(host.Trim(), port, timeout.Token).ConfigureAwait(false);
            newChannel = new MessageChannel(tcp);
            await newChannel.SendAsync(ChatMessage.Join(userName), timeout.Token).ConfigureAwait(false);

            ChatMessage? reply = await newChannel.ReceiveAsync(timeout.Token).ConfigureAwait(false);
            switch (reply?.Type)
            {
                case MessageType.Accept:
                    break;
                case MessageType.Reject:
                    throw new ChatException($"Sunucu katılımı reddetti: {reply.Text}");
                case null:
                    throw new ChatException("Sunucu bağlantıyı kapattı.");
                default:
                    throw new ChatException("Sunucudan beklenmeyen yanıt alındı.");
            }
        }
        catch (Exception ex) when (ex is not ChatException)
        {
            newChannel?.Dispose();
            tcp.Dispose();
            throw new ChatException(DescribeConnectError(ex, host, port, cancellationToken), ex);
        }
        catch
        {
            newChannel?.Dispose();
            tcp.Dispose();
            throw;
        }

        cts?.Dispose();
        cts = new CancellationTokenSource();
        channel = newChannel;
        UserName = userName;
        leaving = false;
        _ = ReadLoopAsync(newChannel, cts.Token);
    }
    public Task SendChatAsync(string text)
    {
        return SendValidatedAsync(text, t => ChatMessage.Chat(UserName!, t));
    }
    public Task SendPrivateAsync(string to, string text)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            throw new ChatException("Özel mesaj için alıcı seçilmelidir.");
        }

        return SendValidatedAsync(text, t => ChatMessage.Private(UserName!, to, t));
    }

    private async Task SendValidatedAsync(string text, Func<string, ChatMessage> create)
    {
        MessageChannel current = channel ?? throw new ChatException("Sunucuya bağlı değilsiniz.");
        if (!UserNameRules.IsValidMessage(text, out string error))
        {
            throw new ChatException(error);
        }

        try
        {
            await current.SendAsync(create(text.Trim())).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
        {
            throw new ChatException("Mesaj gönderilemedi: sunucu ile bağlantı koptu.", ex);
        }
    }
    public async Task DisconnectAsync()
    {
        MessageChannel? current = channel;
        if (current is null)
        {
            return;
        }

        leaving = true;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await current.SendAsync(ChatMessage.Leave(), timeout.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
        {
            // Bağlantı zaten kopmuş olabilir; yine de kapatılır.
        }

        Close("Bağlantı kesildi.");
    }

    private async Task ReadLoopAsync(MessageChannel activeChannel, CancellationToken token)
    {
        string reason = "Sunucu bağlantıyı kapattı.";
        try
        {
            while (!token.IsCancellationRequested)
            {
                ChatMessage? message = await activeChannel.ReceiveAsync(token).ConfigureAwait(false);
                if (message is null)
                {
                    break;
                }

                MessageReceived?.Invoke(message);
            }
        }
        catch (FormatException)
        {
            reason = "Sunucudan geçersiz veri alındı, bağlantı kapatıldı.";
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
        {
            reason = "Sunucu ile bağlantı koptu.";
        }

        Close(leaving ? "Bağlantı kesildi." : reason);
    }

    private void Close(string reason)
    {
        MessageChannel? current = Interlocked.Exchange(ref channel, null);
        if (current is null)
        {
            return;
        }

        cts?.Cancel();
        current.Dispose();
        Disconnected?.Invoke(reason);
    }

    private static string DescribeConnectError(Exception ex, string host, int port, CancellationToken userToken)
    {
        if (ex is OperationCanceledException)
        {
            return userToken.IsCancellationRequested
                ? "Bağlantı iptal edildi."
                : $"{host}:{port} adresine {ConnectTimeout.TotalSeconds:0} saniye içinde bağlanılamadı (zaman aşımı).";
        }

        if (ex is SocketException socketError)
        {
            return socketError.SocketErrorCode switch
            {
                SocketError.ConnectionRefused => $"{host}:{port} adresinde çalışan bir sunucu bulunamadı. Sunucunun başlatıldığından emin olun.",
                SocketError.HostNotFound or SocketError.NoData => $"\"{host}\" adresi bulunamadı.",
                SocketError.TimedOut => $"{host}:{port} adresine bağlanırken zaman aşımı oluştu.",
                SocketError.NetworkUnreachable or SocketError.HostUnreachable => "Ağa veya sunucuya ulaşılamıyor.",
                _ => $"Bağlantı hatası: {socketError.Message}"
            };
        }

        if (ex is FormatException)
        {
            return "Sunucudan geçersiz veri alındı. Bağlandığınız uygulama bu sohbet sunucusu olmayabilir.";
        }

        return $"Bağlantı hatası: {ex.Message}";
    }

    public void Dispose()
    {
        leaving = true;
        Close("Bağlantı kesildi.");
        cts?.Dispose();
    }
}
