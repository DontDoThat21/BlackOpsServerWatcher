using System.Text;
using System.Text.RegularExpressions;
using ServerQuery.Core.Models;

namespace ServerQuery.Core.Parsing;

public partial class ServerResponseParser
{
    private static readonly byte[] ExpectedHeader = [0xFF, 0xFF, 0xFF, 0xFF];

    public QueryResult Parse(byte[] data, string ip, int port)
    {
        var server = new ServerInfo { IP = ip, Port = port };
        var result = new QueryResult { Server = server };

        try
        {
            if (data.Length < 5 || !data[..4].SequenceEqual(ExpectedHeader))
                return Fail(result, "Invalid response header");

            // Latin-1 preserves byte values 0x00–0xFF without loss
            var text = Encoding.Latin1.GetString(data, 4, data.Length - 4);
            result.RawResponse = text;

            int firstNewline = text.IndexOf('\n');
            if (firstNewline < 0)
                return Fail(result, "Malformed response: no newline");

            var command = text[..firstNewline].TrimEnd('\r');
            if (!command.Equals("statusResponse", StringComparison.OrdinalIgnoreCase))
                return Fail(result, $"Unexpected command: {command}");

            var lines = text[(firstNewline + 1)..].Split('\n');
            if (lines.Length == 0 || string.IsNullOrEmpty(lines[0]))
                return Fail(result, "Missing info string");

            var dvars = ParseInfoString(lines[0].TrimEnd('\r'));
            ApplyDvars(server, dvars);

            var players = new List<PlayerInfo>();
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(line)) continue;
                var player = ParsePlayerLine(line);
                if (player != null) players.Add(player);
            }

            server.PlayerCount = players.Count;
            server.Status = ServerStatus.Online;
            result.Players = players;
            result.Success = true;
        }
        catch (Exception ex)
        {
            return Fail(result, ex.Message);
        }

        return result;
    }

    private static void ApplyDvars(ServerInfo server, Dictionary<string, string> dvars)
    {
        if (dvars.TryGetValue("mapname", out var map)) server.Map = map;
        if (dvars.TryGetValue("g_gametype", out var gt)) server.GameType = gt;
        if (dvars.TryGetValue("sv_maxclients", out var mc) && int.TryParse(mc, out var maxClients))
            server.MaxPlayers = maxClients;

        if (dvars.TryGetValue("round", out var rnd) && int.TryParse(rnd, out var round))
            server.Round = round;
        else if (dvars.TryGetValue("roundNumber", out var rnd2) && int.TryParse(rnd2, out var round2))
            server.Round = round2;
    }

    private static Dictionary<string, string> ParseInfoString(string infoString)
    {
        var dvars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(infoString) || infoString[0] != '\\')
            return dvars;

        var parts = infoString.Split('\\');
        // parts[0] is empty (before leading \); then key/value pairs
        for (int i = 1; i + 1 < parts.Length; i += 2)
            dvars[parts[i]] = parts[i + 1];

        return dvars;
    }

    private static PlayerInfo? ParsePlayerLine(string line)
    {
        var match = PlayerLinePattern().Match(line);
        if (!match.Success) return null;

        return new PlayerInfo
        {
            Score = int.Parse(match.Groups[1].Value),
            Ping = int.Parse(match.Groups[2].Value),
            Name = match.Groups[3].Value
        };
    }

    // Format: score ping "name"  (score may be negative)
    [GeneratedRegex(@"^(-?\d+)\s+(\d+)\s+""(.*?)""")]
    private static partial Regex PlayerLinePattern();

    private static QueryResult Fail(QueryResult result, string message)
    {
        result.Success = false;
        result.ErrorMessage = message;
        if (result.Server != null) result.Server.Status = ServerStatus.Offline;
        return result;
    }
}
