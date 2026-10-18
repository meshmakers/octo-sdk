using System.Net;

namespace Meshmakers.Octo.Sdk.ServiceClient.BotServices;

/// <summary>
///     Thrown when a part of a tus upload to the bot services is refused or cannot be delivered (AB#6786).
/// </summary>
/// <remarks>
///     <para>
///         The upload runs in parts (<see cref="BotServiceClientOptions.TusPartSizeBytes" />). The exception names the
///         part and its offset so a refused upload is reported for what it is — before AB#6786 a refused
///         single-request upload surfaced only later as a restore job failing on an "empty (0 bytes)" file.
///     </para>
///     <para>
///         It carries no file content and no response body: only the status, the part number, the offset and the
///         part size.
///     </para>
/// </remarks>
[Serializable]
public sealed class TusUploadException : ServiceClientException
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="message">The message; describes the failed part without file content.</param>
    /// <param name="statusCode">The HTTP status the server (or the ingress in front of it) answered, if any.</param>
    /// <param name="partNumber">The 1-based number of the part that failed.</param>
    /// <param name="partCount">The number of parts of the whole upload.</param>
    /// <param name="offset">The byte offset of the part within the file.</param>
    /// <param name="partSizeBytes">The configured maximum part size.</param>
    /// <param name="inner">The underlying exception, if the failure had no HTTP answer.</param>
    public TusUploadException(string message, HttpStatusCode? statusCode, long partNumber, long partCount,
        long offset, long partSizeBytes, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
        PartNumber = partNumber;
        PartCount = partCount;
        Offset = offset;
        PartSizeBytes = partSizeBytes;
    }

    /// <summary>
    ///     The HTTP status answered for the part; <c>null</c> when the connection failed before an answer arrived.
    /// </summary>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>
    ///     The 1-based number of the part that failed.
    /// </summary>
    public long PartNumber { get; }

    /// <summary>
    ///     The number of parts of the whole upload.
    /// </summary>
    public long PartCount { get; }

    /// <summary>
    ///     The byte offset of the failed part within the file.
    /// </summary>
    public long Offset { get; }

    /// <summary>
    ///     The configured maximum size of one part in bytes.
    /// </summary>
    public long PartSizeBytes { get; }
}
