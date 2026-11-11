using System.Net;
using System.Net.Sockets;

namespace ChatApp.Core;

/// <summary>
/// TcpListener ile istemci bağlantılarını kabul eden ve mesajları dağıtan sohbet sunucusu.
/// Olaylar arka plan iş parçacıklarından tetiklenir.
/// </summary>
public sealed class ChatServer : IDisposable
{
    /// <summary>Bağlanan istemcinin kullanıcı adını göndermesi için tanınan süre.</summary>
    public static readonly TimeSpan JoinTimeout = TimeSpan.FromSeconds(10);

    private readonly object sync = new();
    private readonly Dictionary<string, Session> sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Task> handlerTasks = new();
    private TcpListener? listener;
    private CancellationTokenSource? cts;
    private Task? acceptTask;

    public event Action<string>? Log;
    public event Action<ClientInfo>? ClientConnected;
    public event Action<ClientInfo>? ClientDisconnected;

    public bool IsRunning => listener is not null;

    public IPEndPoint? LocalEndPoint => listener?.LocalEndpoint as IPEndPoint;

    public IReadOnlyList<ClientInfo> Clients
    {
        get
        {
            lock (sync)
            {
                return sessions.Values.Where(s => s.Ready).Select(s => s.Info).ToList();
            }
        }
    }

    /// <summary>Dinlemeyi başlatır. Port kullanımdaysa <see cref="SocketException"/> fırlatır.</summary>
    public void Start(IPAddress address, int port)
    {
        if (IsRunning)
        {
            throw new InvalidOperationException("Sunucu zaten çalışıyor.");
        }

        var newListener = new TcpListener(address, port);
        newListener.Start();
        listener = newListener;
        cts = new CancellationTokenSource();
        acceptTask = AcceptLoopAsync(newListener, cts.Token);
        OnLog($"Sunucu başlatıldı: {LocalEndPoint}");
    }

