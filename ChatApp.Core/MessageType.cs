namespace ChatApp.Core;

/// <summary>
/// İstemci ile sunucu arasında gidip gelen mesaj türleri.
/// </summary>
public enum MessageType
{
    /// <summary>İstemci → sunucu: kullanıcı adıyla katılma isteği.</summary>
    Join,
    /// <summary>Sunucu → istemci: katılım kabul edildi.</summary>
    Accept,
    /// <summary>Sunucu → istemci: katılım reddedildi (Text alanında sebep).</summary>
    Reject,
    /// <summary>Herkese gönderilen sohbet mesajı.</summary>
    Chat,
    /// <summary>Tek bir kullanıcıya gönderilen özel mesaj.</summary>
    Private,
    /// <summary>Sunucu → istemci: güncel çevrimiçi kullanıcı listesi.</summary>
    UserList,
    /// <summary>Sunucu → istemci: bir kullanıcı sohbete katıldı.</summary>
    UserJoined,
    /// <summary>Sunucu → istemci: bir kullanıcı sohbetten ayrıldı.</summary>
    UserLeft,
    /// <summary>Sunucu → istemci: bilgi veya hata bildirimi.</summary>
    System,
    /// <summary>İstemci → sunucu: bağlantıyı düzgün şekilde kapatma isteği.</summary>
    Leave
}
