namespace ChatApp.Core;

/// <summary>
/// Sunucuya bağlı bir istemcinin temel bilgileri.
/// </summary>
public sealed record ClientInfo(string UserName, string Address, DateTime ConnectedAt);