    public async Task StopAsync()
    {
        if (listener is null || cts is null)
        {
            return;
        }

        OnLog("Sunucu durduruluyor...");
        await BroadcastAsync(ChatMessage.System("Sunucu kapatıldı.")).ConfigureAwait(false);

        cts.Cancel();
        listener.Stop();

        Session[] open;
        Task[] handlers;
        lock (sync)
        {
            open = sessions.Values.ToArray();
            handlers = handlerTasks.ToArray();
        }

        foreach (var session in open)
        {
            session.Channel.Dispose();
        }

        try
        {
            await Task.WhenAll(handlers.Append(acceptTask ?? Task.CompletedTask)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            OnLog($"Kapatma sırasında hata: {ex.Message}");
        }

        cts.Dispose();
        cts = null;
        listener = null;
        acceptTask = null;
        OnLog("Sunucu durduruldu.");
    }

    private async Task AcceptLoopAsync(TcpListener activeListener, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            TcpClient tcp;
            try
            {
                tcp = await activeListener.AcceptTcpClientAsync(token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or SocketException)
            {
                break;
            }

            var handler = HandleClientAsync(tcp, token);
            lock (sync)
            {
                handlerTasks.RemoveAll(t => t.IsCompleted);
                handlerTasks.Add(handler);
            }
        }
    }

    private async Task HandleClientAsync(TcpClient tcp, CancellationToken token)
    {
        var channel = new MessageChannel(tcp);
        string address = channel.RemoteAddress;
        OnLog($"Yeni bağlantı: {address}");

        Session? session = null;
        try
        {
            session = await JoinAsync(channel, address, token).ConfigureAwait(false);
            if (session is null)
            {
                return;
            }

            while (!token.IsCancellationRequested)
            {
                ChatMessage? message = await channel.ReceiveAsync(token).ConfigureAwait(false);
                if (message is null || message.Type == MessageType.Leave)
                {
                    break;
                }

                await HandleMessageAsync(session, message).ConfigureAwait(false);
            }
        }
        catch (FormatException ex)
        {
            OnLog($"{address}: geçersiz veri ({ex.Message}), bağlantı kapatılıyor.");
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
        {
            // Bağlantı koptu veya sunucu durduruluyor.
        }
        finally
        {
            channel.Dispose();
            if (session is not null)
            {
                await RemoveSessionAsync(session).ConfigureAwait(false);
            }
            else
            {
                OnLog($"Bağlantı kapandı: {address}");
            }
        }
    }

    private async Task<Session?> JoinAsync(MessageChannel channel, string address, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(JoinTimeout);

        ChatMessage? join;
        try
        {
            join = await channel.ReceiveAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            OnLog($"{address}: kullanıcı adı zamanında gönderilmedi.");
            return null;
        }

        if (join is null || join.Type != MessageType.Join)
        {
            OnLog($"{address}: katılım mesajı beklenirken bağlantı kapandı.");
            return null;
        }

        string userName = join.From?.Trim() ?? string.Empty;
        if (!UserNameRules.IsValid(userName, out string error))
        {
            await RejectAsync(channel, address, error, token).ConfigureAwait(false);
            return null;
        }

        var session = new Session(channel, new ClientInfo(userName, address, DateTime.Now));
        lock (sync)
        {
            if (!sessions.TryAdd(userName, session))
            {
                session = null;
            }
        }

        if (session is null)
        {
            await RejectAsync(channel, address, $"\"{userName}\" kullanıcı adı zaten kullanılıyor.", token).ConfigureAwait(false);
            return null;
        }

        try
        {
            await channel.SendAsync(ChatMessage.Accept(userName), token).ConfigureAwait(false);
        }
        catch
        {
            lock (sync)
            {
                sessions.Remove(userName);
            }
            throw;
        }
        session.Ready = true;

        OnLog($"{userName} katıldı ({address}).");
        ClientConnected?.Invoke(session.Info);
        await BroadcastAsync(ChatMessage.UserJoined(userName), except: session).ConfigureAwait(false);
        await BroadcastUserListAsync().ConfigureAwait(false);
        return session;
    }

    private async Task RejectAsync(MessageChannel channel, string address, string reason, CancellationToken token)
    {
        OnLog($"{address}: katılım reddedildi - {reason}");
        await channel.SendAsync(ChatMessage.Reject(reason), token).ConfigureAwait(false);
    }

    private Task HandleMessageAsync(Session session, ChatMessage message)
    {
        OnLog($"{session.Info.UserName}: desteklenmeyen mesaj türü ({message.Type}).");
        return Task.CompletedTask;
    }

    private async Task RemoveSessionAsync(Session session)
    {
        bool removed;
        lock (sync)
        {
            removed = sessions.Remove(session.Info.UserName);
        }

        if (!removed)
        {
            return;
        }

        OnLog($"{session.Info.UserName} ayrıldı.");
        ClientDisconnected?.Invoke(session.Info);
        await BroadcastAsync(ChatMessage.UserLeft(session.Info.UserName)).ConfigureAwait(false);
        await BroadcastUserListAsync().ConfigureAwait(false);
    }

    private Task BroadcastUserListAsync()
    {
        var names = Clients.Select(c => c.UserName).OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase);
        return BroadcastAsync(ChatMessage.UserList(names));
    }

    private async Task BroadcastAsync(ChatMessage message, Session? except = null)
    {
        Session[] targets;
        lock (sync)
        {
            targets = sessions.Values.Where(s => s.Ready && s != except).ToArray();
        }

        await Task.WhenAll(targets.Select(s => TrySendAsync(s, message))).ConfigureAwait(false);
    }

    private async Task<bool> TrySendAsync(Session session, ChatMessage message)
    {
        try
        {
            await session.Channel.SendAsync(message).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
        {
            // Okuma döngüsü kopan bağlantıyı fark edip oturumu kapatacak.
            return false;
        }
    }

    private void OnLog(string text) => Log?.Invoke(text);

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
    }

    private sealed class Session
    {
        public Session(MessageChannel channel, ClientInfo info)
        {
            Channel = channel;
            Info = info;
        }

        public MessageChannel Channel { get; }
        public ClientInfo Info { get; }

        /// <summary>Kabul mesajı gönderildikten sonra true olur; o ana kadar yayın almaz.</summary>
        public volatile bool Ready;
    }
}
