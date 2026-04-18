namespace ServerQuery.Core.Models;

public class ServerInfo
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string IP { get; set; } = string.Empty;
    public int Port { get; set; }
    public ServerStatus Status { get; set; } = ServerStatus.Unknown;
    public int PlayerCount { get; set; }
    public int MaxPlayers { get; set; }
    public int? Round { get; set; }
    public string? Map { get; set; }
    public string? GameType { get; set; }
    public long PingMs { get; set; }
    public DateTime? LastUpdated { get; set; }
}
