namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;

/// <summary>
/// Wraps a streamed HTTP response body and keeps the owning <see cref="HttpResponseMessage"/> and
/// <see cref="HttpClient"/> alive for the lifetime of the returned content stream. Disposing this
/// stream tears the whole request chain down. Used by
/// <see cref="IStreamDataServicesClient.ExportArchiveRowsAsync"/> so callers can read NDJSON
/// line-by-line without the underlying connection being closed underneath them and without buffering
/// the full dataset.
/// </summary>
internal sealed class HttpOwningStream : Stream
{
    private readonly Stream _inner;
    private readonly HttpResponseMessage _response;
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public HttpOwningStream(Stream inner, HttpResponseMessage response, HttpClient httpClient)
    {
        _inner = inner;
        _response = response;
        _httpClient = httpClient;
    }

    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => _inner.CanSeek;
    public override bool CanWrite => false;
    public override long Length => _inner.Length;

    public override long Position
    {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    public override void Flush() => _inner.Flush();

    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _inner.ReadAsync(buffer, offset, count, cancellationToken);

    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _inner.Dispose();
            _response.Dispose();
            _httpClient.Dispose();
        }

        _disposed = true;
        base.Dispose(disposing);
    }
}
