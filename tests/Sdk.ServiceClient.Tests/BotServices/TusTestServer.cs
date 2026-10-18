using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Sdk.ServiceClient.Tests.BotServices;

/// <summary>
///     A tus test server that behaves like the bot service behind an ingress: it enforces a request body limit,
///     keeps the offset and a running hash of the bytes it has accepted, and can drop the connection once in the
///     middle of a part. Used to prove what the SDK sends, not only that something was sent (AB#6786).
/// </summary>
/// <remarks>
///     Used as a class fixture: <c>HttpListener.Prefixes.Add</c> costs about five seconds on macOS (see
///     <see cref="LoopbackHttpService" />). A test configures it with <see cref="Configure" />, which also resets the
///     state, and creates its upload with <see cref="CreateUploadAsync" /> style calls through the SDK.
/// </remarks>
public sealed class TusTestServer : IDisposable
{
    public const string TusFileId = "tus-file-1";

    private readonly HttpListener _listener = new();
    private readonly Lock _sync = new();
    private readonly List<(string Method, long? Offset, long BodyLength)> _requests = [];

    private IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    private long _offset;
    private long _declaredLength;
    private long _bodyLimit = long.MaxValue;
    private bool _respond413ToLargerBodies = true;
    private int _serverErrorOnPatchNumber = -1;
    private int _refuseFromPatchNumber = -1;
    private int _patchCount;
    private string _metadataHeader = string.Empty;

    public TusTestServer()
    {
        var port = FreeTcpPort();
        BaseUrl = $"http://127.0.0.1:{port}/";
        _listener.Prefixes.Add(BaseUrl);
        _listener.Start();

        _ = Task.Run(ServeAsync);
    }

    public string BaseUrl { get; }

    /// <summary>Method, offset header (PATCH) and body length of every request received, in order.</summary>
    public IReadOnlyList<(string Method, long? Offset, long BodyLength)> Requests
    {
        get
        {
            lock (_sync)
            {
                return _requests.ToArray();
            }
        }
    }

    /// <summary>The offset the server has accepted so far.</summary>
    public long Offset
    {
        get
        {
            lock (_sync)
            {
                return _offset;
            }
        }
    }

    /// <summary>SHA-256 of all accepted bytes so far, hex.</summary>
    public string ReceivedSha256
    {
        get
        {
            lock (_sync)
            {
                using var copy = _hash.Clone();
                return Convert.ToHexString(copy.GetHashAndReset());
            }
        }
    }

    public string UploadMetadata
    {
        get
        {
            lock (_sync)
            {
                return _metadataHeader;
            }
        }
    }

    /// <summary>
    ///     Resets the state and sets the behaviour of the next test.
    /// </summary>
    /// <param name="bodyLimitBytes">A PATCH with a larger body is answered 413 (like the ingress), 0 = no limit.</param>
    /// <param name="serverErrorOnPatchNumber">The n-th PATCH (1-based) is answered 503 without being applied; -1 = never.</param>
    /// <param name="refuseFromPatchNumber">Every PATCH from the n-th on (1-based) is answered 413; -1 = never.</param>
    public void Configure(long bodyLimitBytes = 0,
        int serverErrorOnPatchNumber = -1, int refuseFromPatchNumber = -1)
    {
        lock (_sync)
        {
            _requests.Clear();
            _hash.Dispose();
            _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            _offset = 0;
            _declaredLength = 0;
            _patchCount = 0;
            _bodyLimit = bodyLimitBytes <= 0 ? long.MaxValue : bodyLimitBytes;
            _respond413ToLargerBodies = true;
            _serverErrorOnPatchNumber = serverErrorOnPatchNumber;
            _refuseFromPatchNumber = refuseFromPatchNumber;
            _metadataHeader = string.Empty;
        }
    }

    public void Dispose()
    {
        _listener.Abort();
        _hash.Dispose();
    }

