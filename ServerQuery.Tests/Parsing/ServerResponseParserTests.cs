using System.Text;
using ServerQuery.Core.Models;
using ServerQuery.Core.Parsing;
using Xunit;

namespace ServerQuery.Tests.Parsing;

public class ServerResponseParserTests
{
    private readonly ServerResponseParser _parser = new();

    private static byte[] BuildResponse(string infoString, params string[] playerLines)
    {
        var sb = new StringBuilder();
        sb.Append("statusResponse\n");
        sb.Append(infoString);
        sb.Append('\n');
        foreach (var line in playerLines)
        {
            sb.Append(line);
            sb.Append('\n');
        }

        byte[] header = [0xFF, 0xFF, 0xFF, 0xFF];
        byte[] body = Encoding.Latin1.GetBytes(sb.ToString());
        return [.. header, .. body];
    }

    [Fact]
    public void Parse_FullResponse_MapsAllFields()
    {
        var data = BuildResponse(
            @"\mapname\zm_cod5_asylum\g_gametype\zom\sv_maxclients\18\round\5",
            @"100 42 ""Player1""",
            @"200 67 ""Player2"""
        );

        var result = _parser.Parse(data, "192.168.1.1", 28960);

        Assert.True(result.Success);
        Assert.NotNull(result.Server);
        Assert.Equal("zm_cod5_asylum", result.Server.Map);
        Assert.Equal("zom", result.Server.GameType);
        Assert.Equal(18, result.Server.MaxPlayers);
        Assert.Equal(5, result.Server.Round);
        Assert.Equal(2, result.Server.PlayerCount);
        Assert.Equal(2, result.Players.Count);
        Assert.Equal(ServerStatus.Online, result.Server.Status);
    }

    [Fact]
    public void Parse_EmptyServer_ZeroPlayers()
    {
        var data = BuildResponse(@"\mapname\zm_cod5_nacht\g_gametype\zom\sv_maxclients\18");

        var result = _parser.Parse(data, "192.168.1.1", 28960);

        Assert.True(result.Success);
        Assert.Equal(0, result.Server!.PlayerCount);
        Assert.Empty(result.Players);
        Assert.Equal(ServerStatus.Online, result.Server.Status);
    }

    [Fact]
    public void Parse_PlayerFields_MappedCorrectly()
    {
        var data = BuildResponse(
            @"\mapname\zm_cod5_asylum\g_gametype\zom\sv_maxclients\18",
            @"350 55 ""^1RedPlayer^7"""
        );

        var result = _parser.Parse(data, "192.168.1.1", 28960);

        Assert.True(result.Success);
        Assert.Single(result.Players);
        Assert.Equal(350, result.Players[0].Score);
        Assert.Equal(55, result.Players[0].Ping);
        Assert.Equal("^1RedPlayer^7", result.Players[0].Name);
    }

    [Fact]
    public void Parse_NegativeScore_ParsedCorrectly()
    {
        var data = BuildResponse(
            @"\mapname\zm_cod5_asylum\g_gametype\zom\sv_maxclients\18",
            @"-1 999 ""connecting"""
        );

        var result = _parser.Parse(data, "192.168.1.1", 28960);

        Assert.True(result.Success);
        Assert.Single(result.Players);
        Assert.Equal(-1, result.Players[0].Score);
        Assert.Equal(999, result.Players[0].Ping);
    }

    [Fact]
    public void Parse_InvalidHeader_ReturnsFailure()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, (byte)'x' };

        var result = _parser.Parse(data, "192.168.1.1", 28960);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(ServerStatus.Offline, result.Server!.Status);
    }

    [Fact]
    public void Parse_UnexpectedCommand_ReturnsFailure()
    {
        byte[] header = [0xFF, 0xFF, 0xFF, 0xFF];
        byte[] body = Encoding.Latin1.GetBytes("infoResponse\n\\key\\value\n");
        var data = (byte[])[.. header, .. body];

        var result = _parser.Parse(data, "192.168.1.1", 28960);

        Assert.False(result.Success);
        Assert.Equal(ServerStatus.Offline, result.Server!.Status);
    }

    [Fact]
    public void Parse_RoundDvar_Extracted()
    {
        var data = BuildResponse(@"\mapname\zm_cod5_asylum\g_gametype\zom\sv_maxclients\18\round\12");

        var result = _parser.Parse(data, "192.168.1.1", 28960);

        Assert.True(result.Success);
        Assert.Equal(12, result.Server!.Round);
    }

    [Fact]
    public void Parse_NoRoundDvar_RoundIsNull()
    {
        var data = BuildResponse(@"\mapname\zm_cod5_asylum\g_gametype\zom\sv_maxclients\18");

        var result = _parser.Parse(data, "192.168.1.1", 28960);

        Assert.True(result.Success);
        Assert.Null(result.Server!.Round);
    }

    [Fact]
    public void Parse_IpAndPort_StoredOnServer()
    {
        var data = BuildResponse(@"\mapname\zm_cod5_asylum\g_gametype\zom\sv_maxclients\18");

        var result = _parser.Parse(data, "10.0.0.1", 28961);

        Assert.Equal("10.0.0.1", result.Server!.IP);
        Assert.Equal(28961, result.Server.Port);
    }

    [Fact]
    public void Parse_RoundNumberFallback_ExtractsRound()
    {
        var data = BuildResponse(@"\mapname\zm_cod5_asylum\g_gametype\zom\sv_maxclients\18\roundNumber\7");

        var result = _parser.Parse(data, "192.168.1.1", 28960);

        Assert.True(result.Success);
        Assert.Equal(7, result.Server!.Round);
    }
}
