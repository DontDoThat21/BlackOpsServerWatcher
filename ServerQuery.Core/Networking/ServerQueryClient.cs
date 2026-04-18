using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace ServerQuery.Core.Networking;

public class ServerQueryClient
{
    private static readonly byte[] QueryPacket = [0xFF, 0xFF, 0xFF, 0xFF, .. "getstatus\n"u8];

    public int TimeoutMs { get; set; } = 3000;
    public int RetryCount { get; set; } = 2;

    public async Task<(byte[]? Data, long ElapsedMs)> QueryAsync(
        string ip, int port, CancellationToken cancellationToken = default)
    {
        var endpoint = new IPEndPoint(IPAddress.Parse(ip), port);
        var sw = Stopwatch.StartNew();

        for (int attempt = 0; attempt <= RetryCount; attempt++)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeoutMs);

            using var client = new UdpClient();
            client.Connect(endpoint);

            try
            {
                await client.SendAsync(QueryPacket, cts.Token);
                var result = await client.ReceiveAsync(cts.Token);
                sw.Stop();
                return (result.Buffer, sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // per-attempt timeout — try next attempt
            }
            catch (SocketException)
            {
                // network error — try next attempt
            }
        }

        sw.Stop();
        return (null, sw.ElapsedMilliseconds);
    }
}
