using System.Net;
using System.Net.Sockets;
using System.Text;
using Meshmakers.Octo.Sdk.ServiceClient.BotServices;

namespace Sdk.ServiceClient.Tests.BotServices;

/// <summary>
///     A tenant dump download that breaks off before the announced length must fail and must not leave a file under the
///     target name (AB#6969); a download whose body stalls must fail after the read-idle timeout instead of waiting
///     forever, while a slow but progressing one completes (AB#8920).
/// </summary>
/// <remarks>
///     Every test has a timeout: a regression must fail the test, not hang the test run (AB#8920).
/// </remarks>
public class BotServicesClientDownloadTests
{
    private const int TestTimeoutMs = 60_000;
    private const int AnnouncedBytes = 8 * 1024 * 1024;
    private const int SentBytes = 3 * 1024 * 1024;

    // Small enough to sit in the socket buffers at once, so the server closes with RST right after the write instead
    // of racing a large body against the reset (the 3 MiB variant hung 2 of 5 runs on macOS before AB#8920).
    private const int ResetAfterBytes = 64 * 1024;

    private static readonly TimeSpan ShortIdleTimeout = TimeSpan.FromMilliseconds(500);

    [Fact(Timeout = TestTimeoutMs)]
    public async Task DownloadDumpToFile_ConnectionResetMidBody_Throws_AndLeavesNoFile()
    {
        using var dir = new TempDirectory();
        await using var server = new TruncatingServer(AnnouncedBytes, ResetAfterBytes, BodyEnd.Reset);
        var target = Path.Combine(dir.Path, "dump.octobak.zip");

        await Assert.ThrowsAnyAsync<Exception>(() => CreateClient(server).DownloadDumpToFileAsync("tenant", "job-1",
            target, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Empty(Directory.GetFileSystemEntries(dir.Path));
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task DownloadDumpToFile_CleanEndOfStreamBeforeContentLength_Throws_AndLeavesNoFile()
    {
        using var dir = new TempDirectory();
        await using var server = new TruncatingServer(AnnouncedBytes, SentBytes, BodyEnd.Close);
        var target = Path.Combine(dir.Path, "dump.octobak.zip");

        var exception = await Assert.ThrowsAnyAsync<Exception>(() => CreateClient(server).DownloadDumpToFileAsync(
            "tenant", "job-1", target, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("broke off", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Directory.GetFileSystemEntries(dir.Path));
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task DownloadDumpToFile_BreakOffReplacingAnExistingFile_KeepsTheExistingFile()
    {
        using var dir = new TempDirectory();
        await using var server = new TruncatingServer(AnnouncedBytes, SentBytes, BodyEnd.Close);
        var target = Path.Combine(dir.Path, "dump.octobak.zip");
        await File.WriteAllTextAsync(target, "previous", TestContext.Current.CancellationToken);

        await Assert.ThrowsAnyAsync<Exception>(() => CreateClient(server).DownloadDumpToFileAsync("tenant", "job-1",
            target, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("previous", await File.ReadAllTextAsync(target, TestContext.Current.CancellationToken));
        Assert.Single(Directory.GetFileSystemEntries(dir.Path));
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task DownloadDumpToFile_CompleteBody_WritesTheTargetFileOnly()
    {
        using var dir = new TempDirectory();
        await using var server = new TruncatingServer(SentBytes, SentBytes, BodyEnd.Close);
        var target = Path.Combine(dir.Path, "dump.octobak.zip");
        long reported = 0;

        await CreateClient(server).DownloadDumpToFileAsync("tenant", "job-1", target, bytes => reported = bytes,
            TestContext.Current.CancellationToken);

        Assert.Equal(SentBytes, new FileInfo(target).Length);
        Assert.Equal(SentBytes, reported);
        Assert.Single(Directory.GetFileSystemEntries(dir.Path));
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task DownloadDumpToFile_BodyStalls_FailsAfterTheIdleTimeout_AndLeavesNoFile()
    {
        using var dir = new TempDirectory();
        await using var server = new TruncatingServer(AnnouncedBytes, ResetAfterBytes, BodyEnd.Stall);
        var target = Path.Combine(dir.Path, "dump.octobak.zip");

        var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
            CreateClient(server, ShortIdleTimeout).DownloadDumpToFileAsync("tenant", "job-1", target,
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("job-1", exception.Message, StringComparison.Ordinal);
        Assert.Contains("read-idle timeout", exception.Message, StringComparison.Ordinal);
        Assert.Empty(Directory.GetFileSystemEntries(dir.Path));
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task DownloadDumpToFile_SlowButProgressingBody_IsNotAborted()
    {
        // 10 chunks, 200 ms apart: the whole body takes about four times the idle timeout, every gap stays below it.
        const int chunkBytes = 16 * 1024;
        const int chunks = 10;
        using var dir = new TempDirectory();
        await using var server = new TruncatingServer(chunkBytes * chunks, chunkBytes * chunks, BodyEnd.Close,
            chunkBytes, TimeSpan.FromMilliseconds(200));
        var target = Path.Combine(dir.Path, "dump.octobak.zip");

        await CreateClient(server, ShortIdleTimeout).DownloadDumpToFileAsync("tenant", "job-1", target,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(chunkBytes * chunks, new FileInfo(target).Length);
        Assert.Single(Directory.GetFileSystemEntries(dir.Path));
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task DownloadDumpToFile_CallerCancelsAStalledBody_StaysACancellation_AndLeavesNoFile()
    {
        using var dir = new TempDirectory();
        await using var server = new TruncatingServer(AnnouncedBytes, ResetAfterBytes, BodyEnd.Stall);
        var target = Path.Combine(dir.Path, "dump.octobak.zip");
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        caller.CancelAfter(TimeSpan.FromMilliseconds(300));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateClient(server, TimeSpan.FromMinutes(5)).DownloadDumpToFileAsync("tenant", "job-1", target,
                cancellationToken: caller.Token));

        Assert.Empty(Directory.GetFileSystemEntries(dir.Path));
    }

    private static BotServicesClient CreateClient(TruncatingServer server, TimeSpan? idleTimeout = null)
    {
        var options = new BotServiceClientOptions { EndpointUri = server.BaseUrl };
        if (idleTimeout.HasValue)
        {
            options.StreamReadIdleTimeout = idleTimeout.Value;
        }

        return new BotServicesClient(options, A.Fake<IBotServiceClientAccessToken>());
    }

    private enum BodyEnd
    {
        /// <summary>Close the connection cleanly (FIN).</summary>
        Close,

        /// <summary>Close the connection with a reset (RST).</summary>
        Reset,

        /// <summary>Keep the connection open and send nothing more until the server is disposed.</summary>
        Stall
    }

    /// <summary>
    ///     Answers every request with a 200 that announces <c>announced</c> bytes, sends <c>sent</c> of them (in chunks,
    ///     optionally with a pause between them) and then ends the body as <see cref="BodyEnd" /> says.
    /// </summary>
    private sealed class TruncatingServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _loop;
        private readonly int _announced;
        private readonly int _sent;
        private readonly BodyEnd _end;
        private readonly int _chunkBytes;
        private readonly TimeSpan _chunkDelay;

        public TruncatingServer(int announced, int sent, BodyEnd end, int chunkBytes = 64 * 1024,
            TimeSpan chunkDelay = default)
        {
            _announced = announced;
            _sent = sent;
            _end = end;
            _chunkBytes = chunkBytes;
            _chunkDelay = chunkDelay;
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            BaseUrl = $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}";
            _loop = Task.Run(AcceptLoopAsync);
        }

        public string BaseUrl { get; }

        private async Task AcceptLoopAsync()
        {
            try
            {
                while (!_stop.IsCancellationRequested)
                {
                    var tcp = await _listener.AcceptTcpClientAsync(_stop.Token);
                    _ = Task.Run(() => ServeAsync(tcp));
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private async Task ServeAsync(TcpClient tcp)
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
                        var read = await stream.ReadAsync(buffer, _stop.Token);
                        if (read == 0)
                        {
                            return;
                        }

                        request.Append(Encoding.ASCII.GetString(buffer, 0, read));
                    }

                    var head = Encoding.ASCII.GetBytes(
                        $"HTTP/1.1 200 OK\r\nContent-Type: application/zip\r\nContent-Length: {_announced}\r\nConnection: close\r\n\r\n");
                    await stream.WriteAsync(head, _stop.Token);

                    var chunk = new byte[_chunkBytes];
                    var remaining = _sent;
                    while (remaining > 0)
                    {
                        var count = Math.Min(chunk.Length, remaining);
                        await stream.WriteAsync(chunk.AsMemory(0, count), _stop.Token);
                        await stream.FlushAsync(_stop.Token);
                        remaining -= count;
                        if (_chunkDelay > TimeSpan.Zero && remaining > 0)
                        {
                            await Task.Delay(_chunkDelay, _stop.Token);
                        }
                    }

                    switch (_end)
                    {
                        case BodyEnd.Reset:
                            tcp.LingerState = new LingerOption(true, 0);
                            break;
                        case BodyEnd.Stall:
                            await Task.Delay(Timeout.Infinite, _stop.Token);
                            break;
                    }
                }
                catch (Exception e) when (e is IOException or OperationCanceledException or ObjectDisposedException)
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
