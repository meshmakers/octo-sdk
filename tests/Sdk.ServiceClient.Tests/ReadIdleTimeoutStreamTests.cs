using Meshmakers.Octo.Sdk.ServiceClient;

namespace Sdk.ServiceClient.Tests;

/// <summary>
///     AB#8920: the read-idle timeout restarts with every read, fails a stalled read with a
///     <see cref="TimeoutException" /> and leaves a caller's cancellation a cancellation.
/// </summary>
public class ReadIdleTimeoutStreamTests
{
    private const int TestTimeoutMs = 30_000;
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromMilliseconds(300);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Wrap_WithoutAPositiveTimeout_ReturnsTheInnerStream(int milliseconds)
    {
        using var inner = new MemoryStream();

        Assert.Same(inner, ReadIdleTimeoutStream.Wrap(inner, TimeSpan.FromMilliseconds(milliseconds), "x"));
        Assert.Same(inner, ReadIdleTimeoutStream.Wrap(inner, Timeout.InfiniteTimeSpan, "x"));
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task ReadAsync_StalledStream_ThrowsTimeoutExceptionNamingTheTransfer()
    {
        await using var stream = ReadIdleTimeoutStream.Wrap(new ScriptedStream(chunks: 0, delay: TimeSpan.Zero),
            IdleTimeout, "The download of job 'job-7'");

        var exception = await Assert.ThrowsAsync<TimeoutException>(async () =>
            await stream.ReadExactlyAsync(new byte[16], TestContext.Current.CancellationToken));

        Assert.StartsWith("The download of job 'job-7' received no data for", exception.Message, StringComparison.Ordinal);
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task Read_StalledStream_ThrowsTimeoutExceptionToo()
    {
        await using var stream = ReadIdleTimeoutStream.Wrap(new ScriptedStream(chunks: 0, delay: TimeSpan.Zero),
            IdleTimeout, "x");

        // The synchronous read runs on a pool thread so that the test token can still end the test.
        await Assert.ThrowsAsync<TimeoutException>(() =>
            Task.Run(() => stream.ReadExactly(new byte[16]), TestContext.Current.CancellationToken));
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task ReadAsync_SlowButProgressingStream_ReadsToTheEnd()
    {
        // Five chunks 150 ms apart: longer in total than the idle timeout, every single wait shorter.
        await using var stream = ReadIdleTimeoutStream.Wrap(
            new ScriptedStream(chunks: 5, delay: TimeSpan.FromMilliseconds(150)), IdleTimeout, "x");
        using var target = new MemoryStream();

        await stream.CopyToAsync(target, TestContext.Current.CancellationToken);

        Assert.Equal(5 * ScriptedStream.ChunkBytes, target.Length);
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task ReadAsync_CallerCancels_StaysAnOperationCanceledException()
    {
        await using var stream = ReadIdleTimeoutStream.Wrap(new ScriptedStream(chunks: 0, delay: TimeSpan.Zero),
            TimeSpan.FromMinutes(5), "x");
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        caller.CancelAfter(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await stream.ReadExactlyAsync(new byte[16], caller.Token));
    }

    /// <summary>
    ///     Delivers <c>chunks</c> chunks, each after <c>delay</c>, then stalls forever (until the read is cancelled) when
    ///     <c>chunks</c> is zero, or ends otherwise.
    /// </summary>
    internal sealed class ScriptedStream(int chunks, TimeSpan delay) : Stream
    {
        public const int ChunkBytes = 1024;
        private int _delivered;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (chunks == 0)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            if (_delivered == chunks)
            {
                return 0;
            }

            await Task.Delay(delay, cancellationToken);
            _delivered++;
            var count = Math.Min(ChunkBytes, buffer.Length);
            buffer.Span[..count].Fill(1);
            return count;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override int Read(byte[] buffer, int offset, int count) =>
            ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