    private async Task ServeAsync()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception)
            {
                return;
            }

            try
            {
                await RespondAsync(context);
            }
            catch (Exception)
            {
                // A client that gave up must not take the listener down.
            }
        }
    }

    private async Task RespondAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        var path = request.Url?.AbsolutePath ?? string.Empty;

        if (request.HttpMethod == "POST" && path.EndsWith("/tus-upload", StringComparison.Ordinal))
        {
            lock (_sync)
            {
                _requests.Add(("POST", null, 0));
                _declaredLength = long.Parse(request.Headers["Upload-Length"] ?? "0");
                _metadataHeader = request.Headers["Upload-Metadata"] ?? string.Empty;
            }

            response.StatusCode = (int)HttpStatusCode.Created;
            response.AddHeader("Tus-Resumable", "1.0.0");
            response.AddHeader("Location", $"{BaseUrl.TrimEnd('/')}{path}/{TusFileId}");
            response.ContentLength64 = 0;
            response.Close();
            return;
        }

        if (request.HttpMethod == "HEAD")
        {
            long offset;
            lock (_sync)
            {
                _requests.Add(("HEAD", null, 0));
                offset = _offset;
            }

            response.StatusCode = (int)HttpStatusCode.OK;
            response.AddHeader("Tus-Resumable", "1.0.0");
            response.AddHeader("Upload-Offset", offset.ToString());
            response.AddHeader("Cache-Control", "no-store");
            response.ContentLength64 = 0;
            response.Close();
            return;
        }

        if (request.HttpMethod == "PATCH")
        {
            await HandlePatchAsync(context);
            return;
        }

        // Anything else (the job start): a canned job answer.
        var body = Encoding.UTF8.GetBytes("""{"jobId":"job-1","id":"job-1","status":"Succeeded"}""");
        response.StatusCode = (int)HttpStatusCode.OK;
        response.ContentType = "application/json";
        response.ContentLength64 = body.Length;
        await response.OutputStream.WriteAsync(body);
        response.Close();
    }

    private async Task HandlePatchAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        var claimedOffset = long.Parse(request.Headers["Upload-Offset"] ?? "-1");
        var contentLength = request.ContentLength64;

        int patchNumber;
        lock (_sync)
        {
            patchNumber = ++_patchCount;
        }

        // The ingress decides by the declared body size, before it forwards anything.
        if ((contentLength > _bodyLimit && _respond413ToLargerBodies) ||
            (_refuseFromPatchNumber > 0 && patchNumber >= _refuseFromPatchNumber))
        {
            Record(claimedOffset, contentLength);
            await DrainAsync(request);
            response.StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
            response.ContentLength64 = 0;
            response.Close();
            return;
        }

        if (patchNumber == _serverErrorOnPatchNumber)
        {
            Record(claimedOffset, contentLength);
            await DrainAsync(request);
            response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            response.ContentLength64 = 0;
            response.Close();
            return;
        }

        long currentOffset;
        lock (_sync)
        {
            currentOffset = _offset;
        }

        if (claimedOffset != currentOffset)
        {
            Record(claimedOffset, contentLength);
            await DrainAsync(request);
            response.StatusCode = (int)HttpStatusCode.Conflict;
            response.ContentLength64 = 0;
            response.Close();
            return;
        }

        // Accept bytes as they arrive, like a real tus server: an interrupted request keeps what it delivered.
        long received = 0;
        var buffer = new byte[81920];
        while (true)
        {
            var read = await request.InputStream.ReadAsync(buffer);
            if (read == 0)
            {
                break;
            }

            lock (_sync)
            {
                _hash.AppendData(buffer, 0, read);
                _offset += read;
            }

            received += read;

        }

        Record(claimedOffset, received);
        long newOffset;
        lock (_sync)
        {
            newOffset = _offset;
        }

        response.StatusCode = (int)HttpStatusCode.NoContent;
        response.AddHeader("Tus-Resumable", "1.0.0");
        response.AddHeader("Upload-Offset", newOffset.ToString());
        response.ContentLength64 = 0;
        response.Close();
    }

    /// <summary>
    ///     Reads and discards the request body, so the refusal reaches the client as a status instead of a reset
    ///     connection (a real ingress behaves either way; the SDK reports both).
    /// </summary>
    private static async Task DrainAsync(HttpListenerRequest request)
    {
        var buffer = new byte[81920];
        while (await request.InputStream.ReadAsync(buffer) > 0)
        {
        }
    }

    private void Record(long offset, long bodyLength)
    {
        lock (_sync)
        {
            _requests.Add(("PATCH", offset, bodyLength));
        }
    }

    private static int FreeTcpPort()
    {
        using var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork,
            System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }
}
