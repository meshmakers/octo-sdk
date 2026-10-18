using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

namespace Meshmakers.Octo.Sdk.ServiceClient.BotServices;

/// <summary>
///     Sends the content of a file to an already created tus upload in parts (AB#6786).
/// </summary>
/// <remarks>
///     <para>
///         🔴 <b>Why not <c>TusPatchAsync</c> of BirdMessenger.</b> Its default upload type streams the whole file in
///         one PATCH request, whatever <c>UploadBufferSize</c> says. The ingress of the managed environments
///         refuses a body above 100m with 413 (the ingress log reads "client intended to send too large body"), and
///         BirdMessenger did not report the refusal — the restore job then failed on an empty file. Here every
///         request body is at most the part size, the parts are aligned to multiples of the part size, a refusal
///         throws a <see cref="TusUploadException" /> with status, part number and offset, and the client resumes from
///         the offset the server reports (<c>HEAD</c>) after a dropped connection.
///     </para>
///     <para>
///         The creation of the upload (<c>POST</c>) stays with BirdMessenger; this class starts at the <c>HEAD</c>
///         that reads the server's current offset, so an upload that already holds bytes continues instead of
///         starting over.
///     </para>
/// </remarks>
internal static class TusUploader
{
    private const string TusVersion = "1.0.0";
    private const string TusResumableHeader = "Tus-Resumable";
    private const string UploadOffsetHeader = "Upload-Offset";
    private const string OffsetContentType = "application/offset+octet-stream";

    /// <summary>Network failures of one part are retried this many times (resuming from the server offset).</summary>
    public const int DefaultMaxRetries = 5;

    /// <summary>Time one request of a part may take before it counts as a failed attempt.</summary>
    public static readonly TimeSpan DefaultPartTimeout = TimeSpan.FromMinutes(30);

