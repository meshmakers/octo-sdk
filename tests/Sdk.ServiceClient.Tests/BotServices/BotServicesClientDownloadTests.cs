using System.Net;
using System.Net.Sockets;
using System.Text;
using Meshmakers.Octo.Sdk.ServiceClient.BotServices;

namespace Sdk.ServiceClient.Tests.BotServices;

/// <summary>
///     A tenant dump download that breaks off before the announced length must fail and must not leave a file under the
///     target name (AB#6969).
/// </summary>
public class BotServicesClientDownloadTests
{
    private const int AnnouncedBytes = 8 * 1024 * 1024;
    private const int SentBytes = 3 * 1024 * 1024;

    [Fact]
    public async Task DownloadDumpToFile_ConnectionResetMidBody_Throws_AndLeavesNoFile()
    {
        using var dir = new TempDirectory();
        await using var server = new TruncatingServer(AnnouncedBytes, SentBytes, reset: true);
        var target = Path.Combine(dir.Path, "dump.octobak.zip");

        await Assert.ThrowsAnyAsync<Exception>(() => CreateClient(server).DownloadDumpToFileAsync("tenant", "job-1",
            target, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Empty(Directory.GetFileSystemEntries(dir.Path));
    }

    [Fact]
    public async Task DownloadDumpToFile_CleanEndOfStreamBeforeContentLength_Throws_AndLeavesNoFile()
    {
        using var dir = new TempDirectory();
        await using var server = new TruncatingServer(AnnouncedBytes, SentBytes, reset: false);
        var target = Path.Combine(dir.Path, "dump.octobak.zip");

        var exception = await Assert.ThrowsAnyAsync<Exception>(() => CreateClient(server).DownloadDumpToFileAsync(
            "tenant", "job-1", target, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("broke off", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Directory.GetFileSystemEntries(dir.Path));
    }

    [Fact]
    public async Task DownloadDumpToFile_BreakOffReplacingAnExistingFile_KeepsTheExistingFile()
    {
        using var dir = new TempDirectory();
        await using var server = new TruncatingServer(AnnouncedBytes, SentBytes, reset: false);
        var target = Path.Combine(dir.Path, "dump.octobak.zip");
        await File.WriteAllTextAsync(target, "previous", TestContext.Current.CancellationToken);

        await Assert.ThrowsAnyAsync<Exception>(() => CreateClient(server).DownloadDumpToFileAsync("tenant", "job-1",
            target, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("previous", await File.ReadAllTextAsync(target, TestContext.Current.CancellationToken));
        Assert.Single(Directory.GetFileSystemEntries(dir.Path));
    }

    [Fact]
    public async Task DownloadDumpToFile_CompleteBody_WritesTheTargetFileOnly()
    {
        using var dir = new TempDirectory();
        await using var server = new TruncatingServer(SentBytes, SentBytes, reset: false);
        var target = Path.Combine(dir.Path, "dump.octobak.zip");
        long reported = 0;

        await CreateClient(server).DownloadDumpToFileAsync("tenant", "job-1", target, bytes => reported = bytes,
            TestContext.Current.CancellationToken);

        Assert.Equal(SentBytes, new FileInfo(target).Length);
        Assert.Equal(SentBytes, reported);
        Assert.Single(Directory.GetFileSystemEntries(dir.Path));
    }

    private static BotServicesClient CreateClient(TruncatingServer server)
    {
        var options = new BotServiceClientOptions { EndpointUri = server.BaseUrl };
        return new BotServicesClient(options, A.Fake<IBotServiceClientAccessToken>());
    }

    /// <summary>
    ///     Answers every request with a 200 that announces <c>announced</c> bytes, sends <c>sent</c> of them and then
    ///     closes the connection cleanly (FIN) or with a reset (RST).
    /// </summary>
    private sealed class TruncatingServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _loop;

        public TruncatingServer(int announced, int sent, bool reset)
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            BaseUrl = $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}";
            _loop = Task.Run(() => AcceptLoopAsync(announced, sent, reset));
        }

        public string BaseUrl { get; }

        private async Task AcceptLoopAsync(int announced, int sent, bool reset)
        {
            try
            {
                while (!_stop.IsCancellationRequested)
                {
                    var tcp = await _listener.AcceptTcpClientAsync(_stop.Token);
                    _ = Task.Run(() => ServeAsync(tcp, announced, sent, reset));
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private static async Task ServeAsync(TcpClient tcp, int announced, int sent, bool reset)
        {
            using (tcp)
            {
                try
                {
                    var stream = tcp.GetStream();
                    var buffer = new byte[4096];
                    var request = new StringBuilder();
                    while (!request.ToString().Contains("\r\n\r\n", StringComparison.Ordinal))
                    {
                        var read = await stream.ReadAsync(buffer);
                        if (read == 0)
                        {
                            return;
                        }

                        request.Append(Encoding.ASCII.GetString(buffer, 0, read));
                    }

                    var head = Encoding.ASCII.GetBytes(
                        $"HTTP/1.1 200 OK\r\nContent-Type: application/zip\r\nContent-Length: {announced}\r\nConnection: close\r\n\r\n");
                    await stream.WriteAsync(head);

                    var chunk = new byte[64 * 1024];
                    var remaining = sent;
                    while (remaining > 0)
                    {
                        var count = Math.Min(chunk.Length, remaining);
                        await stream.WriteAsync(chunk.AsMemory(0, count));
                        remaining -= count;
                    }

                    await stream.FlushAsync();
                    if (reset)
                    {
                        tcp.LingerState = new LingerOption(true, 0);
                    }
                }
                catch (IOException)
                {
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _stop.CancelAsync();
            _listener.Stop();
            await _loop;
            _stop.Dispose();
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "octo-download-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, true);
    }
}
