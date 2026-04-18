namespace ServerQuery.Core.Models;

public class QueryResult
{
    public bool Success { get; set; }
    public ServerInfo? Server { get; set; }
    public IReadOnlyList<PlayerInfo> Players { get; set; } = Array.Empty<PlayerInfo>();
    public string? RawResponse { get; set; }
    public string? ErrorMessage { get; set; }
    public long ElapsedMs { get; set; }
}