    /// <summary>Base of the linear back-off between two attempts of a part.</summary>
    public static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     Uploads <paramref name="filePath" /> to <paramref name="fileLocation" />.
    /// </summary>
    /// <param name="httpClient">Client carrying the authorization header; its own timeout must be infinite.</param>
    /// <param name="fileLocation">The location returned when the upload was created.</param>
    /// <param name="filePath">The file to send.</param>
    /// <param name="partSizeBytes">Maximum number of bytes in the body of one request.</param>
    /// <param name="progressCallback">Receives the fraction (0..1) of the file the server holds or has just been sent.</param>
    /// <param name="maxRetries">How often the same part is retried after a network failure.</param>
    /// <param name="retryDelay">Base of the back-off between attempts.</param>
    /// <param name="partTimeout">Time limit of one request.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    public static async Task UploadAsync(HttpClient httpClient, Uri fileLocation, string filePath,
        long partSizeBytes, Action<double>? progressCallback = null, int maxRetries = DefaultMaxRetries,
        TimeSpan? retryDelay = null, TimeSpan? partTimeout = null, CancellationToken cancellationToken = default)
    {
        var length = new FileInfo(filePath).Length;
        var partCount = (length + partSizeBytes - 1) / partSizeBytes;
        var delay = retryDelay ?? DefaultRetryDelay;
        var timeout = partTimeout ?? DefaultPartTimeout;

        // The first HEAD is also what makes an upload that already holds bytes resumable.
        var offset = await ReadOffsetAsync(httpClient, fileLocation, 1, partCount, 0, partSizeBytes, timeout,
            cancellationToken);
        EnsureWithinFile(offset, length, 1, partCount, partSizeBytes);
        progressCallback?.Invoke(Fraction(offset, length));

        var attempt = 0;
        while (offset < length)
        {
            // Parts are aligned to multiples of the part size, also after a resume from an odd offset, so the
            // numbering is exact and no body is larger than the part size.
            var partNumber = offset / partSizeBytes + 1;
            var size = Math.Min(partSizeBytes - offset % partSizeBytes, length - offset);

            try
            {
                var newOffset = await SendPartAsync(httpClient, fileLocation, filePath, offset, size, partNumber,
                    partCount, partSizeBytes, length, progressCallback, timeout, cancellationToken);
                offset = newOffset;
                attempt = 0;
                progressCallback?.Invoke(Fraction(offset, length));
            }
            catch (PartFailure failure) when (failure.Retryable && attempt < maxRetries &&
                                              !cancellationToken.IsCancellationRequested)
            {
                attempt++;
                await Task.Delay(TimeSpan.FromTicks(delay.Ticks * attempt), cancellationToken);

                // Resume from what the server really holds: the failed request may have been applied in part.
                try
                {
                    var serverOffset = await ReadOffsetAsync(httpClient, fileLocation, partNumber, partCount, offset,
                        partSizeBytes, timeout, cancellationToken);
                    if (serverOffset < offset)
                    {
                        throw new TusUploadException(
                            $"Upload of part {partNumber} of {partCount} (offset {offset}) cannot resume: the " +
                            $"server reports offset {serverOffset}, behind the offset it had acknowledged before.",
                            null, partNumber, partCount, offset, partSizeBytes);
                    }

                    EnsureWithinFile(serverOffset, length, partNumber, partCount, partSizeBytes);
                    offset = serverOffset;
                }
                catch (TusUploadException e) when (e.StatusCode is null && e.InnerException is not null)
                {
                    // The connection is still down; the next attempt answers 409 and re-reads the offset.
                }
            }
            catch (PartFailure failure)
            {
                throw failure.ToException(partNumber, partCount, offset, partSizeBytes);
            }
        }
    }

    private static void EnsureWithinFile(long offset, long length, long partNumber, long partCount,
        long partSizeBytes)
    {
        if (offset > length)
        {
            throw new TusUploadException(
                $"The server reports offset {offset}, beyond the file length of {length} bytes.",
                null, partNumber, partCount, offset, partSizeBytes);
        }
    }

    private static double Fraction(long offset, long length)
    {
        return length <= 0 ? 1d : Math.Min(1d, (double)offset / length);
    }

    private static async Task<long> ReadOffsetAsync(HttpClient httpClient, Uri fileLocation, long partNumber,
        long partCount, long knownOffset, long partSizeBytes, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, fileLocation);
        request.Headers.Add(TusResumableHeader, TusVersion);

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        try
        {
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                timeoutSource.Token);
            if (!response.IsSuccessStatusCode)
            {
                throw new TusUploadException(
                    $"Reading the upload offset before part {partNumber} of {partCount} failed: " +
                    $"{(int)response.StatusCode} {response.StatusCode}.",
                    response.StatusCode, partNumber, partCount, knownOffset, partSizeBytes);
            }

            return ReadOffsetHeader(response) ??
                   throw new TusUploadException(
                       $"Reading the upload offset before part {partNumber} of {partCount} failed: the server sent " +
                       "no Upload-Offset header.",
                       response.StatusCode, partNumber, partCount, knownOffset, partSizeBytes);
        }
        catch (Exception e) when (e is HttpRequestException or IOException ||
                                  (e is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new TusUploadException(
                $"Reading the upload offset before part {partNumber} of {partCount} failed: {e.GetType().Name}.",
                null, partNumber, partCount, knownOffset, partSizeBytes, e);
        }
    }

    private static async Task<long> SendPartAsync(HttpClient httpClient, Uri fileLocation, string filePath,
        long offset, long size, long partNumber, long partCount, long partSizeBytes, long length,
        Action<double>? progressCallback, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, fileLocation);
        request.Headers.Add(TusResumableHeader, TusVersion);
        request.Headers.Add(UploadOffsetHeader, offset.ToString(CultureInfo.InvariantCulture));

        // Wait for the answer to the headers before the body goes out: an ingress that refuses the part answers
        // 413 straight away instead of resetting the connection while the body is still being sent.
        request.Headers.ExpectContinue = true;
        request.Content = new FilePartContent(filePath, offset, size, sent =>
            progressCallback?.Invoke(Fraction(offset + sent, length)));

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        try
        {
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                timeoutSource.Token);
            if (!response.IsSuccessStatusCode)
            {
                throw PartFailure.FromStatus(response.StatusCode);
            }

            var newOffset = ReadOffsetHeader(response);
            if (newOffset is null || newOffset <= offset || newOffset > offset + size)
            {
                throw new TusUploadException(
                    $"Upload of part {partNumber} of {partCount} (offset {offset}) was answered with " +
                    $"{(int)response.StatusCode} but without a usable Upload-Offset.",
                    response.StatusCode, partNumber, partCount, offset, partSizeBytes);
            }

            return newOffset.Value;
        }
        catch (Exception e) when (e is HttpRequestException or IOException ||
                                  (e is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw PartFailure.FromConnection(e);
        }
    }

    private static long? ReadOffsetHeader(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues(UploadOffsetHeader, out var values) &&
            long.TryParse(values.FirstOrDefault(), NumberStyles.None, CultureInfo.InvariantCulture, out var offset))
        {
            return offset;
        }

        return null;
    }

    /// <summary>
    ///     Internal carrier for a failed request of one part; turned into a <see cref="TusUploadException" /> once
    ///     the part number and offset are known to the caller.
    /// </summary>
    private sealed class PartFailure : Exception
    {
        private readonly Exception? _cause;
        private readonly HttpStatusCode? _status;

        private PartFailure(HttpStatusCode? status, Exception? cause, bool retryable)
        {
            _status = status;
            _cause = cause;
            Retryable = retryable;
        }

        public bool Retryable { get; }

        public static PartFailure FromStatus(HttpStatusCode status)
        {
            // 409 is the tus answer to an offset the server does not hold: re-read the offset and continue.
            var retryable = (int)status >= 500 || status is HttpStatusCode.RequestTimeout or HttpStatusCode.Conflict;
            return new PartFailure(status, null, retryable);
        }

        public static PartFailure FromConnection(Exception cause)
        {
            return new PartFailure(null, cause, true);
        }

        public TusUploadException ToException(long partNumber, long partCount, long offset, long partSizeBytes)
        {
            var location = $"part {partNumber} of {partCount} (offset {offset}, at most {partSizeBytes} bytes)";
            if (_status is { } status)
            {
                var hint = status == HttpStatusCode.RequestEntityTooLarge
                    ? " The request body is above the body limit of the server or the ingress in front of it; " +
                      "configure a smaller BotServiceClientOptions.TusPartSizeBytes."
                    : string.Empty;
                return new TusUploadException(
                    $"Upload of {location} was refused: {(int)status} {status}.{hint}",
                    status, partNumber, partCount, offset, partSizeBytes);
            }

            return new TusUploadException(
                $"Upload of {location} failed, the connection was lost ({_cause?.GetType().Name}). A connection " +
                "closed while the body is sent can also mean the ingress refused a body above its limit; " +
                "configure a smaller BotServiceClientOptions.TusPartSizeBytes if this repeats.",
                null, partNumber, partCount, offset, partSizeBytes, _cause);
        }
    }

    /// <summary>
    ///     The bytes <c>[offset, offset + size)</c> of a file as the body of one PATCH request, with a known
    ///     length (no chunked encoding) and without reading the part into memory.
    /// </summary>
    private sealed class FilePartContent : HttpContent
    {
        private const int BufferSize = 81920;
        private const long ProgressStep = 1024 * 1024;

        private readonly string _filePath;
        private readonly long _offset;
        private readonly long _size;
        private readonly Action<long> _onSent;

        public FilePartContent(string filePath, long offset, long size, Action<long> onSent)
        {
            _filePath = filePath;
            _offset = offset;
            _size = size;
            _onSent = onSent;
            Headers.ContentType = new MediaTypeHeaderValue(OffsetContentType);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return SerializeToStreamAsync(stream, context, CancellationToken.None);
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context,
            CancellationToken cancellationToken)
        {
            await using var file = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                BufferSize, true);
            file.Seek(_offset, SeekOrigin.Begin);

            var buffer = new byte[BufferSize];
            long sent = 0;
            long reported = 0;
            while (sent < _size)
            {
                var toRead = (int)Math.Min(buffer.Length, _size - sent);
                var read = await file.ReadAsync(buffer.AsMemory(0, toRead), cancellationToken);
                if (read == 0)
                {
                    throw new IOException("The file ended before the part was complete; it was changed during the upload.");
                }

                await stream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                sent += read;

                if (sent - reported >= ProgressStep)
                {
                    reported = sent;
                    _onSent(sent);
                }
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _size;
            return true;
        }
    }
}
