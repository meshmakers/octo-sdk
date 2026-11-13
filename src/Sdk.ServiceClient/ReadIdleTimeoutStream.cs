namespace Meshmakers.Octo.Sdk.ServiceClient;

/// <summary>
///     Read-only stream wrapper that fails a read when the inner stream delivers no data within an idle timeout
///     (AB#8920). Every read gets its own timer, linked to the caller's token, so the timeout restarts after each
///     successful read: a slow but progressing body is never aborted, a stalled one fails with a
///     <see cref="TimeoutException" /> instead of waiting forever. A cancellation by the caller stays an
///     <see cref="OperationCanceledException" />.
/// </summary>
/// <remarks>
///     Meant for HTTP response bodies read after <see cref="System.Net.Http.HttpCompletionOption.ResponseHeadersRead" />,
///     where <see cref="System.Net.Http.HttpClient.Timeout" /> no longer applies. Cancelling a read of a
///     <c>SocketsHttpHandler</c> content stream aborts the connection, so the wrapper must be the last reader of it.
///     Synchronous reads block on the asynchronous read so that they honour the timeout too.
/// </remarks>
internal sealed class ReadIdleTimeoutStream : Stream
{
    private readonly Stream _inner;
    private readonly TimeSpan _idleTimeout;
    private readonly string _description;

    private ReadIdleTimeoutStream(Stream inner, TimeSpan idleTimeout, string description)
    {
        _inner = inner;
        _idleTimeout = idleTimeout;
        _description = description;
    }

    /// <summary>
    ///     Wraps <paramref name="inner" /> when <paramref name="idleTimeout" /> is positive, otherwise returns it as is.
    /// </summary>
    /// <param name="inner">The stream to read from; disposed with the wrapper.</param>
    /// <param name="idleTimeout">Longest time one read may wait for data; zero or negative disables the timeout.</param>
    /// <param name="description">What is read, for the error message (e.g. "The download of job 'x'").</param>
    public static Stream Wrap(Stream inner, TimeSpan idleTimeout, string description)
    {
        ArgumentNullException.ThrowIfNull(inner);
        return idleTimeout > TimeSpan.Zero && idleTimeout != Timeout.InfiniteTimeSpan
            ? new ReadIdleTimeoutStream(inner, idleTimeout, description)
            : inner;
    }

    /// <inheritdoc />
    public override bool CanRead => _inner.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc />
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        using var idleSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        idleSource.CancelAfter(_idleTimeout);
        try
        {
            return await _inner.ReadAsync(buffer, idleSource.Token).ConfigureAwait(false);
        }
        catch (Exception e) when (e is OperationCanceledException or IOException &&
                                  idleSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // An aborted socket read can surface as IOException instead of OperationCanceledException.
            throw new TimeoutException(
                $"{_description} received no data for {_idleTimeout} (read-idle timeout); the connection is " +
                "considered dead.", e);
        }
    }

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        // Stream.Read(Span<byte>) of the base class lands here as well.
        return ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public override void Flush()
    {
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await _inner.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }
}
